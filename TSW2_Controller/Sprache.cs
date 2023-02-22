using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TSW2_Controller.Properties;

namespace TSW2_Controller
{
    internal class Localization
    {
        public static string TrainSelection()
        {
            return Translate("_Zugauswahl", "_Select train");
        }

        public static void ShowMessageBox(string Deutsch, string English)
        {
            MessageBox.Show(English);
        }

        public static string Translate(string Deutsch, string English = "")
        {
            return English;
        }
    }
}
