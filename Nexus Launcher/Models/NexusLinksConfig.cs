using System.Runtime.Serialization;

namespace Nexus_Launcher.Models
{
    [DataContract]
    public class NexusLinksConfig
    {
        [DataMember(Name = "configVersion")]
        public int ConfigVersion { get; set; }

        [DataMember(Name = "steam")]
        public LauncherLinks Steam { get; set; }

        [DataMember(Name = "ubisoft")]
        public LauncherLinks Ubisoft { get; set; }

        [DataMember(Name = "ea")]
        public LauncherLinks EA { get; set; }

        [DataMember(Name = "battlenet")]
        public LauncherLinks BattleNet { get; set; }

        [DataMember(Name = "epic")]
        public LauncherLinks Epic { get; set; }

        [DataMember(Name = "gog")]
        public LauncherLinks GOG { get; set; }

        [DataMember(Name = "nexus")]
        public LauncherLinks Nexus { get; set; }
    }

    [DataContract]
    public class LauncherLinks
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "storeUrl")]
        public string StoreUrl { get; set; }

        [DataMember(Name = "saleUrl")]
        public string SaleUrl { get; set; }

        [DataMember(Name = "communityUrl")]
        public string CommunityUrl { get; set; }

        [DataMember(Name = "supportUrl")]
        public string SupportUrl { get; set; }
    }
}