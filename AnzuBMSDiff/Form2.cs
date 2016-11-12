using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AnzuBMSDiff
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        public int ErrorsCount = -1;

        public string ConsoleMessage = "";

        private void Form2_Load(object sender, EventArgs e)
        {
            label1.Text = "Total Errors Found : " + ErrorsCount;

            textBox1.Text = ConsoleMessage;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
