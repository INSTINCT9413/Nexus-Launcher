using Nexus_Launcher.Models;
using Nexus_Launcher.Services.Library;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// Headline numbers for the library, games per launcher, and the
    /// most and most recently played games.
    /// </summary>
    internal class LibraryStatsPanel : VerticalStack
    {
        private const string Icon = "svgimages/icon%20builder/";

        private readonly StatTile gamesTile;
        private readonly StatTile launchersTile;
        private readonly StatTile playTimeTile;
        private readonly StatTile launchesTile;
        private readonly StatTile playedTile;
        private readonly StatTile favoritesTile;

        private readonly CardPanel launcherCard;
        private readonly CardPanel mostPlayedCard;
        private readonly CardPanel recentCard;

        public LibraryStatsPanel()
        {
            CardGrid tiles =
                new CardGrid();

            gamesTile = new StatTile("Games in library", Icon + "shopping_box.svg");
            launchersTile = new StatTile("Launchers", Icon + "business_world.svg");
            playTimeTile = new StatTile("Time played", Icon + "actions_clock.svg");
            launchesTile = new StatTile("Launches", Icon + "travel_plane.svg");
            playedTile = new StatTile("Games played", Icon + "travel_map.svg");
            favoritesTile = new StatTile("Favourites", Icon + "shopping_favorites.svg");

            tiles.Controls.Add(gamesTile);
            tiles.Controls.Add(launchersTile);
            tiles.Controls.Add(playTimeTile);
            tiles.Controls.Add(launchesTile);
            tiles.Controls.Add(playedTile);
            tiles.Controls.Add(favoritesTile);

            launcherCard = new CardPanel();
            mostPlayedCard = new CardPanel();
            recentCard = new CardPanel();

            Controls.Add(tiles);
            Controls.Add(launcherCard);
            Controls.Add(mostPlayedCard);
            Controls.Add(recentCard);
        }

        public void Bind(
            LibraryStats stats)
        {
            if (stats == null)
                return;

            SuspendLayout();

            try
            {
                gamesTile.SetValue(stats.TotalGames.ToString("N0", CultureInfo.CurrentCulture));
                launchersTile.SetValue(stats.LaunchersWithGames.ToString(CultureInfo.CurrentCulture));
                playTimeTile.SetValue(ProfileStyle.FormatHours(stats.TotalPlaySeconds));
                launchesTile.SetValue(stats.TotalLaunches.ToString("N0", CultureInfo.CurrentCulture));
                playedTile.SetValue(stats.GamesPlayed.ToString("N0", CultureInfo.CurrentCulture));
                favoritesTile.SetValue(stats.Favorites.ToString("N0", CultureInfo.CurrentCulture));

                BindLaunchers(stats);
                BindMostPlayed(stats);
                BindRecent(stats);
            }
            finally
            {
                ResumeLayout(true);
            }

            ProfileTheme.Apply(this);
        }

        /// <summary>
        /// One bar per launcher, scaled to the biggest library, so the
        /// rows read as a bar chart.
        /// </summary>
        private void BindLaunchers(
            LibraryStats stats)
        {
            List<Control> rows = new List<Control>
            {
                new SectionTitle("Games by launcher", Icon + "business_barchart.svg")
            };

            List<KeyValuePair<string, int>> launchers =
                stats.GamesPerLauncher
                    .Where(x => x.Value > 0)
                    .OrderByDescending(x => x.Value)
                    .ToList();

            if (launchers.Count == 0)
            {
                rows.Add(CardStack.Message("No games found yet."));
            }
            else
            {
                double largest =
                    launchers[0].Value;

                foreach (KeyValuePair<string, int> launcher in launchers)
                {
                    MeterRow row =
                        new MeterRow(launcher.Key);

                    row.SetValue(
                        launcher.Value / largest,
                        launcher.Value.ToString("N0", CultureInfo.CurrentCulture));

                    rows.Add(row);
                }
            }

            CardStack.Fill(launcherCard, rows);
        }

        private void BindMostPlayed(
            LibraryStats stats)
        {
            List<Control> rows = new List<Control>
            {
                new SectionTitle("Most played", Icon + "actions_rating.svg")
            };

            if (stats.MostPlayed.Count == 0)
            {
                rows.Add(CardStack.Message(
                    "Play time appears here once a session has been measured."));
            }
            else
            {
                double top =
                    stats.MostPlayed[0].TotalPlaySeconds;

                int rank = 1;

                foreach (GamePlayStats game in stats.MostPlayed)
                {
                    MeterRow row =
                        new MeterRow(rank++ + ".  " + game.Name);

                    row.SetValue(
                        top <= 0 ? 0 : game.TotalPlaySeconds / top,
                        ProfileStyle.FormatHours(game.TotalPlaySeconds));

                    rows.Add(row);
                }
            }

            CardStack.Fill(mostPlayedCard, rows);
        }

        private void BindRecent(
            LibraryStats stats)
        {
            List<Control> rows = new List<Control>
            {
                new SectionTitle("Recently played", Icon + "actions_clock.svg")
            };

            if (stats.RecentlyPlayed.Count == 0)
            {
                rows.Add(CardStack.Message("Nothing played yet."));
            }
            else
            {
                foreach (GamePlayStats game in stats.RecentlyPlayed)
                {
                    // Time as the key and the name as the value: the key
                    // column is a fixed width, so a long game name there
                    // would clip, while the value column trims with an
                    // ellipsis and shows the full name on hover.
                    rows.Add(new SpecRow(
                        game.LastPlayedUtc.HasValue
                            ? SidePanelPresenter.FormatRelative(game.LastPlayedUtc.Value)
                            : "Earlier",
                        game.Name));
                }
            }

            CardStack.Fill(recentCard, rows);
        }
    }
}
