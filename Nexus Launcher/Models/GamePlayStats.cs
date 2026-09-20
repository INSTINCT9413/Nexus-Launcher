using System;

namespace Nexus_Launcher.Models
{
    /// <summary>
    /// Play history for a single game, keyed the same way favourites
    /// and groups are so it survives a rescan.
    ///
    /// Name and Launcher are stored as well as the key so the Recently
    /// Played list can be drawn before the library has finished
    /// scanning.
    /// </summary>
    public class GamePlayStats
    {
        public string Key { get; set; }

        public string Name { get; set; }

        public string Launcher { get; set; }

        public DateTime? LastPlayedUtc { get; set; }

        /// <summary>
        /// Total measured play time. Only counts sessions where the
        /// game's process was actually found, so it can undercount.
        /// </summary>
        public long TotalPlaySeconds { get; set; }

        /// <summary>
        /// How many times Launch was pressed. Always exact, unlike
        /// TotalPlaySeconds.
        /// </summary>
        public int LaunchCount { get; set; }

        /// <summary>
        /// Sessions where the process was never found, so the play
        /// time below is known to be incomplete.
        /// </summary>
        public int UntrackedSessions { get; set; }
    }
}
