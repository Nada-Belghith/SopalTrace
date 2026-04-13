<template>
  <div class="forbidden-view">
    <div class="forbidden-container">
      <div class="forbidden-icon">
        <i class="pi pi-lock"></i>
      </div>
      <h1>Accès Refusé</h1>
      <p class="forbidden-message">
        Vous n'avez pas les permissions requises pour accéder à cette ressource.
      </p>
      <div class="forbidden-code">403 - Forbidden</div>
      <div class="forbidden-actions">
        <Button 
          @click="goToDashboard" 
          class="p-button-primary"
          label="Retour au tableau de bord"
        />
        <Button 
          @click="logout" 
          class="p-button-secondary"
          label="Se déconnecter"
        />
      </div>
    </div>
  </div>
</template>

<script setup>
import { useRouter } from 'vue-router'
import Button from 'primevue/button'

const router = useRouter()

const goToDashboard = () => {
  router.push('/dashboard')
}

const logout = () => {
  router.push('/')
}
</script>

<style scoped>
.forbidden-view {
  width: 100%;
  height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
}

.forbidden-container {
  background: white;
  padding: 3rem;
  border-radius: 1rem;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
  text-align: center;
  max-width: 400px;
}

.forbidden-icon {
  font-size: 4rem;
  color: #ef4444;
  margin-bottom: 1rem;
}

.forbidden-container h1 {
  font-size: 2rem;
  color: #333;
  margin-bottom: 1rem;
}

.forbidden-message {
  color: #666;
  font-size: 1rem;
  margin-bottom: 2rem;
}

.forbidden-code {
  font-size: 0.9rem;
  color: #999;
  margin-bottom: 2rem;
  font-family: monospace;
  background: #f5f5f5;
  padding: 0.5rem;
  border-radius: 0.5rem;
}

.forbidden-actions {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

:deep(.p-button) {
  width: 100%;
}
</style>