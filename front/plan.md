frontend_prompt = """# 🎯 项目代号：CS-Rule-Engine [Frontend 端]
# 👤 角色设定：你是一个顶级的 Vue 3 前端工程与 UI/UX 专家。
# 📝 任务目标：根据以下完整的 PRD，从零开始帮我构建这个内战规则引擎的【纯前端】部分。

---

## 一、 项目概述 & 核心技术栈
这是一个为 CS (Counter-Strike) 10人内战设计的“随机规则抽取与状态同步” Web 客户端。
* **特性：** 无需登录验证，通过 4 位数房间码联机；极具电竞质感的深色UI；严丝合缝的 CSS 轮盘转动动画。
* **技术栈：** Vue 3 (Composition API / <script setup>) + Vite + Tailwind CSS + Socket.io-client
* **通信基准：** 全程通过 Socket.io 与后端通信，无传统 REST 接口。

---

## 二、 前端 UI/UX 视觉规范
* **整体风格：** Dark Mode (暗黑模式)，背景底色使用 `#121212` 或暗色网格渐变。
* **组件质感：** 全面采用 Glassmorphism (磨砂玻璃) 风格，即半透明背景 (`bg-opacity-20` 等) 加上背景模糊 (`backdrop-blur-md`)，配合极细的半透明边框。
* **色彩点缀：** 强调色可使用反恐蓝 (CT) 或土黄色 (T) 元素。警示与错误信息使用高亮红色。排版需符合现代电竞 App 审美。

---

## 三、 核心视图流转与功能 (Views/Components)

建议使用条件渲染 (`v-if`) 或轻量级 Vue Router 在单页面内切换以下 4 个核心阶段：

### 1. 首页 (HomeView)
* **UI:** 极简的中心化布局。Logo/大标题，外加两个核心操作按钮。
* **交互:** - “创建对局”：弹出对话框输入昵称，触发 Socket `create_room`。成功后进入大厅。
  - “加入对局”：弹出对话框输入“4位数房间码”和“昵称”，触发 Socket `join_room`。

### 2. 大厅等候室 (LobbyView)
* **UI:** 顶部超大字号显示当前 `roomId`。主体区域用网格卡片 (Grid) 展示当前房间内的所有玩家 (`players` 数组)。
* **交互:** - 房主（`isHost === true`）的头像旁有高亮皇冠/星星标识。
  - 只有房主能看到屏幕底部的巨大“开始抽取本轮规则”按钮（触发 `start_vote`）。
  - 普通玩家底部显示“等待房主发车...”。

### 3. 投票区 (VotingView)
* **UI:** 监听 `vote_started` 后渲染。横向排列 3 张精美的规则卡片（显示名称和描述）。
* **交互:** - 玩家点击卡片完成单选（加高亮边框），触发 `submit_vote`。
  - 底部通过监听 `vote_progress` 实时显示进度条或文本：“已投票: X / 10”。

### 4. 轮盘结算动画 (RouletteView) [最具挑战的核心实现]
* **逻辑触发:** 监听到后端的 `spin_wheel` 事件，收到载荷 `{ sectors, targetAngle, duration, winner }`。
* **轮盘绘制:** 必须根据 `sectors` 的起止角度，利用 CSS `conic-gradient`、Canvas 或 SVG 动态绘制出一个由 3 个区块组成的饼图转盘。
* **动效执行:** - 利用 CSS Transition 实现平滑旋转：`transition: transform 5s cubic-bezier(0.25, 1, 0.5, 1)`。
  - 目标角度公式：`transform: rotate(${targetAngle + 360 * 5}deg)` (多转 5 圈增强视觉期待感)。
* **最终呈现:** 动画倒计时结束后，弹出一个炫酷的模态框高亮显示 `winner` 规则，并提供“确认/返回大厅”按钮。

---

## 四、 开发步骤执行要求

**步骤 1：** 生成 Vite + Vue 3 的初始化配置指令，以及 Tailwind CSS 和 `socket.io-client` 的安装与配置。
**步骤 2：** 创建全局的 Socket 通信服务 (`socket.js` 或利用组合式函数 `useSocket.js`)。
**步骤 3：** 构建深色磨砂风格的基础 UI 框架与首页模块。
**步骤 4：** 实现大厅列表模块与投票卡片模块。
**步骤 5：** 实现最核心的模块：接收后端数据并动态渲染与旋转的**轮盘抽奖组件**。

请确认你已理解上述需求，并开始执行**步骤 1**。
"""

with open("BACKEND_AI_PROMPT.md", "w", encoding="utf-8") as f:
    f.write(backend_prompt)
    
with open("FRONTEND_AI_PROMPT.md", "w", encoding="utf-8") as f:
    f.write(frontend_prompt)