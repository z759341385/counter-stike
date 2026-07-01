<script setup>
import { ref, onMounted, computed, nextTick } from 'vue'
import { currentView } from '../composables/useGame'
import { crates, inventory, isLoading, prices, cratePrices, loadSimulatorData, unboxCrate, addInventoryItem, clearInventory } from '../composables/useSimulator'

const getItemBasePrice = (itemId, isRare = false) => {
  if (!prices.value || !prices.value[itemId]) return isRare ? '380.00' : '0.50';
  const p = prices.value[itemId];
  if (p['久经沙场'] && p['久经沙场'].price) return p['久经沙场'].price.toFixed(2);
  const anyWear = Object.values(p).find(w => w.price > 0);
  return anyWear ? anyWear.price.toFixed(2) : (isRare ? '380.00' : '0.50');
}

onMounted(async () => {
  await loadSimulatorData()
  if (crates.value.length > 0 && !selectedCrate.value) {
    selectedCrate.value = crates.value[0]
  }
})

const selectedCrate = ref(null)
const isSpinning = ref(false)
const showResult = ref(false)
const spinResult = ref(null)
const spinnerTrack = ref(null)
const searchQuery = ref('')
const isAutoOpen = ref(false)

function toggleAutoOpen() {
  isAutoOpen.value = !isAutoOpen.value
  if (isAutoOpen.value && !isSpinning.value) {
    startUnbox()
  }
}

// Animation state
const spinnerItems = ref([])
const spinDuration = 5000 // 5 seconds
const itemWidth = 140 // pixels per item in spinner

function selectCrate(crate) {
  if (isSpinning.value) return
  selectedCrate.value = crate
  showResult.value = false
}

const filteredCrates = computed(() => {
  if (!searchQuery.value) return crates.value
  const q = searchQuery.value.toLowerCase()
  return crates.value.filter(c => c.name.toLowerCase().includes(q))
})

async function startUnbox() {
  if (!selectedCrate.value || isSpinning.value) return

  isSpinning.value = true
  showResult.value = false

  spinResult.value = unboxCrate(selectedCrate.value)

  const items = []
  for (let i = 0; i < 45; i++) {
    const pool = selectedCrate.value.contains
    if (pool.length > 0) {
      const randItem = pool[Math.floor(Math.random() * pool.length)]
      items.push({
        id: `fake-${i}`,
        image: randItem.image,
        color: randItem.rarity ? randItem.rarity.color : '#4b69ff'
      })
    }
  }
  items[40] = {
    id: 'winner',
    image: spinResult.value.image,
    color: spinResult.value.rarityColor
  }
  for (let i = 41; i < 45; i++) {
    const pool = selectedCrate.value.contains
    const randItem = pool[Math.floor(Math.random() * pool.length)]
    items.push({
      id: `fake-${i}`,
      image: randItem.image,
      color: randItem.rarity ? randItem.rarity.color : '#4b69ff'
    })
  }

  spinnerItems.value = items

  await nextTick()

  if (spinnerTrack.value) {
    spinnerTrack.value.style.transition = 'none'
    spinnerTrack.value.style.transform = 'translateX(0px)'

    void spinnerTrack.value.offsetWidth

    const randomOffset = Math.floor(Math.random() * (itemWidth - 20)) + 10
    // We want the 40th item to land in the center of the spinner container
    // Since spinner is flex-1, its width varies. We use its parent clientWidth.
    const containerWidth = spinnerTrack.value.parentElement.clientWidth
    const targetX = -(40 * itemWidth - (containerWidth / 2) + itemWidth / 2 + randomOffset)

    spinnerTrack.value.style.transition = `transform ${spinDuration}ms cubic-bezier(0.15, 0.85, 0.1, 1)`
    spinnerTrack.value.style.transform = `translateX(${targetX}px)`
  }

  setTimeout(() => {
    isSpinning.value = false
    showResult.value = true
    addInventoryItem(spinResult.value)

    // Auto-open logic
    if (isAutoOpen.value) {
      setTimeout(() => {
        if (isAutoOpen.value) {
          showResult.value = false;
          startUnbox();
        }
      }, 1500); // Wait 1.5s to show the result before opening the next one
    }
  }, spinDuration + 200)
}

function handleSpacebar(e) {
  if (e.code === 'Space' && !isSpinning.value && batchResults.value.length === 0) {
    e.preventDefault()
    if (showResult.value) {
      showResult.value = false
    } else {
      startUnbox()
    }
  }
}

const batchResults = ref([])

