using HatoBMSLib;
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

namespace AnzuBMSDiff
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void listBox1_DragDrop(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));

            if (filenames.Length >= 2)
            {
                listBox1.Items.Clear();
                listBox1.Items.AddRange(filenames);
            }
            else if (filenames.Length == 1)
            {
                listBox1.Items.Add(filenames[0]);
            }
        }

        private void listBox1_DragEnter(object sender, DragEventArgs e)
        {
            String[] filenames = ((string[])e.Data.GetData(DataFormats.FileDrop));

            if (filenames.Length >= 2)
            {
                e.Effect = DragDropEffects.Move;
            }
            else if (filenames.Length == 1)
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            //ofd.InitialDirectory = "";
            ofd.Filter = "Be-Music Sequence File(*.bms;*.bme;*.bml;.pms)|*.bms;*.bme;*.bml;.pms|すべてのファイル(*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Title = "Open BMS File";
            ofd.RestoreDirectory = true;
            ofd.Multiselect = true;

            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                listBox1.Items.AddRange(ofd.FileNames);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            int sel = listBox1.SelectedIndex;

            if (sel == 0 || sel < 0) return;

            var item = listBox1.Items[sel];
            listBox1.Items.RemoveAt(sel);
            listBox1.Items.Insert(sel - 1, item);
            listBox1.SelectedIndex = sel - 1;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int sel = listBox1.SelectedIndex;

            if (sel == listBox1.Items.Count - 1 || sel < 0) return;

            var item = listBox1.Items[sel];
            listBox1.Items.RemoveAt(sel);
            listBox1.Items.Insert(sel + 1, item);
            listBox1.SelectedIndex = sel + 1;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (listBox1.Items.Count <= 0)
            {
                MessageBox.Show("First, Drag and Drop BMS Files");
                return;
            }
            else if (listBox1.Items.Count <= 1)
            {
                MessageBox.Show("No BMS File to Compare to!");
                return;
            }

            BMSStruct b1 = new BMSStruct(listBox1.Items[0].ToString());

            int errCnt;
            int errTotalCnt = 0;
            StringSuruyatuSafe errmsg = new StringSuruyatuSafe();

            for (int i = 0; i < listBox1.Items.Count; i++)
            {
                if (i == 0) continue;

                if (i >= 2)
                {
                    errmsg += "\r\n\r\n#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#*#\r\n\r\n\r\n";
                }

                BMSStruct b2 = new BMSStruct(listBox1.Items[i].ToString());

                errmsg += (new BMSDiffCheck()).Diff(b1, b2, out errCnt);  // 若干無駄な計算を含む気がしますがオーダーが変わらないので無視します

                errTotalCnt += errCnt;
            }

            Form2 form2 = new Form2();

            form2.ErrorsCount = errTotalCnt;
            form2.ConsoleMessage = errmsg.ToString();

            form2.ShowDialog();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            int sel = listBox1.SelectedIndex;

            if (sel < 0) return;

            listBox1.Items.RemoveAt(sel);
        }
    }
}
