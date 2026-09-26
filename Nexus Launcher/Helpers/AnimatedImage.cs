using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace Nexus_Launcher.Helpers
{
    /// <summary>
    /// An image that may have more than one frame, loaded without
    /// locking the file it came from.
    ///
    /// Nexus normally loads artwork by copying a Bitmap, which both
    /// unlocks the file and, unavoidably, throws away every frame but
    /// the current one. A hero can be an animated gif, so this reads
    /// the bytes instead and keeps the stream alive underneath the
    /// image: GDI+ needs the stream for the lifetime of a multi frame
    /// image, and the file is free either way.
    ///
    /// Nothing here starts animating on its own. The control that owns
    /// the image decides when it is worth spending frames on.
    /// </summary>
    internal sealed class AnimatedImage : IDisposable
    {
        /// <summary>
        /// Past this, the frames are not worth the memory: a 1920 wide
        /// hero costs about four and a half megabytes a frame, and a
        /// long gif would quietly cost hundreds.
        /// </summary>
        private const int MaxFrames = 120;

        private MemoryStream stream;

        private EventHandler frameChanged;

        private bool animating;

        public Image Image { get; private set; }

        public int FrameCount { get; private set; }

        /// <summary>
        /// Whether this has frames worth playing. A single frame gif is
        /// just a picture.
        /// </summary>
        public bool IsAnimated
        {
            get
            {
                return FrameCount > 1 && FrameCount <= MaxFrames;
            }
        }

        /// <summary>
        /// True when the file has more frames than are worth playing,
        /// so the caller can say why it is showing a still.
        /// </summary>
        public bool TooManyFrames
        {
            get
            {
                return FrameCount > MaxFrames;
            }
        }

        private AnimatedImage()
        {
        }

        public static AnimatedImage Load(
            string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            AnimatedImage result =
                new AnimatedImage();

            try
            {
                // Read in full, so the file is not held open. The
                // stream has to outlive the image, which is why this
                // type owns both.
                result.stream =
                    new MemoryStream(File.ReadAllBytes(path));

                result.Image =
                    Image.FromStream(result.stream);

                result.FrameCount =
                    CountFrames(result.Image);

                return result;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                result.Dispose();

                return null;
            }
        }

        private static int CountFrames(
            Image image)
        {
            try
            {
                return image.GetFrameCount(FrameDimension.Time);
            }
            catch (Exception)
            {
                // Formats with no time dimension are simply stills.
                return 1;
            }
        }

        /// <summary>
        /// Starts playing, calling back on GDI+'s own animation thread
        /// whenever a new frame is ready.
        /// </summary>
        public void Start(
            EventHandler onFrameChanged)
        {
            if (animating || !IsAnimated || Image == null)
                return;

            try
            {
                if (!ImageAnimator.CanAnimate(Image))
                    return;

                frameChanged = onFrameChanged;

                ImageAnimator.Animate(Image, frameChanged);

                animating = true;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public void Stop()
        {
            if (!animating)
                return;

            try
            {
                ImageAnimator.StopAnimate(Image, frameChanged);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            animating = false;
        }

        /// <summary>
        /// Moves the image on to the frame that is due. Called from
        /// painting, because that is the only moment the frame is
        /// actually needed.
        /// </summary>
        public void Advance()
        {
            if (!animating || Image == null)
                return;

            try
            {
                ImageAnimator.UpdateFrames(Image);
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        public void Dispose()
        {
            Stop();

            try
            {
                if (Image != null)
                    Image.Dispose();
            }
            catch (Exception)
            {
            }

            Image = null;

            try
            {
                if (stream != null)
                    stream.Dispose();
            }
            catch (Exception)
            {
            }

            stream = null;
        }
    }
}
