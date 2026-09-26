using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;

namespace Nexus_Launcher.Services
{
    /// <summary>
    /// One of the sounds that can play when something unlocks.
    /// </summary>
    internal class UnlockSound
    {
        /// <summary>
        /// Stored in settings, so it must not change once shipped.
        /// </summary>
        public string Key { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        /// <summary>
        /// Fetched rather than held, because the resource is only
        /// needed when the sound is actually chosen.
        /// </summary>
        public Func<byte[]> Data { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    /// <summary>
    /// Plays a short sound when an achievement or badge unlocks.
    ///
    /// The sounds are embedded rather than loose files: an unlock is
    /// not worth a missing file error, and a file beside the executable
    /// would not survive an install under Program Files being repaired.
    /// </summary>
    internal static class AchievementSoundService
    {
        /// <summary>
        /// The key used when the user has turned the sound off.
        /// </summary>
        public const string NoneKey = "none";

        /// <summary>
        /// Eight achievements can unlock at once, and eight chimes
        /// overlapping is noise rather than feedback. Only the first
        /// of a run plays, and the rest are silent until things have
        /// been quiet for this long.
        /// </summary>
        private static readonly TimeSpan Gap =
            TimeSpan.FromSeconds(2.5);

        private static DateTime lastPlayed = DateTime.MinValue;

        private static SoundPlayer player;

        private static string loadedKey;

        private static readonly object sync = new object();

        //--------------------------------------------------------------
        // The catalogue
        //--------------------------------------------------------------

        public static readonly UnlockSound[] All =
        {
            new UnlockSound
            {
                Key = NoneKey,
                Name = "No sound",
                Description = "Unlocks appear silently.",
                Data = () => null
            },
            new UnlockSound
            {
                Key = "chime",
                Name = "Chime",
                Description =
                    "Two rising bell notes. The classic console " +
                    "achievement sound.",
                Data = () => Resources.unlock_chime
            },
            new UnlockSound
            {
                Key = "arpeggio",
                Name = "Arpeggio",
                Description =
                    "Three glassy notes upward. Lighter than the " +
                    "chime.",
                Data = () => Resources.unlock_arpeggio
            },
            new UnlockSound
            {
                Key = "thump",
                Name = "Thump and Sparkle",
                Description =
                    "A low knock with a bright shimmer over it. Carries " +
                    "over game audio.",
                Data = () => Resources.unlock_thump
            },
            new UnlockSound
            {
                Key = "marimba",
                Name = "Marimba",
                Description =
                    "A warm wooden triad. The most understated of the " +
                    "set.",
                Data = () => Resources.unlock_marimba
            },
            new UnlockSound
            {
                Key = "crystal",
                Name = "Crystal",
                Description =
                    "A quick sparkle upward, with no melody to clash " +
                    "with music already playing.",
                Data = () => Resources.unlock_crystal
            },
        };

        public static UnlockSound Find(
            string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return All[0];

            return All.FirstOrDefault(x => string.Equals(
                       x.Key, key, StringComparison.OrdinalIgnoreCase))
                   ?? All[0];
        }

        /// <summary>
        /// The sound currently chosen in settings.
        /// </summary>
        public static UnlockSound Current
        {
            get
            {
                try
                {
                    return Find(Settings.Default.UnlockSound);
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);

                    return All[0];
                }
            }
        }

        public static void Choose(
            UnlockSound sound)
        {
            if (sound == null)
                return;

            try
            {
                Settings.Default.UnlockSound = sound.Key;

                Settings.Default.Save();
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }

            lock (sync)
            {
                // The next play reloads, rather than using whatever was
                // chosen before.
                Release();
            }
        }

        //--------------------------------------------------------------
        // Playing
        //--------------------------------------------------------------

        /// <summary>
        /// Plays the chosen sound for an unlock, unless one has just
        /// been played.
        /// </summary>
        public static void PlayUnlock()
        {
            DateTime now = DateTime.UtcNow;

            lock (sync)
            {
                if (now - lastPlayed < Gap)
                    return;

                lastPlayed = now;
            }

            Play(Current);
        }

        /// <summary>
        /// Plays a sound regardless of when the last one was, for the
        /// preview button in settings.
        /// </summary>
        public static void Preview(
            UnlockSound sound)
        {
            Play(sound);
        }

        private static void Play(
            UnlockSound sound)
        {
            if (sound == null || sound.Key == NoneKey)
                return;

            try
            {
                lock (sync)
                {
                    if (player == null || loadedKey != sound.Key)
                    {
                        Release();

                        byte[] data = sound.Data();

                        if (data == null || data.Length == 0)
                            return;

                        player = new SoundPlayer(new MemoryStream(data));

                        // Decoded up front, so the first unlock is not
                        // late while the wav is parsed.
                        player.Load();

                        loadedKey = sound.Key;
                    }

                    // Asynchronous on purpose: this is called while a
                    // toast is being shown, and the UI thread must not
                    // wait for audio.
                    player.Play();
                }
            }
            catch (Exception ex)
            {
                // A machine with no sound device, or a locked one, is
                // not a reason to lose the unlock.
                Program.LogCrash(ex);
            }
        }

        private static void Release()
        {
            try
            {
                if (player != null)
                    player.Dispose();
            }
            catch (Exception)
            {
            }

            player = null;

            loadedKey = null;
        }

        /// <summary>
        /// Lets a burst play its first sound again, used when the queue
        /// has been cleared.
        /// </summary>
        public static void ResetBurst()
        {
            lock (sync)
            {
                lastPlayed = DateTime.MinValue;
            }
        }
    }
}
