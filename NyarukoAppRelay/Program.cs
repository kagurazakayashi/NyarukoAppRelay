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
    /// 程式主要入口類別
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點
        /// </summary>
        /// <param name="args">命令列參數</param>
        [STAThread]
        static void Main(string[] args)
        {
            // 啟用應用程式視覺樣式與文字渲染設定
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 取得命令列參數值
            string cmdA = GetArgValue(args, "/A");
            string cmdE = GetArgValue(args, "/E");
            string iconPath = GetArgValue(args, "/I");
            string trayTitle = GetArgValue(args, "/T");

            // 若未提供必要的 /A 參數，則嘗試顯示幫助內容
            if (string.IsNullOrEmpty(cmdA))
            {
                if (!ShowHelp()) return;
                return;
            }

            // 啟動無表單的背景執行內容
            Application.Run(new RelayContext(cmdA, cmdE, iconPath, trayTitle));
        }

        /// <summary>
        /// 從內嵌資源讀取並顯示說明文件
        /// </summary>
        /// <returns>若讀取並顯示成功傳回 true，否則傳回 false</returns>
        static bool ShowHelp()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string resourceName = "NyarukoAppRelay.help.txt";
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return false;
                    // 使用 UTF8 編碼讀取內嵌的文本資源
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
        /// 解析命令列陣列以獲取指定鍵的值
        /// </summary>
        /// <param name="args">參數陣列</param>
        /// <param name="key">參數名稱 (如 /A)</param>
        /// <returns>參數值，若不存在則傳回 null</returns>
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