<script setup>
/**
 * RouletteView - 轮盘结算动画
 * 核心实现：CSS conic-gradient 绘制扇区 + transition 旋转动画
 */
import { ref, computed, onMounted } from 'vue'
import { spinData, showWinner, isHost, resetRoom } from '../composables/useGame'

const wheelRotation = ref(0)
const isSpinning = ref(false)

// 扇区配色
const sectorColors = [
  ['#4A90D9', '#3672B5'],
  ['#D4A843', '#B8912E'],
  ['#E74C3C', '#C0392B'],
]

// 构建 conic-gradient
const wheelGradient = computed(() => {
  if (!spinData.value) return ''
  const sectors = spinData.value.sectors
  const parts = []
  for (let i = 0; i < sectors.length; i++) {
    const color = sectorColors[i % sectorColors.length][0]
    const colorDark = sectorColors[i % sectorColors.length][1]
    parts.push(`${color} ${sectors[i].startAngle}deg`)
    parts.push(`${colorDark} ${sectors[i].endAngle}deg`)
  }
  return `conic-gradient(${parts.join(', ')})`
})

// 各扇区的标签位置（中间角度）
const sectorLabels = computed(() => {
  if (!spinData.value) return []
  return spinData.value.sectors.map((s, i) => {
    const midAngle = (s.startAngle + s.endAngle) / 2
    const rad = (midAngle - 90) * (Math.PI / 180)
    const r = 38 // 百分比半径
    return {
      name: s.name,
      x: 50 + r * Math.cos(rad),
      y: 50 + r * Math.sin(rad),
      rotation: midAngle,
      index: i,
    }
  })
})

// 启动旋转动画
onMounted(() => {
  if (!spinData.value) return
  // 先设置初始位置（无动画）
  wheelRotation.value = 0
  isSpinning.value = false

  // 下一帧开始动画
  requestAnimationFrame(() => {
    requestAnimationFrame(() => {
      isSpinning.value = true
      // 多转 5 圈 + 目标角度（注意轮盘是顺时针转，指针在顶部）
      wheelRotation.value = 360 * 5 + (360 - spinData.value.targetAngle)
    })
  })

  // 动画结束后显示结果
  setTimeout(() => {
    showWinner.value = true
  }, spinData.value.duration + 500)
})

function handleBack() {
  showWinner.value = false
  resetRoom()
}
</script>

<template>
  <div class="min-h-screen flex flex-col items-center justify-center px-4 py-8 relative">
    <!-- 标题 -->
    <h1 class="font-[Orbitron] text-2xl font-bold text-ct-blue-light mb-8 tracking-wider animate-[fadeIn_0.5s]">
      🎡 命运轮盘
    </h1>

    <!-- 轮盘容器 -->
    <div class="relative w-72 h-72 md:w-96 md:h-96 mb-10">
      <!-- 指针（顶部） -->
      <div class="absolute top-[-18px] left-1/2 -translate-x-1/2 z-20 text-3xl drop-shadow-lg">
        🔻
      </div>

      <!-- 外圈装饰 -->
      <div class="absolute inset-[-8px] rounded-full border-2 border-white/10 shadow-[0_0_60px_rgba(74,144,217,0.15)]"></div>

      <!-- 轮盘本体 -->
      <div
        class="w-full h-full rounded-full overflow-hidden relative shadow-2xl"
        :class="{ 'wheel-spin': isSpinning }"
        :style="{
          background: wheelGradient,
          transform: `rotate(${wheelRotation}deg)`,
        }"
      >
        <!-- 扇区标签 -->
        <div
          v-for="label in sectorLabels"
          :key="label.index"
          class="absolute text-white font-bold text-xs md:text-sm pointer-events-none drop-shadow-md"
          :style="{
            left: `${label.x}%`,
            top: `${label.y}%`,
            transform: `translate(-50%, -50%) rotate(${label.rotation}deg)`,
            textShadow: '0 2px 4px rgba(0,0,0,0.6)',
          }"
        >
          {{ label.name }}
        </div>

        <!-- 中心圆 -->
        <div class="absolute top-1/2 left-1/2 -translate-x-1/2 -translate-y-1/2 w-16 h-16 md:w-20 md:h-20 rounded-full bg-bg-dark border-2 border-white/20 flex items-center justify-center z-10">
          <span class="text-2xl md:text-3xl">🎯</span>
        </div>
      </div>
    </div>

    <!-- 扇区图例 -->
    <div class="flex gap-6 mb-6 animate-[fadeIn_0.8s]">
      <div
        v-for="(sector, i) in spinData?.sectors || []"
        :key="sector.optionId"
        class="flex items-center gap-2"
      >
        <div
          class="w-3 h-3 rounded-full"
          :style="{ background: sectorColors[i % sectorColors.length][0] }"
        ></div>
        <span class="text-text-secondary text-sm">{{ sector.name }} ({{ sector.votes }}票)</span>
      </div>
    </div>

    <!-- 获胜结果弹窗 -->
    <div v-if="showWinner" class="modal-overlay">
      <div class="modal-content glass-card text-center">
        <!-- 彩带效果 -->
        <div class="text-5xl mb-4 animate-[float_1.5s_ease-in-out_infinite]">🏆</div>
        <h2 class="font-[Orbitron] text-sm tracking-widest text-t-yellow mb-3 uppercase">本轮规则</h2>
        <h3 class="font-[Orbitron] text-2xl font-black text-ct-blue-light mb-4">
          {{ spinData?.winner?.name }}
        </h3>
        <p class="text-text-secondary text-sm leading-relaxed mb-8 px-2">
          {{ spinData?.winner?.description }}
        </p>
        <button
          v-if="isHost"
          class="btn-primary w-full"
          @click="handleBack"
        >
          🔄 确认 · 返回大厅
        </button>
        <div v-else class="glass-card px-6 py-3">
          <p class="text-text-secondary text-sm">等待房主确认...</p>
        </div>
      </div>
    </div>
  </div>
</template>
