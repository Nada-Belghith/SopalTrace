<template>
  <MainLayout>
    <template #sidebar>
      <Sidebar />
    </template>

    <template #content>
      <div class="traces-content p-4">
        <div class="traces-header">
          <h1>Traces</h1>
          <p class="subtitle">Gestion des traces de suivi</p>
        </div>

        <!-- Filtres et Actions -->
        <Card class="filter-card mb-4">
          <template #content>
            <div class="filter-row">
              <InputText
                v-model="searchQuery"
                placeholder="Rechercher une trace..."
                class="search-field"
              />
              
              <PermissionGuard permission="CREATE_TRACE">
                <Button 
                  @click="openNewTraceDialog"
                  icon="pi pi-plus"
                  label="Nouvelle Trace"
                  class="p-button-success"
                />
              </PermissionGuard>
            </div>
          </template>
        </Card>

        <!-- Tableau des traces -->
        <Card>
          <template #header>
            <div class="card-header-custom">
              <h2>Liste des Traces</h2>
            </div>
          </template>
          <template #content>
            <DataTable 
              :value="filteredTraces" 
              responsiveLayout="scroll"
              :paginator="true"
              :rows="10"
              class="p-datatable-striped"
            >
              <Column field="id" header="ID" style="width: 10%"></Column>
              <Column field="name" header="Nom" style="width: 35%"></Column>
              <Column field="status" header="Statut" style="width: 20%">
                <template #body="{ data }">
                  <Tag 
                    :value="data.status" 
                    :severity="getStatusSeverity(data.status)"
                  ></Tag>
                </template>
              </Column>
              <Column field="date" header="Date" style="width: 20%"></Column>
              <Column header="Actions" style="width: 15%">
                <template #body="{ data }">
                  <div class="action-buttons">
                    <Button 
                      icon="pi pi-eye" 
                      class="p-button-rounded p-button-info p-button-sm"
                      @click="viewTrace(data)"
                      v-tooltip.top="'Afficher'"
                    />
                    <PermissionGuard permission="EDIT_TRACE">
                      <Button 
                        icon="pi pi-pencil" 
                        class="p-button-rounded p-button-warning p-button-sm"
                        @click="editTrace(data)"
                        v-tooltip.top="'Modifier'"
                      />
                    </PermissionGuard>
                    <PermissionGuard permission="DELETE_TRACE">
                      <Button 
                        icon="pi pi-trash" 
                        class="p-button-rounded p-button-danger p-button-sm"
                        @click="deleteTrace(data)"
                        v-tooltip.top="'Supprimer'"
                      />
                    </PermissionGuard>
                  </div>
                </template>
              </Column>
            </DataTable>
          </template>
        </Card>
      </div>
    </template>
  </MainLayout>
</template>

<script setup>
import { ref, computed } from 'vue'
import MainLayout from '@/layouts/MainLayout.vue'
import Sidebar from '@/components/Navigation/Sidebar.vue'
import PermissionGuard from '@/components/Authorization/PermissionGuard.vue'
import Card from 'primevue/card'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import Button from 'primevue/button'
import InputText from 'primevue/inputtext'
import { TRACE_STATUS, TRACE_STATUS_SEVERITY } from '@/constants'

const searchQuery = ref('')

const traces = ref([
  { id: 1, name: 'Document 1', status: TRACE_STATUS.COMPLETED, date: '2026-03-30' },
  { id: 2, name: 'Document 2', status: TRACE_STATUS.PENDING, date: '2026-03-29' },
  { id: 3, name: 'Document 3', status: TRACE_STATUS.COMPLETED, date: '2026-03-28' },
  { id: 4, name: 'Document 4', status: TRACE_STATUS.FAILED, date: '2026-03-27' },
  { id: 5, name: 'Document 5', status: TRACE_STATUS.COMPLETED, date: '2026-03-26' }
])

const filteredTraces = computed(() => {
  if (!searchQuery.value) return traces.value
  return traces.value.filter(trace =>
    trace.name.toLowerCase().includes(searchQuery.value.toLowerCase())
  )
})

const getStatusSeverity = (status) => TRACE_STATUS_SEVERITY[status] || 'info'

const openNewTraceDialog = () => {
  console.log('Ouvrir dialogue de nouvelle trace')
}

const viewTrace = (trace) => {
  console.log('Afficher trace:', trace)
}

const editTrace = (trace) => {
  console.log('Modifier trace:', trace)
}

const deleteTrace = (trace) => {
  console.log('Supprimer trace:', trace)
}
</script>

<style scoped>
.traces-content {
  background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
  min-height: calc(100vh - 60px);
}

.traces-header {
  margin-bottom: 2rem;
}

.traces-header h1 {
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

.filter-card {
  box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
}

.filter-row {
  display: flex;
  gap: 1rem;
  align-items: center;
}

.search-field {
  flex: 1;
  min-width: 250px;
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

.action-buttons {
  display: flex;
  gap: 0.5rem;
}

:deep(.p-datatable-striped) {
  border-radius: 0.5rem;
  overflow: hidden;
}
</style>