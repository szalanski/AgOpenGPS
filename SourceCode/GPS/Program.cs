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
            //opening the subkey
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AgOpenGPS");

            ////create default keys if not existing
            if (regKey == null)
            {
                RegistryKey Key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgOpenGPS");

                //storing the values
                Key.SetValue("Language", "en");
                Key.SetValue("VehicleFileName", "Default Vehicle");
                Key.SetValue("WorkingDirectory", "Default");
                Key.Close();
            }

            //Base Directory Registry Key
            regKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\AgOpenGPS");

            if (regKey.GetValue("WorkingDirectory") == null)
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgOpenGPS");
                key.SetValue("WorkingDirectory", "Default");
                key.Close();
            }
            string workingDirectory = regKey.GetValue("WorkingDirectory").ToString();
            string baseDirectory = workingDirectory;


            if (workingDirectory == "Default")
            {
                baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AgOpenGPS");
            }
            else //user set to other
            {
                baseDirectory = Path.Combine(workingDirectory, "AgOpenGPS");
            }

            //Vehicle File Name Registry Key
            if (regKey.GetValue("VehicleFileName") == null)
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgOpenGPS");
                key.SetValue("VehicleFileName", "Default Vehicle");
                key.Close();
            }

            string vehicleFileName = regKey.GetValue("VehicleFileName").ToString();

            //Language Registry Key
            if (regKey.GetValue("Language") == null)
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgOpenGPS");
                key.SetValue("Language", "en");
                key.Close();
            }

            string language = regKey.GetValue("Language").ToString();

            regKey.Close();

            //get the fields directory, if not exist, create
            string vehiclesDirectory = Path.Combine(baseDirectory, "Vehicles");
            if (!string.IsNullOrEmpty(vehiclesDirectory) && !Directory.Exists(vehiclesDirectory))
            {
                Directory.CreateDirectory(vehiclesDirectory);
            }

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

            Properties.Settings.Default.setF_culture = language;
            Properties.Settings.Default.setF_workingDirectory = workingDirectory;
            Properties.Settings.Default.setVehicle_vehicleName = vehicleFileName;
            Properties.Settings.Default.Save();

            if (Mutex.WaitOne(TimeSpan.Zero, true))
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo(language);
                Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(language);
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

////check for corrupt settings file
//try
//{
//    Settings.Default.setF_culture = regKey.GetValue("Language").ToString();
//}
//catch (System.Configuration.ConfigurationErrorsException ex)
//{
//    // Corrupted XML! Delete the file, the user can just reload when this fails to appear. No need to worry them
//    MessageBoxButtons btns = MessageBoxButtons.OK;
//    System.Windows.Forms.MessageBox.Show("Error detected in config file - fixing it now", "Problem!", btns);
//    string filename = ((ex.InnerException as System.Configuration.ConfigurationErrorsException)?.Filename) as string;
//    System.IO.File.Delete(filename);
//}
