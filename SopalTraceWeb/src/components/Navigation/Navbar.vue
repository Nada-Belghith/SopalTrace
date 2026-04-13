<template>
  <header class="navbar">
    <div class="navbar-container">
      <div class="navbar-brand">
        <button class="menu-toggle" @click="toggleMenu">
          <i class="pi pi-bars"></i>
        </button>
        <div class="breadcrumb">
          <Breadcrumb :model="breadcrumbs" class="custom-breadcrumb" />
        </div>
      </div>

      <div class="navbar-content">
        <div class="search-bar">
          <i class="pi pi-search search-icon"></i>
          <InputText
            v-model="searchQuery"
            type="text"
            placeholder="Rechercher..."
            class="search-input"
          />
        </div>

        <div class="navbar-actions">
          <!-- Notifications -->
          <button class="action-btn" @click="toggleNotifications">
            <i class="pi pi-bell"></i>
            <span v-if="notificationCount > 0" class="notification-badge">
              {{ notificationCount }}
            </span>
          </button>

          <!-- Messages -->
          <button class="action-btn" @click="toggleMessages">
            <i class="pi pi-envelope"></i>
            <span v-if="messageCount > 0" class="notification-badge">
              {{ messageCount }}
            </span>
          </button>

          <!-- Language Switcher -->
          <Dropdown
            v-model="selectedLanguage"
            :options="languages"
            option-label="name"
            option-value="code"
            class="language-dropdown"
            @change="changeLanguage"
          >
            <template #value="slotProps">
              <div v-if="slotProps.value" class="flex align-items-center">
                <span>{{ slotProps.value }}</span>
              </div>
            </template>
          </Dropdown>

          <!-- User Menu -->
          <Menu 
            :model="userMenuItems" 
            :popup="true" 
            ref="userMenu"
          >
            <template #start>
              <button class="user-btn" @click="toggleUserMenu">
                <Avatar 
                  icon="pi pi-user" 
                  shape="circle"
                  class="user-avatar"
                />
              </button>
            </template>
          </Menu>
        </div>
      </div>
    </div>

    <!-- Notifications Panel -->
    <Panel
      v-if="showNotifications"
      class="notifications-panel"
      header="Notifications"
      :toggleable="true"
      :closable="true"
      @hide="showNotifications = false"
    >
      <div v-if="notifications.length > 0" class="notifications-list">
        <div
          v-for="notification in notifications"
          :key="notification.id"
          class="notification-item"
          :class="{ unread: !notification.read }"
        >
          <i :class="`pi ${notification.icon}`"></i>
          <div class="notification-content">
            <p class="notification-title">{{ notification.title }}</p>
            <p class="notification-message">{{ notification.message }}</p>
            <time class="notification-time">{{ notification.time }}</time>
          </div>
        </div>
      </div>
      <div v-else class="empty-state">
        <p>Aucune notification</p>
      </div>
    </Panel>
  </header>
</template>

<script setup>
import { ref } from 'vue'
import Breadcrumb from 'primevue/breadcrumb'
import Dropdown from 'primevue/dropdown'
import Menu from 'primevue/menu'
import Panel from 'primevue/panel'
import Avatar from 'primevue/avatar'
import InputText from 'primevue/inputtext'
import { useRouter } from 'vue-router'

const router = useRouter()

const searchQuery = ref('')
const selectedLanguage = ref('fr')
const showNotifications = ref(false)
const userMenu = ref(null)
const notificationCount = ref(3)
const messageCount = ref(2)

const languages = [
  { name: 'Français', code: 'fr' },
  { name: 'English', code: 'en' },
  { name: 'Español', code: 'es' },
  { name: 'Deutsch', code: 'de' }
]

const breadcrumbs = ref([
  { label: 'Accueil', route: '/dashboard' },
  { label: 'Tableau de bord' }
])

const notifications = ref([
  {
    id: 1,
    title: 'Nouvelle trace',
    message: 'Une nouvelle trace a été enregistrée',
    time: 'il y a 5 min',
    icon: 'pi-check-circle',
    read: false
  },
  {
    id: 2,
    title: 'Erreur de synchronisation',
    message: 'Impossible de synchroniser avec le serveur',
    time: 'il y a 15 min',
    icon: 'pi-exclamation-circle',
    read: false
  },
  {
    id: 3,
    title: 'Rapport généré',
    message: 'Votre rapport mensuel est prêt',
    time: 'il y a 1 heure',
    icon: 'pi-file',
    read: true
  }
])

