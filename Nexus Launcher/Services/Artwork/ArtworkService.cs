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
        private const int MaxWorkers = 4;

        private static bool WorkersStarted;

        private static int ActiveDownloads;

        private static readonly object WorkerLock =
            new object();
        private static readonly ConcurrentQueue<GameInfo> DownloadQueue =
            new ConcurrentQueue<GameInfo>();

        private static readonly AutoResetEvent QueueEvent =
            new AutoResetEvent(false);
        private static readonly List<GameInfo> registeredGames =
    new List<GameInfo>();
        

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
        public static int totalArtworkJobs;
        public static int completedArtworkJobs;

        public static int TotalArtworkJobs
        {
            get => totalArtworkJobs;
        }

        public static int CompletedArtworkJobs
        {
            get => completedArtworkJobs;
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

            

            ArtworkCache.LoadCachedArtwork(game);

            //----------------------------------------------------
            // Already cached
            //----------------------------------------------------

            if (game.HasArtwork)
            {
               
                return;
            }
            // Every registered game counts toward the total.
            Interlocked.Increment(ref totalArtworkJobs);
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
            lock (WorkerLock)
            {
                if (WorkersStarted)
                    return;

                WorkersStarted = true;

                for (int i = 0; i < MaxWorkers; i++)
                {
                    Task.Run(WorkerLoop);
                }
            }
        }

        private static async Task WorkerLoop()
        {
            while (true)
            {
                QueueEvent.WaitOne();

                while (DownloadQueue.TryDequeue(out GameInfo game))
                {
                    Interlocked.Increment(ref ActiveDownloads);

                    try
                    {
                        await DownloadArtwork(game);
                    }
                    catch (Exception ex)
                    {
                        Program.LogCrash(ex);
                        System.Diagnostics.Debug.WriteLine(
                            ex.ToString());
                    }
                    finally
                    {
                        if (Interlocked.Decrement(ref ActiveDownloads) == 0 &&
                            DownloadQueue.IsEmpty)
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
                // Nothing needed downloading.
                if (TotalArtworkJobs == 0)
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
                // Request artwork simultaneously
                //----------------------------------------------------

                Task<SteamGridImage> gridTask =
                    SteamGridDbClient.GetGridAsync(
                        steamGridGame.Id);

                Task<SteamGridImage> heroTask =
                    SteamGridDbClient.GetHeroAsync(
                        steamGridGame.Id);

                Task<SteamGridImage> logoTask =
                    SteamGridDbClient.GetLogoAsync(
                        steamGridGame.Id);

                await Task.WhenAll(
                    gridTask,
                    heroTask,
                    logoTask);

                SteamGridImage grid = gridTask.Result;
                SteamGridImage hero = heroTask.Result;
                SteamGridImage logo = logoTask.Result;

                //----------------------------------------------------
                // Download artwork simultaneously
                //----------------------------------------------------

                List<Task<bool>> downloadTasks =
                    new List<Task<bool>>();

                List<string> downloadNames =
                    new List<string>();

                if (grid != null)
                {
                    downloadTasks.Add(
                        SteamGridDbClient.DownloadFileAsync(
                            grid.Url,
                            ArtworkCache.GetGridPath(game)));

                    downloadNames.Add("Grid");
                }

                if (hero != null)
                {
                    downloadTasks.Add(
                        SteamGridDbClient.DownloadFileAsync(
                            hero.Url,
                            ArtworkCache.GetHeroPath(game)));

                    downloadNames.Add("Hero");
                }

                if (logo != null)
                {
                    downloadTasks.Add(
                        SteamGridDbClient.DownloadFileAsync(
                            logo.Url,
                            ArtworkCache.GetLogoPath(game)));

                    downloadNames.Add("Logo");
                }

                bool[] results =
                    await Task.WhenAll(downloadTasks);

                for (int i = 0; i < results.Length; i++)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"{downloadNames[i]} Success: {results[i]}");
                }

                //----------------------------------------------------
                // Reload artwork
                //----------------------------------------------------

                ArtworkCache.LoadCachedArtwork(game);

                ArtworkDownloaded?.Invoke(game);

                System.Diagnostics.Debug.WriteLine(
                    "Downloaded artwork for: " + game.Name);

                
            }
            finally
            {

                int completed = Interlocked.Increment(ref completedArtworkJobs);

                ArtworkProgressChanged?.Invoke(
                    completed,
                    TotalArtworkJobs);

                SplashHelper.UpdateArtworkProgress(
                    completed,
                    TotalArtworkJobs);
               
            }
        }
    }
}