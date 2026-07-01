import https from 'https';
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const DATA_DIR = path.join(__dirname, '../public/data');
const CRATES_CN_PATH = path.join(DATA_DIR, 'crates.json');
const PRICES_OUT_PATH = path.join(DATA_DIR, 'prices.json');

async function fetchJson(url) {
  const res = await fetch(url, { headers: { 'User-Agent': 'Mozilla/5.0' } });
  if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);
  return await res.json();
}

async function updatePrices() {
  try {
    console.log('Fetching English crates.json...');
    const cratesEn = await fetchJson('https://raw.githubusercontent.com/ByMykel/CSGO-API/main/public/api/en/crates.json');
    
    console.log('Fetching Jonese1234 price data...');
    const joneseData = await fetchJson('https://raw.githubusercontent.com/jonese1234/Csgo-Case-Data/master/latest.json');

    console.log('Loading local Chinese crates.json...');
    const cratesCn = JSON.parse(fs.readFileSync(CRATES_CN_PATH, 'utf-8'));

    // Create a flat map of English skin names to prices from Jonese1234
    // { "AK-47 | Redline": { FN: 10, MW: 5 ... } }
    const priceDb = {};
    joneseData.Cases.forEach(c => {
      const steamMarket = c.MarketPlaces?.find(m => m.Name === 'Steam');
      if (steamMarket && steamMarket.Skins) {
        steamMarket.Skins.forEach(s => {
          priceDb[s.Name] = s.Price;
        });
      }
    });

    const finalPrices = {}; // Map of skin id to prices (e.g. { "skin-123": { FN: 10, STFN: 20 ... } })

    cratesEn.forEach(crateEn => {
      const allItemsEn = [...(crateEn.contains || []), ...(crateEn.contains_rare || [])];
      
      allItemsEn.forEach(itemEn => {
        const enName = itemEn.name; // e.g. "★ Bowie Knife | Gamma Doppler"
        
        let p = priceDb[enName];
        if (!p) {
          // Knives often don't have the star in jonese1234 DB
          p = priceDb[enName.replace('★ ', '')];
        }
        if (!p) {
          // Try without any stars at all
          p = priceDb[enName.replace(/★ /g, '').replace(/（★）/g, '')];
        }
        
        if (p) {
          finalPrices[itemEn.id] = {
             "崭新出厂": { price: p.FN || 0, st: p.STFN || 0 },
             "略有磨损": { price: p.MW || 0, st: p.STMW || 0 },
             "久经沙场": { price: p.FT || 0, st: p.STFT || 0 },
             "破损不堪": { price: p.WW || 0, st: p.STWW || 0 },
             "战痕累累": { price: p.BS || 0, st: p.STBS || 0 }
          };
        }
      });
    });

    const finalCratePrices = {};
    cratesEn.forEach(crateEn => {
      const joneseCrate = joneseData.Cases.find(c => c.Name === crateEn.name);
      if (joneseCrate) {
        finalCratePrices[crateEn.id] = {
          cost: joneseCrate.Cost || 0,
          keyCost: joneseCrate.KeyCost || 2.49
        };
      } else {
        // Default if not found
        finalCratePrices[crateEn.id] = { cost: 0.1, keyCost: 2.49 };
      }
    });

    fs.writeFileSync(PRICES_OUT_PATH, JSON.stringify(finalPrices));
    console.log(`Saved mapped prices for ${Object.keys(finalPrices).length} skins to ${PRICES_OUT_PATH}`);
    
    const CRATE_PRICES_OUT_PATH = path.join(DATA_DIR, 'cratePrices.json');
    fs.writeFileSync(CRATE_PRICES_OUT_PATH, JSON.stringify(finalCratePrices));
    console.log(`Saved crate prices to ${CRATE_PRICES_OUT_PATH}`);

  } catch (err) {
    console.error('Error updating prices:', err);
  }
}

updatePrices();
