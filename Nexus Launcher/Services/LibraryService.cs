using Nexus_Launcher.Models;
using System.Collections.Generic;
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

                if (!games.Any(x =>
                    x.Name == game.Name &&
                    x.Launcher == game.Launcher))
                {
                    games.Add(game);
                }
            }
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