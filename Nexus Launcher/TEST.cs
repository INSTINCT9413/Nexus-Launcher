using Nexus_Launcher.Helpers;
using Nexus_Launcher.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher
{
    public partial class TEST : DevExpress.XtraEditors.DirectXForm
    {
        public TEST()
        {
            InitializeComponent();
            
        }

        private void TEST_Load(object sender, EventArgs e)
        {
            

            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
        }

        void widgetView1_QueryControl(object sender, DevExpress.XtraBars.Docking2010.Views.QueryControlEventArgs e)
        {
            // Try to determine document identity from ControlTypeName, actual Control type, or Caption
            string docId = e.Document.ControlTypeName
                ?? e.Document.Control?.GetType().FullName
                ?? e.Document.Caption
                ?? string.Empty;

            if (docId.EndsWith("GOGSettings", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.GOGSettings();
            else if (docId.EndsWith("NexusStore", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.NexusStore();
            else if (docId.EndsWith("fullLibraryControl", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.fullLibraryControl();
            else if (docId.EndsWith("LibraryDetailControl", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.LibraryDetailControl();
            else if (docId.EndsWith("ApplicationCard", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.ApplicationCard();
            else if (docId.EndsWith("UserAccount", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Services.UserAccount();
            else if (docId.EndsWith("EASettings", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.EASettings();
            else if (docId.EndsWith("MyRigControl", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.MyRigControl();
            else if (docId.EndsWith("addRemoveForm", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Forms.addRemoveForm();
            else if (docId.EndsWith("LauncherCard", StringComparison.OrdinalIgnoreCase))
                e.Control = new Nexus_Launcher.Controls.LauncherCard();

            if (e.Control == null)
                e.Control = new System.Windows.Forms.Control();
        }
    }
}
