<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  roomId, players, spectators, isHost, roomStatus,
  startVote, takeSeat, leaveSeat, isMe, destroyRoom, leaveRoom,
  voteOptions, selectedOption, hasVoted, totalVotes,
  submitVote, stopVoteAndSpin, spinData, showWinner, resetRoom, adminLogin,
  isImposter, imposterResult, imposterVotesTotal,
  assignImposters, startImposterVote, submitImposterVote, endImposterVote,
  gameMode, imposterCount, hasImposterVoted, selectedImposterTargets,
  imposterVotes
} from '../composables/useGame'
import { socket } from '../composables/socket'

const { t } = useI18n()

const avatarIcons = ['swords', 'shield', 'target', 'skull', 'military_tech', 'radar', 'security', 'token', 'potted_plant', 'rocket']
const cardIcons = ['volume_off', 'dark_mode', 'colorize', 'bolt', 'shield', 'casino']
const cardLetters = ['A', 'B', 'C', 'D', 'E', 'F']
const sectorColors = ['rgba(164,230,255,0.3)', 'rgba(164,230,255,0.12)', 'rgba(164,230,255,0.22)', 'rgba(164,230,255,0.16)']

const isVoting = () => roomStatus.value === 'VOTING'
const isSpinning = () => roomStatus.value === 'SPINNING' || roomStatus.value === 'RESULT'
const isImposterVoting = () => roomStatus.value === 'IMPOSTER_VOTING'
const isImposterResult = () => roomStatus.value === 'IMPOSTER_RESULT'
const isImposterAssigned = () => roomStatus.value === 'IMPOSTER_ASSIGNED'
const isCenterMode = () => isVoting() || isSpinning() || isImposterVoting() || isImposterResult() || isImposterAssigned()

const isMeSeated = computed(() => players.value && players.value.some(p => p !== null && isMe(p)))

const wheelRotation = ref(0)
const wheelAnimating = ref(false)
const showAdminModal = ref(false)
const adminPwInput = ref('')

// 生成SVG扇区路径
function sectorPath(startAngle, endAngle, r = 48, cx = 50, cy = 50) {
  const s = (startAngle - 90) * Math.PI / 180
  const e = (endAngle - 90) * Math.PI / 180
  const x1 = cx + r * Math.cos(s), y1 = cy + r * Math.sin(s)
  const x2 = cx + r * Math.cos(e), y2 = cy + r * Math.sin(e)
  const large = (endAngle - startAngle) > 180 ? 1 : 0
  return `M ${cx} ${cy} L ${x1} ${y1} A ${r} ${r} 0 ${large} 1 ${x2} ${y2} Z`
}

// 扇区标签位置
const sectorLabels = computed(() => {
  if (!spinData.value) return []
  return spinData.value.sectors
    .filter(s => (s.endAngle - s.startAngle) > 0) // 仅显示角度大于 0 的扇区标签
    .map((s, i) => {
      const mid = (s.startAngle + s.endAngle) / 2
      const rad = (mid - 90) * Math.PI / 180
      return { name: s.name, x: 50 + 34 * Math.cos(rad), y: 50 + 34 * Math.sin(rad), rot: mid, i }
    })
})

// 监听进入 SPINNING 状态时启动动画
watch(() => roomStatus.value, async (val) => {
  if (val === 'SPINNING' && spinData.value) {
    wheelRotation.value = 0
    wheelAnimating.value = false
    await nextTick()
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        wheelAnimating.value = true
        wheelRotation.value = 360 * 5 + (360 - spinData.value.targetAngle)
      })
    })
    setTimeout(() => { showWinner.value = true }, (spinData.value.duration || 5000) + 500)
  }
})

function handleNextRound() { startVote() }
function getIcon(i) { return avatarIcons[i % avatarIcons.length] }
function handleSeatClick(i) { if (!players.value || isCenterMode()) return; if (!players.value[i]) takeSeat(i) }
function handleVote(id) { if (!hasVoted.value) submitVote(id) }
function getTotalPlayers() {
  return Math.max(((players.value || []).filter(p => p !== null).length) + ((spectators.value || []).length), 1)
}
function handleImposterVote(targetToken) {
  if (hasImposterVoted.value) return;
  const idx = selectedImposterTargets.value.indexOf(targetToken);
  if (idx !== -1) {
    selectedImposterTargets.value.splice(idx, 1);
  } else {
    if (selectedImposterTargets.value.length < imposterCount.value) {
      selectedImposterTargets.value.push(targetToken);
    }
  }
}

