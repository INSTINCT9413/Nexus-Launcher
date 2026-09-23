using Nexus_Launcher.Helpers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace Nexus_Launcher.Services.Library
{
    /// <summary>
    /// Poster thumbnails for the Full Library grid, decoded off the UI
    /// thread and cached at display size.
    ///
    /// Grid artwork is often 600 x 900 or larger. Holding one decoded
    /// bitmap per game costs hundreds of megabytes on a big library and
    /// makes scrolling stutter while GDI+ rescales on every paint, so
    /// each poster is decoded once, scaled to the tile size, and the
    /// full size bitmap is thrown away.
    ///
    /// Everything public is safe to call from the UI thread: a miss
    /// returns null straight away, queues the decode, and raises
    /// <see cref="ThumbnailReady"/> when the tile can be repainted.
    /// </summary>
    internal static class LibraryThumbnailCache
    {
        private class Entry
        {
            public Image Image;

            public long Stamp;
        }

        private static readonly object sync =
            new object();

        private static readonly Dictionary<string, Entry> cache =
            new Dictionary<string, Entry>(StringComparer.Ordinal);

        /// <summary>
        /// Requests already queued or running, so a tile scrolling in
        /// and out does not queue the same decode repeatedly.
        /// </summary>
        private static readonly HashSet<string> pending =
            new HashSet<string>(StringComparer.Ordinal);

        private static long counter;

        /// <summary>
        /// Roughly 250 posters at 200 x 300, about 60 MB. Tuned for a
        /// few screens of scrollback rather than a whole library.
        /// </summary>
        private const int Capacity = 250;

        /// <summary>
        /// Raised on a worker thread once a thumbnail has been
        /// decoded. The grid marshals this to the UI thread itself.
        /// </summary>
        public static event Action<string> ThumbnailReady;

        /// <summary>
        /// A cache key that changes when the artwork is replaced, so a
        /// new custom poster is picked up without a restart.
        /// </summary>
        public static string GetKey(
            string libraryKey,
            string path,
            Size size)
        {
            long stamp = 0;

            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    stamp = File.GetLastWriteTimeUtc(path).Ticks;
            }
            catch (Exception)
            {
                // An unreadable file falls through to stamp 0 and is
                // handled as a missing poster by the decode.
            }

            return (libraryKey ?? string.Empty) + "|" +
                (path ?? string.Empty) + "|" +
                size.Width + "x" + size.Height + "|" +
                stamp;
        }

        /// <summary>
        /// The cached thumbnail, or null when it is not ready yet.
        /// Never blocks and never decodes on the calling thread.
        /// </summary>
        public static Image Get(
            string cacheKey)
        {
            if (string.IsNullOrEmpty(cacheKey))
                return null;

            lock (sync)
            {
                Entry entry;

                if (!cache.TryGetValue(cacheKey, out entry))
                    return null;

                entry.Stamp = ++counter;

                return entry.Image;
            }
        }

        /// <summary>
        /// Queues a decode for a thumbnail that is not cached yet.
        ///
        /// <paramref name="title"/> is used to draw a generated
        /// placeholder when the game has no artwork, so an unillustrated
        /// game still gets a stable, distinctive tile.
        /// </summary>
        public static void Request(
            string cacheKey,
            string path,
            string title,
            Size size)
        {
            if (string.IsNullOrEmpty(cacheKey))
                return;

            lock (sync)
            {
                if (cache.ContainsKey(cacheKey))
                    return;

                if (!pending.Add(cacheKey))
                    return;
            }

            ThreadPool.QueueUserWorkItem(state =>
            {
                Image image = null;

                try
                {
                    image = Decode(path, title, size);
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                }

                bool ready = false;

                lock (sync)
                {
                    pending.Remove(cacheKey);

                    if (image != null && !cache.ContainsKey(cacheKey))
                    {
                        Entry entry = new Entry();

                        entry.Image = image;
                        entry.Stamp = ++counter;

                        cache[cacheKey] = entry;

                        Trim();

                        ready = true;
                    }
                    else if (image != null)
                    {
                        // Raced with another decode; keep the first.
                        image.Dispose();
                    }
                }

                if (ready)
                {
                    Action<string> handler = ThumbnailReady;

                    if (handler != null)
                        handler(cacheKey);
                }
            });
        }

        /// <summary>
        /// Decodes a poster and scales it to fill the tile the way CSS
        /// object-fit: cover does, cropping the overflow rather than
        /// distorting the art. Games with no usable file get the
        /// generated placeholder instead.
        /// </summary>
        private static Image Decode(
            string path,
            string title,
            Size size)
        {
            int width = Math.Max(1, size.Width);
            int height = Math.Max(1, size.Height);

            Image source = null;

            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    // Read through a stream copy so the file is not
                    // held open: the user can replace their own custom
                    // artwork while the grid is showing it.
                    byte[] bytes = File.ReadAllBytes(path);

                    using (MemoryStream stream = new MemoryStream(bytes))
                    {
                        source = Image.FromStream(stream);
                    }
                }
            }
            catch (Exception)
            {
                source = null;
            }

            if (source == null)
            {
                try
                {
                    return ArtworkPlaceholder.Create(
                        title,
                        new Size(width, height));
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    return null;
                }
            }

            try
            {
                Bitmap target =
                    new Bitmap(width, height, PixelFormat.Format32bppPArgb);

                using (source)
                using (Graphics g = Graphics.FromImage(target))
                {
                    g.InterpolationMode =
                        InterpolationMode.HighQualityBicubic;

                    g.PixelOffsetMode =
                        PixelOffsetMode.HighQuality;

                    g.CompositingQuality =
                        CompositingQuality.HighQuality;

                    double scale = Math.Max(
                        (double)width / source.Width,
                        (double)height / source.Height);

                    int drawWidth =
                        (int)Math.Ceiling(source.Width * scale);

                    int drawHeight =
                        (int)Math.Ceiling(source.Height * scale);

                    g.DrawImage(
                        source,
                        new Rectangle(
                            (width - drawWidth) / 2,
                            (height - drawHeight) / 2,
                            drawWidth,
                            drawHeight));
                }

                return target;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        /// <summary>
        /// Drops the least recently used thumbnails. Called with the
        /// lock already held.
        /// </summary>
        private static void Trim()
        {
            if (cache.Count <= Capacity)
                return;

            List<KeyValuePair<string, Entry>> oldest =
                cache
                    .OrderBy(x => x.Value.Stamp)
                    .Take(cache.Count - Capacity)
                    .ToList();

            foreach (KeyValuePair<string, Entry> pair in oldest)
            {
                cache.Remove(pair.Key);

                try
                {
                    pair.Value.Image.Dispose();
                }
                catch (Exception)
                {
                    // A bitmap already disposed elsewhere is harmless.
                }
            }
        }

        /// <summary>
        /// Throws the whole cache away, for when artwork has been
        /// re-downloaded or the cache folder cleared.
        /// </summary>
        public static void Clear()
        {
            lock (sync)
            {
                foreach (Entry entry in cache.Values)
                {
                    try
                    {
                        entry.Image.Dispose();
                    }
                    catch (Exception)
                    {
                    }
                }

                cache.Clear();
            }
        }
    }
}
