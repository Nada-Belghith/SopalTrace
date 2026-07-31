<script setup>
import { ref, onMounted, computed, onUnmounted, watch } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import apiClient from '@/services/apiClient'
import { useAppToast } from '@/composables/useAppToast'
import { useAuthStore } from '@/stores/authStore'
import { useOperateurStore } from '@/stores/execution/operateurStore'
import Button from 'primevue/button'
import Dialog from 'primevue/dialog'

const router = useRouter()
const route = useRoute()
const toast = useAppToast()
const authStore = useAuthStore()
const operateurStore = useOperateurStore()

const execControleOfId = route.params.execControleOfId
const posteCode = ref(route.query.posteCode)
const rawEquipe = localStorage.getItem('sopal_sessionEquipe') || operateurStore.activeOfContext?.numEquipe || '1'
const equipe = ref(rawEquipe.toString().replace(/\D/g, '') || '1')
const currentStatutId = ref(route.query.statutId)

const loading = ref(false)
const saving = ref(false)
const planData = ref(null)

const existingSessionForDate = computed(() => {
  if (!newDocDate.value) return null
  return availableSessions.value.find(s => s.dateExecution && s.dateExecution.split('T')[0] === newDocDate.value)
})

const viewMode = ref('loading')
const initialMode = route.query.mode === 'all' ? 'gallery' : 'details'
const planManquant = ref(false)

const availableSessions = ref([])
const isConsultationMode = ref(false)
const selectedFilterDate = ref('')
const selectedFilterEquipe = ref('')

const showNewDocModal = ref(false)
const newDocDate = ref(new Date().toISOString().split('T')[0])

const showPlanManquantDialog = ref(false)
const isSignalingPlan = ref(false)

const isDocumentTermine = ref(false)
const documentDate = ref(new Date().toLocaleDateString('fr-FR'))

const currentHour = ref(new Date().getHours())

const timerId = ref(null)

const tranchesParEquipe = {
  '1': ['06-07', '07-08', '08-09', '09-10', '10-11', '11-12', '12-13', '13-14'],
  '2': ['14-15', '15-16', '16-17', '17-18', '18-19', '19-20', '20-21', '21-22'],
  '3': ['22-23', '23-00', '00-01', '01-02', '02-03', '03-04', '04-05', '05-06']
}

const activeTranches = computed(() => {
  return tranchesParEquipe[equipe.value] || tranchesParEquipe['1']
})

const isCurrentTranche = (tranche) => {
  const [start] = tranche.split('-').map(Number)
  return currentHour.value === start
}

const formatDate = (dateStr) => {
  if (!dateStr) return '-'
  return new Date(dateStr).toLocaleDateString('fr-FR')
}

const availableDates = computed(() => {
  const map = new Map()
  availableSessions.value.forEach(s => {
    if (s.dateExecution) {
      const rawDateStr = s.dateExecution.split('T')[0]
      if (!map.has(rawDateStr)) {
        map.set(rawDateStr, formatDate(s.dateExecution))
      }
    }
  })
  return Array.from(map.entries()).map(([raw, formatted]) => ({ raw, formatted }))
})


const filteredSessions = computed(() => {
  return availableSessions.value.filter(s => {
    const matchDate = !selectedFilterDate.value || (s.dateExecution && s.dateExecution.split('T')[0] === selectedFilterDate.value)
    const matchEquipe = !selectedFilterEquipe.value || (s.equipe === selectedFilterEquipe.value)
    return matchDate && matchEquipe
  })
})

const selectCardSession = async (statutIdToLoad) => {
  await loadSession(statutIdToLoad)
  viewMode.value = 'details'
}

const switchToGalleryMode = async () => {
  viewMode.value = 'gallery'
  await loadPlan()
}

const handleCreateNewDocument = async () => {
  if (!newDocDate.value) {
    toast.error('Erreur', 'Veuillez sélectionner une date.')
    return
  }
  loading.value = true
  showNewDocModal.value = false
  try {
    const payload = {
      posteCode: posteCode.value,
      dateExecution: newDocDate.value,
      equipe: equipe.value
    }
    const res = await apiClient.post(`/ExecRcPoste/${execControleOfId}/nouveau-document`, payload)
    processResponse(res)
    viewMode.value = 'details'
    toast.success('Succès', 'Nouveau document créé.')
  } catch (err) {
    toast.error('Erreur', err.response?.data?.message || 'Impossible de créer un nouveau document.')
  } finally {
    loading.value = false
  }
}


