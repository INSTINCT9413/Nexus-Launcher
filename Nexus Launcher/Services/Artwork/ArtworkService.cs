using Nexus_Launcher.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Artwork
{
    internal static class ArtworkService
    {
        private static readonly ConcurrentQueue<GameInfo> DownloadQueue =
            new ConcurrentQueue<GameInfo>();

        private static readonly AutoResetEvent QueueEvent =
            new AutoResetEvent(false);
        private static readonly List<GameInfo> registeredGames =
    new List<GameInfo>();
        private static bool WorkerStarted;

        public static event Action<GameInfo> ArtworkDownloaded;
        public static Task CurrentDownloadTask
        {
            get;
            private set;
        }
        private static Task processingTask =
    Task.CompletedTask;
        private static readonly object WorkerSync =
    new object();

        private static TaskCompletionSource<bool>
            QueueCompletion =
                new TaskCompletionSource<bool>();
        public static int TotalArtworkJobs
        {
            get;
            set;
        }

        public static int CompletedArtworkJobs
        {
            get;
            set;
        }
        public delegate void ArtworkProgressHandler(
    int completed,
    int total);

        public static event ArtworkProgressHandler
            ArtworkProgressChanged;
        public static Task QueueFinished
        {
            get
            {
                return QueueCompletion.Task;
            }
        }
        public static Task ProcessingTask
        {
            get
            {
                return processingTask;
            }
        }
        public static void RegisterGame(GameInfo game)
        {
            if (game == null)
                return;

            if (!registeredGames.Contains(game))
            {
                registeredGames.Add(game);
            }

            // Every registered game counts toward the total.
            TotalArtworkJobs++;

            ArtworkCache.LoadCachedArtwork(game);

            //----------------------------------------------------
            // Already cached
            //----------------------------------------------------

            if (game.HasArtwork)
            {
                CompletedArtworkJobs++;

                ArtworkDownloaded?.Invoke(game);

                ArtworkProgressChanged?.Invoke(
                    CompletedArtworkJobs,
                    TotalArtworkJobs);

                return;
            }

            //----------------------------------------------------
            // Needs downloading
            //----------------------------------------------------

            lock (WorkerSync)
            {
                if (QueueCompletion.Task.IsCompleted)
                {
                    QueueCompletion =
                        new TaskCompletionSource<bool>();
                }
            }

            DownloadQueue.Enqueue(game);

            QueueEvent.Set();

            StartWorker();
        }

        private static void StartWorker()
        {
            if (WorkerStarted)
                return;

            WorkerStarted = true;

            Task.Run(WorkerLoop);
        }

        private static async Task WorkerLoop()
        {
            while (true)
            {
                QueueEvent.WaitOne();

                while (DownloadQueue.TryDequeue(out GameInfo game))
                {
                    try
                    {
                        await DownloadArtwork(game);
                    }
                    catch
                    {
                    }
                    lock (WorkerSync)
                    {
                        if (DownloadQueue.IsEmpty)
                        {
                            QueueCompletion.TrySetResult(true);
                        }
                    }
                }
            }
        }
        public static void FinishRegistration()
        {
            lock (WorkerSync)
            {
                if (DownloadQueue.IsEmpty)
                {
                    QueueCompletion.TrySetResult(true);
                }
            }
        }
        public static void RedownloadAllArtwork()
        {
            foreach (GameInfo game in registeredGames)
            {
                Directory.Delete(
                    ArtworkCache.GetGameFolder(game),
                    true);

                RegisterGame(game);
            }
        }
        private static async Task DownloadArtwork(GameInfo game)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(
                    "[Artwork] " + game.Name);

                if (ArtworkCache.ArtworkExists(game))
                {
                    ArtworkCache.LoadCachedArtwork(game);

                    game.HasArtwork = true;

                    ArtworkDownloaded?.Invoke(game);

                    return;
                }

                SteamGridGame steamGridGame = null;

                //----------------------------------------------------
                // Platform specific lookup
                //----------------------------------------------------

                switch (game.Launcher)
                {
                    case "Steam":
                        steamGridGame =
                            await SteamGridDbClient.LookupSteamAsync(
                                game.AppId);
                        break;

                    case "BattleNet":
                        steamGridGame =
                            await SteamGridDbClient.LookupBattleNetAsync(
                                game.ProductId);
                        break;

                    case "Epic":
                        steamGridGame =
                            await SteamGridDbClient.LookupEpicAsync(
                                game.epicLauncherAppId);
                        break;

                    case "EA":
                        steamGridGame =
                            await SteamGridDbClient.LookupOriginAsync(
                                game.AppUserModelId);
                        break;

                    case "Ubisoft":
                        steamGridGame =
                            await SteamGridDbClient.LookupUbisoftAsync(
                                game.AppUserModelId);
                        break;
                }

                //----------------------------------------------------
                // Fall back to name search
                //----------------------------------------------------

                if (steamGridGame == null)
                {
                    steamGridGame =
                        await SteamGridDbClient.SearchAsync(
                            game.Name);
                }

                if (steamGridGame == null)
                    return;

                //----------------------------------------------------
                // Save SteamGrid ID
                //----------------------------------------------------

                game.ArtworkProviderId =
                    steamGridGame.Id;

                //----------------------------------------------------
                // Grid
                //----------------------------------------------------

                SteamGridImage grid =
                    await SteamGridDbClient.GetGridAsync(
                        steamGridGame.Id);

                System.Diagnostics.Debug.WriteLine(
                    $"Grid Found: {grid != null}");

                if (grid != null)
                {
                    bool gridSuccess =
                        await SteamGridDbClient.DownloadFileAsync(
                            grid.Url,
                            ArtworkCache.GetGridPath(game));

                    System.Diagnostics.Debug.WriteLine(
                        $"Grid Success: {gridSuccess}");
                }

                //----------------------------------------------------
                // Hero
                //----------------------------------------------------

                SteamGridImage hero =
                    await SteamGridDbClient.GetHeroAsync(
                        steamGridGame.Id);

                if (hero != null)
                {
                    bool heroSuccess =
                        await SteamGridDbClient.DownloadFileAsync(
                            hero.Url,
                            ArtworkCache.GetHeroPath(game));

                    System.Diagnostics.Debug.WriteLine(
                        $"Hero Success: {heroSuccess}");
                }

                //----------------------------------------------------
                // Logo
                //----------------------------------------------------

                SteamGridImage logo =
                    await SteamGridDbClient.GetLogoAsync(
                        steamGridGame.Id);

                if (logo != null)
                {
                    await SteamGridDbClient.DownloadFileAsync(
                        logo.Url,
                        ArtworkCache.GetLogoPath(game));
                }

                //----------------------------------------------------
                // Reload artwork
                //----------------------------------------------------

                ArtworkCache.LoadCachedArtwork(game);

                ArtworkDownloaded?.Invoke(game);

                System.Diagnostics.Debug.WriteLine(
                    "Downloaded artwork for: " + game.Name);

                await Task.Delay(500);
            }
            finally
            {
                CompletedArtworkJobs++;

                ArtworkProgressChanged?.Invoke(
                    CompletedArtworkJobs,
                    TotalArtworkJobs);
            }
        }
    }
}