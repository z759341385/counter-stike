<script setup>
/**
 * HomeView - 首页 (国际化版)
 */
import { ref, onMounted, onUnmounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { createRoom, joinRoom, errorMsg, adminLogin } from '../composables/useGame'

const { t } = useI18n()

const showCreateModal = ref(false)
const showJoinModal = ref(false)
const nameInput = ref(localStorage.getItem('cs_player_name') || '')
const gameMode = ref('ROULETTE')
const imposterCount = ref(1)
const roomIdInput = ref('')
const joinNameInput = ref(localStorage.getItem('cs_player_name') || '')
const showAdminModal = ref(false)
const adminPwInput = ref('')

// ── 点阵背景动画 ──
const canvasRef = ref(null)
let animationId = null
const mouse = { x: -1000, y: -1000 }

function initDots(canvas) {
  const ctx = canvas.getContext('2d')
  let width, height, dots = []
  const spacing = 32

  const resize = () => {
    width = canvas.width = window.innerWidth
    height = canvas.height = window.innerHeight
    dots = []
    for (let x = spacing / 2; x < width; x += spacing) {
      for (let y = spacing / 2; y < height; y += spacing) {
        dots.push({ x, y, baseSize: 1 })
      }
    }
  }

  const animate = () => {
    ctx.clearRect(0, 0, width, height)
    dots.forEach(dot => {
      const dx = mouse.x - dot.x
      const dy = mouse.y - dot.y
      const dist = Math.sqrt(dx * dx + dy * dy)
      const maxDist = 150
      let size = dot.baseSize
      let color = 'rgba(216, 228, 242, 0.1)'
      if (dist < maxDist) {
        const ratio = 1 - (dist / maxDist)
        size = dot.baseSize + (ratio * 4)
        color = `rgba(0, 209, 255, ${0.1 + (ratio * 0.8)})`
      }
      ctx.beginPath()
      ctx.arc(dot.x, dot.y, size, 0, Math.PI * 2)
      ctx.fillStyle = color
      ctx.fill()
    })
    animationId = requestAnimationFrame(animate)
  }

  window.addEventListener('resize', resize)
  resize()
  animate()
  return { resize }
}

onMounted(() => {
  if (canvasRef.value) {
    initDots(canvasRef.value)
    window.addEventListener('mousemove', (e) => {
      mouse.x = e.clientX
      mouse.y = e.clientY
    })
  }
})

onUnmounted(() => {
  if (animationId) cancelAnimationFrame(animationId)
})

function handleCreate() {
  if (!nameInput.value.trim()) return
  localStorage.setItem('cs_player_name', nameInput.value.trim())
  createRoom(nameInput.value.trim(), gameMode.value, Number(imposterCount.value))
  showCreateModal.value = false
}

function handleJoin() {
  if (!roomIdInput.value.trim() || !joinNameInput.value.trim()) return
  localStorage.setItem('cs_player_name', joinNameInput.value.trim())
  joinRoom(roomIdInput.value.trim(), joinNameInput.value.trim())
  showJoinModal.value = false
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
    alert(t('home.passwordError'))
  }
}
</script>

