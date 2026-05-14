<script setup>
import { onMounted, ref } from 'vue'
import { allRules, fetchAllRules, saveRule, removeRule, currentView } from '../composables/useGame'

const editingRule = ref(null)
const showModal = ref(false)
const showDeleteModal = ref(false)
const deleteId = ref(null)

onMounted(() => {
  fetchAllRules()
})

function openAdd() {
  editingRule.value = { name: '', description: '', weight: 5, is_active: 1 }
  showModal.value = true
}

function openEdit(rule) {
  editingRule.value = { ...rule }
  showModal.value = true
}

function handleSave() {
  if (!editingRule.value.name || !editingRule.value.description) return
  saveRule(editingRule.value)
  showModal.value = false
}

function handleDelete(id) {
  deleteId.value = id
  showDeleteModal.value = true
}

function confirmDelete() {
  if (deleteId.value) {
    removeRule(deleteId.value)
    showDeleteModal.value = false
    deleteId.value = null
  }
}

function handleBack() {
  currentView.value = 'home'
}
</script>

<template>
  <div class="min-h-screen bg-black text-text-primary p-8 md:p-16 flex flex-col items-center dot-pattern">
    <div class="w-full max-w-6xl relative z-10">
      <header class="flex justify-between items-center mb-12 border-b border-white/10 pb-6">
        <div class="flex items-center gap-4">
          <button @click="handleBack" class="material-symbols-outlined text-primary hover:scale-110 transition-transform">arrow_back</button>
          <h1 class="font-display text-3xl font-black tracking-tighter uppercase">Rules Management</h1>
        </div>
        <button @click="openAdd" class="bg-primary text-on-primary px-6 py-2 chamfer-clip font-bold uppercase text-sm hover:brightness-110 transition-all flex items-center gap-2">
          <span class="material-symbols-outlined text-sm">add</span> Add Rule
        </button>
      </header>

      <div class="grid grid-cols-1 gap-4">
        <div v-for="rule in allRules" :key="rule.id" 
          class="bg-white/5 border border-white/10 p-6 chamfer-clip flex flex-col md:flex-row justify-between items-start md:items-center gap-6 hover:border-primary/30 transition-all group">
          <div class="flex-grow">
            <div class="flex items-center gap-3 mb-2">
              <h3 class="text-xl font-bold text-primary uppercase tracking-tight">{{ rule.name }}</h3>
              <span v-if="!rule.is_active" class="px-2 py-0.5 bg-red-500/20 text-red-500 text-[10px] font-mono border border-red-500/30 uppercase">Disabled</span>
              <span class="font-mono text-[10px] text-outline px-2 py-0.5 border border-white/10">W: {{ rule.weight }}</span>
            </div>
            <p class="text-sm text-text-secondary max-w-2xl">{{ rule.description }}</p>
          </div>
          <div class="flex gap-4 opacity-0 group-hover:opacity-100 transition-opacity">
            <button @click="openEdit(rule)" class="p-2 border border-white/10 chamfer-clip-sm hover:border-primary/50 text-outline hover:text-primary transition-all">
              <span class="material-symbols-outlined text-xl">edit</span>
            </button>
            <button @click="handleDelete(rule.id)" class="p-2 border border-white/10 chamfer-clip-sm hover:border-red-500/50 text-outline hover:text-red-500 transition-all">
              <span class="material-symbols-outlined text-xl">delete</span>
            </button>
          </div>
        </div>
      </div>

      <div v-if="allRules.length === 0" class="text-center py-20 opacity-20 font-mono tracking-widest uppercase">
        No Rules Found
      </div>
    </div>

    <!-- Edit Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal-content w-full max-lg p-8 bg-surface-container border border-white/10 chamfer-clip animate-[slideUp_0.3s_ease-out]">
        <h2 class="font-display text-2xl font-bold text-primary mb-8 uppercase">{{ editingRule.id ? 'Edit Rule' : 'New Rule' }}</h2>
        
        <div class="grid grid-cols-1 md:grid-cols-2 gap-8 mb-8">
          <div class="space-y-6">
            <div>
              <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">Rule Name</label>
              <input v-model="editingRule.name" class="input-field chamfer-clip-sm" placeholder="e.g. SNIPER ONLY" />
            </div>
            <div>
              <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">Weight (1-10)</label>
              <input v-model.number="editingRule.weight" type="number" min="1" max="10" class="input-field chamfer-clip-sm" />
            </div>
            <div class="flex items-center gap-3">
              <input type="checkbox" v-model="editingRule.is_active" :true-value="1" :false-value="0" id="isActive" class="accent-primary" />
              <label for="isActive" class="font-mono text-[10px] text-outline uppercase tracking-widest cursor-pointer">Active in Roulette</label>
            </div>
          </div>
          <div>
            <label class="block font-mono text-[10px] text-outline uppercase tracking-widest mb-2">Description</label>
            <textarea v-model="editingRule.description" class="input-field chamfer-clip-sm h-[180px] resize-none py-3" placeholder="Explain the rule in detail..."></textarea>
          </div>
        </div>

        <div class="flex gap-4">
          <button @click="handleSave" class="flex-1 btn-base bg-primary text-on-primary chamfer-clip py-4 font-bold uppercase tracking-widest hover:brightness-110">Save Configuration</button>
          <button @click="showModal = false" class="px-8 border border-white/10 text-outline chamfer-clip uppercase text-sm hover:bg-white/5">Cancel</button>
        </div>
      </div>
    </div>

    <!-- Delete Confirm Modal -->
    <div v-if="showDeleteModal" class="modal-overlay" @click.self="showDeleteModal = false">
      <div class="modal-content w-full max-xs p-8 bg-surface-container border border-red-500/30 chamfer-clip animate-[slideUp_0.2s_ease-out] text-center">
        <div class="w-16 h-16 bg-red-500/20 border border-red-500/30 rounded-full flex items-center justify-center mx-auto mb-6">
          <span class="material-symbols-outlined text-red-500 text-3xl">warning</span>
        </div>
        <h3 class="text-xl font-bold text-text-primary mb-2 uppercase">Confirm Deletion</h3>
        <p class="text-sm text-text-secondary mb-8">此操作将永久移除该规则，且不可撤销。你确定要继续吗？</p>
        <div class="flex flex-col gap-3">
          <button @click="confirmDelete" class="w-full py-4 bg-red-500 text-white font-bold uppercase tracking-widest chamfer-clip hover:bg-red-600 transition-all">Confirm Delete</button>
          <button @click="showDeleteModal = false" class="w-full py-3 text-outline uppercase font-mono text-xs hover:text-text-primary transition-all">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0,0,0,0.8);
  backdrop-blur: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100;
}
.max-lg { max-width: 800px; }
.max-xs { max-width: 360px; }
input-field { background: rgba(255,255,255,0.03); }
</style>
