namespace AudioProcessingToolbox
{
    partial class Form1
    {
        /// <summary>
        /// 必要なデザイナー変数です。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 使用中のリソースをすべてクリーンアップします。
        /// </summary>
        /// <param name="disposing">マネージ リソースが破棄される場合 true、破棄されない場合は false です。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows フォーム デザイナーで生成されたコード

        /// <summary>
        /// デザイナー サポートに必要なメソッドです。このメソッドの内容を
        /// コード エディターで変更しないでください。
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radioButton_integrate = new System.Windows.Forms.RadioButton();
            this.radioButton_diff = new System.Windows.Forms.RadioButton();
            this.radioButton_conv = new System.Windows.Forms.RadioButton();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(67, 16);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(461, 19);
            this.textBox1.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 12);
            this.label1.TabIndex = 1;
            this.label1.Text = "InputFile";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 83);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 12);
            this.label2.TabIndex = 2;
            this.label2.Text = "OutputFile";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(67, 49);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(461, 19);
            this.textBox2.TabIndex = 3;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.radioButton_conv);
            this.panel1.Controls.Add(this.radioButton_diff);
            this.panel1.Controls.Add(this.radioButton_integrate);
            this.panel1.Location = new System.Drawing.Point(67, 118);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(255, 155);
            this.panel1.TabIndex = 4;
            // 
            // radioButton_integrate
            // 
            this.radioButton_integrate.AutoSize = true;
            this.radioButton_integrate.Checked = true;
            this.radioButton_integrate.Location = new System.Drawing.Point(17, 16);
            this.radioButton_integrate.Name = "radioButton_integrate";
            this.radioButton_integrate.Size = new System.Drawing.Size(68, 16);
            this.radioButton_integrate.TabIndex = 0;
            this.radioButton_integrate.TabStop = true;
            this.radioButton_integrate.Text = "Integrate";
            this.radioButton_integrate.UseVisualStyleBackColor = true;
            // 
            // radioButton_diff
            // 
            this.radioButton_diff.AutoSize = true;
            this.radioButton_diff.Location = new System.Drawing.Point(17, 39);
            this.radioButton_diff.Name = "radioButton_diff";
            this.radioButton_diff.Size = new System.Drawing.Size(87, 16);
            this.radioButton_diff.TabIndex = 1;
            this.radioButton_diff.Text = "Differentiate";
            this.radioButton_diff.UseVisualStyleBackColor = true;
            // 
            // radioButton_conv
            // 
            this.radioButton_conv.AutoSize = true;
            this.radioButton_conv.Location = new System.Drawing.Point(17, 62);
            this.radioButton_conv.Name = "radioButton_conv";
            this.radioButton_conv.Size = new System.Drawing.Size(70, 16);
            this.radioButton_conv.TabIndex = 2;
            this.radioButton_conv.TabStop = true;
            this.radioButton_conv.Text = "Convolve";
            this.radioButton_conv.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(456, 315);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 5;
            this.button1.Text = "Start!";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(67, 80);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(461, 19);
            this.textBox3.TabIndex = 6;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 55);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(36, 12);
            this.label3.TabIndex = 7;
            this.label3.Text = "Input2";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(625, 361);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton radioButton_conv;
        private System.Windows.Forms.RadioButton radioButton_diff;
        private System.Windows.Forms.RadioButton radioButton_integrate;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label3;
    }
}

