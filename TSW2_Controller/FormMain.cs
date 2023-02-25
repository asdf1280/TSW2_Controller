using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Tesseract;
using TSW2_Controller.Properties;

namespace TSW2_Controller {
    public partial class FormMain : Form {
        public static Rectangle res = Screen.PrimaryScreen.Bounds;

        public List<string[]> trainConfig = new List<string[]>();
        List<VirtualController> vControllerList = new List<VirtualController>();

        List<string[]> activeTrain = new List<string[]>();
        List<VirtualController> activeVControllers = new List<VirtualController>();

        public List<string> trainNames = new List<string>();

        static TesseractEngine OCREngine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);

        public static string[] axisNames = { "JoyX", "JoyY", "JoyZ", "pov", "RotX", "RotY", "RotZ", "Sldr" };

        bool isDeactivatedGlobally = false;

        public string selectedTrain = "";

        // TSWOCR Values
        public static TcpListener server;
        public static bool controlHoldNeeded = true;
        public static int desiredThrustPercent = 0;
        public static int desiredBrakePercent = 0;

        public FormMain() {
            checkVersion();

            Log.Add("Init components");
            InitializeComponent();

            // Improves single line reading performance
            OCREngine.DefaultPageSegMode = PageSegMode.SingleLine;

            Log.Add("Check Files");
            #region File structure checking
            try {
                if (!File.Exists(ConfigConsts.configPath)) {
                    if (!Directory.Exists(ConfigConsts.configPath.Replace(@"\Trainconfig.csv", ""))) {
                        Log.Add("Create Dir:" + ConfigConsts.configPath.Replace(@"\Trainconfig.csv", ""));
                        Directory.CreateDirectory(ConfigConsts.configPath.Replace(@"\Trainconfig.csv", ""));
                    }
                    if (File.Exists(ConfigConsts.configStandardPath)) {
                        Log.Add("No TrainConfig.csv");
                        Log.Add("Copy :" + ConfigConsts.configStandardPath + " to " + ConfigConsts.configPath);
                        File.Copy(ConfigConsts.configStandardPath, ConfigConsts.configPath, false);
                    }
                }
                if (!File.Exists(ConfigConsts.controllersConfigPath)) {
                    Log.Add("Copy :" + ConfigConsts.controllersStandardPath + " to " + ConfigConsts.controllersConfigPath);
                    File.Copy(ConfigConsts.controllersStandardPath, ConfigConsts.controllersConfigPath, false);
                }
                if (!Directory.Exists(ConfigConsts.configFolderPath)) {
                    Log.Add("Create Dir:" + ConfigConsts.configFolderPath);
                    Directory.CreateDirectory(ConfigConsts.configFolderPath);
                }
                if (Settings.Default.selectedTrainConfig == "_Standard") {
                    Log.Add("Copy:" + ConfigConsts.configStandardPath + " to " + ConfigConsts.configPath);
                    File.Copy(ConfigConsts.configStandardPath, ConfigConsts.configPath, true);
                } else {
                    if (File.Exists(ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv")) {
                        Log.Add("Copy:" + ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv" + " to " + ConfigConsts.configPath);
                        File.Copy(ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv", ConfigConsts.configPath, true);
                    } else {
                        Log.Add("Copy:" + ConfigConsts.configPath + " to " + ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv");
                        File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + Settings.Default.selectedTrainConfig + ".csv", true);
                    }
                }
            } catch (Exception ex) {
                Log.ErrorException(ex);
                Close();
            }
            #endregion

            lbl_originalResult.Text = "";
            lbl_alternativeResult.Text = "";
            groupBox_ScanResults.Hide();

            Keyboard.initKeylist();

            loadSettings();

            ReadVControllers();
            ReadTrainConfig();


            timer_CheckSticks.Start();

            // TSWOCR Server Socket
            var t = new Thread(() => {
                if (server != null) return;
                server = new TcpListener(IPAddress.Loopback, 4776);
                server.Start();

                while (true) {
                    var c = server.AcceptTcpClient();

                    var t2 = new Thread(() => {
                        var r = new BinaryReader(c.GetStream());
                        try {
                            while (true) {
                                var input = r.ReadString();
                                if (input == "r") {
                                    controlHoldNeeded = true;
                                    desiredBrakePercent = 0;
                                    desiredThrustPercent = 0;
                                } else if (input.StartsWith("a")) {
                                    controlHoldNeeded = false;
                                    desiredBrakePercent = 0;
                                    desiredThrustPercent = int.Parse(input.Substring(1));
                                } else if (input.StartsWith("b")) {
                                    controlHoldNeeded = false;
                                    desiredThrustPercent = 0;
                                    desiredBrakePercent = int.Parse(input.Substring(1));
                                } else if (input == "y") {
                                    Keyboard.HoldKey(Keys.A, 100);
                                } else if (input == "u") {
                                    Keyboard.HoldKey(Keys.U, 100);
                                }
                            }
                        } catch {
                            c.Close();
                            c.Dispose();
                        }
                    });
                    t2.IsBackground = true;
                    t2.Start();
                }
            });
            t.IsBackground = true;
            t.Start();
        }

        #region UI
        #region MainTimer
        private void timer_CheckSticks_Tick(object sender, EventArgs e) {
            Main();
        }
        #endregion
        #region Train Selection
        private void comboBox_Zugauswahl_SelectedIndexChanged(object sender, EventArgs e) {
            selectedTrain = combobox_trainSelection.Text;
            check_active.Checked = false;
            Log.Add(combobox_trainSelection.Text + " selected");

            if (Settings.Default.deactivatedGlobals.Count > 0) {
                if (Settings.Default.deactivatedGlobals.Contains(combobox_trainSelection.Text)) {
                    check_deactivateGlobal.CheckedChanged -= check_deactivateGlobal_CheckedChanged;
                    check_deactivateGlobal.Checked = true;
                    check_deactivateGlobal.CheckedChanged += check_deactivateGlobal_CheckedChanged;

                    isDeactivatedGlobally = true;
                    Log.Add("_global is deactivated", false, 1);
                } else {
                    check_deactivateGlobal.CheckedChanged -= check_deactivateGlobal_CheckedChanged;
                    check_deactivateGlobal.Checked = false;
                    check_deactivateGlobal.CheckedChanged += check_deactivateGlobal_CheckedChanged;

                    isDeactivatedGlobally = false;
                }
            }

            ReadVControllers();//Merken: Diese Zeile ist ein "dreckiger" fix dafür, dass bei getActiveVControllers() die foreachloop eine reference copy von vControllerList macht und somit werte in dieser Liste speichert, obwohl diese nur gelesen werden sollte
            getActiveTrain();
            getActiveVControllers();
        }
        #endregion
        #region Active Checkbox
        private void check_active_CheckedChanged(object sender, EventArgs e) {
            if (check_active.Checked) {
                check_active.BackColor = Color.Lime;
                Log.Add("----------------------------------------------------------------------------------------------------");
                Log.Add("----------------------------------------------------------------------------------------------------");
                Log.Add("Active = true");
                Log.Add("Active Train:"); foreach (string[] train in activeTrain) { Log.Add(string.Join(",", train), false, 1); }
                Log.Add("");

                if (activeVControllers.Count > 0) {
                    Log.Add("VControllers:");
                    foreach (VirtualController vc in activeVControllers) {
                        Log.Add(vc.name + ":", false, 1);
                        Log.Add("Main Indicators:", false, 2);
                        foreach (string indicator in vc.mainIndicators) {
                            Log.Add(indicator, false, 3);
                        }

                        Log.Add("Throttle area:", false, 2);
                        foreach (string indicator in vc.textindicators_throttlearea) {
                            Log.Add(indicator, false, 3);
                        }

                        Log.Add("Brake area:", false, 2);
                        foreach (string indicator in vc.textindicators_brakearea) {
                            Log.Add(indicator, false, 3);
                        }
                    }
                } else {
                    Log.Add("No VControllers found");
                }

                Log.Add("");
                Log.Add("KeyLayout:" + string.Join(",", Settings.Default.Tastenbelegung.Cast<string>().ToArray()));
                Log.Add("version:" + "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString().Remove(Assembly.GetExecutingAssembly().GetName().Version.ToString().Length - 2, 2));
                Log.Add("Resolution:" + Settings.Default.res.Width + "x" + Settings.Default.res.Height);
                Log.Add("Language:" + Settings.Default.Language);
                Log.Add("WindowsLanguage:" + InputLanguage.CurrentInputLanguage.Culture.Name);
                Log.Add("KeyboardLayout:" + InputLanguage.CurrentInputLanguage.LayoutName);
                Log.Add("");

                foreach (VirtualController vc in activeVControllers) {
                    if (vc.isStepless) {
                        vc.currentSimValue = vc.currentJoystickValue;
                    } else {
                        vc.currentSimValue = Convert.ToInt32(Math.Round(vc.currentJoystickValue * (Convert.ToDouble(vc.stufen) / 100), 0));
                    }
                    vc.getText = 0;
                }
            } else {
                check_active.BackColor = Color.Red;
                Log.Add("Active = false");
                Log.Add("----------------------------------------------------------------------------------------------------");
                Log.Add("----------------------------------------------------------------------------------------------------");
            }
        }
        #endregion
        #region Deactivate Global
        private void check_deactivateGlobal_CheckedChanged(object sender, EventArgs e) {
            if (check_deactivateGlobal.Checked) {
                Log.Add("Add " + combobox_trainSelection.Text + " to deactivatedGlobals list");
                Settings.Default.deactivatedGlobals.Add(combobox_trainSelection.Text);
                Settings.Default.Save();

                isDeactivatedGlobally = true;
            } else {
                Log.Add("Remove " + combobox_trainSelection.Text + " from deactivatedGlobals list");
                Settings.Default.deactivatedGlobals.Remove(combobox_trainSelection.Text);
                Settings.Default.Save();

                isDeactivatedGlobally = false;
            }
            getActiveTrain();
        }
        #endregion
        #region Settings
        private void btn_einstellungen_Click(object sender, EventArgs e) {
            Log.Add("Going to settings:");
            check_active.Checked = false;
            FormSettings formSettings = new FormSettings(this);
            formSettings.Location = this.Location;
            formSettings.ShowDialog();
            Log.Add("Leaving settings");
            string tmp_selTrain = selectedTrain;

            loadSettings();

            ReadVControllers();
            ReadTrainConfig();

            if (tmp_selTrain != "") {
                if (combobox_trainSelection.Items.Contains(tmp_selTrain)) {
                    combobox_trainSelection.SelectedItem = tmp_selTrain;
                }
            }
        }
        #endregion

        #region FormClose
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e) {

        }
        #endregion
        #endregion

        #region Global functions
        public static bool ContainsWord(string stringToCheck, string word) {
            if (word != null) {
                string[] split_stringTC = stringToCheck.Split(' ');
                int countOfWordsGiven = word.Count(x => x == ' ') + 1;

                if (stringToCheck != "") {
                    for (int i = 0; i < split_stringTC.Length - countOfWordsGiven + 1; i++) {
                        string str = "";
                        for (int o = 0; o < countOfWordsGiven; o++) {
                            str += split_stringTC[o + i] + " ";
                        }
                        str = str.Trim();

                        if (str.ToLower() == word.ToLower()) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public static double GetDamerauLevenshteinDistanceInPercent(string string_to_check, string string_to_check_from, int maxLengthDiff, bool checkCase = false) {
            try {
                if (checkCase == false) { string_to_check = string_to_check.ToLower(); string_to_check_from = string_to_check_from.ToLower(); }

                if (string.IsNullOrEmpty(string_to_check) || Math.Abs(string_to_check.Length - string_to_check_from.Length) > maxLengthDiff) {
                    return 0;
                    //throw new ArgumentNullException(t, "String Cannot Be Null Or Empty");
                }

                if (string.IsNullOrEmpty(string_to_check_from)) {
                    return 0;
                    //throw new ArgumentNullException(t, "String Cannot Be Null Or Empty");
                }

                int n = string_to_check.Length; // length of s
                int m = string_to_check_from.Length; // length of t

                if (n == 0) {
                    return m;
                }

                if (m == 0) {
                    return n;
                }

                int[] p = new int[n + 1]; //'previous' cost array, horizontally
                int[] d = new int[n + 1]; // cost array, horizontally

                // indexes into strings s and t
                int i; // iterates through s
                int j; // iterates through t

                for (i = 0; i <= n; i++) {
                    p[i] = i;
                }

                for (j = 1; j <= m; j++) {
                    char tJ = string_to_check_from[j - 1]; // jth character of t
                    d[0] = j;

                    for (i = 1; i <= n; i++) {
                        int cost = string_to_check[i - 1] == tJ ? 0 : 1; // cost
                                                                         // minimum of cell to the left+1, to the top+1, diagonally left and up +cost                
                        d[i] = Math.Min(Math.Min(d[i - 1] + 1, p[i] + 1), p[i - 1] + cost);
                    }

                    // copy current distance counts to 'previous row' distance counts
                    int[] dPlaceholder = p; //placeholder to assist in swapping p and d
                    p = d;
                    d = dPlaceholder;
                }

                // our last action in the above loop was to switch d and p, so p now 
                // actually has the most recent cost counts
                //return p[n];

                return Convert.ToDouble(m - p[n]) / Convert.ToDouble(m);
            } catch (Exception ex) {
                Log.ErrorException(ex);
                return 0;
            }
        }

        public Bitmap Screenshot(bool normal) {
            //Width and height of the scan frane
            int width = ConvertHeight(513); //ConvertHeight to prevent image strech
            int height = ConvertHeight(30);
            //Starting position from the top frame
            int x1 = ConvertWidth(2560) - ConvertHeight(513) - ConvertHeight(127); //=1920
            int y1 = ConvertHeight(457);
            //Adjusted height for bottom frame
            int y2 = ConvertHeight(530);


            Bitmap bmpScreenshot = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bmpScreenshot);

            if (normal) {
                //upper frame
                g.CopyFromScreen(x1, y1, 0, 0, new Size(width, height));
            } else {
                //lower frame
                g.CopyFromScreen(x1, y2, 0, 0, new Size(width, height));
            }

            for (int y = 0; (y <= (bmpScreenshot.Height - 1)); y++) {
                for (int x = 0; (x <= (bmpScreenshot.Width - 1)); x++) {
                    Color inv = bmpScreenshot.GetPixel(x, y);

                    //Color adjustment to recognize only the writing if possible
                    if (inv.R + inv.G + inv.G < 650) {
                        inv = Color.FromArgb(0, 0, 0, 0);
                    }

                    //invert the image
                    inv = Color.FromArgb(255, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    bmpScreenshot.SetPixel(x, y, inv);
                }
            }

            if (Settings.Default.showScanResult) { if (normal) { bgw_readScreen.ReportProgress(0, new object[] { bmpScreenshot, new Bitmap(1, 1), null, null, -1, -1 }); } else { bgw_readScreen.ReportProgress(0, new object[] { new Bitmap(1, 1), bmpScreenshot, null, null, -1, -1 }); } Thread.Sleep(50); }//Thread.Sleep(50) prevents crash with picturebox
            return bmpScreenshot;
        }
        private static int ConvertHeight(int height) {
            return Convert.ToInt32(Math.Round((Convert.ToDouble(height) / 1440.0) * Convert.ToDouble(res.Height), 0));
        }
        private static int ConvertWidth(int width) {
            return Convert.ToInt32(Math.Round((Convert.ToDouble(width) / 2560.0) * Convert.ToDouble(res.Width), 0));
        }

        public static string GetText(Bitmap imgsource) {
            var ocrtext = string.Empty;
            //engine.SetVariable("load_system_dawg", true);
            //engine.SetVariable("language_model_penalty_non_dict_word", 1);
            //engine.SetVariable("language_model_penalty_non_freq_dict_word", 1);
            using (var img = PixConverter.ToPix(imgsource)) {
                using (var page = OCREngine.Process(img)) {
                    ocrtext = page.GetText();
                }
            }
            ocrtext = ocrtext.Replace(",", ".").Replace("\n", "");
            return ocrtext;
        }

        public static string GetVersion(bool removeLastDigit) {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (removeLastDigit) {
                return version.Split('.')[0] + "." + version.Split('.')[1] + "." + version.Split('.')[2];
            } else {
                return version;
            }
        }

        private static void CloneDirectory(string root, string dest) {
            foreach (var directory in Directory.GetDirectories(root)) {
                string dirName = Path.GetFileName(directory);
                if (!Directory.Exists(Path.Combine(dest, dirName))) {
                    Directory.CreateDirectory(Path.Combine(dest, dirName));
                }
                CloneDirectory(directory, Path.Combine(dest, dirName));
            }

            foreach (var file in Directory.GetFiles(root)) {
                File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
            }
        }
        #endregion

        #region Version checking
        public void checkVersion() {
            Log.Add("Check version of settings...");
            try {
                if (Settings.Default.UpdateErforderlich) {
                    object prevVersion = Settings.Default.GetPreviousVersion("Version");
                    if (prevVersion != null) {
                        //Update
                        Log.Add("Updating Settings, prevVersion=" + prevVersion.ToString());

                        Settings.Default.Upgrade();

                        #region Update besonderheiten
                        if (new Version(prevVersion.ToString()).CompareTo(new Version("1.0.9")) < 0) {
                            MessageBox.Show("Your previous version is probably too old and cannot be converted automatically. You need to uninstall the program (completely) and reinstall the latest version.");
                        } else {
                            if (new Version(prevVersion.ToString()).CompareTo(new Version("1.1.0")) < 0) {
                                Log.Add("1.1.0", false, 1);
                                if (File.Exists(ConfigConsts.configPath)) {
                                    //Backup
                                    File.Copy(ConfigConsts.configPath, Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\backupTrainConfig.csv", true);
                                    MessageBox.Show("Backup has been created on the Desktop");

                                    //Neue Tastenbenennung
                                    string[] file = File.ReadAllLines(ConfigConsts.configPath);
                                    for (int i = 0; i < file.Count(); i++) {
                                        //Veränderungen
                                        string convertKey(string key) {
                                            switch (key) {
                                                case "einfg":
                                                    return "insert";
                                                case "pos1":
                                                    return "home";
                                                case "bildauf":
                                                    return "pageUp";
                                                case "entf":
                                                    return "del";
                                                case "ende":
                                                    return "end";
                                                case "bildab":
                                                    return "pageDown";
                                                case "hoch":
                                                    return "up";
                                                case "runter":
                                                    return "down";
                                                case "rechts":
                                                    return "right";
                                                case "links":
                                                    return "left";
                                                case "strg":
                                                    return "ctrl";
                                                case "lstrg":
                                                    return "lctrl";
                                                case "rstrg":
                                                    return "rctrl";
                                                case "ü":
                                                    return "oem1";
                                                case "#":
                                                    return "oem2";
                                                case "ö":
                                                    return "oem3";
                                                case "ß":
                                                    return "oem4";
                                                case "^":
                                                    return "oem5";
                                                case "´":
                                                    return "oem6";
                                                case "ä":
                                                    return "oem7";
                                                case "komma":
                                                    return "comma";
                                                case "zurück":
                                                    return "back";
                                                case "enter":
                                                    return "return";
                                                case "drucken":
                                                    return "print";
                                                case "rollen":
                                                    return "scroll";
                                                default:
                                                    return key;
                                            }
                                        }

                                        string[] single = file[i].Split(',');

                                        //Aktion
                                        single[8] = convertKey(single[8]);

                                        //Tastenkombination
                                        string[] tc = single[7].Split('_');
                                        if (tc.Count() >= 3) {
                                            for (int o = 0; o < tc.Count(); o += 3) {
                                                tc[o] = convertKey(tc[o]);
                                            }
                                            single[7] = String.Join("_", tc);
                                        }
                                        //Einstellungen
                                        Settings.Default.Tastenbelegung[0] = convertKey(Settings.Default.Tastenbelegung[0]);
                                        Settings.Default.Tastenbelegung[1] = convertKey(Settings.Default.Tastenbelegung[1]);
                                        Settings.Default.Tastenbelegung[2] = convertKey(Settings.Default.Tastenbelegung[2]);
                                        Settings.Default.Tastenbelegung[3] = convertKey(Settings.Default.Tastenbelegung[3]);

                                        file[i] = String.Join(",", single);
                                    }

                                    File.WriteAllLines(ConfigConsts.configPath, file);
                                }


                                bool areEqual = File.ReadLines(ConfigConsts.configPath).SequenceEqual(File.ReadLines(ConfigConsts.configStandardPath));
                                if (!areEqual) {
                                    Directory.CreateDirectory(ConfigConsts.configFolderPath);
                                    File.Copy(ConfigConsts.configPath, ConfigConsts.configFolderPath + "yourConfig.csv", true);
                                    Settings.Default.selectedTrainConfig = "yourConfig";
                                }


                                Settings.Default.Save();
                            }
                            if (new Version(prevVersion.ToString()).CompareTo(new Version("2.0.0")) < 0) {
                                Log.Add("2.0.0", false, 1);
                                List<string> ThrustIndex = new List<string>();
                                List<string> BrakeIndex = new List<string>();
                                List<string> Combined_ThrustIndex = new List<string>();
                                List<string> Combined_BrakeIndex = new List<string>();

                                List<string> output_Text = new List<string>();

                                ThrustIndex.AddRange(Settings.Default.ThrustIndex.Cast<string>().ToArray());
                                BrakeIndex.AddRange(Settings.Default.BrakeIndex.Cast<string>().ToArray());
                                Combined_BrakeIndex.AddRange(Settings.Default.Combined_BrakeIndex.Cast<string>().ToArray());
                                Combined_ThrustIndex.AddRange(Settings.Default.Combined_ThrustIndex.Cast<string>().ToArray());

                                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Update_Info";

                                if (Directory.Exists(folderPath)) {
                                    folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + @"\Update_Info2";
                                }

                                Directory.CreateDirectory(folderPath);
                                Directory.CreateDirectory(folderPath + @"\backupTrainConfig.csv");

                                CloneDirectory(ConfigConsts.configFolderPath, folderPath + @"\backupTrainConfig.csv");

                                MessageBox.Show("Backup has been created on the Desktop");

                                List<List<string>> tempTrainconfig = new List<List<string>>();

                                if (Directory.Exists(ConfigConsts.configFolderPath) && Directory.GetFiles(ConfigConsts.configFolderPath).Length > 0) {
                                    bool alreadyConverted = false;
                                    foreach (string filePath in Directory.GetFiles(ConfigConsts.configFolderPath)) {
                                        tempTrainconfig.Clear();
                                        using (var reader = new StreamReader(filePath)) {
                                            while (!reader.EndOfStream) {
                                                var line = reader.ReadLine();
                                                List<string> values = new List<string>();
                                                values.AddRange(line.Split(','));

                                                tempTrainconfig.Add(values);
                                            }
                                        }

                                        foreach (List<string> singleRow in tempTrainconfig) {
                                            if (singleRow.Count < 15) {
                                                if (singleRow[2] == "JoystickNummer") {
                                                    singleRow.Insert(ConfigConsts.controllerName, "ReglerName");
                                                } else {
                                                    if (singleRow[13] != "") {
                                                        try {
                                                            string[] longPressArray = singleRow[13].Remove(singleRow[13].Length - 1, 1).Replace("[", "").Split(']');
                                                            singleRow[13] = "";
                                                            foreach (string singleLongPress in longPressArray) {
                                                                int index_Gerade = singleLongPress.IndexOf("|");
                                                                int index_doppelpnkt = singleLongPress.IndexOf(":");

                                                                int erste_grenze = Convert.ToInt32(singleLongPress.Remove(index_Gerade, singleLongPress.Length - index_Gerade));
                                                                int zweite_grenze = Convert.ToInt32(singleLongPress.Remove(0, index_Gerade + 1).Remove(singleLongPress.Remove(0, index_Gerade + 1).IndexOf(":"), singleLongPress.Remove(0, index_Gerade + 1).Length - singleLongPress.Remove(0, index_Gerade + 1).IndexOf(":")));
                                                                int dauer = Convert.ToInt32(singleLongPress.Remove(0, singleLongPress.IndexOf(":") + 1));
                                                                singleRow[13] += "[" + erste_grenze + "|" + zweite_grenze + ":" + dauer + "]" + "[" + zweite_grenze + "|" + erste_grenze + ":" + dauer + "]";
                                                            }
                                                        } catch (Exception ex) {
                                                            Log.ErrorException(ex);
                                                        }
                                                    }

                                                    if (singleRow[7] == "Schub") {
                                                        singleRow.Insert(ConfigConsts.controllerName, "Throttle");
                                                        singleRow[1] = "";
                                                        singleRow[8] = "";
                                                    } else if (singleRow[7] == "Bremse") {
                                                        singleRow.Insert(ConfigConsts.controllerName, "Brake");
                                                        singleRow[1] = "";
                                                        singleRow[8] = "";
                                                    } else if (singleRow[7] == "Kombihebel") {
                                                        singleRow.Insert(ConfigConsts.controllerName, "Master Controller");
                                                        singleRow[1] = "";
                                                        singleRow[8] = "";
                                                    } else {
                                                        singleRow.Insert(ConfigConsts.controllerName, "");
                                                    }
                                                    if (singleRow[5] == "0") {
                                                        if (singleRow[6] == "1") {
                                                            singleRow[5] = "-100|100|100|0";
                                                        }
                                                    } else if (singleRow[5] == "1") {
                                                        if (singleRow[6] == "0") {
                                                            singleRow[5] = "-100|100|0|0|100|-100";
                                                        } else if (singleRow[6] == "1") {
                                                            singleRow[5] = "-100|0|100|100";
                                                        }
                                                    }

                                                    if (singleRow[5] != "") {
                                                        singleRow[6] = "";
                                                    }
                                                }
                                            } else {
                                                alreadyConverted = true;
                                                break;
                                            }
                                        }
                                        if (!alreadyConverted) {
                                            List<string> output = new List<string>();

                                            foreach (List<string> single in tempTrainconfig) {
                                                string line = string.Join(",", single);
                                                output.Add(line);
                                            }

                                            File.WriteAllLines(filePath, output.ToArray());
                                        } else {
                                            MessageBox.Show("Already converted the TrainConfig");
                                            break;
                                        }
                                    }
                                }


                                output_Text.Add("A lot has changed in version 2.0.0. Among other things, the way controllers work.");
                                output_Text.Add("Since I decided to treat Throttle and Master Controller separately, I can't just copy the old text indicators.");
                                output_Text.Add("");
                                output_Text.Add("So what do I need to do to transfer my old settings to the new version?");
                                output_Text.Add("");
                                output_Text.Add("1. ");
                                output_Text.Add("Go to \"Settings\"->\"Controls\"->\"Global keybinds\".");
                                output_Text.Add("There you should now find the controllers Throttle, Brake and Master Controller.");
                                output_Text.Add("");
                                output_Text.Add("2. ");
                                output_Text.Add("Select Throttle and check the keybinds first.");
                                output_Text.Add("Next check the main indicators.");
                                output_Text.Add("But you should no longer enter the text indicators for the Master Controller here!");
                                output_Text.Add("The text indicators you had before for the trottle and Master Controller are the following:");
                                output_Text.AddRange(Settings.Default.ThrustIndex.Cast<string>().ToArray());
                                output_Text.Add("");
                                output_Text.Add("3.");
                                output_Text.Add("Do the same for the brake.");
                                output_Text.Add("The text indicators you had before for the brake are the following:");
                                output_Text.AddRange(Settings.Default.BrakeIndex.Cast<string>().ToArray());
                                output_Text.Add("");
                                output_Text.Add("4.");
                                output_Text.Add("Now select \"Master Controller\". As you can see, the checkbox \"Master Controller\" is checked.");
                                output_Text.Add("This allows the controller to also go into the braking area (negative numbers).");
                                output_Text.Add("Here you now enter the indicators for the Master Controller and those for the throttle/ and brake area.");
                                output_Text.Add("You previously had the following indicators:");
                                output_Text.Add("Throttle area:");
                                output_Text.AddRange(Settings.Default.Combined_ThrustIndex.Cast<string>().ToArray());
                                output_Text.Add("Brake area:");
                                output_Text.AddRange(Settings.Default.Combined_BrakeIndex.Cast<string>().ToArray());
                                output_Text.Add("");
                                output_Text.Add("Done!");
                                output_Text.Add("If you get any problems converting your old settings, or anything else, feel free to contact me on");
                                output_Text.Add("Github (https://github.com/DerJantob/TSW2_Controller/issues/new/choose) or");
                                output_Text.Add("on the DTG Forum (https://forums.dovetailgames.com/threads/tsw2_controller-control-tsw2-with-a-joystick.52402/).");
                                output_Text.Add("");
                                output_Text.Add("Kind regards");
                                output_Text.Add("Jannik");

                                File.WriteAllLines(folderPath + @"\UpdateInfo.txt", output_Text.ToArray());
                                System.Diagnostics.Process.Start(folderPath + @"\UpdateInfo.txt");

                            }
                        }
                        #endregion

                    } else {
                        if (!File.Exists(ConfigConsts.controllersConfigPath)) {
                            File.Copy(ConfigConsts.controllersStandardPath, ConfigConsts.controllersConfigPath, false);
                            Log.Add("Copy :" + ConfigConsts.controllersStandardPath + " to " + ConfigConsts.controllersConfigPath);
                        }
                    }

                    Settings.Default.UpdateErforderlich = false;
                    Settings.Default.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Remove(Assembly.GetExecutingAssembly().GetName().Version.ToString().Length - 2, 2);

                    Settings.Default.Save();
                } else {
                    Log.Add("Settings are up to date", false, 1);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
                Log.ErrorException(ex);
            }
        }
        #endregion

        #region Load settings
        public void loadSettings() {
            Log.Add("Load Settings...");
            try {
                #region Resolution
                Log.Add("load res", false, 1);
                if (Settings.Default.res.IsEmpty) {
                    Settings.Default.res = res;
                    Settings.Default.Save();
                } else {
                    res = Settings.Default.res;
                    lbl_resolution.Text = res.Width.ToString() + "x" + res.Height.ToString();
                }

                #endregion

                #region Load debug info
                Log.Add("load debug info", false, 1);
                if (Settings.Default.showDebug) {
                    listBox_debugInfo.Show();
                    lbl_requests.Show();
                    lbl_scantime.Show();
                    checkBox_autoscroll.Show();
                } else {
                    listBox_debugInfo.Hide();
                    lbl_requests.Hide();
                    lbl_scantime.Hide();
                    checkBox_autoscroll.Hide();
                }
                #endregion

                #region Load scan result
                Log.Add("load scan results", false, 1);
                if (Settings.Default.showScanResult) {
                    pictureBox_Screenshot_original.Show();
                    pictureBox_Screenshot_alternative.Show();
                } else {
                    pictureBox_Screenshot_original.Hide();
                    pictureBox_Screenshot_alternative.Hide();
                }
                #endregion

                #region Load keybinds
                Log.Add("load key bindings", false, 1);
                try {
                    Keyboard.increaseThrottle = Keyboard.ConvertStringToKey(Settings.Default.Tastenbelegung[0]);
                    Keyboard.decreaseThrottle = Keyboard.ConvertStringToKey(Settings.Default.Tastenbelegung[1]);
                    Keyboard.increaseBrake = Keyboard.ConvertStringToKey(Settings.Default.Tastenbelegung[2]);
                    Keyboard.decreaseBrake = Keyboard.ConvertStringToKey(Settings.Default.Tastenbelegung[3]);
                } catch (Exception ex) {
                    MessageBox.Show(ex.ToString());
                    Log.ErrorException(ex);
                }
                #endregion
            } catch (Exception ex) { MessageBox.Show(ex.ToString()); Log.ErrorException(ex); Close(); }
        }
        #endregion

        #region Trainconfig und VControllers
        public void ReadTrainConfig() {
            Log.Add("Read TrainConfig");
            groupBox_ScanResults.Hide();
            if (File.Exists(ConfigConsts.configPath)) {
                trainConfig.Clear();
                using (var reader = new StreamReader(ConfigConsts.configPath)) {
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        bool found = false;
                        foreach (VirtualController vc in vControllerList) {
                            if (vc.name == values[ConfigConsts.controllerName]) {
                                found = true;
                                break;
                            }
                        }
                        if (!found && values[ConfigConsts.controllerName] != "" && values[ConfigConsts.trainName] != "Zug") {
                            groupBox_ScanResults.Show();
                            lbl_originalResult.Text = values[ConfigConsts.trainName] + ":\"" + values[ConfigConsts.controllerName] + "\" not found!";
                        }

                        trainConfig.Add(values);
                    }
                }
            }
            ReadTrainNamesFromTrainconfig();
        }

        public void ReadVControllers() {
            Log.Add("Read Controllers");
            if (File.Exists(ConfigConsts.controllersConfigPath)) {
                vControllerList.Clear();
                using (var reader = new StreamReader(ConfigConsts.controllersConfigPath)) {
                    int counter = 0;
                    while (!reader.EndOfStream) {
                        var line = reader.ReadLine();
                        string[] values = line.Split(',');
                        if (counter > 0) {
                            //Skip first line
                            VirtualController vc = new VirtualController();
                            vc.InsertFileArray(values);
                            vControllerList.Add(vc);
                        }
                        counter++;
                    }
                }
            }
        }

        public void ReadTrainNamesFromTrainconfig() {
            trainNames.Clear();
            combobox_trainSelection.Items.Clear();
            combobox_trainSelection.Items.Add("_Select train");
            combobox_trainSelection.SelectedItem = "_Select train";

            foreach (string[] str in trainConfig) {
                if (!trainNames.Contains(str[0]) && str[0] != "Zug" && str[0] != ConfigConsts.globalTrainConfigName) {
                    trainNames.Add(str[0]);
                }
            }
            combobox_trainSelection.Items.AddRange(trainNames.ToArray());
        }
        public void getActiveTrain() {
            //Reset Infos
            activeTrain.Clear();

            //What was selected
            string selection = combobox_trainSelection.Text;

            foreach (string[] str in trainConfig) {
                if (str[ConfigConsts.trainName] == selection || (str[ConfigConsts.trainName] == ConfigConsts.globalTrainConfigName && !isDeactivatedGlobally)) {
                    //Alle Infos zum Ausgewählten Zug speichern
                    activeTrain.Add(str);
                }
            }
        }
        public void getActiveVControllers() {
            activeVControllers.Clear();

            string selection = combobox_trainSelection.Text;

            foreach (string[] singleTrain in trainConfig) {
                if (singleTrain[ConfigConsts.trainName] == selection) {
                    string selected_vControllername = singleTrain[ConfigConsts.controllerName];
                    if (selected_vControllername != "") {
                        foreach (VirtualController vc in vControllerList.ToList()) {
                            if (vc.name == selected_vControllername) {
                                if (singleTrain[ConfigConsts.timeFactor].Contains("|")) {
                                    vc.timefactor = new int[] { Convert.ToInt32(singleTrain[ConfigConsts.timeFactor].Split('|')[0]), Convert.ToInt32(singleTrain[ConfigConsts.timeFactor].Split('|')[1]) };
                                } else {
                                    vc.timefactor = new int[] { Convert.ToInt32(singleTrain[ConfigConsts.timeFactor]), Convert.ToInt32(singleTrain[ConfigConsts.timeFactor]) };
                                }

                                if (singleTrain[ConfigConsts.steps] != "") {
                                    vc.stufen = Convert.ToInt32(singleTrain[ConfigConsts.steps]);
                                }
                                if (singleTrain[ConfigConsts.type] == "Stufenlos") {
                                    vc.isStepless = true;
                                } else {
                                    vc.isStepless = false;
                                }

                                if (singleTrain[ConfigConsts.specials].Contains("=") && singleTrain[ConfigConsts.specials] != "") {
                                    string[] specialCases = singleTrain[ConfigConsts.specials].Remove(singleTrain[ConfigConsts.specials].Length - 1, 1).Replace("[", "").Split(']');
                                    foreach (string specialCase in specialCases) {
                                        int index = specialCase.IndexOf("=");
                                        string word = specialCase.Remove(index, specialCase.Length - index);
                                        int entsprechendeNummer = Convert.ToInt32(specialCase.Remove(0, index + 1));
                                        vc.specialCases.Add(new string[] { word, entsprechendeNummer.ToString() });
                                    }
                                }

                                if (singleTrain[ConfigConsts.longPress].Contains(":") && singleTrain[ConfigConsts.longPress] != "") {
                                    string[] longPressArray = singleTrain[ConfigConsts.longPress].Remove(singleTrain[ConfigConsts.longPress].Length - 1, 1).Replace("[", "").Split(']');
                                    foreach (string singleLongPress in longPressArray) {
                                        int index_Gerade = singleLongPress.IndexOf("|");
                                        int index_doppelpnkt = singleLongPress.IndexOf(":");

                                        int erste_grenze = Convert.ToInt32(singleLongPress.Remove(index_Gerade, singleLongPress.Length - index_Gerade));
                                        int zweite_grenze = Convert.ToInt32(singleLongPress.Remove(0, index_Gerade + 1).Remove(singleLongPress.Remove(0, index_Gerade + 1).IndexOf(":"), singleLongPress.Remove(0, index_Gerade + 1).Length - singleLongPress.Remove(0, index_Gerade + 1).IndexOf(":")));
                                        int dauer = Convert.ToInt32(singleLongPress.Remove(0, singleLongPress.IndexOf(":") + 1));
                                        vc.longPress.Add(new int[] { erste_grenze, zweite_grenze, dauer });
                                    }
                                }



                                activeVControllers.Add(vc);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region Joystick und Buttons
        private Stopwatch masterControllerZeroWait;

        void fakeStickHandle() {
            if (masterControllerZeroWait == null) {
                masterControllerZeroWait = new Stopwatch();
                masterControllerZeroWait.Stop();
            }
            foreach (string[] strActiveTrain in activeTrain) {
                // Controller names taken from default csv file
                // -> AP only changes controllers named among these three
                var controllerName = strActiveTrain[ConfigConsts.controllerName];
                if (controllerName != "Throttle" && controllerName != "Brake" && controllerName != "Master Controller") continue;

                foreach (var controller in activeVControllers) {
                    if (strActiveTrain[ConfigConsts.controllerName] != controller.name) continue;

                    // Determine emulated input value based on AP inputs
                    int inputValue = 0;
                    if (controllerName == "Master Controller") {
                        if (desiredBrakePercent > 0) {
                            inputValue = desiredBrakePercent * -1;
                        } else if (desiredThrustPercent > 0) {
                            inputValue = desiredThrustPercent;
                            if (controller.currentSimValue < 0) { // Wait for lever to move to 0 first
                                inputValue = 0;
                                masterControllerZeroWait.Restart();
                            } else if (masterControllerZeroWait.ElapsedMilliseconds < 300 && masterControllerZeroWait.IsRunning) { // Do not apply throttle within 300ms after braking
                                inputValue = 0;
                            } else {
                                masterControllerZeroWait.Stop();
                            }
                        }
                    } else if (controllerName == "Throttle") {
                        inputValue = desiredThrustPercent;
                        // Don't apply throttle if any brake lever is active
                        foreach (var c2 in activeVControllers) {
                            if (c2.name == "Brake" && c2.currentSimValue > 0) {
                                inputValue = 0;
                            }
                        }
                    } else if (controllerName == "Brake") {
                        inputValue = desiredBrakePercent;
                    }

                    // 'Reassign Joy States' settings
                    inputValue = calculateReassignedJoyStates(strActiveTrain[ConfigConsts.reassignJoyStates], inputValue);

                    controller.currentJoystickValue = inputValue;
                }
            }

            int calculateReassignedJoyStates(string reassignConfig, int rawInputValue) {
                if (reassignConfig.Length <= 5) return rawInputValue;
                string[] reassignValues = reassignConfig.Remove(reassignConfig.Length - 1).Replace("[", "").Split(']');

                foreach (string reassignValue in reassignValues) {
                    if (reassignValue.Contains("|")) {
                        int von = Convert.ToInt32(reassignValue.Remove(reassignValue.IndexOf("|"), reassignValue.Length - reassignValue.IndexOf("|")));

                        string temp_bis = reassignValue.Remove(0, reassignValue.IndexOf("|") + 1);
                        int index = temp_bis.IndexOf("=");
                        int bis = Convert.ToInt32(temp_bis.Remove(index, temp_bis.Length - index));
                        int entsprechendeNummer = Convert.ToInt32(reassignValue.Remove(0, reassignValue.IndexOf("=") + 1));

                        if (von <= rawInputValue && rawInputValue <= bis) {
                            return entsprechendeNummer;
                        } else if (von >= rawInputValue && rawInputValue >= bis) {
                            return entsprechendeNummer;
                        }
                    } else {
                        int index = reassignValue.IndexOf("=");
                        int gesuchteNummer = Convert.ToInt32(reassignValue.Remove(index, reassignValue.Length - index));
                        int entsprechendeNummer = Convert.ToInt32(reassignValue.Remove(0, index + 1));

                        if (rawInputValue == gesuchteNummer) {
                            return entsprechendeNummer;
                        }
                    }
                }

                return rawInputValue;
            }
        }

        private void ShowJoystickData() {
            if (list_inputs.Items.Count != 3) {
                list_inputs.Items.Clear();
                list_inputs.Items.Add("0");
                list_inputs.Items.Add("1");
                list_inputs.Items.Add("2");
            }

            list_inputs.Items[0] = controlHoldNeeded ? "FD" : "AP";
            list_inputs.Items[1] = "THRUST " + desiredThrustPercent;
            list_inputs.Items[2] = "BRAKE " + desiredBrakePercent;
        }
        #endregion

        #region Main
        private void Main() {
            //for (int i = 0; i < MainSticks.Length; i++) {
            //    //Save the info of each individual joystick
            //    stickHandle(MainSticks[i], i);
            //}

            // This function acts as if all joysticks were scanned
            fakeStickHandle();

            //Show the user info about the joysticks
            ShowJoystickData();


            //When active
            if (check_active.Checked && !controlHoldNeeded) {
                //Check the individual controls
                handleVControllers();

                if (!bgw_readScreen.IsBusy) {
                    //Check if text can be read
                    bgw_readScreen.RunWorkerAsync();
                }
            }


            if (Log.DebugInfoList.Count > listBox_debugInfo.Items.Count) {
                for (int i = listBox_debugInfo.Items.Count; i < Log.DebugInfoList.Count; i++) {
                    listBox_debugInfo.Items.Add(Log.DebugInfoList[i]);
                }

                if (checkBox_autoscroll.Checked) {
                    listBox_debugInfo.TopIndex = listBox_debugInfo.Items.Count - 1;
                }
            }
        }
        #endregion

        #region handleVControllers
        private void handleVControllers() {
            //Go through all active VControllers
            for (int i = 0; i < activeVControllers.Count; i++) {
                VirtualController vc = activeVControllers[i];
                int timefactornumber = 0;

                if (vc.currentJoystickValue < 0) {
                    if (!vc.isMasterController) {
                        //If there is no combination lever then the value must not be less than zero
                        vc.currentJoystickValue = 0;
                    } else {
                        //If it is a master controller, then take the 2nd time factor in the braking area
                        timefactornumber = 1;
                    }
                }

                ConvertLongPress(vc);
                if (vc.isStepless) {
                    //Stepless
                    //Calculate difference
                    int diff = vc.currentJoystickValue - vc.currentSimValue;
                    if (Math.Abs(diff) > 1 && vc.waitToFinishMovement == false) {
                        Log.Add(vc.name + ":move from " + vc.currentSimValue + " to " + vc.currentJoystickValue, true);
                        vc.toleranceMem = false;
                        vc.cancelScan = 1;
                        new Thread(() => {
                            vc.waitToFinishMovement = true;
                            if (diff > 0) {
                                //more
                                if (diff < 4 || false) // Oh no idea somehow it's inaccurate and sometimes it's more accurate with this function
                                {
                                    // (Thought) If the button press is very short, then a lower time value is better
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.increaseKey), Convert.ToInt32(diff * (1000.0 / Convert.ToDouble(vc.timefactor[timefactornumber] * 1.5))));
                                } else {
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.increaseKey), Convert.ToInt32(diff * (1000.0 / Convert.ToDouble(vc.timefactor[timefactornumber]))));
                                }
                            } else if (diff < 0) {
                                //less
                                if (diff > -4 || false) // Oh no idea somehow it's inaccurate and sometimes it's more accurate with this function
                                {
                                    // (Thought) If the button press is very short, then a lower time value is better
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.decreaseKey), Convert.ToInt32(diff * (-1) * (1000.0 / Convert.ToDouble(vc.timefactor[timefactornumber] * 1.5))));
                                } else {
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.decreaseKey), Convert.ToInt32(diff * (-1) * (1000.0 / Convert.ToDouble(vc.timefactor[timefactornumber]))));
                                }
                            }
                            vc.waitToFinishMovement = false;
                            vc.cancelScan = -1;
                            vc.getText = 3;
                        }).Start();
                        vc.currentSimValue = vc.currentJoystickValue;
                    } else if (Math.Abs(diff) == 1 && vc.waitToFinishMovement == false && vc.toleranceMem == false) {
                        Log.Add(vc.name + ":No movement because of tolerance of +-1");
                        vc.toleranceMem = true;
                    }
                } else {
                    //Stepped
                    if (vc.waitToFinishMovement == false) {
                        //Convert number to level
                        int currentNotch = Convert.ToInt32(Math.Round(vc.currentJoystickValue * (Convert.ToDouble(vc.stufen) / 100), 0));
                        //Calculate difference
                        int diff = currentNotch - vc.currentSimValue;
                        if (diff != 0) {
                            Log.Add(vc.name + ":move from " + vc.currentSimValue + " to " + currentNotch, true);
                            vc.cancelScan = 1;
                            new Thread(() => {
                                vc.waitToFinishMovement = true;
                                if (diff > 0) {
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.increaseKey), vc.timefactor[timefactornumber] * Math.Abs(diff));
                                } else if (diff < 0) {
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.decreaseKey), vc.timefactor[timefactornumber] * Math.Abs(diff));
                                }
                                Thread.Sleep(80);
                                vc.waitToFinishMovement = false;
                                vc.cancelScan = -1;
                                vc.getText = 3; //Request for 3x text recognition
                            }).Start();
                            vc.currentSimValue = currentNotch;
                        }
                    }
                }
            }

            void ConvertLongPress(VirtualController vc) {
                foreach (int[] singleLongPress in vc.longPress) {
                    if (!vc.waitToFinishMovement) {
                        int ist = vc.currentSimValue;
                        int soll = vc.currentJoystickValue;
                        int erste_grenze = singleLongPress[0];
                        int zweite_grenze = singleLongPress[1];
                        int dauer = singleLongPress[2];

                        if (!vc.isStepless) {
                            soll = Convert.ToInt32(Math.Round(vc.currentJoystickValue * (Convert.ToDouble(vc.stufen) / 100), 0));
                        }


                        if ((soll - ist < 0 && zweite_grenze - erste_grenze < 0) || (soll - ist > 0 && zweite_grenze - erste_grenze > 0)) {
                            if (soll - ist < 0 && zweite_grenze - erste_grenze < 0) {
                                (erste_grenze, zweite_grenze) = (zweite_grenze, erste_grenze);
                            }


                            if (ist <= erste_grenze && zweite_grenze <= soll) {
                                //Mehr
                                vc.cancelScan = 1;
                                //Der Joystick kommt an der Langdruckstelle vorbei
                                if (ist == erste_grenze) {
                                    //Der sim-Regler ist genau an der Grenze zur Langdruckstelle
                                    Log.Add(vc.name + ":Long press from " + erste_grenze + " to " + zweite_grenze, true);
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.increaseKey), dauer);
                                    vc.currentSimValue = zweite_grenze;
                                    vc.getText = VirtualController.getTextDefault;
                                    Thread.Sleep(100);
                                } else if (ist < erste_grenze) {
                                    //Passe den soll wert so an, dass der sim-Regler an der Grenze stehen bleibt
                                    Log.Add(vc.name + ":Set value to " + erste_grenze + " insted of " + soll + " (moving up)", true);
                                    if (vc.isStepless) {
                                        Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.increaseKey), 10);
                                        vc.currentJoystickValue = erste_grenze;
                                        if (erste_grenze - ist == 1) {
                                            vc.currentSimValue -= 1;
                                        }
                                    }
                                }
                                vc.cancelScan = -1;
                            } else if (soll <= erste_grenze && zweite_grenze <= ist) {
                                //Weniger
                                vc.cancelScan = 1;
                                //Der Joystick kommt an der Langdruckstelle vorbei
                                if (ist == zweite_grenze) {
                                    //Der sim-Regler ist genau an der Grenze zur Langdruckstelle
                                    Log.Add(vc.name + ":Long press from " + zweite_grenze + " to " + erste_grenze, true);
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.decreaseKey), dauer);
                                    vc.currentSimValue = erste_grenze;
                                    vc.getText = VirtualController.getTextDefault;
                                    Thread.Sleep(100);
                                } else if (ist > zweite_grenze) {
                                    //Passe den soll wert so an, dass der sim-Regler an der Grenze stehen bleibt
                                    Log.Add(vc.name + ":Set value to " + zweite_grenze + " insted of " + soll + " (moving down)", true);
                                    if (vc.isStepless) {
                                        Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.decreaseKey), 10);
                                        vc.currentJoystickValue = zweite_grenze;

                                        if (ist - zweite_grenze == 1) {
                                            vc.currentSimValue += 1;
                                        }
                                    }
                                }
                                vc.cancelScan = -1;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region ReadScreen
        private void bgw_readScreen_DoWork(object sender, DoWorkEventArgs e) {
            int noResultValue = -99999;

            int requestcount = 0;
            for (int i = 0; i < activeVControllers.Count; i++) {
                VirtualController vc = activeVControllers[i];
                if (vc.getText > 0) {
                    //Number of different requests
                    requestcount++;
                }
            }

            lbl_requests.Invoke((MethodInvoker)delegate { lbl_requests.Text = "Text requests:" + requestcount.ToString(); });

            if (requestcount > 0) {
                //Read the first line
                Stopwatch stopwatch = Stopwatch.StartNew();
                string result = GetText(Screenshot(true));
                stopwatch.Stop();

                string tmp = result;

                string second_result = "";

                lbl_originalResult.Invoke((MethodInvoker)delegate { lbl_originalResult.Text = result; });
                groupBox_ScanResults.Invoke((MethodInvoker)delegate { groupBox_ScanResults.Show(); });
                lbl_scantime.Invoke((MethodInvoker)delegate { lbl_scantime.Text = "Scantime:" + stopwatch.ElapsedMilliseconds + "ms"; });

                for (int i = 0; i < activeVControllers.Count; i++) {
                    VirtualController vc = activeVControllers[i];

                    if (vc.getText > 0) {
                        if (vc.requestStartTime == new DateTime()) {
                            vc.requestStartTime = DateTime.Now;
                        }

                        if (vc.cancelScan == 0) {
                            if (tmp != "") { Log.Add("1. input:" + tmp, true); tmp = ""; }
                            //Finde den Indikator vom gelesenen Text
                            string indicator = GetBestMainIndicator(result, vc);

                            if (indicator == "") {
                                //Falls kein Indikator gefunden und die 2. Zeile noch nicht gelesen wurde, lese die 2. Zeile
                                if (second_result == "") {
                                    second_result = GetText(Screenshot(false));
                                    if (second_result != "") { Log.Add("2. input:" + second_result, true); }
                                    lbl_alternativeResult.Invoke((MethodInvoker)delegate { lbl_alternativeResult.Text = second_result; });
                                }
                                //Überprüfe das 2. Ergebnis
                                result = second_result;
                                indicator = GetBestMainIndicator(result, vc);
                            } else {
                                lbl_alternativeResult.Invoke((MethodInvoker)delegate { lbl_alternativeResult.Text = ""; });
                            }

                            if (indicator != "") {
                                Log.Add(vc.name + ":Found Indicator (" + indicator + ")");
                                //Wenn ein Indikator gefunden
                                int factor = 1;

                                //Entferne den Indikator
                                result = result.Remove(0, result.IndexOf(indicator) + indicator.Length);

                                if (ContainsBrakingArea(result, vc)) {
                                    //Bremsbereich
                                    factor = -1;
                                }

                                int detectedNumber = noResultValue;
                                int wordlength = 0;
                                foreach (string[] specialCase in vc.specialCases) //Sonderfälle
                                {
                                    if (ContainsWord(result, specialCase[0])) {
                                        if (specialCase[0].Length > wordlength) {
                                            //Den am besten passenden Sonderfall finden
                                            if (wordlength == 0) { Log.Add(vc.name + ":Special case->[" + specialCase[0] + "=" + specialCase[1] + "]", true); } else { Log.Add(vc.name + ":better one->[" + specialCase[0] + "=" + specialCase[1] + "]", true); }
                                            wordlength = specialCase[0].Length;
                                            detectedNumber = Convert.ToInt32(specialCase[1]) * factor;//Der faktor soll entgegenwirken sodass die Specialcases wörtlich genommen werden
                                        }
                                    }
                                }

                                if (detectedNumber == noResultValue) {
                                    //Kein SpecialCase gefunden
                                    int indexOfPercent = result.IndexOf("%");
                                    string result_withoutPercent = result;

                                    if (indexOfPercent != -1) {
                                        result_withoutPercent = result.Remove(indexOfPercent, result.Length - indexOfPercent);
                                    }
                                    try {
                                        int number = Convert.ToInt32(result_withoutPercent);
                                        if (vc.cancelScan == 0) {
                                            detectedNumber = number;
                                        }
                                    } catch {
                                        Log.Error("Could not get a number out of (removing percent method) " + result_withoutPercent);
                                        try {
                                            //Isoliere die letzte Zahl
                                            int number = Convert.ToInt32(Regex.Matches(result, @"\d+")[Regex.Matches(result, @"\d+").Count - 1].Value);
                                            if (vc.cancelScan == 0) {
                                                detectedNumber = number;
                                            }
                                        } catch (Exception ex) {
                                            Log.Error("Could not get a number out of (isolating method)" + result);
                                            Log.ErrorException(ex);
                                        }
                                    }
                                }
                                if (detectedNumber != noResultValue && vc.cancelScan == 0) {
                                    vc.currentSimValue = detectedNumber * factor;
                                    Log.Add(vc.name + ":" + detectedNumber + " (" + result + ")", true);
                                    vc.getText--;
                                    //Reset Time
                                    vc.requestStartTime = new DateTime();
                                }
                            } else {
                                //Wenn nichts gefunden dann drücke eine Taste vom Regler
                                if (vc.requestStartTime != new DateTime() && DateTime.Now.Subtract(vc.requestStartTime).TotalMilliseconds >= 3000) {
                                    Keyboard.HoldKey(Keyboard.ConvertStringToKey(vc.decreaseKey), 1);
                                    vc.requestStartTime = DateTime.Now;
                                    Log.Add(vc.name + ":Nothing found! Trying to show Text again");
                                }
                            }
                        } else {
                            if (vc.cancelScan == -1) {
                                vc.cancelScan = 0;
                            }
                        }
                    }
                }
            } else {
                groupBox_ScanResults.Invoke((MethodInvoker)delegate { groupBox_ScanResults.Hide(); });
            }

            string GetBestMainIndicator(string result, VirtualController virtualController) {
                VirtualController vc = virtualController;
                double bestMatchDistance = 0;
                string bestMatchWord = "";
                int maxwordlength = 0;

                //Gehe alle Indikatoren durch
                foreach (string indicator in vc.mainIndicators) {
                    if (ContainsWord(result, indicator)) {
                        //Indikator 1 zu 1 gefunden
                        if (indicator.Length > maxwordlength) {
                            bestMatchWord = indicator;
                            maxwordlength = indicator.Length;
                        }
                    } else {
                        //Wenn kein Indikator 1 zu 1 gefunden wurde, versuche etwas zu finden, was ähnlich ist

                        int indicatorWordCount = indicator.Split(' ').Count();
                        string[] splitResult = result.Split(' ');

                        for (int j = 0; j < splitResult.Length - indicatorWordCount; j++) {
                            string str = "";
                            for (int o = 0; o < indicatorWordCount; o++) {
                                str += splitResult[o + j] + " ";
                            }
                            str = str.Trim();

                            double distance = GetDamerauLevenshteinDistanceInPercent(str, indicator, 3);
                            if (distance > bestMatchDistance && distance >= 0.8) {
                                bestMatchDistance = distance;
                                bestMatchWord = str;
                            }
                        }
                    }
                }

                if (bestMatchWord != "") {
                    return bestMatchWord;
                } else {
                    return "";
                }
            }
            bool ContainsBrakingArea(string result, VirtualController virtualController) {
                VirtualController vc = virtualController;
                double bestMatchDistance = 0;
                string bestMatchWord = "";

                //Gehe alle Indikatoren durch
                foreach (string indicator in vc.textindicators_brakearea) {
                    if (ContainsWord(result, indicator)) {
                        //Indikator 1 zu 1 gefunden
                        bestMatchWord = indicator;
                        break;
                    } else if (result.Contains(indicator)) {
                        if (ContainsWord(Regex.Replace(result, @"[\d-]", " "), indicator)) {
                            bestMatchWord = indicator;
                        }
                    }

                    if (bestMatchWord == "") {
                        // If no 1 to 1 indicator was found, try to find something that is similar
                        int indicatorWordCount = indicator.Split(' ').Count();
                        string[] splitResult = result.Split(' ');

                        for (int j = 0; j < splitResult.Length - indicatorWordCount; j++) {
                            string str = "";
                            for (int o = 0; o < indicatorWordCount; o++) {
                                str += splitResult[o + j] + " ";
                            }
                            str = str.Trim();

                            double distance = GetDamerauLevenshteinDistanceInPercent(str, indicator, 3);
                            if (distance > bestMatchDistance && distance >= 0.8) {
                                bestMatchDistance = distance;
                                bestMatchWord = str;
                            }
                        }
                    }
                }

                if (bestMatchWord != "") {
                    Log.Add(vc.name + ":Is braking area [" + bestMatchWord + "]", true);
                    return true;
                } else {
                    return false;
                }
            }
        }
        private void bgw_readScreen_ProgressChanged(object sender, ProgressChangedEventArgs e) {
            //Update Picturebox
            if (((Bitmap)((object[])e.UserState)[0]).Height != 1) { pictureBox_Screenshot_original.Image = (Bitmap)((object[])e.UserState)[0]; }
            if (((Bitmap)((object[])e.UserState)[1]).Height != 1) { pictureBox_Screenshot_alternative.Image = (Bitmap)((object[])e.UserState)[1]; }
        }
        private void bgw_readScreen_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {

        }
        #endregion
    }
}
