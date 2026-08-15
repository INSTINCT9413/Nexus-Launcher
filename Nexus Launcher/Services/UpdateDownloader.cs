using Nexus_Launcher.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        public static async Task<string> DownloadAsync(UpdateInfo update)
        {
            string updatesFolder =
                Path.Combine(
                    Application.StartupPath,
                    "Updates");

            Directory.CreateDirectory(updatesFolder);

            string destination =
                Path.Combine(
                    updatesFolder,
                    "update.pkg");

            string url =
                UpdaterEndpoints.Packages +
                update.package;

            //MessageBox.Show(url);
            using (HttpResponseMessage response =
                await Client.GetAsync(
                    url,
                    HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                using (Stream input =
                    await response.Content.ReadAsStreamAsync())
                using (FileStream output =
                    new FileStream(
                        destination,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None))
                {
                    byte[] buffer = new byte[81920];

                    long totalBytes =
                        response.Content.Headers.ContentLength ?? 0;

                    long downloadedBytes = 0;

                    int bytesRead;

                    while ((bytesRead =
                        await input.ReadAsync(
                            buffer,
                            0,
                            buffer.Length)) > 0)
                    {
                        await output.WriteAsync(
                            buffer,
                            0,
                            bytesRead);

                        downloadedBytes += bytesRead;

                        DownloadProgressChanged?.Invoke(
                            downloadedBytes,
                            totalBytes);
                    }
                }
            }

            return destination;
        }
        private static string FormatSize(long bytes)
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
