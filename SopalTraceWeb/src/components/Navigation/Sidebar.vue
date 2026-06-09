<template>
  <aside class="sidebar" :class="{ 'sidebar-collapsed': isCollapsed }">
    <div class="sidebar-header">
      <div class="logo">
        <i class="pi pi-shield logo-icon text-emerald-400"></i>
        <span v-show="!isCollapsed" class="logo-text">SOPAL TRACE</span>
      </div>
      <button class="toggle-btn" @click="toggleSidebar">
        <i :class="isCollapsed ? 'pi pi-angle-right' : 'pi pi-angle-left'"></i>
      </button>
    </div>

    <nav class="sidebar-menu">
      
      <!-- MENU POUR RESPONSABLE QUALITE ET ADMIN -->
      <template v-if="hasAdvancedAccess">
        
        <!-- CATEGORIE: DOCUMENTS ET MODELES -->
        <div class="menu-category" v-if="isSuperviseurOrAdmin || isResponsableOrAdmin">
          <div v-show="!isCollapsed" class="category-header">
            DOCUMENTS ET MODELES
          </div>

          <div class="menu-item-wrapper" v-if="isSuperviseurOrAdmin">
            <div class="menu-group">
              <div class="menu-item-group-title">
                <i class="pi pi-sitemap text-indigo-400"></i>
                <span v-show="!isCollapsed" class="menu-label">Plans Generiques</span>
              </div>
              <div v-show="!isCollapsed" class="submenu-container">
                <RouterLink to="/dev/hub" class="submenu-item" :class="{ active: isActive('/dev/hub') }">
                  <i class="pi pi-home"></i>
                  <span class="menu-label">Hub Documents</span>
                </RouterLink>
                <RouterLink to="/dev/fab/specifique" class="submenu-item" :class="{ active: isActive('/dev/fab/specifique') }">
                  <i class="pi pi-sliders-h"></i>
                  <span class="menu-label">Structure PRC/PRNC</span>
                </RouterLink>
              </div>
            </div>
          </div>

          <div class="menu-item-wrapper" v-if="isResponsableOrAdmin">
            <div class="menu-group">
              <div class="menu-item-group-title">
                <i class="pi pi-cog text-amber-500"></i>
                <span v-show="!isCollapsed" class="menu-label">Modeles de Fabrication</span>
              </div>
              <div v-show="!isCollapsed" class="submenu-container">
                <RouterLink to="/dev/fab/modeles" class="submenu-item" :class="{ active: isActive('/dev/fab/modeles') }">
                  <i class="pi pi-list"></i>
                  <span class="menu-label">Hub Modeles</span>
                </RouterLink>
                <RouterLink to="/dev/fab/nouveau" class="submenu-item" :class="{ active: isActive('/dev/fab/nouveau') }">
                  <i class="pi pi-plus-circle"></i>
                  <span class="menu-label">Creer un Modele</span>
                </RouterLink>
              </div>
            </div>
          </div>
        </div>

        <!-- CATEGORIE: PRODUCTION -->
        <div class="menu-category" v-if="isResponsableOrAdmin">
          <div v-show="!isCollapsed" class="category-header">
            PRODUCTION
          </div>

          <div class="menu-item-wrapper">
            <div class="menu-group">
              <div class="menu-item-group-title">
                <i class="pi pi-list text-blue-400"></i>
                <span v-show="!isCollapsed" class="menu-label">Plans specifiques</span>
              </div>
              <div v-show="!isCollapsed" class="submenu-container">
                <RouterLink to="/dev/hub-plans" class="submenu-item" :class="{ active: isActive('/dev/hub-plans') }">
                  <i class="pi pi-list"></i>
                  <span class="menu-label">Hub Plans Articles</span>
                </RouterLink>
                <RouterLink to="/dev/fab/plans/nouveau" class="submenu-item" :class="{ active: isActive('/dev/fab/plans/nouveau') }">
                  <i class="pi pi-plus-circle"></i>
                  <span class="menu-label">Creer plan par article</span>
                </RouterLink>
              </div>
            </div>
          </div>
        </div>

        <!-- CATEGORIE: ANALYSES -->
        <div class="menu-category">
          <div v-show="!isCollapsed" class="category-header">
            ANALYSES
          </div>
          <div class="menu-item-wrapper">
            <RouterLink to="/traces" class="menu-item" :class="{ active: isActive('/traces') }">
              <i class="pi pi-chart-line text-emerald-400"></i>
              <span v-show="!isCollapsed" class="menu-label">Dashboard & Tracabilite</span>
            </RouterLink>
          </div>
        </div>

      </template>

      <!-- MENU GENERAL POUR LES AUTRES ROLES -->
      <template v-else>
        <div class="menu-category">
          <div v-show="!isCollapsed" class="category-header">GENERAL</div>
          <div class="menu-item-wrapper">
            <RouterLink to="/dev/hub" class="menu-item" :class="{ active: isActive('/dev/hub') }">
              <i class="pi pi-home"></i>
              <span v-show="!isCollapsed" class="menu-label">Accueil</span>
            </RouterLink>
          </div>
          <div class="menu-item-wrapper">
            <RouterLink to="/traces" class="menu-item" :class="{ active: isActive('/traces') }">
              <i class="pi pi-list"></i>
              <span v-show="!isCollapsed" class="menu-label">Traces</span>
            </RouterLink>
          </div>
        </div>
      </template>

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
          <p class="user-role">{{ userRoleLabel }}</p>
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
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import Avatar from 'primevue/avatar'
import ContextMenu from 'primevue/contextmenu'
import { useAuthStore } from '@/stores/authStore'

