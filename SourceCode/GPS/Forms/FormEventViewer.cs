using System;
using System.Windows.Forms;

namespace AgOpenGPS
{
    public partial class FormEventViewer : Form
    {
        //class variables

        public FormEventViewer()
        {
            //get copy of the calling main form
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void FormEventViewer_Load(object sender, EventArgs e)
        {
            rtbAutoSteerStopEvents.HideSelection = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (rtbAutoSteerStopEvents.TextLength != Log.sbEvents.Length)
            {
                rtbAutoSteerStopEvents.Clear();
                rtbAutoSteerStopEvents.AppendText(Log.sbEvents.ToString());
            }
        }
    }
}