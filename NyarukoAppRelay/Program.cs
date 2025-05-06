using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
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

            // 1. 如果没有提供 /A，显示嵌入的帮助文档并退出
            if (string.IsNullOrEmpty(cmdA))
            {
                ShowHelp();
                return;
            }

            Application.Run(new RelayContext(cmdA, cmdE, iconPath, trayTitle));
        }

        static void ShowHelp()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                // 注意：资源名称通常是 "命名空间.文件名.txt"
                string resourceName = "NyarukoAppRelay.help.txt";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string helpContent = reader.ReadToEnd();
                    MessageBox.Show(helpContent, "NyarukoAppRelay 使用帮助");
                }
            }
            catch
            {
                MessageBox.Show("未找到帮助文件。用法示例：NyarukoAppRelay.exe /A \"notepad.exe\" /E \"calc.exe\"", "错误");
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