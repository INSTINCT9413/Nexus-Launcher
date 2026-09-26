using DevExpress.Utils.VisualEffects;
using Nexus_Launcher.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// The little count badges that sit on a button or a menu item.
    ///
    /// Everything is addressed by a key rather than by holding on to
    /// the Badge itself, so the place that raises a notification and
    /// the place that clears it do not have to pass an object between
    /// them: whatever notices new achievements can say
    /// NotificationBadges.Set("profile", ..., 3) and the profile page
    /// can say NotificationBadges.Clear("profile") without the two
    /// knowing about each other.
    /// </summary>
    internal static class NotificationBadges
    {
        /// <summary>
        /// Past this the exact number stops being useful and starts
        /// being too wide to fit on a button.
        /// </summary>
        private const int MaxCount = 99;

        private static readonly Dictionary<string, Badge> badges =
            new Dictionary<string, Badge>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Which manager each badge went into, so clearing it does not
        /// depend on asking every manager whether it has it.
        /// </summary>
        private static readonly Dictionary<string, AdornerUIManager> owners =
            new Dictionary<string, AdornerUIManager>(
                StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Shows a count. Zero or less clears it: a badge reading "0"
        /// is worse than no badge.
        /// </summary>
        public static void Set(
            ContainerControl owner,
            string key,
            object target,
            int count)
        {
            if (count <= 0)
            {
                Clear(key);

                return;
            }

            Set(
                owner,
                key,
                target,
                count > MaxCount ? MaxCount + "+" : count.ToString(),
                BadgePaintStyle.Information);
        }

        /// <summary>
        /// Shows a badge with its own text, for the cases that are not
        /// a count: a dot, an exclamation, "NEW".
        /// </summary>
        public static void Set(
            ContainerControl owner,
            string key,
            object target,
            string text,
            BadgePaintStyle style)
        {
            if (string.IsNullOrWhiteSpace(key) || target == null)
                return;

            try
            {
                AdornerUIManager manager =
                    AdornerHost.For(owner);

                if (manager == null)
                    return;

                Badge badge;

                if (!badges.TryGetValue(key, out badge) ||
                    badge.IsDisposing)
                {
                    badge = new Badge();

                    badge.Properties.Location =
                        System.Drawing.ContentAlignment.TopRight;

                    manager.Elements.Add(badge);

                    badges[key] = badge;

                    owners[key] = manager;
                }

                badge.TargetElement = target;

                badge.Properties.Text = text;

                badge.Properties.PaintStyle = style;

                badge.Visible = true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public static bool IsShowing(
            string key)
        {
            Badge badge;

            return key != null &&
                badges.TryGetValue(key, out badge) &&
                !badge.IsDisposing &&
                badge.Visible;
        }

        public static void Clear(
            string key)
        {
            Badge badge;

            if (key == null || !badges.TryGetValue(key, out badge))
                return;

            badges.Remove(key);

            AdornerUIManager manager;

            bool known =
                owners.TryGetValue(key, out manager);

            owners.Remove(key);

            try
            {
                // Taken off the manager rather than just hidden, so a
                // badge for a window that has closed cannot linger and
                // reappear on the next one.
                if (known && manager != null)
                    manager.Elements.Remove(badge);

                badge.Dispose();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public static void ClearAll()
        {
            foreach (string key in badges.Keys.ToList())
            {
                Clear(key);
            }
        }
    }
}
