using HatoLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioProcessingToolbox
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var buf = AudioFileReader.ReadAllSamples(textBox1.Text);

            if (radioButton_integrate.Checked)
            {
                for (int j = 0; j < buf.Length; j++)
                {
                    double sum = 0;
                    for (int i = 0; i < buf[j].Length; i++)
                    {
                        sum += buf[j][i];
                        buf[j][i] = (float)sum;
                    }
                }

                WaveFileWriter.WriteAllSamples(textBox3.Text, buf);
            }
            else if (radioButton_diff.Checked)
            {
                throw new NotImplementedException("後で書く");
            }
            else if (radioButton_conv.Checked)
            {
                var buf2 = AudioFileReader.ReadAllSamples(textBox2.Text);
                var buf3 = new float[buf.Length][];

                for (int j = 0; j < buf.Length; j++)
                {
                    buf3[j] = new float[buf[j].Length + buf2[0].Length - 1];

                    for (int i = 0; i < buf[j].Length; i++)
                    {
                        for (int k = 0; k < buf2[0].Length; k++)
                        {
                            buf3[j][i + k] += buf[j][i] * buf2[0][buf2[0].Length - k - 1];
                        }
                    }
                }

                WaveFileWriter.WriteAllSamples(textBox3.Text, buf3, buf.Length, 44100, 32);
            }
            
        }
    }
}
