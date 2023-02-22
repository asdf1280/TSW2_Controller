using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSW2_Controller.Properties;

namespace TSW2_Controller
{
    public partial class FormTimefactor : Form
    {
        public FormTimefactor()
        {
            InitializeComponent();
            radioChanged();
        }

        FormMain formMain = new FormMain();

        int counter;
        int startNumber;

        private void txt_Startwert_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }
        private void txt_Startwert_TextChanged(object sender, EventArgs e)
        {
            try { if (Convert.ToInt32(txt_Startwert.Text) > 100) { txt_Startwert.Text = txt_Startwert.Text.Remove(txt_Startwert.Text.Length - 1); } } catch(Exception ex) { Log.ErrorException(ex); }
        }

        private void SetUI(bool enabled)
        {
            if (enabled)
            {
                btn_start.Enabled = true;
                txt_Startwert.Enabled = true;
                radio_Bremse.Enabled = true;
                radio_Schub.Enabled = true;
                radio_Stufen.Enabled = true;
                radio_Stufenlos.Enabled = true;
                btn_start.Text = "Start";
            }
            else
            {
                btn_start.Enabled = false;
                txt_Startwert.Enabled = false;
                radio_Bremse.Enabled = false;
                radio_Schub.Enabled = false;
                radio_Stufen.Enabled = false;
                radio_Stufenlos.Enabled = false;
                radio_kombihebel.Enabled = false;
            }
        }

        private int GetNumber()
        {
            string result = FormMain.GetText(formMain.Screenshot(true));
            int index = result.IndexOf("%");
            result = result.Remove(index, result.Length - index);
            result = result.Remove(0, result.IndexOfAny("0123456789".ToArray()));

            return Convert.ToInt32(result);
        }

