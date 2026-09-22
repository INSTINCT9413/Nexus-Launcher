using Newtonsoft.Json;
using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Services
{
    internal static class UpdateDownloader
    {
        public delegate void DownloadProgressHandler(
    long downloadedBytes,
    long totalBytes);

        public static event DownloadProgressHandler DownloadProgressChanged;
        private static readonly HttpClient Client =
            new HttpClient();

        private static string UpdatesFolder
        {
            get
            {
                return Path.Combine(
                    Application.StartupPath,
                    "Updates");
            }
        }

        private static string PackagePath
        {
            get
            {
                return Path.Combine(
                    UpdatesFolder,
                    "update.pkg");
            }
        }

        private static string PendingFile
        {
            get
            {
                return Path.Combine(
                    UpdatesFolder,
                    "pending.json");
            }
        }

        public static Task<string> DownloadAsync(UpdateInfo update)
        {
            return DownloadAsync(
                update,
                CancellationToken.None);
        }

        /// <summary>
        /// Downloads the update package and returns its path.
        ///
        /// The download goes to a .part file that only replaces
        /// update.pkg once it has fully arrived. Writing straight into
        /// update.pkg, as this used to, left a truncated package behind
        /// after a dropped connection that looked like a finished one.
        /// </summary>
        public static async Task<string> DownloadAsync(
            UpdateInfo update,
            CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(UpdatesFolder);

            string destination =
                PackagePath;

            string partial =
                destination + ".part";

            string url =
                UpdaterEndpoints.Packages +
                update.package;

            try
            {
                using (HttpResponseMessage response =
                    await Client.GetAsync(
                        url,
                        HttpCompletionOption.ResponseHeadersRead,
                        cancellationToken))
                {
                    response.EnsureSuccessStatusCode();

                    long totalBytes =
                        response.Content.Headers.ContentLength ?? 0;

                    long downloadedBytes = 0;

                    using (Stream input =
                        await response.Content.ReadAsStreamAsync())
                    using (FileStream output =
                        new FileStream(
                            partial,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None))
                    {
                        byte[] buffer = new byte[81920];

                        int bytesRead;

                        while ((bytesRead =
                            await input.ReadAsync(
                                buffer,
                                0,
                                buffer.Length,
                                cancellationToken)) > 0)
                        {
                            await output.WriteAsync(
                                buffer,
                                0,
                                bytesRead,
                                cancellationToken);

                            downloadedBytes += bytesRead;

                            DownloadProgressChanged?.Invoke(
                                downloadedBytes,
                                totalBytes);
                        }
                    }

                    // A connection that closes early ends the read loop
                    // without an error, so check the size before trusting
                    // the file.
                    if (totalBytes > 0 && downloadedBytes != totalBytes)
                    {
                        throw new IOException(
                            "The download ended early (" + FormatSize(downloadedBytes) +
                            " of " + FormatSize(totalBytes) + ").");
                    }
                }

                if (File.Exists(destination))
                    File.Delete(destination);

                File.Move(partial, destination);

                return destination;
            }
            catch
            {
                // Never leave a half written file around to be mistaken
                // for the package.
                TryDelete(partial);

                throw;
            }
        }

        //--------------------------------------------------------------
        // Downloaded but not installed
        //--------------------------------------------------------------

        private class PendingUpdate
        {
            public int build { get; set; }

            public string version { get; set; }

            public string package { get; set; }
        }

        /// <summary>
        /// Remembers a finished download the user chose to install later,
        /// so the next check can offer it without downloading again.
        /// </summary>
        public static void SavePending(
            UpdateInfo update,
            string packagePath)
        {
            try
            {
                File.WriteAllText(
                    PendingFile,
                    JsonConvert.SerializeObject(
                        new PendingUpdate
                        {
                            build = update.build,
                            version = update.version,
                            package = packagePath
                        },
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
            }
        }

        /// <summary>
        /// The package already downloaded for this build, or null if
        /// there is not one or it has gone missing.
        /// </summary>
        public static string GetPendingPackage(
            UpdateInfo update)
        {
            try
            {
                if (update == null || !File.Exists(PendingFile))
                    return null;

                PendingUpdate pending =
                    JsonConvert.DeserializeObject<PendingUpdate>(
                        File.ReadAllText(PendingFile));

                if (pending == null ||
                    pending.build != update.build ||
                    string.IsNullOrWhiteSpace(pending.package) ||
                    !File.Exists(pending.package))
                {
                    return null;
                }

                return pending.package;
            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);

                return null;
            }
        }

        public static void ClearPending()
        {
            TryDelete(PendingFile);
        }

        private static void TryDelete(
            string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
            }
        }

        public static string FormatSize(long bytes)
        {
            string[] suffix =
            {
        "B",
        "KB",
        "MB",
        "GB"
    };

            double size = bytes;

            int unit = 0;

            while (size >= 1024 &&
                   unit < suffix.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            return size.ToString("0.0") + " " + suffix[unit];
        }
    }
}
