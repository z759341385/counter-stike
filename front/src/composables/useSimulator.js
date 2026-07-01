import { ref } from 'vue'

export const crates = ref([])
export const skins = ref(null) // map skin id to skin details
export const prices = ref(null) // map skin id to price data
export const cratePrices = ref(null) // map crate id to { cost, keyCost }
export const inventory = ref([])
export const isLoading = ref(true)

const ODDS = {
  blue: { chance: 0.7992, color: '#4b69ff', rarity: '军规级' },
  purple: { chance: 0.1598, color: '#8847ff', rarity: '受限' },
  pink: { chance: 0.0320, color: '#d32ce6', rarity: '保密' },
  red: { chance: 0.0064, color: '#eb4b4b', rarity: '隐秘' },
  gold: { chance: 0.0026, color: '#ffd700', rarity: '罕见特殊物品' }
}

const WEARS = [
  { name: '崭新出厂', min: 0, max: 0.07 },
  { name: '略有磨损', min: 0.07, max: 0.15 },
  { name: '久经沙场', min: 0.15, max: 0.38 },
  { name: '破损不堪', min: 0.38, max: 0.45 },
  { name: '战痕累累', min: 0.45, max: 1.00 }
]

export async function loadSimulatorData() {
  if (crates.value.length > 0) return

  try {
    isLoading.value = true
    const [cratesRes, skinsRes, pricesRes, cratePricesRes] = await Promise.all([
      fetch('/data/crates.json'),
      fetch('/data/skins.json'),
      fetch('/data/prices.json').catch(() => null),
      fetch('/data/cratePrices.json').catch(() => null)
    ])
    
    const cratesData = await cratesRes.json()
    const skinsData = await skinsRes.json()
    
    if (pricesRes && pricesRes.ok) {
      prices.value = await pricesRes.json()
    } else {
      prices.value = {}
    }

    if (cratePricesRes && cratePricesRes.ok) {
      cratePrices.value = await cratePricesRes.json()
    } else {
      cratePrices.value = {}
    }

    // Filter only weapon cases
    crates.value = cratesData.filter(c => c.name && c.name.includes('武器箱') && c.contains && c.contains.length > 0)
    
    // Map skins for quick lookup
    skins.value = {}
    skinsData.forEach(s => {
      skins.value[s.id] = s
    })

    // Load inventory
    const saved = localStorage.getItem('cs_simulator_inventory')
    if (saved) {
      inventory.value = JSON.parse(saved)
    }
  } catch (e) {
    console.error('Failed to load simulator data', e)
  } finally {
    isLoading.value = false
  }
}

export function saveInventory() {
  localStorage.setItem('cs_simulator_inventory', JSON.stringify(inventory.value))
}

export function clearInventory() {
  inventory.value = []
  saveInventory()
}

export function addInventoryItem(item) {
  inventory.value.unshift(item)
  saveInventory()
}

export function unboxCrate(crate) {
  const roll = Math.random()
  let isRare = false
  let selectedTier = ''

  if (roll < ODDS.gold.chance && crate.contains_rare && crate.contains_rare.length > 0) {
    selectedTier = ODDS.gold.color
    isRare = true
  } else if (roll < ODDS.gold.chance + ODDS.red.chance) {
    selectedTier = ODDS.red.color
  } else if (roll < ODDS.gold.chance + ODDS.red.chance + ODDS.pink.chance) {
    selectedTier = ODDS.pink.color
  } else if (roll < ODDS.gold.chance + ODDS.red.chance + ODDS.pink.chance + ODDS.purple.chance) {
    selectedTier = ODDS.purple.color
  } else {
    selectedTier = ODDS.blue.color
  }

  // Filter items in crate by selected tier
  let possibleItems = []
  if (isRare) {
    possibleItems = crate.contains_rare
  } else {
    possibleItems = crate.contains.filter(item => item.rarity && item.rarity.color === selectedTier)
  }

  // Fallback if case doesn't have this tier (e.g., Huntsman case has no blues, wait actually some cases might miss a tier)
  if (possibleItems.length === 0) {
    if (isRare) {
       possibleItems = crate.contains // Super rare fallback
    } else {
       // fallback to highest available that is lower than intended, or just any
       possibleItems = crate.contains.filter(item => item.rarity && item.rarity.color === ODDS.blue.color)
       if (possibleItems.length === 0) possibleItems = crate.contains
    }
  }

  const selectedItem = possibleItems[Math.floor(Math.random() * possibleItems.length)]
  
  // Get full skin details to determine float bounds
  const skinDetails = skins.value[selectedItem.id] || {}
  
  // Generate Float
  const minF = skinDetails.min_float !== undefined ? skinDetails.min_float : 0
  const maxF = skinDetails.max_float !== undefined ? skinDetails.max_float : 1
  const floatValue = minF + Math.random() * (maxF - minF)

  // Determine Wear
  let wearName = '未知'
  for (let w of WEARS) {
    if (floatValue >= w.min && floatValue <= w.max) {
      wearName = w.name
      break
    }
  }

  // Generate StatTrak (10% chance)
  const isStatTrak = Math.random() < 0.1
    
  // Get real price
  let itemPrice = 0;
  if (prices.value && prices.value[selectedItem.id]) {
    const wearPrices = prices.value[selectedItem.id][wearName] || prices.value[selectedItem.id]['Vanilla'];
    if (wearPrices) {
      itemPrice = isStatTrak ? (wearPrices.st || 0) : (wearPrices.price || 0);
    }
    // Fallback to any valid wear if current wear has 0 price (due to no market listings)
    if (itemPrice === 0) {
      const anyWear = Object.values(prices.value[selectedItem.id]).find(w => w && (isStatTrak ? w.st : w.price) > 0);
      if (anyWear) itemPrice = isStatTrak ? anyWear.st : anyWear.price;
    }
  }

  // Fallback for completely missing items (like Vanilla knives)
  if (itemPrice === 0) {
    if (isRare) {
       itemPrice = isStatTrak ? 550.00 : 380.00; // Reasonable fallback for rare missing items
    } else {
       itemPrice = 0.50; // Fallback for normal missing items
    }
  }

  // Calculate unbox cost
  let unboxCost = 2.59;
  if (cratePrices.value && cratePrices.value[crate.id]) {
    unboxCost = (cratePrices.value[crate.id].cost || 0) + (cratePrices.value[crate.id].keyCost || 2.49);
  }

  const result = {
    id: Date.now() + Math.random().toString(36).substring(2),
    crateName: crate.name,
    itemName: selectedItem.name,
    phase: selectedItem.phase,
    image: selectedItem.image,
    rarityColor: isRare ? '#ffd700' : selectedTier,
    floatValue: floatValue,
    wear: wearName,
    isStatTrak: isStatTrak,
    price: itemPrice,
    cost: unboxCost,
    timestamp: Date.now()
  }

  return result
}
