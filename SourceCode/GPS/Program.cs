using AgOpenGPS.Properties;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AgOpenGPS
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static readonly Mutex Mutex = new Mutex(true, "{516-0AC5-B9A1-55fd-A8CE-72F04E6BDE8F}");

        [STAThread]
        private static void Main()
        {
            string configPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            if (!File.Exists(configPath))
            {
                //Existing user config does not exist, so load settings from previous assembly
                Settings.Default.Upgrade();
                Settings.Default.Reload();
                Settings.Default.Save();
            }

            string workingDirectory = Settings.Default.setF_workingDirectory == "Default"
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : Settings.Default.setF_workingDirectory;

            string baseDirectory = Path.Combine(workingDirectory, "AgOpenGPS");

            //get the fields directory, if not exist, create
            string vehiclesDirectory = Path.Combine(baseDirectory, "Vehicles");
            if (!string.IsNullOrEmpty(vehiclesDirectory) && !Directory.Exists(vehiclesDirectory))
            {
                Directory.CreateDirectory(vehiclesDirectory);

                //be sure at default settings
                Settings.Default.Reset();
                Settings.Default.Save();
            }

            //keep for later to load settings
            string vehicleFileName = Settings.Default.setVehicle_vehicleName;

            //reset to default Vehicle and save
            Settings.Default.Reset();
            Settings.Default.Save();

            //what's in the vehicle directory
            DirectoryInfo dinfo = new DirectoryInfo(vehiclesDirectory);
            FileInfo[] vehicleFiles = dinfo.GetFiles("*.xml");

            bool isVehicleExist = false;

            foreach (FileInfo file in vehicleFiles)
            {
                string temp = Path.GetFileNameWithoutExtension(file.Name).Trim();

                if (temp == vehicleFileName)
                {
                    isVehicleExist = true;
                }
            }

            //does current vehicle exist?
            if (isVehicleExist && vehicleFileName != "Default Vehicle")
            {
                SettingsIO.ImportAll(Path.Combine(vehiclesDirectory, vehicleFileName + ".XML"));
            }

            //opening the subkey
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AgOpenGPS");

            ////create default keys if not existing
            if (regKey == null)
            {
                RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgOpenGPS");

                //storing the values
                Key.SetValue("Language", "en");
                Key.Close();

                Settings.Default.setF_culture = "en";
                Settings.Default.Save();
            }
            else
            {
                //check for corrupt settings file
                try
                {
                    Settings.Default.setF_culture = regKey.GetValue("Language").ToString();
                }
                catch (System.Configuration.ConfigurationErrorsException ex)
                {
                    // Corrupted XML! Delete the file, the user can just reload when this fails to appear. No need to worry them
                    MessageBoxButtons btns = MessageBoxButtons.OK;
                    System.Windows.Forms.MessageBox.Show("Error detected in config file - fixing it now", "Problem!", btns);
                    string filename = ((ex.InnerException as System.Configuration.ConfigurationErrorsException)?.Filename) as string;
                    System.IO.File.Delete(filename);

                    //configPath = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
                    //if (!File.Exists(configPath))
                    //{
                    //    //Existing user config does not exist, so load settings from previous assembly
                    //    Settings.Default.Upgrade();
                    //    Settings.Default.Reload();
                    //    Settings.Default.Save();
                    //}
                }

                Settings.Default.Save();
                regKey.Close();
            }

            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(Properties.Settings.Default.setF_culture);
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Properties.Settings.Default.setF_culture);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FormGPS());
            }
            else
            {
                MessageBox.Show("AgOpenGPS is Already Running");
            }
        }
    }
}