import { createI18n } from 'vue-i18n'
import zh from './zh'
import en from './en'

const i18n = createI18n({
  legacy: false, // 使用 Composition API 模式
  locale: 'zh', // 强制使用中文
  fallbackLocale: 'zh',
  messages: {
    zh,
    en
  }
})

export default i18n
