using Nexus_Launcher.Controls.Notifications;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Nexus_Launcher.Services.Achievements
{
    /// <summary>
    /// Shows unlocks one at a time in the bottom right, each waiting
    /// its turn.
    ///
    /// A burst of unlocks is normal: the first run of a new version
    /// can earn a dozen at once. Rather than stacking them up the
    /// screen or collapsing them into a single "you earned 12 things"
    /// summary, they are queued and each gets its own moment.
    /// </summary>
    internal static class AchievementToastService
    {
        private static readonly Queue<UnlockEvent> pending =
            new Queue<UnlockEvent>();

        private static AchievementToastForm current;

        private static Form owner;

        /// <summary>
        /// How long each card sits on screen once it has slid in,
        /// excluding the slide either side.
        /// </summary>
        public static int DisplaySeconds { get; set; }

        /// <summary>
        /// A burst longer than this is trimmed: past a point, watching
        /// them go by stops being a reward and starts being a queue to
        /// sit through. The rest are still earned and still counted,
        /// they simply do not each get a card.
        /// </summary>
        public static int MaxInOneBurst { get; set; }

        /// <summary>
        /// Turns the popups off without affecting what is earned.
        /// </summary>
        public static bool Enabled { get; set; }

        static AchievementToastService()
        {
            DisplaySeconds = 6;

            MaxInOneBurst = 8;

            Enabled = true;
        }

        /// <summary>
        /// The window whose screen the cards appear on. Also gives the
        /// service a UI thread to marshal onto.
        /// </summary>
        public static void Initialize(
            Form value)
        {
            owner = value;
        }

        /// <summary>
        /// Queues a batch. Safe to call from any thread.
        /// </summary>
        public static void Show(
            IEnumerable<UnlockEvent> unlocks)
        {
            if (!Enabled || unlocks == null)
                return;

            List<UnlockEvent> list =
                new List<UnlockEvent>(unlocks);

            if (list.Count == 0)
                return;

            if (owner == null || owner.IsDisposed)
                return;

            if (owner.InvokeRequired)
            {
                try
                {
                    owner.BeginInvoke(
                        new Action(() => Enqueue(list)));
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }

                return;
            }

            Enqueue(list);
        }

        private static void Enqueue(
            List<UnlockEvent> unlocks)
        {
            try
            {
                // Badges first: a tier is the bigger moment, and it
                // reads oddly to be told about a badge only after the
                // achievements that earned it.
                unlocks.Sort((a, b) =>
                {
                    int byKind =
                        b.IsBadge.CompareTo(a.IsBadge);

                    return byKind != 0
                        ? byKind
                        : b.Points.CompareTo(a.Points);
                });

                int room =
                    Math.Max(0, MaxInOneBurst - pending.Count);

                for (int i = 0; i < unlocks.Count && i < room; i++)
                {
                    pending.Enqueue(unlocks[i]);
                }

                ShowNext();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private static void ShowNext()
        {
            if (current != null || pending.Count == 0)
                return;

            if (owner == null || owner.IsDisposed)
            {
                pending.Clear();

                return;
            }

            UnlockEvent next =
                pending.Dequeue();

            try
            {
                AchievementToastForm toast =
                    new AchievementToastForm(next, DisplaySeconds);

                current = toast;

                toast.Finished += () =>
                {
                    current = null;

                    // A short gap so two cards do not read as one
                    // sliding straight into the next.
                    Timer gap = new Timer();

                    gap.Interval = 220;

                    gap.Tick += (s, e) =>
                    {
                        gap.Stop();

                        gap.Dispose();

                        ShowNext();
                    };

                    gap.Start();
                };

                toast.Play(owner);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                current = null;
            }
        }

        /// <summary>
        /// Drops anything still waiting and closes what is on screen,
        /// for shutdown.
        /// </summary>
        public static void Clear()
        {
            pending.Clear();

            if (current != null)
                current.DismissNow();
        }
    }
}
