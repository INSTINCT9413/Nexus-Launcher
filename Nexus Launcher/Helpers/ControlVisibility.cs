using System;
using System.Drawing;
using System.Windows.Forms;

namespace Nexus_Launcher.Helpers
{
    /// <summary>
    /// Whether a control is genuinely in front of the user.
    ///
    /// Control.Visible is not that question. It reports whether the
    /// control and its parents are switched on, and says nothing about
    /// whether something else is sitting on top. Nexus stacks the Full
    /// Library, the game card, the store and the profile in the same
    /// panel and swaps them by z-order, so a page that is completely
    /// hidden behind another still says it is visible.
    ///
    /// Anything that draws on top of a control, such as a guide marker,
    /// has to ask the harder question or it ends up pointing at a spot
    /// occupied by something else entirely.
    /// </summary>
    internal static class ControlVisibility
    {
        /// <summary>
        /// True when this is the thing the user would actually touch if
        /// they clicked the middle of it.
        /// </summary>
        public static bool IsReallyShowing(
            object target)
        {
            Control control =
                target as Control;

            // Bar items and the like are not controls and cannot be hit
            // tested this way. They live on a bar that is either there
            // or not, so they are taken at their word.
            if (control == null)
                return target != null;

            try
            {
                if (control.IsDisposed ||
                    !control.Visible ||
                    control.Width <= 0 ||
                    control.Height <= 0)
                {
                    return false;
                }

                Form form =
                    control.FindForm();

                if (form == null ||
                    form.IsDisposed ||
                    !form.Visible ||
                    form.WindowState == FormWindowState.Minimized)
                {
                    return false;
                }

                Point centre =
                    control.PointToScreen(
                        new Point(control.Width / 2, control.Height / 2));

                // Off the window entirely, which happens inside a
                // scrolling panel.
                if (!form.ClientRectangle.Contains(
                        form.PointToClient(centre)))
                {
                    return false;
                }

                Control front =
                    DeepestAt(form, centre);

                // The control itself, or something sitting inside it,
                // both mean this control's middle is on show.
                return front != null &&
                    (ReferenceEquals(front, control) ||
                     control.Contains(front));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return false;
            }
        }

        /// <summary>
        /// The innermost control at a screen point, which is what the
        /// user would hit.
        /// </summary>
        private static Control DeepestAt(
            Control root,
            Point screenPoint)
        {
            Control current = root;

            // Bounded rather than while(true): a malformed parent chain
            // would otherwise spin here forever.
            for (int depth = 0; depth < 64; depth++)
            {
                Control child =
                    current.GetChildAtPoint(
                        current.PointToClient(screenPoint),
                        GetChildAtPointSkip.Invisible |
                            GetChildAtPointSkip.Transparent);

                if (child == null || ReferenceEquals(child, current))
                    return current;

                current = child;
            }

            return current;
        }
    }
}
