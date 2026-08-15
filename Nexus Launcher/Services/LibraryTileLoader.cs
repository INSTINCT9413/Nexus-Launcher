using DevExpress.XtraEditors;
using DevExpress.XtraEditors.TableLayout;

using Nexus_Launcher.Models;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;

namespace Nexus_Launcher.Services.Library
{
    internal static class LibraryTileLoader
    {
        public static void Populate(
            TileControl tileControl)
        {
            tileControl.Groups.Clear();

            TileGroup group =
                new TileGroup();

            tileControl.Groups.Add(group);

            foreach (GameInfo game in LibraryService.Games)
            {
                TileItem tile =
                    CreateTile(game);
                tile.ItemSize = TileItemSize.Wide;
                group.Items.Add(tile);
            }
        }

        private static TileItem CreateTile(
    GameInfo game)
        {
            TileItem tile =
                new TileItem();

            tile.Tag = game;

            tile.ItemSize =
                TileItemSize.Wide;

            //----------------------------------------------------
            // Artwork
            //----------------------------------------------------

            if (File.Exists(game.GridImagePath))
            {
                tile.BackgroundImage =
    LoadUnlockedImage(game.GridImagePath);

                tile.BackgroundImageScaleMode =
                    TileItemImageScaleMode.ZoomInside;
            }

            //----------------------------------------------------
            // Tile Appearance
            //----------------------------------------------------

            tile.AppearanceItem.Normal.BorderColor =
                Color.FromArgb(40, 40, 40);

            tile.AppearanceItem.Normal.Options.UseBorderColor =
                true;

            //----------------------------------------------------
            // Game Title
            //----------------------------------------------------

            TileItemElement title =
                new TileItemElement();

            title.Text =
                game.Name;

            title.TextAlignment =
                TileItemContentAlignment.BottomLeft;

            title.Appearance.Normal.Font =
                new Font(
                    "Segoe UI",
                    11F,
                    FontStyle.Bold);

            title.Appearance.Normal.ForeColor =
                Color.White;

            //// Background behind text
            //title.Appearance.Normal.BackColor =
            //    Color.FromArgb(
            //        220,
            //        25,
            //        25,
            //        25);

            //title.Appearance.Normal.Options.UseBackColor =
            //    true;

            title.Appearance.Normal.Options.UseForeColor =
                true;

            title.Appearance.Normal.Options.UseFont =
                true;
            

            tile.Elements.Add(title);

            //----------------------------------------------------
            // Launcher Name
            //----------------------------------------------------

            TileItemElement launcher =
                new TileItemElement();

            launcher.Text =
                game.Launcher;

            launcher.TextAlignment =
                TileItemContentAlignment.BottomRight;

            launcher.Appearance.Normal.Font =
                new Font(
                    "Segoe UI",
                    6F);

            launcher.Appearance.Normal.ForeColor =
                Color.Silver;

            launcher.Appearance.Normal.BackColor =
                Color.FromArgb(
                    220,
                    25,
                    25,
                    25);

            launcher.Appearance.Normal.Options.UseBackColor =
                true;

            launcher.Appearance.Normal.Options.UseForeColor =
                true;

            launcher.Appearance.Normal.Options.UseFont =
                true;

            launcher.TextLocation =
                new Point(0, 18);

            //tile.Elements.Add(launcher);
            tile.BackgroundImageScaleMode =
    TileItemImageScaleMode.ZoomOutside;
            return tile;
        }
        private static Image LoadUnlockedImage(string path)
        {
            using (Bitmap bmp = new Bitmap(path))
            {
                return new Bitmap(bmp);
            }
        }
    }
}