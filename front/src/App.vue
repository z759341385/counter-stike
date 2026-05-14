<script setup>
import { onMounted } from 'vue'
import { currentView, initGame } from './composables/useGame'
import HomeView from './views/HomeView.vue'
import LobbyView from './views/LobbyView.vue'
import AdminView from './views/AdminView.vue'

onMounted(() => {
  initGame()
})
</script>

<template>
  <Transition name="view" mode="out-in">
    <HomeView v-if="currentView === 'home'" key="home" />
    <LobbyView v-else-if="currentView === 'lobby'" key="lobby" />
    <AdminView v-else-if="currentView === 'admin'" key="admin" />
  </Transition>
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
