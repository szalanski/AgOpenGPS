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
            DirectoryInfo dinfo = new DirectoryInfo(mf.profileDirectory);
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
                if (mf.profileName != "Default Profile")
                    SettingsIO.ExportSettings(Path.Combine(mf.profileDirectory, mf.profileName + ".xml"));

                SettingsIO.ImportSettings(Path.Combine(mf.profileDirectory, cboxEnv.SelectedItem.ToString().Trim() + ".xml"));

                mf.profileName = cboxEnv.SelectedItem.ToString().Trim();
                Properties.Settings.Default.setConfig_profileName = mf.profileName;
                Properties.Settings.Default.Save();

                RegistryKey key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\AgIO");
                key.SetValue("ProfileName", mf.profileName);
                key.Close();

                DialogResult = DialogResult.OK;
                Close();
            }

        }
    }
}