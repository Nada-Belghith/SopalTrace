<script setup>
import { defineProps, defineEmits, ref, computed } from 'vue'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Dropdown from 'primevue/dropdown'
import Tooltip from 'primevue/tooltip'

const vTooltip = Tooltip

const props = defineProps({
  visible: {
    type: Boolean,
    required: true
  },
  machinesPoste: {
    type: Array,
    default: () => []
  },
  allMachines: {
    type: Array,
    default: () => []
  },
  documentCategories: {
    type: Array,
    default: () => []
  },
  sessionEquipe: {
    type: String,
    default: ''
  },
  isDemarrageMode: {
    type: Boolean,
    default: false
  },
  targetPeriodicite: {
    type: String,
    default: ''
  }
})

const emit = defineEmits(['update:visible', 'ouvrir-document', 'init-document', 'remove-machine', 'add-machine', 'cloturer-machine', 'signaler-plan-manquant', 'marquer-termine', 'rouvrir-document'])

const selectedNewMachine = ref(null)

const displayedMachines = computed(() => {
  if (props.isDemarrageMode) {
    // Affiche uniquement :
    // 1. Les machines avec plan de démarrage
    // 2. Les machines qui n'ont AUCUN plan (pour pouvoir les signaler)
    // Cache les machines qui ont d'autres plans (ex: MAS19) mais pas de démarrage, car c'est volontaire de la part du superviseur.
    return props.machinesPoste.filter(m => 
      m.hasDemarragePlan || 
      !(m.hasDemarragePlan || m.hasApresPausePlan || m.hasFinPostePlan)
    )
  }
  return props.machinesPoste
})


const getDocs = (machineCode) => {
  const cat = props.documentCategories.find(c => c.id === 'verifMachine')
  return cat?.docs.filter(d => d.machineCode === machineCode) || []
}

const hasDocForCurrentSession = (machineCode) => {
  const docs = getDocs(machineCode)
  const todayStr = new Date().toISOString().split('T')[0]
  return docs.some(d => d.equipe === props.sessionEquipe && d.dateExecution && d.dateExecution.split('T')[0] === todayStr)
}

const isMachineTerminee = (machineCode) => {
  const docs = getDocs(machineCode)
  if (docs.length === 0) return false
  if (props.isDemarrageMode) {
    return docs.every(d => d.estDemarrageTermine || d.estTermine)
  }
  return docs.every(d => d.estTermine)
}

