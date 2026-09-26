using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using Nexus_Launcher.Services.Achievements;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls.Profile
{
    /// <summary>
    /// The top of the profile: avatar, name, Nexus level and the user's
    /// best badges, with a few headline facts on the right.
    /// </summary>
    internal class ProfileHeaderPanel : Panel, IProfileThemed
    {
        private const int ShowcaseCount = 3;

        private readonly PictureEdit avatar;
        private readonly LabelControl nameLabel;
        private readonly LabelControl levelLabel;
        private readonly BarMeter levelMeter;
        private readonly LabelControl xpLabel;
        private readonly LabelControl showcaseLabel;
        private readonly List<BadgeMedal> showcase = new List<BadgeMedal>();
        private readonly Panel facts;

        private readonly SimpleButton accountButton = new SimpleButton();

        /// <summary>
        /// The Nexus Account button was pressed.
        /// </summary>
        public event Action AccountRequested;
        private readonly ToolTip tips = new ToolTip();

        public ProfileHeaderPanel()
        {
            BackColor = Color.Transparent;
            Height = 172;
            Padding = new Padding(16, 12, 16, 12);

            avatar = new PictureEdit();
            avatar.Location = new Point(16, 14);
            avatar.Size = new Size(128, 128);
            avatar.Properties.SizeMode = PictureSizeMode.Zoom;
            avatar.Properties.ShowMenu = false;
            avatar.Properties.ReadOnly = true;
            avatar.Properties.BorderStyle = BorderStyles.NoBorder;
            avatar.Properties.OptionsMask.MaskType = PictureEditMaskType.Circle;
            avatar.BackColor = Color.Transparent;
            avatar.Properties.Appearance.BackColor = Color.Transparent;
            avatar.Properties.Appearance.Options.UseBackColor = true;

            nameLabel = Label(160, 14, 22F, FontStyle.Bold);
            levelLabel = Label(162, 58, 11F, FontStyle.Bold);

            levelMeter = new BarMeter();
            levelMeter.Location = new Point(162, 84);
            levelMeter.Size = new Size(300, 8);

            xpLabel = Label(162, 96, 8.5F, FontStyle.Regular);

            showcaseLabel = Label(162, 122, 8.5F, FontStyle.Regular);
            showcaseLabel.Text = "Top badges";

            accountButton.Click += (s, e) =>
            {
                Action handler = AccountRequested;

                if (handler != null)
                    handler();
            };

            for (int i = 0; i < ShowcaseCount; i++)
            {
                BadgeMedal medal =
                    new BadgeMedal();

                medal.Size = new Size(34, 34);
                medal.Location = new Point(236 + i * 40, 114);
                medal.Visible = false;

                showcase.Add(medal);
                Controls.Add(medal);
            }

            facts = new Panel();
            facts.Dock = DockStyle.Right;
            facts.Width = 270;
            facts.BackColor = Color.Transparent;
            facts.Padding = new Padding(0, 8, 0, 6);

            // Sits at the foot of the facts column rather than floating
            // over it. Added first and sent to the back so docking gives
            // it the bottom edge before the rows fill from the top.
            accountButton.Dock = DockStyle.Bottom;
            accountButton.Height = 28;

            facts.Controls.Add(accountButton);

            accountButton.SendToBack();

            Controls.Add(avatar);
            Controls.Add(nameLabel);
            Controls.Add(levelLabel);
            Controls.Add(levelMeter);
            Controls.Add(xpLabel);
            Controls.Add(showcaseLabel);
            Controls.Add(facts);

            ApplyTheme();
        }

        private static LabelControl Label(
            int x,
            int y,
            float size,
            FontStyle style)
        {
            LabelControl label =
                new LabelControl();

            label.Location = new Point(x, y);
            label.AutoSizeMode = LabelAutoSizeMode.Horizontal;
            label.Appearance.Font = ProfileStyle.Font(size, style);
            label.Appearance.Options.UseFont = true;

            return label;
        }

        /// <summary>
        /// The user's name and account picture.
        /// </summary>
        public void SetUser(
            string name,
            Image picture)
        {
            nameLabel.Text =
                string.IsNullOrWhiteSpace(name)
                    ? Environment.UserName
                    : name;

            if (picture != null)
                avatar.Image = picture;
        }

        public void Bind(
            LibraryStats stats)
        {
            int points =
                AchievementService.TotalPoints;

            int level =
                AchievementService.Level;

            levelLabel.Text = "Nexus Level " + level;

            levelMeter.Value =
                AchievementService.LevelProgress;

            xpLabel.Text =
                (points % AchievementService.PointsPerLevel) + " / " +
                AchievementService.PointsPerLevel + " XP to level " +
                (level + 1) + "   (" + points + " XP total)";

            BindShowcase(stats);

            BindFacts(stats);

            ApplyTheme();
        }

        /// <summary>
        /// The highest tier badges earned, best first.
        /// </summary>
        private void BindShowcase(
            LibraryStats stats)
        {
            List<BadgeProgress> earned =
                AchievementService.GetBadgeProgress(stats)
                    .Where(x => x.Tier != BadgeTier.None)
                    .OrderByDescending(x => x.Tier)
                    .ThenByDescending(x => x.Current)
                    .Take(ShowcaseCount)
                    .ToList();

            showcaseLabel.Text =
                earned.Count == 0
                    ? "No badges yet. Keep playing!"
                    : "Top badges";

            for (int i = 0; i < showcase.Count; i++)
            {
                BadgeMedal medal =
                    showcase[i];

                if (i >= earned.Count)
                {
                    medal.Visible = false;
                    continue;
                }

                BadgeProgress badge =
                    earned[i];

                medal.IconKey = badge.Definition.IconKey;
                medal.DiscColor = ProfileStyle.TierColor(badge.Tier);
                medal.Locked = false;
                medal.Visible = true;

                tips.SetToolTip(
                    medal,
                    badge.Definition.Title + " - " +
                    AchievementService.TierName(badge.Tier));
            }
        }

        private void BindFacts(
            LibraryStats stats)
        {
            int badgesEarned =
                AchievementService.GetBadgeProgress(stats)
                    .Count(x => x.Tier != BadgeTier.None);

            DateTime? since =
                AchievementService.MemberSinceUtc;

            List<Control> rows = new List<Control>
            {
                new SpecRow(
                    "Achievements",
                    AchievementService.UnlockedAchievementCount + " of " +
                    AchievementService.Achievements.Count),

                new SpecRow(
                    "Badges earned",
                    badgesEarned + " of " + AchievementService.Badges.Count),

                new SpecRow(
                    "Days active",
                    AchievementService.DaysActive.ToString(CultureInfo.CurrentCulture)),

                new SpecRow(
                    "Member since",
                    since.HasValue
                        ? since.Value.ToLocalTime()
                            .ToString("d MMMM yyyy", CultureInfo.CurrentCulture)
                        : "Today")
            };

            facts.SuspendLayout();

            foreach (Control old in facts.Controls.Cast<Control>().ToList())
            {
                // The account button is not one of the rows and is
                // reused, so it is left alone rather than disposed.
                if (ReferenceEquals(old, accountButton))
                    continue;

                facts.Controls.Remove(old);
                old.Dispose();
            }

            for (int i = rows.Count - 1; i >= 0; i--)
            {
                facts.Controls.Add(rows[i]);
            }

            facts.ResumeLayout();
        }

        /// <summary>
        /// Keeps the button saying what pressing it will do.
        /// </summary>
        public void RefreshAccountButton()
        {
            accountButton.Text =
                Nexus_Launcher.Services.Account.NexusAccountService.IsSignedIn
                    ? "Nexus Account"
                    : "Sign in to Nexus";
        }

        public void ApplyTheme()
        {
            levelLabel.Appearance.ForeColor = ProfileStyle.AccentColor;
            levelLabel.Appearance.Options.UseForeColor = true;

            xpLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            xpLabel.Appearance.Options.UseForeColor = true;

            showcaseLabel.Appearance.ForeColor = ProfileStyle.MutedTextColor;
            showcaseLabel.Appearance.Options.UseForeColor = true;
        }

        protected override void Dispose(
            bool disposing)
        {
            if (disposing)
                tips.Dispose();

            base.Dispose(disposing);
        }
    }
}
