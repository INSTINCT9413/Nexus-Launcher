using DevExpress.XtraEditors;
using Nexus_Launcher.Services.Achievements;
using Nexus_Launcher.Services.Library;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// The Achievements and Badges tab: level summary, tiered badges,
    /// then every one off achievement.
    /// </summary>
    internal class AchievementsPanel : VerticalStack
    {
        private const string Icon = "svgimages/icon%20builder/";

        private readonly CardPanel summary;
        private readonly LabelControl levelLabel;
        private readonly BarMeter levelMeter;
        private readonly LabelControl detailLabel;

        private readonly CardGrid badgeGrid;
        private readonly CardGrid achievementGrid;

        private readonly AchievementFilterBar filterBar;

        private readonly SectionTitle badgeTitle;
        private readonly SectionTitle achievementTitle;

        private AchievementViewFilter filter =
            new AchievementViewFilter();

        /// <summary>
        /// Kept so a filter change can rebind without the caller
        /// having to hand the statistics over again.
        /// </summary>
        private LibraryStats lastStats;

        private readonly List<BadgeCard> badgeCards =
            new List<BadgeCard>();

        private readonly List<AchievementCard> achievementCards =
            new List<AchievementCard>();

        public AchievementsPanel()
        {
            summary = new CardPanel();
            summary.Height = 96;

            levelLabel = new LabelControl();
            levelLabel.Location = new Point(16, 12);
            levelLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            levelLabel.Appearance.Font = ProfileStyle.Font(16F, FontStyle.Bold);
            levelLabel.Appearance.Options.UseFont = true;

            levelMeter = new BarMeter();
            levelMeter.Location = new Point(16, 50);
            levelMeter.Size = new Size(420, 9);
            levelMeter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

            detailLabel = new LabelControl();
            detailLabel.Location = new Point(16, 66);
            detailLabel.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            detailLabel.Appearance.Font = ProfileStyle.Font(9F);
            detailLabel.Appearance.Options.UseFont = true;

            summary.Controls.Add(levelLabel);
            summary.Controls.Add(levelMeter);
            summary.Controls.Add(detailLabel);

            filterBar = new AchievementFilterBar();
            filterBar.ReloadLaunchers();
            filterBar.FilterChanged += Filter_Changed;

            badgeGrid = new CardGrid();
            achievementGrid = new CardGrid();

            badgeTitle = new SectionTitle("Badges", Icon + "actions_rating.svg");
            achievementTitle = new SectionTitle("Achievements", Icon + "actions_checkcircled.svg");

            // The definitions are fixed, so the cards are built once and
            // just rebound on every refresh.
            foreach (BadgeDefinition definition in AchievementService.Badges)
            {
                BadgeCard card = new BadgeCard();
                badgeCards.Add(card);
                badgeGrid.Controls.Add(card);
            }

            foreach (AchievementDefinition definition in AchievementService.Achievements)
            {
                AchievementCard card = new AchievementCard();
                achievementCards.Add(card);
                achievementGrid.Controls.Add(card);
            }

            Controls.Add(summary);
            Controls.Add(filterBar);
            Controls.Add(badgeTitle);
            Controls.Add(badgeGrid);
            Controls.Add(achievementTitle);
            Controls.Add(achievementGrid);
        }

        private void Filter_Changed(
            AchievementViewFilter value)
        {
            filter = value ?? new AchievementViewFilter();

            // Rebinding from the snapshot already in hand: a filter is
            // a view of the same progress, not a reason to rebuild it.
            Bind(lastStats);
        }

        public void Bind(
            LibraryStats stats)
        {
            if (stats == null)
                return;

            lastStats = stats;

            SuspendLayout();

            try
            {
                BindSummary(stats);

                // The filter decides what is shown and in what order;
                // the cards themselves are a fixed pool, so the i-th
                // surviving item is bound to the i-th card and the
                // leftovers are hidden. No card is ever created or
                // destroyed by filtering.
                AchievementFilterResult view =
                    AchievementFilterService.Apply(
                        AchievementService.GetAchievementProgress(stats),
                        AchievementService.GetBadgeProgress(stats),
                        filter,
                        stats);

                BindCards(view);

                filterBar.SetSummary(
                    view.Achievements.Count,
                    view.TotalAchievements,
                    view.Badges.Count,
                    view.TotalBadges,
                    view.StashedForEmptyLaunchers,
                    view.EmptyLaunchers);

                // An empty section header over an empty grid reads as a
                // rendering fault, so both go away together.
                badgeTitle.Visible = view.Badges.Count > 0;
                badgeGrid.Visible = view.Badges.Count > 0;

                achievementTitle.Visible = view.Achievements.Count > 0;
                achievementGrid.Visible = view.Achievements.Count > 0;
            }
            finally
            {
                ResumeLayout(true);
            }

            // Hiding a card changes its grid's preferred height, but
            // that does not mark the stack around it dirty, so the
            // stack would keep positioning sections using the heights
            // from before the filter ran. The grids are re-measured
            // first, then the stack is laid out over the new sizes.
            badgeGrid.PerformLayout();

            achievementGrid.PerformLayout();

            PerformLayout();

            ProfileTheme.Apply(this);
        }

        /// <summary>
        /// Points the card pools at the filtered lists and hides the
        /// rest.
        /// </summary>
        private void BindCards(
            AchievementFilterResult view)
        {
            for (int i = 0; i < badgeCards.Count; i++)
            {
                bool used = i < view.Badges.Count;

                if (used)
                    badgeCards[i].Bind(view.Badges[i]);

                badgeCards[i].Visible = used;
            }

            for (int i = 0; i < achievementCards.Count; i++)
            {
                bool used = i < view.Achievements.Count;

                if (used)
                    achievementCards[i].Bind(view.Achievements[i]);

                achievementCards[i].Visible = used;
            }
        }

        private void BindSummary(
            LibraryStats stats)
        {
            int points =
                AchievementService.TotalPoints;

            int level =
                AchievementService.Level;

            int badgesEarned =
                AchievementService.GetBadgeProgress(stats)
                    .Count(x => x.Tier != BadgeTier.None);

            levelLabel.Text = "Nexus Level " + level;

            levelMeter.Width =
                System.Math.Max(120, summary.Width - 32);

            levelMeter.Value =
                AchievementService.LevelProgress;

            detailLabel.Text =
                points + " XP    " +
                (points % AchievementService.PointsPerLevel) + " / " +
                AchievementService.PointsPerLevel + " to level " + (level + 1) +
                "    " + AchievementService.UnlockedAchievementCount + " of " +
                AchievementService.Achievements.Count + " achievements    " +
                badgesEarned + " of " + AchievementService.Badges.Count + " badges";

            levelLabel.Appearance.ForeColor = ProfileStyle.AccentColor;
            levelLabel.Appearance.Options.UseForeColor = true;

            detailLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            detailLabel.Appearance.Options.UseForeColor = true;
        }
    }
}
