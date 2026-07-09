using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Artwork
{
    internal static class SteamGridDbClient
    {
        //=====================================================
        // CHANGE THIS
        //=====================================================

        private const string ApiKey = "d8c1b2c8ddb51c98e62da5fbe559ab49";

        //=====================================================

        private const string BaseUrl =
            "https://www.steamgriddb.com/api/v2/";

        private static readonly HttpClient Client;

        static SteamGridDbClient()
        {
            Client = new HttpClient();

            Client.BaseAddress =
                new Uri(BaseUrl);

            Client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    ApiKey);

            Client.Timeout =
                TimeSpan.FromSeconds(30);
        }

        //----------------------------------------------------
        // Generic GET
        //----------------------------------------------------

        private static async Task<T> GetAsync<T>(
            string endpoint)
        {
            try
            {
                HttpResponseMessage response =
                    await Client.GetAsync(endpoint);

                if (!response.IsSuccessStatusCode)
                    return default(T);

                string json =
                    await response.Content.ReadAsStringAsync();

                return JsonConvert.DeserializeObject<T>(
                    json);
            }
            catch
            {
                return default(T);
            }
        }

        //----------------------------------------------------
        // Search by name
        //----------------------------------------------------

        public static async Task<SteamGridGame> SearchAsync(
            string gameName)
        {
            SearchResponse result =
                await GetAsync<SearchResponse>(
                    "search/autocomplete/" +
                    Uri.EscapeDataString(gameName));

            if (result == null ||
                result.Data == null ||
                result.Data.Count == 0)
            {
                return null;
            }

            string wanted =
                NormalizeName(gameName);

            SteamGridGame bestMatch = null;
            int bestScore = 0;

            foreach (SteamGridGame candidate in result.Data)
            {
                System.Diagnostics.Debug.WriteLine(
    "--------------------------------");

                System.Diagnostics.Debug.WriteLine(
                    "Raw Name: " + candidate.Name);

                System.Diagnostics.Debug.WriteLine(
                    "Normalized: " + NormalizeName(candidate.Name));

                System.Diagnostics.Debug.WriteLine(
                    "SteamGrid ID: " + candidate.Id);
                string candidateName =
                    NormalizeName(candidate.Name);

                int score =
                    CalculateScore(
                        wanted,
                        candidateName);

                System.Diagnostics.Debug.WriteLine(
                    $"Search: {gameName}");

                System.Diagnostics.Debug.WriteLine(
                    $"Candidate: {candidate.Name}");

                System.Diagnostics.Debug.WriteLine(
                    $"Score: {score}");

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMatch = candidate;
                }
            }

            if (bestScore < 85)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Rejected all matches for {gameName}");

                return null;
            }

            System.Diagnostics.Debug.WriteLine(
                $"Selected: {bestMatch.Name} ({bestScore})");

            return bestMatch;
        }

        //----------------------------------------------------
        // Steam AppID lookup
        //----------------------------------------------------

        public static async Task<SteamGridGame> LookupSteamAsync(
            int appId)
        {
            SteamLookupResponse result =
                await GetAsync<SteamLookupResponse>(
                    "games/steam/" + appId);

            return result?.Data;
        }

        //----------------------------------------------------
        // Battle.net lookup
        //----------------------------------------------------

        public static async Task<SteamGridGame> LookupBattleNetAsync(
            string productId)
        {
            SteamLookupResponse result =
                await GetAsync<SteamLookupResponse>(
                    "games/bnet/" + productId);

            return result?.Data;
        }

        //----------------------------------------------------
        // Epic lookup
        //----------------------------------------------------

        public static async Task<SteamGridGame> LookupEpicAsync(
            string catalogId)
        {
            SteamLookupResponse result =
                await GetAsync<SteamLookupResponse>(
                    "games/egs/" + catalogId);

            return result?.Data;
        }

        //----------------------------------------------------
        // Ubisoft lookup
        //----------------------------------------------------

        public static async Task<SteamGridGame> LookupUbisoftAsync(
            string id)
        {
            SteamLookupResponse result =
                await GetAsync<SteamLookupResponse>(
                    "games/uplay/" + id);

            return result?.Data;
        }

        //----------------------------------------------------
        // EA / Origin lookup
        //----------------------------------------------------

        public static async Task<SteamGridGame> LookupOriginAsync(
            string id)
        {
            SteamLookupResponse result =
                await GetAsync<SteamLookupResponse>(
                    "games/origin/" + id);

            return result?.Data;
        }
        //----------------------------------------------------
        // Get Grids (600x900)
        //----------------------------------------------------

        public static async Task<SteamGridImage> GetGridAsync(
            int steamGridId)
        {

            ImageResponse result =
                await GetAsync<ImageResponse>(
                    "grids/game/" +
                    steamGridId +
                    "?dimensions=600x900");
            System.Diagnostics.Debug.WriteLine(
    Newtonsoft.Json.JsonConvert.SerializeObject(
        result.Data[0],
        Newtonsoft.Json.Formatting.Indented));

            if (result == null ||
                result.Data == null ||
                result.Data.Count == 0)
            {
                return null;
            }

            return result.Data[0];
        }

        //----------------------------------------------------
        // Get Hero
        //----------------------------------------------------

        public static async Task<SteamGridImage> GetHeroAsync(
            int steamGridId)
        {
            ImageResponse result =
                await GetAsync<ImageResponse>(
                    "heroes/game/" +
                    steamGridId);
            System.Diagnostics.Debug.WriteLine(
    Newtonsoft.Json.JsonConvert.SerializeObject(
        result.Data[0],
        Newtonsoft.Json.Formatting.Indented));
            if (result == null ||
                result.Data == null ||
                result.Data.Count == 0)
            {
                return null;
            }

            return result.Data[0];
        }

        //----------------------------------------------------
        // Get Logo
        //----------------------------------------------------

        public static async Task<SteamGridImage> GetLogoAsync(
            int steamGridId)
        {
            ImageResponse result =
                await GetAsync<ImageResponse>(
                    "logos/game/" +
                    steamGridId);

            if (result == null ||
                result.Data == null ||
                result.Data.Count == 0)
            {
                return null;
            }

            return result.Data[0];
        }

        //----------------------------------------------------
        // Get Icon
        //----------------------------------------------------

        public static async Task<SteamGridImage> GetIconAsync(
            int steamGridId)
        {
            ImageResponse result =
                await GetAsync<ImageResponse>(
                    "icons/game/" +
                    steamGridId);

            if (result == null ||
                result.Data == null ||
                result.Data.Count == 0)
            {
                return null;
            }

            return result.Data[0];
        }

        //----------------------------------------------------
        // Download Image
        //----------------------------------------------------

        public static async Task<bool> DownloadFileAsync(
    string imageUrl,
    string destination)
        {
            try
            {
                using (HttpClient downloadClient = new HttpClient())
                {
                    byte[] data =
                        await downloadClient.GetByteArrayAsync(
                            imageUrl);

                    string folder =
                        Path.GetDirectoryName(destination);

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    File.WriteAllBytes(
                        destination,
                        data);

                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("DOWNLOAD FAILED");
                System.Diagnostics.Debug.WriteLine(imageUrl);
                System.Diagnostics.Debug.WriteLine(ex.ToString());

                return false;
            }
        }
        //----------------------------------------------------
        // Normalize Names for games that do not have ID search
        //----------------------------------------------------
        private static string NormalizeName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "";

            name = name.ToLowerInvariant();

            name = name.Replace(":", " ");
            name = name.Replace("-", " ");
            name = name.Replace("(", " ");
            name = name.Replace(")", " ");
            name = name.Replace(",", " ");

            while (name.Contains("  "))
                name = name.Replace("  ", " ");

            return name.Trim();
        }
        //----------------------------------------------------
        // Calculate Score
        //----------------------------------------------------

        private static int CalculateScore(
    string wanted,
    string candidate)
        {
            wanted = NormalizeName(wanted);
            candidate = NormalizeName(candidate);

            //----------------------------------------------------
            // Perfect match
            //----------------------------------------------------

            if (wanted == candidate)
                return 1000;

            int score = 0;

            //----------------------------------------------------
            // Compare sequel numbers
            //----------------------------------------------------

            int? wantedNumber = GetNumber(wanted);
            int? candidateNumber = GetNumber(candidate);

            if (wantedNumber.HasValue)
            {
                if (candidateNumber == wantedNumber)
                {
                    score += 200;
                }
                else if (candidateNumber.HasValue)
                {
                    score -= 300;
                }
            }
            else
            {
                // User searched for original game.
                // Penalize sequels heavily.
                if (candidateNumber.HasValue)
                {
                    score -= 250;
                }
            }

            //----------------------------------------------------
            // Compare words
            //----------------------------------------------------

            string[] wantedWords =
                wanted.Split(new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            string[] candidateWords =
                candidate.Split(new[] { ' ' },
                    StringSplitOptions.RemoveEmptyEntries);

            int matchingWords = 0;

            foreach (string word in wantedWords)
            {
                if (candidateWords.Contains(word))
                {
                    matchingWords++;
                }
            }

            score += matchingWords * 100;

            //----------------------------------------------------
            // Reward candidates that don't add lots of words
            //----------------------------------------------------

            score -=
                Math.Abs(candidateWords.Length - wantedWords.Length) * 10;

            //----------------------------------------------------
            // Bonus if candidate begins with wanted
            //----------------------------------------------------

            if (candidate.StartsWith(wanted))
            {
                score += 40;
            }

            //----------------------------------------------------
            // Bonus if wanted is contained as a whole phrase
            //----------------------------------------------------

            if (candidate.Contains(wanted))
            {
                score += 20;
            }

            return score;
        }

        private static int? GetNumber(string text)
        {
            Match match =
                Regex.Match(text, @"\b\d+\b");

            if (!match.Success)
                return null;

            return int.Parse(match.Value);
        }
    }
}