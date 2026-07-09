using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Nexus_Launcher.Models
{
    internal class EpicManifest
    {
        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("InstallLocation")]
        public string InstallLocation { get; set; }

        [JsonProperty("LaunchExecutable")]
        public string LaunchExecutable { get; set; }

        [JsonProperty("AppVersionString")]
        public string AppVersionString { get; set; }

        [JsonProperty("CatalogItemId")]
        public string CatalogItemId { get; set; }

        [JsonProperty("AppName")]
        public string AppName { get; set; }
    }
}
