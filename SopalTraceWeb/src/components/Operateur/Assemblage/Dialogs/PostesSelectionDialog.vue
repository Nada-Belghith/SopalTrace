<script setup>
import { defineProps, defineEmits, ref, watch } from 'vue'
import Dialog from 'primevue/dialog'
import Tag from 'primevue/tag'
import MultiSelect from 'primevue/multiselect'
import Button from 'primevue/button'
import Dropdown from 'primevue/dropdown'

const props = defineProps({
  visible: {
    type: Boolean,
    required: true
  },
  ofClique: {
    type: Object,
    default: null
  },
  postesExistants: {
    type: Array,
    default: () => []
  },
  postesAAjouter: {
    type: Array,
    default: () => []
  },
  isDejaEnCours: {
    type: Boolean,
    default: false
  },
  isSubmitting: {
    type: Boolean,
    default: false
  }
})

const emit = defineEmits(['update:visible', 'confirmer'])

const postesSelectionnes = ref([])
const equipeSelectionnee = ref(null)

watch(() => props.visible, (newVal) => {
  if (newVal) {
    postesSelectionnes.value = []
    equipeSelectionnee.value = null
  }
})

const fermer = () => {
  emit('update:visible', false)
}

const confirmer = () => {
  emit('confirmer', { postes: postesSelectionnes.value, equipe: equipeSelectionnee.value })
}
</script>

<template>
  <Dialog
    :visible="visible"
    @update:visible="val => emit('update:visible', val)"
    modal
    :header="isDejaEnCours ? `Gérer les postes — ${ofClique?.numeroOf}` : `Démarrer l'assemblage — ${ofClique?.numeroOf}`"
    :style="{ width: '480px' }"
    :closable="!isSubmitting"
  >
    <div class="flex flex-col gap-5 mt-2">
      <!-- Infos OF -->
      <div class="bg-gray-50 rounded-lg p-3 text-sm text-gray-600">
        <span class="font-semibold text-gray-800">Article :</span> {{ ofClique?.designationArticle }}<br>
        <span class="font-semibold text-gray-800">Qté lancée :</span> {{ ofClique?.quantiteLancee ?? '-' }}
      </div>

      <!-- Postes existants (lecture seule, si EN COURS) -->
      <div v-if="isDejaEnCours && postesExistants.length > 0">
        <label class="block text-sm font-bold text-gray-700 mb-2">
          <i class="pi pi-lock mr-1 text-green-600"></i> Postes déjà enregistrés (non modifiables)
        </label>
        <div class="flex flex-wrap gap-2 bg-green-50 border border-green-200 rounded-lg p-3">
          <Tag v-for="p in postesExistants" :key="p" :value="p" severity="success" />
        </div>
      </div>

      <!-- Sélection de nouveaux postes -->
      <div>
        <label class="block text-sm font-bold text-gray-700 mb-2">
          <i class="pi pi-plus-circle mr-1 text-primary"></i>
          {{ isDejaEnCours ? 'Ajouter des postes supplémentaires (optionnel)' : 'Sélectionner les postes de travail *' }}
        </label>
        <MultiSelect
          v-model="postesSelectionnes"
          :options="postesAAjouter"
          optionLabel="libelle"
          optionValue="codePoste"
          :placeholder="isDejaEnCours ? 'Ajouter un poste...' : 'Sélectionner des postes'"
          :maxSelectedLabels="5"
          class="w-full"
          display="chip"
        />
        <small v-if="!isDejaEnCours" class="text-gray-500 mt-1 block">
          Sélectionnez tous les postes concernés par cet OF.
        </small>
      </div>

      <!-- Sélection de l'équipe -->
      <div>
        <label class="block text-sm font-bold text-gray-700 mb-2">
          <i class="pi pi-users mr-1 text-primary"></i>
          Sélectionner votre équipe *
        </label>
        <Dropdown
          v-model="equipeSelectionnee"
          :options="['E1', 'E2', 'E3']"
          placeholder="Choisir l'équipe..."
          class="w-full"
        />
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 mt-4">
        <Button label="Annuler" icon="pi pi-times" severity="secondary" text @click="fermer" :disabled="isSubmitting" />
        <Button
          :label="isDejaEnCours ? (postesSelectionnes?.length > 0 ? 'Ajouter et Reprendre' : 'Reprendre') : 'Démarrer l\'assemblage'"
          :icon="isDejaEnCours ? 'pi pi-play' : 'pi pi-play'"
          severity="primary"
          @click="confirmer"
          :loading="isSubmitting"
          :disabled="!equipeSelectionnee"
        />
      </div>
    </template>
  </Dialog>
</template>