function submitImposterVotes() {
  submitImposterVote(selectedImposterTargets.value);
}

function hasPlayerImposterVoted(token) {
  return imposterVotes.value.some(v => v.voter === token)
}

async function handleAdminEntry() {
  showAdminModal.value = true
}

async function handleAdminLogin() {
  const ok = await adminLogin(adminPwInput.value)
  if (ok) {
    showAdminModal.value = false
    adminPwInput.value = ''
  } else {
    alert('密码错误')
  }
}
</script>

<template>
  <div class="min-h-screen flex flex-col items-center relative overflow-hidden dot-pattern">
    <!-- Header -->
    <header
      class="flex justify-between items-center w-full px-8 md:px-12 py-6 fixed top-0 z-50 bg-black/60 backdrop-blur-xl border-b border-white/5">
      <div @click="handleAdminEntry"
        class="font-display text-xl md:text-2xl tracking-tighter text-primary font-bold uppercase cursor-pointer select-none active:opacity-50">
        CS Rule Engine
      </div>
      <div class="flex items-center gap-4">
        <div
          class="px-3 py-1 bg-primary/10 border border-primary/30 chamfer-clip-sm font-mono text-[10px] text-primary uppercase">
          {{ $t('common.room') }}: {{ roomId }}
        </div>
        <button v-if="isHost" @click="destroyRoom"
          class="px-3 py-1 bg-red-500/10 border border-red-500/30 chamfer-clip-sm font-mono text-[10px] text-red-500 uppercase hover:bg-red-500 hover:text-white transition-all">
          {{ $t('common.destroyRoom') }}
        </button>
        <button v-else @click="leaveRoom"
          class="px-3 py-1 bg-white/10 border border-white/30 chamfer-clip-sm font-mono text-[10px] text-white uppercase hover:bg-white/20 transition-all">
          离开房间
        </button>
      </div>
    </header>

    <main class="flex-grow flex flex-col items-center justify-center w-full pt-24 pb-24 px-6 md:px-12">
      <!-- Title -->
      <div class="text-center mb-10 relative z-10">
        <p class="font-mono text-[10px] text-text-secondary tracking-[0.6em] mb-2 uppercase opacity-60">
          {{ isSpinning() ? 'DETERMINING SYSTEM PARAMETERS' : isVoting() ? $t('voting.title') : isImposterVoting() ? 'IMPOSTER HUNT' :
            $t('lobby.tacticalDeployment') }}
        </p>
        <h1 class="font-display font-black text-primary uppercase transition-all duration-500"
          :class="isCenterMode() ? 'text-3xl md:text-5xl tracking-tight' : 'text-5xl md:text-7xl tracking-[0.1em]'">
          {{ isSpinning() ? $t('roulette.title') : isVoting() ? $t('voting.title') : isImposterVoting() ? '抓内鬼投票' : $t('lobby.title') }}
        </h1>
        <div v-if="(isImposterAssigned() || isImposterVoting()) && isMeSeated" class="mt-4 px-6 py-2 border-2 rounded-full inline-block"
          :class="isImposter ? 'bg-red-600/30 border-red-500 animate-pulse' : 'bg-green-600/30 border-green-500'">
          <span v-if="isImposter" class="font-bold text-red-100 uppercase tracking-widest text-lg shadow-black drop-shadow-md">⚠️ 你是内鬼 ⚠️</span>
          <span v-else class="font-bold text-green-100 uppercase tracking-widest text-lg shadow-black drop-shadow-md">🛡️ 你是平民 🛡️</span>
        </div>
      </div>

      <!-- Core Layout -->
      <div class="w-full relative z-10 transition-all duration-600"
        :class="isCenterMode() ? 'max-w-[1600px] grid grid-cols-12 gap-6 items-start' : 'max-w-6xl mx-auto flex flex-col md:flex-row gap-8 items-start'">

        <!-- Team Alpha -->
        <section :class="isCenterMode() ? 'col-span-2' : 'flex-1 w-full'">
          <div class="flex items-center gap-3 mb-5 px-1">
            <span class="w-1.5 h-6 bg-primary shadow-[0_0_10px_#a4e6ff]"></span>
            <h2 class="font-display font-bold tracking-widest text-primary uppercase"
              :class="isCenterMode() ? 'text-base' : 'text-2xl'">{{ $t('lobby.teamAlpha') }}</h2>
          </div>
          <div class="grid grid-cols-1 gap-2">
            <div v-for="idx in [0, 1, 2, 3, 4]" :key="idx" @click="handleSeatClick(idx)"
              class="relative border transition-all duration-300 group"
              :class="[isCenterMode() ? 'p-3' : 'p-4 chamfer-clip',
              (players && players[idx]) ? (isMe(players[idx]) ? 'border-primary bg-primary/10' : 'border-white/10 bg-white/5') : 'border-white/5 bg-transparent border-dashed cursor-pointer hover:border-primary/40 hover:bg-primary/5']">
              <div v-if="players && players[idx]" class="flex items-center gap-3">
                <span class="material-symbols-outlined text-primary" :class="isCenterMode() ? 'text-lg' : 'text-2xl'">{{
                  getIcon(idx) }}</span>
                <div class="flex-grow min-w-0">
                  <p class="font-bold uppercase tracking-tight truncate"
                    :class="[isMe(players[idx]) ? 'text-primary' : 'text-text-primary', isCenterMode() ? 'text-xs' : 'text-sm']">
                    {{ players[idx].playerName }}<span v-if="isMe(players[idx])" class="text-[9px] opacity-40 ml-1">({{
                      $t('common.you') }})</span>
                  </p>
                </div>
                <div v-if="isVoting() && hasVoted && isMe(players[idx])"
                  class="bg-primary/20 text-primary text-[8px] px-1.5 py-0.5 border border-primary/30 font-bold shrink-0">
                  READY</div>
                <div v-if="isImposterVoting() && hasPlayerImposterVoted(players[idx].token)"
                  class="bg-red-500/20 text-red-500 text-[8px] px-1.5 py-0.5 border border-red-500/40 font-bold shrink-0">
                  已投票</div>
                <button v-if="isImposterVoting() && !isMe(players[idx]) && (players.findIndex(p => p && p.socketId === socket.id) < 5) && !hasImposterVoted" @click.stop="handleImposterVote(players[idx].token)"
                  class="px-2 py-1 border font-mono text-[9px] uppercase chamfer-clip-sm transition-all shrink-0"
                  :class="selectedImposterTargets.includes(players[idx].token) ? 'bg-red-500 text-white border-red-500' : 'bg-red-500/20 text-red-500 border-red-500/40 hover:bg-red-500 hover:text-white'">
                  {{ selectedImposterTargets.includes(players[idx].token) ? '已选' : '投票' }}
                </button>
                <button v-if="!isCenterMode() && isMe(players[idx])" @click.stop="leaveSeat"
                  class="px-2 py-1 bg-red-500/20 border border-red-500/40 text-red-500 font-mono text-[9px] uppercase chamfer-clip-sm hover:bg-red-500 hover:text-white transition-all shrink-0">{{
                    $t('lobby.leave') }}</button>
              </div>
              <div v-else class="flex items-center gap-3 opacity-20 group-hover:opacity-60 transition-opacity">
                <span class="material-symbols-outlined"
                  :class="isCenterMode() ? 'text-lg' : 'text-2xl'">add_circle</span>
                <span class="font-mono uppercase tracking-widest"
                  :class="isCenterMode() ? 'text-[9px]' : 'text-[10px]'">{{
                    $t('lobby.joinAlpha') }}</span>
              </div>
            </div>
          </div>
        </section>

        <!-- === CENTER: VOTING === -->
        <section v-if="isVoting()" class="col-span-8 flex flex-col items-center">
          <div class="vs-divider py-6"><span
              class="font-display text-3xl italic font-black text-white/10 tracking-tighter">VS</span></div>
          <div class="grid grid-cols-1 md:grid-cols-3 gap-5 w-full px-2 mt-6">
            <button v-for="(option, index) in voteOptions" :key="option.id"
              class="group relative text-left transition-all duration-300 hover:translate-y-[-4px] active:scale-95"
              :class="{ 'opacity-30 pointer-events-none': hasVoted && selectedOption !== option.id }"
              :disabled="hasVoted" @click="handleVote(option.id)">
              <div class="vote-card p-6 flex flex-col items-center relative"
                :class="selectedOption === option.id ? 'border-primary shadow-[0_0_30px_rgba(164,230,255,0.1)]' : 'border-white/10 group-hover:border-primary/50'">
                <div v-if="selectedOption === option.id"
                  class="absolute top-3 right-3 w-6 h-6 bg-primary flex items-center justify-center text-on-primary text-[10px] font-bold">
                  ✓</div>
                <div
                  class="absolute top-4 left-4 w-6 h-6 rounded-full flex items-center justify-center text-[10px] font-bold border"
                  :class="selectedOption === option.id ? 'bg-primary/20 text-primary border-primary/30' : 'bg-white/5 text-white/40 border-white/10'">
                  {{ cardLetters[index] }}</div>
                <span class="material-symbols-outlined text-4xl mb-4 mt-4"
                  :class="selectedOption === option.id ? 'text-primary' : 'text-primary/60'"></span>
                <h3 class="text-sm font-bold text-text-primary mb-2 uppercase">{{ option.name }}</h3>
                <p class="text-[10px] text-text-secondary text-center leading-tight mb-5">{{ option.description }}</p>
                <div class="w-full h-[2px] bg-white/5">
                  <div class="h-full bg-primary transition-all duration-500"
                    :style="{ width: selectedOption === option.id ? '100%' : '0%' }"></div>
                </div>
              </div>
            </button>
          </div>
          <div class="mt-10 w-full max-w-2xl flex flex-col md:flex-row items-center gap-6">
            <div class="flex-1 w-full">
              <div class="flex justify-between items-end mb-2">
                <span class="font-mono text-[10px] text-primary uppercase tracking-[0.1em]">{{ $t('voting.progress')
                  }}</span>
                <span class="font-mono text-[11px] text-text-primary">{{ totalVotes }} / {{ getTotalPlayers() }}</span>
              </div>
              <div class="h-1.5 w-full bg-white/5 overflow-hidden">
                <div
                  class="h-full bg-gradient-to-r from-primary to-primary/60 transition-all duration-500 shadow-[0_0_8px_rgba(164,230,255,0.4)]"
                  :style="{ width: `${(totalVotes / getTotalPlayers()) * 100}%` }"></div>
              </div>
            </div>
            <button v-if="isHost" @click="stopVoteAndSpin"
              class="shrink-0 bg-primary text-on-primary font-display font-bold py-3 px-10 flex items-center gap-3 chamfer-clip hover:brightness-110 active:scale-95 transition-all shadow-[0_0_20px_rgba(164,230,255,0.3)] uppercase text-sm tracking-widest">
              <span class="material-symbols-outlined text-lg">casino</span>{{ $t('voting.endVote') }}
            </button>
            <div v-else-if="hasVoted"
              class="shrink-0 px-6 py-3 border border-outline/30 font-mono text-[10px] text-outline uppercase tracking-[0.1em]">
              ✅ {{ $t('voting.votedWaiting') }}</div>
          </div>
        </section>

        <!-- === CENTER: SPINNING WHEEL === -->
        <section v-if="isSpinning()" class="col-span-8 flex flex-col items-center">
          <!-- Wheel -->
          <div v-if="!showWinner"
            class="relative w-[320px] h-[320px] md:w-[420px] md:h-[420px] flex items-center justify-center my-4">
            <!-- Pointer -->
            <div class="absolute -top-3 left-1/2 -translate-x-1/2 z-20">
              <div
                class="w-0 h-0 border-l-[12px] border-l-transparent border-r-[12px] border-r-transparent border-t-[18px] border-t-red-400 drop-shadow-[0_0_8px_rgba(255,100,100,0.5)]">
              </div>
            </div>
            <!-- Wheel body -->
            <div
              class="w-full h-full rounded-full border border-primary/20 relative overflow-hidden shadow-[0_0_20px_rgba(164,230,255,0.2)]"
              :class="{ 'wheel-animating': wheelAnimating }" :style="{ transform: `rotate(${wheelRotation}deg)` }">
              <svg class="w-full h-full" viewBox="0 0 100 100">
                <circle cx="50" cy="50" r="48" fill="none" stroke="rgba(164,230,255,0.2)" stroke-width="0.5"
                  stroke-dasharray="1 3" />
                <path v-for="(sector, i) in (spinData?.sectors || []).filter(s => s.endAngle - s.startAngle > 0)"
                  :key="i" :d="sectorPath(sector.startAngle, sector.endAngle)"
                  :fill="'rgba(164,230,255,' + [0.3, 0.12, 0.22, 0.16][i % 4] + ')'" stroke="rgba(164,230,255,0.4)"
                  stroke-width="0.5" />
              </svg>
              <!-- Labels -->
              <div v-for="lbl in sectorLabels" :key="lbl.i"
                class="absolute pointer-events-none font-display text-[11px] md:text-[13px] text-primary font-bold whitespace-nowrap drop-shadow-[0_0_5px_rgba(164,230,255,0.5)]"
                :style="{ left: lbl.x + '%', top: lbl.y + '%', transform: `translate(-50%,-50%) rotate(${lbl.rot}deg)` }">
                {{ lbl.name }}
              </div>
              <!-- Center -->
              <div
                class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-16 h-16 md:w-20 md:h-20 rounded-full bg-black border-2 border-primary/40 flex items-center justify-center z-10">
                <div class="absolute inset-0 bg-primary/5 rounded-full animate-pulse"></div>
                <span class="material-symbols-outlined text-red-400 text-3xl"
                  style="font-variation-settings:'FILL' 1;">target</span>
              </div>
            </div>
          </div>
          <!-- Legend -->
          <div v-if="!showWinner" class="mt-8 flex flex-wrap items-center justify-center gap-6">
            <div v-for="(sector, i) in (spinData?.sectors || [])" :key="sector.optionId" class="flex items-center gap-2"
              :class="{ 'opacity-50': sector.votes === 0 }">
              <div class="w-3 h-3 rounded-full"
                :style="{ background: ['#a4e6ff', '#ffdb9d', '#ff6b6b', '#6bffb8'][i % 4] }">
              </div>
              <span class="font-mono text-[11px] text-text-primary uppercase">{{ sector.name }} ({{ sector.votes
                }})</span>
            </div>
          </div>
          <!-- Winner overlay -->
          <Transition name="winner">
            <div v-if="showWinner"
              class="mt-8 w-full max-w-lg border border-primary/30 bg-black/80 p-8 text-center backdrop-blur-sm">
              <div class="text-4xl mb-3">🏆</div>
              <p class="font-mono text-[10px] text-primary uppercase tracking-[0.2em] mb-2">{{
                $t('roulette.winningRule') }}</p>
              <h3 class="font-display text-2xl font-black text-primary mb-3">{{ spinData?.winner?.name }}</h3>
              <p class="text-sm text-text-secondary mb-6">{{ spinData?.winner?.description }}</p>
              <button v-if="isHost" @click="handleNextRound"
                class="w-full bg-primary text-on-primary font-display font-bold py-3 chamfer-clip uppercase text-sm tracking-widest hover:brightness-110 active:scale-95 transition-all">
                🔄 {{ $t('roulette.nextRound') }}
              </button>
              <div v-else class="py-3 border border-white/10 font-mono text-[10px] text-outline uppercase">{{
                $t('roulette.waitingHost') }}</div>
            </div>
          </Transition>
        </section>

        <!-- === CENTER: IMPOSTER ASSIGNED === -->
        <section v-if="isImposterAssigned()" class="col-span-8 flex flex-col items-center justify-center">
          <div class="w-full max-w-lg border border-red-500/30 bg-black/80 p-8 text-center backdrop-blur-sm shadow-[0_0_20px_rgba(255,0,0,0.1)]">
            <h2 class="font-display text-3xl font-black text-red-500 mb-4 uppercase">身份已分配</h2>
            <p class="text-text-secondary text-lg mb-6">所有玩家请确认自己的身份。<br>上方会显示你的具体身份提示。</p>
            <button v-if="isHost" @click="startImposterVote"
              class="w-full bg-red-600 text-white font-display font-bold py-4 chamfer-clip uppercase text-lg tracking-widest hover:brightness-110 active:scale-95 transition-all shadow-[0_0_20px_rgba(255,0,0,0.3)]">
              🔥 开始投票
            </button>
            <div v-else class="py-3 border border-white/10 font-mono text-[10px] text-outline uppercase">等待房主开始投票...</div>
          </div>
        </section>

        <!-- === CENTER: IMPOSTER VOTING === -->
        <section v-if="isImposterVoting()" class="col-span-8 flex flex-col items-center">
          <div class="vs-divider py-6"><span class="font-display text-3xl italic font-black text-white/10 tracking-tighter">VS</span></div>
          <p class="text-text-primary text-lg mb-6">请在左右两侧选择你要投出的队友 (共 {{ imposterCount }} 票)</p>
          
          <div v-if="!hasImposterVoted" class="w-full max-w-md flex flex-col items-center mb-6">
            <p class="text-text-secondary text-sm mb-4">已选目标: {{ selectedImposterTargets.length }} / {{ imposterCount }}</p>
            <button @click="submitImposterVotes" :disabled="selectedImposterTargets.length === 0"
              class="w-full bg-red-600 text-white font-display font-bold py-3 px-10 flex items-center justify-center gap-3 chamfer-clip hover:brightness-110 active:scale-95 transition-all shadow-[0_0_20px_rgba(255,0,0,0.3)] uppercase text-sm tracking-widest disabled:opacity-50 disabled:pointer-events-none">
              <span class="material-symbols-outlined text-lg">how_to_vote</span>确认提交选票
            </button>
          </div>
          <div v-else class="w-full max-w-md text-center py-4 border border-red-500/30 bg-red-500/10 mb-6 font-mono text-red-400">
            你已完成投票，等待其他玩家...
          </div>

          <div class="mt-4 w-full max-w-2xl flex flex-col items-center gap-6">
            <div class="flex-1 w-full">
              <div class="flex justify-between items-end mb-2">
                <span class="font-mono text-[10px] text-primary uppercase tracking-[0.1em]">总投票进度</span>
                <span class="font-mono text-[11px] text-text-primary">{{ imposterVotesTotal }} / {{ players.filter(p => p !== null).length }}</span>
              </div>
              <div class="h-1.5 w-full bg-white/5 overflow-hidden">
                <div class="h-full bg-gradient-to-r from-red-500 to-red-400 transition-all duration-500 shadow-[0_0_8px_rgba(255,0,0,0.4)]"
                  :style="{ width: `${(imposterVotesTotal / (players.filter(p => p !== null).length || 1)) * 100}%` }"></div>
              </div>
            </div>
            
            <div class="w-full mt-2 text-left">
              <span class="font-mono text-[10px] text-outline uppercase">还未投票的玩家:</span>
              <div class="flex flex-wrap gap-2 mt-2">
                <span v-for="p in players.filter(p => p !== null && !hasPlayerImposterVoted(p.token))" :key="p.token"
                  class="text-[10px] px-2 py-1 bg-white/5 border border-white/10 text-white/60">
                  {{ p.playerName }}
                </span>
                <span v-if="players.filter(p => p !== null && !hasPlayerImposterVoted(p.token)).length === 0" class="text-[10px] text-green-400">
                  所有人已完成投票
                </span>
              </div>
            </div>

            <button v-if="isHost" @click="endImposterVote"
              class="w-full bg-red-600 text-white font-display font-bold py-3 px-10 flex items-center justify-center gap-3 chamfer-clip hover:brightness-110 active:scale-95 transition-all shadow-[0_0_20px_rgba(255,0,0,0.3)] uppercase text-sm tracking-widest mt-4">
              <span class="material-symbols-outlined text-lg">gavel</span>结束投票并揭晓
            </button>
          </div>
        </section>

        <!-- === CENTER: IMPOSTER RESULT === -->
        <section v-if="isImposterResult()" class="col-span-8 flex flex-col items-center">
          <div class="w-full max-w-lg border border-red-500/50 bg-black/80 p-8 text-center backdrop-blur-sm shadow-[0_0_40px_rgba(255,0,0,0.2)]">
            <h2 class="text-4xl mb-4 font-black text-red-500">揭晓时刻</h2>
            <div v-if="imposterResult">
              <p class="text-lg text-white mb-2">被投出的玩家:</p>
              <div class="flex justify-center gap-4 mb-6">
                <div v-for="token in imposterResult.votedOut" :key="token" class="px-4 py-2 bg-white/10 rounded border border-white/20 text-white font-bold">
                  {{ players.find(p => p && p.token === token)?.playerName || '没人' }}
                </div>
              </div>
              <p class="text-lg text-white mb-2">真实的内鬼:</p>
              <div class="flex justify-center gap-4 mb-8">
                <div v-for="token in imposterResult.realImposters" :key="token" class="px-4 py-2 bg-red-500/20 rounded border border-red-500 text-red-500 font-bold">
                  {{ players.find(p => p && p.token === token)?.playerName || '未知' }}
                </div>
              </div>
            </div>
            <button v-if="isHost" @click="resetRoom"
              class="w-full bg-primary text-on-primary font-display font-bold py-3 chamfer-clip uppercase text-sm tracking-widest hover:brightness-110 active:scale-95 transition-all">
              🔄 返回大厅
            </button>
            <div v-else class="py-3 border border-white/10 font-mono text-[10px] text-outline uppercase">等待房主返回大厅...</div>
          </div>
        </section>

        <!-- VS (WAITING only) -->
        <div v-if="!isCenterMode()"
          class="hidden md:flex flex-col items-center justify-center gap-4 opacity-20 self-center">
          <span class="font-display text-4xl font-black italic tracking-tighter">VS</span>
        </div>

        <!-- Team Bravo -->
        <section :class="isCenterMode() ? 'col-span-2 text-right' : 'flex-1 w-full text-right'">
          <div class="flex flex-row-reverse items-center gap-3 mb-5 px-1">
            <span class="w-1.5 h-6 bg-secondary shadow-[0_0_10px_#ffdb9d]"></span>
            <h2 class="font-display font-bold tracking-widest text-secondary uppercase"
              :class="isCenterMode() ? 'text-base' : 'text-2xl'">{{ $t('lobby.teamBravo') }}</h2>
          </div>
          <div class="grid grid-cols-1 gap-2">
            <div v-for="idx in [5, 6, 7, 8, 9]" :key="idx" @click="handleSeatClick(idx)"
              class="relative border transition-all duration-300 group"
              :class="[isCenterMode() ? 'p-3' : 'p-4 chamfer-clip',
              (players && players[idx]) ? (isMe(players[idx]) ? 'border-secondary bg-secondary/10' : 'border-white/10 bg-white/5') : 'border-white/5 bg-transparent border-dashed cursor-pointer hover:border-secondary/40 hover:bg-secondary/5']">
              <div v-if="players && players[idx]" class="flex items-center gap-3 flex-row-reverse">
                <span class="material-symbols-outlined text-secondary"
                  :class="isCenterMode() ? 'text-lg' : 'text-2xl'">{{
                    getIcon(idx) }}</span>
                <div class="flex-grow min-w-0">
                  <p class="font-bold uppercase tracking-tight truncate"
                    :class="[isMe(players[idx]) ? 'text-secondary' : 'text-text-primary', isCenterMode() ? 'text-xs' : 'text-sm']">
                    <span v-if="isMe(players[idx])" class="text-[9px] opacity-40 mr-1">({{ $t('common.you') }})</span>{{
                      players[idx].playerName }}
                  </p>
                </div>
                <div v-if="isVoting() && hasVoted && isMe(players[idx])"
                  class="bg-secondary/20 text-secondary text-[8px] px-1.5 py-0.5 border border-secondary/30 font-bold shrink-0">
                  READY</div>
                <div v-if="isImposterVoting() && hasPlayerImposterVoted(players[idx].token)"
                  class="bg-red-500/20 text-red-500 text-[8px] px-1.5 py-0.5 border border-red-500/40 font-bold shrink-0">
                  已投票</div>
                <button v-if="isImposterVoting() && !isMe(players[idx]) && (players.findIndex(p => p && p.socketId === socket.id) >= 5) && !hasImposterVoted" @click.stop="handleImposterVote(players[idx].token)"
                  class="px-2 py-1 border font-mono text-[9px] uppercase chamfer-clip-sm transition-all shrink-0"
                  :class="selectedImposterTargets.includes(players[idx].token) ? 'bg-red-500 text-white border-red-500' : 'bg-red-500/20 text-red-500 border-red-500/40 hover:bg-red-500 hover:text-white'">
                  {{ selectedImposterTargets.includes(players[idx].token) ? '已选' : '投票' }}
                </button>
                <button v-if="!isCenterMode() && isMe(players[idx])" @click.stop="leaveSeat"
                  class="px-2 py-1 bg-red-500/20 border border-red-500/40 text-red-500 font-mono text-[9px] uppercase chamfer-clip-sm hover:bg-red-500 hover:text-white transition-all shrink-0">{{
                    $t('lobby.leave') }}</button>
              </div>
              <div v-else
                class="flex items-center gap-3 opacity-20 group-hover:opacity-60 transition-opacity flex-row-reverse">
                <span class="material-symbols-outlined"
                  :class="isCenterMode() ? 'text-lg' : 'text-2xl'">add_circle</span>
                <span class="font-mono uppercase tracking-widest"
                  :class="isCenterMode() ? 'text-[9px]' : 'text-[10px]'">{{
                    $t('lobby.joinBravo') }}</span>
              </div>
            </div>
          </div>
        </section>
      </div>

      <!-- Spectator (WAITING) -->
      <div v-if="!isCenterMode()" class="w-full max-w-6xl mt-12 relative z-10">
        <div class="flex items-center gap-3 mb-6 px-4">
          <span class="material-symbols-outlined text-outline">visibility</span>
          <h2 class="font-mono text-sm font-bold tracking-[0.3em] text-outline uppercase">{{ $t('lobby.spectators') }}
            ({{ spectators.length }})</h2>
        </div>
        <div class="flex flex-wrap gap-4 p-6 bg-white/[0.02] border border-white/5 chamfer-clip">
          <div v-for="spec in spectators" :key="spec.token"
            class="px-4 py-2 bg-white/5 border border-white/10 chamfer-clip-sm flex items-center gap-2"
            :class="{ 'border-primary/50 bg-primary/5': isMe(spec) }">
            <span class="w-1.5 h-1.5 rounded-full"
              :class="spec.connected ? 'bg-green-500 animate-pulse' : 'bg-red-500'"></span>
            <span class="text-xs font-mono uppercase tracking-tight"
              :class="isMe(spec) ? 'text-primary' : 'text-text-secondary'">{{ spec.playerName }}</span>
            <span v-if="spec.isHost" class="material-symbols-outlined text-[14px] text-primary">crown</span>
          </div>
          <div v-if="spectators.length === 0"
            class="w-full text-center py-4 font-mono text-[10px] uppercase opacity-20">
            {{ $t('lobby.noPersonnel') }}</div>
        </div>
      </div>

      <!-- Start button (WAITING) -->
      <div v-if="!isCenterMode()" class="mt-16 pb-12 text-center relative z-10 w-full max-w-lg">
        <template v-if="isHost">
          <button v-if="gameMode === 'ROULETTE'"
            class="w-full btn-base bg-primary text-on-primary chamfer-clip py-5 text-xl uppercase font-black shadow-[0_0_40px_rgba(164,230,255,0.2)]"
            @click="startVote">
            🎲 {{ $t('lobby.startOperation') }}
          </button>
          <button v-if="gameMode === 'IMPOSTER'"
            class="w-full btn-base bg-red-600 text-white chamfer-clip py-5 text-xl uppercase font-black shadow-[0_0_40px_rgba(255,0,0,0.2)]"
            @click="assignImposters">
            😈 开始游戏
          </button>
        </template>
        <div v-else
          class="p-5 bg-white/5 border border-white/10 chamfer-clip font-mono text-xs text-primary uppercase tracking-[0.2em]">
          {{ $t('lobby.standby') }}</div>
      </div>
    </main>

    <!-- Footer -->
    <footer
      class="w-full px-8 md:px-12 py-4 flex justify-between items-center text-[9px] font-mono text-text-secondary border-t border-white/5 bg-black/80 backdrop-blur-sm fixed bottom-0 left-0 z-50 uppercase tracking-[0.15em]">
      <div class="flex gap-6"><span>System: <span class="text-primary">Nominal</span></span><span>Encryption: <span
            class="text-text-primary">AES-256</span></span></div>
      <div>© 2026 CS-RULE-ENGINE v1.0.4</div>
    </footer>

    <!-- Admin Auth Modal -->
    <div v-if="showAdminModal" class="modal-overlay" @click.self="showAdminModal = false">
      <div
        class="modal-content w-[90%] max-w-[450px] p-8 bg-surface-container border border-white/10 chamfer-clip animate-[slideUp_0.3s_ease-out]">
        <div class="flex items-center gap-3 mb-6">
          <span class="material-symbols-outlined text-primary">security</span>
          <h2 class="font-display text-2xl font-bold text-primary uppercase">Authorization</h2>
        </div>
        <div class="mb-6">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">Security Access
            Code</label>
          <input v-model="adminPwInput" type="password" class="input-field chamfer-clip-sm" placeholder="••••••••"
            @keyup.enter="handleAdminLogin" />
        </div>
        <button @click="handleAdminLogin"
          class="w-full btn-base bg-primary text-on-primary chamfer-clip py-4 uppercase font-bold tracking-widest hover:brightness-110">Access
          Terminal</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.vs-divider {
  position: relative;
}

.vs-divider::before,
.vs-divider::after {
  content: '';
  position: absolute;
  left: 50%;
  width: 1px;
  height: 60px;
  background: linear-gradient(to bottom, transparent, rgba(133, 147, 153, 0.2));
}

.vs-divider::before {
  top: -70px;
}

.vs-divider::after {
  bottom: -70px;
  background: linear-gradient(to top, transparent, rgba(133, 147, 153, 0.2));
}

.vote-card {
  background: linear-gradient(135deg, rgba(32, 43, 54, 0.6), rgba(21, 33, 43, 0.4));
  border: 1px solid;
  clip-path: polygon(5% 0, 100% 0, 100% 100%, 0 100%, 0 12%);
  transition: all 0.3s ease;
}

.wheel-animating {
  transition: transform 5s cubic-bezier(0.17, 0.67, 0.12, 0.99);
}

.winner-enter-active {
  transition: all 0.5s cubic-bezier(0.16, 1, 0.3, 1);
}

.winner-leave-active {
  transition: all 0.3s ease;
}

.winner-enter-from {
  opacity: 0;
  transform: translateY(20px) scale(0.95);
}

.winner-leave-to {
  opacity: 0;
}
</style>
