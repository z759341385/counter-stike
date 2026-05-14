import { createI18n } from 'vue-i18n'
import zh from './zh'
import en from './en'

const i18n = createI18n({
  legacy: false, // 使用 Composition API 模式
  locale: navigator.language.startsWith('zh') ? 'zh' : 'en', // 自动识别
  fallbackLocale: 'en',
  messages: {
    zh,
    en
  }
})

export default i18n
