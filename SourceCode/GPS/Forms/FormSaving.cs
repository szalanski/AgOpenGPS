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
        }

        public void InitializeSteps(bool isJobStarted)
        {
            listViewSteps.Items.Clear();

            if (isJobStarted)
            {
                AddStep(ShutdownSteps.SaveParams);
                AddStep(ShutdownSteps.SaveField);
                AddStep(ShutdownSteps.SaveSettings);
                AddStep(ShutdownSteps.Finalizing);
            }
            else
            {
                AddStep(ShutdownSteps.SaveSettings);
                AddStep(ShutdownSteps.Finalizing);
            }
        }

        private void AddStep(string stepText)
        {
            listViewSteps.Items.Add(new ListViewItem(stepText)
            {
                ForeColor = Color.Gray
            });
        }

        public void UpdateStep(int index, string newText)
        {
            if (index >= 0 && index < listViewSteps.Items.Count)
            {
                listViewSteps.Items[index].Text = newText;
                listViewSteps.Items[index].ForeColor = Color.Black;
            }
        }

        public void InsertStep(int index, string text)
        {
            if (index >= 0 && index <= listViewSteps.Items.Count)
            {
                listViewSteps.Items.Insert(index, new ListViewItem(text)
                {
                    ForeColor = Color.Gray
                });
            }
        }

        public void AddFinalMessage()
        {
            listViewSteps.Items.Add(new ListViewItem("")
            {
                ForeColor = Color.Gray
            });

            listViewSteps.Items.Add(new ListViewItem(ShutdownSteps.Beer)
            {
                ForeColor = Color.Black
            });
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
        public static string Beer => "✔ " + gStr.gsSaveBeerTime;
    }
}