const docHeader = computed(() => {
  return {
    codeArticle: operateurStore.activeOfContext?.codeArticle || operateurStore.activeOfContext?.articleCode || '-',
    designationArticle: operateurStore.activeOfContext?.designationArticle || '-',
    numeroOf: operateurStore.activeOfContext?.numeroOf || operateurStore.activeOfContext?.numeroOF || '-',
    equipe: equipe.value || '-',
    posteCode: posteCode.value || '-',
    dateExecution: documentDate.value
  }
})

// Initialize form model
const formLignes = ref([])
const formReponses = ref({})
const formBilan = ref({
  totalDefauts: 0,
  totalPiecesTestees: 0,
  tauxNc: 0,
  nbPiecesRebutees: 0,
  nbPieceConforme: 0
})

const configurationEquipes = ref([])

const allColumns = computed(() => {
  const cols = [];
  if (configurationEquipes.value.length === 0) {
    // Fallback if no config
    Object.keys(tranchesParEquipe).forEach(eq => {
      tranchesParEquipe[eq].forEach(t => {
        cols.push({ id: `col_${eq}_${t}`, header: t, group: `Equipe ${eq}`, numEquipe: eq });
      });
    });
    return cols;
  }
  
  configurationEquipes.value.forEach((eq, idx) => {
    let cur = eq.debut;
    let steps = eq.fin > eq.debut ? (eq.fin - eq.debut) : (24 - eq.debut + eq.fin);
    for (let i = 0; i < steps; i++) {
      let start = cur % 24;
      let end = (cur + 1) % 24;
      cols.push({
        id: `col_${idx}_${start}_${end}`,
        header: `${start.toString().padStart(2, '0')}-${end.toString().padStart(2, '0')}`,
        group: eq.nom,
        numEquipe: (idx + 1).toString()
      });
      cur++;
    }
  });
  return cols;
});

const headerGroups = computed(() => {
  const groups = [];
  let currentGroup = null;
  let currentGroupCols = [];
  let colspan = 0;
  
  for (const col of allColumns.value) {
    if (col.group === currentGroup) {
      colspan++;
      currentGroupCols.push(col);
    } else {
      if (colspan > 0) {
        groups.push({ name: currentGroup, colspan, columns: currentGroupCols, numEquipe: currentGroupCols[0].numEquipe });
      }
      currentGroup = col.group;
      currentGroupCols = [col];
      colspan = 1;
    }
  }
  if (colspan > 0) {
    groups.push({ name: currentGroup, colspan, columns: currentGroupCols, numEquipe: currentGroupCols[0].numEquipe });
  }
  return groups;
});

const activeTranchesComputed = computed(() => {
  const currentGroup = headerGroups.value.find(g => g.numEquipe == equipe.value);
  if (currentGroup) return currentGroup.columns.map(c => c.header);
  return activeTranches.value;
});

const loadPlan = async () => {
  loading.value = true
  try {
    const res = await apiClient.get(`/ExecRcPoste/${execControleOfId}?posteCode=${posteCode.value}&equipe=${equipe.value}`)
    processResponse(res)
    if (viewMode.value === 'loading') {
      if (route.query.mode === 'auto') {
        const active = availableSessions.value.find(s => !s.estTermine)
        viewMode.value = active ? 'details' : 'gallery'
      } else {
        viewMode.value = initialMode
      }
    }
  } catch (err) {
    if (err.status === 404 || err.status === 400) {
      planManquant.value = true
      viewMode.value = 'planManquant'
    } else {
      viewMode.value = initialMode
      toast.error('Erreur', 'Impossible de charger le plan RC Poste.')
    }
  } finally {
    loading.value = false
  }
}

const signalerPlanManquant = async () => {
  isSignalingPlan.value = true
  try {
    await apiClient.post('/Alertes/plan-manquant', {
      operationCode: 'ASS',
      posteCode: posteCode.value,
      numeroOf: operateurStore.activeOfContext?.numeroOf,
      articleCode: operateurStore.activeOfContext?.articleCode,
      designationArticle: operateurStore.activeOfContext?.designationArticle,
      descriptionProbleme: `Document Contrôle au Poste introuvable ou inactif pour le poste ${posteCode.value}`
    })
    toast.success('Signalé !', 'Une notification a été envoyée au superviseur.')
    showPlanManquantDialog.value = false
    router.push({ name: 'operateur-assemblage' })
  } catch {
    toast.error('Erreur', 'Impossible d\'envoyer l\'alerte.')
  } finally {
    isSignalingPlan.value = false
  }
}

