using Microsoft.Win32;
using System;
using System.IO;
using System.Windows.Forms;

namespace AgIO
{
    public partial class FormCommPicker : Form
    {
        //class variables
        private readonly FormLoop mf = null;

        bool isDefaultExist = false;

        public FormCommPicker(Form callingForm)
        {
            //get copy of the calling main form
            mf = callingForm as FormLoop;
            InitializeComponent();
        }

        private void FormCommPicker_Load(object sender, EventArgs e)
        {
            DirectoryInfo dinfo = new DirectoryInfo(RegistrySettings.profileDirectory);
            FileInfo[] Files = dinfo.GetFiles("*.xml");
            if (Files.Length == 0)
            {
                DialogResult = DialogResult.Ignore;
                Close();
                FormTimedMessage form = new FormTimedMessage(2000, "Non Saved", "Save one First");
                form.Show();
            }
            else
            {
                foreach (FileInfo file in Files)
                {
                    string temp = Path.GetFileNameWithoutExtension(file.Name);
                    if (temp.Trim() == "Default Profile")
                    {
                        isDefaultExist = true;
                    }

                    cboxEnv.Items.Add(temp);
                }
            }

            if (cboxEnv.Items.Count < 2 && isDefaultExist )
            {
                mf.YesMessageBox("No Profiles To Load, Save One First");
                Close();
            }
        }

        private void cboxVeh_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (cboxEnv.SelectedItem.ToString().Trim() == "Default Profile")
            {
                mf.YesMessageBox("Choose a Different Profile, Or Create a New One");
            }
            else
            {
                if (RegistrySettings.profileName != "Default Profile")
                    SettingsIO.ExportSettings(Path.Combine(RegistrySettings.profileDirectory, RegistrySettings.profileName + ".xml"));

                SettingsIO.ImportSettings(Path.Combine(RegistrySettings.profileDirectory, cboxEnv.SelectedItem.ToString().Trim() + ".xml"));

                RegistrySettings.profileName = cboxEnv.SelectedItem.ToString().Trim();

                Properties.Settings.Default.setConfig_profileName = RegistrySettings.profileName;
                Properties.Settings.Default.Save();

                RegistrySettings.Save();

                DialogResult = DialogResult.OK;
                Close();
            }

        }
    }
}