<template>
  <div class="dashboard-view">
    <MainLayout>
      <template #sidebar>
        <Sidebar />
      </template>
      
      <template #content>
        <div class="dashboard-content">
          <div class="dashboard-header">
            <h1>Tableau de Bord</h1>
            <p class="subtitle">Bienvenue, {{ authStore.user?.name || 'Utilisateur' }}</p>
          </div>
          
          <div class="dashboard-grid">
            <!-- Cartes de statistiques -->
            <Card class="stat-card">
              <template #content>
                <div class="stat-item">
                  <i class="pi pi-chart-bar stat-icon"></i>
                  <h3>Visites</h3>
                  <p class="stat-value">1,234</p>
                </div>
              </template>
            </Card>
            
            <Card class="stat-card">
              <template #content>
                <div class="stat-item">
                  <i class="pi pi-users stat-icon"></i>
                  <h3>Utilisateurs</h3>
                  <p class="stat-value">567</p>
                </div>
              </template>
            </Card>
            
            <Card class="stat-card">
              <template #content>
                <div class="stat-item">
                  <i class="pi pi-eye stat-icon"></i>
                  <h3>Traces</h3>
                  <p class="stat-value">8,901</p>
                </div>
              </template>
            </Card>
            
            <Card class="stat-card">
              <template #content>
                <div class="stat-item">
                  <i class="pi pi-check-circle stat-icon"></i>
                  <h3>Complétés</h3>
                  <p class="stat-value">94%</p>
                </div>
              </template>
            </Card>
          </div>
          
          <!-- Contenu principal -->
          <Card class="main-content-card">
            <template #header>
              <div class="card-header-custom">
                <h2>Données Récentes</h2>
              </div>
            </template>
            <template #content>
              <DataTable :value="sampleData" responsiveLayout="scroll">
                <Column field="id" header="ID"></Column>
                <Column field="name" header="Nom"></Column>
                <Column field="status" header="Statut">
                  <template #body="{ data }">
                    <Tag 
                      :value="data.status" 
                      :severity="getStatusSeverity(data.status)"
                    ></Tag>
                  </template>
                </Column>
                <Column field="date" header="Date"></Column>
              </DataTable>
            </template>
          </Card>
        </div>
      </template>
    </MainLayout>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import MainLayout from '../layouts/MainLayout.vue'
import Sidebar from '../components/Navigation/Sidebar.vue'
import Card from 'primevue/card'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'

const authStore = {
  user: null
}

const sampleData = ref([
  { id: 1, name: 'Document 1', status: 'Complet', date: '2026-03-30' },
  { id: 2, name: 'Document 2', status: 'En attente', date: '2026-03-29' },
  { id: 3, name: 'Document 3', status: 'Complet', date: '2026-03-28' },
  { id: 4, name: 'Document 4', status: 'Erreur', date: '2026-03-27' },
  { id: 5, name: 'Document 5', status: 'Complet', date: '2026-03-26' }
])

const getStatusSeverity = (status) => {
  switch (status) {
    case 'Complet':
      return 'success'
    case 'En attente':
      return 'warning'
    case 'Erreur':
      return 'danger'
    default:
      return 'info'
  }
}
</script>

<style scoped>
.dashboard-view {
  width: 100%;
  min-height: 100vh;
}

.dashboard-content {
  padding: 2rem;
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  min-height: calc(100vh - 60px);
}

.dashboard-header {
  margin-bottom: 2rem;
}

.dashboard-header h1 {
  font-size: 2.5rem;
  margin: 0;
  color: #333;
  font-weight: 700;
}

.subtitle {
  color: #666;
  font-size: 1rem;
  margin: 0.5rem 0 0 0;
}

.dashboard-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
  gap: 1.5rem;
  margin-bottom: 2rem;
}

.stat-card {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
  transition: transform 0.3s ease, box-shadow 0.3s ease;
  cursor: pointer;
}

.stat-card:hover {
  transform: translateY(-5px);
  box-shadow: 0 8px 12px rgba(0, 0, 0, 0.15);
}

.stat-item {
  text-align: center;
  padding: 1rem;
}

.stat-icon {
  font-size: 2.5rem;
  color: #3b82f6;
  margin-bottom: 1rem;
  display: block;
}

.stat-item h3 {
  font-size: 1rem;
  color: #666;
  margin: 0.5rem 0;
  font-weight: 600;
}

.stat-value {
  font-size: 1.8rem;
  color: #333;
  margin: 0;
  font-weight: 700;
}

.main-content-card {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.card-header-custom {
  padding: 1rem;
  border-bottom: 1px solid #e5e7eb;
}

.card-header-custom h2 {
  margin: 0;
  font-size: 1.5rem;
  color: #333;
}
</style>