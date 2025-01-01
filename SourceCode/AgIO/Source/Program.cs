using AgIO.Properties;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AgIO
{
    internal static class Program
    {
        private static readonly Mutex Mutex = new Mutex(true, "{8F6F0AC4-B9A1-55fd-A8CF-72F04E6BDE8F}");

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            //reset to default Vehicle and save
            Settings.Default.Reset();
            Settings.Default.Save();

            //load the profile name and set profile directory
            RegistrySettings.Load();

            Properties.Settings.Default.setConfig_profileName = RegistrySettings.profileName;
            Properties.Settings.Default.Save();

            Log.sbEvent.Append("Program Started: " + DateTime.Now.ToString("f", CultureInfo.CreateSpecificCulture(RegistrySettings.culture)) + "\r");
            Log.sbEvent.Append("AgIO Version: ");
            Log.sbEvent.Append(Application.ProductVersion.ToString(CultureInfo.InvariantCulture));
            Log.sbEvent.Append("\r");

            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(RegistrySettings.culture);
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(RegistrySettings.culture);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormLoop());
            }
        }
    }
}               

//catch (System.Configuration.ConfigurationErrorsException ex)
//{
//    // Corrupted XML! Delete the file, the user can just reload when this fails to appear. No need to worry them
//    MessageBoxButtons btns = MessageBoxButtons.OK;
//    System.Windows.Forms.MessageBox.Show("Error detected in config file - fixing it now, please close this and restart app", "Problem!", btns);
//    string filename = ((ex.InnerException as System.Configuration.ConfigurationErrorsException)?.Filename) as string;
//    System.IO.File.Delete(filename);
//    Settings.Default.Reload();
//    Application.Exit();
//}