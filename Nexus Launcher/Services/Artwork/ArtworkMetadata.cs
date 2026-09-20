using System;

namespace Nexus_Launcher.Services.Artwork
{
    /// <summary>
    /// What happened the last time we went looking for a game's
    /// artwork, written next to the images as metadata.json.
    ///
    /// This exists so a game that SteamGridDB has nothing for is not
    /// looked up again on every single startup. Without it there is no
    /// record of a failed attempt, because a failure writes no files.
    /// </summary>
    internal class ArtworkMetadata
    {
        /// <summary>
        /// The SteamGridDB id that was matched, 0 when nothing matched.
        /// </summary>
        public int ProviderId { get; set; }

        /// <summary>
        /// True once at least one image has actually been saved.
        /// </summary>
        public bool HasArtwork { get; set; }

        public DateTime LastAttemptUtc { get; set; }

        public int FailedAttempts { get; set; }
    }
}
