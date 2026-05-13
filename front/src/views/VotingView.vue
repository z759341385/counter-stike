<script setup>
/**
 * VotingView - 投票区
 * 3 张规则卡片横向排列，玩家点击投票
 */
import {
  voteOptions, selectedOption, hasVoted,
  totalVotes, players, isHost,
  submitVote, stopVoteAndSpin
} from '../composables/useGame'

const cardColors = [
  { border: 'rgba(74, 144, 217, 0.6)', bg: 'rgba(74, 144, 217, 0.08)' },
  { border: 'rgba(212, 168, 67, 0.6)', bg: 'rgba(212, 168, 67, 0.08)' },
  { border: 'rgba(231, 76, 60, 0.6)',  bg: 'rgba(231, 76, 60, 0.08)' },
]

function handleVote(optionId) {
  if (hasVoted.value) return
  submitVote(optionId)
}
</script>

<template>
  <div class="min-h-screen flex flex-col items-center px-4 py-8">
    <!-- 标题 -->
    <div class="text-center mb-10 animate-[fadeIn_0.5s]">
      <h1 class="font-[Orbitron] text-2xl md:text-3xl font-bold tracking-wider text-ct-blue-light mb-2">
        📋 选择你支持的规则
      </h1>
      <p class="text-text-secondary">点击卡片完成投票（每人一票）</p>
    </div>

    <!-- 规则卡片 -->
    <div class="w-full max-w-4xl grid grid-cols-1 md:grid-cols-3 gap-6 mb-10">
      <div
        v-for="(option, index) in voteOptions"
        :key="option.id"
        class="glass-card p-6 cursor-pointer relative overflow-hidden transition-all duration-300"
        :class="{
          'scale-105 ring-2': selectedOption === option.id,
          'opacity-60 cursor-not-allowed': hasVoted && selectedOption !== option.id,
        }"
        :style="{
          borderColor: selectedOption === option.id ? cardColors[index].border : '',
          background: selectedOption === option.id ? cardColors[index].bg : '',
          ringColor: selectedOption === option.id ? cardColors[index].border : '',
          animationDelay: `${index * 0.15}s`,
        }"
        @click="handleVote(option.id)"
      >
        <!-- 选中标记 -->
        <div
          v-if="selectedOption === option.id"
          class="absolute top-3 right-3 w-7 h-7 rounded-full bg-ct-blue flex items-center justify-center text-white text-sm font-bold"
        >
          ✓
        </div>

        <!-- 序号 -->
        <div class="text-3xl mb-3">
          {{ ['🅰️', '🅱️', '🅲'][index] || '❓' }}
        </div>

        <!-- 规则名 -->
        <h3 class="font-[Orbitron] text-lg font-bold mb-3 text-text-primary">
          {{ option.name }}
        </h3>

        <!-- 规则描述 -->
        <p class="text-text-secondary text-sm leading-relaxed">
          {{ option.description }}
        </p>
      </div>
    </div>

    <!-- 投票进度 -->
    <div class="w-full max-w-md mb-8">
      <div class="glass-card p-4 text-center">
        <p class="text-text-secondary text-sm mb-2">投票进度</p>
        <div class="flex items-center gap-3">
          <div class="flex-1 h-2 bg-white/5 rounded-full overflow-hidden">
            <div
              class="h-full bg-gradient-to-r from-ct-blue to-ct-blue-light rounded-full transition-all duration-500"
              :style="{ width: `${(totalVotes / Math.max(players.length, 1)) * 100}%` }"
            ></div>
          </div>
          <span class="font-[Orbitron] text-sm text-ct-blue-light whitespace-nowrap">
            {{ totalVotes }} / {{ players.length }}
          </span>
        </div>
      </div>
    </div>

    <!-- 房主结束投票按钮 -->
    <div v-if="isHost" class="mt-auto pb-8">
      <button
        class="btn-secondary text-lg px-10 py-4"
        @click="stopVoteAndSpin"
      >
        🎡 结束投票 · 开始轮盘
      </button>
    </div>
    <div v-else-if="hasVoted" class="mt-auto pb-8">
      <div class="glass-card px-8 py-4">
        <p class="text-text-secondary">✅ 已投票，等待房主结束投票...</p>
      </div>
    </div>
  </div>
</template>
