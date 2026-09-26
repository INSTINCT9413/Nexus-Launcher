using Newtonsoft.Json;
using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services.Achievements
{
    public enum BadgeTier
    {
        None = 0,

        Bronze = 1,

        Silver = 2,

        Gold = 3,

        Platinum = 4
    }

    /// <summary>
    /// Something an achievement or badge can be measured against. Add
    /// to this, and to AchievementService.Measure, to make a new kind of
    /// goal possible.
    /// </summary>
    public enum AchievementMetric
    {
        TotalGames,
        SteamGames,
        EpicGames,
        NexusEntries,

        //----------------------------------------------------------
        // Per launcher. These read the Launcher named on the
        // definition rather than a fixed launcher, so one metric
        // covers every launcher Nexus scans.
        //----------------------------------------------------------

        LauncherGames,
        LauncherPlayHours,
        LauncherLaunches,
        LauncherGamesPlayed,

        LaunchersWithGames,
        LaunchersPlayed,
        TotalPlayHours,
        LongestSessionHours,
        TopGameHours,
        TotalLaunches,
        GamesPlayed,
        Favorites,
        Groups,
        CustomArtworkGames,
        CustomThemes,
        DaysActive,
        LongestStreakDays,
        NightLaunch,
        EarlyBirdLaunch,
        WeekendLaunch
    }

    /// <summary>
    /// Everything an achievement check can look at.
    /// </summary>
    internal class AchievementContext
    {
        public LibraryStats Stats { get; set; }

        public int DaysActive { get; set; }

        public bool NightLaunch { get; set; }

        public bool EarlyBirdLaunch { get; set; }

        public bool WeekendLaunch { get; set; }

        /// <summary>
        /// Longest run of consecutive days Nexus has been opened.
        /// </summary>
        public int LongestStreakDays { get; set; }
    }

    /// <summary>
    /// A one off goal: done or not done.
    /// </summary>
    internal class AchievementDefinition
    {
        public string Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string IconKey { get; set; }

        public int Points { get; set; }

        /// <summary>
        /// The value Measure has to reach. Also drives the progress bar.
        /// </summary>
        public long Target { get; set; }

        /// <summary>
        /// Unit shown next to the progress, for example "hours".
        /// Empty for yes/no goals.
        /// </summary>
        public string Unit { get; set; }

        public AchievementMetric Metric { get; set; }

        /// <summary>
        /// Which launcher a per launcher metric measures. Null for
        /// every other metric.
        /// </summary>
        public string Launcher { get; set; }

        public Func<AchievementContext, long> Measure { get; set; }
    }

    /// <summary>
    /// A goal that levels up Bronze, Silver, Gold then Platinum.
    /// </summary>
    internal class BadgeDefinition
    {
        public string Id { get; set; }

        public string Title { get; set; }

        /// <summary>
        /// "{0}" is replaced with the next threshold.
        /// </summary>
        public string DescriptionFormat { get; set; }

        public string Unit { get; set; }

        public string IconKey { get; set; }

        /// <summary>
        /// Bronze, Silver, Gold and Platinum thresholds, in order.
        /// </summary>
        public long[] Thresholds { get; set; }

        public AchievementMetric Metric { get; set; }

        /// <summary>
        /// Which launcher a per launcher metric measures. Null for
        /// every other metric.
        /// </summary>
        public string Launcher { get; set; }

        public Func<AchievementContext, long> Measure { get; set; }
    }

    internal class AchievementProgress
    {
        public AchievementDefinition Definition { get; set; }

        public long Current { get; set; }

        public bool Unlocked { get; set; }

        public DateTime? UnlockedUtc { get; set; }

        public double Fraction
        {
            get
            {
                if (Unlocked || Definition.Target <= 0)
                    return 1;

                return Math.Min(1d, (double)Current / Definition.Target);
            }
        }
    }

    internal class BadgeProgress
    {
        public BadgeDefinition Definition { get; set; }

        public long Current { get; set; }

        public BadgeTier Tier { get; set; }

        /// <summary>
        /// Threshold for the next tier, or 0 once Platinum is reached.
        /// </summary>
        public long NextThreshold { get; set; }

        public bool IsMaxed
        {
            get
            {
                return Tier == BadgeTier.Platinum;
            }
        }

        /// <summary>
        /// Progress from the current tier towards the next one.
        /// </summary>
        public double FractionToNext
        {
            get
            {
                if (IsMaxed)
                    return 1;

                long floor =
                    Tier == BadgeTier.None
                        ? 0
                        : Definition.Thresholds[(int)Tier - 1];

                long span =
                    NextThreshold - floor;

                if (span <= 0)
                    return 0;

                return Math.Max(
                    0d,
                    Math.Min(1d, (double)(Current - floor) / span));
            }
        }
    }

    /// <summary>
    /// Something that has just been earned, for the unlock toast.
    /// </summary>
    internal class UnlockEvent
    {
        public string Title { get; set; }

        public string Description { get; set; }

        public string IconKey { get; set; }

        public int Points { get; set; }

        public bool IsBadge { get; set; }

        public BadgeTier Tier { get; set; }
    }

    /// <summary>
    /// What gets written to disk. Only records what has been earned;
    /// progress is always recomputed from live data.
    /// </summary>
    internal class AchievementState
    {
        public Dictionary<string, DateTime> UnlockedAchievements { get; set; }

        public Dictionary<string, int> BadgeTiers { get; set; }

        public List<string> ActiveDays { get; set; }

        public bool NightLaunch { get; set; }

        public bool EarlyBirdLaunch { get; set; }

        public bool WeekendLaunch { get; set; }

        public DateTime? FirstSeenUtc { get; set; }

        public AchievementState()
        {
            UnlockedAchievements = new Dictionary<string, DateTime>();
            BadgeTiers = new Dictionary<string, int>();
            ActiveDays = new List<string>();
        }
    }

    /// <summary>
    /// Nexus's own achievements and badges, separate from anything the
    /// launchers track. Everything is measured from data Nexus already
    /// holds, so it all unlocks retroactively the first time it runs.
    /// </summary>
    internal static class AchievementService
    {
        private static readonly string SaveFile =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData),
                "NexusLauncher",
                "Achievements.json");

        private const string Icon = "svgimages/icon%20builder/";

        /// <summary>
        /// Points earned for reaching each badge tier. Cumulative, so a
        /// Gold badge is worth Bronze plus Silver plus Gold.
        /// </summary>
        private static readonly int[] TierPoints =
            { 0, 10, 25, 50, 100 };

        public const int PointsPerLevel = 100;

        private static readonly object sync =
            new object();

        private static AchievementState state;

        private static bool initialized;

        public static event Action Changed;

        //--------------------------------------------------------------
        // Definitions
        //
        // HOW TO ADD ONE
        //
        // Each entry is one line. Pick a metric from AchievementMetric
        // (add a new one there and to Measure below if nothing fits),
        // then:
        //
        //   One off, done or not done:
        //     Achievement("id", "Title", "What to do.", "icon.svg",
        //         points, AchievementMetric.X, target, "unit")
        //
        //   Tiered, Bronze / Silver / Gold / Platinum:
        //     Badge("id", "Title", "Do {0} things.", "unit", "icon.svg",
        //         AchievementMetric.X, bronze, silver, gold, platinum)
        //
        // Icons are the DevExpress "icon builder" SVGs; the file name
        // alone is enough. The unit is optional on achievements whose
        // target is 1.
        //
        // NEVER change an existing id. Earned progress is stored against
        // it, so renaming one silently takes that achievement away from
        // everyone who has already unlocked it. Titles, descriptions,
        // icons and points are safe to change.
        //--------------------------------------------------------------

        public static readonly IReadOnlyList<AchievementDefinition> Achievements =
            BuildAchievements();

        private static List<AchievementDefinition> BuildAchievements()
        {
            List<AchievementDefinition> list =
                new List<AchievementDefinition>
            {
                // Getting started
                Achievement("first_steps", "First Steps", "Launch a game from Nexus.", "travel_walk.svg", 15, AchievementMetric.TotalLaunches, 1),
                Achievement("favourite_thing", "Favourite Thing", "Mark a game as a favourite.", "shopping_favorites.svg", 15, AchievementMetric.Favorites, 1),
                Achievement("organizer", "Organizer", "Create your first library group.", "actions_folderclose.svg", 15, AchievementMetric.Groups, 1),
                Achievement("picture_perfect", "Picture Perfect", "Give a game your own artwork.", "actions_image.svg", 15, AchievementMetric.CustomArtworkGames, 1),
                Achievement("strikingly_rendered", "Strikingly Rendered", "Give 4 games your own artwork.", "actions_image.svg", 15, AchievementMetric.CustomArtworkGames, 4),
                Achievement("consummate_art", "Consummate Art", "Give 10 games your own artwork.", "actions_image.svg", 15, AchievementMetric.CustomArtworkGames, 10),
                Achievement("your_own_way", "Your Own Way", "Add a program to Nexus Launcher.", "actions_add.svg", 15, AchievementMetric.NexusEntries, 1),
                Achievement("weekend_warrior", "Weekend Warrior", "Launch a game on a Saturday or Sunday.", "travel_beach.svg", 15, AchievementMetric.WeekendLaunch, 1),

                // Making Nexus yours
                Achievement("make_it_yours", "Make It Yours", "Build a custom theme.", "actions_settings.svg", 25, AchievementMetric.CustomThemes, 1),
                Achievement("tidy_library", "Tidy Library", "Create 3 library groups.", "actions_list.svg", 25, AchievementMetric.Groups, 3, "groups"),
                Achievement("cataloged", "Cataloged", "Create 6 or more library groups.", "actions_list.svg", 25, AchievementMetric.Groups, 6, "groups"),
                Achievement("theme_park", "Theme Park", "Build 3 custom themes.", "weather_partlycloudyday.svg", 25, AchievementMetric.CustomThemes, 3, "themes"),

                // Time of day
                Achievement("night_owl", "Night Owl", "Launch a game between midnight and 5 AM.", "weather_moon.svg", 25, AchievementMetric.NightLaunch, 1),
                Achievement("early_bird", "Early Bird", "Launch a game between 5 and 8 AM.", "weather_sunny.svg", 25, AchievementMetric.EarlyBirdLaunch, 1),

                // Playing
                Achievement("well_rounded", "Well Rounded", "Play games from 3 different launchers.", "actions_checkcircled.svg", 25, AchievementMetric.LaunchersPlayed, 3, "launchers"),
                Achievement("all_rounder", "All Rounder", "Play a game from all launchers.", "actions_checkcircled.svg", 25, AchievementMetric.LaunchersPlayed, 7, "launchers"),
                Achievement("marathon", "Marathon", "Play a single game for 5 hours in one session.", "business_target.svg", 50, AchievementMetric.LongestSessionHours, 5, "hours"),
                Achievement("devoted", "Devoted", "Spend 10 hours in a single game.", "security_fingerprint.svg", 50, AchievementMetric.TopGameHours, 10, "hours"),
                Achievement("iron_will", "Iron Will", "Play a single game for 8 hours in one session.", "security_security.svg", 75, AchievementMetric.LongestSessionHours, 8, "hours"),
                // Library milestones
Achievement("library_starter", "Library Starter", "Add 5 games to your Nexus library.", "shopping_box.svg", 15, AchievementMetric.TotalGames, 5, "games"),
Achievement("library_growing", "Growing Library", "Add 25 games to your Nexus library.", "shopping_box.svg", 25, AchievementMetric.TotalGames, 25, "games"),
Achievement("library_builder", "Library Builder", "Add 50 games to your Nexus library.", "actions_book.svg", 35, AchievementMetric.TotalGames, 50, "games"),
Achievement("library_keeper", "Library Keeper", "Build a library of 100 games.", "actions_book.svg", 50, AchievementMetric.TotalGames, 100, "games"),
Achievement("library_legend", "Library Legend", "Build a library of 250 games.", "actions_book.svg", 75, AchievementMetric.TotalGames, 250, "games"),

// Steam
Achievement("steam_beginner", "Steam Beginner", "Have 5 Steam games in your library.", "electronics_desktopwindows.svg", 15, AchievementMetric.SteamGames, 5, "games"),
Achievement("steam_collector", "Steam Collector", "Have 25 Steam games in your library.", "electronics_desktopwindows.svg", 25, AchievementMetric.SteamGames, 25, "games"),
Achievement("steam_library", "Steam Library", "Have 100 Steam games in your library.", "electronics_desktopwindows.svg", 50, AchievementMetric.SteamGames, 100, "games"),

// Launcher collection
Achievement("launcher_collector", "Launcher Collector", "Have games from 3 different launchers.", "business_world.svg", 25, AchievementMetric.LaunchersWithGames, 3, "launchers"),
Achievement("launcher_network", "Launcher Network", "Have games from 5 different launchers.", "business_world.svg", 40, AchievementMetric.LaunchersWithGames, 5, "launchers"),
Achievement("launcher_universe", "Launcher Universe", "Have games from all 7 supported launchers.", "business_world.svg", 75, AchievementMetric.LaunchersWithGames, 7, "launchers"),

// Nexus programs
Achievement("program_collector", "Program Collector", "Add 5 programs to Nexus Launcher.", "actions_add.svg", 20, AchievementMetric.NexusEntries, 5, "programs"),
Achievement("program_library", "Program Library", "Add 25 programs to Nexus Launcher.", "business_briefcase.svg", 35, AchievementMetric.NexusEntries, 25, "programs"),
Achievement("program_archive", "Program Archive", "Add 50 programs to Nexus Launcher.", "business_briefcase.svg", 50, AchievementMetric.NexusEntries, 50, "programs"),

// Playing
Achievement("first_ten", "Getting Into It", "Launch games 10 times.", "actions_arrow4right.svg", 15, AchievementMetric.TotalLaunches, 10, "launches"),
Achievement("frequent_player", "Frequent Player", "Launch games 50 times.", "actions_arrow4right.svg", 25, AchievementMetric.TotalLaunches, 50, "launches"),
Achievement("launch_veteran", "Launch Veteran", "Launch games 250 times.", "travel_plane.svg", 40, AchievementMetric.TotalLaunches, 250, "launches"),
Achievement("launch_legend", "Launch Legend", "Launch games 1,000 times.", "travel_plane.svg", 75, AchievementMetric.TotalLaunches, 1000, "launches"),

// Game variety
Achievement("variety_player", "Variety Player", "Play 10 different games.", "travel_map.svg", 20, AchievementMetric.GamesPlayed, 10, "games"),
Achievement("game_explorer", "Game Explorer", "Play 25 different games.", "travel_map.svg", 30, AchievementMetric.GamesPlayed, 25, "games"),
Achievement("game_connoisseur", "Game Connoisseur", "Play 50 different games.", "travel_map.svg", 50, AchievementMetric.GamesPlayed, 50, "games"),
Achievement("game_tourist", "Game Tourist", "Play 100 different games.", "travel_map.svg", 75, AchievementMetric.GamesPlayed, 100, "games"),

// Playtime
Achievement("five_hour_club", "Five Hour Club", "Spend 5 hours playing games.", "actions_clock.svg", 15, AchievementMetric.TotalPlayHours, 5, "hours"),
Achievement("twenty_five_hours", "Time Well Spent", "Spend 25 hours playing games.", "actions_clock.svg", 25, AchievementMetric.TotalPlayHours, 25, "hours"),
Achievement("hundred_hours", "Century Club", "Spend 100 hours playing games.", "actions_clock.svg", 40, AchievementMetric.TotalPlayHours, 100, "hours"),
Achievement("five_hundred_hours", "Dedicated Gamer", "Spend 500 hours playing games.", "actions_clock.svg", 60, AchievementMetric.TotalPlayHours, 500, "hours"),
Achievement("thousand_hours", "Gaming Institution", "Spend 1,000 hours playing games.", "business_target.svg", 100, AchievementMetric.TotalPlayHours, 1000, "hours"),

// Most-played game
Achievement("favorite_game", "Favorite Game", "Spend 25 hours in your most-played game.", "actions_rating.svg", 25, AchievementMetric.TopGameHours, 25, "hours"),
Achievement("main_game", "Main Game", "Spend 50 hours in your most-played game.", "actions_rating.svg", 35, AchievementMetric.TopGameHours, 50, "hours"),
Achievement("dedicated_main", "Dedicated Main", "Spend 100 hours in your most-played game.", "security_fingerprint.svg", 50, AchievementMetric.TopGameHours, 100, "hours"),
Achievement("ultimate_main", "Ultimate Main", "Spend 250 hours in your most-played game.", "security_security.svg", 75, AchievementMetric.TopGameHours, 250, "hours"),

// Favorites
Achievement("favorite_five", "Fan Favorite", "Favourite 5 games.", "shopping_favorites.svg", 20, AchievementMetric.Favorites, 5, "favourites"),
Achievement("favorite_ten", "Favorite Collector", "Favourite 10 games.", "shopping_favorites.svg", 25, AchievementMetric.Favorites, 10, "favourites"),
Achievement("favorite_twenty_five", "Favorite Curator", "Favourite 25 games.", "actions_bookmark.svg", 40, AchievementMetric.Favorites, 25, "favourites"),
Achievement("favorite_fifty", "Favorite Hoarder", "Favourite 50 games.", "actions_bookmark.svg", 60, AchievementMetric.Favorites, 50, "favourites"),

// Artwork
Achievement("art_collector", "Art Collector", "Give 5 games your own artwork.", "actions_image.svg", 20, AchievementMetric.CustomArtworkGames, 5, "games"),
Achievement("art_curator", "Art Curator", "Give 25 games your own artwork.", "actions_image.svg", 35, AchievementMetric.CustomArtworkGames, 25, "games"),
Achievement("art_gallery", "Art Gallery", "Give 50 games your own artwork.", "electronics_photo.svg", 50, AchievementMetric.CustomArtworkGames, 50, "games"),

// Groups
Achievement("library_organizer", "Library Organizer", "Create 10 library groups.", "actions_folderclose.svg", 25, AchievementMetric.Groups, 10, "groups"),
Achievement("library_architect", "Library Architect", "Create 15 library groups.", "actions_list.svg", 40, AchievementMetric.Groups, 15, "groups"),
Achievement("library_master", "Library Master", "Create 25 library groups.", "actions_list.svg", 60, AchievementMetric.Groups, 25, "groups"),

// Themes
Achievement("theme_creator", "Theme Creator", "Build 5 custom themes.", "actions_settings.svg", 25, AchievementMetric.CustomThemes, 5, "themes"),
Achievement("theme_designer", "Theme Designer", "Build 10 custom themes.", "weather_partlycloudyday.svg", 40, AchievementMetric.CustomThemes, 10, "themes"),
Achievement("theme_master", "Theme Master", "Build 25 custom themes.", "actions_settings.svg", 60, AchievementMetric.CustomThemes, 25, "themes"),

// Nexus activity
Achievement("returning_player", "Returning Player", "Use Nexus on 14 different days.", "actions_calendar.svg", 20, AchievementMetric.DaysActive, 14, "days"),
Achievement("nexus_regular", "Nexus Regular", "Use Nexus on 30 different days.", "actions_calendar.svg", 30, AchievementMetric.DaysActive, 30, "days"),
Achievement("nexus_veteran", "Nexus Veteran", "Use Nexus on 100 different days.", "actions_calendar.svg", 50, AchievementMetric.DaysActive, 100, "days"),
Achievement("nexus_year", "A Year With Nexus", "Use Nexus on 365 different days.", "actions_calendar.svg", 100, AchievementMetric.DaysActive, 365, "days"),

// Streaks
Achievement("three_day_streak", "Three Day Streak", "Use Nexus for 3 days in a row.", "weather_lightning.svg", 20, AchievementMetric.LongestStreakDays, 3, "days"),
Achievement("one_week_streak", "Week Strong", "Use Nexus for 7 days in a row.", "weather_lightning.svg", 30, AchievementMetric.LongestStreakDays, 7, "days"),
Achievement("two_week_streak", "Two Week Streak", "Use Nexus for 14 days in a row.", "weather_storm.svg", 40, AchievementMetric.LongestStreakDays, 14, "days"),
Achievement("month_streak", "Monthly Streak", "Use Nexus for 30 days in a row.", "weather_storm.svg", 60, AchievementMetric.LongestStreakDays, 30, "days"),
Achievement("streak_legend", "Streak Legend", "Use Nexus for 60 days in a row.", "weather_storm.svg", 100, AchievementMetric.LongestStreakDays, 60, "days")
            };

            // One matching set per launcher, generated rather than
            // written out seven times over.
            foreach (string launcher in
                LibraryStatsService.GameLaunchers)
            {
                list.AddRange(
                    LauncherAchievements(launcher));
            }

            return list;
        }

        public static readonly IReadOnlyList<BadgeDefinition> Badges =
            BuildBadges();

        private static List<BadgeDefinition> BuildBadges()
        {
            List<BadgeDefinition> list =
                new List<BadgeDefinition>
            {
                // Library
                Badge("collector", "Collector", "Have {0} games in your library.", "games", "shopping_box.svg", AchievementMetric.TotalGames, 10, 20, 50, 100),
                Badge("steam_veteran", "Steam Veteran", "Have {0} Steam games.", "games", "electronics_desktopwindows.svg", AchievementMetric.SteamGames, 10, 50, 100, 250),
                Badge("launcher_hopper", "Launcher Hopper", "Have games in {0} launchers.", "launchers", "business_world.svg", AchievementMetric.LaunchersWithGames, 2, 3, 5, 7),
                Badge("nexus_builder", "Nexus Builder", "Add {0} programs to Nexus.", "programs", "business_briefcase.svg", AchievementMetric.NexusEntries, 3, 10, 25, 50),

                // Playing
                Badge("dedicated", "Dedicated", "Play for {0} hours in total.", "hours", "actions_clock.svg", AchievementMetric.TotalPlayHours, 5, 25, 100, 500),
                Badge("frequent_flyer", "Frequent Flyer", "Launch games {0} times.", "launches", "travel_plane.svg", AchievementMetric.TotalLaunches, 10, 50, 250, 1000),
                Badge("explorer", "Explorer", "Play {0} different games.", "games", "travel_map.svg", AchievementMetric.GamesPlayed, 5, 15, 40, 100),

                // Using Nexus
                Badge("regular", "Regular", "Use Nexus on {0} different days.", "days", "actions_calendar.svg", AchievementMetric.DaysActive, 3, 7, 30, 100),
                Badge("on_a_roll", "On a Roll", "Use Nexus {0} days in a row.", "days", "weather_lightning.svg", AchievementMetric.LongestStreakDays, 3, 7, 14, 30),
                Badge("curator", "Curator", "Favourite {0} games.", "favourites", "actions_bookmark.svg", AchievementMetric.Favorites, 3, 10, 25, 50),
                Badge("artist", "Artist", "Give {0} games custom artwork.", "games", "electronics_photo.svg", AchievementMetric.CustomArtworkGames, 3, 10, 25, 50),
                // Library
                Badge("library_grower", "Library Grower", "Have {0} games in your library.", "games", "actions_add.svg", AchievementMetric.TotalGames, 25, 50, 100, 250),
                Badge("steam_collector", "Steam Collector", "Have {0} Steam games.", "games", "electronics_desktopwindows.svg", AchievementMetric.SteamGames, 25, 75, 150, 300),
                Badge("platform_explorer", "Platform Explorer", "Play games from {0} different launchers.", "launchers", "travel_map.svg", AchievementMetric.LaunchersPlayed, 2, 4, 6, 7),
                Badge("program_archive", "Program Archive", "Add {0} programs to Nexus.", "programs", "business_briefcase.svg", AchievementMetric.NexusEntries, 10, 25, 50, 100),

                // Playing
                Badge("seasoned_player", "Seasoned Player", "Play for {0} hours in total.", "hours", "actions_clock.svg", AchievementMetric.TotalPlayHours, 25, 100, 250, 750),
                Badge("veteran_player", "Veteran Player", "Launch games {0} times.", "launches", "travel_plane.svg", AchievementMetric.TotalLaunches, 50, 250, 1000, 2500),
                Badge("marathoner", "Marathoner", "Play a single game for {0} hours in one session.", "hours", "business_target.svg", AchievementMetric.LongestSessionHours, 2, 5, 10, 24),
                Badge("top_game", "Top Game", "Spend {0} hours in your most-played game.", "hours", "actions_rating.svg", AchievementMetric.TopGameHours, 10, 50, 100, 250),

// Using Nexus
Badge("nexus_regular", "Nexus Regular", "Use Nexus on {0} different days.", "days", "actions_calendar.svg", AchievementMetric.DaysActive, 14, 30, 90, 180),
Badge("streak_keeper", "Streak Keeper", "Use Nexus {0} days in a row.", "days", "weather_lightning.svg", AchievementMetric.LongestStreakDays, 5, 10, 20, 50),
Badge("super_curator", "Super Curator", "Favourite {0} games.", "favourites", "actions_bookmark.svg", AchievementMetric.Favorites, 5, 15, 30, 75),
Badge("art_director", "Art Director", "Give {0} games custom artwork.", "games", "electronics_photo.svg", AchievementMetric.CustomArtworkGames, 5, 15, 30, 75),
Badge("theme_creator", "Theme Creator", "Create {0} custom themes.", "themes", "actions_settings.svg", AchievementMetric.CustomThemes, 5, 15, 30, 75),
Badge("group_master", "Group Master", "Create {0} library groups.", "groups", "actions_folderclose.svg", AchievementMetric.Groups, 10, 25, 50, 100),

// Launcher Milestones
Badge("launcher_master", "Launcher Master", "Play games from {0} different launchers.", "launchers", "business_world.svg", AchievementMetric.LaunchersPlayed, 3, 4, 6, 7),
Badge("launcher_collector", "Launcher Collector", "Have games in {0} different launchers.", "launchers", "business_world.svg", AchievementMetric.LaunchersWithGames, 3, 4, 6, 7),

// Time-Based
Badge("night_player", "Night Player", "Launch games during the late night {0} times.", "launches", "weather_moon.svg", AchievementMetric.NightLaunch, 1, 1, 1, 1),
Badge("early_bird_badge", "Early Bird", "Launch games during the early morning.", "launches", "weather_sunny.svg", AchievementMetric.EarlyBirdLaunch, 1, 1, 1, 1),
Badge("weekend_gamer", "Weekend Gamer", "Launch games on the weekend.", "launches", "travel_beach.svg", AchievementMetric.WeekendLaunch, 1, 1, 1, 1)


            };

            foreach (string launcher in
                LibraryStatsService.GameLaunchers)
            {
                list.AddRange(
                    LauncherBadges(launcher));
            }

            return list;
        }

        //--------------------------------------------------------------
        // Per launcher definitions
        //
        // Every launcher Nexus scans gets the same ladder, so a user who
        // lives in Epic or GOG has the same to aim for as a Steam user.
        // Ids are built from a fixed slug, never from the display name:
        // renaming a launcher in the UI must not orphan earned progress.
        //--------------------------------------------------------------

        /// <summary>
        /// Short, permanent id fragment for a launcher. Anything not
        /// listed falls back to a sanitised name so a launcher added
        /// later still gets a usable id.
        /// </summary>
        private static string Slug(
            string launcher)
        {
            switch (launcher)
            {
                case LibraryStatsService.SteamLauncher: return "steam";
                case LibraryStatsService.EpicLauncher: return "epic";
                case LibraryStatsService.BattleNetLauncher: return "bnet";
                case LibraryStatsService.GogLauncher: return "gog";
                case LibraryStatsService.EaLauncher: return "ea";
                case LibraryStatsService.UbisoftLauncher: return "ubisoft";
                case LibraryStatsService.XboxLauncher: return "xbox";

                default:
                    return new string(
                        (launcher ?? "other")
                            .ToLowerInvariant()
                            .Where(char.IsLetterOrDigit)
                            .ToArray());
            }
        }

        /// <summary>
        /// The glyph shown on a launcher's achievements. The icon
        /// builder set has no store logos, so these are chosen to be
        /// distinguishable rather than literal.
        /// </summary>
        private static string LauncherIcon(
            string launcher)
        {
            switch (launcher)
            {
                case LibraryStatsService.SteamLauncher:
                    return "electronics_desktopwindows.svg";
                case LibraryStatsService.EpicLauncher:
                    return "actions_flag.svg";
                case LibraryStatsService.BattleNetLauncher:
                    return "security_security.svg";
                case LibraryStatsService.GogLauncher:
                    return "travel_mountains.svg";
                case LibraryStatsService.EaLauncher:
                    return "electronics_video.svg";
                case LibraryStatsService.UbisoftLauncher:
                    return "travel_mappointer.svg";
                case LibraryStatsService.XboxLauncher:
                    return "electronics_tv.svg";
                default:
                    return "business_world.svg";
            }
        }

        private static IEnumerable<AchievementDefinition> LauncherAchievements(
            string launcher)
        {
            string slug = Slug(launcher);

            string icon = LauncherIcon(launcher);

            List<AchievementDefinition> list =
                new List<AchievementDefinition>();

            // Collecting. Steam already has its own 5 / 25 / 100 ladder
            // from before these were generated, so it is skipped here
            // rather than given two overlapping sets.
            if (launcher != LibraryStatsService.SteamLauncher)
            {
                list.Add(LauncherAchievement(
                    "lib_" + slug + "_5", launcher + " Beginner",
                    "Have 5 games from " + launcher + " in your library.",
                    icon, 15, AchievementMetric.LauncherGames, launcher, 5, "games"));

                list.Add(LauncherAchievement(
                    "lib_" + slug + "_25", launcher + " Collector",
                    "Have 25 games from " + launcher + " in your library.",
                    icon, 25, AchievementMetric.LauncherGames, launcher, 25, "games"));

                list.Add(LauncherAchievement(
                    "lib_" + slug + "_100", launcher + " Library",
                    "Have 100 games from " + launcher + " in your library.",
                    icon, 50, AchievementMetric.LauncherGames, launcher, 100, "games"));
            }

            // Playing. Steam gets these too: it had no per launcher
            // play goals before.
            list.Add(LauncherAchievement(
                "play_" + slug + "_first", "First Run on " + launcher,
                "Launch a game from " + launcher + " through Nexus.",
                "actions_arrow4right.svg", 15, AchievementMetric.LauncherLaunches, launcher, 1));

            list.Add(LauncherAchievement(
                "play_" + slug + "_10h", launcher + " Regular",
                "Spend 10 hours in games from " + launcher + ".",
                "actions_clock.svg", 25, AchievementMetric.LauncherPlayHours, launcher, 10, "hours"));

            list.Add(LauncherAchievement(
                "play_" + slug + "_50h", launcher + " Devotee",
                "Spend 50 hours in games from " + launcher + ".",
                "actions_clock.svg", 40, AchievementMetric.LauncherPlayHours, launcher, 50, "hours"));

            list.Add(LauncherAchievement(
                "play_" + slug + "_variety", launcher + " Explorer",
                "Play 10 different games from " + launcher + ".",
                "travel_map.svg", 30, AchievementMetric.LauncherGamesPlayed, launcher, 10, "games"));

            return list;
        }

        private static IEnumerable<BadgeDefinition> LauncherBadges(
            string launcher)
        {
            string slug = Slug(launcher);

            return new List<BadgeDefinition>
            {
                LauncherBadge(
                    "badge_lib_" + slug, launcher + " Collection",
                    "Have {0} games from " + launcher + ".", "games",
                    LauncherIcon(launcher),
                    AchievementMetric.LauncherGames, launcher,
                    10, 50, 100, 250),

                LauncherBadge(
                    "badge_play_" + slug, launcher + " Hours",
                    "Play {0} hours of games from " + launcher + ".", "hours",
                    "actions_clock.svg",
                    AchievementMetric.LauncherPlayHours, launcher,
                    5, 25, 100, 250)
            };
        }

        private static AchievementDefinition LauncherAchievement(
            string id,
            string title,
            string description,
            string icon,
            int points,
            AchievementMetric metric,
            string launcher,
            long target,
            string unit = null)
        {
            return new AchievementDefinition
            {
                Id = id,
                Title = title,
                Description = description,
                IconKey = Icon + icon,
                Points = points,
                Target = target,
                Unit = unit,
                Metric = metric,
                Launcher = launcher,
                Measure = c => Measure(metric, c, launcher)
            };
        }

        private static BadgeDefinition LauncherBadge(
            string id,
            string title,
            string descriptionFormat,
            string unit,
            string icon,
            AchievementMetric metric,
            string launcher,
            long bronze,
            long silver,
            long gold,
            long platinum)
        {
            return new BadgeDefinition
            {
                Id = id,
                Title = title,
                DescriptionFormat = descriptionFormat,
                Unit = unit,
                IconKey = Icon + icon,
                Metric = metric,
                Launcher = launcher,
                Thresholds = new[] { bronze, silver, gold, platinum },
                Measure = c => Measure(metric, c, launcher)
            };
        }

        private static AchievementDefinition Achievement(
            string id,
            string title,
            string description,
            string icon,
            int points,
            AchievementMetric metric,
            long target,
            string unit = null)
        {
            return new AchievementDefinition
            {
                Id = id,
                Title = title,
                Description = description,
                IconKey = Icon + icon,
                Points = points,
                Target = target,
                Unit = unit,
                Metric = metric,
                Measure = c => Measure(metric, c)
            };
        }

        private static BadgeDefinition Badge(
            string id,
            string title,
            string descriptionFormat,
            string unit,
            string icon,
            AchievementMetric metric,
            long bronze,
            long silver,
            long gold,
            long platinum)
        {
            return new BadgeDefinition
            {
                Id = id,
                Title = title,
                DescriptionFormat = descriptionFormat,
                Unit = unit,
                IconKey = Icon + icon,
                Metric = metric,
                Thresholds = new[] { bronze, silver, gold, platinum },
                Measure = c => Measure(metric, c)
            };
        }

        /// <summary>
        /// The value of a metric right now. Add a case here when adding a
        /// new AchievementMetric.
        /// </summary>
        public static long Measure(
            AchievementMetric metric,
            AchievementContext c)
        {
            return Measure(metric, c, null);
        }

        /// <summary>
        /// <paramref name="launcher"/> is only read by the per launcher
        /// metrics, and is the accordion display name, for example
        /// "Epic Games".
        /// </summary>
        public static long Measure(
            AchievementMetric metric,
            AchievementContext c,
            string launcher)
        {
            switch (metric)
            {
                case AchievementMetric.LauncherGames:
                    return c.Stats.GamesIn(launcher);

                case AchievementMetric.LauncherPlayHours:
                    return c.Stats.PlaySecondsIn(launcher) / 3600;

                case AchievementMetric.LauncherLaunches:
                    return c.Stats.LaunchesIn(launcher);

                case AchievementMetric.LauncherGamesPlayed:
                    return c.Stats.GamesPlayedIn(launcher);

                case AchievementMetric.TotalGames: return c.Stats.TotalGames;
                case AchievementMetric.SteamGames: return c.Stats.SteamGames;
                case AchievementMetric.EpicGames: return c.Stats.GamesIn(LibraryStatsService.EpicLauncher);
                case AchievementMetric.NexusEntries: return c.Stats.NexusEntries;
                case AchievementMetric.LaunchersWithGames: return c.Stats.LaunchersWithGames;
                case AchievementMetric.LaunchersPlayed: return c.Stats.LaunchersPlayed;
                case AchievementMetric.TotalPlayHours: return c.Stats.TotalPlaySeconds / 3600;
                case AchievementMetric.LongestSessionHours: return c.Stats.LongestSessionSeconds / 3600;
                case AchievementMetric.TopGameHours: return c.Stats.TopGameSeconds / 3600;
                case AchievementMetric.TotalLaunches: return c.Stats.TotalLaunches;
                case AchievementMetric.GamesPlayed: return c.Stats.GamesPlayed;
                case AchievementMetric.Favorites: return c.Stats.Favorites;
                case AchievementMetric.Groups: return c.Stats.Groups;
                case AchievementMetric.CustomArtworkGames: return c.Stats.CustomArtworkGames;
                case AchievementMetric.CustomThemes: return c.Stats.CustomThemes;
                case AchievementMetric.DaysActive: return c.DaysActive;
                case AchievementMetric.LongestStreakDays: return c.LongestStreakDays;
                case AchievementMetric.NightLaunch: return c.NightLaunch ? 1 : 0;
                case AchievementMetric.EarlyBirdLaunch: return c.EarlyBirdLaunch ? 1 : 0;
                case AchievementMetric.WeekendLaunch: return c.WeekendLaunch ? 1 : 0;
                default: return 0;
            }
        }

        //--------------------------------------------------------------
        // Storage
        //--------------------------------------------------------------

        /// <summary>
        /// How many achievements and badges have unlocked since a
        /// moment, for the badge on the profile button.
        /// </summary>
        public static int UnlockedSince(
            DateTime? since)
        {
            try
            {
                if (!since.HasValue)
                    return 0;

                lock (sync)
                {
                    return State.UnlockedAchievements
                        .Count(x => x.Value > since.Value);
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return 0;
            }
        }

        private static AchievementState State
        {
            get
            {
                lock (sync)
                {
                    if (state == null)
                        state = LoadFromDisk();

                    return state;
                }
            }
        }

        private static AchievementState LoadFromDisk()
        {
            try
            {
                if (!File.Exists(SaveFile))
                    return new AchievementState();

                AchievementState loaded =
                    JsonConvert.DeserializeObject<AchievementState>(
                        File.ReadAllText(SaveFile));

                if (loaded == null)
                    return new AchievementState();

                if (loaded.UnlockedAchievements == null)
                    loaded.UnlockedAchievements = new Dictionary<string, DateTime>();

                if (loaded.BadgeTiers == null)
                    loaded.BadgeTiers = new Dictionary<string, int>();

                if (loaded.ActiveDays == null)
                    loaded.ActiveDays = new List<string>();

                return loaded;
            }
            catch (Exception ex)
            {
                // Losing progress is bad, but refusing to start is
                // worse, so a corrupt file falls back to nothing earned.
                Program.LogCrash(ex);

                return new AchievementState();
            }
        }

        private static void Save()
        {
            try
            {
                string folder =
                    Path.GetDirectoryName(SaveFile);

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string json;

                lock (sync)
                {
                    json =
                        JsonConvert.SerializeObject(
                            State,
                            Formatting.Indented);
                }

                File.WriteAllText(
                    SaveFile,
                    json);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            Changed?.Invoke();
        }

        //--------------------------------------------------------------
        // Lifetime
        //--------------------------------------------------------------

        /// <summary>
        /// Records today as an active day and starts listening for
        /// launches. Call once at startup.
        /// </summary>
        public static void Initialize()
        {
            lock (sync)
            {
                if (initialized)
                    return;

                initialized = true;
            }

            string today =
                DateTime.Now.ToString(
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture);

            bool dirty = false;

            lock (sync)
            {
                if (!State.FirstSeenUtc.HasValue)
                {
                    State.FirstSeenUtc = DateTime.UtcNow;
                    dirty = true;
                }

                if (!State.ActiveDays.Contains(today))
                {
                    State.ActiveDays.Add(today);
                    dirty = true;
                }
            }

            if (dirty)
                Save();

            PlayTrackingService.GameLaunched +=
                PlayTracking_GameLaunched;
        }

        /// <summary>
        /// Records when a launch happened, for the achievements that care
        /// about the time of day or the day of the week. These cannot be
        /// worked out afterwards: play stats only keep the last launch.
        /// </summary>
        private static void PlayTracking_GameLaunched(
            GameInfo game,
            DateTime localTime)
        {
            bool night =
                localTime.Hour < 5;

            bool earlyBird =
                localTime.Hour >= 5 && localTime.Hour < 8;

            bool weekend =
                localTime.DayOfWeek == DayOfWeek.Saturday ||
                localTime.DayOfWeek == DayOfWeek.Sunday;

            bool changed = false;

            lock (sync)
            {
                if (night && !State.NightLaunch)
                {
                    State.NightLaunch = true;
                    changed = true;
                }

                if (earlyBird && !State.EarlyBirdLaunch)
                {
                    State.EarlyBirdLaunch = true;
                    changed = true;
                }

                if (weekend && !State.WeekendLaunch)
                {
                    State.WeekendLaunch = true;
                    changed = true;
                }
            }

            if (changed)
                Save();
        }

        /// <summary>
        /// Longest run of consecutive calendar days in the active list.
        /// </summary>
        private static int LongestStreak(
            IEnumerable<string> days)
        {
            List<DateTime> dates =
                days
                    .Select(x =>
                    {
                        DateTime parsed;

                        return DateTime.TryParseExact(
                            x,
                            "yyyy-MM-dd",
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out parsed)
                            ? (DateTime?)parsed.Date
                            : null;
                    })
                    .Where(x => x.HasValue)
                    .Select(x => x.Value)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList();

            int best = 0;
            int run = 0;
            DateTime? previous = null;

            foreach (DateTime date in dates)
            {
                run =
                    previous.HasValue && (date - previous.Value).Days == 1
                        ? run + 1
                        : 1;

                best = Math.Max(best, run);
                previous = date;
            }

            return best;
        }

        //--------------------------------------------------------------
        // Reading
        //--------------------------------------------------------------

        public static DateTime? MemberSinceUtc
        {
            get
            {
                lock (sync)
                {
                    return State.FirstSeenUtc;
                }
            }
        }

        public static int DaysActive
        {
            get
            {
                lock (sync)
                {
                    return State.ActiveDays.Count;
                }
            }
        }

        /// <summary>
        /// Points from everything earned so far.
        /// </summary>
        public static int TotalPoints
        {
            get
            {
                lock (sync)
                {
                    int points =
                        Achievements
                            .Where(x => State.UnlockedAchievements.ContainsKey(x.Id))
                            .Sum(x => x.Points);

                    foreach (KeyValuePair<string, int> tier in State.BadgeTiers)
                    {
                        points += PointsForTier(tier.Value);
                    }

                    return points;
                }
            }
        }

        public static int Level
        {
            get
            {
                return TotalPoints / PointsPerLevel + 1;
            }
        }

        /// <summary>
        /// Progress through the current level, 0 to 1.
        /// </summary>
        public static double LevelProgress
        {
            get
            {
                return (TotalPoints % PointsPerLevel) /
                    (double)PointsPerLevel;
            }
        }

        public static int UnlockedAchievementCount
        {
            get
            {
                lock (sync)
                {
                    return State.UnlockedAchievements.Count;
                }
            }
        }

        private static int PointsForTier(
            int tier)
        {
            int points = 0;

            for (int i = 1; i <= tier && i < TierPoints.Length; i++)
            {
                points += TierPoints[i];
            }

            return points;
        }

        public static string TierName(
            BadgeTier tier)
        {
            return tier == BadgeTier.None
                ? "Locked"
                : tier.ToString();
        }

        //--------------------------------------------------------------
        // Evaluation
        //--------------------------------------------------------------

        private static AchievementContext BuildContext(
            LibraryStats stats)
        {
            lock (sync)
            {
                return new AchievementContext
                {
                    Stats = stats,
                    DaysActive = State.ActiveDays.Count,
                    LongestStreakDays = LongestStreak(State.ActiveDays),
                    NightLaunch = State.NightLaunch,
                    EarlyBirdLaunch = State.EarlyBirdLaunch,
                    WeekendLaunch = State.WeekendLaunch
                };
            }
        }

        public static List<AchievementProgress> GetAchievementProgress(
            LibraryStats stats)
        {
            AchievementContext context =
                BuildContext(stats);

            List<AchievementProgress> result =
                new List<AchievementProgress>();

            lock (sync)
            {
                foreach (AchievementDefinition definition in Achievements)
                {
                    DateTime unlocked;

                    bool isUnlocked =
                        State.UnlockedAchievements.TryGetValue(
                            definition.Id,
                            out unlocked);

                    result.Add(new AchievementProgress
                    {
                        Definition = definition,
                        Current = SafeMeasure(definition.Measure, context),
                        Unlocked = isUnlocked,
                        UnlockedUtc = isUnlocked ? unlocked : (DateTime?)null
                    });
                }
            }

            return result;
        }

        public static List<BadgeProgress> GetBadgeProgress(
            LibraryStats stats)
        {
            AchievementContext context =
                BuildContext(stats);

            List<BadgeProgress> result =
                new List<BadgeProgress>();

            lock (sync)
            {
                foreach (BadgeDefinition definition in Badges)
                {
                    int stored;

                    State.BadgeTiers.TryGetValue(
                        definition.Id,
                        out stored);

                    BadgeTier tier =
                        (BadgeTier)stored;

                    result.Add(new BadgeProgress
                    {
                        Definition = definition,
                        Current = SafeMeasure(definition.Measure, context),
                        Tier = tier,
                        NextThreshold = tier == BadgeTier.Platinum
                            ? 0
                            : definition.Thresholds[(int)tier]
                    });
                }
            }

            return result;
        }

        /// <summary>
        /// Checks everything against the latest stats, records anything
        /// newly earned, and returns it for the unlock toast.
        ///
        /// Tiers only ever go up. Removing games or favourites later
        /// does not take a badge away once it has been earned.
        /// </summary>
        public static List<UnlockEvent> Evaluate(
            LibraryStats stats)
        {
            List<UnlockEvent> unlocked =
                new List<UnlockEvent>();

            if (stats == null)
                return unlocked;

            AchievementContext context =
                BuildContext(stats);

            lock (sync)
            {
                foreach (AchievementDefinition definition in Achievements)
                {
                    if (State.UnlockedAchievements.ContainsKey(definition.Id))
                        continue;

                    if (SafeMeasure(definition.Measure, context) <
                        definition.Target)
                    {
                        continue;
                    }

                    State.UnlockedAchievements[definition.Id] =
                        DateTime.UtcNow;

                    unlocked.Add(new UnlockEvent
                    {
                        Title = definition.Title,
                        Description = definition.Description,
                        IconKey = definition.IconKey,
                        Points = definition.Points
                    });
                }

                foreach (BadgeDefinition definition in Badges)
                {
                    long value =
                        SafeMeasure(definition.Measure, context);

                    int reached = 0;

                    for (int i = 0; i < definition.Thresholds.Length; i++)
                    {
                        if (value >= definition.Thresholds[i])
                            reached = i + 1;
                    }

                    int stored;

                    State.BadgeTiers.TryGetValue(
                        definition.Id,
                        out stored);

                    if (reached <= stored)
                        continue;

                    State.BadgeTiers[definition.Id] = reached;

                    // One toast for the highest new tier, not one per
                    // tier skipped past on a first run.
                    unlocked.Add(new UnlockEvent
                    {
                        Title = definition.Title + " - " +
                            TierName((BadgeTier)reached),
                        Description = string.Format(
                            definition.DescriptionFormat,
                            definition.Thresholds[reached - 1]),
                        IconKey = definition.IconKey,
                        Points = PointsForTier(reached) - PointsForTier(stored),
                        IsBadge = true,
                        Tier = (BadgeTier)reached
                    });
                }
            }

            if (unlocked.Count > 0)
                Save();

            return unlocked;
        }

        /// <summary>
        /// A broken check must not stop the others running.
        /// </summary>
        private static long SafeMeasure(
            Func<AchievementContext, long> measure,
            AchievementContext context)
        {
            try
            {
                return measure(context);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return 0;
            }
        }
    }
}
