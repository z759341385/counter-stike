/**
 * useGame 组合式函数
 */
import { ref, computed } from 'vue'
import { socket } from './socket'

export const currentView = ref('home')
export const roomId = ref('')
export const players = ref(new Array(10).fill(null))
export const spectators = ref([])
export const roomStatus = ref('WAITING')
export const errorMsg = ref('')
export const spinData = ref(null)
export const showWinner = ref(false)
export const winningRule = ref(null)

export const isHost = computed(() => {
  const token = localStorage.getItem('cs_rule_token');
  const allPlayers = [...(players.value || []), ...(spectators.value || [])];
  return allPlayers.some(p => p && (p.token === token || p.socketId === socket.id) && p.isHost);
})

export function isMe(player) {
  if (!player) return false;
  const token = localStorage.getItem('cs_rule_token');
  return player.token === token || player.socketId === socket.id;
}

// ── 方法 ──

export function connectSocket() {
  if (!socket.connected) socket.connect();
}

export function initGame() {
  const token = localStorage.getItem('cs_rule_token');
  if (token) connectSocket();
}

export function createRoom(name) {
  connectSocket();
  socket.emit('create_room', { playerName: name }, (res) => {
    if (res.success) {
      roomId.value = res.roomId;
      localStorage.setItem('cs_rule_token', res.token);
      currentView.value = 'lobby';
    }
  });
}

export function joinRoom(id, name) {
  connectSocket();
  socket.emit('join_room', { roomId: id, playerName: name }, (res) => {
    if (res.success) {
      roomId.value = res.roomId;
      localStorage.setItem('cs_rule_token', res.token);
      currentView.value = 'lobby';
    } else {
      errorMsg.value = res.error || '加入失败';
      setTimeout(() => { errorMsg.value = ''; }, 3000);
    }
  });
}

export function takeSeat(targetIdx) {
  console.log('[Game] 发起上座请求, 目标位置:', targetIdx);
  socket.emit('take_seat', { targetIdx });
}

export function leaveSeat() {
  console.log('[Game] 发起退到观战区请求');
  socket.emit('leave_seat');
}

export function startVote() {
  socket.emit('start_vote', { roomId: roomId.value });
}

export const voteOptions = ref([])
export const selectedOption = ref(null)
export const hasVoted = ref(false)
export const totalVotes = ref(0)

export function submitVote(optionId) {
  selectedOption.value = optionId;
  socket.emit('submit_vote', { roomId: roomId.value, optionId }, (res) => {
    if (res.success) hasVoted.value = true;
  });
}

export function stopVoteAndSpin() {
  socket.emit('stop_vote_and_spin', { roomId: roomId.value });
}

export function resetRoom() {
  socket.emit('reset_room', { roomId: roomId.value });
}

// ── Socket 事件 ──

function updateRoomState(room) {
  if (!room) return;
  console.log('[Game] 收到房间更新:', room);
  if (Array.isArray(room.players)) {
    players.value = [...room.players]; // 强制触发响应式
  }
  if (Array.isArray(room.spectators)) {
    spectators.value = [...room.spectators];
  }
  roomStatus.value = room.status || 'WAITING';
}

socket.on('room_update', (room) => {
  updateRoomState(room);
});

socket.on('reconnect_success', ({ roomId: rid, room }) => {
  roomId.value = rid;
  updateRoomState(room);
  currentView.value = room.status === 'WAITING' ? 'lobby' : (room.status === 'VOTING' ? 'voting' : 'lobby');
});

socket.on('vote_started', (options) => {
  voteOptions.value = options;
  hasVoted.value = false;
  currentView.value = 'voting';
});

socket.on('vote_progress', (data) => {
  totalVotes.value = data.totalVotes;
});

socket.on('spin_wheel', (data) => {
  spinData.value = data;
  winningRule.value = data.winner;
  showWinner.value = false;
  currentView.value = 'roulette';
});

socket.on('room_reset', () => {
  currentView.value = 'lobby';
});

socket.on('error_msg', (msg) => {
  errorMsg.value = msg;
  setTimeout(() => { errorMsg.value = ''; }, 5000);
});
