using DevExpress.Utils.VisualEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Helpers
{
    /// <summary>
    /// The AdornerUIManager for a window, made once and shared.
    ///
    /// Badges and guides both hang off one of these, and a manager
    /// belongs to a window rather than to whatever asked for it first.
    /// Two managers on the same form would each draw their own overlay
    /// and fight over hit testing, so there is exactly one per form and
    /// it is found by asking here.
    /// </summary>
    internal static class AdornerHost
    {
        private static readonly Dictionary<ContainerControl, AdornerUIManager> managers =
            new Dictionary<ContainerControl, AdornerUIManager>();

        /// <summary>
        /// The manager for a window, creating it the first time.
        /// Returns null when there is no window to put one on.
        /// </summary>
        public static AdornerUIManager For(
            ContainerControl owner)
        {
            if (owner == null || owner.IsDisposed)
                return null;

            AdornerUIManager manager;

            if (managers.TryGetValue(owner, out manager))
                return manager;

            try
            {
                manager = new AdornerUIManager();

                manager.Owner = owner;

                managers[owner] = manager;

                // Nothing is left pointing at a window that has gone.
                owner.Disposed += Owner_Disposed;

                return manager;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        /// <summary>
        /// The manager for whatever window a control happens to be on.
        /// </summary>
        public static AdornerUIManager ForControlOn(
            Control control)
        {
            if (control == null)
                return null;

            return For(control.FindForm());
        }

        private static void Owner_Disposed(
            object sender,
            EventArgs e)
        {
            ContainerControl owner =
                sender as ContainerControl;

            if (owner == null)
                return;

            owner.Disposed -= Owner_Disposed;

            Release(owner);
        }

        public static void Release(
            ContainerControl owner)
        {
            AdornerUIManager manager;

            if (owner == null || !managers.TryGetValue(owner, out manager))
                return;

            managers.Remove(owner);

            try
            {
                manager.Elements.Clear();

                manager.Dispose();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// Every live manager, for things that act on all of them.
        /// </summary>
        public static IEnumerable<AdornerUIManager> All
        {
            get
            {
                return managers.Values
                    .Where(x => x != null)
                    .ToList();
            }
        }
    }
}