const loadSession = async (statutId) => {
  loading.value = true
  try {
    const res = await apiClient.get(`/ExecRcPoste/statut/${statutId}`)
    processResponse(res)
  } catch {
    toast.error('Erreur', 'Impossible de charger cette session.')
  } finally {
    loading.value = false
  }
}

const processResponse = (res) => {
    if (res.data) {
      planData.value = res.data
      currentStatutId.value = res.data.execControleDocumentStatutId
      
      if (res.data.availableSessions) {
        availableSessions.value = res.data.availableSessions
      }
      const currentSession = availableSessions.value.find(s => s.statutId === currentStatutId.value)
      if (currentSession) {
        isDocumentTermine.value = currentSession.estTermine
        isConsultationMode.value = currentSession.estTermine
        if (currentSession.dateExecution) {
          selectedFilterDate.value = currentSession.dateExecution.split('T')[0]
          documentDate.value = formatDate(currentSession.dateExecution)
        }
      }
      
      try {
        if (res.data.configurationColonnesJson && res.data.configurationColonnesJson !== "{}") {
           const parsed = JSON.parse(res.data.configurationColonnesJson)
           configurationEquipes.value = parsed.equipes || []
        }
      } catch (e) {
        console.error("Error parsing config", e);
      }
      
      const tranchesToUse = allColumns.value.map(c => c.header);

      // Map lines
      formLignes.value = res.data.lignes.map(l => {
        const row = {
          docLigneId: l.docLigneId,
          libelleAffiche: l.libelleAffiche,
          machineCodeCtrlPoste: l.machineCodeCtrlPoste,
          heures: {}
        }
        tranchesToUse.forEach(t => {
          const h = l.heures.find(x => x.trancheHoraire === t)
          row.heures[t] = h ? h.nbNcParHeure : null
        })
        return row
      })

      // Map reponses (totals)
      tranchesToUse.forEach(t => {
        const r = res.data.reponses.find(x => x.trancheHoraire === t)
        formReponses.value[t] = r ? r.totalRealiseHeure : null
      })

      if (res.data.bilan) {
        formBilan.value = { ...res.data.bilan }
      }
    }
}

const computeTotals = () => {
  let totalDef = 0
  let totalTested = 0
  
  const allTranches = allColumns.value.map(c => c.header);

  formLignes.value.forEach(l => {
    allTranches.forEach(t => {
      if (l.heures[t]) totalDef += Number(l.heures[t])
    })
  })

  allTranches.forEach(t => {
    if (formReponses.value[t]) totalTested += Number(formReponses.value[t])
  })

  formBilan.value.totalDefauts = totalDef
  // Total pièces testées = Total réalisé + Total des défauts
  formBilan.value.totalPiecesTestees = totalTested + totalDef
  const totalPieces = formBilan.value.totalPiecesTestees
  if (totalPieces > 0) {
    formBilan.value.tauxNc = Number(((totalDef / totalPieces) * 100).toFixed(2))
  } else {
    formBilan.value.tauxNc = 0
  }
  formBilan.value.nbPieceConforme = totalPieces - (Number(formBilan.value.nbPiecesRebutees) || 0)
}

watch([formLignes, formReponses, () => formBilan.value.nbPiecesRebutees], () => {
  computeTotals()
}, { deep: true })

const handleValider = async () => {
  computeTotals()
  saving.value = true
  try {
    const allTranches = allColumns.value.map(c => c.header)
    const payload = {
      execControleDocumentStatutId: currentStatutId.value,
      lignes: formLignes.value.map(l => ({
        docLigneId: l.docLigneId,
        heures: allTranches
          .filter(t => l.heures[t] !== null && l.heures[t] !== undefined && l.heures[t] !== '')
          .map(t => ({
             trancheHoraire: t,
             nbNcParHeure: l.heures[t]
          }))
      })),
      reponses: allTranches
        .filter(t => formReponses.value[t] !== null && formReponses.value[t] !== undefined && formReponses.value[t] !== '')
        .map(t => {
          let colDefauts = 0
          formLignes.value.forEach(l => {
            if (l.heures[t]) colDefauts += Number(l.heures[t])
          })
          return {
             trancheHoraire: t,
             totalNcHeure: colDefauts,
             totalRealiseHeure: formReponses.value[t]
          }
        }),
      bilan: formBilan.value
    }

    await apiClient.put(`/ExecRcPoste/${currentStatutId.value}`, payload)
    toast.success('Succès', 'Les données ont été enregistrées.')
    switchToGalleryMode()
  } catch {
    toast.error('Erreur', 'Impossible de sauvegarder.')
  } finally {
    saving.value = false
  }
}

