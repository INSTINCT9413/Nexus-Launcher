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

        /// <summary>
        /// Run just before this step goes on screen, for a step whose
        /// target is on a tab that is not the open one. Opening that
        /// tab is the step's own business: nothing else knows which
        /// page a given step belongs to.
        /// </summary>
        public Action Prepare { get; set; }

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
    /// One marker at a time. Guide mode takes every click in the
    /// window while it is up, so a page covered in dots the user has
    /// to hunt for is no good; each step is opened for them, and
    /// finishing one moves straight on to the next, opening whatever
    /// tab that next step lives on.
    /// </summary>
    internal static class TutorialService
    {
        /// <summary>
        /// Names of the tutorials. Stored in settings, so they must
        /// not change once shipped.
        /// </summary>
        public const string FullLibrary = "full-library";

        public const string Sidebar = "sidebar";

        public const string GameCard = "game-card";

        public const string ClientReset = "client-reset";

        public const string AddRemove = "add-remove";

        public const string SettingsTour = "settings";

        public const string Account = "account";

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
        /// The marker of each running tutorial. A list of one, kept as
        /// a list because taking a tutorial down should not care how
        /// many markers it happens to have.
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

            /// <summary>
            /// The page the whole tour belongs to, if it has one.
            ///
            /// A tour that walks across several tabs cannot judge
            /// whether it is still in front by looking at the step it
            /// is on, because the next step is on a tab that is not
            /// open yet. The page as a whole is the honest question.
            /// </summary>
            public Func<object> Anchor;
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
                HashSet<string> seen =
                    Seen();

                // Only the ones Nexus still has. A name left over in
                // settings from a tutorial that has since been folded
                // into another would otherwise be counted, and the
                // button would claim more have been read than exist.
                return AllKeys.Count(seen.Contains);
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
                    Sidebar,
                    FullLibrary,
                    GameCard,
                    AddRemove,
                    ClientReset,
                    Account,
                    SettingsTour
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

            return Show(owner, key, null, plan);
        }

        /// <summary>
        /// Shows a tutorial that walks across a page, if it has not
        /// been shown before.
        ///
        /// <paramref name="page"/> is the page the whole tour belongs
        /// to, which is what decides whether it is in front. Needed by
        /// any tour whose later steps live on tabs that are not open
        /// when it starts.
        /// </summary>
        public static bool ShowOnce(
            ContainerControl owner,
            string key,
            Func<object> page,
            TutorialStep[] plan)
        {
            if (HasSeen(key))
                return false;

            return Show(owner, key, page, plan);
        }

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
            return Show(owner, key, null, plan);
        }

        /// <summary>
        /// Queues a tutorial, judged in front by its page rather than
        /// by the step it happens to be on.
        /// </summary>
        public static bool Show(
            ContainerControl owner,
            string key,
            Func<object> page,
            TutorialStep[] plan)
        {
            if (plan == null || plan.Length == 0 || owner == null)
                return false;

            waiting[key] = new Waiting
            {
                Owner = owner,
                Steps = plan,
                Anchor = page
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

            bool ready =
                IsPageShowing(key, pending);

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
        /// Whether this tutorial's page is the one in front.
        /// </summary>
        private static bool IsPageShowing(
            string key,
            Waiting pending)
        {
            // A tour that names its page is judged on that, because
            // its later steps are on tabs that are not open yet.
            if (pending.Anchor != null)
                return ControlVisibility.IsReallyShowing(pending.Anchor());

            // Otherwise: ready when anything still unread is actually
            // on show. Anchoring on the first step alone was wrong
            // twice over: once that step has been read it is no longer
            // a good proxy for the page, and a step further down may
            // be the only one visible.
            HashSet<int> alreadyRead;

            if (!read.TryGetValue(key, out alreadyRead))
                alreadyRead = new HashSet<int>();

            for (int i = 0; i < pending.Steps.Length; i++)
            {
                if (alreadyRead.Contains(i))
                    continue;

                TutorialStep step = pending.Steps[i];

                object target =
                    step.Target == null ? null : step.Target();

                if (ControlVisibility.IsReallyShowing(target))
                    return true;
            }

            return false;
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

        /// <summary>
        /// The manager the last marker belonged to, so guide mode can
        /// be left after it has gone.
        /// </summary>
        private static AdornerUIManager lastManager;

        /// <summary>
        /// The flyout panel last handed to the adorner layer, so its
        /// position on screen can be checked once it is up.
        /// </summary>
        private static Control lastFlyout;

        /// <summary>
        /// How each guide's flyout placement is being corrected, and
        /// the best placement found for it so far.
        /// </summary>
        private class Placing
        {
            public int Attempt;

            public int BestOverflow = int.MaxValue;

            public GuideFlyoutLocation BestLocation;

            public Point BestOffset;
        }

        private static readonly Dictionary<Guide, Placing> fitting =
            new Dictionary<Guide, Placing>();

        /// <summary>
        /// True while a flyout is being closed and reopened to move
        /// it, which must not read as the user having closed it.
        /// </summary>
        private static bool placing;

        /// <summary>
        /// How close to the edge of the screen a flyout may sit.
        /// </summary>
        private const int ScreenMargin = 8;

        /// <summary>
        /// Placements to fall back on, in the order they are tried.
        /// </summary>
        private static readonly GuideFlyoutLocation[] FitOrder =
            new[]
            {
                GuideFlyoutLocation.Right,
                GuideFlyoutLocation.Left,
                GuideFlyoutLocation.Top,
                GuideFlyoutLocation.Bottom
            };

        /// <summary>
        /// Two goes at each placement: one nudging where it is, one
        /// moving on to the next placement.
        /// </summary>
        private static readonly int FitAttempts =
            FitOrder.Length * 2 + 1;

        private static bool IsReading(
            ContainerControl owner)
        {
            return reading != null || placing;
        }

        /// <summary>
        /// Brings a flyout back onto the screen when it hangs off it.
        ///
        /// A flyout is placed beside the thing it points at, and
        /// nothing about that placement knows how big the flyout
        /// turned out to be. Point at something near an edge -- the
        /// sidebar shows its launcher groups as a strip along the
        /// bottom, which a maximised window pushes right down to the
        /// edge of the screen -- and the buttons end up past the edge
        /// with no way to reach them.
        ///
        /// Nudging it with an offset is tried first, since that keeps
        /// the flyout beside what it points at. The control ignores
        /// the offset for some placements, so a placement that fits
        /// is looked for as well, and whatever came closest is used
        /// if none of them fit.
        /// </summary>
        private static void FitOnScreen(
            AdornerUIManager manager,
            Guide guide)
        {
            if (manager == null || guide == null || guide.IsDisposing)
                return;

            Control panel = lastFlyout;

            if (panel == null ||
                panel.IsDisposed ||
                !panel.IsHandleCreated)
            {
                return;
            }

            int dx;

            int dy;

            int overflow;

            try
            {
                Rectangle now =
                    panel.RectangleToScreen(panel.ClientRectangle);

                Rectangle area =
                    Screen.FromRectangle(now).WorkingArea;

                area.Inflate(-ScreenMargin, -ScreenMargin);

                dx = 0;

                dy = 0;

                if (now.Right > area.Right)
                    dx = area.Right - now.Right;

                if (now.Left + dx < area.Left)
                    dx = area.Left - now.Left;

                if (now.Bottom > area.Bottom)
                    dy = area.Bottom - now.Bottom;

                if (now.Top + dy < area.Top)
                    dy = area.Top - now.Top;

                overflow = Math.Abs(dx) + Math.Abs(dy);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return;
            }

            Placing state;

            if (!fitting.TryGetValue(guide, out state))
            {
                state = new Placing();

                fitting[guide] = state;
            }

            if (overflow < state.BestOverflow)
            {
                state.BestOverflow = overflow;

                state.BestLocation =
                    guide.Properties.FlyoutLocation
                        ?? GuideFlyoutLocation.Default;

                state.BestOffset =
                    guide.Properties.FlyoutOffset ?? Point.Empty;
            }

            // Where it is now is fine.
            if (overflow == 0)
            {
                fitting.Remove(guide);

                return;
            }

            if (state.Attempt >= FitAttempts)
            {
                // Nothing fits, so settle on whichever was closest.
                fitting.Remove(guide);

                if (state.BestOverflow < overflow)
                {
                    Apply(
                        manager,
                        guide,
                        state.BestLocation,
                        state.BestOffset,
                        false);
                }

                return;
            }

            int attempt = state.Attempt++;

            GuideFlyoutLocation where;

            Point offset;

            if (attempt % 2 == 0)
            {
                // Keep it beside what it points at, just shifted.
                where =
                    guide.Properties.FlyoutLocation
                        ?? GuideFlyoutLocation.Default;

                Point was =
                    guide.Properties.FlyoutOffset ?? Point.Empty;

                offset = new Point(was.X + dx, was.Y + dy);
            }
            else
            {
                where = FitOrder[(attempt / 2) % FitOrder.Length];

                offset = Point.Empty;
            }

            Apply(manager, guide, where, offset, true);
        }

        /// <summary>
        /// Moves a flyout and, when asked, looks again at where it
        /// ended up.
        /// </summary>
        private static void Apply(
            AdornerUIManager manager,
            Guide guide,
            GuideFlyoutLocation where,
            Point offset,
            bool thenCheck)
        {
            ContainerControl owner = manager.Owner;

            try
            {
                placing = true;

                guide.Properties.FlyoutLocation = where;

                guide.Properties.FlyoutOffset = offset;

                // Closed and reopened, because the placement is read
                // when the flyout goes up rather than continuously.
                manager.SelectElement(null);

                manager.SelectElement(guide);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
            finally
            {
                placing = false;
            }

            if (!thenCheck ||
                owner == null ||
                owner.IsDisposed ||
                !owner.IsHandleCreated)
            {
                return;
            }

            try
            {
                owner.BeginInvoke(new Action(() =>
                    FitOnScreen(manager, guide)));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        //--------------------------------------------------------------
        // Guide mode
        //--------------------------------------------------------------

        /// <summary>
        /// The window a tour is running on, and the tutorial it is for,
        /// so Escape knows what to close.
        /// </summary>
        private static ContainerControl touring;

        private static string touringKey;

        private static bool restoreKeyPreview;

        private static void EnterGuideMode(
            AdornerUIManager manager,
            ContainerControl owner,
            string key)
        {
            manager.ShowGuides =
                DevExpress.Utils.DefaultBoolean.True;

            manager.AllowTabNavigation = true;

            manager.AllowArrowKeysNavigation = true;

            if (ReferenceEquals(touring, owner) && touringKey == key)
                return;

            touring = owner;

            touringKey = key;

            Form form =
                owner as Form ?? owner.FindForm();

            if (form == null)
                return;

            // Escape has to work. A guide mode with no way out is the
            // whole complaint.
            restoreKeyPreview = form.KeyPreview;

            form.KeyPreview = true;

            form.KeyDown -= Form_KeyDown;

            form.KeyDown += Form_KeyDown;
        }

        /// <summary>
        /// Gives the window back.
        /// </summary>
        private static void ExitGuideMode(
            AdornerUIManager manager)
        {
            try
            {
                if (manager != null)
                {
                    manager.ShowGuides =
                        DevExpress.Utils.DefaultBoolean.False;
                }
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            reading = null;

            ContainerControl owner = touring;

            touring = null;

            touringKey = null;

            if (owner == null || owner.IsDisposed)
                return;

            Form form =
                owner as Form ?? owner.FindForm();

            if (form == null || form.IsDisposed)
                return;

            form.KeyDown -= Form_KeyDown;

            form.KeyPreview = restoreKeyPreview;
        }

        /// <summary>
        /// Leaves guide mode if this window has no markers left on it.
        /// </summary>
        private static void ExitIfNothingLeft(
            AdornerUIManager manager)
        {
            if (manager == null)
                return;

            foreach (AdornerElement element in manager.Elements)
            {
                if (element is Guide)
                    return;
            }

            ExitGuideMode(manager);
        }

        private static void Form_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Escape || touringKey == null)
                return;

            e.Handled = true;

            // Left for now, not dismissed: it comes back next time the
            // page is opened.
            string key = touringKey;

            Hide(key);
        }

        private static void Select(
            AdornerUIManager manager,
            Guide guide,
            string key)
        {
            if (manager == null || guide == null)
                return;

            ContainerControl owner = manager.Owner;

            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated)
                return;

            // After the current message, so the markers have been laid
            // out before one of them is opened.
            try
            {
                owner.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!guide.IsDisposing)
                            manager.SelectElement(guide);
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                    }

                    // Checked on the next pass rather than here, so a
                    // selection the control makes in its own time is
                    // not mistaken for a refusal.
                    try
                    {
                        owner.BeginInvoke(new Action(() =>
                            VerifyOpened(manager, guide, key)));
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                    }
                }));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// A marker that will not open is worse than no marker at all.
        ///
        /// Some containers hand their own sub elements to the adorner
        /// layer and refuse to be selected as a whole; an accordion
        /// and the settings tab pane both do. The marker appears, the
        /// flyout never does, and because guide mode has the window
        /// there is nothing the user can click. Step over it instead.
        /// </summary>
        private static void VerifyOpened(
            AdornerUIManager manager,
            Guide guide,
            string key)
        {
            if (manager == null ||
                guide == null ||
                guide.IsDisposing ||
                key == null)
            {
                return;
            }

            if (manager.SelectedElement != null)
            {
                FitOnScreen(manager, guide);

                return;
            }

            List<Guide> current;

            // Already moved on under its own steam.
            if (!running.TryGetValue(key, out current) ||
                !current.Contains(guide))
            {
                return;
            }

            int index;

            order.TryGetValue(guide, out index);

            Program.LogCrash(new InvalidOperationException(
                "Tutorial '" + key + "' step " + (index + 1) +
                " could not be opened: its target will not take a " +
                "guide flyout. Skipping to the next step."));

            Dismiss(guide);
        }

        /// <summary>
        /// Puts the next unread step of a tutorial on screen, opening
        /// whatever tab it lives on first. False when none of what is
        /// left can be shown.
        /// </summary>
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

                // One tour at a time. Guide mode takes the whole
                // window, so two tutorials whose pages are both up --
                // the sidebar and the Full Library, when Nexus is set
                // to open on the library -- would put two markers on
                // one window and fight over it. The other one stays
                // queued and gets its turn.
                foreach (string other in running.Keys)
                {
                    if (!string.Equals(
                            other,
                            key,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return false;
                    }
                }

                Hide(key);

                HashSet<int> alreadyRead;

                if (!read.TryGetValue(key, out alreadyRead))
                    alreadyRead = new HashSet<int>();

                for (int index = 0; index < plan.Length; index++)
                {
                    if (alreadyRead.Contains(index))
                        continue;

                    TutorialStep step = plan[index];

                    // Open the tab this step is on. Selecting a tab
                    // page lays out synchronously, so the hit test
                    // below sees where things have just landed.
                    if (step.Prepare != null)
                    {
                        try
                        {
                            step.Prepare();
                        }
                        catch (Exception ex)
                        {
                            Program.LogCrash(ex);
                        }
                    }

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

                    running[key] =
                        new List<Guide> { guide };

                    EnterGuideMode(manager, owner, key);

                    // Opened straight away rather than waiting to be
                    // clicked. The window is taking no other input, so
                    // leaving the user to find a small pulsing dot with
                    // a dead window is not a fair thing to do.
                    Select(manager, guide, key);

                    return true;
                }

                return false;
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

            AdornerUIManager manager = null;

            foreach (Guide guide in guides)
            {
                AdornerUIManager owning;

                if (owners.TryGetValue(guide, out owning))
                    manager = owning;

                Remove(guide);
            }

            ExitIfNothingLeft(manager);
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

            fitting.Remove(guide);

            AdornerUIManager manager;

            bool known =
                owners.TryGetValue(guide, out manager);

            if (known)
                lastManager = manager;

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

                lastFlyout = BuildFlyout(guide, step);

                e.Control = lastFlyout;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// The tutorial a marker belongs to, or null.
        /// </summary>
        private static string KeyOf(
            Guide guide)
        {
            foreach (KeyValuePair<string, List<Guide>> pair in running)
            {
                if (pair.Value.Contains(guide))
                    return pair.Key;
            }

            return null;
        }

        /// <summary>
        /// "2 of 6", so a step that opens a different tab still reads
        /// as part of one walk through rather than a fresh surprise.
        /// </summary>
        private static string Position(
            Guide guide)
        {
            int index;

            string key = KeyOf(guide);

            Waiting pending;

            if (key == null ||
                !order.TryGetValue(guide, out index) ||
                !waiting.TryGetValue(key, out pending) ||
                pending.Steps.Length <= 1)
            {
                return string.Empty;
            }

            return "  " + (index + 1) + " of " + pending.Steps.Length;
        }

        /// <summary>
        /// Whether anything is still unread after this step, which is
        /// the difference between "Next" and "Got it".
        /// </summary>
        private static bool HasMore(
            Guide guide)
        {
            int index;

            string key = KeyOf(guide);

            Waiting pending;

            if (key == null ||
                !order.TryGetValue(guide, out index) ||
                !waiting.TryGetValue(key, out pending))
            {
                return false;
            }

            HashSet<int> alreadyRead;

            if (!read.TryGetValue(key, out alreadyRead))
                alreadyRead = new HashSet<int>();

            for (int i = index + 1; i < pending.Steps.Length; i++)
            {
                if (!alreadyRead.Contains(i))
                    return true;
            }

            return false;
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

            title.Text =
                (step.Title ?? string.Empty) + Position(guide);

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

            // Guide mode takes every click in the window, so the way
            // out has to be on the flyout itself. Escape does the same
            // thing as Skip, for anyone who reaches for it.
            SimpleButton skip =
                new SimpleButton();

            skip.Text = "Skip tips";

            skip.SetBounds(pad, pad + 32 + bodyHeight, 74, 26);

            skip.Click += (s, e) => BeginSkip(guide);

            panel.Controls.Add(skip);

            SimpleButton got =
                new SimpleButton();

            got.Text = HasMore(guide) ? "Next" : "Got it";

            // Focusable on purpose: it is the way out of the flyout,
            // so it has to be reachable from the keyboard.

            got.SetBounds(width - pad - 78, pad + 32 + bodyHeight, 78, 26);

            got.Click += (s, e) => BeginDismiss(guide);

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
        /// Ends the whole tour, and does not offer it again.
        /// </summary>
        private static void BeginSkip(
            Guide guide)
        {
            string key = KeyOf(guide);

            if (key == null)
            {
                BeginDismiss(guide);

                return;
            }

            AdornerUIManager manager;

            ContainerControl owner =
                owners.TryGetValue(guide, out manager) && manager != null
                    ? manager.Owner
                    : null;

            Action finish = () =>
            {
                Hide(key);

                waiting.Remove(key);

                read.Remove(key);

                StopWatching();

                // Skipping is a decision, so it is remembered. Review
                // Guides in Advanced Settings brings it back.
                MarkSeen(key);
            };

            if (owner == null || owner.IsDisposed || !owner.IsHandleCreated)
            {
                finish();

                return;
            }

            try
            {
                owner.BeginInvoke(finish);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                finish();
            }
        }

        /// <summary>
        /// Takes the marker away once its own click has finished.
        ///
        /// Tearing an adorner element down from inside the flyout it
        /// put on screen leaves the manager mid interaction, still
        /// holding the mouse, and the window stops responding to
        /// anything at all.
        /// </summary>
        private static void BeginDismiss(
            Guide guide)
        {
            AdornerUIManager manager;

            ContainerControl owner =
                owners.TryGetValue(guide, out manager) && manager != null
                    ? manager.Owner
                    : null;

            if (owner == null ||
                owner.IsDisposed ||
                !owner.IsHandleCreated)
            {
                Dismiss(guide);

                return;
            }

            try
            {
                owner.BeginInvoke(new Action(() => Dismiss(guide)));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                Dismiss(guide);
            }
        }

        /// <summary>
        /// This step has been read: on to the next, or done.
        /// </summary>
        private static void Dismiss(
            Guide guide)
        {
            if (ReferenceEquals(reading, guide))
                reading = null;

            string key = KeyOf(guide);

            if (key != null)
            {
                int index;

                if (order.TryGetValue(guide, out index))
                {
                    HashSet<int> alreadyRead;

                    if (!read.TryGetValue(key, out alreadyRead))
                    {
                        alreadyRead = new HashSet<int>();

                        read[key] = alreadyRead;
                    }

                    alreadyRead.Add(index);
                }

                running.Remove(key);
            }

            Remove(guide);

            if (key == null)
            {
                ExitIfNothingLeft(lastManager);

                return;
            }

            Waiting pending;

            if (!waiting.TryGetValue(key, out pending))
            {
                ExitIfNothingLeft(lastManager);

                return;
            }

            // Straight on to the next step, which may well be on
            // another tab, so the tour visibly moves rather than
            // appearing to do nothing.
            if (Present(key, pending))
                return;

            // Nothing left that can be shown. Everything read is the
            // ordinary end of a tour. Steps left unread but with
            // nowhere to point happen when a control has gone or its
            // page was closed part way through: that is only the end
            // if the page is still in front, otherwise the rest is
            // owed to the user next time they open it.
            if (AllRead(key, pending) || IsPageShowing(key, pending))
                Finish(key);

            ExitIfNothingLeft(lastManager);
        }

        private static bool AllRead(
            string key,
            Waiting pending)
        {
            HashSet<int> alreadyRead;

            if (!read.TryGetValue(key, out alreadyRead))
                return false;

            for (int i = 0; i < pending.Steps.Length; i++)
            {
                if (!alreadyRead.Contains(i))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// A tutorial is done with and will not be offered again.
        /// </summary>
        private static void Finish(
            string key)
        {
            running.Remove(key);

            waiting.Remove(key);

            read.Remove(key);

            StopWatching();

            MarkSeen(key);
        }
    }
}
