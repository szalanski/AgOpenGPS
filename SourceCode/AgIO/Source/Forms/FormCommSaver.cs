using AgIO.Properties;
using Microsoft.Win32;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace AgIO
{
    public partial class FormCommSaver : Form
    {
        //class variables
        private readonly FormLoop mf = null;

        public FormCommSaver(Form callingForm)
        {
            //get copy of the calling main form
            mf = callingForm as FormLoop;
            InitializeComponent();
        }

        private void FormCommSaver_Load(object sender, EventArgs e)
        {
            lblLast.Text = "Current " + RegistrySettings.profileName;
            DirectoryInfo dinfo = new DirectoryInfo(RegistrySettings.profileDirectory);
            FileInfo[] Files = dinfo.GetFiles("*.xml");

            foreach (FileInfo file in Files)
            {
                string temp = Path.GetFileNameWithoutExtension(file.Name);
                if (temp.Trim() != "Default Profile")
                    cboxEnv.Items.Add(temp);
            }

            if (cboxEnv.Items.Count == 0)
            {
                cboxEnv.Enabled = false;
            }

            lblCurrentProfile.Text = RegistrySettings.profileName;
        }

        private void cboxVeh_SelectedIndexChanged(object sender, EventArgs e)
        {
            DialogResult result3 = MessageBox.Show(
                "Overwrite: " + cboxEnv.SelectedItem.ToString() + ".xml",
                "Save And Return",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);

            if (result3 == DialogResult.Yes)
            {
                if (RegistrySettings.profileName != "Default Profile")
                    SettingsIO.ExportSettings(Path.Combine(RegistrySettings.profileDirectory, RegistrySettings.profileName + ".xml"));

                RegistrySettings.profileName = SanitizeFileName(cboxEnv.SelectedItem.ToString().Trim());

                Properties.Settings.Default.setConfig_profileName = RegistrySettings.profileName;
                Properties.Settings.Default.Save();

                //save profile in registry
                RegistrySettings.Save();

                if (RegistrySettings.profileName != "Default Profile")
                    SettingsIO.ExportSettings(RegistrySettings.profileDirectory + RegistrySettings.profileName + ".xml");
                else
                    mf.YesMessageBox("Default Profile, Changes will NOT be Saved");
                Close();
            }
        }

        private void tboxName_TextChanged(object sender, EventArgs e)
        {
            TextBox textboxSender = (TextBox)sender;
            int cursorPosition = textboxSender.SelectionStart;
            textboxSender.Text = Regex.Replace(textboxSender.Text, glm.fileRegex, "");

            textboxSender.SelectionStart = cursorPosition;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (tboxName.Text.Trim().Length > 0 && tboxName.Text.Trim() != "Default Profile")
            {
                RegistrySettings.profileName = SanitizeFileName(tboxName.Text.ToString().Trim());

                //reset to default Vehicle and save
                Settings.Default.Reset();
                Settings.Default.Save();


                Properties.Settings.Default.setConfig_profileName = RegistrySettings.profileName;
                Properties.Settings.Default.Save();

                //save profile in registry
                RegistrySettings.Save();

                SettingsIO.ExportSettings(Path.Combine(RegistrySettings.profileDirectory, RegistrySettings.profileName + ".xml"));
                
                DialogResult = DialogResult.Yes;
                Close();
            }
            else
            {
                _ = MessageBox.Show("Enter a File Name To Save...",
                "Save And Return", MessageBoxButtons.OK);
            }
        }

        private void tboxName_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                mf.KeyboardToText((TextBox)sender, this);
                btnSave.Focus();
            }
        }

        private void btnSerialCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private static readonly Regex InvalidFileRegex = new Regex(string.Format("[{0}]", Regex.Escape(@"<>:""/\|?*")));

        public static string SanitizeFileName(string fileName)
        {
            return InvalidFileRegex.Replace(fileName, string.Empty);
        }


        //save As
        private void tboxSaveAs_TextChanged(object sender, EventArgs e)
        {
            TextBox textboxSender = (TextBox)sender;
            int cursorPosition = textboxSender.SelectionStart;
            textboxSender.Text = Regex.Replace(textboxSender.Text, glm.fileRegex, "");

            textboxSender.SelectionStart = cursorPosition;
        }

        private void tboxSaveAs_Click(object sender, EventArgs e)
        {
            if (mf.isKeyboardOn)
            {
                mf.KeyboardToText((TextBox)sender, this);
                btnSave.Focus();
            }
        }

        private void btnSaveAs_Click(object sender, EventArgs e)
        {
            if (tboxSaveAs.Text.Trim().Length > 0 && tboxSaveAs.Text.Trim() != "Default Profile")
            {
                RegistrySettings.profileName = SanitizeFileName(tboxSaveAs.Text.ToString().Trim());

                Properties.Settings.Default.setConfig_profileName = RegistrySettings.profileName;
                Properties.Settings.Default.Save();

                //save profile in registry
                RegistrySettings.Save();

                SettingsIO.ExportSettings(Path.Combine(RegistrySettings.profileDirectory, RegistrySettings.profileName + ".xml"));
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                if (tboxSaveAs.Text.Trim() != "Default Profile")
                {
                    _ = MessageBox.Show("Enter a File Name To Save...",
                    "Save And Return", MessageBoxButtons.OK);
                }
                else
                {
                    _ = MessageBox.Show("Enter a File Name To Save...",
                    "You Cannot Use Default Profile", MessageBoxButtons.OK);
                }
            }
        }
    }
}