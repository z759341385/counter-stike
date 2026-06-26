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
