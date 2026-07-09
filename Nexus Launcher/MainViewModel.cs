using DevExpress.Mvvm.DataAnnotations;
using QlmControls.v10;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Nexus_Launcher
{
    [POCOViewModel()]
    public class MainViewModel
    {
            public virtual string Title { get; set; } = "Nexus Launcher";
            
    }
}
