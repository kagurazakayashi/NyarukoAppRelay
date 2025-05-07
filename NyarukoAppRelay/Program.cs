using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NyarukoAppRelay
{
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

            // 1. 如果没有提供 /A，尝试显示帮助文档
            if (string.IsNullOrEmpty(cmdA))
            {
                if (!ShowHelp())
                {
                    // 2. 如果找不到帮助文档，立即退出程序
                    return;
                }
                return;
            }

            Application.Run(new RelayContext(cmdA, cmdE, iconPath, trayTitle));
        }

        static bool ShowHelp()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "NyarukoAppRelay.help.txt";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return false; // 找不到资源，返回 false

                    // 使用 Encoding.UTF8 并配合带 BOM 的文件可解决乱码
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string helpContent = reader.ReadToEnd();
                        MessageBox.Show(helpContent, "NyarukoAppRelay 使用帮助");
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
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