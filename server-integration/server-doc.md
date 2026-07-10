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
### 常见问题排查 (Troubleshooting)
- **提示 "Connection Refused" 或 "Timeout"**：请检查服务端 IP 是否正确，以及 CS2 服务端进程是否确实启动成功。检查防火墙是否拦截了 `27015` TCP 端口。
- **提示 "Auth failed" 或一直无响应**：请检查脚本中的 `rconPassword` 是否与服务端设置的完全一致。如果在服务端启动参数中没有加 `+usercon` 可能会导致认证失败。

---

## 第五步：编写、编译与部署 C# 插件 (CounterStrikeSharp)

如果您需要实现比 RCON 更复杂的玩家交互（如在游戏内弹出海克斯选择菜单、伤害吸血、移速加成等），需要使用 **CounterStrikeSharp (CSS)** 编写 C# 插件。

我们已经在目录中准备好了插件源码文件 [CS2HextechPlugin.cs](file:///c:/Users/75934/Documents/GitHub/counter-stike/server-integration/CS2HextechPlugin.cs) 以及自动化构建部署脚本 [build_and_deploy_plugin.ps1](file:///c:/Users/75934/Documents/GitHub/counter-stike/server-integration/build_and_deploy_plugin.ps1)。

### 选项 A：使用自动化部署脚本（推荐）

1. **检查与配置路径**：
   用文本编辑器打开 `server-integration/build_and_deploy_plugin.ps1`，修改 `$ServerPath` 为您本机实际的 CS2 服务端路径（默认是 `C:\steamcmd\cs2server`）。
2. **执行脚本**：
   在 PowerShell 中运行该脚本：
   ```powershell
   cd server-integration
   .\build_and_deploy_plugin.ps1
   ```
   脚本将自动创建 .NET 项目、拉取 `CounterStrikeSharp.API` 依赖包、移动源码、完成编译并直接发布到您的服务端中。

### 选项 B：手动操作指南

若您想手动完成整个流程，可以遵循以下三步：

#### 1. 创建项目并拉取依赖
1. 打开终端（CMD 或 PowerShell），在主目录下运行：
   ```bash
   dotnet new classlib -o CS2HextechPlugin
   ```
2. 进入刚刚创建好的文件夹：
   ```bash
   cd CS2HextechPlugin
   ```
3. 引入 CounterStrikeSharp 框架依赖包：
   ```bash
   dotnet add package CounterStrikeSharp.API
   ```
4. 删掉自动生成的 `Class1.cs` 文件，然后把 [CS2HextechPlugin.cs](file:///c:/Users/75934/Documents/GitHub/counter-stike/server-integration/CS2HextechPlugin.cs) 复制到该目录下。

#### 2. 编译项目
在 `CS2HextechPlugin` 项目文件夹下执行编译命令，生成 Release 版本：
```bash
dotnet build -c Release
```
编译成功后，生成的 DLL 二进制模块将存放在：
`CS2HextechPlugin/bin/Release/net8.0/CS2HextechPlugin.dll`

#### 3. 部署到服务器
1. 进入您的 CS2 专用服务端插件文件夹下：
   `\game\csgo\addons\counterstrikesharp\plugins\`
2. 新建一个与插件同名的文件夹：`CS2HextechPlugin`。
3. 将编译好的 `CS2HextechPlugin.dll` 拷贝到该文件夹内。
4. 重启 CS2 服务端，或者在游戏控制台输入 `css_plugins reload` 即可加载。


---

## 第六步：使用 CounterStrikeSharp (CS#) 插件进行深度定制

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

[CS2 服务器正式第一回合开始]
       │
       ▼ (CS# 插件监听到事件)
[CS# 插件发送 HTTP/接口请求] ───> 告诉 Node.js 后端：“第一局已开始，请发牌”
                                       │
                                 (Node.js 随机挑选规则)
                                       ▼
[CS# 插件收到发牌结果] <─── 返回规则：{ rule: "低重力大战", desc: "重力减半", gravity: 400 }
       │
       ├─► [广播视觉弹窗]：所有在线玩家的屏幕中央同时弹出 5 秒 HTML 炫酷提示
       │
       └─► [自动执行规则]：插件修改服务端参数（如 `sv_gravity 400`），或者调整玩家血量

---

## 第七步：进阶配置 - 切换官方服役地图与创意工坊地图

在使用服务器联调或者游玩时，我们经常需要更换地图。

### A. 切换官方服役地图 (Active Duty Maps)
如果你想玩官方自带的常规地图（如炼狱小镇、荒漠迷城等），非常简单，不需要任何下载配置。
1. **在启动脚本中设置初始地图**：在 `start_server.bat` 中，修改 `+map` 参数。例如 `+map de_mirage`（荒漠迷城）或 `+map de_inferno`（炼狱小镇）。
2. **游戏中途动态切换**：在服务器黑窗口、Node.js RCON 脚本或游戏内的控制台中，直接输入 `map 地图代码` 即可瞬间切换。
   **常见官方地图代码表**：
   - 炙热沙城2：`de_dust2`
   - 荒漠迷城：`de_mirage`
   - 炼狱小镇：`de_inferno`
   - 核子危机：`de_nuke`
   - 死亡游乐园：`de_overpass`
   - 眩晕大厦：`de_vertigo`
   - 远古遗迹：`de_ancient`
   - 阿努比斯：`de_anubis`

### B. 游玩创意工坊地图 (Workshop Maps)
想玩社区制作的练枪图或趣味地图，则需要以下配置：

### 1. 查找地图的 Workshop ID
1. 在 Steam 社区的 CS2 创意工坊中找到你想玩的地图（确保该地图的可见性是**公开**的）。
2. 在浏览器或者客户端复制网页链接，找到链接末尾的 `?id=xxx`，这串数字就是 Workshop ID（例如：`3070290869`）。

### 2. 获取 Steam 游戏服务器登录令牌 (GSLT)
为了让服务器能够正常连接 Steam 下载创意工坊内容，强烈建议配置 GSLT：
1. 访问 [Steam 游戏服务器帐户管理](https://steamcommunity.com/dev/managegameservers)（需登录 Steam）。
2. 在底部的“创建新的游戏服务器帐户”中，**App ID 填入 730** (CS2 的游戏编号)，备忘录随便填（如“我的测试服”），点击创建。
3. 复制生成的 **登录令牌 (Login Token)**。

### 3. 修改启动脚本并加载地图
在你的 `start_server.bat` 中，添加 GSLT 令牌参数，并指定要加载的创意工坊地图：
- **修改启动脚本**：
  在原本的启动参数后追加 `+sv_setsteamaccount "你的令牌" +host_workshop_map 3070290869`。
  *示例*：
  ```bat
  cs2.exe -dedicated -usercon +game_type 0 +game_mode 1 +sv_setsteamaccount "你的令牌" +host_workshop_map 3070290869 +rcon_password "123456" -insecure
  ```
  *(注意：删除了原本的 `+map de_dust2`，改为用 `+host_workshop_map` 来加载默认地图)*

### 4. 游戏中途切换创意工坊地图
如果服务器正在运行，你也可以随时动态切换地图：
- 在 Node.js RCON 脚本中，或者在游戏开发者控制台 (`~`) 中，输入命令：
  ```text
  host_workshop_map 3070290869
  ```
- *服务器会开始在后台自动下载地图文件，下载完成后会自动切换地图并重新加载对局！*
