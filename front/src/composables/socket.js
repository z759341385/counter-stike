/**
 * Socket.io 通信服务
 * 全局单例，连接后端 WebSocket
 */
import { io } from 'socket.io-client'

const BACKEND_URL = import.meta.env.VITE_BACKEND_URL || 'http://localhost:3000'

export const socket = io(BACKEND_URL, {
  autoConnect: false,
  reconnection: true,
  reconnectionAttempts: 10,
  reconnectionDelay: 1000,
  // 动态获取 Token
  auth: (cb) => {
    cb({ token: localStorage.getItem('cs_rule_token') })
  }
})

// 连接状态日志
socket.on('connect', () => {
  console.log('[Socket] ✅ 已连接:', socket.id)
})

socket.on('disconnect', (reason) => {
  console.log('[Socket] ❌ 断开:', reason)
  // 如果是服务器强制断开且没有重连尝试了，可以考虑清理
})

socket.on('connect_error', (err) => {
  console.error('[Socket] 连接错误:', err.message)
  // 如果后端报错 Token 无效，则清理
  if (err.message === 'xhr poll error' || err.message === 'websocket error') return; 
  // 以上是网络错误，如果是逻辑错误则清理
  // localStorage.removeItem('cs_rule_token')
})

