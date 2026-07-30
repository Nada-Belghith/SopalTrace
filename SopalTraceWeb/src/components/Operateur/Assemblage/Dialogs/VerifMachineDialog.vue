<script setup>
import { defineProps, defineEmits, ref } from 'vue'
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
  }
})

const emit = defineEmits(['update:visible', 'ouvrir-document', 'init-document', 'remove-machine', 'add-machine', 'cloturer-machine'])

const selectedNewMachine = ref(null)


const getDocs = (machineCode) => {
  const cat = props.documentCategories.find(c => c.id === 'verifMachine')
  return cat?.docs.filter(d => d.machineCode === machineCode) || []
}

const hasDocForCurrentSession = (machineCode) => {
  const docs = getDocs(machineCode)
  const todayStr = new Date().toISOString().split('T')[0]
  return docs.some(d => d.equipe === props.sessionEquipe && d.dateExecution && d.dateExecution.split('T')[0] === todayStr)
}

const hasUnfinishedDoc = (machineCode) => {
  const docs = getDocs(machineCode)
  return docs.some(d => !d.estTermine)
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
      
      <div class="space-y-4 mb-6">
        <div v-for="machine in machinesPoste" :key="machine.codeMachine" class="flex justify-between items-center p-4 border rounded-xl shadow-sm bg-white hover:bg-gray-50">
          <div class="font-bold text-gray-800 text-lg uppercase">{{ machine.codeMachine }}</div>
          
          <div class="flex flex-wrap gap-2 justify-end">
            <!-- Boutons pour chaque document existant -->
            <template v-for="doc in getDocs(machine.codeMachine)" :key="doc.id">
              <Button 
                :label="(doc.estTermine ? 'Consulter' : 'Ouvrir') + ' (' + (doc.equipe || 'N/A') + ' - ' + formatDate(doc.dateExecution) + ')'" 
                :icon="doc.estTermine ? 'pi pi-eye' : 'pi pi-pencil'" 
                :severity="doc.estTermine ? 'secondary' : 'primary'" 
                size="small" 
                @click="emit('ouvrir-document', doc)" 
              />
            </template>

            <!-- Bouton Tous les documents si au moins 1 document existe -->
            <Button 
              v-if="getDocs(machine.codeMachine).length > 0" 
              label="Tous les documents" 
              icon="pi pi-th-large" 
              severity="info" 
              size="small" 
              @click="emit('ouvrir-document', { typeDocument: 'VERIF_MACHINE', machineCode: machine.codeMachine, ...(getDocs(machine.codeMachine)[0] || {}), _openMode: 'all' })" 
              v-tooltip.top="'Voir toutes les cartes des rapports de cette machine'"
            />

            <!-- Bouton Clôturer la machine s'il reste au moins un doc non terminé -->
            <Button 
              v-if="hasUnfinishedDoc(machine.codeMachine)" 
              label="Marquer comme Terminé" 
              icon="pi pi-check-circle" 
              severity="success" 
              size="small" 
              @click="emit('cloturer-machine', machine.codeMachine)" 
              v-tooltip.top="'Valider et clôturer tous les documents de cette machine'"
            />

            <!-- Bouton Initialiser disponible dès qu'aucun document n'existe pour la session/équipe courante aujourd'hui -->
            <Button 
              v-if="!hasDocForCurrentSession(machine.codeMachine)" 
              label="Initialiser" 
              icon="pi pi-play" 
              severity="success" 
              size="small" 
              @click="emit('init-document', machine.codeMachine)" 
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
