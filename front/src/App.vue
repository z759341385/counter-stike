<script setup>
import { onMounted } from 'vue'
import { currentView, initGame } from './composables/useGame'
import HomeView from './views/HomeView.vue'
import LobbyView from './views/LobbyView.vue'
import VotingView from './views/VotingView.vue'
import RouletteView from './views/RouletteView.vue'

onMounted(() => {
  initGame()
})
</script>


<template>
  <Transition name="view" mode="out-in">
    <HomeView v-if="currentView === 'home'" key="home" />
    <LobbyView v-else-if="currentView === 'lobby'" key="lobby" />
    <VotingView v-else-if="currentView === 'voting'" key="voting" />
    <RouletteView v-else-if="currentView === 'roulette'" key="roulette" />
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
