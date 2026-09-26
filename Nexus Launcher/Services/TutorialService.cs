using DevExpress.Utils.VisualEffects;
using DevExpress.XtraEditors;
using Nexus_Launcher.Controls.Profile;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// One thing to point out: what to point at, and what to say.
    /// </summary>
    internal class TutorialStep
    {
        public string Title { get; set; }

        public string Body { get; set; }

        /// <summary>
        /// Resolved when the tutorial runs rather than when it is
        /// described, because the control may not exist yet, and a
        /// control that has gone should simply skip its step instead
        /// of throwing.
        /// </summary>
        public Func<object> Target { get; set; }

        public GuideFlyoutLocation Location { get; set; }

        public TutorialStep()
        {
            Location = GuideFlyoutLocation.Default;
        }
    }

    /// <summary>
    /// The pulsing markers that appear the first time a part of Nexus
    /// is used, explaining what it does.
    ///
    /// Shown once each and then never again, tracked by name so a
    /// feature added later gets its own introduction without
    /// re-introducing everything that came before it. The whole set
    /// can be replayed from Settings.
    ///
    /// Deliberately not modal. A walkthrough that blocks the window is
    /// something to get past; a marker that sits there is something to
    /// read when there is a moment for it.
    /// </summary>
    internal static class TutorialService
    {
        /// <summary>
        /// Names of the tutorials. Stored in settings, so they must
        /// not change once shipped.
        /// </summary>
        public const string FullLibrary = "full-library";

        public const string ClientReset = "client-reset";

        public const string Achievements = "achievements";

        public const string ThemeSharing = "theme-sharing";

        private static readonly Dictionary<Guide, TutorialStep> steps =
            new Dictionary<Guide, TutorialStep>();

        /// <summary>
        /// Which manager each marker went into, so taking it away
        /// again does not depend on asking every manager whether it
        /// has it.
        /// </summary>
        private static readonly Dictionary<Guide, AdornerUIManager> owners =
            new Dictionary<Guide, AdornerUIManager>();

        /// <summary>
        /// Which step of its tutorial each marker is, so dismissing it
        /// can be remembered.
        /// </summary>
        private static readonly Dictionary<Guide, int> order =
            new Dictionary<Guide, int>();

        /// <summary>
        /// Guides belonging to a running tutorial, so it can be marked
        /// seen once the last one has been read.
        /// </summary>
        private static readonly Dictionary<string, List<Guide>> running =
            new Dictionary<string, List<Guide>>(
                StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<AdornerUIManager> hooked =
            new HashSet<AdornerUIManager>();

        /// <summary>
        /// Tutorials that have been asked for but whose page is not in
        /// front of the user yet.
        ///
        /// A guide marker is drawn at the screen position of the thing
        /// it points at, so showing one for a page that is stacked
        /// behind another puts a marker in the middle of whatever is
        /// actually on show. They wait here until their page is really
        /// visible, and go back to waiting if it is covered again.
        /// </summary>
        private static readonly Dictionary<string, Waiting> waiting =
            new Dictionary<string, Waiting>(
                StringComparer.OrdinalIgnoreCase);

        private static Timer watcher;

        /// <summary>
        /// Which steps of each tutorial have already been read, so a
        /// page that is covered and shown again does not bring back
        /// markers the user has dealt with.
        /// </summary>
        private static readonly Dictionary<string, HashSet<int>> read =
            new Dictionary<string, HashSet<int>>(
                StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Raised when the tutorials are reset, so anything holding a
        /// page can offer its tips again without the user having to
        /// navigate away and back.
        /// </summary>
        public static event Action Reset;

        /// <summary>
        /// Often enough to feel immediate when a page is opened, rare
        /// enough to cost nothing.
        /// </summary>
        private const int WatchMs = 350;

        private class Waiting
        {
            public ContainerControl Owner;

            public TutorialStep[] Steps;
        }

        //--------------------------------------------------------------
        // What has been seen
        //--------------------------------------------------------------

        public static bool HasSeen(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return true;

            return Seen().Contains(key);
        }

        public static void MarkSeen(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            HashSet<string> seen =
                Seen();

            if (!seen.Add(key))
                return;

            Save(seen);
        }

        /// <summary>
        /// Forgets everything, so the tips appear again. Behind a
        /// button in Settings, for anyone who dismissed them too fast.
        /// </summary>
        public static void ResetAll()
        {
            Save(new HashSet<string>());

            read.Clear();

            Action handler = Reset;

            if (handler != null)
                handler();
        }

        /// <summary>
        /// How many of the tutorials Nexus has have been read, for the
        /// wording on the button that brings them back.
        /// </summary>
        public static int SeenCount
        {
            get
            {
                return Seen().Count;
            }
        }

        /// <summary>
        /// Every tutorial Nexus knows how to show.
        /// </summary>
        public static string[] AllKeys
        {
            get
            {
                return new[]
                {
                    FullLibrary,
                    ClientReset,
                    Achievements,
                    ThemeSharing
                };
            }
        }

        /// <summary>
        /// Drops a queued tutorial entirely, markers and all.
        /// </summary>
        public static void Cancel(
            string key)
        {
            if (key == null)
                return;

            waiting.Remove(key);

            read.Remove(key);

            Hide(key);

            StopWatching();
        }

        private static HashSet<string> Seen()
        {
            try
            {
                string stored =
                    Settings.Default.SeenTutorials ?? string.Empty;

                return new HashSet<string>(
                    stored.Split(
                        new[] { ',' },
                        StringSplitOptions.RemoveEmptyEntries),
                    StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return new HashSet<string>();
            }
        }

        private static void Save(
            HashSet<string> seen)
        {
            try
            {
                Settings.Default.SeenTutorials =
                    string.Join(",", seen.ToArray());

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Showing
        //--------------------------------------------------------------

        /// <summary>
        /// Shows a tutorial if it has not been shown before. Returns
        /// true if it was shown this time.
        /// </summary>
        public static bool ShowOnce(
            ContainerControl owner,
            string key,
            params TutorialStep[] plan)
        {
            if (HasSeen(key))
                return false;

            return Show(owner, key, plan);
        }

        /// <summary>
        /// Shows a tutorial whether or not it has been seen.
        /// </summary>
        /// <summary>
        /// Queues a tutorial. It appears as soon as the page it points
        /// at is actually in front of the user, and steps aside again
        /// if that page is covered.
        /// </summary>
        public static bool Show(
            ContainerControl owner,
            string key,
            params TutorialStep[] plan)
        {
            if (plan == null || plan.Length == 0 || owner == null)
                return false;

            waiting[key] = new Waiting
            {
                Owner = owner,
                Steps = plan
            };

            StartWatching();

            // Shown straight away when the page is already in front,
            // so opening a page and seeing its tips is not delayed by
            // a timer tick.
            return Refresh(key);
        }

        //--------------------------------------------------------------
        // Waiting for the page to be in front
        //--------------------------------------------------------------

        private static void StartWatching()
        {
            if (watcher != null)
                return;

            watcher = new Timer();

            watcher.Interval = WatchMs;

            watcher.Tick += Watcher_Tick;

            watcher.Start();
        }

        private static void StopWatching()
        {
            if (watcher == null || waiting.Count > 0)
                return;

            watcher.Stop();

            watcher.Tick -= Watcher_Tick;

            watcher.Dispose();

            watcher = null;
        }

        private static void Watcher_Tick(
            object sender,
            EventArgs e)
        {
            foreach (string key in waiting.Keys.ToList())
            {
                Refresh(key);
            }

            StopWatching();
        }

        /// <summary>
        /// Brings a waiting tutorial on screen, or takes it off again,
        /// depending on whether its page is in front right now.
        /// </summary>
        private static bool Refresh(
            string key)
        {
            Waiting pending;

            if (!waiting.TryGetValue(key, out pending))
                return false;

            if (pending.Owner == null || pending.Owner.IsDisposed)
            {
                waiting.Remove(key);

                Hide(key);

                return false;
            }

            // Ready when anything still unread is actually on show.
            // Anchoring on the first step alone was wrong twice over:
            // once that step has been read it is no longer a good
            // proxy for the page, and a step further down may be the
            // only one visible.
            HashSet<int> alreadyRead;

            if (!read.TryGetValue(key, out alreadyRead))
                alreadyRead = new HashSet<int>();

            bool ready = false;

            for (int i = 0; i < pending.Steps.Length; i++)
            {
                if (alreadyRead.Contains(i))
                    continue;

                TutorialStep step = pending.Steps[i];

                object target =
                    step.Target == null ? null : step.Target();

                if (ControlVisibility.IsReallyShowing(target))
                {
                    ready = true;

                    break;
                }
            }

            bool onScreen =
                running.ContainsKey(key);

            if (ready == onScreen)
                return onScreen;

            if (!ready)
            {
                // An open flyout is itself something sitting on top of
                // the thing it points at, so the anchor reads as
                // covered while it is being read. Pulling the markers
                // away underneath someone mid sentence is the one time
                // this check must not fire.
                if (IsReading(pending.Owner))
                    return true;

                // Covered, or the page was closed. The markers go, but
                // the tutorial stays queued for next time.
                Hide(key);

                return false;
            }

            return Present(key, pending);
        }

        /// <summary>
        /// The marker whose flyout is open, if any.
        ///
        /// Tracked here rather than read off the manager: its
        /// SelectedElement is read only, so this is the part that can
        /// actually be relied on and tested. The manager's own
        /// selection is still watched, to catch a flyout closed by
        /// clicking away rather than by the button.
        /// </summary>
        private static Guide reading;

        private static bool IsReading(
            ContainerControl owner)
        {
            return reading != null;
        }

        private static bool Present(
            string key,
            Waiting pending)
        {
            ContainerControl owner = pending.Owner;

            TutorialStep[] plan = pending.Steps;

            try
            {
                AdornerUIManager manager =
                    AdornerHost.For(owner);

                if (manager == null)
                    return false;

                Hook(manager);

                Hide(key);

                List<Guide> made =
                    new List<Guide>();

                HashSet<int> alreadyRead;

                if (!read.TryGetValue(key, out alreadyRead))
                    alreadyRead = new HashSet<int>();

                for (int index = 0; index < plan.Length; index++)
                {
                    TutorialStep step = plan[index];

                    if (alreadyRead.Contains(index))
                        continue;

                    object target =
                        step.Target == null ? null : step.Target();

                    // A step whose control is not in front is skipped
                    // rather than pointing at a spot something else is
                    // occupying.
                    if (!ControlVisibility.IsReallyShowing(target))
                        continue;

                    Guide guide =
                        new Guide();

                    guide.TargetElement = target;

                    if (step.Location != GuideFlyoutLocation.Default)
                        guide.Properties.FlyoutLocation = step.Location;

                    manager.Elements.Add(guide);

                    steps[guide] = step;

                    owners[guide] = manager;

                    order[guide] = index;

                    made.Add(guide);
                }

                if (made.Count == 0)
                    return false;

                running[key] = made;

                manager.ShowGuides =
                    DevExpress.Utils.DefaultBoolean.True;

                return true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        /// <summary>
        /// Takes a tutorial's markers away, without recording it as
        /// seen.
        /// </summary>
        public static void Hide(
            string key)
        {
            List<Guide> guides;

            if (key == null || !running.TryGetValue(key, out guides))
                return;

            running.Remove(key);

            foreach (Guide guide in guides)
            {
                Remove(guide);
            }
        }

        private static void Remove(
            Guide guide)
        {
            if (guide == null)
                return;

            if (ReferenceEquals(reading, guide))
                reading = null;

            steps.Remove(guide);

            order.Remove(guide);

            AdornerUIManager manager;

            bool known =
                owners.TryGetValue(guide, out manager);

            owners.Remove(guide);

            try
            {
                if (known && manager != null)
                    manager.Elements.Remove(guide);

                guide.Dispose();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // The flyout
        //--------------------------------------------------------------

        private static void Hook(
            AdornerUIManager manager)
        {
            if (!hooked.Add(manager))
                return;

            manager.QueryGuideFlyoutControl += Manager_QueryFlyout;

            manager.SelectedElementChanged += Manager_SelectionChanged;
        }

        /// <summary>
        /// A flyout closed without the button being pressed, which is
        /// what clicking elsewhere does.
        /// </summary>
        private static void Manager_SelectionChanged(
            object sender,
            EventArgs e)
        {
            AdornerUIManager manager =
                sender as AdornerUIManager;

            if (manager != null && manager.SelectedElement == null)
                reading = null;
        }

        private static void Manager_QueryFlyout(
            object sender,
            QueryGuideFlyoutControlEventArgs e)
        {
            Guide guide =
                e.SelectedElement as Guide;

            TutorialStep step;

            if (guide == null || !steps.TryGetValue(guide, out step))
                return;

            try
            {
                reading = guide;

                e.Control = BuildFlyout(guide, step);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        private static Control BuildFlyout(
            Guide guide,
            TutorialStep step)
        {
            const int width = 290;

            const int pad = 14;

            PanelControl panel =
                new PanelControl();

            panel.BorderStyle =
                DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

            panel.Appearance.BackColor = ProfileStyle.CardColor;

            panel.Appearance.Options.UseBackColor = true;

            LabelControl title =
                new LabelControl();

            title.Text = step.Title ?? string.Empty;

            title.Appearance.Font =
                ProfileStyle.Font(10.5F, FontStyle.Bold);

            title.Appearance.ForeColor = ProfileStyle.TextColor;

            title.Appearance.Options.UseFont = true;

            title.Appearance.Options.UseForeColor = true;

            title.SetBounds(pad, pad, width - pad * 2, 20);

            panel.Controls.Add(title);

            LabelControl body =
                new LabelControl();

            body.Text = step.Body ?? string.Empty;

            body.AutoSizeMode = LabelAutoSizeMode.None;

            body.Appearance.TextOptions.WordWrap =
                DevExpress.Utils.WordWrap.Wrap;

            body.Appearance.Options.UseTextOptions = true;

            body.Appearance.ForeColor = ProfileStyle.TextColor;

            body.Appearance.Options.UseForeColor = true;

            body.Appearance.Font = ProfileStyle.Font(9F);

            body.Appearance.Options.UseFont = true;

            int bodyHeight =
                Measure(body, width - pad * 2);

            body.SetBounds(pad, pad + 26, width - pad * 2, bodyHeight);

            panel.Controls.Add(body);

            SimpleButton got =
                new SimpleButton();

            got.Text = "Got it";

            // Focusable on purpose: it is the only way out of the
            // flyout, so it has to be reachable from the keyboard.

            got.SetBounds(width - pad - 78, pad + 32 + bodyHeight, 78, 26);

            got.Click += (s, e) => Dismiss(guide);

            panel.Controls.Add(got);

            panel.Size =
                new Size(width, got.Bottom + pad);

            return panel;
        }

        private static int Measure(
            LabelControl label,
            int width)
        {
            try
            {
                using (Graphics g = label.CreateGraphics())
                {
                    SizeF size =
                        g.MeasureString(
                            label.Text,
                            label.Appearance.GetFont(),
                            width);

                    return Math.Max(18, (int)Math.Ceiling(size.Height) + 2);
                }
            }
            catch (Exception)
            {
                return 54;
            }
        }

        /// <summary>
        /// One marker has been read. When the last of a tutorial goes,
        /// the tutorial is done and will not come back.
        /// </summary>
        private static void Dismiss(
            Guide guide)
        {
            if (ReferenceEquals(reading, guide))
                reading = null;

            string finished = null;

            foreach (KeyValuePair<string, List<Guide>> pair in running)
            {
                if (!pair.Value.Contains(guide))
                    continue;

                pair.Value.Remove(guide);

                int index;

                if (order.TryGetValue(guide, out index))
                {
                    HashSet<int> alreadyRead;

                    if (!read.TryGetValue(pair.Key, out alreadyRead))
                    {
                        alreadyRead = new HashSet<int>();

                        read[pair.Key] = alreadyRead;
                    }

                    alreadyRead.Add(index);
                }

                if (pair.Value.Count == 0)
                    finished = pair.Key;

                break;
            }

            Remove(guide);

            if (finished == null)
                return;

            running.Remove(finished);

            // Read and done: it should not come back when the page is
            // next opened.
            waiting.Remove(finished);

            read.Remove(finished);

            StopWatching();

            MarkSeen(finished);
        }
    }
}
