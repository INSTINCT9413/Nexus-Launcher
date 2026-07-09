using DevExpress.XtraSplashScreen;
using System;
using System.Threading;
using System.Windows.Forms;

public static class SplashHelper
{
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
}