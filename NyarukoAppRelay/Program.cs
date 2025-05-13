using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NyarukoAppRelay
{
    /// <summary>
    /// 程式啟動類別
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 應用程式進入點
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 取得各項參數值
            string cmdA = GetArgValue(args, "/A");
            string cmdE = GetArgValue(args, "/E");
            string iconPath = GetArgValue(args, "/I");
            string trayTitle = GetArgValue(args, "/T");

            // 檢查是否存在 /W 參數以決定是否啟用視窗監控模式
            bool useWindowMode = HasArg(args, "/W");

            // 若缺少核心參數 /A 則顯示說明文件
            if (string.IsNullOrEmpty(cmdA))
            {
                if (!ShowHelp()) return;
                return;
            }

            // 執行背景監控上下文
            Application.Run(new RelayContext(cmdA, cmdE, iconPath, trayTitle, useWindowMode));
        }

        /// <summary>
        /// 顯示內嵌的說明文件
        /// </summary>
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
                        MessageBox.Show(reader.ReadToEnd(), "NyarukoAppRelay 使用帮助");
                        return true;
                    }
                }
            }
            catch { return false; }
        }

        /// <summary>
        /// 判斷是否存在指定的參數開關
        /// </summary>
        static bool HasArg(string[] args, string key)
        {
            foreach (var arg in args)
            {
                if (arg.Equals(key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// 取得參數鍵對應的數值
        /// </summary>
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