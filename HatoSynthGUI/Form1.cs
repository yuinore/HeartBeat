using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoSynthGUI
{
    public partial class Form1 : Form
    {
        SynthGUIHandler handler;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            handler = new SynthGUIHandler(this);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            handler.Dispose();
        }
    }
}
