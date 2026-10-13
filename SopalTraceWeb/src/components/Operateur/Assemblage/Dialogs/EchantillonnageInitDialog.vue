<script setup>
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import InputNumber from 'primevue/inputnumber'
import MultiSelect from 'primevue/multiselect'

const visible = defineModel('visible', { type: Boolean, default: false })
const nbPostes = defineModel('nbPostes', { type: Number, default: 1 })
const instruments = defineModel('instruments', { type: Array, default: () => [] })

defineProps({
  instrumentsDisponibles: { type: Array, default: () => [] },
  isSubmitting: { type: Boolean, default: false }
})

const emit = defineEmits(['generer'])
</script>

<template>
  <Dialog v-model:visible="visible" modal header="Fiche Échantillonnage" :style="{ width: '400px' }" :closable="!isSubmitting">
    <div class="flex flex-col gap-4 mt-2">
      <p class="text-gray-600">Nombre de postes utilisé pour calculer l'effectif par poste.</p>
      <div class="flex flex-col gap-2">
        <label class="font-semibold text-gray-800">Nombre de postes</label>
        <InputNumber v-model="nbPostes" :min="1" :max="50" showButtons buttonLayout="horizontal"
          decrementButtonClass="p-button-secondary" incrementButtonClass="p-button-secondary"
          incrementButtonIcon="pi pi-plus" decrementButtonIcon="pi pi-minus" class="w-full" />
      </div>
      <div class="flex flex-col gap-2">
        <label class="font-semibold text-gray-800">Instruments de mesure</label>
        <MultiSelect v-model="instruments" :options="instrumentsDisponibles" optionLabel="designation" optionValue="codeInstrument"
          placeholder="Sélectionner des instruments" :maxSelectedLabels="3" class="w-full" filter />
      </div>
    </div>
    <template #footer>
      <div class="flex justify-end gap-2 mt-4">
        <Button label="Annuler" icon="pi pi-times" text severity="secondary" @click="visible = false" :disabled="isSubmitting" />
        <Button label="Générer Fiche" icon="pi pi-check" @click="emit('generer')" :loading="isSubmitting" />
      </div>
    </template>
  </Dialog>
</template>