defineOptions({
  name: 'TheSidebar'
})

const router = useRouter()
const authStore = useAuthStore()

const isCollapsed = ref(false)
const showUserMenu = ref(false)

const userName = computed(() => authStore.userName)
const userRoleLabel = computed(() => {
  const role = (authStore.userRole || '').toUpperCase();
  if (role.includes('RESPONSABLE')) return 'Responsable Qualite';
  if (role.includes('ADMIN')) return 'Administrateur';
  return role;
})

const isSuperviseurOrAdmin = computed(() => {
  const role = (authStore.userRole || '').toUpperCase();
  return role.includes('SUPERVISEUR') || role.includes('ADMIN');
});

const isResponsableOrAdmin = computed(() => {
  const role = (authStore.userRole || '').toUpperCase();
  return role.includes('RESPONSABLE') || role.includes('ADMIN');
});

const hasAdvancedAccess = computed(() => {
  return isSuperviseurOrAdmin.value || isResponsableOrAdmin.value;
});

const userMenu = [
  {
    label: 'Profil',
    icon: 'pi pi-user',
    command: () => router.push('/profile')
  },
  {
    label: 'Parametres',
    icon: 'pi pi-cog',
    command: () => router.push('/settings')
  },
  { separator: true },
  {
    label: 'Deconnexion',
    icon: 'pi pi-sign-out',
    command: () => authStore.logout()
  }
]

const toggleSidebar = () => {
  isCollapsed.value = !isCollapsed.value
}

const isActive = (path) => {
  const currentPath = router.currentRoute?.value?.path || router.currentRoute?.path;
  if (currentPath === path) return true;
  return false;
}
</script>

<style scoped>
.sidebar {
  width: 280px;
  background: #111827;
  color: #e5e7eb;
  padding: 1rem 0;
  height: 100vh;
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  transition: width 0.3s ease;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.2);
  font-family: 'Inter', ui-sans-serif, system-ui, sans-serif;
}

.sidebar-collapsed {
  width: 80px;
}

.sidebar-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 1.25rem 1rem 1.25rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.05);
  margin-bottom: 1rem;
}

