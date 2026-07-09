using DevExpress.XtraPrinting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Models
{
    using System.Drawing;

    public class GameInfo
    {
        public int AppId { get; set; }

        public string Name { get; set; }
        public string GridImagePath { get; set; }

        public string HeroImagePath { get; set; }
        public int ArtworkProviderId { get; set; }
        public string ArtworkId
        {
            get;
            set;
        }
        public int SteamGridDbId
        {
            get;
            set;
        }
        public string CacheKey
        {
            get;
            set;
        }
        public bool HasArtwork { get; set; }
        public string InstallPath { get; set; }
        public string epicLauncherAppId { get; set; }
        public string ExecutablePath { get; set; }
        public string LaunchUri { get; set; }
        public string AppUserModelId { get; set; }
        public string HeaderImageUrl { get; set; }
        public string ProductId { get; set; }
        public string LogoUrl { get; set; }
        public string IconPath { get; set; }
        public string LibraryImagePath { get; set; }    
        public string HeaderImagePath { get; set; }
        public string LogoPath { get; set; }
        public Image IconImage { get; set; }
        public Image HeaderImage { get; set; }
        public bool IsInstalled { get; set; }
        public string ShortcutPath { get; set; }
        public string Launcher {  get; set; }
    }
}
