<template>
  <aside class="sidebar" :class="{ 'sidebar-collapsed': isCollapsed }">
    <div class="sidebar-header">
      <div class="logo">
        <i class="pi pi-chart-line logo-icon"></i>
        <span v-show="!isCollapsed" class="logo-text">SopalTrace</span>
      </div>
      <button class="toggle-btn" @click="toggleSidebar">
        <i :class="isCollapsed ? 'pi pi-angle-right' : 'pi pi-angle-left'"></i>
      </button>
    </div>

    <nav class="sidebar-menu">
      <RouterLink 
        v-for="item in menuItems" 
        :key="item.path"
        :to="item.path"
        class="menu-item"
        :class="{ active: isActive(item.path) }"
      >
        <i :class="`pi ${item.icon}`"></i>
        <span v-show="!isCollapsed" class="menu-label">{{ item.label }}</span>
        <span v-if="!isCollapsed && item.badge" class="badge">{{ item.badge }}</span>
      </RouterLink>
    </nav>

    <div class="sidebar-footer">
      <button class="user-profile" @click="showUserMenu = !showUserMenu">
        <Avatar 
          icon="pi pi-user" 
          class="user-avatar"
          shape="circle"
        />
        <div v-show="!isCollapsed" class="user-info">
          <p class="user-name">{{ userName }}</p>
          <p class="user-role">Administrateur</p>
        </div>
      </button>

      <ContextMenu 
        v-if="showUserMenu && !isCollapsed"
        :model="userMenu"
        @hide="showUserMenu = false"
      />
    </div>
  </aside>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import Avatar from 'primevue/avatar'
import ContextMenu from 'primevue/contextmenu'

const router = useRouter()
const isCollapsed = ref(false)
const showUserMenu = ref(false)

const userName = ref('Jean Dupont')

const menuItems = [
  { path: '/dashboard', label: 'Tableau de bord', icon: 'pi-home', badge: null },
  { path: '/traces', label: 'Traces', icon: 'pi-list', badge: '5' },
  { path: '/reports', label: 'Rapports', icon: 'pi-file', badge: null },
  { path: '/analytics', label: 'Analytiques', icon: 'pi-chart-bar', badge: null },
  { path: '/settings', label: 'Paramètres', icon: 'pi-cog', badge: null },
  { path: '/documentation', label: 'Documentation', icon: 'pi-book', badge: null }
]

const userMenu = [
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
    command: () => router.push('/')
  }
]

const toggleSidebar = () => {
  isCollapsed.value = !isCollapsed.value
}

const isActive = (path) => {
  return router.currentRoute.value.path === path
}

const logout = () => {
  router.push('/')
}
</script>

<style scoped>
.sidebar {
  width: 280px;
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 1rem;
  height: 100vh;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  transition: width 0.3s ease;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
}

.sidebar-collapsed {
  width: 80px;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.2);
  margin-bottom: 1rem;
}

.logo {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-weight: 700;
  font-size: 1.25rem;
}

.logo-icon {
  font-size: 1.5rem;
}

.logo-text {
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.toggle-btn {
  background: rgba(255, 255, 255, 0.2);
  border: none;
  color: white;
  width: 32px;
  height: 32px;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.3s ease;
}

.toggle-btn:hover {
  background: rgba(255, 255, 255, 0.3);
}

.sidebar-menu {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.menu-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  color: rgba(255, 255, 255, 0.8);
  text-decoration: none;
  transition: all 0.3s ease;
  cursor: pointer;
  white-space: nowrap;
  position: relative;
}

.menu-item:hover {
  background: rgba(255, 255, 255, 0.2);
  color: white;
}

.menu-item.active {
  background: rgba(255, 255, 255, 0.25);
  color: white;
  font-weight: 600;
  border-left: 3px solid white;
  padding-left: calc(1rem - 3px);
}

.menu-item i {
  font-size: 1.2rem;
  min-width: 1.5rem;
}

.menu-label {
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
}

.badge {
  background: #fbbf24;
  color: #92400e;
  font-size: 0.75rem;
  padding: 0.25rem 0.5rem;
  border-radius: 9999px;
  font-weight: 700;
  margin-left: auto;
}

.sidebar-footer {
  padding-top: 1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.2);
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: rgba(255, 255, 255, 0.1);
  border: none;
  color: white;
  padding: 0.75rem;
  border-radius: 0.5rem;
  cursor: pointer;
  width: 100%;
  transition: background 0.3s ease;
}

.user-profile:hover {
  background: rgba(255, 255, 255, 0.2);
}

.user-info {
  text-align: left;
  flex: 1;
}

.user-name {
  margin: 0;
  font-weight: 600;
  font-size: 0.95rem;
}

.user-role {
  margin: 0;
  font-size: 0.8rem;
  opacity: 0.8;
}

:deep(.p-avatar) {
  width: 2.5rem;
  height: 2.5rem;
}

::-webkit-scrollbar {
  width: 6px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.2);
  border-radius: 3px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.3);
}
</style>