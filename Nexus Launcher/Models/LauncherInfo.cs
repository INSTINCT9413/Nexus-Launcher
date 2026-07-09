using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Models
{
    public class LauncherInfo
    {
        public string Name { get; set; }
        public string InstallPath { get; set; }
        public string ExecutablePath { get; set; }

        public List<GameInfo> Games { get; set; }

        public LauncherInfo()
        {
            Games = new List<GameInfo>();
        }
        
    }
}
