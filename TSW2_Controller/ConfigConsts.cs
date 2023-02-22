using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TSW2_Controller
{
    internal class ConfigConsts
    {
        public static int arrayLength = 15;

        public static int trainName = 0;
        public static int description = 1;
        public static int controllerName = 2;
        public static int joystickNumber = 3;
        public static int joystickInput = 4;
        public static int invert = 5;
        public static int inputType = 6;
        public static int inputConvert = 7;
        public static int keyCombination = 8;
        public static int action = 9;
        public static int type = 10;
        public static int steps = 11;
        public static int specials = 12;
        public static int timeFactor = 13;
        public static int longPress = 14;

        public static string globalTrainConfigName = "_Global";


        public static string configFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\TSW2_Controller\TrainConfigs\";

        public static string configPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\TSW2_Controller\Trainconfig.csv";
        public static string controllersConfigPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\TSW2_Controller\Controllers.csv";

        public static string configStandardPath = @".\Configs\TrainConfig\StandardTrainconfig.csv";
        public static string controllersStandardPath = @".\Configs\ControllerConfig\StandardControllers_EN.csv";

        public static string fullLogPath;
        public static string logFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\TSW2_Controller\Log\";
    }
}
