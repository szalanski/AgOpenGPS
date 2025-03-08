using System;
using System.Drawing;
using System.Windows.Forms;
using AgDiag.Protocol;

namespace AgDiag
{
    public partial class FormLoop : Form
    {
        private PGNs _pgns;
        private UDP _udp;

        public FormLoop()
        {
            _pgns = new PGNs();
            _udp = new UDP(_pgns);

            _udp.DefaultSendsUpdated += (s, e) => BeginInvoke((MethodInvoker)(() => UpdateDefaultSends(e)));

            InitializeComponent();
        }

        private static string ByteArrayToHex(byte[] barray)
        {
            char[] c = new char[barray.Length * 3];
            byte b;
            for (int i = 0; i < barray.Length; ++i)
            {
                b = ((byte)(barray[i] >> 4));
                c[i * 3] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = ((byte)(barray[i] & 0xF));
                c[i * 3 + 1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                c[i * 3 + 2] = (char)0x2D;
            }
            return new string(c);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 1) == 1) lblSection1.BackColor = Color.Green;
            else lblSection1.BackColor = Color.Red;
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 2) == 2) lblSection2.BackColor = Color.Green;
            else lblSection2.BackColor = Color.Red;
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 4) == 4) lblSection3.BackColor = Color.Green;
            else lblSection3.BackColor = Color.Red;
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 8) == 8) lblSection4.BackColor = Color.Green;
            else lblSection4.BackColor = Color.Red;

            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 16) == 16) lblSection5.BackColor = Color.Green;
            else lblSection5.BackColor = Color.Red;
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 32) == 32) lblSection6.BackColor = Color.Green;
            else lblSection6.BackColor = Color.Red;
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 64) == 64) lblSection7.BackColor = Color.Green;
            else lblSection7.BackColor = Color.Red;
            if ((_pgns.asData.Bytes[_pgns.asData.sc1to8] & 128) == 128) lblSection8.BackColor = Color.Green;
            else lblSection8.BackColor = Color.Red;

            lblSpeed.Text = (_pgns.asData.Bytes[_pgns.asData.speedHi] << 8 | _pgns.asData.Bytes[_pgns.asData.speedLo]).ToString();
            lblSetSteerAngle.Text = (_pgns.asData.Bytes[_pgns.asData.steerAngleHi] << 8 | _pgns.asData.Bytes[_pgns.asData.steerAngleLo]).ToString();
            lblStatus.Text = _pgns.asData.Bytes[_pgns.asData.status].ToString();

            lblSteerDataPGN.Text = ByteArrayToHex(_pgns.asData.Bytes);

            //from autosteer  module
            lblSteerAngleActual.Text = ((Int16)((_pgns.asModule.Bytes[_pgns.asModule.actualHi] << 8)
                + _pgns.asModule.Bytes[_pgns.asModule.actualLo])).ToString();

            lblHeading.Text = ((Int16)((_pgns.asModule.Bytes[_pgns.asModule.headHi] << 8)
                + _pgns.asModule.Bytes[_pgns.asModule.headLo])).ToString();

            lblRoll.Text = ((Int16)((_pgns.asModule.Bytes[_pgns.asModule.rollHi] << 8)
                + _pgns.asModule.Bytes[_pgns.asModule.rollLo])).ToString();

            lblPWM.Text = (_pgns.asModule.Bytes[_pgns.asModule.pwm]).ToString();

            if ((_pgns.asModule.Bytes[_pgns.asModule.switchStatus] & 1) == 1)
                lblWorkSwitch.BackColor = Color.Red;
            else lblWorkSwitch.BackColor = Color.Green;

            if ((_pgns.asModule.Bytes[_pgns.asModule.switchStatus] & 2) == 2)
                lblSteerSwitch.BackColor = Color.Red;
            else lblSteerSwitch.BackColor = Color.Green;

            lblPGNFromAutosteerModule.Text = ByteArrayToHex(_pgns.asModule.Bytes);

            //Autosteer settings
            lblPGNSteerSettings.Text = ByteArrayToHex(_pgns.asSet.Bytes);
            lblP.Text = _pgns.asSet.Bytes[_pgns.asSet.gainProportional].ToString();
            lblHiPWM.Text = _pgns.asSet.Bytes[_pgns.asSet.highPWM].ToString();
            lblLoPWM.Text = _pgns.asSet.Bytes[_pgns.asSet.lowPWM].ToString();
            lblMinPWM.Text = _pgns.asSet.Bytes[_pgns.asSet.minPWM].ToString();
            lblCPD.Text = _pgns.asSet.Bytes[_pgns.asSet.countsPerDegree].ToString();
            lblAckerman.Text = _pgns.asSet.Bytes[_pgns.asSet.ackerman].ToString();
            lblOffset.Text = (_pgns.asSet.Bytes[_pgns.asSet.wasOffsetHi] << 8 | _pgns.asSet.Bytes[_pgns.asSet.wasOffsetLo]).ToString();


            //autosteer config bytes
            lblPGNAutoSteerConfig.Text = ByteArrayToHex(_pgns.asConfig.Bytes);
            lblSet0.Text = _pgns.asConfig.Bytes[_pgns.asConfig.set0].ToString();
            lblPulseCount.Text = _pgns.asConfig.Bytes[_pgns.asConfig.maxPulse].ToString();
            lblMinSpeed.Text = _pgns.asConfig.Bytes[_pgns.asConfig.minSpeed].ToString();

            //machine bytes
            lblPNGMachine.Text = ByteArrayToHex(_pgns.maData.Bytes);
            TreeLbl.Text = _pgns.maData.Bytes[_pgns.maData.tree].ToString();
        }

        private void UpdateDefaultSends(int defaultSends)
        {
            lblDefaultSends.Text = defaultSends.ToString();
        }

        private void FormLoop_Load(object sender, EventArgs e)
        {
            timer1.Enabled = true;
            _udp.LoadLoopback();
        }

        private void FormLoop_FormClosing(object sender, FormClosingEventArgs e)
        {
            _udp.CloseLoopback();
        }
    }
}

