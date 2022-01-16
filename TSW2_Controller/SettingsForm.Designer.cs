﻿namespace TSW2_Controller
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.txt_resWidth = new System.Windows.Forms.TextBox();
            this.txt_resHeight = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.check_showDebug = new System.Windows.Forms.CheckBox();
            this.button1 = new System.Windows.Forms.Button();
            this.lbl_version = new System.Windows.Forms.Label();
            this.check_ShowScan = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // txt_resWidth
            // 
            this.txt_resWidth.Location = new System.Drawing.Point(6, 25);
            this.txt_resWidth.Name = "txt_resWidth";
            this.txt_resWidth.Size = new System.Drawing.Size(45, 20);
            this.txt_resWidth.TabIndex = 0;
            this.txt_resWidth.Text = "1920";
            // 
            // txt_resHeight
            // 
            this.txt_resHeight.Location = new System.Drawing.Point(57, 25);
            this.txt_resHeight.Name = "txt_resHeight";
            this.txt_resHeight.Size = new System.Drawing.Size(45, 20);
            this.txt_resHeight.TabIndex = 1;
            this.txt_resHeight.Text = "1080";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(83, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Spiel-Auflösung:";
            // 
            // check_showDebug
            // 
            this.check_showDebug.AutoSize = true;
            this.check_showDebug.Location = new System.Drawing.Point(6, 56);
            this.check_showDebug.Name = "check_showDebug";
            this.check_showDebug.Size = new System.Drawing.Size(125, 17);
            this.check_showDebug.TabIndex = 4;
            this.check_showDebug.Text = "Debug Informationen";
            this.check_showDebug.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(12, 138);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(74, 22);
            this.button1.TabIndex = 5;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // lbl_version
            // 
            this.lbl_version.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_version.AutoSize = true;
            this.lbl_version.Location = new System.Drawing.Point(139, 150);
            this.lbl_version.Name = "lbl_version";
            this.lbl_version.Size = new System.Drawing.Size(40, 13);
            this.lbl_version.TabIndex = 6;
            this.lbl_version.Text = "vX.X.X";
            // 
            // check_ShowScan
            // 
            this.check_ShowScan.AutoSize = true;
            this.check_ShowScan.Location = new System.Drawing.Point(6, 79);
            this.check_ShowScan.Name = "check_ShowScan";
            this.check_ShowScan.Size = new System.Drawing.Size(141, 17);
            this.check_ShowScan.TabIndex = 7;
            this.check_ShowScan.Text = "Scan-Ergebnis anzeigen";
            this.check_ShowScan.UseVisualStyleBackColor = true;
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(191, 172);
            this.Controls.Add(this.check_ShowScan);
            this.Controls.Add(this.lbl_version);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.check_showDebug);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txt_resHeight);
            this.Controls.Add(this.txt_resWidth);
            this.Name = "SettingsForm";
            this.Text = "Einstellungen";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txt_resWidth;
        private System.Windows.Forms.TextBox txt_resHeight;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox check_showDebug;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label lbl_version;
        private System.Windows.Forms.CheckBox check_ShowScan;
    }
}