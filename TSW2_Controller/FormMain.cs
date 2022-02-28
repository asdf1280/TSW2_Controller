﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tesseract;
using SlimDX.DirectInput;
using System.IO;
using TSW2_Controller.Properties;
using System.Reflection;
using System.Net;
using Octokit;
using System.Globalization;

namespace TSW2_Controller
{
    public partial class FormMain : Form
    {
        ///Todo:
        ///Wenn man den Regler langsam bewegt, dann passt der den irgendwie ungenau an.

        DirectInput input = new DirectInput();
        Joystick mainStick;
        public static Joystick[] MainSticks;

        public Rectangle res = Screen.PrimaryScreen.Bounds;

        public List<string[]> trainConfig = new List<string[]>();
        List<string[]> activeTrain = new List<string[]>();
        public List<string> trainNames = new List<string>();
        List<object[]> joystickStates = new List<object[]>(); // id, joyInputs, inputNames, buttons
        List<string> debugData = new List<string>();
        List<string> schubIndexe = new List<string>();
        List<string> bremsIndexe = new List<string>();
        List<string> kombihebel_schubIndexe = new List<string>();
        List<string> kombihebel_bremsIndexe = new List<string>();

        bool[] currentlyPressedButtons = new bool[128];
        bool[] previouslyPressedButtons = new bool[128];

        string[] throttleConfig; //{Index,Art,Schritte,Specials,Zeit,längerDrücken}
        string[] brakeConfig; //{Index,Art,Schritte,Specials,Zeit,LängerDrücken}
        public static string[] inputNames = { "JoyX", "JoyY", "JoyZ", "pov", "RotX", "RotY", "RotZ", "Sldr" };

        string[] defaultDE_schubIndexe = { "Fahrschalter", "Geschwindigkeitswähler", "Leistungsregler", "Fahrstufenschalter", "Leistungshebel", "Kombihebel", "Leistung/Bremse" };
        string[] defaultDE_bremsIndexe = { "Führerbremsventil", "Zugbremse", "Fahrerbremsventil" };
        string[] defaultDE_kombihebel_schubIndexe = { "Leistung" };
        string[] defaultDE_kombihebel_bremsIndexe = { "Bremsleistung", "Bremse" };

        string[] defaultEN_schubIndexe = { "Throttle", "Master Controller" };
        string[] defaultEN_bremsIndexe = { "Train Brake" };
        string[] defaultEN_kombihebel_schubIndexe = { "Power" };
        string[] defaultEN_kombihebel_bremsIndexe = { "Brake" };

        bool isKombihebel = false;
        bool globalIsDeactivated = false;

        int cancelThrottleRequest = 0; //0=false 1=true -1=Warte bis laufender Scan fertig
        int cancelBrakeRequest = 0; //0=false 1=true -1=Warte bis laufender Scan fertig

        int requestThrottle = 0;
        int requestBrake = 0;

        int schubIst = 0;
        int schubSoll = 0;

        int bremseIst = 0;
        int bremseSoll = 0;

        public List<string> DebugData { get => debugData; set => debugData = value; }



        public FormMain()
        {
            checkVersion();
            checkLanguageSetting();

            InitializeComponent();
            if (!File.Exists(Tcfg.configpfad))
            {
                if (!Directory.Exists(Tcfg.configpfad.Replace(@"\Trainconfig.csv", "")))
                {
                    Directory.CreateDirectory(Tcfg.configpfad.Replace(@"\Trainconfig.csv", ""));
                }
                if (File.Exists(Tcfg.configstandardpfad))
                {
                    File.Copy(Tcfg.configstandardpfad, Tcfg.configpfad, false);
                }
            }
            lbl_originalResult.Text = "";
            lbl_alternativeResult.Text = "";
            label2.Text = "";
            groupBox_ScanErgebnisse.Hide();

            CheckGitHubNewerVersion();

            loadSettings();
            Keyboard.initKeylist();

            comboBox_JoystickNumber.SelectedIndex = 0;
            MainSticks = getSticks();

            ReadTrainConfig();

            timer_CheckSticks.Start();
        }

        #region UI
        private void timer_CheckSticks_Tick(object sender, EventArgs e)
        {
            Main();
        }
        private void comboBox_Zugauswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            check_active.Checked = false;

            if (Settings.Default.deactivatedGlobals.Count > 0)
            {
                if (Settings.Default.deactivatedGlobals.Contains(comboBox_Zugauswahl.Text))
                {
                    check_deactivateGlobal.CheckedChanged -= check_deactivateGlobal_CheckedChanged;
                    check_deactivateGlobal.Checked = true;
                    check_deactivateGlobal.CheckedChanged += check_deactivateGlobal_CheckedChanged;

                    globalIsDeactivated = true;
                }
                else
                {
                    check_deactivateGlobal.CheckedChanged -= check_deactivateGlobal_CheckedChanged;
                    check_deactivateGlobal.Checked = false;
                    check_deactivateGlobal.CheckedChanged += check_deactivateGlobal_CheckedChanged;

                    globalIsDeactivated = false;
                }
            }

            getActiveTrain();
        }

        private void check_active_CheckedChanged(object sender, EventArgs e)
        {
            if (check_active.Checked)
            {
                check_active.BackColor = Color.Lime;
            }
            else
            {
                check_active.BackColor = Color.Red;
            }
        }

        private void check_deactivateGlobal_CheckedChanged(object sender, EventArgs e)
        {
            if (check_deactivateGlobal.Checked)
            {
                Settings.Default.deactivatedGlobals.Add(comboBox_Zugauswahl.Text);
                Settings.Default.Save();

                globalIsDeactivated = true;
            }
            else
            {
                Settings.Default.deactivatedGlobals.Remove(comboBox_Zugauswahl.Text);
                Settings.Default.Save();

                globalIsDeactivated = false;
            }
            getActiveTrain();
        }

        private void btn_einstellungen_Click(object sender, EventArgs e)
        {
            check_active.Checked = false;
            FormSettings settingsForm = new FormSettings();
            settingsForm.Location = this.Location;
            settingsForm.ShowDialog();
            loadSettings();

            ReadTrainConfig();
        }

        private void btn_reloadConfig_Click(object sender, EventArgs e)
        {
            check_active.Checked = false;
            ReadTrainConfig();
        }

