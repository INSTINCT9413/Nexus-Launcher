using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Nexus_Launcher.Helpers
{
    public class Scanner
    {
        /// <summary>
        /// Searches a directory for a specific .exe file.
        /// </summary>
        /// <param name="path">The directory path to search.</param>
        /// <param name="nameOfExe">The name of the executable (with or without the .exe extension).</param>
        /// <returns>The full path to the .exe if found; otherwise, null.</returns>
        public string GetExe(string path, string nameOfExe)
        {
            try
            {
                // Validate that the directory actually exists
                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException($"The directory '{path}' does not exist.");
                }

                // Ensure the search pattern ends with .exe
                string searchPattern = nameOfExe;
                if (!searchPattern.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    searchPattern += ".exe";
                }

                try
                {
                    // Search only the top directory. 
                    // Use SearchOption.AllDirectories instead if you want to look inside subfolders.
                    return Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories)
                                    .FirstOrDefault();
                }
                catch (UnauthorizedAccessException)
                {
                    Console.WriteLine($"Permission denied for directory: {path}");
                    return null;
                }
                catch (Exception ex)
                {
                    Program.LogCrash(ex);
                    Console.WriteLine($"An error occurred while searching for '{nameOfExe}' in '{path}': {ex.Message}");
                    return null;
                }

            }
            catch (Exception ex)
            {
                Program.LogCrash(ex);
                Console.WriteLine($"An error occurred while accessing the directory '{path}': {ex.Message}");
                return null;
            }
        }
        public string BrowseForExe(string nameOfExe)
        {
            // Ensure the title looks clean
            string cleanName = nameOfExe.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? nameOfExe
                : nameOfExe + ".exe";

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe";
                openFileDialog.Title = $"Please locate and select {cleanName}";
                openFileDialog.CheckFileExists = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }

            return null; // User canceled the dialog
        }
    }
}