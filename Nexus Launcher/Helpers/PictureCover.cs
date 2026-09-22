using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraEditors.Repository;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Nexus_Launcher.Helpers
{
    /// <summary>
    /// Makes a PictureEdit fill its area without distorting the image,
    /// like CSS object-fit: cover. The image is scaled until it covers
    /// the whole control and whatever overflows is cropped evenly.
    ///
    /// PictureEdit has no cover mode. Stretch fills but distorts, Zoom
    /// keeps the shape but leaves bars, and Squeeze only ever shrinks.
    /// Clip with a ZoomPercent does exactly what cover needs, but the
    /// right zoom depends on both the control and the image size, so it
    /// has to be worked out again whenever either changes. A fixed zoom
    /// only fits one window size.
    ///
    /// ZoomPercent is also ignored outside Clip mode, which is why
    /// setting it alongside Stretch or Squeeze appears to do nothing.
    /// </summary>
    internal static class PictureCover
    {
        /// <summary>
        /// Keeps a picture edit in cover mode from now on. Alignment
        /// decides which part survives the crop; centre suits banners
        /// whose subject sits in the middle.
        /// </summary>
        public static void Attach(
            PictureEdit edit,
            ContentAlignment alignment = ContentAlignment.MiddleCenter)
        {
            if (edit == null)
                return;

            RepositoryItemPictureEdit properties =
                edit.Properties;

            properties.SizeMode = PictureSizeMode.Clip;
            properties.PictureAlignment = alignment;
            properties.PictureInterpolationMode = InterpolationMode.HighQualityBicubic;

            // Without these the mouse wheel over the banner would zoom
            // or pan it away from the computed fit.
            properties.AllowZoom = DefaultBoolean.False;
            properties.AllowZoomOnMouseWheel = DefaultBoolean.False;
            properties.AllowScrollOnMouseWheel = DefaultBoolean.False;
            properties.ShowZoomSubMenu = DefaultBoolean.False;
            properties.ShowScrollBars = false;
            properties.AllowScrollViaMouseDrag = false;

            // Detach first so attaching twice cannot double the work.
            edit.SizeChanged -= Edit_Changed;
            edit.SizeChanged += Edit_Changed;

            edit.EditValueChanged -= Edit_Changed;
            edit.EditValueChanged += Edit_Changed;

            Apply(edit);
        }

        private static void Edit_Changed(
            object sender,
            EventArgs e)
        {
            Apply(sender as PictureEdit);
        }

        /// <summary>
        /// Works out the zoom for the current image and size. Runs on
        /// its own after a resize or a new image; call it directly only
        /// if something else has changed the zoom.
        /// </summary>
        public static void Apply(
            PictureEdit edit)
        {
            if (edit == null || edit.IsDisposed)
                return;

            Image image =
                edit.Image;

            Size area =
                edit.ClientSize;

            if (image == null ||
                image.Width <= 0 ||
                image.Height <= 0 ||
                area.Width <= 0 ||
                area.Height <= 0)
            {
                return;
            }

            // The larger of the two ratios is the one that leaves no
            // gap; the other axis overflows and gets cropped.
            double scale =
                Math.Max(
                    (double)area.Width / image.Width,
                    (double)area.Height / image.Height);

            double percent =
                scale * 100d;

            // Skip redundant sets; resizing fires this repeatedly.
            if (Math.Abs(edit.Properties.ZoomPercent - percent) > 0.01)
                edit.Properties.ZoomPercent = percent;
        }
    }
}
