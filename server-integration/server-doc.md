# CS2 游戏服务端与 Node.js 集成联调完全指南 (小白向)

本文档专为“零基础”开发者编写，详细介绍了从零开始搭建本地 CS2 专用服务器 (Dedicated Server)，并使用 Node.js 通过 RCON 协议与其建立通信的完整可用流程。通过此方案，我们可以在 Web 前端完成“抽卡”后，将规则指令直接发送至游戏内执行。

---

## 零、前期准备

1. **硬件要求**：至少准备 40GB 可用硬盘空间（推荐固态硬盘）。
2. **环境要求**：
   - 您的电脑已安装 Node.js。
   - 您的电脑已安装 Steam 和 CS2（用于自己进入游戏进行测试）。

---

## 第一步：搭建本地 CS2 专用服务端

对于小白用户，强烈建议在 **Windows 系统**下进行本地测试，流程最直观。

### 1. 下载并配置 SteamCMD
- 前往官方链接下载 SteamCMD：[SteamCMD 官方下载](https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip)
- 在您的硬盘（如 D 盘）根目录创建一个名为 `steamcmd` 的文件夹（即 `D:\steamcmd`）。
- 将下载压缩包内的 `steamcmd.exe` 解压到该文件夹中。
- **双击运行** `steamcmd.exe`，它会自动弹出一个黑框并下载更新文件，等待它停止滚动并出现 `Steam> ` 提示符即可关闭该窗口。

### 2. 一键下载 CS2 服务端
- 在 `D:\steamcmd` 目录下，新建一个文本文档，重命名为 `update_cs2.bat`（注意：必须显示文件扩展名并修改为 `.bat`，而不是 `.bat.txt`）。
- 右键点击 `update_cs2.bat`，选择“编辑”（或用记事本打开），填入以下代码并保存：
  ```bat
  @echo off
  steamcmd.exe +login anonymous +force_install_dir .\cs2server\ +app_update 730 validate +quit
  pause
  ```
- **双击运行 `update_cs2.bat`**。程序会自动连接 Steam 并开始下载 CS2 服务端文件（约 35GB）。由于文件较大，请耐心等待（可能需要几十分钟到几个小时），直到最后提示“按任意键继续”即可关闭。
---

## 第二步：配置并启动游戏服务器

RCON (Remote Console) 是允许我们用 Node.js 代码远程控制游戏服务器的核心协议。

### 1. 创建服务器启动脚本
- 进入我们刚刚下载好服务端的目录：`D:\steamcmd\cs2server\game\bin\win64\`
- 在该目录下，新建一个名为 `start_server.bat` 的文件。
- 右键编辑该文件，填入以下启动命令并保存：
  ```bat
  cs2.exe -dedicated -usercon +game_type 0 +game_mode 1 +map de_dust2 +rcon_password "123456" -insecure
  ```
  > **参数说明：**
  > `-dedicated`：作为专用服务器运行（不带游戏画面，占用极低）。
  > `-usercon`：**关键参数**，启用 RCON 远程控制台功能。
  > `+game_type 0 +game_mode 1`：代表启动休闲模式。
  > `+map de_dust2`：默认加载“炙热沙城2”地图。
  > `+rcon_password "123456"`：**关键参数**，设置 RCON 密码为 `123456`（可自行修改），我们的 Node.js 必须通过这个密码才能连接。
  > `-insecure`：关闭 VAC 反作弊（本地测试必备，否则无法使用部分自制插件或指令）。

### 2. 启动服务器
- 双击运行刚刚创建的 `start_server.bat`。
- 会弹出一个黑色的控制台窗口，这代表你的 CS2 服务器正在运行。
- **注意：在接下来的所有测试中，请始终保持这个黑窗口开启，不要关闭！**

---

## 第三步：作为玩家进入你的测试服务器

在让 Node.js 发号施令之前，我们需要先进游戏，确认服务器是否正常。

1. 打开 Steam，正常启动你的 CS2 游戏。
2. 开启控制台：在游戏主界面，进入“设置” -> “游戏设置” -> 找到“启用开发者控制台 (~)”，设置为“是”。
3. 按下键盘左上角的 `~` 键（Esc下方），呼出控制台。
4. 在控制台输入以下命令并回车：
   ```text
   connect 127.0.0.1
   ```
5. 等待读条加载，如果你成功进入了 de_dust2 并且可以到处跑动，说明你的服务器搭建完美成功！

---

## 第四步：配置 Node.js 测试脚本

现在我们让 Node.js 与刚刚建好的服务器连接。
我们已经在当前项目目录下（`server-integration/`）准备好了测试脚本 `test_rcon.js`。

### 1. 安装所需依赖
在本项目根目录打开终端，运行：
```bash
npm install rcon
```

### 2. 确认脚本配置
打开项目中的 `server-integration/test_rcon.js` 文件，找到配置项并确保与服务器一致：
```javascript
const cs2ServerIp = '127.0.0.1'; // 保持 127.0.0.1，代表本机
const rconPassword = '123456';   // 必须与第二步 bat 脚本里设置的密码完全一致
const cs2ServerPort = 27015;     // 默认端口，保持不变
```

---

## 第五步：执行联调测试 (见证奇迹)

准备好迎接激动人心的时刻了吗？

1. 确认你的 **CS2 服务器黑窗口** 正在运行。
2. 确认你作为玩家**正处于该游戏服务器中**。
3. 在 Node.js 项目根目录的终端中，运行测试脚本：
   ```bash
   node server-integration/test_rcon.js
   ```
4. **预期结果**：
   - 您的终端会输出：`✅ 成功连接并认证到 CS2 服务器 RCON！`
   - **切回游戏**：您会在游戏左下角的聊天框中看到一条由服务端发出的绿色系统提示：
     > `[内战抽卡系统] 本回合规则已锁定: 西部牛仔! 仅限沙鹰和左轮！`

这证明我们的 Node.js 已经拥有了向 CS2 发送任意游戏指令（如给钱、发枪、改重力）的能力！

---

## 附录：常见问题排查 (Troubleshooting)

- **Q1: 游戏里输入 `connect 127.0.0.1` 进不去？**
  - 检查服务器启动黑窗口是否还在运行。
  - 检查 `start_server.bat` 中是否正确添加了 `-insecure` 参数。

- **Q2: Node 终端报错 `Connection Refused` 或 `Timeout`？**
  - 如果你在 Windows 的命令提示符（黑窗口）里按到了鼠标左键，进程可能会被“暂停”。请在黑窗口里按一下**回车键**恢复运行。
  - 检查防火墙是否拦截了 `27015` 端口（本地测试一般不会，但以防万一）。

- **Q3: Node 终端报错 `Auth failed` 或一直卡住无响应？**
  - 密码错误！请严格检查 `test_rcon.js` 里的密码与 `start_server.bat` 里的 `+rcon_password` 是否**完全一致**。
  - 检查启动参数是否漏掉了 `-usercon`，没有它就无法进行远程控制。
