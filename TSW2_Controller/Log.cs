using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TSW2_Controller {
    public class Log {
        public static List<string> DebugInfoList = new List<string>();
        public Log() {
        }

        public static void RemoveUnnecessaryLogs() {
            if (Properties.Settings.Default.DeleteLogsAutomatically) {
                CheckPath();
                foreach (string filePath in Directory.GetFiles(ConfigConsts.logFolderPath)) {
                    int days = 30;
                    if (File.GetLastWriteTime(filePath) < DateTime.Now.AddDays(-days)) {
                        File.Delete(filePath);
                    }
                }
            }
        }

        private static void CheckPath() {
            if (!Directory.Exists(ConfigConsts.logFolderPath)) {
                Directory.CreateDirectory(ConfigConsts.logFolderPath);
            }
        }


        private static readonly object locker = new object();

        public static void Add(string message, bool ShowUser = false, int indent = 0) {
            lock (locker) {
                string blankCharacter = "";
                while (indent > 0) {
                    blankCharacter += "    ";
                    indent--;
                }

                CheckPath();

                StreamWriter w;
                w = File.AppendText(ConfigConsts.fullLogPath);
                w.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "             :" + blankCharacter + message);
                w.Close();

                if (ShowUser) {
                    DebugInfoList.Add(message);
                }
            }
        }

        public static void Error(string message, bool ShowUser = false) {
            lock (locker) {
                CheckPath();

                StreamWriter w;
                w = File.AppendText(ConfigConsts.fullLogPath);
                w.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "    " + "-Error-  :" + message);
                w.Close();

                if (ShowUser) {
                    DebugInfoList.Add(message);
                }
            }
        }

        public static void ErrorException(Exception ex) {
            lock (locker) {
                CheckPath();

                StreamWriter w;
                w = File.AppendText(ConfigConsts.fullLogPath);
                w.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "    " + "-Error-  :" + ex.ToString().Replace("\r\n", ""));
                w.Close();
            }
        }
    }
}
