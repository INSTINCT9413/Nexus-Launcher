using Nexus.Plugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Nexus.Plugin.SaveBackup
{
    //==================================================================
    // Backs a game's saves up into a dated zip.
    //
    // Nexus does not know where any game keeps its saves, and there
    // is no reliable way to find out, so this does two things: it
    // guesses the usual places, and it lets the user say where when
    // the guess is wrong. What it is told is remembered, so a game
    // only has to be sorted out once.
    //==================================================================

    [NexusPlugin(
        "nexus.savebackup",
        "Save Backup",
        Author = "Nexus Launcher",
        Version = "1.0.0",
        Description = "Zips a game's save folder, on demand or when " +
                      "you finish playing.")]
    public class SaveBackupPlugin : INexusPlugin
    {
        private INexusHost host;
        private SaveLocations locations;

        public void Initialize(
            INexusHost host)
        {
            this.host = host;

            locations = new SaveLocations(host);

            host.AddGameAction(new BackupNowAction(host, locations));
            host.AddGameAction(new SetSaveFolderAction(host, locations));
            host.AddGameAction(new OpenBackupsAction(host));

            // Back up when a game is finished with, not while it is
            // running: copying a save file mid-write is how you get a
            // backup of a corrupt save.
            host.GameExited += (s, e) => BackupQuietly(e.Game);
        }

        public void Shutdown()
        {
        }

        private void BackupQuietly(
            GameRef game)
        {
            try
            {
                // Only games the user has already pointed at a folder.
                // Guessing silently in the background would fill the
                // disk with backups of the wrong thing.
                string folder = locations.Known(game);

                if (folder == null)
                    return;

                string file = Backup.Run(host, game, folder);

                host.Log.Info("backed up " + game.Name + " to " + file);
            }
            catch (Exception ex)
            {
                host.Log.Error("automatic backup of " + game.Name +
                    " failed", ex);
            }
        }
    }

    //------------------------------------------------------------------
    // Where saves live
    //------------------------------------------------------------------

    /// <summary>
    /// Remembers a save folder per game, and guesses when it has not
    /// been told.
    /// </summary>
    internal class SaveLocations
    {
        private readonly INexusHost host;
        private readonly string file;
        private readonly Dictionary<string, string> known =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public SaveLocations(
            INexusHost host)
        {
            this.host = host;

            file = Path.Combine(host.DataFolder, "locations.txt");

            Load();
        }

        /// <summary>
        /// A plain "name=path" file rather than a serialised blob, so
        /// a user can open it and fix it by hand.
        /// </summary>
        private void Load()
        {
            try
            {
                if (!File.Exists(file))
                    return;

                foreach (string line in File.ReadAllLines(file))
                {
                    int split = line.IndexOf('=');

                    if (split <= 0)
                        continue;

                    known[line.Substring(0, split).Trim()] =
                        line.Substring(split + 1).Trim();
                }
            }
            catch (Exception ex)
            {
                host.Log.Error("could not read saved locations", ex);
            }
        }

        private void Save()
        {
            try
            {
                File.WriteAllLines(file,
                    known.Select(p => p.Key + "=" + p.Value).ToArray(),
                    Encoding.UTF8);
            }
            catch (Exception ex)
            {
                host.Log.Error("could not write saved locations", ex);
            }
        }

        public string Known(
            GameRef game)
        {
            string path;

            if (game != null &&
                known.TryGetValue(game.Name ?? string.Empty, out path) &&
                Directory.Exists(path))
            {
                return path;
            }

            return null;
        }

        public void Remember(
            GameRef game,
            string folder)
        {
            if (game == null || string.IsNullOrWhiteSpace(game.Name))
                return;

            known[game.Name] = folder;

            Save();
        }

        /// <summary>
        /// The places games usually keep saves, checked for a folder
        /// named after the game. A guess, offered for confirmation,
        /// never acted on by itself.
        /// </summary>
        public string Guess(
            GameRef game)
        {
            if (game == null || string.IsNullOrWhiteSpace(game.Name))
                return null;

            string documents = Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments);

            string roaming = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

            string local = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

            string profile = Environment.GetFolderPath(
                Environment.SpecialFolder.UserProfile);

            List<string> candidates = new List<string>
            {
                Path.Combine(documents, "My Games", game.Name),
                Path.Combine(documents, game.Name),
                Path.Combine(profile, "Saved Games", game.Name),
                Path.Combine(roaming, game.Name),
                Path.Combine(local, game.Name)
            };

            if (!string.IsNullOrEmpty(game.InstallPath))
            {
                candidates.Add(Path.Combine(game.InstallPath, "Saves"));
                candidates.Add(Path.Combine(game.InstallPath, "save"));
                candidates.Add(Path.Combine(game.InstallPath, "savegames"));
            }

            return candidates.FirstOrDefault(Directory.Exists);
        }
    }

    //------------------------------------------------------------------
    // The backup itself
    //------------------------------------------------------------------

    internal static class Backup
    {
        public static string Folder(
            INexusHost host)
        {
            string path = Path.Combine(host.DataFolder, "Backups");

            Directory.CreateDirectory(path);

            return path;
        }

        /// <summary>
        /// Zips a folder into a dated file and returns its path.
        ///
        /// Written to a .part first and renamed, so an interrupted
        /// backup never leaves behind a zip that looks finished.
        /// </summary>
        public static string Run(
            INexusHost host,
            GameRef game,
            string saveFolder)
        {
            string safe = game.Name;

            foreach (char bad in Path.GetInvalidFileNameChars())
                safe = safe.Replace(bad, '_');

            string target = Path.Combine(
                Folder(host),
                safe + "-" +
                DateTime.Now.ToString("yyyy-MM-dd-HHmmss") + ".zip");

            string partial = target + ".part";

            try
            {
                ZipFile.CreateFromDirectory(saveFolder, partial);

                if (File.Exists(target))
                    File.Delete(target);

                File.Move(partial, target);
            }
            catch
            {
                try
                {
                    if (File.Exists(partial))
                        File.Delete(partial);
                }
                catch
                {
                }

                throw;
            }

            Prune(Folder(host), safe);

            return target;
        }

        /// <summary>
        /// Keeps the ten most recent backups of a game. Without this
        /// an automatic backup on every exit would grow without
        /// limit, which is the sort of thing a user only notices when
        /// the disk is full.
        /// </summary>
        private static void Prune(
            string folder,
            string safeName)
        {
            try
            {
                FileInfo[] mine = new DirectoryInfo(folder)
                    .GetFiles(safeName + "-*.zip")
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .ToArray();

                for (int i = 10; i < mine.Length; i++)
                    mine[i].Delete();
            }
            catch
            {
            }
        }
    }

    //------------------------------------------------------------------
    // Menu entries
    //------------------------------------------------------------------

    internal class BackupNowAction : IGameAction
    {
        private readonly INexusHost host;
        private readonly SaveLocations locations;

        public BackupNowAction(
            INexusHost host,
            SaveLocations locations)
        {
            this.host = host;
            this.locations = locations;
        }

        public string Id
        {
            get { return "savebackup.now"; }
        }

        public string Caption
        {
            get { return "Back up saves"; }
        }

        public bool AppliesTo(
            GameRef game)
        {
            return game != null;
        }

        public void Invoke(
            GameRef game)
        {
            string folder =
                locations.Known(game) ?? locations.Guess(game);

            if (folder == null)
            {
                System.Windows.Forms.MessageBox.Show(
                    "Nexus could not work out where " + game.Name +
                    " keeps its saves." + Environment.NewLine +
                    Environment.NewLine +
                    "Use \"Set save folder\" on the same menu to point " +
                    "it at the right place, and it will be remembered.",
                    "Save Backup",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);

                return;
            }

            // A guess is confirmed before anything is written, so a
            // wrong guess costs a click rather than a junk backup.
            if (locations.Known(game) == null)
            {
                var answer = System.Windows.Forms.MessageBox.Show(
                    "Back up this folder as " + game.Name + "'s saves?" +
                    Environment.NewLine + Environment.NewLine + folder,
                    "Save Backup",
                    System.Windows.Forms.MessageBoxButtons.YesNo,
                    System.Windows.Forms.MessageBoxIcon.Question);

                if (answer != System.Windows.Forms.DialogResult.Yes)
                    return;

                locations.Remember(game, folder);
            }

            try
            {
                string file = Backup.Run(host, game, folder);

                Process.Start(new ProcessStartInfo("explorer.exe",
                    "/select,\"" + file + "\"")
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                host.Log.Error("backup failed", ex);

                System.Windows.Forms.MessageBox.Show(
                    "The backup failed." + Environment.NewLine +
                    Environment.NewLine + ex.Message,
                    "Save Backup",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Warning);
            }
        }
    }

    internal class SetSaveFolderAction : IGameAction
    {
        private readonly INexusHost host;
        private readonly SaveLocations locations;

        public SetSaveFolderAction(
            INexusHost host,
            SaveLocations locations)
        {
            this.host = host;
            this.locations = locations;
        }

        public string Id
        {
            get { return "savebackup.setfolder"; }
        }

        public string Caption
        {
            get { return "Set save folder"; }
        }

        public bool AppliesTo(
            GameRef game)
        {
            return game != null;
        }

        public void Invoke(
            GameRef game)
        {
            using (var picker =
                new System.Windows.Forms.FolderBrowserDialog())
            {
                picker.Description =
                    "Where does " + game.Name + " keep its saves?";

                string start =
                    locations.Known(game) ?? locations.Guess(game);

                if (start != null)
                    picker.SelectedPath = start;

                if (picker.ShowDialog() !=
                    System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }

                locations.Remember(game, picker.SelectedPath);

                host.Log.Info("save folder for " + game.Name +
                    " set to " + picker.SelectedPath);
            }
        }
    }

    internal class OpenBackupsAction : IGameAction
    {
        private readonly INexusHost host;

        public OpenBackupsAction(
            INexusHost host)
        {
            this.host = host;
        }

        public string Id
        {
            get { return "savebackup.open"; }
        }

        public string Caption
        {
            get { return "Open save backups"; }
        }

        public bool AppliesTo(
            GameRef game)
        {
            return true;
        }

        public void Invoke(
            GameRef game)
        {
            Process.Start(new ProcessStartInfo(Backup.Folder(host))
            {
                UseShellExecute = true
            });
        }
    }
}
