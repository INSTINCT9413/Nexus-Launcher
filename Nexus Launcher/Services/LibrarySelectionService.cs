using System;
using Nexus_Launcher.Models;

namespace Nexus_Launcher.Services.Library
{
    internal static class LibrarySelectionService
    {
        public static GameInfo SelectedGame
        {
            get;
            private set;
        }

        public static event Action<GameInfo> SelectedGameChanged;

        public static void Select(GameInfo game)
        {
            if (game == null)
                return;

            SelectedGame = game;

            SelectedGameChanged?.Invoke(game);
        }
    }
}
