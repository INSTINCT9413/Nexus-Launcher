using Nexus_Launcher;
using Nexus_Launcher.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

internal static class GameLauncherService
{
    public static void Launch(GameInfo game)
    {
        try
        {
            if (game == null)
                return;
            System.Diagnostics.Debug.WriteLine(
    $"Launcher = '{game.Launcher}'");
            switch (game.Launcher)
            {
                case "Steam":
                    LaunchSteam(game);
                    break;

                case "Epic Games":
                    LaunchEpic(game);
                    break;

                case "Battlenet":
                    LaunchBattleNet(game);
                    break;

                case "GOG":
                    LaunchGOG(game);
                    break;

                case "EA":
                    LaunchEA(game);
                    break;

                case "Ubisoft":
                    LaunchUbisoft(game);
                    break;

                case "Nexus Launcher":
                    LaunchExecutable(game);
                    break;
            }
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
            MessageBox.Show(ex.ToString());
        }

    }
    public static void LaunchSteam(GameInfo game)
    {
       
            try
            {
                string exePathSteam = MainView.sysDisk + @"Program Files (x86)\Steam\Steam.exe";
                bool isSteamRunning = Process.GetProcessesByName("Steam").Any();
                if (!isSteamRunning)
                {
                    // Start Steam normally first so it can initialize its background hooks
                    Process.Start(new ProcessStartInfo { FileName = exePathSteam, UseShellExecute = true });

                    // Give it 3 to 5 seconds to load up before sending the game instruction
                    Thread.Sleep(4000);
                }
                Process.Start(
                "steam://rungameid/" + game.AppId);
            }
            catch (Exception ex)
            {
            Program.LogCrash(ex);
            MessageBox.Show(
                    "Failed to launch the application: " + ex.Message);
            }

        }
        
    public static void LaunchEpic(GameInfo game)
    {
            Process.Start(new ProcessStartInfo
            {
                FileName =
       "com.epicgames.launcher://apps/" +
       game.epicLauncherAppId +
       "?action=launch&silent=true",
                UseShellExecute = true
            });
        }
        
    
    public static void LaunchBattleNet(GameInfo game)
    {
            try
            {

                string exePath = MainView.sysDisk + @"Program Files (x86)\Battle.net\Battle.net.exe";
                string launchParams = "-nostreamline -sso -launch -uid";
                string diabloIVParams = "-launch";
                // 1. Force your product ID to uppercase if required by Blizzard's system
                string cleanProductID = game.ProductId.ToUpper();

                // 2. Check if Battle.net is already running in the background
                bool isBnetRunning = Process.GetProcessesByName("Battle.net").Any();

                if (!isBnetRunning)
                {
                    // Start Battle.net normally first so it can initialize its background hooks
                    Process.Start(new ProcessStartInfo { FileName = exePath, UseShellExecute = true });

                    // Give it 3 to 5 seconds to load up before sending the game instruction
                    Thread.Sleep(4000);
                }

                // 3. Send the execution command (Now it will successfully trigger the game!)
                //Process.Start(new ProcessStartInfo
                //{
                //    FileName = exePath,
                //    Arguments = $@"--exec=""launch {cleanProductID}""",
                //    UseShellExecute = true
                //});
                if (game.ProductId == "fenris")
                {
                    // Explicitly launch DiabloIV with Battlenet process, passing argument
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,

                        // This forces Windows to read the correct 'battlenet://' URI association
                        Arguments = "--exec=\"launch Fen\"",
                        UseShellExecute = true,
                        Verb = "runas"
                    });

                }
                else
                {

                    Process.Start(new ProcessStartInfo { FileName = game.ExecutablePath, Arguments = launchParams + " " + game.ProductId, UseShellExecute = true });
                }

            //MessageBox.Show(_executablePath + _productID);
            
        }
            catch (Exception ex)
            {
            Program.LogCrash(ex);
            MessageBox.Show(
                    "Failed to launch the application: " + ex.Message);
            }
        }
    
    public static void LaunchGOG(GameInfo game)
    {
        
            try
            {

                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            game.ShortcutPath,

                        UseShellExecute =
                            true
                    });



            }
            catch (Exception ex)
            {
            Program.LogCrash(ex);
            MessageBox.Show(
                    "Failed to launch the application: " + ex.Message);
            }
        }

    public static void LaunchEA(GameInfo game)
    {
        try
        {
            //labelControl1.Text = _EAShortuct;
            if (!string.IsNullOrWhiteSpace(
game.ExecutablePath) &&
File.Exists(
game.ExecutablePath))
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                            game.ExecutablePath,
                        UseShellExecute =
                            true
                    });

                return;
            }
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
        }
    }
    public static void LaunchUbisoft(GameInfo game)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(
game.LaunchUri))
            {
                Process.Start(
                    new ProcessStartInfo
                    {
                        FileName =
                           game.LaunchUri,

                        UseShellExecute =
                            true
                    });

                return;
            }
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
        }
    }
    public static void LaunchExecutable(GameInfo game)
    {
        if (string.IsNullOrEmpty(game.ExecutablePath))
            return;
        try
        {
            System.Diagnostics.Process.Start(game.ExecutablePath);
        }
        catch (Exception ex)
        {
            Program.LogCrash(ex);
            // Handle exceptions, e.g., log the error or show a message to the user
            Console.WriteLine($"Failed to launch {game.Name}: {ex.Message}");
        }
    }
}