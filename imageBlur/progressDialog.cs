using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace imageBlur
{
    public partial class progressDialog : Form
    {
        public progressDialog()
        {
            InitializeComponent();
            progressBar1.Maximum = 0;
            progressBar1.Value = 0;
            timer1.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GaussProcessing.setBreakProgress(true);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (progressBar1.Maximum == 0) { progressBar1.Maximum = GaussProcessing.getMaxValue(); }
            else
            {
                if (!GaussProcessing.getBreakProgress())
                {
                    progressBar1.Value = GaussProcessing.getProgress();
                    if (progressBar1.Maximum == progressBar1.Value) 
                    {
                        Dispose();
                        Close();
                    } 
                }
            }

        }

        private void progressDialog_FormClosing(object sender, FormClosingEventArgs e)
        {
            GaussProcessing.setBreakProgress(true);
            Dispose();
        }
    }
}
