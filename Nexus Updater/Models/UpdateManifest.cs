using System.Collections.Generic;

namespace NexusUpdater.Models
{
    internal class UpdateManifest
    {
        public string product { get; set; }

        public string version { get; set; }

        public string minimumVersion { get; set; }

        public string releaseDate { get; set; }

        public string sha256 { get; set; }

        public bool restart { get; set; }
        public int manifestVersion
        {
            get;
            set;
        }

        

        

        public int build
        {
            get;
            set;
        }

        
        public List<string> files { get; set; }
    }
}