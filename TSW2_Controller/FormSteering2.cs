using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSW2_Controller
{
    public partial class FormSteering2 : Form
    {
        private FormMain _FormMain;

        List<string[]> trainConfig = new List<string[]>();
        List<VirtualController> virtualControllerList = new List<VirtualController>();
        List<string[]> customController = new List<string[]>();
        string selectedTrain = "";
        string selectedRegler = "";
        int configIsBeeingChanged = 0;

        public FormSteering2(FormMain formMain)
        {
            InitializeComponent();

            _FormMain = formMain;

            trainConfig = _FormMain.trainConfig;

            comboBoxT0_Zugauswahl.Items.Add(ConfigConsts.globalTrainConfigName);
            comboBoxT0_Zugauswahl.Items.AddRange(formMain.trainNames.ToArray());
            comboBoxT0_Zugauswahl.SelectedItem = ConfigConsts.globalTrainConfigName;

            if (_FormMain.selectedTrain != "")
            {
                comboBoxT0_Zugauswahl.SelectedItem = _FormMain.selectedTrain;
            }

            lblB_Bedingung.Hide();
            txtB_Bedingung.Hide();
            lblR_KnopfNr.Text = "Button no.";

            dataGridView1.Size = new Size(254, 205);

            //Verstecke die Auswahl der Tab-Buttons
            tabControl_main.Appearance = TabAppearance.FlatButtons;
            tabControl_main.ItemSize = new Size(0, 1);
            tabControl_main.SizeMode = TabSizeMode.Fixed;
            tabControl_main.Size = new Size(313, 130);
        }

        #region Allgemeines
        private void tabControl_main_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl_main.SelectedIndex == 0)
            {
                tabControl_main.Size = new Size(313, 130);
            }
            else if (tabControl_main.SelectedIndex == 2)
            {
                tabControl_main.Size = new Size(313, 348);
            }
            else if (tabControl_main.SelectedIndex == 3)
            {
                tabControl_main.Size = new Size(474, 266);
            }
            else
            {
                tabControl_main.Size = new Size(647, 348);
            }
        }
        private void ReadControllersFile()
        {
            comboBoxT1_Controllers.Items.Clear();
            virtualControllerList.Clear();
            if (File.Exists(ConfigConsts.controllersConfigPath))
            {
                using (var reader = new StreamReader(ConfigConsts.controllersConfigPath))
                {
                    bool skipFirst = true;
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        if (!skipFirst)
                        {
                            VirtualController vc = new VirtualController();
                            vc.InsertFileArray(values);
                            virtualControllerList.Add(vc);

                            comboBoxT1_Controllers.Items.Add(values[0]);
                        }
                        else
                        {
                            skipFirst = false;
                        }
                    }
                }
            }
        }

        private void txt_OnlyNumbers_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void hilfeToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }
        #endregion

        #region Zugauswahl
        private void btnT0_edit_Click(object sender, EventArgs e)
        {
            selectedTrain = comboBoxT0_Zugauswahl.Text;
            _FormMain.selectedTrain = selectedTrain;
            ResetKonfiguration();
            if (selectedTrain == ConfigConsts.globalTrainConfigName)
            {
                tabControl_ReglerKnopf.SelectedIndex = 1;
                //btnT1_Controller_Add.Enabled = false;
            }
            if (tabControl_ReglerKnopf.SelectedIndex == 1)
            {
                groupBoxT1_Regler.Hide();
            }
            else
            {
                groupBoxT1_Regler.Show();
            }
            tabControl_main.SelectedIndex = 1;
            panel_Regler.Enabled = false;
        }
        private void btnT0_Add_Click(object sender, EventArgs e)
        {
            _FormMain.selectedTrain = comboBoxT0_Zugauswahl.Text;
            ComboBox cb = comboBoxT0_Zugauswahl;
            cb.Text = cb.Text.Replace(",", "");
            if (!cb.Items.Contains(cb.Text))
            {
                cb.Items.Add(cb.Text);
                cb.SelectedItem = cb.Text;
                resetControllerBearbeiten(false);
                selectedTrain = comboBoxT0_Zugauswahl.Text;
                ResetKonfiguration();
                tabControl_main.SelectedIndex = 1;
                panel_Regler.Enabled = false;
            }
            else
            {
                resetControllerBearbeiten(false);
            }
            //panel_main.Enabled = true;
        }
        private void btnT0_Delete_Click(object sender, EventArgs e)
        {
            selectedTrain = comboBoxT0_Zugauswahl.Text;

            if (selectedTrain != "")
            {
                if (MessageBox.Show("Do you really want to remove \"" + selectedTrain + "?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    if (selectedTrain != ConfigConsts.globalTrainConfigName)
                    {
                        comboBoxT0_Zugauswahl.Items.Remove(selectedTrain);
                    }

                    int counter = 0;
                    for (int i = 0; i < trainConfig.Count; i++)
                    {
                        if (trainConfig[i][ConfigConsts.trainName] == selectedTrain)
                        {
                            trainConfig.RemoveAt(i);
                            i--;
                            counter++;
                        }
                    }

                    //Schreibe Datei
                    string[] line = new string[trainConfig.Count];
                    for (int i = 0; i < trainConfig.Count; i++)
                    {
                        string combined = "";
                        foreach (string s in trainConfig[i])
                        {
                            combined += s + ",";
                        }
                        combined = combined.Remove(combined.Length - 1);
                        line[i] = combined;
                    }
                    File.WriteAllLines(ConfigConsts.configPath, line);

                    MessageBox.Show(counter + " entries deleted!");
                }
            }
        }
        private void btnT0_globalKeybinds_Click(object sender, EventArgs e)
        {
            selectedTrain = "";
            resetControllerBearbeiten();
            tabControl_main.SelectedIndex = 2;
        }
        private void btnT0_back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion

        #region Konfiguration
        private void ResetKonfiguration(bool clearLists = true)
        {
            ReadControllersFile();
            lblT1_TrainName.Text = selectedTrain;

            txtR_AnzahlStufen.Text = "";
            txtR_InputUmrechnen.Text = "";
            txtR_JoyAchse.Text = "";
            txtR_JoyNr.Text = "";
            txtR_LongPress.Text = "";
            txtR_Sonderfaelle.Text = "";
            txtR_Zeitfaktor.Text = "";

            txtB_Aktion.Text = "";
            txtB_Bedingung.Text = "";
            txtB_JoystickKnopf.Text = "";
            txtB_JoystickNr.Text = "";
            txtB_Tastenkombination.Text = "";

            comboBoxB_KnopfAuswahl.Text = "";

            if (clearLists)
            {
                listBoxT1_ControllerList.Items.Clear();
                comboBoxB_KnopfAuswahl.Items.Clear();
                foreach (string[] singleTrain in trainConfig)
                {
                    if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.controllerName] != "" && !listBoxT1_ControllerList.Items.Equals(singleTrain[ConfigConsts.controllerName]))
                    {
                        listBoxT1_ControllerList.Items.Add(singleTrain[ConfigConsts.controllerName]);
                    }
                }
                foreach (string[] singleTrain in trainConfig)
                {
                    if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.inputType].Contains("Button") && !comboBoxB_KnopfAuswahl.Items.Equals(singleTrain[ConfigConsts.description]))
                    {
                        comboBoxB_KnopfAuswahl.Items.Add(singleTrain[ConfigConsts.description]);
                    }
                }
            }
        }
        private void btnT1_Controller_Add_Click(object sender, EventArgs e)
        {
            bool bereitsVorhanden = false;
            foreach (string item in listBoxT1_ControllerList.Items)
            {
                if (comboBoxT1_Controllers.Text == item || comboBoxT1_Controllers.Text == "")
                {
                    bereitsVorhanden = true;
                    break;
                }
            }
            if (!bereitsVorhanden)
            {
                listBoxT1_ControllerList.Items.Add(comboBoxT1_Controllers.Text);
            }
        }
        private void tabControl_ReglerKnopf_KeyPress(object sender, KeyPressEventArgs e)
        {
            configIsBeeingChanged = 1;
        }
        private void listBoxT1_ControllerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool cancel = false;
            if (configIsBeeingChanged != 0)
            {
                if (configIsBeeingChanged == -1)
                {
                    configIsBeeingChanged = 1;
                    cancel = true;
                }
                else
                {
                    if (MessageBox.Show("Continue without saving?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        configIsBeeingChanged = 0;
                    }
                    else
                    {
                        cancel = true;
                        configIsBeeingChanged = -1;
                        listBoxT1_ControllerList.SelectedItem = selectedRegler;
                    }
                }
            }

            if (!cancel)
            {
                configIsBeeingChanged = 0;
                ResetKonfiguration(false);
                if (listBoxT1_ControllerList.SelectedItem != null)
                {
                    foreach (string[] singleTrain in trainConfig)
                    {
                        if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.controllerName] == listBoxT1_ControllerList.SelectedItem.ToString())
                        {
                            txtR_JoyNr.Text = singleTrain[ConfigConsts.joystickNumber];
                            txtR_JoyAchse.Text = singleTrain[ConfigConsts.joystickInput];
                            txtR_AnzahlStufen.Text = singleTrain[ConfigConsts.steps];
                            txtR_LongPress.Text = singleTrain[ConfigConsts.longPress].Replace("[", "").Replace("]", " ").TrimEnd(' ');
                            txtR_Sonderfaelle.Text = singleTrain[ConfigConsts.specials].Replace(" ", "_").Replace("[", "").Replace("]", " ").TrimEnd(' '); ;
                            txtR_Zeitfaktor.Text = singleTrain[ConfigConsts.timeFactor];
                            txtR_InputUmrechnen.Text = singleTrain[ConfigConsts.inputConvert].Replace("[", "").Replace("]", " ").TrimEnd(' '); ;

                            if (singleTrain[ConfigConsts.type] == "Stufenlos")
                            {
                                radioR_Stufenlos.Checked = true;
                            }
                            else
                            {
                                radioR_Stufen.Checked = true;
                            }

                            if (singleTrain[ConfigConsts.controllerName].Split('|').Count() > 0)
                            {
                                dataGridView1.Rows.Clear();
                                string[] split = singleTrain[ConfigConsts.invert].Split('|');
                                for (int i = 0; i < split.Count() - 1; i += 2)
                                {
                                    dataGridView1.Rows.Add(new string[] { split[i], split[i + 1] });
                                }
                                ReadDataGrid();
                            }
                        }
                    }
                    selectedRegler = listBoxT1_ControllerList.Text;

                    radioR_Stufen.Enabled = true;
                    foreach (VirtualController vc in virtualControllerList)
                    {
                        if (selectedRegler == vc.name)
                        {
                            if (vc.isMasterController)
                            {
                                radioR_Stufen.Enabled = false;
                                radioR_Stufenlos.Checked = true;
                            }
                            break;
                        }
                    }

                    panel_Regler.Enabled = true;
                    btnT1_Controller_Remove.Enabled = true;
                }
            }
        }
        private void btnT1_back_Click(object sender, EventArgs e)
        {
            bool cancel = false;
            if (configIsBeeingChanged != 0)
            {
                if (MessageBox.Show("Continue without saving?", "", MessageBoxButtons.YesNo) != DialogResult.Yes)
                {
                    cancel = true;
                }
            }

            if (!cancel)
            {
                configIsBeeingChanged = 0;
                tabControl_main.SelectedIndex = 0;
            }
        }

        private void tabControl_ReglerKnopf_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (selectedTrain == ConfigConsts.globalTrainConfigName)
            {
                tabControl_ReglerKnopf.SelectedIndex = 1;
            }
            if (tabControl_ReglerKnopf.SelectedIndex == 0)
            {
                groupBoxT1_Regler.Visible = true;
            }
            else
            {
                groupBoxT1_Regler.Visible = false;
            }
        }

        #region Regler
        
        private void btn_R_eigenes_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Visible)
            {
                dataGridView1.Hide();
                ReadDataGrid();
            }
            else
            {
                dataGridView1.Show();
                dataGridView1.BringToFront();
            }
        }
        private void ReadDataGrid()
        {
            try
            {
                customController.Clear();
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    dataGridView1.Rows[i].Cells[0].Value = Convert.ToInt32(dataGridView1.Rows[i].Cells[0].Value);
                    dataGridView1.Rows[i].Cells[1].Value = Convert.ToInt32(dataGridView1.Rows[i].Cells[1].Value);
                }
                dataGridView1.Sort(dataGridView1.Columns[0], ListSortDirection.Ascending);
                for (int i = 0; i < dataGridView1.Rows.Count - 1; i++)
                {
                    customController.Add(new string[] { dataGridView1.Rows[i].Cells[0].Value.ToString(), dataGridView1.Rows[i].Cells[1].Value.ToString() });
                }
            }
            catch
            {

            }
        }
        private void radioR_CheckedChanged(object sender, EventArgs e)
        {
            if (radioR_Stufenlos.Checked)
            {
                txtR_AnzahlStufen.Visible = false;
                lblR_AnzahlStufen.Visible = false;
            }
            else
            {
                txtR_AnzahlStufen.Visible = true;
                lblR_AnzahlStufen.Visible = true;
            }
        }
        private void btnR_GetTimeFactor_Click(object sender, EventArgs e)
        {
            FormTimefactor2 formZeitfaktor2 = new FormTimefactor2(listBoxT1_ControllerList.SelectedItem.ToString(), radioR_Stufenlos.Checked);
            if (formZeitfaktor2.DialogResult != DialogResult.Cancel) { formZeitfaktor2.ShowDialog(); }
            txtR_Zeitfaktor.Text = formZeitfaktor2.resultString;
        }
        private void btnT1_Controller_Remove_Click(object sender, EventArgs e)
        {
            RemoveSelectedController();
        }
        private void listBoxT1_ControllerList_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete && MessageBox.Show("Do you really want to remove this controller?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                RemoveSelectedController();
            }
        }
        private void RemoveSelectedController()
        {
            if (MessageBox.Show("Do you really want to delete " + listBoxT1_ControllerList.SelectedItem.ToString() + "?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                for (int i = 0; i < trainConfig.Count; i++)
                {
                    string[] singleTrain = trainConfig[i];
                    if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.controllerName] == listBoxT1_ControllerList.SelectedItem.ToString())
                    {
                        trainConfig.RemoveAt(i);
                    }
                }
                listBoxT1_ControllerList.Items.Remove(listBoxT1_ControllerList.SelectedItem);
                ReglerSpeichern(true);
                panel_Regler.Enabled = false;
                btnT1_Controller_Remove.Enabled = false;
            }
        }
        private void btnR_Speichern_Click(object sender, EventArgs e)
        {
            ReglerSpeichern();
            configIsBeeingChanged = 0;
        }
        private void ReglerSpeichern(bool justWriteFile = false)
        {
            if (!justWriteFile)
            {
                bool ok = true;
                #region Eingabeüberprüfung
                if (txtR_JoyAchse.Text == "" && txtR_JoyNr.Text == "" && txtR_AnzahlStufen.Text == "" && txtR_InputUmrechnen.Text == "" && txtR_Zeitfaktor.Text == "" && txtR_LongPress.Text == "" && txtR_Sonderfaelle.Text == "")
                {
                    ok = false;
                }
                else if (txtR_JoyAchse.Text.Contains(",") || txtR_JoyNr.Text.Contains(",") || txtR_AnzahlStufen.Text.Contains(",") || txtR_InputUmrechnen.Text.Contains(",") || txtR_Zeitfaktor.Text.Contains(",") || txtR_LongPress.Text.Contains(",") || txtR_Sonderfaelle.Text.Contains(","))
                {
                    ok = false;
                    MessageBox.Show("You are not allowed to enter a comma! That would break your config, so don't try to work around it :)");
                }
                else
                {
                    if (!(radioR_Stufen.Checked || radioR_Stufenlos.Checked))
                    {
                        ok = false;
                        MessageBox.Show("Please select \"" + radioR_Stufenlos.Text + "\" or \"" + radioR_Stufen.Text + "\"");
                    }
                    txtR_JoyNr.Text = "0";
                    if (txtR_JoyNr.Text == "" || !txtR_JoyNr.Text.All(char.IsDigit))
                    {
                        ok = false;
                        MessageBox.Show("Error with Joy no. Try to enter some random number.");
                    }
                    txtR_JoyAchse.Text = "FakeAxis";
                    if (txtR_JoyAchse.Text == "")
                    {
                        ok = false;
                        MessageBox.Show("Error with Joy-Axis. Try to enter some random stuff.");
                    }
                    if (radioR_Stufen.Checked && (!txtR_AnzahlStufen.Text.All(char.IsDigit) || txtR_AnzahlStufen.Text == ""))
                    {
                        ok = false;
                        MessageBox.Show("Error with Number of notches");
                    }
                    if (txtR_InputUmrechnen.Text != "" && (txtR_InputUmrechnen.Text.Any(char.IsLetter) || txtR_InputUmrechnen.Text.Split(' ').Count() + 1 != txtR_InputUmrechnen.Text.Split('=').Count()))
                    {
                        //txtT3_JoyUmrechnen.Text != "" weil es leer sein darf
                        ok = false;
                        MessageBox.Show("Error with Reassign joy states");
                    }
                    if (txtR_Sonderfaelle.Text != "" && (txtR_Sonderfaelle.Text.Split(' ').Count() + 1 != txtR_Sonderfaelle.Text.Split('=').Count()))
                    {
                        //txtT3_Sonderfaelle.Text != "" weil es leer sein darf
                        ok = false;
                        MessageBox.Show("Error with Convert special cases");
                    }
                    if (txtR_Zeitfaktor.Text == "" || txtR_Zeitfaktor.Text.Any(char.IsLetter) || txtR_Zeitfaktor.Text.Contains("-"))
                    {
                        ok = false;
                        MessageBox.Show("Error with Time factor");
                    }
                    if (txtR_LongPress.Text != "" && (txtR_LongPress.Text.Split(' ').Count() + 1 != txtR_LongPress.Text.Split(':').Count() && !txtR_LongPress.Text.Contains("|")))
                    {
                        //txtT3_Zeitfaktor.Text != "" weil es leer sein darf
                        ok = false;
                        MessageBox.Show("Error with Long press");
                    }
                    txtR_LongPress.Text = txtR_LongPress.Text.Replace("=", ":");
                }
                #endregion

                if (ok)
                {
                    bool bereitsVorhanden = false;
                    for (int i = 0; i < trainConfig.Count; i++)
                    {
                        string[] singleTrain = trainConfig[i];
                        if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.controllerName] == selectedRegler)
                        {
                            bereitsVorhanden = true;

                            singleTrain[ConfigConsts.trainName] = selectedTrain;
                            singleTrain[ConfigConsts.joystickNumber] = txtR_JoyNr.Text;
                            singleTrain[ConfigConsts.joystickInput] = txtR_JoyAchse.Text;
                            singleTrain[ConfigConsts.steps] = txtR_AnzahlStufen.Text;
                            singleTrain[ConfigConsts.timeFactor] = txtR_Zeitfaktor.Text;
                            if (txtR_InputUmrechnen.Text.Length >= 3) { singleTrain[ConfigConsts.inputConvert] = "[" + txtR_InputUmrechnen.Text.Replace(" ", "][") + "]"; } else { singleTrain[ConfigConsts.inputConvert] = ""; }
                            if (txtR_Sonderfaelle.Text.Length >= 3) { singleTrain[ConfigConsts.specials] = "[" + txtR_Sonderfaelle.Text.Replace(" ", "][").Replace("_", " ") + "]"; } else { singleTrain[ConfigConsts.specials] = ""; }
                            if (txtR_LongPress.Text.Length >= 3) { singleTrain[ConfigConsts.longPress] = "[" + txtR_LongPress.Text.Replace(" ", "][") + "]"; } else { singleTrain[ConfigConsts.longPress] = ""; }
                            if (radioR_Stufen.Checked) { singleTrain[ConfigConsts.type] = "Stufen"; } else { singleTrain[ConfigConsts.type] = "Stufenlos"; }

                            string dataGridString = "";
                            if (dataGridView1.Rows.Count > 1)
                            {
                                foreach (DataGridViewRow row in dataGridView1.Rows)
                                {
                                    foreach (DataGridViewCell cell in row.Cells)
                                    {
                                        if (cell.Value != null)
                                        {
                                            dataGridString += cell.Value.ToString() + "|";
                                        }
                                    }
                                }
                                dataGridString = dataGridString.Remove(dataGridString.Length - 1, 1);
                            }
                            singleTrain[ConfigConsts.invert] = dataGridString;
                            trainConfig[i] = singleTrain;
                        }
                    }
                    if (!bereitsVorhanden)
                    {
                        string[] singleTrain = new string[ConfigConsts.arrayLength];
                        singleTrain[ConfigConsts.trainName] = selectedTrain;
                        singleTrain[ConfigConsts.controllerName] = selectedRegler;
                        singleTrain[ConfigConsts.joystickNumber] = txtR_JoyNr.Text;
                        singleTrain[ConfigConsts.joystickInput] = txtR_JoyAchse.Text;
                        singleTrain[ConfigConsts.steps] = txtR_AnzahlStufen.Text;
                        singleTrain[ConfigConsts.timeFactor] = txtR_Zeitfaktor.Text;
                        if (txtR_InputUmrechnen.Text.Length >= 3) { singleTrain[ConfigConsts.inputConvert] = "[" + txtR_InputUmrechnen.Text.Replace(" ", "][") + "]"; } else { singleTrain[ConfigConsts.inputConvert] = ""; }
                        if (txtR_Sonderfaelle.Text.Length >= 3) { singleTrain[ConfigConsts.specials] = "[" + txtR_Sonderfaelle.Text.Replace(" ", "][").Replace("_", " ") + "]"; } else { singleTrain[ConfigConsts.specials] = ""; }
                        if (txtR_LongPress.Text.Length >= 3) { singleTrain[ConfigConsts.longPress] = "[" + txtR_LongPress.Text.Replace(" ", "][") + "]"; } else { singleTrain[ConfigConsts.longPress] = ""; }
                        if (radioR_Stufen.Checked) { singleTrain[ConfigConsts.type] = "Stufen"; } else { singleTrain[ConfigConsts.type] = "Stufenlos"; }

                        string dataGridString = "";
                        if (dataGridView1.Rows.Count > 1)
                        {
                            foreach (DataGridViewRow row in dataGridView1.Rows)
                            {
                                foreach (DataGridViewCell cell in row.Cells)
                                {
                                    if (cell.Value != null)
                                    {
                                        dataGridString += cell.Value.ToString() + "|";
                                    }
                                }
                            }
                            dataGridString = dataGridString.Remove(dataGridString.Length - 1, 1);
                        }
                        singleTrain[ConfigConsts.invert] = dataGridString;
                        trainConfig.Add(singleTrain);
                    }
                }
            }

            string[] line = new string[trainConfig.Count];
            for (int i = 0; i < trainConfig.Count; i++)
            {
                string combined = "";
                foreach (string s in trainConfig[i])
                {
                    combined += s + ",";
                }
                combined = combined.Remove(combined.Length - 1);
                line[i] = combined;
            }

            File.WriteAllLines(ConfigConsts.configPath, line);

        }
        private void btnT1_editController_Click(object sender, EventArgs e)
        {
            resetControllerBearbeiten();
            tabControl_main.SelectedIndex = 2;
            if (listBoxT1_ControllerList.SelectedItem != null)
            {
                if (comboBoxT2_Reglerauswahl.Items.Contains(listBoxT1_ControllerList.SelectedItem))
                {
                    comboBoxT2_Reglerauswahl.SelectedItem = listBoxT1_ControllerList.SelectedItem;
                }
            }
        }
        #endregion

        #region Knöpfe
        private void comboBoxB_KnopfAuswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            foreach (string[] singleTrain in trainConfig)
            {
                if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.description] == comboBoxB_KnopfAuswahl.Text)
                {
                    txtB_JoystickNr.Text = singleTrain[ConfigConsts.joystickNumber];
                    txtB_JoystickKnopf.Text = singleTrain[ConfigConsts.joystickInput];
                    txtB_Aktion.Text = singleTrain[ConfigConsts.action];
                    txtB_Tastenkombination.Text = singleTrain[ConfigConsts.keyCombination];
                    txtB_Bedingung.Text = singleTrain[ConfigConsts.inputType].Replace("Button", "").Replace("[", "").Replace("]", " ").TrimEnd(' ');

                    if (singleTrain[ConfigConsts.inputType].Contains("["))
                    {
                        radioB_regler.Checked = true;
                        radioB_normal.Checked = false;
                    }
                    else
                    {
                        radioB_regler.Checked = false;
                        radioB_normal.Checked = true;
                    }
                }
            }
        }
        private void radioB_CheckedChanged(object sender, EventArgs e)
        {
            if (radioB_normal.Checked)
            {
                lblB_Bedingung.Hide();
                txtB_Bedingung.Hide();
                lblR_KnopfNr.Text = "Button no.";
            }
            else
            {
                lblB_Bedingung.Show();
                txtB_Bedingung.Show();
                lblR_KnopfNr.Text = "Joyname";
            }
        }
        private void btnB_entfernen_Click(object sender, EventArgs e)
        {
            if (comboBoxB_KnopfAuswahl.SelectedItem != null)
            {
                for (int i = 0; i < trainConfig.Count; i++)
                {
                    string[] singleTrain = trainConfig[i];
                    if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.description] == comboBoxB_KnopfAuswahl.SelectedItem.ToString())
                    {
                        trainConfig.RemoveAt(i);
                        comboBoxB_KnopfAuswahl.Items.Remove(comboBoxB_KnopfAuswahl.SelectedItem);
                        break;
                    }
                }
                Buttons_Speichern(true);
                ResetKonfiguration();
            }
        }
        private void btnB_Speichern_Click(object sender, EventArgs e)
        {
            Buttons_Speichern();
            comboBoxB_KnopfAuswahl.Items.Add(comboBoxB_KnopfAuswahl.Text);
        }
        private void Buttons_Speichern(bool justWriteFile = false)
        {
            if (!justWriteFile)
            {
                bool ok = true;
                #region Eingabeüberprüfung
                if (comboBoxB_KnopfAuswahl.Text.Contains(",") || txtB_Aktion.Text.Contains(",") || txtB_Bedingung.Text.Contains(",") || txtB_JoystickKnopf.Text.Contains(",") || txtB_Tastenkombination.Text.Contains(","))
                {
                    ok = false;
                    MessageBox.Show("You are not allowed to enter a comma! That would break your config, so don't try to work around it :)");
                }
                else
                {
                    if (comboBoxB_KnopfAuswahl.Text == "")
                    {
                        ok = false;
                        MessageBox.Show("No name entered");
                    }
                    if (txtB_JoystickNr.Text == "" || !txtB_JoystickNr.Text.All(char.IsDigit))
                    {
                        ok = false;
                        MessageBox.Show("Error with Joy no.");
                    }
                    txtB_JoystickKnopf.Text = "69";
                    if (txtB_JoystickKnopf.Text == "")
                    {
                        ok = false;
                        if (radioB_normal.Checked)
                        {
                            MessageBox.Show("Error with Button-no.");
                        }
                        else
                        {
                            MessageBox.Show("Error with Joyname");
                        }
                    }
                    if (txtB_Bedingung.Text != "" && (!(txtB_Bedingung.Text.Contains("<") || txtB_Bedingung.Text.Contains(">") || txtB_Bedingung.Text.Contains("=")) || txtB_Bedingung.Text.Any(char.IsLetter)))
                    {
                        //txtB_Bedingung.Text != "" weil es leer sein darf
                        ok = false;
                        MessageBox.Show("Error with Condition");
                    }
                    if (txtB_Aktion.Text == "" && txtB_Tastenkombination.Text == "")
                    {
                        ok = false;
                        MessageBox.Show("No action or keyboard shortcut");
                    }
                    if (txtB_Tastenkombination.Text != "" && !(txtB_Tastenkombination.Text.Split('_').Count() == 3 || txtB_Tastenkombination.Text.Split('_').Count() % 3 == 0))
                    {
                        ok = false;
                        MessageBox.Show("Error with keyboard shortcut");
                    }
                }
                #endregion

                if (ok)
                {
                    bool bereitsVorhanden = false;
                    for (int i = 0; i < trainConfig.Count; i++)
                    {
                        string[] singleTrain = trainConfig[i];
                        if (singleTrain[ConfigConsts.trainName] == selectedTrain && singleTrain[ConfigConsts.description] == comboBoxB_KnopfAuswahl.Text)
                        {
                            bereitsVorhanden = true;

                            singleTrain[ConfigConsts.joystickNumber] = txtB_JoystickNr.Text;
                            if (radioB_normal.Checked) { singleTrain[ConfigConsts.inputType] = "Button"; } else { singleTrain[ConfigConsts.inputType] = "Button[" + txtB_Bedingung.Text.Replace(" ", "][") + "]"; }
                            if (radioB_normal.Checked) { singleTrain[ConfigConsts.joystickInput] = txtB_JoystickKnopf.Text; } else { singleTrain[ConfigConsts.joystickInput] = txtB_JoystickKnopf.Text; }
                            singleTrain[ConfigConsts.action] = txtB_Aktion.Text;
                            singleTrain[ConfigConsts.keyCombination] = txtB_Tastenkombination.Text;
                            trainConfig[i] = singleTrain;
                        }
                    }

                    if (!bereitsVorhanden)
                    {
                        string[] singleTrain = new string[trainConfig[0].Count()];
                        singleTrain[ConfigConsts.trainName] = selectedTrain;
                        singleTrain[ConfigConsts.description] = comboBoxB_KnopfAuswahl.Text.ToString();
                        singleTrain[ConfigConsts.joystickNumber] = txtB_JoystickNr.Text;
                        if (radioB_normal.Checked) { singleTrain[ConfigConsts.inputType] = "Button"; } else { singleTrain[ConfigConsts.inputType] = "Button[" + txtB_Bedingung.Text.Replace(" ", "][") + "]"; }
                        if (radioB_normal.Checked) { singleTrain[ConfigConsts.joystickInput] = "B" + txtB_JoystickKnopf.Text; } else { singleTrain[ConfigConsts.joystickInput] = txtB_JoystickKnopf.Text; }
                        singleTrain[ConfigConsts.action] = txtB_Aktion.Text;
                        singleTrain[ConfigConsts.keyCombination] = txtB_Tastenkombination.Text;
                        trainConfig.Add(singleTrain);
                    }
                }
            }
            string[] line = new string[trainConfig.Count];
            for (int i = 0; i < trainConfig.Count; i++)
            {
                string combined = "";
                foreach (string s in trainConfig[i])
                {
                    combined += s + ",";
                }
                combined = combined.Remove(combined.Length - 1);
                line[i] = combined;
            }

            File.WriteAllLines(ConfigConsts.configPath, line);
        }
        private void btnB_Editor_Click(object sender, EventArgs e)
        {
            resetTastenkombination();

            if (txtB_Tastenkombination.Text != "" && (txtB_Tastenkombination.Text.Split('_').Count() == 3 || txtB_Tastenkombination.Text.Split('_').Count() % 3 == 0))
            {
                string[] splitted = txtB_Tastenkombination.Text.Split('_');

                for (int i = 0; i < splitted.Count(); i += 3)
                {
                    tastenkombiliste.Add(splitted[i] + "_" + splitted[i + 1] + "_" + splitted[i + 2]);


                    splitted[i + 1] = splitted[i + 1].Replace("[", "").Replace("]", "");
                    splitted[i + 2] = splitted[i + 2].Replace("[", "").Replace("]", "");

                    if (splitted[i + 1].Contains("press")) { listBoxT3_Output.Items.Add(splitted[i] + " short press, then wait " + splitted[i + 2] + "ms"); }
                    if (splitted[i + 1].Contains("hold")) { listBoxT3_Output.Items.Add("hold " + splitted[i] + " for " + splitted[i + 1].Replace("hold", "") + "ms, then wait " + splitted[i + 2] + "ms"); }
                    if (splitted[i + 1].Contains("down")) { listBoxT3_Output.Items.Add("hold down " + splitted[i] + ", then wait " + splitted[i + 2] + "ms"); }
                    if (splitted[i + 1].Contains("up")) { listBoxT3_Output.Items.Add("release " + splitted[i] + ", then wait " + splitted[i + 2] + "ms"); }
                }
            }

            tabControl_main.SelectedIndex = 3;
        }
        #region Tastenkombination
        List<string> tastenkombiliste = new List<string>();
        private void resetTastenkombination()
        {
            listBoxT3_Output.Items.Clear();
            txtT3_Haltezeit.Text = "0";
            txtT3_Taste.Text = "";
            txtT3_Wartezeit.Text = "10";
            radioT3_einmalDruecken.Checked = true;
            lblT3_haltezeit.Hide();
            txtT3_Haltezeit.Hide();

            tastenkombiliste.Clear();
        }
        private void btnT3_Hinzufügen_Click(object sender, EventArgs e)
        {
            int insertIndex = listBoxT3_Output.SelectedIndex + 1;
            if (insertIndex == 0)
            {
                insertIndex = listBoxT3_Output.Items.Count;
            }


            if (txtT3_Taste.Text != "")
            {
                if (txtT3_Wartezeit.Text == "") { txtT3_Wartezeit.Text = "0"; }
                if (txtT3_Haltezeit.Text == "") { txtT3_Haltezeit.Text = "0"; }

                if (radioT3_einmalDruecken.Checked)
                {
                    tastenkombiliste.Insert(insertIndex, txtT3_Taste.Text + "_[press]_[" + txtT3_Wartezeit.Text + "]");
                    listBoxT3_Output.Items.Insert(insertIndex, txtT3_Taste.Text + " short press, then wait " + txtT3_Wartezeit.Text + "ms");
                }
                else if (radioT3_Halten.Checked)
                {
                    tastenkombiliste.Insert(insertIndex, txtT3_Taste.Text + "_[hold" + txtT3_Haltezeit.Text + "]_[" + txtT3_Wartezeit.Text + "]");
                    listBoxT3_Output.Items.Insert(insertIndex, "hold " + txtT3_Taste.Text + " for " + txtT3_Haltezeit.Text + "ms, then wait " + txtT3_Wartezeit.Text + "ms");
                }
                else if (radioT3_Druecken.Checked)
                {
                    tastenkombiliste.Insert(insertIndex, txtT3_Taste.Text + "_[down]_[" + txtT3_Wartezeit.Text + "]");
                    listBoxT3_Output.Items.Insert(insertIndex, "press " + txtT3_Taste.Text + " down, then wait " + txtT3_Wartezeit.Text + "ms");
                }
                else if (radioT3_Loslassen.Checked)
                {
                    tastenkombiliste.Insert(insertIndex, txtT3_Taste.Text + "_[up]_[" + txtT3_Wartezeit.Text + "]");
                    listBoxT3_Output.Items.Insert(insertIndex, "release " + txtT3_Taste.Text + ", then wait " + txtT3_Wartezeit.Text + "ms");
                }
                listBoxT3_Output.SelectedIndex = insertIndex;
                radioT3_einmalDruecken.Checked = true;
            }
            else
            {
                MessageBox.Show("No key");
            }
        }
        private void btnT3_Fertig_Click(object sender, EventArgs e)
        {
            string combined = "";
            if (tastenkombiliste.Count != 0)
            {
                foreach (string single in tastenkombiliste)
                {
                    combined += single + "_";
                }
                combined = combined.Remove(combined.Length - 1, 1);
            }
            txtB_Tastenkombination.Text = combined;
            tabControl_main.SelectedIndex = 1;
        }
        private void listBoxT3_Output_KeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.KeyCode == Keys.Delete)
                {
                    int index = listBoxT3_Output.SelectedIndex;
                    tastenkombiliste.RemoveAt(index);
                    listBoxT3_Output.Items.RemoveAt(index);
                    if (listBoxT3_Output.Items.Count > 0)
                    {
                        listBoxT3_Output.SelectedIndex = index - 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ErrorException(ex);
                MessageBox.Show("ERROR");
            }
        }
        private void radio_Changed(object sender, EventArgs e)
        {
            if (radioT3_Halten.Checked)
            {
                lblT3_haltezeit.Show();
                txtT3_Haltezeit.Show();
            }
            else
            {
                lblT3_haltezeit.Hide();
                txtT3_Haltezeit.Hide();
            }
        }
        #endregion
        #endregion

        #endregion

        #region Regler bearbeiten
        private void resetControllerBearbeiten(bool fullReset = true)
        {

            txtT2_increase.Text = "";
            txtT2_decrease.Text = "";
            comboBoxT2_mainIndicator.Text = "";
            comboBoxT2_brakearea.Text = "";
            comboBoxT2_throttlearea.Text = "";
            comboBoxT2_mainIndicator.Items.Clear();
            comboBoxT2_brakearea.Items.Clear();
            comboBoxT2_throttlearea.Items.Clear();
            checkboxT2_Kombihebel.Checked = false;

            if (fullReset)
            {
                virtualControllerList.Clear();
                comboBoxT2_Reglerauswahl.Text = "";
                comboBoxT2_Reglerauswahl.Items.Clear();
                if (File.Exists(ConfigConsts.controllersConfigPath))
                {
                    using (var reader = new StreamReader(ConfigConsts.controllersConfigPath))
                    {
                        bool skipFirst = true;
                        while (!reader.EndOfStream)
                        {
                            var line = reader.ReadLine();
                            string[] values = line.Split(',');
                            if (!skipFirst)
                            {
                                VirtualController vc = new VirtualController();
                                vc.InsertFileArray(values);

                                virtualControllerList.Add(vc);
                                comboBoxT2_Reglerauswahl.Items.Add(values[0]);
                            }
                            else
                            {
                                skipFirst = false;
                            }
                        }
                    }
                }
            }
        }
        private void comboBoxT2_Reglerauswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            resetControllerBearbeiten(false);
            string selection = comboBoxT2_Reglerauswahl.SelectedItem.ToString();

            foreach (VirtualController singleController in virtualControllerList)
            {
                if (selection == singleController.name)
                {
                    txtT2_increase.Text = singleController.increaseKey;
                    txtT2_decrease.Text = singleController.decreaseKey;

                    comboBoxT2_mainIndicator.Items.AddRange(singleController.mainIndicators);
                    comboBoxT2_throttlearea.Items.AddRange(singleController.textindicators_throttlearea);
                    comboBoxT2_brakearea.Items.AddRange(singleController.textindicators_brakearea);
                    checkboxT2_Kombihebel.Checked = singleController.isMasterController;
                }
            }
            panel_main.Enabled = true;
        }
        private void comboBoxT2_Reglerauswahl_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ',')
            {
                e.Handled = true;
            }
            panel_main.Enabled = false;
        }
        private void btnT2_add_Click(object sender, EventArgs e)
        {
            ComboBox cb = comboBoxT2_Reglerauswahl;
            if (!cb.Items.Contains(cb.Text))
            {
                cb.Items.Add(cb.Text);
                panel_main.Enabled = true;
            }
            resetControllerBearbeiten(false);
        }
        private void btnT2_remove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to remove " + comboBoxT2_Reglerauswahl.Text + "?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                ComboBox cb = comboBoxT2_Reglerauswahl;
                if (cb.Items.Contains(cb.Text))
                {
                    for (int i = 0; i < virtualControllerList.Count; i++)
                    {
                        if (virtualControllerList[i].name == cb.Text)
                        {
                            virtualControllerList.RemoveAt(i);
                        }
                    }
                    cb.Items.Remove(cb.Text);

                    //Schreibe Datei
                    string[] line = new string[virtualControllerList.Count + 1];
                    line[0] = VirtualController.firstLine;
                    for (int i = 1; i < virtualControllerList.Count + 1; i++)
                    {
                        VirtualController vc = virtualControllerList[i - 1];

                        string combined = vc.combineToString();

                        line[i] = combined;
                    }

                    File.WriteAllLines(ConfigConsts.controllersConfigPath, line);

                    ComboBox cb2 = comboBoxT2_Reglerauswahl;
                    if (!cb2.Items.Contains(cb2.Text))
                    {
                        cb2.Items.Add(cb2.Text);
                    }
                    resetControllerBearbeiten();
                }
                panel_main.Enabled = false;
            }
        }
        private void txt_Aktion_KeyDown(object sender, KeyEventArgs e)
        {
            //Verhindert, dass die gedrückte Taste ins Textfeld geschrieben wird
            e.SuppressKeyPress = true;
        }
        private void txt_Aktion_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //Wenn man im "Aktion" Feld eine Taste drückt finde passenden Namen zur Taste
            //PreviewKeyDown um auch tab-Taste zu erlauben
            ((TextBox)sender).Text = Keyboard.ConvertKeyToString(e.KeyCode);
            if (((TextBox)sender).Name != "txtT3_Taste")
            {
                SelectNextControl((Control)sender, true, false, true, true);
            }
        }
        private void txt_Aktion_MouseDown(object sender, MouseEventArgs e)
        {
            ((TextBox)sender).Text = "";
        }
        private void comboBoxT2_Indicators_KeyPress(object sender, KeyPressEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (e.KeyChar == (char)Keys.Enter)
            {
                //add
                if (!cb.Items.Contains(cb.Text) && cb.Text != "")
                {
                    cb.Items.Add(cb.Text);
                    cb.Text = "";
                }
            }
            if (e.KeyChar == ',')
            {
                e.Handled = true;
            }
        }
        private void comboBoxT2_Indicators_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (MessageBox.Show("Do you want to REMOVE " + cb.Text + "?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                cb.Items.Remove(cb.Text);
            }
        }
        private void checkboxT2_Combined_CheckedChanged(object sender, EventArgs e)
        {
            if (checkboxT2_Kombihebel.Checked)
            {
                groupBox_kombihebel.Enabled = true;
            }
            else
            {
                groupBox_kombihebel.Enabled = false;
            }
        }
        private void btnT2_Save_Click(object sender, EventArgs e)
        {
            void checkCombobox(ComboBox comboBox)
            {
                if (comboBox.SelectedItem == null && comboBox.Text != "")
                {
                    comboBox.Items.Add(comboBox.Text);
                }
            }

            checkCombobox(comboBoxT2_mainIndicator);
            checkCombobox(comboBoxT2_throttlearea);
            checkCombobox(comboBoxT2_brakearea);

            if (txtT2_increase.Text != "" && txtT2_decrease.Text != "")
            {
                VirtualController vc = new VirtualController();
                vc.name = comboBoxT2_Reglerauswahl.Text;
                vc.increaseKey = txtT2_increase.Text;
                vc.decreaseKey = txtT2_decrease.Text;
                vc.isMasterController = checkboxT2_Kombihebel.Checked;
                vc.mainIndicators = comboBoxT2_mainIndicator.Items.Cast<Object>().Select(item => item.ToString()).ToArray();
                if (vc.isMasterController)
                {
                    vc.textindicators_throttlearea = comboBoxT2_throttlearea.Items.Cast<Object>().Select(item => item.ToString()).ToArray();
                    vc.textindicators_brakearea = comboBoxT2_brakearea.Items.Cast<Object>().Select(item => item.ToString()).ToArray();
                }
                else
                {
                    vc.textindicators_throttlearea = new string[0];
                    vc.textindicators_brakearea = new string[0];
                }

                bool aleardyExists = false;
                for (int i = 0; i < virtualControllerList.Count; i++)
                {
                    if (virtualControllerList[i].name == comboBoxT2_Reglerauswahl.Text)
                    {
                        aleardyExists = true;
                        virtualControllerList[i] = vc;

                        break;
                    }
                }
                if (!aleardyExists)
                {
                    virtualControllerList.Add(vc);
                }
            }

            //Schreibe Datei
            string[] line = new string[virtualControllerList.Count + 1];
            line[0] = VirtualController.firstLine;
            for (int i = 1; i < virtualControllerList.Count + 1; i++)
            {
                VirtualController vc = virtualControllerList[i - 1];

                string combined = vc.combineToString();

                line[i] = combined;
            }

            File.WriteAllLines(ConfigConsts.controllersConfigPath, line);

            ComboBox cb = comboBoxT2_Reglerauswahl;
            if (!cb.Items.Contains(cb.Text))
            {
                cb.Items.Add(cb.Text);
            }
            resetControllerBearbeiten();

            ReadControllersFile();

            if (selectedTrain != "")
            {
                tabControl_main.SelectedIndex = 1;
            }
            else
            {
                tabControl_main.SelectedIndex = 0;
            }
        }
        private void btnT2_back_Click(object sender, EventArgs e)
        {
            if (selectedTrain != "")
            {
                resetControllerBearbeiten(true);
                tabControl_main.SelectedIndex = 1;
            }
            else
            {
                tabControl_main.SelectedIndex = 0;
            }
        }
        private void btnT2_defaultSettings_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Do you really want to reset all controllers to default settings?", "", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                File.Copy(ConfigConsts.controllersStandardPath, ConfigConsts.controllersConfigPath, true);
                Log.Add("Copy :" + ConfigConsts.controllersStandardPath + " to " + ConfigConsts.controllersConfigPath);
                resetControllerBearbeiten();
            }
        }
        #endregion
    }
}
