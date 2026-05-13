<script setup>
/**
 * LobbyView - 大厅等候室 (观战区版)
 */
import { roomId, players, spectators, isHost, startVote, takeSeat, leaveSeat, isMe } from '../composables/useGame'

const avatarIcons = ['swords', 'shield', 'target', 'skull', 'military_tech', 'radar', 'security', 'token', 'potted_plant', 'rocket']

function getIcon(index) {
  return avatarIcons[index % avatarIcons.length]
}

function handleSeatClick(index) {
  if (!players.value) return;
  
  const currentPlayerInSeat = players.value[index];
  if (!currentPlayerInSeat) {
    // 位置是空的，坐下
    takeSeat(index);
  }
}
</script>

<template>
  <div class="min-h-screen flex flex-col items-center px-6 pt-32 pb-24 relative overflow-hidden dot-pattern">
    <!-- Header -->
    <header class="flex justify-between items-center w-full px-8 md:px-16 py-6 fixed top-0 z-50 bg-black/60 backdrop-blur-xl border-b border-white/5">
      <div class="font-display text-xl md:text-2xl tracking-tighter text-primary font-bold uppercase">CS Rule Engine</div>
      <div class="flex items-center gap-4">
        <div class="px-3 py-1 bg-primary/10 border border-primary/30 chamfer-clip-sm font-mono text-[10px] text-primary uppercase">
          Room: {{ roomId }}
        </div>
      </div>
    </header>

    <!-- 房间标题 -->
    <div class="text-center mb-12 animate-[fadeIn_0.5s] relative z-10">
      <p class="font-mono text-[11px] text-text-secondary tracking-[0.4em] mb-4 uppercase opacity-60">Operations Lobby</p>
      <h1 class="font-display text-5xl md:text-7xl font-black tracking-[0.1em] text-primary uppercase">
        Deployment
      </h1>
    </div>

    <!-- 战队分边布局 -->
    <div class="w-full max-w-6xl relative z-10 flex flex-col md:flex-row gap-8 items-start">
      
      <!-- Team Alpha (0-4) -->
      <div class="flex-1 w-full">
        <div class="flex items-center gap-3 mb-6 px-4">
          <span class="w-2 h-6 bg-primary shadow-[0_0_10px_#a4e6ff]"></span>
          <h2 class="font-display text-2xl font-bold tracking-widest text-primary uppercase">Team Alpha</h2>
        </div>
        <div class="grid grid-cols-1 gap-3">
          <div
            v-for="idx in [0, 1, 2, 3, 4]"
            :key="idx"
            @click="handleSeatClick(idx)"
            class="relative p-4 border chamfer-clip transition-all duration-300 group"
            :class="[
              (players && players[idx]) ? (isMe(players[idx]) ? 'border-primary bg-primary/10' : 'border-white/10 bg-white/5') : 'border-white/5 bg-transparent border-dashed cursor-pointer hover:border-primary/40 hover:bg-primary/5'
            ]"
          >
            <div v-if="players && players[idx]" class="flex items-center gap-4">
              <span class="material-symbols-outlined text-2xl text-primary">{{ getIcon(idx) }}</span>
              <div class="flex-grow">
                <p class="text-sm font-bold uppercase tracking-tight" :class="isMe(players[idx]) ? 'text-primary' : 'text-text-primary'">
                  {{ players[idx].playerName }} <span v-if="isMe(players[idx])" class="text-[9px] opacity-40 ml-1">(YOU)</span>
                </p>
              </div>
              <button v-if="isMe(players[idx])" @click.stop="leaveSeat" class="px-2 py-1 bg-red-500/20 border border-red-500/40 text-red-500 font-mono text-[9px] uppercase chamfer-clip-sm hover:bg-red-500 hover:text-white transition-all">
                Leave
              </button>
            </div>
            <div v-else class="flex items-center gap-4 opacity-20 group-hover:opacity-100 transition-opacity">
              <span class="material-symbols-outlined text-2xl">add_circle</span>
              <span class="font-mono text-[10px] uppercase tracking-widest">Join Alpha</span>
            </div>
          </div>
        </div>
      </div>

      <!-- VS -->
      <div class="hidden md:flex flex-col items-center justify-center gap-4 opacity-20 self-center">
        <span class="font-display text-4xl font-black italic tracking-tighter">VS</span>
      </div>

      <!-- Team Bravo (5-9) -->
      <div class="flex-1 w-full text-right">
        <div class="flex items-center gap-3 mb-6 px-4 flex-row-reverse">
          <span class="w-2 h-6 bg-secondary shadow-[0_0_10px_#ffdb9d]"></span>
          <h2 class="font-display text-2xl font-bold tracking-widest text-secondary uppercase">Team Bravo</h2>
        </div>
        <div class="grid grid-cols-1 gap-3">
          <div
            v-for="idx in [5, 6, 7, 8, 9]"
            :key="idx"
            @click="handleSeatClick(idx)"
            class="relative p-4 border chamfer-clip transition-all duration-300 group"
            :class="[
              (players && players[idx]) ? (isMe(players[idx]) ? 'border-secondary bg-secondary/10' : 'border-white/10 bg-white/5') : 'border-white/5 bg-transparent border-dashed cursor-pointer hover:border-secondary/40 hover:bg-secondary/5'
            ]"
          >
            <div v-if="players && players[idx]" class="flex items-center gap-4 flex-row-reverse">
              <span class="material-symbols-outlined text-2xl text-secondary">{{ getIcon(idx) }}</span>
              <div class="flex-grow">
                <p class="text-sm font-bold uppercase tracking-tight" :class="isMe(players[idx]) ? 'text-secondary' : 'text-text-primary'">
                   <span v-if="isMe(players[idx])" class="text-[9px] opacity-40 mr-1">(YOU)</span> {{ players[idx].playerName }}
                </p>
              </div>
              <button v-if="isMe(players[idx])" @click.stop="leaveSeat" class="px-2 py-1 bg-red-500/20 border border-red-500/40 text-red-500 font-mono text-[9px] uppercase chamfer-clip-sm hover:bg-red-500 hover:text-white transition-all">
                Leave
              </button>
            </div>
            <div v-else class="flex items-center gap-4 opacity-20 group-hover:opacity-100 transition-opacity flex-row-reverse">
              <span class="material-symbols-outlined text-2xl">add_circle</span>
              <span class="font-mono text-[10px] uppercase tracking-widest">Join Bravo</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Spectator Area -->
    <div class="w-full max-w-6xl mt-12 relative z-10">
      <div class="flex items-center gap-3 mb-6 px-4">
        <span class="material-symbols-outlined text-outline">visibility</span>
        <h2 class="font-mono text-sm font-bold tracking-[0.3em] text-outline uppercase">Spectator Area ({{ spectators.length }})</h2>
      </div>
      <div class="flex flex-wrap gap-4 p-6 bg-white/[0.02] border border-white/5 chamfer-clip">
        <div 
          v-for="spec in spectators" 
          :key="spec.token"
          class="px-4 py-2 bg-white/5 border border-white/10 chamfer-clip-sm flex items-center gap-2"
          :class="{'border-primary/50 bg-primary/5': isMe(spec)}"
        >
          <span class="w-1.5 h-1.5 rounded-full" :class="spec.connected ? 'bg-green-500 animate-pulse' : 'bg-red-500'"></span>
          <span class="text-xs font-mono uppercase tracking-tight" :class="isMe(spec) ? 'text-primary' : 'text-text-secondary'">
            {{ spec.playerName }}
          </span>
          <span v-if="spec.isHost" class="material-symbols-outlined text-[14px] text-primary">crown</span>
        </div>
        <div v-if="spectators.length === 0" class="w-full text-center py-4 font-mono text-[10px] uppercase opacity-20">
          No personnel in standby
        </div>
      </div>
    </div>

    <!-- Start Button -->
    <div class="mt-16 pb-12 text-center relative z-10 w-full max-w-lg">
      <button
        v-if="isHost"
        class="w-full btn-base bg-primary text-on-primary chamfer-clip py-5 text-xl uppercase font-black shadow-[0_0_40px_rgba(164,230,255,0.2)]"
        @click="startVote"
      >
        🎲 Start Operation
      </button>
      <div v-else class="p-5 bg-white/5 border border-white/10 chamfer-clip font-mono text-xs text-primary uppercase tracking-[0.2em]">
        Waiting for command...
      </div>
    </div>
  </div>
</template>
