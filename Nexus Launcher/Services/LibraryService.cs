using Nexus_Launcher.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Nexus_Launcher.Services
{
    internal static class LibraryService
    {
        private static readonly List<GameInfo> games =
            new List<GameInfo>();

        public static IReadOnlyList<GameInfo> Games
        {
            get
            {
                return games;
            }
        }

        public static void Clear()
        {
            games.Clear();
        }

        public static void AddGames(
            IEnumerable<GameInfo> newGames)
        {
            if (newGames == null)
                return;

            foreach (GameInfo game in newGames)
            {
                if (game == null)
                    continue;

                if (!game.IsInstalled)
                    game.IsInstalled = LooksInstalled(game);

                if (!games.Any(x =>
                    x.Name == game.Name &&
                    x.Launcher == game.Launcher))
                {
                    games.Add(game);
                }
            }
        }

        /// <summary>
        /// A scanner only reports a game because it found it on the
        /// machine, so a recorded path that still exists means the game
        /// is installed however the scanner chose to describe it.
        ///
        /// This only ever turns the flag on, and only against a path
        /// that is really there, so a scanner that deliberately reports
        /// an uninstalled game keeps saying so.
        /// </summary>
        private static bool LooksInstalled(
            GameInfo game)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(game.InstallPath) &&
                    Directory.Exists(game.InstallPath))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(game.ExecutablePath) &&
                    File.Exists(game.ExecutablePath))
                {
                    return true;
                }
            }
            catch (System.Exception)
            {
                // A malformed path is not worth a crash; the game just
                // keeps whatever the scanner said.
            }

            return false;
        }

        public static List<GameInfo> GetGames()
        {
            return games
                .OrderBy(x => x.Name)
                .ToList();
        }

        public static List<GameInfo> GetGames(
            string launcher)
        {
            return games
                .Where(x =>
                    x.Launcher == launcher)
                .OrderBy(x => x.Name)
                .ToList();
        }
    }
}