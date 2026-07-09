using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nexus_Launcher.Helpers
{
    internal class IconHelper
    {
        public static Image ExtractExeIcon(
        string exePath)
        {
            try
            {
                if (!File.Exists(exePath))
                    return null;

                Icon icon =
                    Icon.ExtractAssociatedIcon(
                        exePath);

                if (icon == null)
                    return null;

                return icon.ToBitmap();
            }
            catch
            {
                return null;
            }
        }
    }
}
