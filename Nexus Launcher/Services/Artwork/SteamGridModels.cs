using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Artwork
{
    internal class SteamGridResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }
    }

    internal class SteamGridGame
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("types")]
        public List<string> Types { get; set; }

        [JsonProperty("verified")]
        public bool Verified { get; set; }
    }

    internal class SteamGridImage
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }

        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }

        [JsonProperty("style")]
        public string Style { get; set; }

        [JsonProperty("nsfw")]
        public bool Nsfw { get; set; }

        [JsonProperty("humor")]
        public bool Humor { get; set; }

        [JsonProperty("lock")]
        public bool Locked { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("thumb")]
        public string ThumbnailUrl { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    internal class SearchResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public List<SteamGridGame> Data { get; set; }
    }

    internal class ImageResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public List<SteamGridImage> Data { get; set; }
    }

    internal class SteamLookupResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("data")]
        public SteamGridGame Data { get; set; }
    }
}