const formatDate = (dateStr) => {
  if (!dateStr) return ''
  const d = new Date(dateStr)
  return d.toLocaleDateString('fr-FR')
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="val => emit('update:visible', val)"
    modal
    header="Machines du Poste"
    :style="{ width: '600px' }"
  >
    <div class="pt-4">
      <p class="mb-4 text-gray-600">Sélectionnez la machine pour laquelle vous souhaitez effectuer la vérification.</p>
      
      <div v-if="displayedMachines.length === 0" class="p-6 text-center text-gray-500 bg-gray-50 rounded-xl border border-dashed border-gray-300">
        <i class="pi pi-exclamation-circle text-4xl mb-3 text-gray-400"></i>
        <p v-if="isDemarrageMode">Aucune machine de ce poste ne possède de plan avec une étape "Démarrage".</p>
        <p v-else>Aucune machine n'est affectée à ce poste.</p>
      </div>

      <div v-else class="space-y-4 mb-6">
        <div v-for="machine in displayedMachines" :key="machine.codeMachine" class="flex justify-between items-center p-4 border rounded-xl shadow-sm bg-white hover:bg-gray-50" :class="isMachineTerminee(machine.codeMachine) ? 'border-emerald-300 bg-emerald-50/40' : ''">
          <div class="font-bold text-gray-800 text-lg uppercase flex items-center gap-2">
            <span>{{ machine.codeMachine }}</span>
            <span v-if="isMachineTerminee(machine.codeMachine)" class="px-2.5 py-0.5 bg-emerald-100 text-emerald-800 text-xs font-bold rounded-full border border-emerald-300 flex items-center gap-1">
              <i class="pi pi-check text-emerald-600 text-xs"></i> Répondu
            </span>
          </div>
          
          <div class="flex flex-wrap gap-2 justify-end items-center">
            <!-- Si la machine est clôturée / terminée, afficher uniquement le bouton Tous les documents -->
            <template v-if="isMachineTerminee(machine.codeMachine)">
              <Button 
                label="Tous les documents" 
                icon="pi pi-check-circle" 
                severity="success" 
                size="small" 
                @click="emit('ouvrir-document', { typeDocument: 'VERIF_MACHINE', machineCode: machine.codeMachine, ...(getDocs(machine.codeMachine)[0] || {}), _openMode: 'all', targetPeriodicite: props.targetPeriodicite || (props.isDemarrageMode ? 'demarrage' : '') })" 
                v-tooltip.top="'Consulter tous les rapports de cette machine'"
              />
            </template>

            <!-- Sinon, afficher les boutons individuels + Tous les documents -->
            <template v-else>
              <!-- Boutons pour chaque document existant -->
              <template v-for="doc in getDocs(machine.codeMachine)" :key="doc.id">
                <Button 
                  v-if="!doc.estDemarrageTermine && !doc.estTermine" 
                  icon="pi pi-check" 
                  severity="success" 
                  outlined 
                  size="small" 
                  @click="emit('marquer-termine', doc.id)" 
                  v-tooltip.top="'Marquer comme terminé'" 
                />
                <Button 
                  v-if="doc.estTermine" 
                  icon="pi pi-undo" 
                  severity="warning" 
                  outlined 
                  size="small" 
                  @click="emit('rouvrir-document', doc.id)" 
                  v-tooltip.top="'Réouvrir le document'" 
                />
                <Button 
                  :label="(doc.estTermine ? 'Consulter' : 'Ouvrir') + ' (' + (doc.equipe || 'N/A') + ' - ' + formatDate(doc.dateExecution) + ')'" 
                  :icon="doc.estTermine ? 'pi pi-eye' : 'pi pi-pencil'" 
                  :severity="doc.estTermine ? 'secondary' : 'primary'" 
                  size="small" 
                  @click="emit('ouvrir-document', { ...doc, targetPeriodicite: props.targetPeriodicite || (props.isDemarrageMode ? 'demarrage' : '') })" 
                />
              </template>

              <!-- Bouton Tous les documents si au moins 1 document existe -->
              <Button 
                v-if="getDocs(machine.codeMachine).length > 0" 
                label="Tous les documents" 
                icon="pi pi-th-large" 
                severity="info" 
                size="small" 
                @click="emit('ouvrir-document', { typeDocument: 'VERIF_MACHINE', machineCode: machine.codeMachine, ...(getDocs(machine.codeMachine)[0] || {}), _openMode: 'all', targetPeriodicite: props.targetPeriodicite || (props.isDemarrageMode ? 'demarrage' : '') })" 
                v-tooltip.top="'Voir toutes les cartes des rapports de cette machine'"
              />
            </template>



            <!-- Bouton Initialiser disponible dès qu'aucun document n'existe pour la session/équipe courante aujourd'hui -->
            <Button 
              v-if="!hasDocForCurrentSession(machine.codeMachine) && (machine.hasDemarragePlan || machine.hasApresPausePlan || machine.hasFinPostePlan)" 
              label="Initialiser" 
              icon="pi pi-play" 
              severity="success" 
              size="small" 
              @click="emit('init-document', machine.codeMachine)" 
            />

            <!-- Bouton Signaler Plan Manquant (pour signaler au superviseur l'oubli) -->
            <Button 
              v-if="!(machine.hasDemarragePlan || machine.hasApresPausePlan || machine.hasFinPostePlan) && getDocs(machine.codeMachine).length === 0" 
              label="Signaler" 
              icon="pi pi-exclamation-triangle" 
              severity="danger" 
              outlined
              size="small" 
              @click="emit('signaler-plan-manquant', machine.codeMachine)" 
              v-tooltip.top="'Signaler au superviseur que cette machine n\'a aucun plan paramétré'"
            />

            <!-- Bouton supprimer si ajout manuel sans doc -->
            <Button 
              v-if="!machine.isDefault && getDocs(machine.codeMachine).length === 0" 
              icon="pi pi-trash" 
              severity="danger" 
              text rounded
              @click="emit('remove-machine', machine.codeMachine)" 
              v-tooltip.top="'Retirer cette machine'"
            />
          </div>
        </div>
        
        <div v-if="machinesPoste.length === 0" class="text-center p-4 text-gray-500 italic">
          Aucune machine n'est configurée par défaut pour ce poste.
        </div>
      </div>
      
      <div class="border-t pt-4">
        <label class="block text-sm font-bold text-gray-700 mb-2">Ajouter une autre machine</label>
        <div class="flex gap-2">
          <Dropdown 
            v-model="selectedNewMachine" 
            :options="allMachines" 
            optionLabel="libelle" 
            optionValue="codeMachine" 
            filter 
            placeholder="Sélectionnez une machine..." 
            class="flex-1"
            @change="emit('add-machine', selectedNewMachine); selectedNewMachine = null;"
          />
        </div>
      </div>
    </div>
  </Dialog>
</template>
