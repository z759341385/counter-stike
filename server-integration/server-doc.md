# CS2 游戏服务端与 Node.js 集成测试指南

本文档详细介绍了如何使用 RCON (Remote Console) 协议，将我们的 Node.js 后端与本地的 CS2 (Counter-Strike 2) 专用服务端建立通信。通过此方案，我们可以在 Web 前端完成“抽卡”后，将规则指令直接发送至游戏内执行。

## 第一步：准备 CS2 专用服务端 (Dedicated Server)

为了进行本地开发与测试，我们需要在局域网内（或同一台电脑上）运行一个轻量级的 CS2 专用服务端。

### Windows 系统快速搭建（推荐测试环境使用）
1. **下载 SteamCMD**：前往 Steam 官方文档下载 SteamCMD 工具，解压到一个文件夹（例如 `C:\steamcmd`）。
2. **下载服务端文件**：在 SteamCMD 目录下打开命令行，依次输入：
   ```bash
   steamcmd
   force_install_dir .\cs2server\
   login anonymous
   app_update 730 validate
   quit
   ```
   *注意：服务端安装后大约需要 35GB-40GB 空间。但由于 SteamCMD 在下载和解压过程中需要临时缓存，**建议预留至少 60GB-80GB 的可用磁盘空间**。如果遇到 `Error App 730 state is 0x202 after update job` 错误，通常代表**磁盘空间不足**或**权限受限**。*

### Linux 系统快速搭建
如果您在旧电脑上安装了 Ubuntu，推荐使用 **LinuxGSM** 脚本进行一键安装：
```bash
wget -O linuxgsm.sh https://linuxgsm.sh && chmod +x linuxgsm.sh && bash linuxgsm.sh cs2server
./cs2server install
```

---

## 第二步：开启与配置 RCON

RCON 允许外部程序连接到游戏服务端并执行控制台命令。CS2 默认情况下可能没有启用该功能。

1. **设置 RCON 密码**：
   在启动 CS2 服务端时，必须在启动参数中显式指定一个 RCON 密码。
   例如，如果您通过命令行或快捷方式启动服务器，请加上：
   ```text
   +rcon_password "your_test_password"
   ```
2. **(可选) 修改 server.cfg**：
   您也可以在服务端的 `game/csgo/cfg/server.cfg` 文件中添加一行：
   ```text
   rcon_password "your_test_password"
   ```
3. **启动服务器**：
   启动您的 CS2 测试服，确保服务器处于“已启动”状态。
   *Windows 启动示例:*
   ```bat
   \cs2server\game\bin\win64\cs2.exe -dedicated -usercon +game_type 0 +game_mode 1 +map de_dust2 +rcon_password "your_test_password"
   ```

---

## 第三步：配置 Node.js 测试脚本

我们已经在当前目录下准备好了一个简单的测试脚本 `test_rcon.js`，它的作用是连接服务端并发送一条公屏广播测试消息。

1. **安装依赖**：
   确保您在项目根目录已经执行过：
   ```bash
   npm install rcon
   ```
2. **修改 IP 与密码**：
   打开 `test_rcon.js` 文件，找到配置区域。
   - `cs2ServerIp`：填写您运行 CS2 服务端电脑的 **局域网 IP**。如果您的服务端和 Node.js 项目运行在同一台电脑上，请填写 `127.0.0.1`。
   - `rconPassword`：填写您在“第二步”中设置的密码（例如 `"your_test_password"`）。
   - `cs2ServerPort`：保持默认的 `27015`（除非您在启动服务端时修改了端口）。

---

## 第四步：执行测试指令

当游戏服务器已经启动，并且脚本中的 IP/密码 配置完毕后，即可进行联调测试。

1. 在项目根目录（`/Users/zhc/project/nodeJs/counter-strike`）打开终端。
2. 运行以下命令启动测试脚本：
   ```bash
   node server-integration/test_rcon.js
   ```
