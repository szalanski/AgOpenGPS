using AgIO.Properties;
using Microsoft.Win32;
using System;
using System.Configuration;
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
            //opening the subkey
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AgIO");

            ////create default keys if not existing
            if (regKey == null)
            {
                RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgIO");

                //storing the values
                Key.SetValue("Language", "en");
                Key.SetValue("ProfileName", "Default Profile");
                Key.Close();
            }

            //Profile File Name from Registry Key
            if (regKey.GetValue("ProfileName") == null)
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgIO");
                key.SetValue("ProfileName", "Default Profile");
                key.Close();
            }

            string profileName = regKey.GetValue("ProfileName").ToString();
            regKey.Close();

            //get the Documents directory, if not exist, create
            string profileDirectory = 
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AgOpenGPS", "AgIO");

            if (!string.IsNullOrEmpty(profileDirectory) && !Directory.Exists(profileDirectory))
            {
                Directory.CreateDirectory(profileDirectory);
            }

            //reset to default Vehicle and save
            Settings.Default.Reset();
            Settings.Default.Save();

            //what's in the vehicle directory
            DirectoryInfo dinfo = new DirectoryInfo(profileDirectory);
            FileInfo[] vehicleFiles = dinfo.GetFiles("*.xml");

            bool isProfileExist = false;

            foreach (FileInfo file in vehicleFiles)
            {
                string temp = Path.GetFileNameWithoutExtension(file.Name).Trim();

                if (temp == profileName)
                {
                    isProfileExist = true;
                }
            }

            //does current vehicle exist?
            if (isProfileExist && profileName != "Default Profile")
            {
                SettingsIO.ImportSettings(Path.Combine(profileDirectory, profileName + ".XML"));
            }
            else
            {
                if (profileName == "Default Profile")
                    Log.EventWriter("Default Profile does not exist in Program.cs");
                else
                    Log.EventWriter(profileName + ".XML Profile does not exist. Called in Program.cs");
            }

            Properties.Settings.Default.setConfig_profileName = profileName;
            Properties.Settings.Default.Save();


            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
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