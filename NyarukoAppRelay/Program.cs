using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace NyarukoAppRelay
{
    /// <summary>
    /// 程式啟動類別，負責處理命令列參數並初始化應用程式環境
    /// </summary>
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點
        /// </summary>
        /// <param name="args">從命令列傳入的參數陣列</param>
        [STAThread]
        static void Main(string[] args)
        {
            // 啟用視窗視覺樣式（如按鈕的圓角效果等）
            Application.EnableVisualStyles();
            // 設定相容的文字渲染方式
            Application.SetCompatibleTextRenderingDefault(false);

            // 解析各項功能參數
            string cmdA = GetArgValue(args, "/A");         // 監控的目標程式與參數
            string cmdE = GetArgValue(args, "/E");         // 目標結束後執行的程式
            string iconPath = GetArgValue(args, "/I");     // 自定義圖示路徑
            string trayTitle = GetArgValue(args, "/T");    // 系統匣自定義標題
            bool useWindowMode = HasArg(args, "/W");      // 是否啟用視窗監控模式

            // 若未提供核心監控對象 (/A)，則顯示幫助文件後退出
            if (string.IsNullOrEmpty(cmdA))
            {
                if (!ShowHelp()) return;
                return;
            }

            // 啟動主要的背景監控上下文邏輯
            Application.Run(new RelayContext(cmdA, cmdE, iconPath, trayTitle, useWindowMode));
        }

        /// <summary>
        /// 從內嵌資源讀取並顯示說明文件 (help.txt)
        /// </summary>
        /// <returns>若顯示成功傳回 true，資源不存在或讀取失敗傳回 false</returns>
        static bool ShowHelp()
        {
            try
            {
                // 取得目前正在執行的程式集
                var assembly = Assembly.GetExecutingAssembly();
                // 內嵌資源的完整路徑名稱
                string resourceName = "NyarukoAppRelay.help.txt";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return false;

                    // 使用 UTF-8 編碼讀取文字內容，避免中文亂碼
                    using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        // 取得程式的版本資訊
                        AssemblyName assemblyName = assembly.GetName();
                        string assemblyVersion = assemblyName.Version?.ToString() ?? "1.0.0";

                        // 顯示說明內容對話框（標題保持簡體中文）
                        MessageBox.Show(reader.ReadToEnd(), "NyarukoAppRelay v" + assemblyVersion + " by KagurazakaYashi");
                        return true;
                    }
                }
            }
            catch
            {
                // 發生任何異常時不中斷程式，直接傳回失敗
                return false;
            }
        }

        /// <summary>
        /// 檢查命令列參數中是否存在指定的開關 (Flag)
        /// </summary>
        /// <param name="args">參數陣列</param>
        /// <param name="key">要尋找的鍵名 (不分大小寫)</param>
        /// <returns>存在傳回 true，否則傳回 false</returns>
        static bool HasArg(string[] args, string key)
        {
            foreach (var arg in args)
            {
                if (arg.Equals(key, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        /// <summary>
        /// 從命令列參數中取得指定鍵對應的後續數值
        /// </summary>
        /// <param name="args">參數陣列</param>
        /// <param name="key">參數名稱 (如 /A)</param>
        /// <returns>對應的參數值字串，若找不到或格式錯誤則傳回 null</returns>
        static string GetArgValue(string[] args, string key)
        {
            for (int i = 0; i < args.Length; i++)
            {
                // 當匹配到鍵名且其後方還有資料時，視為該鍵的值
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    return args[i + 1];
                }
            }
            return null;
        }
    }
}