.logo {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  font-weight: 900;
  font-size: 1.1rem;
  letter-spacing: 0.05em;
  color: white;
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
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #9ca3af;
  width: 32px;
  height: 32px;
  border-radius: 0.5rem;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.toggle-btn:hover {
  background: rgba(255, 255, 255, 0.1);
  color: white;
}

.sidebar-menu {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 0 1rem;
}

.menu-category {
  margin-bottom: 1.5rem;
}

.category-header {
  font-size: 0.7rem;
  font-weight: 800;
  color: #6b7280;
  text-transform: uppercase;
  letter-spacing: 0.1em;
  margin-bottom: 0.75rem;
  padding-left: 0.5rem;
}

.menu-item-wrapper {
  margin-bottom: 0.25rem;
}

.menu-group {
  margin-bottom: 0.5rem;
}

.menu-item-group-title {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 0.5rem;
  color: #d1d5db;
  font-weight: 600;
  font-size: 0.95rem;
}

.menu-item-group-title i {
  color: #f59e0b;
  font-size: 1.1rem;
}

.submenu-container {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
  padding-left: 1rem;
  margin-top: 0.25rem;
}

.submenu-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.75rem;
  border-radius: 0.5rem;
  color: #9ca3af;
  text-decoration: none;
  font-size: 0.85rem;
  font-weight: 500;
  transition: all 0.2s ease;
  position: relative;
}

.submenu-item i {
  font-size: 0.9rem;
  color: #10b981;
}

.submenu-item:hover {
  background: rgba(255, 255, 255, 0.08);
  color: white;
  border-radius: 0.5rem;
}

.submenu-item.active {
  background: rgba(255, 255, 255, 0.15);
  color: white;
  border-left: 3px solid #f59e0b;
  border-radius: 0 0.5rem 0.5rem 0;
  font-weight: 700;
  box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.08);
}

.menu-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.75rem 0.5rem;
  border-radius: 0.5rem;
  color: #d1d5db;
  text-decoration: none;
  transition: all 0.2s ease;
  cursor: pointer;
  white-space: nowrap;
  position: relative;
  font-weight: 600;
  font-size: 0.95rem;
}

.menu-item:hover {
  background: rgba(255, 255, 255, 0.08);
  color: white;
}

.menu-item.active {
  background: rgba(255, 255, 255, 0.15);
  color: white;
  border-left: 3px solid #f59e0b;
  border-radius: 0 0.5rem 0.5rem 0;
  font-weight: 700;
}

.menu-item i {
  font-size: 1.1rem;
  min-width: 1.5rem;
  color: #60a5fa;
}

.menu-label {
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
}

.badge {
  background: #3b82f6;
  color: white;
  font-size: 0.7rem;
  padding: 0.15rem 0.4rem;
  border-radius: 9999px;
  font-weight: 700;
  margin-left: auto;
}

.sidebar-footer {
  padding: 1rem 1.25rem 0 1.25rem;
  border-top: 1px solid rgba(255, 255, 255, 0.05);
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  background: rgba(255, 255, 255, 0.05);
  border: 1px solid rgba(255, 255, 255, 0.05);
  color: white;
  padding: 0.75rem;
  border-radius: 0.75rem;
  cursor: pointer;
  width: 100%;
  transition: all 0.2s ease;
}

.user-profile:hover {
  background: rgba(255, 255, 255, 0.1);
}

.user-info {
  text-align: left;
  flex: 1;
  overflow: hidden;
}

.user-name {
  margin: 0;
  font-weight: 600;
  font-size: 0.85rem;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.user-role {
  margin: 0;
  font-size: 0.7rem;
  color: #9ca3af;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

:deep(.p-avatar) {
  width: 2rem;
  height: 2rem;
}

::-webkit-scrollbar {
  width: 4px;
}

::-webkit-scrollbar-track {
  background: transparent;
}

::-webkit-scrollbar-thumb {
  background: rgba(255, 255, 255, 0.1);
  border-radius: 4px;
}

::-webkit-scrollbar-thumb:hover {
  background: rgba(255, 255, 255, 0.2);
}
</style>