3. **预期结果**：
   - 终端中应该输出：`✅ 成功连接并认证到 CS2 服务器 RCON！` 以及 `正在下发单功能测试指令...`
   - 如果您此时在游戏内加入了这个服务器，您会在左下角的聊天窗口看到一条由服务端发出的绿色/系统提示文本，例如：
     > `[内战抽卡系统] 本回合规则已锁定: 西部牛仔! 仅限沙鹰和左轮！`

### 常见问题排查 (Troubleshooting)
- **提示 "Connection Refused" 或 "Timeout"**：请检查服务端 IP 是否正确，以及 CS2 服务端进程是否确实启动成功。检查防火墙是否拦截了 `27015` TCP 端口。
- **提示 "Auth failed" 或一直无响应**：请检查脚本中的 `rconPassword` 是否与服务端设置的完全一致。如果在服务端启动参数中没有加 `+usercon` 可能会导致认证失败。

---

## 第五步：使用 CounterStrikeSharp (CS#) 插件进行深度定制

如果 RCON 远程控制指令无法满足更深度的规则定制（例如修改玩家血量上限、限制特定武器购买等），我们需要在 CS2 专用服务器上部署 **CounterStrikeSharp (CS#)** 插件。

已为您在项目目录下准备好了用于修改玩家 HP 的插件脚本模板：[HpModifier.cs](file:///c:/Users/75934/Documents/GitHub/counter-stike/server-integration/HpModifier.cs)。

### 1. 服务端运行环境准备
1. **安装 Metamod:Source**：
   * 前往 [Metamod 官网](https://www.sourcemm.net/downloads.php) 下载最新的稳定版。
   * 将其解压并合并到你 CS2 专用服务端的 `game/csgo/` 目录下（最终应该能看到 `game/csgo/addons/metamod/` 路径）。
2. **安装 CounterStrikeSharp**：
   * 前往 [CounterStrikeSharp Releases](https://github.com/roccodev/CounterStrikeSharp/releases) 下载适合您服务器系统版本的软件包（内含 Runtime）。
   * 将其解压并合并到你 CS2 服务端的 `game/csgo/` 目录下（最终在 `game/csgo/addons/` 下会出现 `counterstrikesharp` 目录）。

### 2. 编译 C# 插件项目
您需要在一台安装了 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 的电脑上执行编译：

1. **创建新项目**：
   打开终端或命令行，初始化一个标准的 C# 类库：
   ```bash
   dotnet new classlib -o HpModifier
   cd HpModifier
   ```
2. **添加 API 依赖包**：
   通过 NuGet 引入官方的 CS# 依赖：
   ```bash
   dotnet add package CounterStrikeSharp.API
   ```
3. **添加源文件**：
   * 将项目本地的 [HpModifier.cs](file:///c:/Users/75934/Documents/GitHub/counter-stike/server-integration/HpModifier.cs) 文件复制到新建的 `HpModifier` 文件夹下，删除或覆盖原有的 `Class1.cs`。
4. **编译打包**：
   ```bash
   dotnet build -c Release
   ```
   * 编译完成后，前往项目的 `./bin/Release/net8.0/` 目录下找到生成的 `HpModifier.dll`。

### 3. 部署插件并测试
1. **上传 DLL**：
   * 在你的 CS2 服务端路径 `game/csgo/addons/counterstrikesharp/plugins/` 下新建一个子文件夹 `HpModifier`。
   * 将刚才生成的 `HpModifier.dll` 上传并放入该文件夹中。
2. **加载插件**：
   * 启动/重启服务器，插件将自动加载。
   * 如果服务器正在运行，你也可以在控制台或 RCON 中直接执行重载命令：
     ```text
     css_plugins reload HpModifier
     ```
3. **功能验证**：
   * **复活自动生效**：玩家在局中或新回合复活（Spawn）时，血量上限和当前血量均会自动被设置成 `200`。
   * **管理员命令**：在游戏控制台或 RCON 终端中输入 `css_hp200`，可以手动且即时地把当前在场的所有在线玩家血量重置为 `200`。