function startBatchUnbox(count) {
  if (!selectedCrate.value || isSpinning.value) return;

  // 隐藏单次开箱的弹窗
  showResult.value = false;

  const results = [];
  for (let i = 0; i < count; i++) {
    const res = unboxCrate(selectedCrate.value);
    results.push(res);
    addInventoryItem(res);
  }

  // 按价格从高到低排序，让玩家第一眼看到最值钱的
  results.sort((a, b) => (b.price || 0) - (a.price || 0));

  batchResults.value = results;
}

onMounted(() => {
  window.addEventListener('keydown', handleSpacebar)
})

// 根据SA网站的逻辑：分离普通物品和特殊(罕见)物品
const normalContents = computed(() => {
  if (!selectedCrate.value || !selectedCrate.value.contains) return []

  const colorOrder = { '#eb4b4b': 1, '#d32ce6': 2, '#8847ff': 3, '#4b69ff': 4 }
  return [...selectedCrate.value.contains].sort((a, b) => {
    const orderA = colorOrder[a.rarity?.color] || 99
    const orderB = colorOrder[b.rarity?.color] || 99
    return orderA - orderB
  })
})

const rareContents = computed(() => {
  if (!selectedCrate.value || !selectedCrate.value.contains_rare) return []
  return selectedCrate.value.contains_rare
})

const totalOpens = computed(() => inventory.value.length)
const totalCost = computed(() => inventory.value.reduce((sum, item) => sum + (item.cost || 2.59), 0))
const totalReturn = computed(() => inventory.value.reduce((sum, item) => sum + (item.price || 0), 0))
const profit = computed(() => totalReturn.value - totalCost.value)
const roi = computed(() => totalCost.value > 0 ? ((profit.value / totalCost.value) * 100).toFixed(2) : '0.00')

const currentCrateCost = computed(() => {
  if (!selectedCrate.value) return '0.00';
  const c = cratePrices.value?.[selectedCrate.value.id];
  if (!c) return '2.59';
  return (c.cost + c.keyCost).toFixed(2);
})

const rates = [
  { name: '军规', color: '#4b69ff', percent: '79.92' },
  { name: '受限制的', color: '#8847ff', percent: '15.98' },
  { name: '机密', color: '#d32ce6', percent: '3.20' },
  { name: '隐蔽', color: '#eb4b4b', percent: '0.64' },
  { name: '刀/手套', color: '#ffd700', percent: '0.26' },
]

</script>

