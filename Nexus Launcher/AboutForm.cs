using Nexus_Launcher.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher
{
    public partial class AboutForm : DevExpress.XtraEditors.XtraForm
    {
       
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            
            
            
            //Thread.Sleep(1000);
            Getdll();
            
        }

        
        private async void Getdll()
                    {
            
        }
        private void tabNavigationPage1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}