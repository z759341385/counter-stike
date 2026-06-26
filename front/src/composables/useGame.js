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
export const adminPassword = ref('')
export const allRules = ref([])
export const gameMode = ref('ROULETTE')
export const imposterCount = ref(1)

export const isImposter = ref(false)
export const imposterResult = ref(null)
export const imposterVotesTotal = ref(0)
export const hasImposterVoted = ref(false)
export const selectedImposterTargets = ref([])
export const imposterVotes = ref([])

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

export function createRoom(name, mode = 'ROULETTE', count = 1) {
  connectSocket();
  socket.emit('create_room', { playerName: name, gameMode: mode, imposterCount: count }, (res) => {
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
  socket.emit('take_seat', { targetIdx });
}

export function leaveSeat() {
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

export function destroyRoom() {
  socket.emit('destroy_room', { roomId: roomId.value });
}

export function leaveRoom() {
  socket.emit('leave_room', { roomId: roomId.value }, (res) => {
    if (res && res.success) {
      localStorage.removeItem('cs_rule_token');
      currentView.value = 'home';
      roomId.value = '';
      players.value = new Array(10).fill(null);
      spectators.value = [];
    }
  });
}

// ── 内鬼模式方法 ──
export function assignImposters() {
  socket.emit('assign_imposters', { roomId: roomId.value }, (res) => {
    if (!res.success) {
      errorMsg.value = res.error;
      setTimeout(() => { errorMsg.value = ''; }, 3000);
    }
  });
}

export function startImposterVote() {
  socket.emit('start_imposter_vote', { roomId: roomId.value });
}

export function submitImposterVote(targetTokens) {
  socket.emit('submit_imposter_vote', { roomId: roomId.value, targetTokens }, (res) => {
    if (!res.success) {
      errorMsg.value = res.error;
      setTimeout(() => { errorMsg.value = ''; }, 3000);
    }
  });
}

export function endImposterVote() {
  socket.emit('end_imposter_vote', { roomId: roomId.value });
}

// ── 管理员方法 ──

export function adminLogin(password) {
  return new Promise((resolve) => {
    connectSocket();
    socket.emit('admin_login', password, (res) => {
      if (res.success) {
        adminPassword.value = password;
        currentView.value = 'admin';
        resolve(true);
      } else {
        resolve(false);
      }
    });
  });
}

export function fetchAllRules() {
  socket.emit('admin_get_rules', adminPassword.value, (res) => {
    if (res.success) allRules.value = res.rules;
  });
}

export function saveRule(rule) {
  const event = rule.id ? 'admin_update_rule' : 'admin_add_rule';
  socket.emit(event, { password: adminPassword.value, rule }, (res) => {
    if (res.success) fetchAllRules();
  });
}

export function removeRule(id) {
  socket.emit('admin_delete_rule', { password: adminPassword.value, id }, (res) => {
    if (res.success) fetchAllRules();
  });
}

export function importRules(rules, mode = 'append') {
  return new Promise((resolve) => {
    socket.emit('admin_import_rules', { password: adminPassword.value, rules, mode }, (res) => {
      if (res.success) {
        fetchAllRules();
        resolve(true);
      } else {
        resolve(false);
      }
    });
  });
}

export function clearRules() {
  return new Promise((resolve) => {
    socket.emit('admin_clear_rules', adminPassword.value, (res) => {
      if (res.success) {
        fetchAllRules();
        resolve(true);
      } else {
        resolve(false);
      }
    });
  });
}

// ── Socket 事件 ──

function updateRoomState(room) {
  if (!room) return;
  if (Array.isArray(room.players)) {
    players.value = [...room.players];
  }
  if (Array.isArray(room.spectators)) {
    spectators.value = [...room.spectators];
  }
  if (Array.isArray(room.voteOptions)) {
    voteOptions.value = [...room.voteOptions];
  }
  roomStatus.value = room.status || 'WAITING';
  gameMode.value = room.gameMode || 'ROULETTE';
  imposterCount.value = room.imposterCount || 1;

  // 恢复已投票状态
  const myToken = localStorage.getItem('cs_rule_token');
  if (Array.isArray(room.votes)) {
    hasVoted.value = room.votes.includes(myToken);
  }
  
  if (Array.isArray(room.imposterVotes)) {
    imposterVotes.value = room.imposterVotes;
    hasImposterVoted.value = room.imposterVotes.some(v => v.voter === myToken);
  }

  // 恢复轮盘数据
  if (room.lastSpinResult) {
    spinData.value = room.lastSpinResult;
    winningRule.value = room.lastSpinResult.winner;
    // 如果已经处于结果阶段，直接显示赢家面板
    if (room.status === 'RESULT') {
      showWinner.value = true;
    }
  }

  // 重置内鬼状态
  if (room.status === 'WAITING' || room.status === 'VOTING') {
    isImposter.value = false;
    imposterResult.value = null;
    imposterVotesTotal.value = 0;
    hasImposterVoted.value = false;
    selectedImposterTargets.value = [];
  }
}

socket.on('room_update', (room) => {
  updateRoomState(room);
});

socket.on('reconnect_success', ({ roomId: rid, room }) => {
  roomId.value = rid;
  updateRoomState(room);
  currentView.value = 'lobby';
});

socket.on('vote_started', (options) => {
  voteOptions.value = options;
  hasVoted.value = false;
  selectedOption.value = null;
  totalVotes.value = 0;
  showWinner.value = false;
  roomStatus.value = 'VOTING';
});

socket.on('vote_progress', (data) => {
  totalVotes.value = data.totalVotes;
});

socket.on('spin_wheel', (data) => {
  spinData.value = data;
  winningRule.value = data.winner;
  showWinner.value = false;
  roomStatus.value = 'SPINNING';
});

socket.on('room_reset', () => {
  roomStatus.value = 'WAITING';
  currentView.value = 'lobby';
});

socket.on('room_destroyed', () => {
  localStorage.removeItem('cs_rule_token');
  currentView.value = 'home';
  roomId.value = '';
});

socket.on('error_msg', (msg) => {
  errorMsg.value = msg;
  setTimeout(() => { errorMsg.value = ''; }, 5000);
});

socket.on('you_are_imposter', () => {
  isImposter.value = true;
});

socket.on('imposter_vote_progress', (data) => {
  imposterVotesTotal.value = data.totalVotes;
});

socket.on('imposter_result', (data) => {
  imposterResult.value = data;
});