        private void btn_start_Click(object sender, EventArgs e)
        {
            counter = 7;
            if (!radio_Stufenlos.Checked)
            {
                SetUI(false);
                btn_start.Text = counter.ToString();
                timer1.Start();
            }
            else
            {
                try
                {
                    if (txt_Startwert.Text != "")
                    {
                        startNumber = Convert.ToInt32(txt_Startwert.Text);

                        SetUI(false);

                        btn_start.Text = counter.ToString();
                        timer1.Start();
                    }
                    else
                    {
                        MessageBox.Show("You didn't enter any starting value!");
                    }
                }
                catch (Exception ex)
                {
                    Log.ErrorException(ex);
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (counter > 0)
            {
                counter--;
                btn_start.Text = counter.ToString();
            }
            else
            {
                //Start
                timer1.Stop();
                if (radio_kombihebel.Checked)
                {
                    RunKombihebel();
                }
                else
                {
                    RunNormal();
                }
            }
        }


        private void RunNormal()
        {
            int delay = 1000;
            Keys taste_mehr;
            Keys taste_weniger;
            int endNumber = 0;

            if (radio_Schub.Checked)
            {
                taste_mehr = Keyboard.increaseThrottle;
                taste_weniger = Keyboard.decreaseThrottle;
            }
            else
            {
                taste_mehr = Keyboard.increaseBrake;
                taste_weniger = Keyboard.decreaseBrake;
            }

            bool wait = true;
            bool nothingDetected = false;
            if (radio_Stufenlos.Checked)
            {
                while (wait)
                {
                    Keyboard.HoldKey(taste_mehr, delay);
                    Thread.Sleep(1000);
                    string result = FormMain.GetText(formMain.Screenshot(true));

                    if (result.Contains("%") && "0123456789".ToArray().Any(result.Contains))
                    {
                        int index = result.IndexOf("%");
                        result = result.Remove(index, result.Length - index);
                        result = result.Remove(0, result.IndexOfAny("0123456789".ToArray()));

                        try
                        {
                            endNumber = Convert.ToInt32(result);
                            wait = false;
                        }
                        catch (Exception ex) { MessageBox.Show(result + "\n" + ex.ToString()); Log.ErrorException(ex); }

                    }
                    else
                    {
                        Keyboard.HoldKey(taste_mehr, 1000);

                        Keyboard.HoldKey(Keys.Escape, 300);
                        this.Focus();
                        Interaction.Beep();
                        DialogResult dialog = MessageBox.Show("No number detected. Could it be that you have reached the maximum?", "Error", MessageBoxButtons.YesNo);

                        if (dialog == DialogResult.Yes)
                        {
                            MessageBox.Show("OK, than set the controller back to " + startNumber + "%" + " and press OK");
                            MessageBox.Show("Press OK again, switch to TSW within 7 seconds and wait");
                            Thread.Sleep(7000);
                            delay = 800;
                            //Versuche es nochmal
                        }
                        else if (dialog == DialogResult.No)
                        {
                            while (true)
                            {
                                try
                                {
                                    string userinput = Interaction.InputBox("What number did you land on?");
                                    endNumber = Convert.ToInt32(userinput);
                                    wait = false;
                                    nothingDetected = true;
                                    break;
                                }
                                catch(Exception ex) { Log.ErrorException(ex); }
                            }
                        }
                    }
                }

                if (endNumber - startNumber > 0)
                {
                    if (!nothingDetected) { Keyboard.HoldKey(Keys.Escape, 300); }
                    this.Focus();
                    Interaction.Beep();
                    MessageBox.Show("You can now set the time factor to " + Math.Round(Convert.ToDouble(endNumber - startNumber) * (1000.0 / delay), 0));
                }
                else if (endNumber != -1)
                {
                    this.Focus();
                    Interaction.Beep();
                    MessageBox.Show("Hmm, you must have done something wrong. The starting number should be greater than the ending number.");
                }
            }
            else
            {
                Keyboard.HoldKey(taste_mehr, 0);
                Thread.Sleep(500);
                Regex rgx = new Regex("[^a-zA-Z0-9 -]");

                string grundstellung = rgx.Replace(FormMain.GetText(formMain.Screenshot(true)), "");
                Thread.Sleep(500);

                int nextStep_Value = 0;
                int overskip_Value = 0;

                for (int i = 0; i < 1000; i += 5)
                {
                    Keyboard.HoldKey(taste_mehr, i);
                    Thread.Sleep(180);
                    string neueStellung = rgx.Replace(FormMain.GetText(formMain.Screenshot(true)), "");
                    if (neueStellung != grundstellung)
                    {
                        bool different = true;
                        for (int o = 0; o < 5; o++)
                        {
                            Thread.Sleep(100);
                            neueStellung = rgx.Replace(FormMain.GetText(formMain.Screenshot(true)), "");
                            if (neueStellung == grundstellung)
                            {
                                different = false;
                            }
                        }
                        if (different)
                        {
                            nextStep_Value = i;
                            break;
                        }
                    }
                    Thread.Sleep(100);
                }
                if (nextStep_Value != 0)
                {
                    Thread.Sleep(1000);
                    for (int i = 30; i < 2000; i += 5)
                    {
                        Keyboard.HoldKey(taste_weniger, nextStep_Value + i);
                        Thread.Sleep(500);
                        string neueStellung = rgx.Replace(FormMain.GetText(formMain.Screenshot(true)), "");
                        if (neueStellung != grundstellung)
                        {
                            bool different = true;
                            for (int o = 0; o < 5; o++)
                            {
                                Thread.Sleep(100);
                                neueStellung = rgx.Replace(FormMain.GetText(formMain.Screenshot(true)), "");
                                if (neueStellung == grundstellung)
                                {
                                    different = false;
                                }
                            }

                            if (different)
                            {
                                overskip_Value = nextStep_Value + i;
                                break;
                            }
                        }
                        Keyboard.HoldKey(taste_mehr, nextStep_Value + 30);
                        Thread.Sleep(500);
                    }
                }
                if (overskip_Value != 0)
                {
                    Keyboard.HoldKey(Keys.Escape, 300);
                    this.Focus();
                    Interaction.Beep();
                    MessageBox.Show("Done!" + "\n" + "You can now set the time factor to " + Math.Round((Convert.ToDouble(overskip_Value - nextStep_Value) / 2) + nextStep_Value, 0));
                }
                else
                {
                    Keyboard.HoldKey(Keys.Escape, 300);
                    this.Focus();
                    Interaction.Beep();
                    MessageBox.Show("Hmm, you must have done something wrong. The starting number should be greater than the ending number.");
                }
            }

            Close();
        }

        private void RunKombihebel()
        {
            int ersterZeitwert = 0;
            int zweiterZeitwert = 0;

            Keyboard.HoldKey(Keyboard.increaseThrottle, 0);
            Thread.Sleep(500);
            try
            {
                int startWert = 0;
                int endWert = 0;

                startWert = GetNumber();
                Keyboard.HoldKey(Keyboard.increaseThrottle, 1000);
                Thread.Sleep(500);
                endWert = GetNumber();
                ersterZeitwert = endWert - startWert;
            }
            catch(Exception ex) { MessageBox.Show("not able to read number"); Log.ErrorException(ex); }

            Keyboard.HoldKey(Keys.Escape, 300);
            this.Focus();
            Interaction.Beep();
            MessageBox.Show("Now set the controller to the braking value");
            MessageBox.Show("Press OK again, switch to TSW within 7 seconds and wait");
            Thread.Sleep(7000);

            Keyboard.HoldKey(Keyboard.decreaseThrottle, 0);
            Thread.Sleep(500);
            try
            {
                int startWert = 0;
                int endWert = 0;

                startWert = GetNumber();
                Keyboard.HoldKey(Keyboard.decreaseThrottle, 1000);
                Thread.Sleep(500);
                endWert = GetNumber();
                zweiterZeitwert = endWert - startWert;
            }
            catch (Exception ex) { MessageBox.Show("not able to read number"); Log.ErrorException(ex); }

            Keyboard.HoldKey(Keys.Escape, 300);
            this.Focus();
            Interaction.Beep();
            MessageBox.Show("Done!" + "\n" + "You can now set the time factor to " + ersterZeitwert + "|" + zweiterZeitwert);
            Close();
        }


        private void radioChanged()
        {
            SetUI(true);
            if (radio_Stufenlos.Checked)
            {
                lbl_anleitung.Text = "- Set the continuously variable controller to a small value, from which you can reach the maximum at a constant speed.\n\n- Enter the corresponding percentage value in the text box. (e.g. if min = 5%)\n\n- Press start and switch to TSW2 within 7 seconds.\n\n- Wait for pause screen.\n";
            }

            if (radio_Stufen.Checked)
            {
                lbl_anleitung.Text = "- Set the notch control to midpoint (the level above and below should be within a normal key press length)\n\n- Press start and switch to the TSW2 within 7 seconds.\n\n- Wait for the pause screen.\n";
                txt_Startwert.Text = "";
                txt_Startwert.Enabled = false;
            }

            if (radio_kombihebel.Checked)
            {
                lbl_anleitung.Text = "- Set the master controller in the thrust range to a small value displayed in %. (e.g. 10%,13%,18%)\n\n- Press start and switch to TSW2 within 7 seconds.\n\n- Wait for pause screen.\n\n- Master controller in brake range to a small value displayed in %. (e.g. 10%,13%,18%)\n\n- Press OK\n\n- Wait for pause screen ";
                radio_Schub.Checked = true;
                radio_Bremse.Enabled = false;
                txt_Startwert.Text = "";
                txt_Startwert.Enabled = false;
            }
        }

        private void radio_Combined_CheckedChanged(object sender, EventArgs e)
        {
            radioChanged();
        }

        private void radio_Stufen_CheckedChanged(object sender, EventArgs e)
        {
            radioChanged();
        }

        private void radio_Stufenlos_CheckedChanged(object sender, EventArgs e)
        {
            radioChanged();
        }

        private void txt_taste_KeyDown(object sender, KeyEventArgs e)
        {
            txt_taste.Text = Keyboard.ConvertKeyToString(e.KeyCode);
            e.SuppressKeyPress = true;
        }

        private void txt_dauer_KeyPress(object sender, KeyPressEventArgs e)
        {
            e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void btn_start_longpress_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Press OK again, switch to TSW within 7 seconds and wait");
            Thread.Sleep(7000);
            try
            {
                Keyboard.HoldKey(Keyboard.ConvertStringToKey(txt_taste.Text), Convert.ToInt32(txt_dauer.Text));
            }
            catch (Exception ex)
            {
                Log.ErrorException(ex);
            }
        }

        private void FormZeitfaktor_Load(object sender, EventArgs e)
        {

        }
    }
}