<template>
  <div class="flex h-screen bg-[#1a1918] text-[#d4d4d4] font-sans overflow-hidden pt-20">

    <!-- Left Sidebar: Case Selection -->
    <div class="w-[300px] bg-[#232220] flex flex-col border-r border-[#333] shrink-0 z-20">
      <div class="p-4 flex justify-between items-center text-sm border-b border-[#333]">
        <span class="font-bold text-white cursor-pointer hover:text-primary transition-colors flex items-center gap-1"
          @click="currentView = 'home'">
          <span class="material-symbols-outlined text-[16px]">arrow_back</span>
          选择个案
        </span>
        <span class="text-xs text-gray-500">{{ filteredCrates.length }}例</span>
      </div>

      <div class="p-4">
        <div class="relative flex items-center">
          <span class="material-symbols-outlined absolute left-3 text-gray-500 text-sm">search</span>
          <input v-model="searchQuery"
            class="w-full bg-[#111] border border-[#333] rounded-md pl-9 pr-3 py-2 text-sm text-white focus:outline-none focus:border-gray-500 transition-colors"
            placeholder="搜索案例..." />
        </div>
      </div>

      <div class="flex-1 overflow-y-auto custom-scrollbar px-2 pb-4">
        <div v-if="isLoading" class="flex justify-center p-8">
          <div class="animate-spin rounded-full h-8 w-8 border-t-2 border-b-2 border-primary"></div>
        </div>

        <div v-for="crate in filteredCrates" :key="crate.id" @click="selectCrate(crate)"
          :class="['flex items-center gap-3 p-2 mb-1 rounded-md cursor-pointer transition-colors',
            selectedCrate?.id === crate.id ? 'bg-[#333] border border-gray-600' : 'hover:bg-[#2a2928] border border-transparent']">
          <img :src="crate.image" class="w-14 h-14 object-contain drop-shadow-md shrink-0" />
          <div class="flex flex-col overflow-hidden">
            <span class="text-sm font-bold text-white truncate">{{ crate.name }}</span>
            <span class="text-xs text-gray-500 truncate">{{ (cratePrices && cratePrices[crate.id] ?
              cratePrices[crate.id].cost : 0.1).toFixed(2) }} 美元 + {{ (cratePrices && cratePrices[crate.id] ?
                cratePrices[crate.id].keyCost : 2.49).toFixed(2) }} 美元密钥</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Right Main Area -->
    <div class="flex-1 flex flex-col min-h-0 overflow-y-auto custom-scrollbar bg-[#1a1918] pb-12">

      <!-- Top Stats Row -->
      <div class="p-6 pb-0 flex gap-4">
        <div
          class="flex-1 bg-[#232220] rounded-md p-4 text-center border border-white/5 flex flex-col justify-center items-center">
          <div class="text-xs text-gray-400 mb-1">已打开</div>
          <div class="text-xl font-bold text-white">{{ totalOpens }}</div>
        </div>
        <div
          class="flex-1 bg-[#232220] rounded-md p-4 text-center border border-white/5 flex flex-col justify-center items-center">
          <div class="text-xs text-gray-400 mb-1">花费</div>
          <div class="text-xl font-bold text-red-400">{{ totalCost.toFixed(2) }}美元</div>
        </div>
        <div
          class="flex-1 bg-[#232220] rounded-md p-4 text-center border border-white/5 flex flex-col justify-center items-center">
          <div class="text-xs text-gray-400 mb-1">价值</div>
          <div class="text-xl font-bold text-green-500">{{ totalReturn.toFixed(2) }}美元</div>
        </div>
        <div
          class="flex-1 bg-[#232220] rounded-md p-4 text-center border border-white/5 flex flex-col justify-center items-center">
          <div class="text-xs text-gray-400 mb-1">利润</div>
          <div :class="['text-xl font-bold', profit >= 0 ? 'text-green-500' : 'text-red-500']">{{ profit >= 0 ? '+' : ''
          }}{{ profit.toFixed(2) }} 美元</div>
        </div>
        <div
          class="flex-1 bg-[#232220] rounded-md p-4 text-center border border-white/5 flex flex-col justify-center items-center">
          <div class="text-xs text-gray-400 mb-1">投资回报率</div>
          <div :class="['text-xl font-bold', profit >= 0 ? 'text-green-500' : 'text-red-500']">{{ profit >= 0 &&
            totalCost > 0 ? '+' : '' }}{{ roi }} %</div>
        </div>
        <div class="flex items-center gap-2">
          <!-- Inventory / Clear Buttons -->
          <button @click="clearInventory"
            class="bg-[#232220] border border-white/5 p-4 rounded-md hover:bg-[#333] transition-colors flex items-center justify-center h-full aspect-square"
            title="清空库存">
            <span class="material-symbols-outlined text-gray-400">delete</span>
          </button>
        </div>
      </div>

      <!-- Main Content Area -->
      <div class="p-6 flex flex-col gap-6" v-if="selectedCrate">

        <!-- Unbox Section -->
        <div class="flex bg-[#232220] rounded-lg h-[280px] border border-white/5 relative overflow-hidden">

          <!-- Case Info (Left) -->
          <div
            class="w-[300px] flex flex-col justify-center items-center p-6 shrink-0 relative z-10 bg-gradient-to-r from-[#232220] to-transparent bg-[#232220] shadow-[10px_0_20px_rgba(0,0,0,0.5)]">
            <img :src="selectedCrate.image"
              class="w-32 h-32 object-contain drop-shadow-[0_10px_20px_rgba(0,0,0,0.5)]" />
            <div class="font-bold text-white text-lg mt-4 text-center">{{ selectedCrate.name }}</div>
            <div class="text-[11px] text-gray-500 mt-2 text-center">点击 <span
                class="bg-[#333] text-gray-300 px-1 py-0.5 rounded border border-gray-600">Space</span> 或单击“打开”按钮。</div>
          </div>

          <!-- Spinner Area (Right) -->
          <div class="flex-1 bg-[#111] relative overflow-hidden flex items-center justify-center">

            <!-- Result Overlay -->
            <div v-if="showResult && spinResult"
              class="absolute inset-0 z-30 flex flex-col items-center justify-center bg-black/80 backdrop-blur-sm animate-[fadeIn_0.3s_ease-out]">
              <div class="absolute inset-0 blur-[80px] opacity-20 rounded-full"
                :style="{ background: spinResult.rarityColor }"></div>
              <img :src="spinResult.image"
                class="w-48 h-48 object-contain drop-shadow-[0_20px_30px_rgba(0,0,0,0.8)] hover:scale-110 transition-transform duration-300" />
              <div class="text-xl font-bold text-white mt-4" :style="{ color: spinResult.rarityColor }">{{
                spinResult.itemName }}{{ spinResult.phase ? ' (' + spinResult.phase + ')' : '' }}</div>
              <div class="text-xs text-gray-400 mt-2">
                <span v-if="spinResult.isStatTrak" class="text-orange-500 mr-2">StatTrak™</span>
                {{ spinResult.wear }} | {{ spinResult.floatValue.toFixed(6) }}
              </div>
              <div class="text-green-400 font-bold mt-2">${{ (spinResult.price || 0).toFixed(2) }}</div>
            </div>

            <!-- Spinner Track -->
            <div v-show="isSpinning && !showResult"
              class="absolute top-0 bottom-0 flex items-center will-change-transform" ref="spinnerTrack"
              style="left: 0;">
              <div v-for="(item, idx) in spinnerItems" :key="idx"
                class="flex flex-col items-center justify-center h-full border-r border-[#222] bg-[#1a1918]"
                :style="{ width: itemWidth + 'px', borderBottom: `4px solid ${item.color}` }">
                <img :src="item.image" class="w-full h-[60%] object-contain drop-shadow-md" />
              </div>
            </div>

            <!-- Center Line -->
            <div v-show="isSpinning && !showResult"
              class="absolute left-1/2 top-0 bottom-0 w-[2px] bg-yellow-400 z-20 transform -translate-x-1/2 shadow-[0_0_10px_rgba(250,204,21,1)]">
            </div>
          </div>
        </div>

        <!-- Controls -->
        <div class="flex items-center gap-3">
          <button @click="startUnbox" :disabled="isSpinning"
            class="bg-[#d4b024] hover:bg-[#ebd55b] text-black px-6 py-3 rounded text-sm font-bold flex items-center gap-2 transition-colors disabled:opacity-50 disabled:cursor-not-allowed">
            <span class="material-symbols-outlined text-lg">lock</span>
            开箱 ${{ currentCrateCost }}
          </button>

          <button @click="startBatchUnbox(10)" :disabled="isSpinning"
            class="bg-[#333] hover:bg-[#444] text-white px-6 py-3 rounded text-sm font-bold transition-colors border border-white/5 disabled:opacity-50 disabled:cursor-not-allowed">
            打开 10 个
          </button>
          <button @click="startBatchUnbox(50)" :disabled="isSpinning"
            class="bg-[#333] hover:bg-[#444] text-white px-6 py-3 rounded text-sm font-bold transition-colors border border-white/5 disabled:opacity-50 disabled:cursor-not-allowed">
            打开 50 个
          </button>
          <button @click="startBatchUnbox(100)" :disabled="isSpinning"
            class="bg-[#333] hover:bg-[#444] text-white px-6 py-3 rounded text-sm font-bold transition-colors border border-white/5 disabled:opacity-50 disabled:cursor-not-allowed">
            打开 100 个
          </button>

          <div class="ml-auto flex items-center gap-4 text-xs text-gray-400">
            <div class="flex items-center gap-2 cursor-pointer hover:text-white transition-colors"
              @click="toggleAutoOpen">
              自动 <div
                :class="['w-8 h-4 rounded-full relative transition-colors', isAutoOpen ? 'bg-green-500' : 'bg-[#333] border border-gray-600']">
                <div
                  :class="['w-3 h-3 rounded-full absolute top-[1px] transition-all', isAutoOpen ? 'bg-white left-[18px]' : 'bg-gray-500 left-[2px]']">
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Drop Rates -->
        <div class="bg-[#232220] rounded-lg p-5 border border-white/5">
          <h4 class="font-bold text-sm text-white mb-4">掉落率</h4>
          <div class="flex flex-col gap-3">
            <div v-for="rate in rates" :key="rate.name" class="relative">
              <div class="flex justify-between text-[11px] mb-1 font-bold" :style="{ color: rate.color }">
                <span>{{ rate.name }}</span>
                <span class="text-gray-400 font-mono">{{ rate.percent }} %</span>
              </div>
              <div class="w-full bg-[#111] h-1.5 rounded-full overflow-hidden">
                <div class="h-full rounded-full" :style="{ width: rate.percent + '%', backgroundColor: rate.color }">
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Case Contents Grid -->
        <div class="bg-[#232220] rounded-lg p-5 border border-white/5">
          <h4 class="font-bold text-sm text-white mb-4">{{ selectedCrate.name }} 内容 <span
              class="text-gray-500 font-normal ml-2">{{ normalContents.length + rareContents.length }}件</span></h4>

          <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 xl:grid-cols-7 gap-3 mb-8">
            <div v-for="item in normalContents" :key="item.id"
              class="bg-[#1a1918] border border-white/5 rounded p-3 flex flex-col items-center text-center hover:bg-[#2a2928] transition-colors relative"
              :style="{ borderTopColor: item.rarity?.color, borderTopWidth: '2px' }">
              <img :src="item.image" class="w-full h-16 object-contain mb-3 drop-shadow-md" />
              <span class="text-[10px] text-gray-500 font-bold line-clamp-1 w-full">{{ item.name.split('|')[0] }}</span>
              <span class="text-[11px] font-bold line-clamp-1 w-full mt-0.5" :style="{ color: item.rarity?.color }">{{
                item.name.split('|')[1] || item.name }}</span>
              <span class="text-[10px] text-gray-500 font-mono mt-1 w-full">${{ getItemBasePrice(item.id, false) }}</span>
            </div>
          </div>

          <div v-if="rareContents.length > 0">
            <div class="flex items-center justify-center my-6 relative">
              <div class="absolute inset-0 flex items-center">
                <div class="w-full border-t border-white/10"></div>
              </div>
              <div class="relative bg-[#232220] px-4 font-bold text-sm flex items-center gap-2" style="color: #ffd700;">
                <span class="material-symbols-outlined text-[16px]">star</span> 特殊物品 (0.26%)
              </div>
            </div>

            <div class="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-6 xl:grid-cols-7 gap-3">
              <div v-for="item in rareContents" :key="item.id"
                class="bg-[#1a1918] border border-white/5 rounded p-3 flex flex-col items-center text-center hover:bg-[#2a2928] transition-colors relative"
                style="border-top-color: #ffd700; border-top-width: 2px;">
                <img :src="item.image" class="w-full h-16 object-contain mb-3 drop-shadow-md" />
                <span class="text-[10px] text-gray-500 font-bold line-clamp-1 w-full" style="color: #ffd700">{{
                  item.name.split('|')[0] }}</span>
                <span class="text-[11px] font-bold line-clamp-2 w-full mt-0.5" style="color: #ffd700">{{
                  item.name.split('|')[1] || item.name }}{{ item.phase ? ' (' + item.phase + ')' : '' }}</span>
                <span class="text-[10px] text-gray-500 font-mono mt-1 w-full">${{ getItemBasePrice(item.id, true) }}</span>
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>

    <!-- Batch Result Modal -->
    <div v-if="batchResults.length > 0"
      class="fixed inset-0 z-[100] bg-black/90 flex flex-col items-center justify-center p-8 backdrop-blur-md animate-[fadeIn_0.3s_ease-out]">
      <h2 class="text-2xl font-bold text-white mb-2">成功开箱 {{ batchResults.length }} 次</h2>
      <p class="text-gray-400 mb-6">总花费: ${{ (batchResults.length * currentCrateCost).toFixed(2) }} | 获得价值: <span
          class="text-green-500 font-bold">${{batchResults.reduce((sum, item) => sum + (item.price || 0), 0).toFixed(2)
          }}</span></p>

      <div
        class="w-full max-w-7xl max-h-[70vh] overflow-y-auto custom-scrollbar bg-[#232220] p-6 rounded-lg border border-white/10">
        <div class="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 xl:grid-cols-10 gap-3">
          <div v-for="item in batchResults" :key="item.id"
            class="bg-[#1a1918] border border-white/5 rounded p-3 flex flex-col items-center text-center relative hover:bg-[#2a2928] transition-colors"
            :style="{ borderTopColor: item.rarityColor, borderTopWidth: '2px' }">
            <img :src="item.image" class="w-full h-12 object-contain mb-2 drop-shadow-md" />
            <span class="text-[9px] text-gray-500 font-bold line-clamp-1 w-full">{{ item.itemName.split('|')[0]
              }}</span>
            <span class="text-[10px] font-bold line-clamp-1 w-full mt-0.5" :style="{ color: item.rarityColor }">{{
              item.itemName.split('|')[1] || item.itemName }}{{ item.phase ? ' (' + item.phase + ')' : '' }}</span>
            <span class="text-[9px] text-gray-500 font-mono mt-1 w-full"><span v-if="item.isStatTrak"
                class="text-orange-500 mr-1">ST</span>${{ (item.price || 0).toFixed(2) }}</span>
          </div>
        </div>
      </div>

      <button @click="batchResults = []"
        class="mt-8 bg-[#d4b024] text-black px-12 py-3 rounded font-bold hover:bg-[#ebd55b] transition-colors shadow-lg">
        确认并关闭
      </button>
    </div>

  </div>
</template>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
  width: 6px;
}

.custom-scrollbar::-webkit-scrollbar-track {
  background: rgba(255, 255, 255, 0.02);
}

.custom-scrollbar::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 4px;
}

.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.2);
}

@keyframes fadeIn {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}
</style>
