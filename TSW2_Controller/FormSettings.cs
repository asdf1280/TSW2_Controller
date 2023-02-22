using Octokit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSW2_Controller.Properties;

namespace TSW2_Controller {
    public partial class FormSettings : Form {
        private FormMain _FormMain;
        public FormSettings(FormMain formMain) {
            InitializeComponent();

            _FormMain = formMain;

            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (version.Split('.')[3] == "0") {
                lbl_version.Text = "v" + FormMain.GetVersion(true);
            } else {
                lbl_version.Text = "Pre-release " + version.Split('.')[3] + " " + "v" + version.Split('.')[0] + "." + version.Split('.')[1] + "." + (Convert.ToInt32(version.Split('.')[2]) + 1);
            }
        }

        private void SettingsForm_Load(object sender, EventArgs e) {
            try {
                check_showDebug.Checked = Settings.Default.showDebug;
                check_ShowScan.Checked = Settings.Default.showScanResult;


                string resName = Settings.Default.res.Width.ToString() + "x" + Settings.Default.res.Height.ToString();
                if (!comboBox_resolution.Items.Contains(resName)) {
                    comboBox_resolution.Items.Add(resName);
                }
                if (comboBox_resolution.Items.Contains(resName)) {
                    comboBox_resolution.SelectedItem = resName;
                }

                checkBox_deleteLogsAutomatically.Checked = Settings.Default.DeleteLogsAutomatically;

                string[] files = Directory.GetFiles(ConfigConsts.configFolderPath);
                comboBox_TrainConfig.Items.Add("_Standard");
                foreach (string file in files) {
                    comboBox_TrainConfig.Items.Add(Path.GetFileName(file).Replace(".csv", ""));
                }

                if (File.Exists(ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv")) {
                    comboBox_TrainConfig.SelectedItem = Settings.Default.selectedTrainConfig;
                } else if (Settings.Default.selectedTrainConfig == "_Standard") {
                    comboBox_TrainConfig.SelectedItem = Settings.Default.selectedTrainConfig;
                }
            } catch (Exception ex) {
                Log.ErrorException(ex);
            }
        }


        #region TrainConfig wechseln
        private void comboBox_TrainConfig_KeyUp(object sender, KeyEventArgs e) {
            if (comboBox_TrainConfig.Items.Contains(comboBox_TrainConfig.Text)) {
                btn_trainconfigHinzufuegen.Enabled = false;
                changeConfig();
                if (comboBox_TrainConfig.Text == "_Standard") {
                    btn_trainconfigLoeschen.Enabled = false;
                    btn_export.Enabled = false;

                    btn_trainconfigHinzufuegen.Enabled = false;
                } else {
                    btn_trainconfigLoeschen.Enabled = true;
                    btn_export.Enabled = true;
                }
            } else if (comboBox_TrainConfig.Text == "") {
                btn_trainconfigLoeschen.Enabled = false;
                btn_export.Enabled = false;
                btn_trainconfigHinzufuegen.Enabled = false;
            } else {
                btn_trainconfigLoeschen.Enabled = false;
                btn_export.Enabled = false;
                btn_trainconfigHinzufuegen.Enabled = true;
            }
        }

        private void comboBox_TrainConfig_KeyPress(object sender, KeyPressEventArgs e) {
            if (!System.Text.RegularExpressions.Regex.IsMatch(e.KeyChar.ToString(), @"[^?,:\\/:*?\""<>|]")) {
                e.Handled = true;
            }
        }

        private void comboBox_TrainConfig_SelectedIndexChanged(object sender, EventArgs e) {
            if (comboBox_TrainConfig.Text == "_Standard") {
                btn_trainconfigLoeschen.Enabled = false;
                btn_export.Enabled = false;
                btn_trainconfigHinzufuegen.Enabled = false;
            } else {
                btn_trainconfigLoeschen.Enabled = true;
                btn_export.Enabled = true;
                btn_trainconfigHinzufuegen.Enabled = true;
            }
            btn_trainconfigHinzufuegen.Enabled = false;
            changeConfig();
        }

        private void btn_trainconfigHinzufuegen_Click(object sender, EventArgs e) {
            if (comboBox_TrainConfig.Text != "") {
                if (!comboBox_TrainConfig.Items.Contains(comboBox_TrainConfig.Text)) {
                    //Hinzufügen
                    if (MessageBox.Show("Transfer the data of " + Settings.Default.selectedTrainConfig + "?", "", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                        File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv", true);
                    } else {
                        File.Create(ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv").Close();
                        File.WriteAllText(ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv", "Zug,Beschreibung,ReglerName,JoystickNummer,JoystickInput,Invertieren,InputTyp,InputUmrechnen,Tastenkombination,Aktion,Art,Schritte,Specials,Zeitfaktor,Länger drücken");
                    }
                    changeConfig();
                    comboBox_TrainConfig.Items.Add(comboBox_TrainConfig.Text);
                    comboBox_TrainConfig.SelectedItem = comboBox_TrainConfig.Text;
                }
            } else {
                MessageBox.Show("The text field cannot be empty");
            }
        }

        private void btn_trainconfigLoeschen_Click(object sender, EventArgs e) {
            if (comboBox_TrainConfig.Text != "_Standard") {
                if (File.Exists(ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv")) {
                    if (MessageBox.Show("Do you want to remove \"" + comboBox_TrainConfig.Text + "\"?" + "\n" + "All trains will be deleted!", "", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                        File.Delete(ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv");
                        Settings.Default.selectedTrainConfig = "_Standard";
                        Settings.Default.Save();
                        comboBox_TrainConfig.Items.Remove(comboBox_TrainConfig.Text);
                        comboBox_TrainConfig.Text = "_Standard";
                    }
                }
            }
        }

        private void changeConfig() {
            try {
                if (comboBox_TrainConfig.Text == "_Standard") {
                    if (Settings.Default.selectedTrainConfig != "_Standard") {
                        File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv", true);
                    }
                    File.Copy(ConfigConsts.configStandardPath, ConfigConsts.configPath, true);
                    Settings.Default.selectedTrainConfig = "_Standard";
                } else {
                    File.Copy(ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv", ConfigConsts.configPath, true);
                    Settings.Default.selectedTrainConfig = comboBox_TrainConfig.Text;
                }
            } catch (Exception ex) {
                Log.ErrorException(ex);
                MessageBox.Show("Error with Trainconfig");
            }
            Settings.Default.Save();
        }
        #endregion

        private void btn_speichern_Click(object sender, EventArgs e) {
            try {
                Settings.Default.res = new Rectangle(0, 0, Convert.ToInt32(comboBox_resolution.Text.Split('x')[0]), Convert.ToInt32(comboBox_resolution.Text.Split('x')[1]));
            } catch (Exception ex) {
                Log.ErrorException(ex);
                MessageBox.Show("Error with resolution!");
            }

            try {
                Settings.Default.DeleteLogsAutomatically = checkBox_deleteLogsAutomatically.Checked;
            } catch (Exception ex) {
                Log.ErrorException(ex);
            }

            Settings.Default.showDebug = check_showDebug.Checked;
            Settings.Default.showScanResult = check_ShowScan.Checked;
            Settings.Default.Save();
            Close();
        }

        private void btn_steuerung_Click(object sender, EventArgs e) {
            Log.Add("Going to controls");
            FormSteering2 formSteuerung2 = new FormSteering2(_FormMain);
            formSteuerung2.Location = this.Location;
            formSteuerung2.ShowDialog();
            Log.Add("Leaving controls");

            if (Settings.Default.selectedTrainConfig == "_Standard") {
                if (!File.ReadLines(ConfigConsts.configPath).SequenceEqual(File.ReadLines(ConfigConsts.configStandardPath))) {
                    //Die Datei hat sich geändert
                    Log.Add("Config has changed, but standard selected");
                    string name = "yourConfig";
                    if (!File.Exists(ConfigConsts.configFolderPath + name + ".csv")) {
                        File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + name + ".csv");
                        Settings.Default.selectedTrainConfig = name;
                        Settings.Default.Save();
                        comboBox_TrainConfig.Items.Add(name);
                        comboBox_TrainConfig.SelectedItem = name;
                    } else {
                        int counter = 0;
                        while (File.Exists(ConfigConsts.configFolderPath + name + counter + ".csv")) { counter++; }

                        name = name + counter.ToString();
                        File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + name + ".csv");
                        Settings.Default.selectedTrainConfig = name;
                        Settings.Default.Save();
                        comboBox_TrainConfig.Items.Add(name);
                        comboBox_TrainConfig.SelectedItem = name;
                    }
                    Log.Add("Saved as " + name);
                }
            } else {
                File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv", true);
            }
        }

        private void txt_ConvertKeyToString_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e) {
            //Wenn man im "Aktion" Feld eine Taste drückt finde passenden Namen zur Taste
            //PreviewKeyDown um auch tab-Taste zu erlauben
            ((TextBox)sender).Text = Keyboard.ConvertKeyToString(e.KeyCode);
            btn_speichern.Select();
        }

        private void txt_SuppressKeyPress_KeyDown(object sender, KeyEventArgs e) {
            //Verhindert, dass die gedrückte Taste ins Textfeld geschrieben wird
            e.SuppressKeyPress = true;
        }

        private void informationsdateiErstellenToolStripMenuItem_Click(object sender, EventArgs e) {
            string finishedFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TSW2Controller_HelpFile.zip");
            string startfolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\TSW2_Controller";

            if (File.Exists(finishedFile)) { File.Delete(finishedFile); }
            ZipFile.CreateFromDirectory(startfolder, finishedFile, CompressionLevel.Fastest, true);
            Process.Start("explorer.exe", "/select, \"" + finishedFile + "\"");
            MessageBox.Show("File has been created on the desktop!");
            Close();
            System.Windows.Forms.Application.Exit();
        }

        private void zurConfigGehenToolStripMenuItem_Click(object sender, EventArgs e) {
            Process.Start(ConfigConsts.configPath.Replace("Trainconfig.csv", ""));
        }

        private void btn_export_Click(object sender, EventArgs e) {
            string path = ConfigConsts.configFolderPath + comboBox_TrainConfig.Text + ".csv";
            if (File.Exists(path)) {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "(*.csv)|*.csv";
                saveFileDialog.DefaultExt = "csv";
                saveFileDialog.AddExtension = true;
                saveFileDialog.FileName = comboBox_TrainConfig.Text;
                if (saveFileDialog.ShowDialog() == DialogResult.OK) {

                    if (File.Exists(saveFileDialog.FileName) && saveFileDialog.OverwritePrompt == true) {
                        File.Copy(path, saveFileDialog.FileName, true);
                    } else {
                        File.Copy(path, saveFileDialog.FileName, false);
                    }
                }
            }
        }

        private void btn_import_Click(object sender, EventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "(*.csv)|*.csv";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                bool isOK = true;
                using (var reader = new StreamReader(openFileDialog.FileName)) {
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        string[] values = line.Split(',');
                        if (values.Count() != ConfigConsts.arrayLength) {
                            isOK = false;
                        }
                    }
                }
                if (isOK) {
                    if (File.Exists(ConfigConsts.configFolderPath + openFileDialog.SafeFileName)) {
                        if (MessageBox.Show("Overwrite?", "", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                            File.Copy(openFileDialog.FileName, ConfigConsts.configFolderPath + openFileDialog.SafeFileName, true);
                        } else {
                            isOK = false;
                        }
                    } else {
                        File.Copy(openFileDialog.FileName, ConfigConsts.configFolderPath + openFileDialog.SafeFileName, true);
                    }

                    if (isOK) {
                        comboBox_TrainConfig.Items.Clear();
                        string[] files = Directory.GetFiles(ConfigConsts.configFolderPath);
                        comboBox_TrainConfig.Items.Add("_Standard");
                        foreach (string file in files) {
                            comboBox_TrainConfig.Items.Add(Path.GetFileName(file).Replace(".csv", ""));
                        }

                        if (File.Exists(ConfigConsts.configFolderPath + openFileDialog.SafeFileName)) {
                            comboBox_TrainConfig.SelectedItem = openFileDialog.SafeFileName.Replace(".csv", "");
                        }
                    }
                } else {
                    MessageBox.Show("ERROR");
                }
            }
        }
    }
}
