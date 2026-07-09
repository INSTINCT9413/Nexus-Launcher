using DevExpress.XtraBars.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Models
{
    internal class LauncherDefinition
    {
        public string Name { get; set; }

        public AccordionControlElement Group { get; set; }

        public Func<bool> DetectInstalled { get; set; }
    }
}
