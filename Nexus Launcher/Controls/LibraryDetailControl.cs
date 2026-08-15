using Nexus_Launcher.Helpers;
using Nexus_Launcher.Models;
using Nexus_Launcher.Properties;
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
    public partial class LibraryDetailControl : DevExpress.XtraEditors.XtraUserControl
    {
        public LibraryDetailControl()
        {
            InitializeComponent();
        }

        private void LibraryHoverCard_Load(object sender, EventArgs e)
        {
            FontManager.ApplyFont(
    this,
    Settings.Default.UIFont);
        }
        public void SetGame(GameInfo game)
        {
            if (game == null)
                return;

            lblTitle.Text =
                game.Name;

            lblLauncher.Text =
                game.Launcher;

            btnPlay.Tag =
                game;

            btnFavorite.Tag =
                game;

            btnStore.Tag =
                game;

            ////-------------------------------------------------
            //// Hero Artwork
            ////-------------------------------------------------

            //if (File.Exists(game.HeroImagePath))
            //{
            //    pictureHero.Image =
            //        Image.FromFile(game.HeroImagePath);
            //}
            //else
            //{
            //    pictureHero.Image = null;
            //}

            ////-------------------------------------------------
            //// Logo
            ////-------------------------------------------------

            //if (File.Exists(game.LogoPath))
            //{
            //    pictureLogo.Image =
            //        Image.FromFile(game.LogoPath);
            //}
            //else
            //{
            //    pictureLogo.Image = null;
            //}
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (btnPlay.Tag is GameInfo game)
            {
                GameLauncherService.Launch(game);
            }
        }
    }
}