const userMenuItems = [
  {
    label: 'Profil',
    icon: 'pi pi-user',
    command: () => router.push('/profile')
  },
  {
    label: 'Paramètres',
    icon: 'pi pi-cog',
    command: () => router.push('/settings')
  },
  { separator: true },
  {
    label: 'Déconnexion',
    icon: 'pi pi-sign-out',
    command: () => {
      router.push('/')
    }
  }
]

const toggleMenu = () => {
  console.log('Toggle menu')
}

const toggleNotifications = () => {
  showNotifications.value = !showNotifications.value
}

const toggleMessages = () => {
  console.log('Toggle messages')
}

const changeLanguage = (event) => {
  console.log('Language changed to:', event.value)
}

const toggleUserMenu = (event) => {
  userMenu.value.toggle(event)
}
</script>

<style scoped>
.navbar {
  background: white;
  border-bottom: 1px solid #e5e7eb;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
  position: sticky;
  top: 0;
  z-index: 1000;
}

.navbar-container {
  padding: 1rem 2rem;
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 2rem;
}

.navbar-brand {
  display: flex;
  align-items: center;
  gap: 1rem;
  flex: 1;
}

.menu-toggle {
  background: none;
  border: none;
  font-size: 1.5rem;
  color: #666;
  cursor: pointer;
  display: none;
  transition: color 0.3s ease;
}

.menu-toggle:hover {
  color: #333;
}

.breadcrumb {
  flex: 1;
}

.navbar-content {
  display: flex;
  align-items: center;
  gap: 2rem;
  flex: 1;
}

.search-bar {
  position: relative;
  display: flex;
  align-items: center;
  background: #f5f5f5;
  border-radius: 0.5rem;
  padding: 0 0.75rem;
  flex: 1;
  max-width: 300px;
}

.search-icon {
  color: #999;
  margin-right: 0.5rem;
}

.search-input {
  border: none;
  background: transparent;
  outline: none;
  flex: 1;
  padding: 0.5rem 0;
}

.navbar-actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.action-btn {
  background: none;
  border: none;
  font-size: 1.25rem;
  color: #666;
  cursor: pointer;
  position: relative;
  transition: color 0.3s ease;
}

.action-btn:hover {
  color: #333;
}

.notification-badge {
  position: absolute;
  top: -5px;
  right: -5px;
  background: #ef4444;
  color: white;
  font-size: 0.7rem;
  padding: 0.25rem 0.5rem;
  border-radius: 9999px;
  font-weight: 700;
}

.language-dropdown {
  width: 100px;
}

.user-btn {
  background: none;
  border: none;
  cursor: pointer;
  display: flex;
  align-items: center;
}

.notifications-panel {
  position: absolute;
  right: 2rem;
  top: 4rem;
  width: 350px;
  max-height: 400px;
  overflow-y: auto;
  z-index: 1001;
}

.notifications-list {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.notification-item {
  display: flex;
  gap: 1rem;
  padding: 1rem;
  background: #f9fafb;
  border-radius: 0.5rem;
  border-left: 3px solid #3b82f6;
  cursor: pointer;
  transition: background 0.3s ease;
}

.notification-item:hover {
  background: #f0f4f8;
}

.notification-item.unread {
  background: #eff6ff;
  border-left-color: #dc2626;
}

.notification-item i {
  font-size: 1.5rem;
  color: #3b82f6;
  min-width: 1.5rem;
}

.notification-content {
  flex: 1;
}

.notification-title {
  margin: 0;
  font-weight: 600;
  color: #333;
  font-size: 0.95rem;
}

.notification-message {
  margin: 0.25rem 0;
  color: #666;
  font-size: 0.9rem;
}

.notification-time {
  font-size: 0.8rem;
  color: #999;
}

.empty-state {
  text-align: center;
  padding: 2rem 1rem;
  color: #999;
}

:deep(.p-breadcrumb) {
  background: transparent;
  padding: 0;
}

:deep(.p-datatable-scrollable-header) {
  border: none;
}

@media (max-width: 768px) {
  .menu-toggle {
    display: block;
  }

  .navbar-container {
    flex-wrap: wrap;
  }

  .search-bar {
    flex: 1;
    min-width: 150px;
  }

  .notifications-panel {
    width: calc(100vw - 2rem);
    max-width: 350px;
  }
}
</style>