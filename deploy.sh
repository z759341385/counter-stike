#!/bin/bash
# -------------------------------------------------------------------------
# CS-Rule-Engine 自动化发版脚本 (Shell + rsync)
# -------------------------------------------------------------------------

# ==================== 服务器配置参数 ====================
SERVER_USER="root"                           # 服务器的 SSH 用户名
SERVER_IP="106.12.165.85"                    # 服务器 IP 地址
SERVER_PORT="22"                             # 服务器的 SSH 端口（默认 22，如改过请修改）
FRONT_SERVER_DIR="/data/www/cs_front"        # 服务器前端 Nginx 目录
BACKEND_SERVER_DIR="/www/wwwroot/counter-strike/backend" # 服务器后端代码目录
PM2_APP_NAME="backend"                       # 宝塔 Node 项目管理中的名称
# ========================================================

# 发生错误即退出脚本
set -e

echo "🚀 [1/4] 开始构建前端..."
cd front
npm install
npm run build
cd ..
echo "✅ 前端构建完成！"

echo "⚙️  [2/4] 开始构建后端..."
cd backend
npm install
npm run build
cd ..
echo "✅ 后端构建完成！"

echo "📦 [3/4] 正在上传前端产物到服务器 ($FRONT_SERVER_DIR)..."
rsync -avz --delete -e "ssh -p $SERVER_PORT" ./front/dist/ $SERVER_USER@$SERVER_IP:$FRONT_SERVER_DIR/
echo "✅ 前端上传完成！"

echo "📦 [3/4] 正在上传后端代码到服务器 ($BACKEND_SERVER_DIR)..."
# 注意：取消了排除 dist，因为我们需要把本地打包好的 dist 上传上去
rsync -avz --delete --exclude='node_modules' --exclude='.env*' -e "ssh -p $SERVER_PORT" ./backend/ $SERVER_USER@$SERVER_IP:$BACKEND_SERVER_DIR/
echo "✅ 后端上传完成！"

echo "🔄 [4/4] 正在服务器上重启后端服务..."
ssh -p $SERVER_PORT $SERVER_USER@$SERVER_IP "bash -l -s" << 'EOF'
  # 尝试多种方式寻找 node / yarn / pm2
  NODE_BIN=""
  
  # 1. 优先查找宝塔常用的 Node 版本的 bin 目录
  for d in /www/server/nodejs/*/bin /www/server/nvm/versions/node/*/bin /usr/local/bin /usr/bin; do
    if [ -x "$d/node" ]; then
      NODE_BIN="$d"
      # 如果同时有 pm2，则这大概率是我们要找的目录
      if [ -x "$d/pm2" ]; then
        break
      fi
    fi
  done

  # 2. 如果没找到带 pm2 的目录，在 /www 目录下全局搜索 pm2 文件的路径
  if [ -z "$NODE_BIN" ] || [ ! -x "$NODE_BIN/pm2" ]; then
    PM2_PATH=$(find /www -name pm2 -type f -executable 2>/dev/null | grep -v "\.pm2" | head -n 1)
    if [ -n "$PM2_PATH" ]; then
      NODE_BIN=$(dirname "$PM2_PATH")
    fi
  fi

  # 3. 输出调试信息并配置 PATH
  if [ -z "$NODE_BIN" ]; then
    echo "❌ 未能在服务器上自动找到 Node/PM2 环境，开启调试模式："
    echo "当前 PATH: $PATH"
    echo "宝塔 Node 目录内容:"
    ls -la /www/server/nodejs 2>/dev/null || echo "未找到 /www/server/nodejs"
    echo "全局 node 路径: $(which node 2>/dev/null || echo '未找到')"
    echo "全局 pm2 路径: $(which pm2 2>/dev/null || echo '未找到')"
  else
    echo "🔍 成功找到 Node 环境路径: $NODE_BIN"
    export PATH=$NODE_BIN:$PATH
  fi

  # 由于我们要访问 BACKEND_SERVER_DIR，但这是在 EOF 里，我们直接使用绝对路径
  cd /www/wwwroot/counter-strike/backend
  
  # 使用 yarn 安装生产环境依赖
  yarn install --production
  
  # 使用 pm2 重启宝塔中的项目 (如果 pm2 存在)
  if command -v pm2 &>/dev/null; then
    pm2 restart backend
    pm2 save
  else
    echo "⚠️  未在当前环境中找到 pm2 命令，正在尝试通过宝塔 Python API 重启 Node 项目..."
    
    # 使用宝塔内置的 Python 环境和 API 进行项目重启
    # 如果重启成功退出状态为 0，失败为 1
    if /www/server/panel/pyenv/bin/python -c "
import sys
sys.path.append('/www/server/panel/class')
success = False

try:
    import panelNode
    import public
    get = public.dict_obj()
    get.name = 'backend'
    p = panelNode.panelNode()
    for method in ['restart_node_project', 'RestartNodeProject', 'restart_project', 'RestartProject']:
        if hasattr(p, method):
            res = getattr(p, method)(get)
            print('通过 panelNode.%s 重启成功: %s' % (method, res))
            success = True
            break
except Exception as e:
    pass

if not success:
    try:
        import node_projects
        import public
        get = public.dict_obj()
        get.name = 'backend'
        p = node_projects.node_projects()
        for method in ['restart_project', 'RestartProject', 'restart_node_project', 'RestartNodeProject']:
            if hasattr(p, method):
                res = getattr(p, method)(get)
                print('通过 node_projects.%s 重启成功: %s' % (method, res))
                success = True
                break
    except Exception as e:
        pass

if not success:
    sys.exit(1)
"; then
      echo "✅ 宝塔 API 重启指令执行成功！"
    else
      echo "❌ 宝塔 Python API 重启失败，正在尝试 Systemd 服务重启..."
      SYSTEMD_SERVICE=$(systemctl list-units --type=service --all | grep -Fi "backend" | awk '{print $1}' | head -n 1)
      if [ -n "$SYSTEMD_SERVICE" ]; then
        systemctl restart "$SYSTEMD_SERVICE"
        echo "✅ Systemd 服务重启成功！"
      else
        echo "⚠️  服务重启失败，正在尝试通过强制结束进程并后台重新启动..."
        
        # 1. 查找当前运行的后端 Node 进程 PID (通过端口或进程名)
        PID=$(lsof -t -i:3001 2>/dev/null || ps aux | grep "dist/server.js" | grep -v "grep" | awk '{print $2}' | head -n 1)
        
        if [ -n "$PID" ]; then
          echo "🎯 找到旧的进程 PID: $PID，正在强制结束..."
          kill -9 $PID
          sleep 1
        fi
        
        # 2. 使用 nohup 在后台重新启动服务，并重定向日志
        echo "🚀 正在后台启动新服务..."
        nohup node dist/server.js > nohup_backend.log 2>&1 < /dev/null &
        
        # 3. 验证是否启动成功 (检测端口或进程)
        sleep 2
        NEW_PID=$(pgrep -f "dist/server.js" | head -n 1)
        if [ -n "$NEW_PID" ]; then
          echo "✅ 新进程已在后台成功启动 (PID: $NEW_PID)！"
        else
          echo "❌ 后台启动失败，请检查 backend/nohup_backend.log 中的日志。"
        fi
      fi
    fi
  fi
EOF

echo "🎉 发布全部完成！"
echo "-------------------------------------------------------------------------"
