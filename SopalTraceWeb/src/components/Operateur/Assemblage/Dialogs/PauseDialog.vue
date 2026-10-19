<script setup>
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import RadioButton from 'primevue/radiobutton'
import { TYPES_PAUSE } from '@/constants/execution'

const visible = defineModel('visible', { type: Boolean, default: false })
const typePause = defineModel('typePause', { type: String, default: TYPES_PAUSE.DEJEUNER })

const emit = defineEmits(['confirmer'])

const options = [
  { value: TYPES_PAUSE.DEJEUNER, id: 'pauseDejeuner', label: 'Pause Déjeuner', icon: 'pi pi-sun text-amber-500' },
  { value: TYPES_PAUSE.AUTRE, id: 'pauseAutre', label: 'Autre Pause (Maintenance, etc.)', icon: 'pi pi-coffee text-slate-500' }
]
</script>

<template>
  <Dialog v-model:visible="visible" modal header="Mettre en Pause" :style="{ width: '400px' }">
    <div class="flex flex-col gap-4 mt-2">
      <p class="text-gray-600">Sélectionnez le type de pause. Cela figera le chronomètre des contrôles d'échantillonnage.</p>
      <div class="flex flex-col gap-3">
        <div
          v-for="option in options"
          :key="option.value"
          class="flex items-center gap-2 border p-3 rounded-lg cursor-pointer transition hover:bg-slate-50"
          :class="{ 'border-blue-500 bg-blue-50': typePause === option.value }"
          @click="typePause = option.value"
        >
          <RadioButton v-model="typePause" :inputId="option.id" name="pause" :value="option.value" />
          <label :for="option.id" class="cursor-pointer font-semibold flex items-center gap-2">
            <i :class="option.icon"></i> {{ option.label }}
          </label>
        </div>
      </div>
    </div>
    <template #footer>
      <div class="flex justify-end gap-2 mt-4">
        <Button label="Annuler" icon="pi pi-times" text severity="secondary" @click="visible = false" />
        <Button label="Confirmer la Pause" icon="pi pi-pause" severity="warning" @click="emit('confirmer')" />
      </div>
    </template>
  </Dialog>
</template>
