using System;
using System.Windows.Forms;

namespace AgIO
{
    public partial class FormCommSetGPS : Form
    {
        //class variables
        private readonly FormLoop mf = null;

        //constructor
        public FormCommSetGPS(Form callingForm)
        {
            //get copy of the calling main form
            mf = callingForm as FormLoop;
            InitializeComponent();
        }

        private void FormCommSet_Load(object sender, EventArgs e)
        {
            //check if GPS port is open or closed and set buttons accordingly
            if (mf.spGPS.IsOpen)
            {
                cboxBaud.Enabled = false;
                cboxPort.Enabled = false;
                btnCloseSerial.Enabled = true;
                btnOpenSerial.Enabled = false;
            }
            else
            {
                cboxBaud.Enabled = true;
                cboxPort.Enabled = true;
                btnCloseSerial.Enabled = false;
                btnOpenSerial.Enabled = true;
            }

            if (mf.spRtcm.IsOpen)
            {
                cboxRtcmBaud.Enabled = false;
                cboxRtcmPort.Enabled = false;
                btnCloseRTCM.Enabled = true;
                btnOpenRTCM.Enabled = false;
                labelRtcmBaud.Text = mf.spGPS.BaudRate.ToString();
                labelRtcmPort.Text = mf.spGPS.PortName;

            }
            else
            {
                cboxRtcmBaud.Enabled = true;
                cboxRtcmPort.Enabled = true;
                btnCloseRTCM.Enabled = false;
                btnOpenRTCM.Enabled = true;
                labelRtcmBaud.Text = "-";
                labelRtcmPort.Text = "-";

            }

            //load the port box with valid port names
            cboxPort.Items.Clear();
            cboxRtcmPort.Items.Clear();

            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                cboxPort.Items.Add(s);
                cboxRtcmPort.Items.Add(s);
            }

            lblCurrentBaud.Text = mf.spGPS.BaudRate.ToString();
            lblCurrentPort.Text = mf.spGPS.PortName;

            labelRtcmBaud.Text = mf.spRtcm.BaudRate.ToString();
            labelRtcmPort.Text = mf.spRtcm.PortName.ToString();
        }

        #region PortSettings //----------------------------------------------------------------

        // GPS Serial Port
        private void cboxBaud_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            mf.spGPS.BaudRate = Convert.ToInt32(cboxBaud.Text);
            FormLoop.baudRateGPS = Convert.ToInt32(cboxBaud.Text);
        }

        private void cboxPort_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            mf.spGPS.PortName = cboxPort.Text;
            FormLoop.portNameGPS = cboxPort.Text;
        }

        private void btnOpenSerial_Click(object sender, EventArgs e)
        {
            mf.OpenGPSPort();
            if (mf.spGPS.IsOpen)
            {
                cboxBaud.Enabled = false;
                cboxPort.Enabled = false;
                btnCloseSerial.Enabled = true;
                btnOpenSerial.Enabled = false;
                lblCurrentBaud.Text = mf.spGPS.BaudRate.ToString();
                lblCurrentPort.Text = mf.spGPS.PortName;
            }
            else
            {
                cboxBaud.Enabled = true;
                cboxPort.Enabled = true;
                btnCloseSerial.Enabled = false;
                btnOpenSerial.Enabled = true;
                MessageBox.Show("Unable to connect to Port");
            }
        }

        private void btnCloseSerial_Click(object sender, EventArgs e)
        {
            mf.CloseGPSPort();
            if (mf.spGPS.IsOpen)
            {
                cboxBaud.Enabled = false;
                cboxPort.Enabled = false;
                btnCloseSerial.Enabled = true;
                btnOpenSerial.Enabled = false;
            }
            else
            {
                cboxBaud.Enabled = true;
                cboxPort.Enabled = true;
                btnCloseSerial.Enabled = false;
                btnOpenSerial.Enabled = true;
            }
        }

        private void btnOpenRTCM_Click(object sender, EventArgs e)
        {
            mf.OpenRtcmPort();
            if (mf.spRtcm.IsOpen)
            {
                cboxRtcmBaud.Enabled = false;
                cboxRtcmPort.Enabled = false;
                btnCloseRTCM.Enabled = true;
                btnOpenRTCM.Enabled = false;
                labelRtcmBaud.Text = mf.spRtcm.BaudRate.ToString();
                labelRtcmPort.Text = mf.spRtcm.PortName;
            }
            else
            {
                cboxRtcmBaud.Enabled = true;
                cboxRtcmPort.Enabled = true;
                btnCloseRTCM.Enabled = false;
                btnOpenRTCM.Enabled = true;
                MessageBox.Show("Unable to connect to Port");
            }
        }

        private void btnCloseRTCM_Click(object sender, EventArgs e)
        {
            mf.CloseRtcmPort();

            if (mf.spRtcm.IsOpen)
            {
                cboxRtcmBaud.Enabled = false;
                cboxRtcmPort.Enabled = false;
                btnCloseRTCM.Enabled = true;
                btnOpenRTCM.Enabled = false;
            }
            else
            {
                cboxRtcmBaud.Enabled = true;
                cboxRtcmPort.Enabled = true;
                btnCloseRTCM.Enabled = false;
                btnOpenRTCM.Enabled = true;
            }
        }

        #endregion PortSettings //----------------------------------------------------------------

        private void timer1_Tick(object sender, EventArgs e)
        {
            //GPS phrase
            textBoxRcv.Text = mf.recvGPSSentence;
        }

        private void btnSerialOK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnRescan_Click(object sender, EventArgs e)
        {
            cboxPort.Items.Clear();
            cboxRtcmPort.Items.Clear();

            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                cboxPort.Items.Add(s);
                cboxRtcmPort.Items.Add(s);
            }
        }

        private void cboxRtcmPort_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.spRtcm.PortName = cboxRtcmPort.Text;
            FormLoop.portNameRtcm = cboxRtcmPort.Text;
        }

        private void cboxRtcmBaud_SelectedIndexChanged(object sender, EventArgs e)
        {
            mf.spRtcm.BaudRate = Convert.ToInt32(cboxRtcmBaud.Text);
            FormLoop.baudRateRtcm = Convert.ToInt32(cboxRtcmBaud.Text);
        }
    } //class
} //namespace
