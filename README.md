# CS-Rule-Engine

本仓库包含两个主要部分：提供实时通信服务和游戏逻辑状态管理的后端 (`backend`)，以及负责与玩家交互的前端 Web 页面 (`front`)。

## 目录结构
- `backend/` - Node.js + Express + Socket.IO 后端
- `front/` - Vue 3 + Vite + TailwindCSS 前端

## 1. 后端 (Backend)

后端使用 TypeScript 编写，负责管理所有的对局状态（包括抽卡模式和内鬼模式），以及提供 Websocket 通信支持。

### 环境准备
请确保你已经安装了 Node.js (推荐 v18+)。

```bash
cd backend
npm install
```

### 运行开发环境
如果在开发阶段，你可以直接使用 `ts-node` 来启动，不需要预先编译：
```bash
npm run dev
```
后端服务默认将在 `http://localhost:3001` 启动。

### 生产环境打包与运行
在部署到服务器等生产环境时，你需要先将 TypeScript 代码编译成原生的 JavaScript。

```bash
# 1. 编译打包 (会生成 dist 目录)
npm run build

# 2. 运行打包好的代码
npm run start
```
*提示：生产环境中建议使用 `pm2` 等进程守护工具来管理 `npm run start`。*

---

## 2. 前端 (Front)

前端是一个由 Vite 驱动的 Vue 3 单页面应用。

### 环境准备
```bash
cd front
npm install
```

### 运行开发环境
启动 Vite 本地开发服务器，支持热更新（HMR）：
```bash
npm run dev
```
前端默认将在 `http://localhost:5173` 启动。

### 生产环境打包与部署
当你准备将应用发布上线时，你需要构建生产版本的静态文件。

```bash
# 执行构建，结果会输出到 front/dist 目录下
npm run build
```

构建完成后，你可以将 `front/dist` 目录下的所有静态文件部署到任何 Web 服务器上（如 Nginx, Vercel, Netlify, 或者托管在云端的静态网页托管服务）。

如果需要在本地预览打包出来的效果，可以使用：
```bash
npm run preview
```

---

## 3. 游戏服务端集成 (Server Integration)

由于本项目（内战抽卡系统）需要在 CS2 游戏内实现自动化规则执行，我们引入了通过 RCON 协议与游戏服务端通信的测试模块。

### 依赖安装
项目中使用了 `rcon` 库用于与服务端进行 TCP 通信。如果在本地开发，请确保已在根目录安装该依赖：
```bash
npm install rcon
```

### 本地服务端测试流程
我们提供了一个单功能测试脚本 `server-integration/test_rcon.js`。它的作用是验证 Node.js 后端是否能成功连接本地测试的 CS2 游戏服务端，并将抽卡获取的规则（例如“西部牛仔”）直接发送到游戏公屏中。

**测试步骤：**
1. **准备 CS2 服务端**：在本地旧电脑或开发机上启动 CS2 Dedicated Server（专用服务端）。
2. **开启 RCON**：确保游戏服务端的启动参数或 `server.cfg` 中开启了 RCON 密码，例如包含配置：`+rcon_password your_test_password`。
3. **配置测试脚本**：打开 `server-integration/test_rcon.js` 文件，将代码顶部的 `cs2ServerIp` 改为您的服务器局域网 IP（如果在同一台电脑测试则保持 `127.0.0.1`），并填入您在第 2 步设置的 RCON 密码。
4. **执行测试指令**：
在项目根目录的终端中运行：
```bash
node server-integration/test_rcon.js
```
> 若配置无误，终端会输出“✅ 成功连接并认证到 CS2 服务器 RCON！”，同时您会在游戏内的左下角聊天框中看到系统下发的内战规则广播。
