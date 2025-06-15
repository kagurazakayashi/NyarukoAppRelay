![icon](NyarukoAppRelay/icon.ico)

# NyarukoAppRelay

该程序用于启动一个软件，并监控这个软件是否退出。如果该软件退出，就执行另一条命令并结束。

## 使用方法

- `/A`: 要执行的软件路径和参数
- `/E`: 等上面的程序执行完了要执行的软件路径和参数（可选）
- `/I`: 指定一个任务栏图标（可选）
- `/T`: 自定义任务栏标题（可选）
- `/W`: 如果添加/W，则检测到所有**窗口**都关闭视为退出；如果不添加/W，则检测到**进程**结束视为退出。

## 示例

`/A "notepad.exe 1.txt" /E "cmd.exe /k TYPE 1.txt" /I "C:\Windows\System32\cmmon32.exe" /T "请在记事本中编辑内容，编辑完成后将显示出来。" /W`

## 环境要求

.NET Framework 4.8

## LICENSE

Copyright (c) 2025 [神楽坂雅詩](https://github.com/KagurazakaYashi) [NyarukoAppRelay](https://github.com/kagurazakayashi/NyarukoAppRelay) is licensed under Mulan PSL v2. You can use this software according to the terms and conditions of the Mulan PSL v2. You may obtain a copy of Mulan PSL v2 at: <http://license.coscl.org.cn/MulanPSL2> THIS SOFTWARE IS PROVIDED ON AN “AS IS” BASIS, WITHOUT WARRANTIES OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO NON-INFRINGEMENT, MERCHANTABILITY OR FIT FOR A PARTICULAR PURPOSE. See the Mulan PSL v2 for more details.
