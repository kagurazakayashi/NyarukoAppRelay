using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NyarukoAppRelay
{
    /// <summary>
    /// 程式啟動類別
    /// </summary>
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string cmdA = GetArgValue(args, "/A");
            string cmdE = GetArgValue(args, "/E");
            string iconPath = GetArgValue(args, "/I");
            string trayTitle = GetArgValue(args, "/T");
            bool useWindowMode = HasArg(args, "/W");

            if (string.IsNullOrEmpty(cmdA))
            {
                if (!ShowHelp()) return;
                return;
            }

            Application.Run(new RelayContext(cmdA, cmdE, iconPath, trayTitle, useWindowMode));
        }

        static bool ShowHelp()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "NyarukoAppRelay.help.txt";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return false;
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        AssemblyName assemblyName = assembly.GetName();
                        string assemblyVersion = assemblyName.Version?.ToString() ?? "";
                        MessageBox.Show(reader.ReadToEnd(), "NyarukoAppRelay v" + assemblyVersion + " by KagurazakaYashi");
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        static bool HasArg(string[] args, string key)
        {
            foreach (var arg in args)
            {
                if (arg.Equals(key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        static string GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    return args[i + 1];
            }
            return null;
        }
    }
}