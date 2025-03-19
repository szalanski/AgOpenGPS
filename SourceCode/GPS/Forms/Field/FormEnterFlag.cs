using AgOpenGPS.Controls;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Culture;
using AgOpenGPS.Helpers;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace AgOpenGPS
{
    public partial class FormEnterFlag : Form
    {
        private readonly FormGPS mf = null;

        public FormEnterFlag(Form callingForm)
        {
            //get copy of the calling main form
            mf = callingForm as FormGPS;

            InitializeComponent();

            this.Text = gStr.gsFormFlag;
            labelPoint.Text = gStr.gsPoint;
            nudLatitude.Controls[0].Enabled = false;
            nudLongitude.Controls[0].Enabled = false;

            nudLatitude.Value = (decimal)mf.AppModel.CurrentLatLon.Latitude;
            nudLongitude.Value = (decimal)mf.AppModel.CurrentLatLon.Longitude;
        }

        private void FormEnterAB_Load(object sender, EventArgs e)
        {
            if (!ScreenHelper.IsOnScreen(Bounds))
            {
                Top = 0;
                Left = 0;
            }

        }

        private void nudLatitude_Click(object sender, EventArgs e)
        {
            ((NudlessNumericUpDown)sender).ShowKeypad(this);
        }

        private void nudLongitude_Click(object sender, EventArgs e)
        {
            ((NudlessNumericUpDown)sender).ShowKeypad(this);
        }

        public void CalcHeading()
        {

        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnRed_Click(object sender, EventArgs e)
        {
            Button btnRed = (Button)sender;
            byte flagColor = 0;

            if (btnRed.Name == "btnRed")
            {
                flagColor = 0;
            }
            else if (btnRed.Name == "btnGreen")
            {
                flagColor = 1;
            }
            else if (btnRed.Name == "btnYellow")
            {
                flagColor = 2;
            }

            GeoCoord geoCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84((double)nudLatitude.Value, (double)nudLongitude.Value));
            int nextflag = mf.flagPts.Count + 1;
            CFlag flagPt = new CFlag(
                (double)nudLatitude.Value, (double)nudLongitude.Value,
                geoCoord.Easting, geoCoord.Northing,
                0, flagColor, nextflag, (nextflag).ToString());
            mf.flagPts.Add(flagPt);
            mf.FileSaveFlags();

            Form fc = Application.OpenForms["FormFlags"];

            if (fc != null)
            {
                fc.Focus();
                return;
            }

            if (mf.flagPts.Count > 0)
            {
                mf.flagNumberPicked = nextflag;
                Form form = new FormFlags(mf);
                form.Show(mf);
            }

            Close();

        }

        private void btnLoadFlags_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();

            fileDialog.Filter = "Text Document | *.txt| CSV Document | *.csv";
            fileDialog.Title = "Please select points file";
            fileDialog.Multiselect = false;

            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePath = fileDialog.FileName;
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(filePath);

                        int startIndex;
                        bool isAOGfile;

                        if (lines[0].StartsWith("$Flags")) //flags from AOG
                        {
                            startIndex = 2;
                            isAOGfile = true;
                        }
                        else                             //flags from text file
                        {
                            startIndex = 1;
                            isAOGfile = false;
                        }

                    foreach (string line in lines.Skip(startIndex))
                    {
                        double latitude, longitude;
                        int flagColor;
                        string[] parts = line.Split(',');
                        if (isAOGfile &&
                            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) &&
                            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) &&
                            int.TryParse(parts[6], out flagColor))
                        {
                            string flagName = parts[7];
                            GeoCoord geoCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84(latitude, longitude));
                            int nextflag = mf.flagPts.Count + 1;
                            CFlag flagPt = new CFlag(
                               latitude, longitude,
                               geoCoord.Easting, geoCoord.Northing,
                               0, flagColor, nextflag, flagName);
                            mf.flagPts.Add(flagPt);
                            mf.FileSaveFlags();
                        }
                        else if(!isAOGfile &&
                            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) &&
                            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out longitude) &&
                            int.TryParse(parts[2], out flagColor))
                        {
                            string flagName = (!string.IsNullOrWhiteSpace(parts[3])) ? parts[3].Trim() : $"{mf.flagPts.Count + 1}";  
                            GeoCoord geoCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(new Wgs84(latitude, longitude));
                            int nextflag = mf.flagPts.Count + 1;
                            CFlag flagPt = new CFlag(
                               latitude, longitude,
                               geoCoord.Easting, geoCoord.Northing,
                               0, flagColor, nextflag, flagName);
                            mf.flagPts.Add(flagPt);
                            mf.FileSaveFlags();
                        }
                        else
                        {
                            MessageBox.Show($"Invalid line in file: {line}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    MessageBox.Show("Flags successfully added!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"Error reading file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}