        private async System.Threading.Tasks.Task CheckGitHubNewerVersion()
        {
            try
            {
                GitHubClient client = new GitHubClient(new ProductHeaderValue("DerJantob"));
                IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("DerJantob", "TSW2_Controller");

                //Setup the versions
                Version latestGitHubVersion = new Version(releases[0].TagName);
                Version localVersion = new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString().Remove(Assembly.GetExecutingAssembly().GetName().Version.ToString().Length - 2, 2)); //Replace this with your local version. 
                                                                                                                                                                                                     //Only tested with numeric values.
                int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                if (versionComparison < 0)
                {
                    //The version on GitHub is more up to date than this local release.
                    label2.Text = "Version " + latestGitHubVersion + "\n" + Sprache.ist_verfuegbar;
                }
            }
            catch
            {
            }
        }
        #endregion

        #region Allgemeine Funktionen
        public bool ContainsWord(string stringToCheck, string word)
        {
            if (word != null)
            {
                string[] split_stringTC = stringToCheck.Split(' ');
                int countOfWordsGiven = word.Count(x => x == ' ') + 1;

                if (stringToCheck != "")
                {
                    for (int i = 0; i < split_stringTC.Length - countOfWordsGiven + 1; i++)
                    {
                        string str = "";
                        for (int o = 0; o < countOfWordsGiven; o++)
                        {
                            str += split_stringTC[o + i] + " ";
                        }
                        str = str.Trim();

                        if (str.ToLower() == word.ToLower())
                        {
                            return true;
                        }
                    }
                }
            }
            return false;

        }

        public static double GetDamerauLevenshteinDistanceInPercent(string string_to_check, string string_to_check_from, int maxLengthDiff)
        {
            try
            {
                if (string.IsNullOrEmpty(string_to_check) || Math.Abs(string_to_check.Length - string_to_check_from.Length) > maxLengthDiff)
                {
                    return 0;
                    //throw new ArgumentNullException(t, "String Cannot Be Null Or Empty");
                }

                if (string.IsNullOrEmpty(string_to_check_from))
                {
                    return 0;
                    //throw new ArgumentNullException(t, "String Cannot Be Null Or Empty");
                }

                int n = string_to_check.Length; // length of s
                int m = string_to_check_from.Length; // length of t

                if (n == 0)
                {
                    return m;
                }

                if (m == 0)
                {
                    return n;
                }

                int[] p = new int[n + 1]; //'previous' cost array, horizontally
                int[] d = new int[n + 1]; // cost array, horizontally

                // indexes into strings s and t
                int i; // iterates through s
                int j; // iterates through t

                for (i = 0; i <= n; i++)
                {
                    p[i] = i;
                }

                for (j = 1; j <= m; j++)
                {
                    char tJ = string_to_check_from[j - 1]; // jth character of t
                    d[0] = j;

                    for (i = 1; i <= n; i++)
                    {
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
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return 0;
            }
        }

        public static void checkLanguageSetting()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(Properties.Settings.Default.Sprache);
            System.Threading.Thread.CurrentThread.CurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(Properties.Settings.Default.Sprache);
        }

        public Bitmap Screenshot(bool normal)
        {
            //Breite und Höhe des ScanFensters
            int width = ConvertWidth(513);
            int height = ConvertHeight(30);
            //Startposition vom oberen Fenster
            int x1 = ConvertWidth(1920);
            int y1 = ConvertHeight(458);
            //Angepasste Höhe fürs untere Fenster
            int y2 = ConvertHeight(530);


            Bitmap bmpScreenshot = new Bitmap(width, height);
            Graphics g = Graphics.FromImage(bmpScreenshot);

            if (normal)
            {
                //oberes Fenster
                g.CopyFromScreen(x1, y1, 0, 0, new Size(width, height));
            }
            else
            {
                //unteres Fenster
                g.CopyFromScreen(x1, y2, 0, 0, new Size(width, height));
            }

            for (int y = 0; (y <= (bmpScreenshot.Height - 1)); y++)
            {
                for (int x = 0; (x <= (bmpScreenshot.Width - 1)); x++)
                {
                    Color inv = bmpScreenshot.GetPixel(x, y);

                    //Farbanpassung um möglichst nur die Schrift zu erkennen
                    if (inv.R + inv.G + inv.G < 500)
                    {
                        inv = Color.FromArgb(0, 0, 0, 0);
                    }

                    //invertier das Bild
                    inv = Color.FromArgb(255, (255 - inv.R), (255 - inv.G), (255 - inv.B));
                    bmpScreenshot.SetPixel(x, y, inv);
                }
            }

            if (Settings.Default.showScanResult) { if (normal) { bgw_readScreen.ReportProgress(0, new object[] { bmpScreenshot, new Bitmap(1, 1), null, null, -1, -1 }); } else { bgw_readScreen.ReportProgress(0, new object[] { new Bitmap(1, 1), bmpScreenshot, null, null, -1, -1 }); } }

            return bmpScreenshot;
        }
        private int ConvertHeight(int height)
        {
            return Convert.ToInt32(Math.Round((Convert.ToDouble(height) / 1440.0) * Convert.ToDouble(res.Height), 0));
        }
        private int ConvertWidth(int width)
        {
            return Convert.ToInt32(Math.Round((Convert.ToDouble(width) / 2560.0) * Convert.ToDouble(res.Width), 0));
        }

        public static string GetText(Bitmap imgsource)
        {
            var ocrtext = string.Empty;
            using (var engine = new TesseractEngine(@"./tessdata", "deu", EngineMode.Default))
            {
                //engine.SetVariable("load_system_dawg", true);
                //engine.SetVariable("language_model_penalty_non_dict_word", 1);
                //engine.SetVariable("language_model_penalty_non_freq_dict_word", 1);
                using (var img = PixConverter.ToPix(imgsource))
                {
                    using (var page = engine.Process(img))
                    {
                        ocrtext = page.GetText();
                    }
                }
            }
            ocrtext = ocrtext.Replace(",", ".");
            return ocrtext;
        }
        #endregion

        #region Versions überprüfung
        public void checkVersion()
        {
            try
            {
                if (Settings.Default.UpdateErforderlich)
                {
                    object prevVersion = Settings.Default.GetPreviousVersion("Version");
                    if (prevVersion != null)
                    {
                        //Update
                        Settings.Default.Upgrade();

                        FormWasIstNeu formWasIstNeu = new FormWasIstNeu(prevVersion.ToString());
                        formWasIstNeu.ShowDialog();

                        #region Update besonderheiten
                        if (new Version(prevVersion.ToString()).CompareTo(new Version("1.0.0")) >= 0)
                        {
                            //Neue Einstellung muss mit Daten gefüllt werden
                            Settings.Default.SchubIndexe_EN.AddRange(defaultEN_schubIndexe);
                            Settings.Default.BremsIndexe_EN.AddRange(defaultEN_bremsIndexe);
                            Settings.Default.Kombihebel_SchubIndexe_EN.AddRange(defaultEN_kombihebel_schubIndexe);
                            Settings.Default.Kombihebel_BremsIndexe_EN.AddRange(defaultEN_kombihebel_bremsIndexe);

                            Settings.Default.SchubIndexe_DE.AddRange(defaultDE_schubIndexe);
                            Settings.Default.BremsIndexe_DE.AddRange(defaultDE_bremsIndexe);
                            Settings.Default.Kombihebel_SchubIndexe_DE.AddRange(defaultDE_kombihebel_schubIndexe);
                            Settings.Default.Kombihebel_BremsIndexe_DE.AddRange(defaultDE_kombihebel_bremsIndexe);

                            Settings.Default.Save();
                        }
                        #endregion

                    }
                    else
                    {
                        //Neuinstallation
                        CultureInfo ci = CultureInfo.InstalledUICulture;
                        if (ci.Name == "de-DE")
                        {
                            //Sprache von Windows
                            Settings.Default.Sprache = "de-DE";
                            Settings.Default.Save();
                        }

                        Settings.Default.SchubIndexe_EN.AddRange(defaultEN_schubIndexe);
                        Settings.Default.BremsIndexe_EN.AddRange(defaultEN_bremsIndexe);
                        Settings.Default.Kombihebel_SchubIndexe_EN.AddRange(defaultEN_kombihebel_schubIndexe);
                        Settings.Default.Kombihebel_BremsIndexe_EN.AddRange(defaultEN_kombihebel_bremsIndexe);

                        Settings.Default.SchubIndexe_DE.AddRange(defaultDE_schubIndexe);
                        Settings.Default.BremsIndexe_DE.AddRange(defaultDE_bremsIndexe);
                        Settings.Default.Kombihebel_SchubIndexe_DE.AddRange(defaultDE_kombihebel_schubIndexe);
                        Settings.Default.Kombihebel_BremsIndexe_DE.AddRange(defaultDE_kombihebel_bremsIndexe);

                        if (Sprache.SprachenName == "Deutsch")
                        {
                            Settings.Default.SchubIndexe.AddRange(defaultDE_schubIndexe); Settings.Default.Save();
                            Settings.Default.BremsIndexe.AddRange(defaultDE_bremsIndexe); Settings.Default.Save();
                            Settings.Default.Kombihebel_SchubIndexe.AddRange(defaultDE_kombihebel_schubIndexe); Settings.Default.Save();
                            Settings.Default.Kombihebel_BremsIndexe.AddRange(defaultDE_kombihebel_bremsIndexe); Settings.Default.Save();
                        }
                        else
                        {
                            Settings.Default.SchubIndexe.AddRange(defaultEN_schubIndexe); Settings.Default.Save();
                            Settings.Default.BremsIndexe.AddRange(defaultEN_bremsIndexe); Settings.Default.Save();
                            Settings.Default.Kombihebel_SchubIndexe.AddRange(defaultEN_kombihebel_schubIndexe); Settings.Default.Save();
                            Settings.Default.Kombihebel_BremsIndexe.AddRange(defaultEN_kombihebel_bremsIndexe); Settings.Default.Save();
                        }
                    }

                    Settings.Default.UpdateErforderlich = false;
                    Settings.Default.Version = Assembly.GetExecutingAssembly().GetName().Version.ToString().Remove(Assembly.GetExecutingAssembly().GetName().Version.ToString().Length - 2, 2);

                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        #endregion

        #region Einstellungen laden
        public void loadSettings()
        {
            try
            {
                #region Auflösung
                if (Settings.Default.res.IsEmpty)
                {
                    Settings.Default.res = res;
                    Settings.Default.Save();
                }
                else
                {
                    res = Settings.Default.res;
                    lbl_resolution.Text = res.Width.ToString() + "x" + res.Height.ToString();
                }

                #endregion

                #region Zeige Debug-Infos
                if (Settings.Default.showDebug)
                {
                    listBox_debugInfo.Show();
                    lbl_bremse.Show();
                    lbl_schub.Show();
                    lbl_requests.Show();
                }
                else
                {
                    listBox_debugInfo.Hide();
                    lbl_bremse.Hide();
                    lbl_schub.Hide();
                    lbl_requests.Hide();
                }
                #endregion

                #region Zeige Scan Ergebnis
                if (Settings.Default.showScanResult)
                {
                    pictureBox_Screenshot_original.Show();
                    pictureBox_Screenshot_alternativ.Show();
                }
                else
                {
                    pictureBox_Screenshot_original.Hide();
                    pictureBox_Screenshot_alternativ.Hide();
                }
                #endregion

                #region TextIndexe
                if (Settings.Default.SchubIndexe.Count == 0)
                {
                    if (MessageBox.Show(Sprache.Schubindikator_Leer__Standard_Laden, "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (Sprache.SprachenName == "Deutsch")
                        {
                            Settings.Default.SchubIndexe.AddRange(defaultDE_schubIndexe);
                        }
                        else
                        {
                            Settings.Default.SchubIndexe.AddRange(defaultEN_schubIndexe);
                        }
                        Settings.Default.Save();
                    }
                }
                if (Settings.Default.BremsIndexe.Count == 0)
                {
                    if (MessageBox.Show(Sprache.Bremsindikator_Leer__Standard_Laden, "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (Sprache.SprachenName == "Deutsch")
                        {
                            Settings.Default.BremsIndexe.AddRange(defaultDE_bremsIndexe);
                        }
                        else
                        {
                            Settings.Default.BremsIndexe.AddRange(defaultEN_bremsIndexe);
                        }
                        Settings.Default.Save();
                    }
                }
                if (Settings.Default.Kombihebel_SchubIndexe.Count == 0)
                {
                    if (MessageBox.Show(Sprache.Kombihebel_Schubindikator_Leer__Standard_Laden, "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (Sprache.SprachenName == "Deutsch")
                        {
                            Settings.Default.Kombihebel_SchubIndexe.AddRange(defaultDE_kombihebel_schubIndexe);
                        }
                        else
                        {
                            Settings.Default.Kombihebel_SchubIndexe.AddRange(defaultEN_kombihebel_schubIndexe);
                        }
                        Settings.Default.Save();
                    }
                }
                if (Settings.Default.Kombihebel_BremsIndexe.Count == 0)
                {
                    if (MessageBox.Show(Sprache.Kombihebel_Bremsindikator_Leer__Standard_Laden, "", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        if (Sprache.SprachenName == "Deutsch")
                        {
                            Settings.Default.Kombihebel_BremsIndexe.AddRange(defaultDE_kombihebel_bremsIndexe);
                        }
                        else
                        {
                            Settings.Default.Kombihebel_BremsIndexe.AddRange(defaultEN_kombihebel_bremsIndexe);
                        }
                        Settings.Default.Save();
                    }
                }

                schubIndexe.Clear();
                bremsIndexe.Clear();
                kombihebel_schubIndexe.Clear();
                kombihebel_bremsIndexe.Clear();

                schubIndexe.AddRange(Settings.Default.SchubIndexe.Cast<string>().ToArray());
                bremsIndexe.AddRange(Settings.Default.BremsIndexe.Cast<string>().ToArray());
                kombihebel_schubIndexe.AddRange(Settings.Default.Kombihebel_SchubIndexe.Cast<string>().ToArray());
                kombihebel_bremsIndexe.AddRange(Settings.Default.Kombihebel_BremsIndexe.Cast<string>().ToArray());
                #endregion
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); Close(); }
        }
        #endregion


        #region Main
        private void Main()
        {
            //Lösche alle Infos über die Joysticks
            joystickStates.Clear();

            for (int i = 0; i < MainSticks.Length; i++)
            {
                //Speichere die Infos von jedem einzelnen Joystick
                stickHandle(MainSticks[i], i);
            }

            //Zeige Infos über den Joystick dem Nutzer an
            ShowJoystickData();


            //Wenn Aktiv
            if (check_active.Checked)
            {
                if (MainSticks.Length > 0)
                {
                    //Wenn es einen Schubregler gibt und der BGW nicht beschäftigt ist dann starte ihn
                    if (!bgw_Throttle.IsBusy && throttleConfig[0] != null)
                    {
                        bgw_Throttle.RunWorkerAsync();
                    }

                    //Wenn es einen Bremsregler gibt und der BGW nicht beschäftigt ist dann starte ihn
                    if (!bgw_Brake.IsBusy && !isKombihebel && brakeConfig[0] != null)
                    {
                        bgw_Brake.RunWorkerAsync();
                    }

                    //Überprüfe die einzelnen Joystick knöpfe
                    HandleButtons();
                }
                else
                {
                    check_active.Checked = false;
                    MessageBox.Show(Sprache.Kein_Joystick_angeschlossen);
                }
            }
            else
            {
                //Wenn nicht Aktiv entferne die Anfrage an den bgw_readScreen
                requestBrake = 0;
                requestThrottle = 0;
            }


            if (requestBrake > 0 || requestThrottle > 0)
            {
                //Wenn der Schub-Wert oder Brems-Wert angefordert wird
                if (!bgw_readScreen.IsBusy)
                {
                    bgw_readScreen.RunWorkerAsync();
                }
            }

            //Debuginfos anzeigen
            try
            {
                string[] listArray = listBox_debugInfo.Items.OfType<string>().ToArray();
                if (listArray.Count() < DebugData.Count())
                {
                    int diff = DebugData.Count() - listArray.Count();
                    for (int i = 1; i <= diff; i++)
                    {
                        listBox_debugInfo.Items.Add(DateTime.Now.ToString("HH:mm:ss") + "    " + DebugData[DebugData.Count() - i]);
                    }
                    listBox_debugInfo.SelectedIndex = listBox_debugInfo.Items.Count - 1;
                }
            }
            catch { }
        }
        #endregion

        #region Trainconfig
        public void ReadTrainConfig()
        {
            if (File.Exists(Tcfg.configpfad))
            {
                trainConfig.Clear();
                using (var reader = new StreamReader(Tcfg.configpfad))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');

                        trainConfig.Add(values);
                    }
                }
            }
            ReadTrainNamesFromTrainconfig();
        }

        public void ReadTrainNamesFromTrainconfig()
        {
            trainNames.Clear();
            comboBox_Zugauswahl.Items.Clear();
            comboBox_Zugauswahl.Items.Add(Sprache.Zugauswahl);
            comboBox_Zugauswahl.SelectedItem = Sprache.Zugauswahl;

            foreach (string[] str in trainConfig)
            {
                bool alreadyExists = false;
                foreach (string tN in trainNames)
                {
                    if (str[0] == tN)
                    {
                        alreadyExists = true;
                    }
                }

                if (!alreadyExists && str[0] != "Zug" && str[0] != Tcfg.nameForGlobal)
                {
                    trainNames.Add(str[0]);
                }
            }
            comboBox_Zugauswahl.Items.AddRange(trainNames.ToArray());
        }

        public void getActiveTrain()
        {
            //Reset Infos
            throttleConfig = new string[6];
            brakeConfig = new string[6];
            activeTrain.Clear();

            //Was wurde ausgewählt
            string selection = comboBox_Zugauswahl.Text;

            foreach (string[] str in trainConfig)
            {
                if (str[0] == selection || (str[0] == Tcfg.nameForGlobal && !globalIsDeactivated))
                {
                    //Alle Infos zum Ausgewählten Zug speichern
                    activeTrain.Add(str);
                }
            }

            foreach (string[] str in activeTrain)
            {
                isKombihebel = false;
                if (str[Tcfg.tastenKombination].Contains("Schub"))
                {
                    //Infos über den Schubhebel
                    throttleConfig[0] = "empty";
                    throttleConfig[1] = str[Tcfg.art];
                    throttleConfig[2] = str[Tcfg.schritte];
                    throttleConfig[3] = str[Tcfg.specials];
                    throttleConfig[4] = str[Tcfg.zeitfaktor];
                    throttleConfig[5] = str[Tcfg.laengerDruecken];
                }
                else if (str[Tcfg.tastenKombination].Contains("Kombihebel"))
                {
                    //Infos über den Kombihebel
                    isKombihebel = true;
                    throttleConfig[0] = "empty";
                    throttleConfig[1] = str[Tcfg.art];
                    throttleConfig[2] = str[Tcfg.schritte];
                    throttleConfig[3] = str[Tcfg.specials];
                    throttleConfig[4] = str[Tcfg.zeitfaktor];
                    throttleConfig[5] = str[Tcfg.laengerDruecken];
                }

                if (str[Tcfg.tastenKombination].Contains("Bremse"))
                {
                    //Infos über die Bremse
                    brakeConfig[0] = "empty";
                    brakeConfig[1] = str[Tcfg.art];
                    brakeConfig[2] = str[Tcfg.schritte];
                    brakeConfig[3] = str[Tcfg.specials];
                    brakeConfig[4] = str[Tcfg.zeitfaktor];
                    brakeConfig[5] = str[Tcfg.laengerDruecken];
                }
            }

        }
        #endregion

        #region Joystick
        public Joystick[] getSticks()
        {
            //strg+c strg+v
            List<SlimDX.DirectInput.Joystick> sticks = new List<SlimDX.DirectInput.Joystick>();
            foreach (DeviceInstance device in input.GetDevices(DeviceClass.GameController, DeviceEnumerationFlags.AttachedOnly))
            {
                try
                {
                    mainStick = new SlimDX.DirectInput.Joystick(input, device.InstanceGuid);
                    mainStick.Acquire();

                    foreach (DeviceObjectInstance deviceObject in mainStick.GetObjects())
                    {
                        if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                        {
                            mainStick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(-100, 100);
                        }
                    }
                    sticks.Add(mainStick);
                }
                catch (DirectInputException)
                {
                    throw;
                }
            }

            return sticks.ToArray();

        }

        void stickHandle(Joystick stick, int id)
        {
            bool[] buttons;
            int[] joyInputs = new int[8];

            JoystickState state = new JoystickState();

            //Bekomme alle Infos über den mit id ausgewählten Stick
            state = stick.GetCurrentState();

            joyInputs[0] = state.X;
            joyInputs[1] = state.Y;
            joyInputs[2] = state.Z;
            joyInputs[3] = state.GetPointOfViewControllers()[0] + 1;
            joyInputs[4] = state.RotationX;
            joyInputs[5] = state.RotationY;
            joyInputs[6] = state.RotationZ;
            joyInputs[7] = state.GetSliders()[0];


            for (int i = 0; i < inputNames.Length; i++)
            {
                foreach (string[] strActiveTrain in activeTrain)
                {
                    //In der Trainconfig kommt ein bekannter Achsen-Name vor
                    if (strActiveTrain[Tcfg.joystickInput] == inputNames[i])
                    {
                        if (strActiveTrain[Tcfg.invertieren] == "1")
                        {
                            //Soll Invertiert werden
                            joyInputs[i] = joyInputs[i] * (-1);
                        }
                        if (strActiveTrain[Tcfg.inputTyp] == "1")
                        {
                            //Soll von (0<>100) in (-100<>0<>100) geändert werden
                            joyInputs[i] = (joyInputs[i] / (-2)) + 50;
                        }


                        //Bestimmt Inputwerte sollen in andere Umgerechnet werden
                        if (strActiveTrain[Tcfg.inputUmrechnen].Length > 5)
                        {
                            string[] umrechnen = strActiveTrain[Tcfg.inputUmrechnen].Remove(strActiveTrain[Tcfg.inputUmrechnen].Length - 1).Replace("[", "").Split(']');

                            foreach (string single_umrechnen in umrechnen)
                            {
                                if (single_umrechnen.Contains("|"))
                                {
                                    int von = Convert.ToInt32(single_umrechnen.Remove(single_umrechnen.IndexOf("|"), single_umrechnen.Length - single_umrechnen.IndexOf("|")));

                                    string temp_bis = single_umrechnen.Remove(0, single_umrechnen.IndexOf("|") + 1);
                                    int index = temp_bis.IndexOf("=");
                                    int bis = Convert.ToInt32(temp_bis.Remove(index, temp_bis.Length - index));
                                    int entsprechendeNummer = Convert.ToInt32(single_umrechnen.Remove(0, single_umrechnen.IndexOf("=") + 1));

                                    if (von <= joyInputs[i] && joyInputs[i] <= bis)
                                    {
                                        joyInputs[i] = entsprechendeNummer;
                                        break;
                                    }
                                    else if (von >= joyInputs[i] && joyInputs[i] >= bis)
                                    {
                                        joyInputs[i] = entsprechendeNummer;
                                        break;
                                    }
                                }
                                else
                                {
                                    int index = single_umrechnen.IndexOf("=");
                                    int gesuchteNummer = Convert.ToInt32(single_umrechnen.Remove(index, single_umrechnen.Length - index));
                                    int entsprechendeNummer = Convert.ToInt32(single_umrechnen.Remove(0, index + 1));

                                    if (joyInputs[i] == gesuchteNummer)
                                    {
                                        joyInputs[i] = entsprechendeNummer;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Alle Knopf states bekommen
            buttons = state.GetButtons();

            //Alle wichtigen Infos über den Joystick in Liste speichern
            joystickStates.Add(new object[] { id, joyInputs, inputNames, buttons });
        }

        public int GetJoystickStateByName(string id, string input)
        {
            foreach (object[] obyJstk in joystickStates)
            {
                if ((int)obyJstk[0] == Convert.ToInt32(id))
                {
                    for (int i = 0; i < ((string[])obyJstk[2]).Length; i++)
                    {
                        if (((string[])obyJstk[2])[i] == input)
                        {
                            return ((int[])obyJstk[1])[i];
                        }
                    }
                }
            }
            return 0;
        }

        public void HandleButtons()
        {
            for (int i = 0; i < activeTrain.Count; i++)
            {
                //Wenn es ein Normaler Knopf vom Joystick ist
                if (activeTrain[i][Tcfg.inputTyp] == "Button")
                {
                    int buttonNumber = Convert.ToInt32(activeTrain[i][Tcfg.joystickInput].Replace("B", ""));
                    currentlyPressedButtons[i] = ((bool[])((object[])joystickStates[Convert.ToInt32(activeTrain[i][Tcfg.joystickNummer])])[3])[buttonNumber];
                }
                else if (activeTrain[i][Tcfg.inputTyp].Contains("Button"))
                {
                    //Wenn ein Analoger Input zum Knopf werden soll
                    for (int o = 0; o < inputNames.Count(); o++)
                    {
                        if (activeTrain[i][Tcfg.joystickInput] == inputNames[o])
                        {
                            int joyButtonValue = GetJoystickStateByName(activeTrain[i][Tcfg.joystickNummer], inputNames[o]);
                            string[] convert = activeTrain[i][Tcfg.inputTyp].Replace("Button", "").Replace("[", "").Split(']');


                            currentlyPressedButtons[i] = false;
                            foreach (string single_convert in convert)
                            {
                                if (single_convert.Contains("="))
                                {
                                    if (joyButtonValue == Convert.ToInt32(single_convert.Remove(0, 1)))
                                    {
                                        currentlyPressedButtons[i] = true;
                                        break;
                                    }
                                    else
                                    {
                                        currentlyPressedButtons[i] = false;
                                    }
                                }
                                else if (single_convert.Contains(">"))
                                {
                                    if (joyButtonValue > Convert.ToInt32(single_convert.Remove(0, 1)))
                                    {
                                        currentlyPressedButtons[i] = true;
                                    }
                                    else
                                    {
                                        currentlyPressedButtons[i] = false;
                                        break;
                                    }
                                }
                                else if (single_convert.Contains("<"))
                                {
                                    if (joyButtonValue < Convert.ToInt32(single_convert.Remove(0, 1)))
                                    {
                                        currentlyPressedButtons[i] = true;
                                    }
                                    else
                                    {
                                        currentlyPressedButtons[i] = false;
                                        break;
                                    }
                                }

                            }
                        }
                    }
                }
            }


            for (int i = 0; i < currentlyPressedButtons.Count(); i++)
            {
                if (currentlyPressedButtons[i] != previouslyPressedButtons[i])
                {
                    if (currentlyPressedButtons[i] == true)
                    {
                        //on Press
                        if (activeTrain[i][Tcfg.tastenKombination] != "") { Keyboard.ProcessAktion(activeTrain[i][Tcfg.tastenKombination]); }
                        if (activeTrain[i][Tcfg.aktion] != "") { Keyboard.KeyDown(Keyboard.ConvertStringToKey(activeTrain[i][Tcfg.aktion])); }
                    }
                    else
                    {
                        //on release
                        //if (activeTrain[i][Tcfg.buttonUp] != "") { Keyboard.KeyUp(Keyboard.ConvertStringToKey(activeTrain[i][Tcfg.buttonUp])); }
                        if (activeTrain[i][Tcfg.aktion] != "") { Keyboard.KeyUp(Keyboard.ConvertStringToKey(activeTrain[i][Tcfg.aktion])); }
                    }
                    previouslyPressedButtons[i] = currentlyPressedButtons[i];
                }
            }
        }

        private void ShowJoystickData()
        {
            //Anzeigeliste leeren
            lst_inputs.Items.Clear();

            //Welcher Joystick wurde ausgewählt
            int selectedJoystickIndex = Convert.ToInt32(comboBox_JoystickNumber.SelectedItem);

            //Wähle von allen Joysticks nur den ausgewählten aus
            if (selectedJoystickIndex < joystickStates.Count)
            {
                object[] selectedJoystick = (object[])joystickStates[Convert.ToInt32(selectedJoystickIndex)];
                for (int i = 0; i < ((bool[])selectedJoystick[3]).Length; i++)
                {
                    if (((bool[])selectedJoystick[3])[i])
                    {
                        //Zeige den gedrückten Button
                        lst_inputs.Items.Add("B" + i);
                    }
                }
                for (int i = 0; i < ((int[])selectedJoystick[1]).Length; i++)
                {
                    if (((int[])selectedJoystick[1])[i] != 0)
                    {
                        //Zeige den Joystick-Wert nur, wenn er != 0 ist
                        lst_inputs.Items.Add(((string[])selectedJoystick[2])[i] + "  " + ((int[])selectedJoystick[1])[i]);
                    }
                }
            }
        }
        #endregion

        #region ChangeGameState
        private void bgw_Throttle_DoWork(object sender, DoWorkEventArgs e)
        {
            int currentThrottleJoystickState = 0;

            //Bekomme den Wert vom Joystick-Schubregler
            for (int i = 0; i < activeTrain.Count; i++)
            {
                if (activeTrain[i][Tcfg.tastenKombination].Contains("Schub") || activeTrain[i][Tcfg.tastenKombination].Contains("Kombihebel"))
                {
                    if (throttleConfig[1] == "Stufen")
                    {
                        currentThrottleJoystickState = GetJoystickStateByName(((string[])activeTrain[i])[Tcfg.joystickNummer], ((string[])activeTrain[i])[Tcfg.joystickInput]);
                        currentThrottleJoystickState = Convert.ToInt32(Math.Round(currentThrottleJoystickState * (Convert.ToDouble(throttleConfig[2]) / 100), 0));
                        break;
                    }
                    else if (throttleConfig[1] == "Stufenlos")
                    {
                        currentThrottleJoystickState = GetJoystickStateByName(((string[])activeTrain[i])[Tcfg.joystickNummer], ((string[])activeTrain[i])[Tcfg.joystickInput]);
                        break;
                    }
                }
            }


            //Erlaube keine Joystick-Werte unter 0, wenn es kein Kombihebel ist 
            if (currentThrottleJoystickState < 0 && !isKombihebel)
            {
                currentThrottleJoystickState = 0;
            }

            //Wenn sich der Wert vom Joystick-Schubregler geändert hat oder der vom Bildschirm gelesene Wert nicht passt
            if (schubSoll != currentThrottleJoystickState || (schubSoll != schubIst && throttleConfig[1] == "Stufen") || (Math.Abs(schubSoll - schubIst) > 1 && throttleConfig[1] == "Stufenlos") || (schubSoll == 0 && schubIst != 0))
            {
                cancelThrottleRequest = 1;

                schubSoll = currentThrottleJoystickState;
                ChangeGameState(true);

                cancelThrottleRequest = -1;
            }

        }

        private void bgw_Brake_DoWork(object sender, DoWorkEventArgs e)
        {
            int currentBrakeJoystickState = 0;

            //Bekomme den Wert vom Joystick-Bremsregler
            for (int i = 0; i < activeTrain.Count; i++)
            {
                if (activeTrain[i][Tcfg.tastenKombination].Contains("Bremse"))
                {
                    if (brakeConfig[1] == "Stufen")
                    {
                        currentBrakeJoystickState = GetJoystickStateByName(((string[])activeTrain[i])[Tcfg.joystickNummer], ((string[])activeTrain[i])[Tcfg.joystickInput]);
                        currentBrakeJoystickState = Convert.ToInt32(Math.Round(currentBrakeJoystickState * (Convert.ToDouble(brakeConfig[2]) / 100), 0));
                        break;
                    }
                    else if (brakeConfig[1] == "Stufenlos")
                    {
                        currentBrakeJoystickState = GetJoystickStateByName(((string[])activeTrain[i])[Tcfg.joystickNummer], ((string[])activeTrain[i])[Tcfg.joystickInput]);
                        break;
                    }
                }

            }

            //Erlaube keine Joystick-Werte unter 0
            if (currentBrakeJoystickState < 0)
            {
                currentBrakeJoystickState = 0;
            }

            //Wenn sich der Wert vom Joystick-Bremsregler geändert hat oder der vom Bildschirm gelesene Wert nicht passt
            if (bremseSoll != currentBrakeJoystickState || (bremseSoll != bremseIst && brakeConfig[1] == "Stufen") || (Math.Abs(bremseSoll - bremseIst) > 1 && brakeConfig[1] == "Stufenlos") || (bremseSoll == 0 && bremseIst != 0))
            {
                cancelBrakeRequest = 1;

                bremseSoll = currentBrakeJoystickState;
                ChangeGameState(false);

                cancelBrakeRequest = -1;
            }
        }


        private void ChangeGameState(bool isThrottle)
        {
            if (isThrottle)
            {
                if (throttleConfig[1] == "Stufenlos")
                {
                    int delay = 0;

                    ConvertLongPress(true, true);
                    int diffSchub = schubSoll - schubIst; //x>0 mehr |x<0 weniger

                    if (throttleConfig[4].Contains("|"))
                    {
                        if (schubIst < 0 || schubSoll < 0)
                        {
                            delay = Convert.ToInt32(throttleConfig[4].Remove(0, throttleConfig[4].IndexOf("|") + 1));
                        }
                        else
                        {
                            delay = Convert.ToInt32(throttleConfig[4].Remove(throttleConfig[4].IndexOf("|"), throttleConfig[4].Length - throttleConfig[4].IndexOf("|")));
                        }
                    }
                    else
                    {
                        delay = Convert.ToInt32(throttleConfig[4]);
                    }

                    if (diffSchub > 0)
                    {
                        //mehr
                        if (diffSchub < 4 && false)//Ach keine Ahnung irgendwie mal ist es ungenau und manchmal ist es mit dieser Funktion genauer
                        { Keyboard.HoldKey(Keyboard.increaseThrottle, Convert.ToInt32(diffSchub * (1000.0 / Convert.ToDouble(delay * 1.5)))); } //(Gedacht ist) Wenn der Knopfdruck sehr kurz ist dann ist ein niedriger Zeitwert besser
                        else
                        { Keyboard.HoldKey(Keyboard.increaseThrottle, Convert.ToInt32(diffSchub * (1000.0 / Convert.ToDouble(delay)))); }
                        schubIst = schubSoll;
                        requestThrottle = 3;
                    }
                    else if (diffSchub < 0)
                    {
                        //weniger
                        if (diffSchub > -4 && false)
                        { Keyboard.HoldKey(Keyboard.decreaseThrottle, Convert.ToInt32(diffSchub * (-1) * (1000.0 / Convert.ToDouble(delay * 1.5)))); }
                        else
                        { Keyboard.HoldKey(Keyboard.decreaseThrottle, Convert.ToInt32(diffSchub * (-1) * (1000.0 / Convert.ToDouble(delay)))); }
                        schubIst = schubSoll;
                        requestThrottle = 3;
                    }
                }
                else if (throttleConfig[1] == "Stufen")
                {
                    int delay = Convert.ToInt32(throttleConfig[4]);

                    ConvertLongPress(true, false); ;
                    int diffSchub = schubSoll - schubIst;

                    for (int i = 0; i < Math.Abs(diffSchub); i++)
                    {
                        if (diffSchub > 0)
                        {
                            Keyboard.HoldKey(Keyboard.increaseThrottle, delay);
                        }
                        else
                        {
                            Keyboard.HoldKey(Keyboard.decreaseThrottle, delay);
                        }
                        Thread.Sleep(80);
                    }
                    requestThrottle = 3;
                    schubIst = schubSoll;
                }
            }
            else
            {
                if (brakeConfig[1] == "Stufenlos")
                {
                    int delay = 0;

                    ConvertLongPress(false, true);
                    int diffBremse = bremseSoll - bremseIst; //x>0 mehr |x<0 weniger

                    delay = Convert.ToInt32(brakeConfig[4]);

                    if (diffBremse > 0)
                    {
                        //mehr
                        if (diffBremse < 4 && false)
                        { Keyboard.HoldKey(Keyboard.increaseBrake, Convert.ToInt32(diffBremse * (1000.0 / Convert.ToDouble(delay * 1.5)))); }
                        else
                        { Keyboard.HoldKey(Keyboard.increaseBrake, Convert.ToInt32(diffBremse * (1000.0 / Convert.ToDouble(delay)))); }
                        requestBrake = 2;
                        bremseIst = bremseSoll;
                    }
                    else if (diffBremse < 0)
                    {
                        //weniger
                        if (diffBremse > -4 && false)
                        { Keyboard.HoldKey(Keyboard.decreaseBrake, Convert.ToInt32(diffBremse * (-1) * (1000.0 / Convert.ToDouble(delay * 1.5)))); }
                        else
                        { Keyboard.HoldKey(Keyboard.decreaseBrake, Convert.ToInt32(diffBremse * (-1) * (1000.0 / Convert.ToDouble(delay)))); }
                        requestBrake = 2;
                        bremseIst = bremseSoll;
                    }
                }
                else if (brakeConfig[1] == "Stufen")
                {
                    int delay = Convert.ToInt32(brakeConfig[4]);

                    ConvertLongPress(false, false);
                    int diffBremse = bremseSoll - bremseIst;

                    for (int i = 0; i < Math.Abs(diffBremse); i++)
                    {
                        if (diffBremse > 0)
                        {
                            Keyboard.HoldKey(Keyboard.increaseBrake, delay);
                        }
                        else
                        {
                            Keyboard.HoldKey(Keyboard.decreaseBrake, delay);
                        }
                        Thread.Sleep(80);
                    }
                    requestBrake = 2;
                    bremseIst = bremseSoll;
                }
            }
        }

        public void ConvertLongPress(bool isThrottle, bool isStufenlos)
        {
            string[] config;
            int ist;
            int soll;
            Keys keyIncrease;
            Keys keyDecrease;

            if (isThrottle) { config = throttleConfig; } else { config = brakeConfig; }
            if (isThrottle) { ist = schubIst; } else { ist = bremseIst; }
            if (isThrottle) { soll = schubSoll; } else { soll = bremseSoll; }
            if (isThrottle) { keyIncrease = Keyboard.increaseThrottle; } else { keyIncrease = Keyboard.increaseBrake; }
            if (isThrottle) { keyDecrease = Keyboard.decreaseThrottle; } else { keyDecrease = Keyboard.decreaseBrake; }

            if (config[5].Length > 0)
            {
                foreach (string single in config[5].Remove(config[5].Length - 1).Replace("[", "").Split(']'))
                {
                    int index_minus = single.IndexOf("|");
                    int index_doppelpnkt = single.IndexOf(":");

                    int untere_grenze = Convert.ToInt32(single.Remove(index_minus, single.Length - index_minus));
                    int obere_grenze = Convert.ToInt32(single.Remove(0, index_minus + 1).Remove(single.Remove(0, index_minus + 1).IndexOf(":"), single.Remove(0, index_minus + 1).Length - single.Remove(0, index_minus + 1).IndexOf(":")));
                    int dauer = Convert.ToInt32(single.Remove(0, single.IndexOf(":") + 1));

                    if (ist <= untere_grenze && obere_grenze <= soll)
                    {
                        //Mehr
                        if (ist == untere_grenze)
                        {
                            DebugData.Add("Halte mehr gedrückt");
                            Keyboard.HoldKey(Keyboard.increaseThrottle, dauer);
                            ist = obere_grenze;
                            if (isThrottle) { requestThrottle = 3; } else { requestBrake = 3; }
                            Thread.Sleep(100);
                        }
                        else if (ist < untere_grenze)
                        {
                            DebugData.Add("Anpassen auf hoch " + untere_grenze);
                            if (isStufenlos)
                            {
                                Keyboard.HoldKey(keyIncrease, 10);
                                soll = untere_grenze;
                            }
                        }
                    }
                    else if (soll <= untere_grenze && obere_grenze <= ist)
                    {
                        //Weniger
                        if (ist == obere_grenze)
                        {
                            DebugData.Add("Halte weniger gedrückt");
                            Keyboard.HoldKey(keyDecrease, dauer);
                            ist = untere_grenze;
                            if (isThrottle) { requestThrottle = 3; } else { requestBrake = 3; }
                            Thread.Sleep(100);
                        }
                        else if (ist > obere_grenze)
                        {
                            DebugData.Add("Anpassen auf runter " + obere_grenze);
                            if (isStufenlos)
                            {
                                Keyboard.HoldKey(Keyboard.decreaseThrottle, 10);
                                soll = obere_grenze;
                            }
                        }
                    }
                }
            }

            if (isThrottle)
            {
                schubIst = ist;
                schubSoll = soll;
            }
            else
            {
                bremseIst = ist;
                bremseSoll = soll;
            }

        }
        #endregion

        #region ReadScreen
        private void bgw_readScreen_DoWork(object sender, DoWorkEventArgs e)
        {
            string original_result = GetText(Screenshot(true));
            string alternative_result = "";


            if (original_result == "")
            {
                alternative_result = GetText(Screenshot(false));
                if (alternative_result == "")
                {
                    goto Ende;
                }
            }


            if ((requestThrottle > 0 && requestBrake > 0) || (!ContainsWord(original_result, throttleConfig[0]) && requestThrottle > 0) || (!ContainsWord(original_result, brakeConfig[0]) && requestBrake > 0))
            {
                alternative_result = GetText(Screenshot(false));
            }


            //Zeige Scan-Ergebnisse
            bgw_readScreen.ReportProgress(0, new object[] { new Bitmap(1, 1), new Bitmap(1, 1), original_result.Replace("\n", ""), alternative_result.Replace("\n", ""), requestThrottle, requestBrake });


            //Wenn nichts gefunden wurde, dann gehe verschienene Indexe durch und drücke gesuchte Taste
            #region Schub
            if (requestThrottle > 0 && !ContainsWord(original_result, throttleConfig[0]) && !ContainsWord(alternative_result, throttleConfig[0]))
            {
                int maxLength = 0;
                foreach (string leistungsIndex in schubIndexe)
                {
                    if (ContainsWord(original_result, leistungsIndex))
                    {
                        throttleConfig[0] = leistungsIndex;
                        if (leistungsIndex.Length > maxLength)
                        {
                            maxLength = leistungsIndex.Length;
                        }
                    }
                    else if (ContainsWord(alternative_result, leistungsIndex))
                    {
                        throttleConfig[0] = leistungsIndex;
                        if (leistungsIndex.Length > maxLength)
                        {
                            maxLength = leistungsIndex.Length;
                        }
                    }
                }
                if (maxLength > 0) { DebugData.Add("SchubIndex = " + throttleConfig[0]); }
                Keyboard.HoldKey(Keyboard.decreaseThrottle, 1);
            }
            #endregion
            #region Bremse
            if (requestBrake > 0 && !ContainsWord(original_result, brakeConfig[0]) && !ContainsWord(alternative_result, brakeConfig[0]))
            {
                int maxLength = 0;
                foreach (string bremsindex in bremsIndexe)
                {
                    if (ContainsWord(original_result, bremsindex))
                    {
                        if (bremsindex.Length > maxLength)
                        {
                            brakeConfig[0] = bremsindex;
                        }
                    }
                    else if (ContainsWord(alternative_result, bremsindex))
                    {
                        if (bremsindex.Length > maxLength)
                        {
                            brakeConfig[0] = bremsindex;
                        }
                    }
                }
                if (maxLength > 0) { DebugData.Add("BremsIndex = " + throttleConfig[0]); }
                Keyboard.HoldKey(Keyboard.decreaseBrake, 1);
            }
            #endregion

            if (!ContainsWord(original_result, throttleConfig[0]) && !ContainsWord(original_result, brakeConfig[0]))
            {
                //Passe fehlerhaften Input bei original_result an
                original_result = korrigiereReadScreenText(original_result);
            }
            if (!ContainsWord(alternative_result, throttleConfig[0]) && !ContainsWord(alternative_result, brakeConfig[0]))
            {
                //Passe fehlerhaften Input bei alternative_result an
                alternative_result = korrigiereReadScreenText(alternative_result);
            }




            if (ContainsWord(original_result, throttleConfig[0]) || ContainsWord(alternative_result, throttleConfig[0]))
            {
                int erkannterWert = TextZuZahl_readScreen(original_result, alternative_result, throttleConfig);
                if (erkannterWert != -99999)
                {
                    if (cancelThrottleRequest == 0)
                    {
                        requestThrottle--;
                        schubIst = erkannterWert;
                    }
                    else if (cancelThrottleRequest == -1)
                    {
                        cancelThrottleRequest = 0;
                    }
                }
            }

            if (original_result.Contains(brakeConfig[0]) || alternative_result.Contains(brakeConfig[0]))
            {
                int erkannterWert = TextZuZahl_readScreen(original_result, alternative_result, brakeConfig);

                if (erkannterWert != -99999)
                {
                    if (cancelBrakeRequest == 0)
                    {
                        requestBrake--;
                        bremseIst = erkannterWert;
                    }
                    else if (cancelBrakeRequest == -1)
                    {
                        cancelBrakeRequest = 0;
                    }
                }

            }
        Ende:;
        }
        private void bgw_readScreen_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (((Bitmap)((object[])e.UserState)[0]).Height != 1) { pictureBox_Screenshot_original.Image = (Bitmap)((object[])e.UserState)[0]; }
            if (((Bitmap)((object[])e.UserState)[1]).Height != 1) { pictureBox_Screenshot_alternativ.Image = (Bitmap)((object[])e.UserState)[1]; }
            if (((string)((object[])e.UserState)[2]) != null) { lbl_originalResult.Text = ((string)((object[])e.UserState)[2]); }
            if (((string)((object[])e.UserState)[3]) != null) { lbl_alternativeResult.Text = ((string)((object[])e.UserState)[3]); }
            if (((int)((object[])e.UserState)[4]) != -1) { lbl_requests.Text = "reqT:" + (((int)((object[])e.UserState)[4]) - 1).ToString() + " reqB:" + (((int)((object[])e.UserState)[5]) - 1).ToString(); }

            if (requestBrake <= 0 && requestThrottle <= 0)
            {
                groupBox_ScanErgebnisse.Hide();
            }
            else
            {
                groupBox_ScanErgebnisse.Show();
            }
        }
        private void bgw_readScreen_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            lbl_schub.Text = Sprache.Schub_ist + " " + schubIst.ToString() + " " + Sprache.und_soll + " " + schubSoll.ToString();
            lbl_bremse.Text = Sprache.Bremse_ist + " " + bremseIst.ToString() + " " + Sprache.und_soll + " " + bremseSoll.ToString();
        }

        int TextZuZahl_readScreen(string original_result, string alternative_result, string[] config)
        {
            int noResultValue = -99999;

            string result = "";
            int erkannterWert = noResultValue;
            int schubFaktor = 1;

            if (ContainsWord(original_result, config[0]))
            {
                result = original_result;
            }
            else if (ContainsWord(alternative_result, config[0]))
            {
                result = alternative_result;
            }
            if (ContainsWord(result, config[0]) && result != "")
            {
                result = result.Remove(0, result.IndexOf(config[0]) + config[0].Length).Replace("\n", "");
                result = result.Trim();

                if (isKombihebel)
                {
                    result = result.Replace("%", "");
                }
                else if (result.Contains("%"))
                {
                    result = result.Remove(result.IndexOf("%"), result.Length - result.IndexOf("%"));
                }

                if (config[3].Length > 0)
                {
                    foreach (string singleSpezial in config[3].Remove(config[3].Length - 1).Replace("[", "").Split(']'))
                    {
                        int index = singleSpezial.IndexOf("=");
                        string word = singleSpezial.Remove(index, singleSpezial.Length - index);
                        int entsprechendeNummer = Convert.ToInt32(singleSpezial.Remove(0, index + 1));

                        if (ContainsWord(result, word) && singleSpezial != "")
                        {
                            erkannterWert = entsprechendeNummer;
                            schubFaktor = 0;
                            break;
                        }
                    }
                }

                #region Kombihebel
                if (isKombihebel)
                {
                    if(schubFaktor == 0)
                    {
                        schubFaktor = 1;
                    }
                    else if (kombihebel_bremsIndexe.Any(result.Contains) && result != "")
                    {
                        schubFaktor = -1;
                    }

                    foreach (string bremsIndex in kombihebel_bremsIndexe)
                    {
                        if (ContainsWord(result, bremsIndex) && result != "")
                        {
                            result = result.Replace(bremsIndex, "");
                            result = result.Remove(result.Length - 1);
                        }
                    }
                    foreach (string schubIndex in kombihebel_schubIndexe)
                    {
                        if (ContainsWord(result, schubIndex) && result != "")
                        {
                            result = result.Replace(schubIndex, "");
                            result = result.Remove(result.Length - 1);
                        }
                    }
                }
                #endregion


                if (erkannterWert == noResultValue)
                {
                    try { erkannterWert = Convert.ToInt32(result); } catch { }
                }
                if (erkannterWert != noResultValue)
                {
                    return erkannterWert * schubFaktor;
                }
            }
            return erkannterWert;
        }

        private string korrigiereReadScreenText(string textinput)
        {
            //Passe fehlerhaften Input bei alternative_result an
            string config = throttleConfig[0];
            string[] indexe = schubIndexe.ToArray();

            for (int type = 0; type < 2; type++)
            {
                if (type == 1 && !isKombihebel) { config = brakeConfig[0]; indexe = bremsIndexe.ToArray(); }

                if (config != null)
                {
                    try
                    {
                        if (!textinput.Contains(config) && textinput != "")
                        {
                            bool change = false;
                            string changelog = "";
                            double bestMatch = 0;

                            string[] seperated_textinput = textinput.Split(' ');
                            for (int i = 0; i < seperated_textinput.Length && !change; i++)
                            {
                                foreach (string textindex in indexe)
                                {
                                    double distance = GetDamerauLevenshteinDistanceInPercent(seperated_textinput[i], textindex, 2);
                                    if (distance > 0.8 && distance > bestMatch)
                                    {
                                        changelog = "Ändere \"" + seperated_textinput[i] + "\" zu " + textindex;
                                        seperated_textinput[i] = textindex;
                                        change = true;
                                        bestMatch = distance;
                                    }
                                }
                            }
                            if (change)
                            {
                                DebugData.Add(changelog);
                                textinput = "";
                                foreach (string single_textinput_result in seperated_textinput)
                                {
                                    textinput = textinput + single_textinput_result + " ";
                                }
                                return textinput;
                            }
                        }
                    }
                    catch (Exception ex)
                    { MessageBox.Show(ex.ToString()); }
                }
            }
            return textinput;
        }
        #endregion
    }
}
