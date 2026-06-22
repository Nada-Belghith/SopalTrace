import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    vueDevTools(),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    },
  },
  // Force Vite to always re-bundle deps on start — prevents "Outdated Optimize Dep" (504)
  // errors that occur after Docker container rebuilds invalidate the chunk hashes.
  optimizeDeps: {
    force: true,
  },
  server: {
    host: true,
    allowedHosts: ['.ngrok-free.app'],
    // Automatically reload the page when a stale/missing chunk is detected
    // instead of showing a hard error in the browser console.
    hmr: {
      overlay: true,
    },
    proxy: {
      '/api': {
        target: 'http://sopaltrace.api:8080',
        changeOrigin: true,
        secure: false,
      }
    }
  },
})
