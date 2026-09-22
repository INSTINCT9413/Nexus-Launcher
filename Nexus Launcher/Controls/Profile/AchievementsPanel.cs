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

            badgeGrid = new CardGrid();
            achievementGrid = new CardGrid();

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
            Controls.Add(new SectionTitle("Badges", Icon + "actions_rating.svg"));
            Controls.Add(badgeGrid);
            Controls.Add(new SectionTitle("Achievements", Icon + "actions_checkcircled.svg"));
            Controls.Add(achievementGrid);
        }

        public void Bind(
            LibraryStats stats)
        {
            if (stats == null)
                return;

            SuspendLayout();

            try
            {
                BindSummary(stats);

                List<BadgeProgress> badges =
                    AchievementService.GetBadgeProgress(stats);

                for (int i = 0; i < badgeCards.Count && i < badges.Count; i++)
                {
                    badgeCards[i].Bind(badges[i]);
                }

                // Earned first, then the ones closest to done, so the
                // list leads with progress rather than a wall of locks.
                List<AchievementProgress> achievements =
                    AchievementService.GetAchievementProgress(stats)
                        .OrderByDescending(x => x.Unlocked)
                        .ThenByDescending(x => x.Fraction)
                        .ToList();

                for (int i = 0; i < achievementCards.Count && i < achievements.Count; i++)
                {
                    achievementCards[i].Bind(achievements[i]);
                }
            }
            finally
            {
                ResumeLayout(true);
            }

            ProfileTheme.Apply(this);
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
