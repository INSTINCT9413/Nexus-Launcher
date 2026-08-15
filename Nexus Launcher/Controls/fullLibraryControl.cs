using DevExpress.Mvvm.POCO;
using DevExpress.XtraEditors;
using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
using Nexus_Launcher.Services;
using Nexus_Launcher.Services.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nexus_Launcher.Controls
{
    public partial class fullLibraryControl : DevExpress.XtraEditors.XtraUserControl
    {
        private DevExpress.XtraEditors.TileItem hoveredTile;

        private Point targetLocation;

        private bool hoverVisible;
        public fullLibraryControl()
        {
            InitializeComponent();
            LibrarySelectionService.SelectedGameChanged +=
        LibrarySelectionService_SelectedGameChanged;

            tileControl1.ItemClick +=
                tileControl1_Click;
        }

        private void fullLibraryControl_Load(object sender, EventArgs e)
        {
            splitContainerControl1.SplitterPosition = 600;
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);

        }
        private void LibrarySelectionService_SelectedGameChanged(
    GameInfo game)
        {
            libraryDetailControl1.SetGame(game);
        }
        public void PopulateLibrary()
        {
            LibraryTileLoader.Populate(tileControl1);
            labelControl1.Text = $"Full Library - Total Games: {tileControl1.Groups.Sum(g => g.Items.Count)}";
        }
        private void tileControl1_Click(object sender, DevExpress.XtraEditors.TileItemEventArgs e)
        {
            GameInfo game =
        e.Item.Tag as GameInfo;
            if (File.Exists(game.HeroImagePath))
            {
                libraryDetailControl1.BackgroundImage = Image.FromFile(game.HeroImagePath);
            }
            LibrarySelectionService.Select(game);
            splitContainerControl1.PanelVisibility = DevExpress.XtraEditors.SplitPanelVisibility.Both;
        }

        

        private void simpleButton1_Click(object sender, EventArgs e)
        {
            tileControl1.Groups.Clear();
            PopulateLibrary();
        }

        private void splitContainerControl1_SplitterPositionChanged(object sender, EventArgs e)
        {
            //labelControl1.Text = $"Splitter Position: {splitContainerControl1.SplitterPosition}";
        }
    }
}
