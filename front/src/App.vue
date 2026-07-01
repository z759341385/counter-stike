<script setup>
import { onMounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { currentView, initGame } from './composables/useGame'
import HomeView from './views/HomeView.vue'
import LobbyView from './views/LobbyView.vue'
import AdminView from './views/AdminView.vue'
import SimulatorView from './views/SimulatorView.vue'

const { locale } = useI18n()

onMounted(() => {
  initGame()
  
  // 简易 Hash 路由逻辑，让每个页面都有对应的 URL
  const hash = window.location.hash.replace('#/', '').replace('#', '')
  if (['home', 'lobby', 'admin', 'simulator'].includes(hash)) {
    currentView.value = hash
  } else {
    window.location.hash = '/' + currentView.value
  }
  
  window.addEventListener('hashchange', () => {
    const newHash = window.location.hash.replace('#/', '').replace('#', '')
    if (['home', 'lobby', 'admin', 'simulator'].includes(newHash)) {
      if (currentView.value !== newHash) {
        currentView.value = newHash
      }
    }
  })
})

watch(currentView, (newVal) => {
  const currentHash = window.location.hash.replace('#/', '').replace('#', '')
  if (currentHash !== newVal) {
    window.location.hash = '/' + newVal
  }
})
</script>

<template>
  <div class="app-wrapper flex flex-col min-h-screen bg-black">
    <!-- 全局顶部导航栏 (仅在首页和模拟器页面显示) -->
    <header v-if="['home', 'simulator'].includes(currentView)"
      class="flex justify-between items-center w-full px-8 md:px-16 py-6 fixed top-0 z-[60] bg-black/60 backdrop-blur-xl border-b border-white/5 transition-all duration-300">
      <div class="flex items-center gap-8">
        <div @click="currentView = 'home'"
          class="font-display text-xl md:text-2xl tracking-tighter text-primary font-bold uppercase cursor-pointer select-none active:opacity-50">
          56CS
        </div>
        <nav class="hidden md:flex gap-6">
          <button @click="currentView = 'home'"
            :class="['text-sm font-mono tracking-widest uppercase transition-colors', currentView === 'home' ? 'text-primary' : 'text-outline/60 hover:text-outline']">
            {{ $t('nav.home', '首页') }}
          </button>
          <button @click="currentView = 'simulator'"
            :class="['text-sm font-mono tracking-widest uppercase transition-colors', currentView === 'simulator' ? 'text-primary' : 'text-outline/60 hover:text-outline']">
            {{ $t('nav.simulator', '开箱模拟器') }}
          </button>
        </nav>
      </div>
      <!-- 移动端也可以补充菜单栏，或者暂时仅在桌面端显示导航 -->
    </header>

    <Transition name="view" mode="out-in">
      <HomeView v-if="currentView === 'home'" key="home" />
      <LobbyView v-else-if="currentView === 'lobby'" key="lobby" />
      <AdminView v-else-if="currentView === 'admin'" key="admin" />
      <SimulatorView v-else-if="currentView === 'simulator'" key="simulator" />
    </Transition>
  </div>
</template>

<style>
.view-enter-active,
.view-leave-active {
  transition: all 0.4s cubic-bezier(0.16, 1, 0.3, 1);
}
.view-enter-from {
  opacity: 0;
  transform: translateY(20px);
}
.view-leave-to {
  opacity: 0;
  transform: translateY(-20px);
}
</style>