const marquerTermine = async () => {
  await handleValider()
  saving.value = true
  try {
    await apiClient.put(`/Operateur/assemblage-documents/${currentStatutId.value}/terminer`)
    isDocumentTermine.value = true
    toast.success('Succès', 'Le document a été clôturé.')
    switchToGalleryMode()
  } catch {
    toast.error('Erreur', 'Impossible de clôturer le document.')
  } finally {
    saving.value = false
  }
}

const goBack = () => {
  if (viewMode.value === 'details') {
    switchToGalleryMode()
  } else {
    router.push({ 
      name: 'operateur-of-fini',
      query: { 
        execControleOfId: execControleOfId,
        posteCode: posteCode.value
      }
    })
  }
}

onMounted(() => {
  if (!posteCode.value || !equipe.value) {
    toast.error('Paramètres manquants', 'Poste ou Equipe non définis.')
    return
  }
  loadPlan()
  
  // Timer to update current hour display
  timerId.value = setInterval(() => {
    currentHour.value = new Date().getHours()
  }, 60000) // check every minute
})

onUnmounted(() => {
  if (timerId.value) clearInterval(timerId.value)
})

</script>

<template>
  <div class="rc-poste-view p-6 flex flex-col h-full bg-slate-50">
    <!-- Breadcrumb / Back -->
    <div class="mb-4">
      <Button icon="pi pi-arrow-left" label="Retour" class="p-button-text" @click="goBack" />
    </div>

    <div v-if="loading" class="flex justify-center p-8">
      <i class="pi pi-spin pi-spinner text-3xl text-primary"></i>
    </div>

    <!-- PLAN MANQUANT -->
    <div v-else-if="viewMode === 'planManquant'" class="flex-1 flex items-center justify-center">
      <div class="bg-white rounded-2xl shadow-md border border-slate-200 p-12 max-w-lg w-full text-center">
        <i class="pi pi-info-circle text-6xl text-slate-300 mb-6" style="display:block"></i>
        <h2 class="text-xl font-bold text-slate-700 mb-2">Contrôle au Poste</h2>
        <p class="text-slate-600 text-base mb-2">Aucun plan de contrôle au poste n'est paramétré pour <strong>{{ posteCode }}</strong>.</p>
        <p class="text-slate-400 text-sm mb-8">Contactez votre superviseur pour qu'il crée ou active le document associé à ce poste.</p>
        <Button 
          label="Signaler au Superviseur" 
          icon="pi pi-exclamation-triangle" 
          severity="danger" 
          outlined 
          :loading="isSignalingPlan"
          @click="signalerPlanManquant" 
        />
      </div>
    </div>

    <!-- MODE GALERIE -->
    <div v-else-if="viewMode === 'gallery'" class="flex-1 flex flex-col">
      <div class="flex justify-between items-end mb-6 bg-white p-5 rounded-2xl shadow-sm border border-slate-200">
        <div>
          <h1 class="text-2xl font-black text-slate-800 tracking-tight flex items-center gap-3">
            <i class="pi pi-th-large text-blue-500"></i>
            Sessions Contrôle au Poste
          </h1>
          <p class="text-slate-500 font-medium text-sm mt-1 ml-9">
            Poste : <strong class="text-slate-700">{{ posteCode }}</strong>
          </p>
        </div>
        
        <div class="flex gap-6 items-end">
          <div class="flex flex-col gap-3">
            <div class="flex items-center gap-2">
              <label class="text-xs font-bold text-slate-600 uppercase tracking-wide">Date :</label>
              <select 
                v-model="selectedFilterDate" 
                class="bg-slate-50 border border-slate-300 text-slate-800 text-xs font-bold rounded-lg p-2.5 focus:ring-2 focus:ring-blue-100 focus:border-blue-500 outline-none shadow-sm cursor-pointer min-w-[150px]"
              >
                <option value="">Toutes les dates</option>
                <option v-for="d in availableDates" :key="d.raw" :value="d.raw">
                  📅 {{ d.formatted }}
                </option>
              </select>
            </div>
          </div>
          
          <Button label="Nouveau document" icon="pi pi-plus" class="p-button-primary mb-1" @click="showNewDocModal = true" />
        </div>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        <div 
          v-for="s in filteredSessions" 
          :key="s.statutId"
          @click="selectCardSession(s.statutId)"
          class="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm hover:shadow-md hover:border-blue-400 transition-all cursor-pointer flex flex-col justify-between group relative overflow-hidden"
        >
          <div class="flex justify-between items-center mb-4">
            <span class="px-2.5 py-1 rounded-full text-[10px] font-extrabold uppercase tracking-wider flex items-center gap-1"
                  :class="s.estTermine ? 'bg-emerald-50 text-emerald-700 border border-emerald-200' : 'bg-amber-50 text-amber-700 border border-amber-200'">
              <i :class="s.estTermine ? 'pi pi-check-circle' : 'pi pi-clock'"></i>
              {{ s.estTermine ? 'Terminé' : 'En Cours' }}
            </span>
          </div>

          <div class="space-y-2 mb-4">
            <div class="flex items-center text-slate-800 font-bold text-sm">
              <i class="pi pi-calendar mr-2 text-blue-500"></i>
              <span>Date : {{ formatDate(s.dateExecution) }}</span>
            </div>
          </div>

          <div class="pt-3 border-t border-slate-100 flex justify-between items-center text-xs font-bold text-blue-600 group-hover:text-blue-700">
            <span>Ouvrir & Consulter</span>
            <i class="pi pi-arrow-right transform group-hover:translate-x-1 transition-transform"></i>
          </div>
        </div>

        <div v-if="filteredSessions.length === 0" class="col-span-full text-center py-12 bg-white rounded-2xl border border-slate-200">
          <i class="pi pi-search text-3xl text-slate-300 mb-2"></i>
          <p class="text-slate-500 font-bold text-sm">Aucun rapport ne correspond à votre filtre.</p>
        </div>
      </div>
      
      <!-- Modal Nouveau Document -->
      <Dialog v-model:visible="showNewDocModal" header="Nouveau Document" :modal="true" class="w-full max-w-md">
        <div class="p-4 flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label class="font-bold text-slate-700 text-sm">Date d'exécution :</label>
            <input type="date" v-model="newDocDate" class="p-2 border rounded-md" />
          </div>
          <div v-if="existingSessionForDate" class="p-3 bg-blue-50 border border-blue-200 rounded-lg text-blue-800 text-sm flex items-start gap-2">
            <i class="pi pi-info-circle mt-0.5"></i>
            <div>
              <p v-if="existingSessionForDate.estTermine">Un document pour cette date existe déjà et est <strong>terminé</strong>.</p>
              <p v-else>Un document pour cette date est déjà <strong>en cours</strong>.</p>
            </div>
          </div>
        </div>
        <template #footer>
          <Button label="Annuler" icon="pi pi-times" class="p-button-text text-slate-500" @click="showNewDocModal = false" />
          <Button v-if="existingSessionForDate" label="Ouvrir" icon="pi pi-folder-open" class="p-button-outlined p-button-info" @click="selectCardSession(existingSessionForDate.statutId); showNewDocModal = false" />
          <Button v-else label="Créer" icon="pi pi-check" class="p-button-primary" @click="handleCreateNewDocument" />
        </template>
      </Dialog>
    </div>

    <!-- MODE DETAILS -->
    <template v-else-if="viewMode === 'details' && planData">
      <div class="header-card bg-white shadow-sm rounded-lg p-5 mb-6 border border-slate-200">
        <div class="flex justify-between items-start">
          <div>
            <h1 class="text-2xl font-bold text-slate-800 tracking-tight">Résultat Contrôle Poste</h1>
            <p class="text-slate-500 mt-1 font-medium">Poste: {{ docHeader.posteCode }} - Equipe {{ docHeader.equipe }}</p>
          </div>
          <div class="text-right flex flex-col items-end gap-2">
            <div class="flex items-center gap-3">
              <Button 
                :label="`Voir toutes les cartes (${availableSessions.length})`" 
                icon="pi pi-th-large" 
                class="p-button-outlined p-button-secondary bg-white shadow-sm border-slate-300 !py-1" 
                @click="switchToGalleryMode" 
              />
              <span class="inline-flex items-center px-3 py-1.5 rounded-full text-sm font-semibold bg-blue-50 text-blue-700">
                <i class="pi pi-calendar mr-2"></i> {{ docHeader.dateExecution }}
              </span>
            </div>
            <div class="flex items-center gap-2">
              <button 
                v-if="isConsultationMode"
                @click="isConsultationMode = false; isDocumentTermine = false"
                class="px-3 py-1.5 rounded-lg text-xs font-bold bg-amber-500 text-white shadow-sm hover:bg-amber-600 transition-all flex items-center gap-2"
              >
                <i class="pi pi-pencil"></i>
                <span>Modifier</span>
              </button>
            </div>
            <div v-if="isDocumentTermine" class="mt-1">
              <span class="px-3 py-1 bg-green-100 text-green-700 rounded text-sm font-bold">Terminé</span>
            </div>
          </div>
        </div>
        <div class="grid grid-cols-2 md:grid-cols-4 gap-4 mt-6 pt-4 border-t border-slate-100">
          <div>
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">Code Article</span>
            <span class="text-slate-800 font-medium">{{ docHeader.codeArticle }}</span>
          </div>
          <div class="md:col-span-2">
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">Désignation</span>
            <span class="text-slate-800 font-medium">{{ docHeader.designationArticle }}</span>
          </div>
          <div>
            <span class="block text-xs font-semibold text-slate-400 uppercase tracking-wider mb-1">N° OF</span>
            <span class="text-slate-800 font-medium">{{ docHeader.numeroOf }}</span>
          </div>
        </div>
      </div>

      <div class="bg-white shadow-sm rounded-lg border border-slate-200 overflow-hidden flex-1 flex flex-col relative border-l-4 border-l-emerald-500">
        <!-- Main Header -->
        <div class="bg-slate-800 px-5 py-4 flex items-center relative min-h-[64px]">
            <div class="flex-1 text-center">
                <h2 class="text-white font-bold uppercase tracking-widest text-lg">
                  Résultat de contrôle - Poste {{ docHeader.posteCode }}
                </h2>
            </div>
        </div>
        <!-- Scrollable Table Container -->
        <div class="overflow-auto custom-scrollbar flex-1 p-0 relative min-h-[400px] pb-3">
          <table class="w-full text-left border-collapse min-w-[800px] text-sm whitespace-nowrap">
            <thead class="bg-slate-50 sticky top-0 z-10 shadow-sm text-slate-700 text-[11px] font-bold border-b-2 border-slate-200">
              <tr>
                <th colspan="3" class="p-2 border-b border-r text-center align-middle">
                    <div class="text-[12px] font-black text-slate-800 tracking-wide uppercase">Test de Non-conformité</div>
                </th>
                
                <template v-for="(g, idx) in headerGroups" :key="'g'+idx">
                  <th :colspan="g.colspan" class="border-b border-r text-center align-top relative p-2 bg-slate-100 min-w-[200px]">
                    <div class="mb-2 text-[13px] font-black text-slate-800 uppercase">{{ g.name }}</div>
                    <div class="flex items-center justify-center gap-4 mt-2 px-2 pb-1 text-[10px] normal-case font-semibold">
                        <div class="flex items-center gap-1.5 whitespace-nowrap">
                            <label class="text-slate-500">Nom et prénom :</label>
                            <input type="text" :value="g.numEquipe == equipe ? (authStore.user?.prenom ? authStore.user.prenom + ' ' : '') + (authStore.user?.nom || '') : ''" disabled
                                class="border-b border-slate-300 bg-transparent px-1 py-0.5 outline-none text-emerald-700 font-bold w-32" />
                        </div>
                        <div class="flex items-center gap-1.5 whitespace-nowrap">
                            <label class="text-slate-500">Matricule :</label>
                            <input type="text" :value="g.numEquipe == equipe ? authStore.user?.matricule : ''" disabled
                                class="border-b border-slate-300 bg-transparent px-1 py-0.5 outline-none text-emerald-700 font-bold w-20" />
                        </div>
                    </div>
                  </th>
                </template>
                
                <th class="p-2 border-b border-r text-center w-24 align-middle" rowspan="2">Total des défauts</th>
                <th class="p-2 border-b border-r text-center w-28 align-middle" rowspan="2">Total des pièces testées</th>
                <th class="p-2 border-b border-r text-center w-24 align-middle" rowspan="2">Taux de NC</th>
                <th class="p-2 border-b border-r text-center w-24 align-middle" rowspan="2">NB des pièces rebutées</th>
                <th class="p-2 border-b text-center w-24 align-middle" rowspan="2">NB des pièces conformes</th>
              </tr>
              <tr>
                <th class="p-2 border-b border-r text-center align-middle w-10 bg-slate-50/50">N°</th>
                <th class="p-2 border-b border-r text-center align-middle min-w-[120px] bg-slate-50/50">Machine<br/>Banc d'essai</th>
                <th class="p-2 border-b border-r text-center align-middle min-w-[150px] bg-slate-50/50">Désignation du défaut</th>
                <template v-for="(g, idx) in headerGroups" :key="'subg'+idx">
                  <th v-for="col in g.columns" :key="col.id" 
                      class="p-2 border-b border-r text-center font-semibold min-w-[5rem] text-[10px] transition-all"
                      :class="isCurrentTranche(col.header) && col.numEquipe == equipe ? 'bg-emerald-100 text-emerald-900 border-b-2 border-emerald-500 shadow-sm' : 'bg-slate-50/50'">
                    {{ col.header }}
                  </th>
                </template>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(ligne, idx) in formLignes" :key="ligne.docLigneId" class="border-b hover:bg-slate-50 transition-colors">
                <td class="p-2 border-r text-center text-slate-500 font-bold">{{ idx + 1 }}</td>
                <td class="p-2 border-r text-slate-700 text-xs text-center font-semibold">{{ ligne.machineCodeCtrlPoste || '-' }}</td>
                <td class="p-4 text-slate-800 border-r font-medium text-sm text-center">{{ ligne.libelleAffiche }}</td>
                
                <template v-for="(g, gIdx) in headerGroups" :key="'rg'+gIdx">
                  <template v-if="g.numEquipe == equipe">
                    <td v-for="col in g.columns" :key="col.id" class="p-1 border-r text-center transition-all" 
                        :class="isCurrentTranche(col.header) ? 'bg-emerald-50 ring-2 ring-inset ring-emerald-300' : ''">
                      <input type="number" v-model.number="ligne.heures[col.header]" :disabled="isDocumentTermine" 
                             class="w-full text-center border-0 bg-transparent hover:bg-white focus:bg-white focus:ring-1 focus:ring-emerald-400 p-1.5 rounded transition-all text-xs font-semibold text-gray-800 outline-none" 
                             min="0" />
                    </td>
                  </template>
                  <template v-else>
                    <td v-for="col in g.columns" :key="col.id" class="p-1 border-r text-center bg-slate-50/50">
                      <input type="number" :value="ligne.heures[col.header]" disabled 
                             class="w-full text-center border-0 bg-transparent p-1.5 rounded text-xs font-semibold text-gray-800 outline-none" />
                    </td>
                  </template>
                </template>

                <!-- Total des défauts (par ligne) -->
                <td class="p-4 text-center font-bold text-slate-700 bg-slate-50 border-r">
                  {{ activeTranchesComputed.reduce((sum, t) => sum + (Number(ligne.heures[t]) || 0), 0) }}
                </td>
                <td class="bg-white border-r"></td>
                <td class="bg-white border-r"></td>
                <td class="bg-white border-r"></td>
                <td class="bg-white"></td>
              </tr>
              
              <!-- Row: Total Non conforme -->
              <tr class="bg-red-50 border-t-2 border-slate-200">
                <td colspan="3" class="p-4 font-bold text-slate-700 text-right border-r">Total Non conforme :</td>
                
                <template v-for="(g, gIdx) in headerGroups" :key="'t1'+gIdx">
                  <template v-if="g.numEquipe == equipe">
                    <td v-for="col in g.columns" :key="col.id" class="p-4 border-r text-center font-bold text-red-600 text-lg">
                      {{ formLignes.reduce((sum, l) => sum + (Number(l.heures[col.header]) || 0), 0) }}
                    </td>
                  </template>
                  <template v-else>
                    <td v-for="col in g.columns" :key="col.id" class="p-4 border-r text-center font-bold text-red-600 text-lg bg-slate-50/50">
                      {{ formLignes.reduce((sum, l) => sum + (Number(l.heures[col.header]) || 0), 0) }}
                    </td>
                  </template>
                </template>

                <!-- Total global des défauts -->
                <td class="p-4 text-center font-bold text-red-700 border-r text-lg">
                    {{ formBilan.totalDefauts }}
                </td>
                <td class="p-4 text-center font-bold text-blue-700 border-r text-lg">
                    {{ formBilan.totalPiecesTestees }}
                </td>
                <td class="p-4 text-center font-bold text-slate-700 border-r text-lg">
                    {{ formBilan.tauxNc }}%
                </td>
                <td class="p-2 border-r text-center bg-white">
                  <input type="number" v-model.number="formBilan.nbPiecesRebutees" :disabled="isDocumentTermine" 
                         class="w-full text-center border border-slate-300 bg-white p-2 rounded focus:ring-2 focus:ring-red-400 focus:outline-none transition-all text-lg font-bold text-red-600" 
                         min="0" />
                </td>
                <td class="p-4 text-center font-bold text-green-600 text-lg">
                    {{ formBilan.totalPiecesTestees - (formBilan.nbPiecesRebutees || 0) }}
                </td>
              </tr>

              <!-- Row: Total réalisé -->
              <tr class="bg-slate-100 border-t border-slate-200">
                <td colspan="3" class="p-4 font-bold text-slate-700 text-right border-r">Total réalisé :</td>
                
                <template v-for="(g, gIdx) in headerGroups" :key="'t2'+gIdx">
                  <template v-if="g.numEquipe == equipe">
                      <td v-for="col in g.columns" :key="col.id" class="p-1 border-r text-center">
                        <input type="number" v-model.number="formReponses[col.header]" :disabled="isDocumentTermine" 
                               :class="['w-full text-center p-2 rounded text-sm font-bold text-gray-800 outline-none transition-colors', isDocumentTermine ? 'bg-transparent border-0' : 'border border-slate-300 bg-white hover:border-emerald-400 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500']" 
                               min="0" />
                      </td>
                  </template>
                  <template v-else>
                      <td v-for="col in g.columns" :key="col.id" class="p-1 border-r text-center bg-slate-50/50">
                        <input type="number" :value="formReponses[col.header]" disabled 
                               class="w-full text-center border-0 bg-transparent p-2 rounded text-sm font-bold text-gray-800 outline-none" />
                      </td>
                  </template>
                </template>

                <td class="bg-slate-100 border-r"></td>
                <td class="bg-slate-100 border-r"></td>
                <td class="bg-slate-100 border-r"></td>
                <td class="bg-slate-100 border-r"></td>
                <td class="bg-slate-100"></td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Action Buttons -->
      <div v-if="!isDocumentTermine" class="flex justify-end gap-3 mt-6 pb-8">
        <Button label="Enregistrer" icon="pi pi-save" @click="handleValider" :loading="saving" :disabled="isDocumentTermine" class="p-button-outlined" />
        <Button label="Marquer comme terminé" icon="pi pi-check" @click="marquerTermine" :disabled="isDocumentTermine" severity="success" />
      </div>

    </template>

    <!-- Dialog Plan Manquant -->
    <Dialog v-model:visible="showPlanManquantDialog" modal header="Contrôle au Poste" :style="{ width: '50vw' }">
      <div class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300 m-2">
        <i class="pi pi-info-circle text-5xl text-gray-400 mb-4" style="display:block"></i>
        <p class="text-gray-600 text-lg mb-2">Aucun plan de contrôle au poste n'est paramétré pour <strong>{{ posteCode }}</strong>.</p>
        <p class="text-gray-500 text-sm mb-6">Contactez votre superviseur pour qu'il crée ou active le document associé à ce poste.</p>
        <Button 
          label="Signaler au Superviseur" 
          icon="pi pi-exclamation-triangle" 
          severity="danger" 
          outlined 
          :loading="isSignalingPlan"
          @click="signalerPlanManquant" 
        />
      </div>
    </Dialog>

  </div>
</template>

<style scoped>
.rc-poste-view {
  min-height: calc(100vh - 4rem);
}
:deep(.p-inputnumber-input) {
  width: 100%;
  text-align: center;
}
/* Hide number spinners for better readability of large numbers */
input[type="number"]::-webkit-outer-spin-button,
input[type="number"]::-webkit-inner-spin-button {
  -webkit-appearance: none;
  margin: 0;
}
input[type="number"] {
  -moz-appearance: textfield;
}

/* Scrollbar personnalisée pour qu'elle soit toujours visible et claire */
.custom-scrollbar::-webkit-scrollbar {
  height: 12px;
  width: 12px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: #f1f5f9;
  border-radius: 8px;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background: #94a3b8;
  border-radius: 8px;
  border: 2px solid #f1f5f9;
}
.custom-scrollbar::-webkit-scrollbar-thumb:hover {
  background: #64748b;
}
</style>
