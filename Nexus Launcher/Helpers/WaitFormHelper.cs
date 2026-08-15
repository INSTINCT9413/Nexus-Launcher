using DevExpress.XtraSplashScreen;
using System;
using System.Threading;
using System.Windows.Forms;
using static Nexus_Launcher.WaitForm1;

public static class SplashHelper
{
    public class ArtworkProgressInfo
    {
        public int Completed { get; set; }

        public int Total { get; set; }
    }
    
    public static void UpdateStatus(string text)
    {
        try
        {
            if (!IsSplashOpen())
                return;

            SplashScreenManager.Default.SetWaitFormCaption(
                "Loading Nexus Launcher...");

            if (Application.MessageLoop &&
                SynchronizationContext.Current != null)
            {
                SynchronizationContext.Current.Post(_ =>
                {
                    try
                    {
                        if (!IsSplashOpen())
                            return;

                        SplashScreenManager.Default.SetWaitFormDescription(text);
                    }
                    catch
                    {
                        // Splash closed while updating.
                    }

                }, null);
            }
            else
            {
                SplashScreenManager.Default.SetWaitFormDescription(text);
            }
        }
        catch
        {
            // Splash already closed.
        }
    }
    public static bool IsSplashOpen()
    {
        return
            SplashScreenManager.Default != null &&
            SplashScreenManager.Default.IsSplashFormVisible;
    }
    public static void UpdateArtworkProgress(
    int completed,
    int total)
    {
        if (SplashScreenManager.Default == null)
            return;

        if (total <= 0)
            total = 1;

        try
        {
            SplashScreenManager.Default.SendCommand(
                WaitFormCommand.UpdateArtworkProgress,
                new ArtworkProgressInfo
                {
                    Completed = completed,
                    Total = total
                });
        }
        catch
        {
            // Wait form already closed
        }

    }
}