<template>
  <div class="min-h-screen bg-black text-text-primary flex flex-col items-center relative overflow-hidden">
    <!-- 背景层 -->
    <div class="fixed inset-0 pointer-events-none z-0 dot-pattern opacity-20"></div>
    <canvas ref="canvasRef" class="fixed inset-0 pointer-events-none z-1 opacity-40"></canvas>

    <!-- Header -->
    <header
      class="flex justify-between items-center w-full px-8 md:px-16 py-6 fixed top-0 z-50 bg-black/60 backdrop-blur-xl border-b border-white/5">
      <div @click="handleAdminEntry" class="font-display text-xl md:text-2xl tracking-tighter text-primary font-bold uppercase cursor-pointer select-none active:opacity-50">CS 规则引擎</div>
    </header>

    <!-- Main Content -->
    <main class="flex-grow flex flex-col items-center justify-center w-full max-w-6xl px-6 relative z-10 pt-32 pb-20">
      <!-- Hero Section -->
      <div class="w-full mb-16 animate-[fadeIn_0.8s_ease-out] text-center">
        <h1
          class="font-display text-[48px] sm:text-[72px] md:text-[100px] lg:text-[120px] tracking-tighter leading-[1.1] font-black uppercase mb-12">
          CS规则<br class="md:hidden">引擎
        </h1>

        <div class="flex flex-wrap justify-center gap-6 mt-8">
          <div class="flex items-center gap-3 px-5 py-2.5 border border-outline/20 chamfer-clip-sm bg-white/5">
            <span class="material-symbols-outlined text-primary text-xl">casino</span>
            <span class="font-mono text-[11px] tracking-widest uppercase opacity-70">{{ $t('voting.progress') }}</span>
          </div>
          <div class="flex items-center gap-3 px-5 py-2.5 border border-outline/20 chamfer-clip-sm bg-white/5">
            <span class="material-symbols-outlined text-primary text-xl">how_to_vote</span>
            <span class="font-mono text-[11px] tracking-widest uppercase opacity-70">{{ $t('voting.title') }}</span>
          </div>
          <div class="flex items-center gap-3 px-5 py-2.5 border border-outline/20 chamfer-clip-sm bg-white/5">
            <span class="material-symbols-outlined text-primary text-xl">cyclone</span>
            <span class="font-mono text-[11px] tracking-widest uppercase opacity-70">{{ $t('roulette.title') }}</span>
          </div>
        </div>
      </div>

      <!-- CTA Buttons -->
      <div class="flex flex-col md:flex-row gap-8 w-full max-w-2xl mb-20 animate-[slideUp_0.8s_ease-out_0.2s_both]">
        <button @click="showCreateModal = true"
          class="flex-1 btn-base bg-primary text-on-primary chamfer-clip py-6 flex items-center justify-center gap-4 hover:brightness-110 shadow-[0_0_50px_rgba(164,230,255,0.15)]">
          <span class="material-symbols-outlined text-3xl" style="font-variation-settings: 'FILL' 1;">swords</span>
          <span class="text-xl tracking-tight uppercase font-bold">{{ $t('home.createRoom') }}</span>
        </button>
        <button @click="showJoinModal = true"
          class="flex-1 btn-base bg-transparent text-secondary border border-secondary/40 chamfer-clip py-6 flex items-center justify-center gap-4 hover:bg-secondary/10">
          <span class="material-symbols-outlined text-3xl">login</span>
          <span class="text-xl tracking-tight uppercase font-bold">{{ $t('home.joinRoom') }}</span>
        </button>
      </div>

      <!-- Metadata -->
      <div
        class="flex flex-wrap justify-center gap-16 font-mono text-[11px] text-outline/40 tracking-widest animate-[fadeIn_1s_ease-out_0.5s_both]">
        <div class="flex flex-col items-center">
          <span class="mb-2">系统状态</span>
          <div class="flex items-center gap-2 text-text-secondary">
            <span class="w-1.5 h-1.5 bg-green-500 rounded-full animate-pulse shadow-[0_0_8px_#22c55e]"></span>
            <span class="uppercase">运行正常</span>
          </div>
        </div>
      </div>
    </main>

    <!-- Footer -->
    <footer
      class="flex justify-between items-center w-full px-8 md:px-16 py-6 fixed bottom-0 left-0 bg-black/60 backdrop-blur-xl border-t border-white/5 z-50">
      <div class="font-mono text-[10px] text-outline/50 uppercase tracking-widest">© 2026 CS-RULE-ENGINE v1.0.4</div>
    </footer>

    <!-- Modals -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div
        class="modal-content w-[90%] max-w-[450px] p-8 bg-surface-container border border-white/10 chamfer-clip animate-[slideUp_0.3s_ease-out]">
        <h2 class="font-display text-2xl font-bold text-primary mb-6 uppercase">{{ $t('home.createRoom') }}</h2>
        <div class="mb-6">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">{{ $t('home.placeholderName') }}</label>
          <input v-model="nameInput" class="input-field chamfer-clip-sm" :placeholder="t('home.tactical')" maxlength="20"
            @keyup.enter="handleCreate" />
        </div>
        <div class="mb-6">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">{{ $t('home.gameModeTitle') }}</label>
          <div class="flex gap-4">
            <label class="flex items-center gap-2 text-white cursor-pointer">
              <input type="radio" v-model="gameMode" value="ROULETTE" class="accent-primary" />
              {{ $t('home.modeRoulette') }}
            </label>
            <label class="flex items-center gap-2 text-white cursor-pointer">
              <input type="radio" v-model="gameMode" value="IMPOSTER" class="accent-primary" />
              {{ $t('home.modeImposter') }}
            </label>
          </div>
        </div>
        <div v-if="gameMode === 'IMPOSTER'" class="mb-6">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">{{ $t('home.imposterCountLabel') }}</label>
          <select v-model="imposterCount" class="input-field chamfer-clip-sm bg-surface-container text-white">
            <option :value="1">1</option>
            <option :value="2">2</option>
            <option :value="3">3</option>
            <option :value="4">4</option>
            <option :value="5">5</option>
          </select>
        </div>
        <button @click="handleCreate"
          class="w-full btn-base bg-primary text-on-primary chamfer-clip py-4 uppercase font-bold">{{ $t('home.initGame') }}</button>
      </div>
    </div>

    <div v-if="showJoinModal" class="modal-overlay" @click.self="showJoinModal = false">
      <div
        class="modal-content w-[90%] max-w-[450px] p-8 bg-surface-container border border-white/10 chamfer-clip animate-[slideUp_0.3s_ease-out]">
        <h2 class="font-display text-2xl font-bold text-secondary mb-6 uppercase">{{ $t('home.joinRoom') }}</h2>
        <div class="mb-4">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">{{ $t('home.placeholderRoomId') }}</label>
          <input v-model="roomIdInput"
            class="input-field chamfer-clip-sm text-center text-2xl tracking-[0.4em] font-display uppercase"
            placeholder="0000" maxlength="4" @keyup.enter="handleJoin" />
        </div>
        <div class="mb-6">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">{{ $t('home.placeholderName') }}</label>
          <input v-model="joinNameInput" class="input-field chamfer-clip-sm" :placeholder="t('home.tactical')"
            maxlength="20" @keyup.enter="handleJoin" />
        </div>
        <button @click="handleJoin"
          class="w-full btn-base bg-secondary text-on-secondary chamfer-clip py-4 uppercase font-bold">{{ $t('home.enterGame') }}</button>
      </div>
    </div>

    <div v-if="showAdminModal" class="modal-overlay" @click.self="showAdminModal = false">
      <div
        class="modal-content w-[90%] max-w-[450px] p-8 bg-surface-container border border-white/10 chamfer-clip animate-[slideUp_0.3s_ease-out]">
        <div class="flex items-center gap-3 mb-6">
          <span class="material-symbols-outlined text-primary">security</span>
          <h2 class="font-display text-2xl font-bold text-primary uppercase">{{ $t('home.authTitle') }}</h2>
        </div>
        <div class="mb-6">
          <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">{{ $t('home.authCode') }}</label>
          <input v-model="adminPwInput" type="password" class="input-field chamfer-clip-sm" placeholder="••••••••"
            @keyup.enter="handleAdminLogin" />
        </div>
        <button @click="handleAdminLogin"
          class="w-full btn-base bg-primary text-on-primary chamfer-clip py-4 uppercase font-bold tracking-widest hover:brightness-110">{{ $t('home.authAccess') }}</button>
      </div>
    </div>

    <Transition name="fade">
      <div v-if="errorMsg"
        class="fixed top-24 right-8 z-[60] px-6 py-3 bg-red-900/80 border border-red-500/50 backdrop-blur-md text-white font-mono text-xs chamfer-clip-sm">
        [{{ $t('home.errorPrefix') }}]: {{ errorMsg }}
      </div>
    </Transition>
  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

input::placeholder {
  color: rgba(216, 228, 242, 0.2);
}
</style>
