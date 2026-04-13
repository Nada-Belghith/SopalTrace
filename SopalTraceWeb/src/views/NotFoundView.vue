<template>
  <div class="not-found-view">
    <div class="not-found-container">
      <div class="not-found-icon">
        <i class="pi pi-times"></i>
      </div>
      <h1>Page Non Trouvée</h1>
      <p class="not-found-message">
        Désolé, la page que vous recherchez n'existe pas.
      </p>
      <div class="not-found-code">404 - Not Found</div>
      <div class="not-found-actions">
        <Button 
          @click="goHome" 
          class="p-button-primary"
          label="Aller à l'accueil"
        />
        <Button 
          @click="goBack" 
          class="p-button-secondary"
          label="Retour"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import Button from 'primevue/button'

const router = useRouter()

const goHome = () => {
  router.push('/dashboard')
}

const goBack = () => {
  router.back()
}
</script>

<style scoped>
.not-found-view {
  width: 100%;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.not-found-container {
  background: white;
  padding: 3rem;
  border-radius: 1rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  text-align: center;
  max-width: 400px;
}

.not-found-icon {
  font-size: 4rem;
  color: #fbbf24;
  margin-bottom: 1rem;
}

.not-found-container h1 {
  font-size: 2rem;
  color: #333;
  margin-bottom: 1rem;
}

.not-found-message {
  color: #666;
  font-size: 1rem;
  margin-bottom: 2rem;
}

.not-found-code {
  font-size: 0.9rem;
  color: #999;
  margin-bottom: 2rem;
  font-family: monospace;
  background: #f5f5f5;
  padding: 0.5rem;
  border-radius: 0.5rem;
}

.not-found-actions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

:deep(.p-button) {
  width: 100%;
}
</style>