using ImageMagick;
using Nexus_Launcher.Models;
using System;
using System.IO;
using System.Net;

namespace Nexus_Launcher.Helpers
{
    internal static class BattleNetArtworkHelper
    {
        private static string CacheFolder =>
            Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Cache",
                "BattleNet");

        public static void DownloadArtwork(
            GameInfo game)
        {
            try
            {
                Directory.CreateDirectory(
                    CacheFolder);

                DownloadHeader(game);
                DownloadLogo(game);
            }
            catch
            {
                // Ignore artwork failures
            }
        }

        private static void DownloadHeader(
            GameInfo game)
        {
            if (string.IsNullOrWhiteSpace(
                game.HeaderImageUrl))
            {
                return;
            }

            string pngFile =
                Path.Combine(
                    CacheFolder,
                    game.ProductId +
                    "_header.png");

            if (!File.Exists(pngFile))
            {
                DownloadAndConvert(
                    game.HeaderImageUrl,
                    pngFile);
            }

            if (File.Exists(pngFile))
            {
                game.HeaderImagePath =
                    pngFile;
            }
        }

        private static void DownloadLogo(
            GameInfo game)
        {
            if (string.IsNullOrWhiteSpace(
                game.LogoUrl))
            {
                return;
            }

            string pngFile =
                Path.Combine(
                    CacheFolder,
                    game.ProductId +
                    "_logo.png");

            if (!File.Exists(pngFile))
            {
                DownloadAndConvert(
                    game.LogoUrl,
                    pngFile);
            }

            if (File.Exists(pngFile))
            {
                game.LogoPath =
                    pngFile;
            }
        }

        private static void DownloadAndConvert(
            string url,
            string pngOutput)
        {
            try
            {
                string tempWebp =
                    Path.ChangeExtension(
                        pngOutput,
                        ".webp");

                using (WebClient wc =
                    new WebClient())
                {
                    wc.DownloadFile(
                        url,
                        tempWebp);
                }

                using (MagickImage image =
                    new MagickImage(
                        tempWebp))
                {
                    image.Write(
                        pngOutput,
                        MagickFormat.Png);
                }

                if (File.Exists(
                    tempWebp))
                {
                    File.Delete(
                        tempWebp);
                }
            }
            catch
            {
                if (File.Exists(
                    pngOutput))
                {
                    File.Delete(
                        pngOutput);
                }
            }
        }
    }
}