namespace TSW2_Controller
{
    partial class FormMain
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.list_inputs = new System.Windows.Forms.ListBox();
            this.check_active = new System.Windows.Forms.CheckBox();
            this.combobox_trainSelection = new System.Windows.Forms.ComboBox();
            this.timer_CheckSticks = new System.Windows.Forms.Timer(this.components);
            this.bgw_readScreen = new System.ComponentModel.BackgroundWorker();
            this.listBox_debugInfo = new System.Windows.Forms.ListBox();
            this.lbl_resolution = new System.Windows.Forms.Label();
            this.btn_settings = new System.Windows.Forms.Button();
            this.pictureBox_Screenshot_original = new System.Windows.Forms.PictureBox();
            this.pictureBox_Screenshot_alternative = new System.Windows.Forms.PictureBox();
            this.check_deactivateGlobal = new System.Windows.Forms.CheckBox();
            this.lbl_originalResult = new System.Windows.Forms.Label();
            this.lbl_alternativeResult = new System.Windows.Forms.Label();
            this.groupBox_ScanResults = new System.Windows.Forms.GroupBox();
            this.lbl_requests = new System.Windows.Forms.Label();
            this.lbl_scantime = new System.Windows.Forms.Label();
            this.checkBox_autoscroll = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Screenshot_original)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Screenshot_alternative)).BeginInit();
            this.groupBox_ScanResults.SuspendLayout();
            this.SuspendLayout();
            // 
            // list_inputs
            // 
            resources.ApplyResources(this.list_inputs, "list_inputs");
            this.list_inputs.FormattingEnabled = true;
            this.list_inputs.Name = "list_inputs";
            // 
            // check_active
            // 
            resources.ApplyResources(this.check_active, "check_active");
            this.check_active.BackColor = System.Drawing.Color.Red;
            this.check_active.Name = "check_active";
            this.check_active.UseVisualStyleBackColor = false;
            this.check_active.CheckedChanged += new System.EventHandler(this.check_active_CheckedChanged);
            // 
            // combobox_trainSelection
            // 
            resources.ApplyResources(this.combobox_trainSelection, "combobox_trainSelection");
            this.combobox_trainSelection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.combobox_trainSelection.FormattingEnabled = true;
            this.combobox_trainSelection.Name = "combobox_trainSelection";
            this.combobox_trainSelection.Sorted = true;
            this.combobox_trainSelection.SelectedIndexChanged += new System.EventHandler(this.comboBox_Zugauswahl_SelectedIndexChanged);
            // 
            // timer_CheckSticks
            // 
            this.timer_CheckSticks.Interval = 10;
            this.timer_CheckSticks.Tick += new System.EventHandler(this.timer_CheckSticks_Tick);
            // 
            // bgw_readScreen
            // 
            this.bgw_readScreen.WorkerReportsProgress = true;
            this.bgw_readScreen.DoWork += new System.ComponentModel.DoWorkEventHandler(this.bgw_readScreen_DoWork);
            this.bgw_readScreen.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.bgw_readScreen_ProgressChanged);
            this.bgw_readScreen.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.bgw_readScreen_RunWorkerCompleted);
            // 
            // listBox_debugInfo
            // 
            resources.ApplyResources(this.listBox_debugInfo, "listBox_debugInfo");
            this.listBox_debugInfo.FormattingEnabled = true;
            this.listBox_debugInfo.Name = "listBox_debugInfo";
            // 
            // lbl_resolution
            // 
            resources.ApplyResources(this.lbl_resolution, "lbl_resolution");
            this.lbl_resolution.Name = "lbl_resolution";
            // 
            // btn_settings
            // 
            resources.ApplyResources(this.btn_settings, "btn_settings");
            this.btn_settings.Name = "btn_settings";
            this.btn_settings.UseVisualStyleBackColor = true;
            this.btn_settings.Click += new System.EventHandler(this.btn_einstellungen_Click);
            // 
            // pictureBox_Screenshot_original
            // 
            resources.ApplyResources(this.pictureBox_Screenshot_original, "pictureBox_Screenshot_original");
            this.pictureBox_Screenshot_original.Name = "pictureBox_Screenshot_original";
            this.pictureBox_Screenshot_original.TabStop = false;
            // 
            // pictureBox_Screenshot_alternative
            // 
            resources.ApplyResources(this.pictureBox_Screenshot_alternative, "pictureBox_Screenshot_alternative");
            this.pictureBox_Screenshot_alternative.Name = "pictureBox_Screenshot_alternative";
            this.pictureBox_Screenshot_alternative.TabStop = false;
            // 
            // check_deactivateGlobal
            // 
            resources.ApplyResources(this.check_deactivateGlobal, "check_deactivateGlobal");
            this.check_deactivateGlobal.Name = "check_deactivateGlobal";
            this.check_deactivateGlobal.UseVisualStyleBackColor = true;
            this.check_deactivateGlobal.CheckedChanged += new System.EventHandler(this.check_deactivateGlobal_CheckedChanged);
            // 
            // lbl_originalResult
            // 
            resources.ApplyResources(this.lbl_originalResult, "lbl_originalResult");
            this.lbl_originalResult.Name = "lbl_originalResult";
            // 
            // lbl_alternativeResult
            // 
            resources.ApplyResources(this.lbl_alternativeResult, "lbl_alternativeResult");
            this.lbl_alternativeResult.Name = "lbl_alternativeResult";
            // 
            // groupBox_ScanResults
            // 
            resources.ApplyResources(this.groupBox_ScanResults, "groupBox_ScanResults");
            this.groupBox_ScanResults.Controls.Add(this.lbl_alternativeResult);
            this.groupBox_ScanResults.Controls.Add(this.lbl_originalResult);
            this.groupBox_ScanResults.Name = "groupBox_ScanResults";
            this.groupBox_ScanResults.TabStop = false;
            // 
            // lbl_requests
            // 
            resources.ApplyResources(this.lbl_requests, "lbl_requests");
            this.lbl_requests.Name = "lbl_requests";
            // 
            // lbl_scantime
            // 
            resources.ApplyResources(this.lbl_scantime, "lbl_scantime");
            this.lbl_scantime.Name = "lbl_scantime";
            // 
            // checkBox_autoscroll
            // 
            resources.ApplyResources(this.checkBox_autoscroll, "checkBox_autoscroll");
            this.checkBox_autoscroll.Checked = true;
            this.checkBox_autoscroll.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox_autoscroll.Name = "checkBox_autoscroll";
            this.checkBox_autoscroll.UseVisualStyleBackColor = true;
            // 
            // FormMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox_autoscroll);
            this.Controls.Add(this.lbl_scantime);
            this.Controls.Add(this.lbl_requests);
            this.Controls.Add(this.groupBox_ScanResults);
            this.Controls.Add(this.check_deactivateGlobal);
            this.Controls.Add(this.pictureBox_Screenshot_alternative);
            this.Controls.Add(this.pictureBox_Screenshot_original);
            this.Controls.Add(this.btn_settings);
            this.Controls.Add(this.lbl_resolution);
            this.Controls.Add(this.listBox_debugInfo);
            this.Controls.Add(this.combobox_trainSelection);
            this.Controls.Add(this.check_active);
            this.Controls.Add(this.list_inputs);
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Screenshot_original)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox_Screenshot_alternative)).EndInit();
            this.groupBox_ScanResults.ResumeLayout(false);
            this.groupBox_ScanResults.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox list_inputs;
        private System.Windows.Forms.CheckBox check_active;
        private System.Windows.Forms.ComboBox combobox_trainSelection;
        private System.Windows.Forms.Timer timer_CheckSticks;
        private System.ComponentModel.BackgroundWorker bgw_readScreen;
        private System.Windows.Forms.Label lbl_resolution;
        private System.Windows.Forms.Button btn_settings;
        private System.Windows.Forms.PictureBox pictureBox_Screenshot_original;
        private System.Windows.Forms.PictureBox pictureBox_Screenshot_alternative;
        private System.Windows.Forms.CheckBox check_deactivateGlobal;
        private System.Windows.Forms.Label lbl_originalResult;
        private System.Windows.Forms.Label lbl_alternativeResult;
        private System.Windows.Forms.GroupBox groupBox_ScanResults;
        private System.Windows.Forms.Label lbl_requests;
        private System.Windows.Forms.ListBox listBox_debugInfo;
        private System.Windows.Forms.Label lbl_scantime;
        private System.Windows.Forms.CheckBox checkBox_autoscroll;
    }
}

