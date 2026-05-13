# 🎯 项目代号：CS-Rule-Engine (Backend)

## 📌 项目概述 & 技术栈
- **目标**：为 CS（Counter‑Strike）10 人内战提供随机规则抽取、投票、轮盘结算的后端服务。
- **技术栈**：Node.js (v18+) + Express + Socket.io + better‑sqlite3
- **约束**：不提供传统 REST 接口，所有业务通过 Socket.io 完成。

---

## 📂 项目结构（即将实现）
```
counter‑strike/
├─ src/
│  ├─ server.ts          # 主入口，装配 Express、Socket.io、CORS
│  ├─ db.ts              # SQLite 数据库初始化、查询
│  ├─ rooms.ts           # 房间管理（Map<roomId, RoomState>）
│  ├─ vote.ts            # 投票抽取 & 计分（加权随机）
│  ├─ spin.ts            # 轮盘算法（角度计算、落点判定）
│  └─ logger.ts          # 简易日志（后期可替换为 Winston）
├─ data/
│  └─ rules.db           # 启动时自动创建，存放规则表
├─ package.json
└─ tsconfig.json
```

---

## 📋 关键业务流程
1. **数据库（SQLite）**
   - 启动时自动创建 `./data/rules.db` 并建表 `rules`（id, name, description, weight, is_active）。
   - 若表为空，插入 5 条示例规则（如 “西部牛仔”、"静音突击" 等）。
2. **房间管理**
   - `Map<string, RoomState>` 全局维护房间状态。
   - 事件 `create_room`、`join_room`、`disconnect` 负责房间生命周期。
3. **投票流程**
   - `start_vote`：加权随机抽取 3 条启用规则，状态切换为 `VOTING`，广播 `vote_started`。
   - `submit_vote`：记录玩家投票，只广播 **总票数**（防窥屏）。
4. **防操盘轮盘结算**
   - `stop_vote_and_spin`（仅房主可调用）
     1. 统计 3 项投票总数，若为 0 则默认等权。
     2. 计算每个选项的扇区角度 (`startAngle`, `endAngle`)。
     3. 随机生成 `targetAngle = Math.random() * 360`。
     4. 判断落点所属扇区，得到 `winner`。
     5. 广播 `spin_wheel`，包含扇区信息、目标角度、动画时长、获胜规则。

---

## 🚀 实施路线（阶段式）
| 阶段 | 目标 | 关键任务 |
|------|------|----------|
| **1️⃣ 重构代码结构** | 按功能划分目录 `src/`，确保每个模块职责单一。 | 创建 `server.ts`、`db.ts`、`rooms.ts`、`vote.ts`、`spin.ts`。
| **2️⃣ 引入 TypeScript** | 开启类型检查，提高可维护性。 | 添加 `tsconfig.json`，把核心文件改为 `.ts`，安装 `typescript`、`@types/*`。
| **3️⃣ 错误处理 & 日志** | 统一异常捕获，输出可追溯日志。 | 实现 `logger.ts`（使用 `console.log`），在每个 Socket.io 事件外层做 `asyncHandler`。
| **4️⃣ 持久化快照（SQLite）** | 房间状态在服务器重启后可恢复。 | 在 `db.ts` 中提供 `saveRoomSnapshot(roomId, state)` 与 `loadSnapshots()`，启动时恢复已有房间（可选）。
| **5️⃣ 本机运行验证** | 使用 `npm run dev` 启动，手动测试关键事件。 | 运行 `npm install` → `npm run dev`，用 `socket.io-client` 简单脚本验证 `create_room`、`join_room`、`start_vote`、`stop_vote_and_spin`。
| **6️⃣ 后续部署** | PM2 守护进程上线。 | `pm2 start src/server.ts --name cs-engine`，后期可写 `ecosystem.config.js`。

---

## 📦 运行指令
```bash
# 安装依赖
npm install
# 开发调试（ts-node）
npm run dev   # 等价于: ts-node src/server.ts
# 后期使用 PM2 部署（示例）
pm i -g pm2
pm2 start src/server.ts --name cs-engine
```

---

## ✅ 下一步
- **确认**：以上计划是否满足您的需求？
- 如无异议，我将依据此结构在 `src/` 目录下逐步实现代码并提交。

如需进一步细化某一模块（例如投票加权算法细节），请告诉我。
"""