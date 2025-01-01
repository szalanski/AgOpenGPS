using AgIO.Properties;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;

namespace AgIO
{
    public static class SettingsIO
    {
        /// <summary>
        /// Import an XML and save to 1 section of user.config
        /// </summary>
        /// <param name="settingFile">Either Settings or Vehicle or Tools</param>
        /// <param name="settingsFilePath">Usually Documents.Drive.Folder</param>
        internal static void ImportSettings(string settingsFilePath)
        {
            if (!File.Exists(settingsFilePath))
            {
                throw new FileNotFoundException();
            }

            //var appSettings = Properties.Settings.Default;
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);

                string sectionName = "";

                sectionName = Properties.Settings.Default.Context["GroupName"].ToString();

                XDocument document = XDocument.Load(Path.Combine(settingsFilePath));
                string settingsSection = document.XPathSelectElements($"//{sectionName}").Single().ToString();
                config.GetSectionGroup("userSettings").Sections[sectionName].SectionInformation.SetRawXml(settingsSection);
                config.Save(ConfigurationSaveMode.Modified);

                {
                    Properties.Settings.Default.Reload();
                }
            }
            catch (Exception) // Should make this more specific
            {
                // Could not import settings.
                {
                    Properties.Settings.Default.Reload();
                }
            }
        }

        internal static void ExportSettings(string settingsFilePath)
        {
            Properties.Settings.Default.Save();

            //Export the entire settings as an xml
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            config.SaveAs(settingsFilePath);
        }
    }

    public static class RegistrySettings
    {
        public static string culture = "en";
        public static string profileDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AgOpenGPS", "AgIO");
        public static string LogsDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AgOpenGPS", "Logs");
        public static string profileName = "Default Profile";

        public static void Load()
        {
            try
            {
                try
                {
                    //create Logs directory if not exist
                    if (!string.IsNullOrEmpty(LogsDirectory) && !Directory.Exists(LogsDirectory))
                    {
                        Directory.CreateDirectory(LogsDirectory);
                        Log.sbEvent.Append("Logs Dir Created\r");
                    }
                }
                catch (Exception ex)
                {
                    Log.sbEvent.Append("Catch, Serious Problem Making Logs Directory: " + ex.ToString());
                }

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
                    Log.sbEvent.Append("Registry -> SubKey AgIO and Keys Created\r");
                }
                else
                {
                    try
                    {
                        //Profile File Name from Registry Key
                        if (regKey.GetValue("ProfileName") == null)
                        {
                            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgIO");
                            key.SetValue("ProfileName", "Default Profile");
                            Log.sbEvent.Append("Registry -> Key Profile Name was null and Created\r");
                        }
                        else
                        {
                            profileName = regKey.GetValue("ProfileName").ToString();

                            //get the Documents directory, if not exist, create
                            profileDirectory =
                                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AgOpenGPS", "AgIO");

                            if (!string.IsNullOrEmpty(profileDirectory) && !Directory.Exists(profileDirectory))
                            {
                                Directory.CreateDirectory(profileDirectory);
                            }

                            //what's in the vehicle directory
                            DirectoryInfo dinfo = new DirectoryInfo(RegistrySettings.profileDirectory);
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
                                Log.sbEvent.Append("Registry -> " + profileName + ".XML Profile Loaded");
                            }
                            else
                            {
                                Log.EventWriter("Registry -> " + profileName + ".XML Profile does not exist. Called in Program.cs");
                                profileName = "Default Profile";
                                Save();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.sbEvent.Append("Registry -> Catch, Serious Problem Loading Profile, Doing Registry Reset: " + ex.ToString());
                        Reset();

                        //reset to default Vehicle and save
                        Settings.Default.Reset();
                        Settings.Default.Save();
                    }
                    regKey.Close();
                }
            }
            catch (Exception ex)
            {
                Log.sbEvent.Append("Registry -> Catch, Serious Problem Creating Registry keys: " + ex.ToString());
                Reset();
            }
        }

        public static void Save()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgIO");
            try
            {
                key.SetValue("ProfileName", profileName);
            }
            catch (Exception ex)
            {
                Log.sbEvent.Append("Registry -> Catch, Serious Problem Saving keys: " + ex.ToString());
            }
            key.Close();
        }

        public static void Reset()
        {
            RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgIO");
            try
            {
                key.SetValue("ProfileName", "Default Profile");
                Log.sbEvent.Append("Registry -> Resetting Registry keys");
            }
            catch (Exception ex)
            {
                Log.sbEvent.Append("\"Registry -> Catch, Serious Problem Resetting Registry keys: " + ex.ToString());
            }
            key.Close();
        }
    }
}