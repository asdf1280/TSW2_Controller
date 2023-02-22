namespace TSW2_Controller
{
    partial class FormSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormSettings));
            this.label1 = new System.Windows.Forms.Label();
            this.check_showDebug = new System.Windows.Forms.CheckBox();
            this.lbl_version = new System.Windows.Forms.Label();
            this.check_ShowScan = new System.Windows.Forms.CheckBox();
            this.btn_save = new System.Windows.Forms.Button();
            this.btn_controls = new System.Windows.Forms.Button();
            this.comboBox_resolution = new System.Windows.Forms.ComboBox();
            this.progressBar_updater = new System.Windows.Forms.ProgressBar();
            this.comboBox_TrainConfig = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.btn_addTrainConfig = new System.Windows.Forms.Button();
            this.btn_delTrainConfig = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.btn_export = new System.Windows.Forms.Button();
            this.btn_import = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.helpToolStripItem = new System.Windows.Forms.ToolStripMenuItem();
            this.zurConfigGehenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.checkBox_deleteLogsAutomatically = new System.Windows.Forms.CheckBox();
            this.groupBox3.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.Name = "label1";
            // 
            // check_showDebug
            // 
            resources.ApplyResources(this.check_showDebug, "check_showDebug");
            this.check_showDebug.Name = "check_showDebug";
            this.check_showDebug.UseVisualStyleBackColor = true;
            // 
            // lbl_version
            // 
            resources.ApplyResources(this.lbl_version, "lbl_version");
            this.lbl_version.Name = "lbl_version";
            // 
            // check_ShowScan
            // 
            resources.ApplyResources(this.check_ShowScan, "check_ShowScan");
            this.check_ShowScan.Name = "check_ShowScan";
            this.check_ShowScan.UseVisualStyleBackColor = true;
            // 
            // btn_save
            // 
            resources.ApplyResources(this.btn_save, "btn_save");
            this.btn_save.Name = "btn_save";
            this.btn_save.UseVisualStyleBackColor = true;
            this.btn_save.Click += new System.EventHandler(this.btn_speichern_Click);
            // 
            // btn_controls
            // 
            resources.ApplyResources(this.btn_controls, "btn_controls");
            this.btn_controls.Name = "btn_controls";
            this.btn_controls.UseVisualStyleBackColor = true;
            this.btn_controls.Click += new System.EventHandler(this.btn_steuerung_Click);
            // 
            // comboBox_resolution
            // 
            resources.ApplyResources(this.comboBox_resolution, "comboBox_resolution");
            this.comboBox_resolution.FormattingEnabled = true;
            this.comboBox_resolution.Items.AddRange(new object[] {
            resources.GetString("comboBox_resolution.Items"),
            resources.GetString("comboBox_resolution.Items1"),
            resources.GetString("comboBox_resolution.Items2")});
            this.comboBox_resolution.Name = "comboBox_resolution";
            // 
            // progressBar_updater
            // 
            resources.ApplyResources(this.progressBar_updater, "progressBar_updater");
            this.progressBar_updater.Name = "progressBar_updater";
            // 
            // comboBox_TrainConfig
            // 
            resources.ApplyResources(this.comboBox_TrainConfig, "comboBox_TrainConfig");
            this.comboBox_TrainConfig.FormattingEnabled = true;
            this.comboBox_TrainConfig.Name = "comboBox_TrainConfig";
            this.comboBox_TrainConfig.SelectedIndexChanged += new System.EventHandler(this.comboBox_TrainConfig_SelectedIndexChanged);
            this.comboBox_TrainConfig.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.comboBox_TrainConfig_KeyPress);
            this.comboBox_TrainConfig.KeyUp += new System.Windows.Forms.KeyEventHandler(this.comboBox_TrainConfig_KeyUp);
            // 
            // label10
            // 
            resources.ApplyResources(this.label10, "label10");
            this.label10.Name = "label10";
            // 
            // btn_addTrainConfig
            // 
            resources.ApplyResources(this.btn_addTrainConfig, "btn_addTrainConfig");
            this.btn_addTrainConfig.Name = "btn_addTrainConfig";
            this.btn_addTrainConfig.UseVisualStyleBackColor = true;
            this.btn_addTrainConfig.Click += new System.EventHandler(this.btn_trainconfigHinzufuegen_Click);
            // 
            // btn_delTrainConfig
            // 
            resources.ApplyResources(this.btn_delTrainConfig, "btn_delTrainConfig");
            this.btn_delTrainConfig.Name = "btn_delTrainConfig";
            this.btn_delTrainConfig.UseVisualStyleBackColor = true;
            this.btn_delTrainConfig.Click += new System.EventHandler(this.btn_trainconfigLoeschen_Click);
            // 
            // groupBox3
            // 
            resources.ApplyResources(this.groupBox3, "groupBox3");
            this.groupBox3.Controls.Add(this.btn_export);
            this.groupBox3.Controls.Add(this.btn_import);
            this.groupBox3.Controls.Add(this.comboBox_TrainConfig);
            this.groupBox3.Controls.Add(this.btn_addTrainConfig);
            this.groupBox3.Controls.Add(this.btn_delTrainConfig);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.TabStop = false;
            // 
            // btn_export
            // 
            resources.ApplyResources(this.btn_export, "btn_export");
            this.btn_export.Name = "btn_export";
            this.btn_export.UseVisualStyleBackColor = true;
            this.btn_export.Click += new System.EventHandler(this.btn_export_Click);
            // 
            // btn_import
            // 
            resources.ApplyResources(this.btn_import, "btn_import");
            this.btn_import.Name = "btn_import";
            this.btn_import.UseVisualStyleBackColor = true;
            this.btn_import.Click += new System.EventHandler(this.btn_import_Click);
            // 
            // menuStrip1
            // 
            resources.ApplyResources(this.menuStrip1, "menuStrip1");
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripItem});
            this.menuStrip1.Name = "menuStrip1";
            // 
            // helpToolStripItem
            // 
            resources.ApplyResources(this.helpToolStripItem, "helpToolStripItem");
            this.helpToolStripItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.zurConfigGehenToolStripMenuItem});
            this.helpToolStripItem.Name = "helpToolStripItem";
            // 
            // zurConfigGehenToolStripMenuItem
            // 
            resources.ApplyResources(this.zurConfigGehenToolStripMenuItem, "zurConfigGehenToolStripMenuItem");
            this.zurConfigGehenToolStripMenuItem.Name = "zurConfigGehenToolStripMenuItem";
            this.zurConfigGehenToolStripMenuItem.Click += new System.EventHandler(this.zurConfigGehenToolStripMenuItem_Click);
            // 
            // checkBox_deleteLogsAutomatically
            // 
            resources.ApplyResources(this.checkBox_deleteLogsAutomatically, "checkBox_deleteLogsAutomatically");
            this.checkBox_deleteLogsAutomatically.Name = "checkBox_deleteLogsAutomatically";
            this.checkBox_deleteLogsAutomatically.UseVisualStyleBackColor = true;
            // 
            // FormSettings
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox_deleteLogsAutomatically);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.progressBar_updater);
            this.Controls.Add(this.comboBox_resolution);
            this.Controls.Add(this.btn_controls);
            this.Controls.Add(this.btn_save);
            this.Controls.Add(this.check_ShowScan);
            this.Controls.Add(this.lbl_version);
            this.Controls.Add(this.check_showDebug);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.menuStrip1);
            this.MaximizeBox = false;
            this.Name = "FormSettings";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupBox3.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox check_showDebug;
        private System.Windows.Forms.Label lbl_version;
        private System.Windows.Forms.CheckBox check_ShowScan;
        private System.Windows.Forms.Button btn_save;
        private System.Windows.Forms.Button btn_controls;
        private System.Windows.Forms.ComboBox comboBox_resolution;
        private System.Windows.Forms.ProgressBar progressBar_updater;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.ComboBox comboBox_TrainConfig;
        private System.Windows.Forms.Button btn_addTrainConfig;
        private System.Windows.Forms.Button btn_delTrainConfig;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem helpToolStripItem;
        private System.Windows.Forms.ToolStripMenuItem zurConfigGehenToolStripMenuItem;
        private System.Windows.Forms.CheckBox checkBox_deleteLogsAutomatically;
        private System.Windows.Forms.Button btn_export;
        private System.Windows.Forms.Button btn_import;
    }
}