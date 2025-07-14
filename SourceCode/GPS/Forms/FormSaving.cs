using AgOpenGPS.Core.Translations;
using System.Drawing;
using System.Windows.Forms;

namespace AgOpenGPS
{
    public partial class FormSaving : Form
    {
        public FormSaving()
        {
            InitializeComponent();

            labelBeer.Text = "✔ " + gStr.gsSaveBeerTime;
        }

        public void AddStep(string key, string stepText)
        {
            listViewSteps.Items.Add(new ListViewItem(stepText)
            {
                Name = key,
                ForeColor = Color.Gray
            });
        }

        public void UpdateStep(string key, string newText)
        {
            listViewSteps.Items[key].Text = newText;
            listViewSteps.Items[key].ForeColor = Color.Black;
        }

        public void Finish()
        {
            progressBar.Visible = false;
            labelBeer.Visible = true;
        }
    }

    public static class ShutdownSteps
    {
        public static string SaveParams => "• " + gStr.gsSaveFieldParam;
        public static string SaveField => "• " + gStr.gsSaveField;
        public static string SaveSettings => "• " + gStr.gsSaveSettings;
        public static string Finalizing => "• " + gStr.gsSaveFinalizeShutdown;

        public static string UploadAgShare => "• " + gStr.gsSaveUploadToAgshare;
        public static string UploadDone => "✓ " + gStr.gsSaveUploadCompleted;
        public static string UploadFailed => "✗ " + gStr.gsSaveUploadFailed;

        public static string ParamsDone => "✓ " + gStr.gsSaveFieldParamSaved;
        public static string FieldSaved => "✓ " + gStr.gsSaveFieldSavedLocal;
        public static string SettingsSaved => "✓ " + gStr.gsSaveSettingsSaved;
        public static string AllDone => "✔ " + gStr.gsSaveAllDone;
    }
}
