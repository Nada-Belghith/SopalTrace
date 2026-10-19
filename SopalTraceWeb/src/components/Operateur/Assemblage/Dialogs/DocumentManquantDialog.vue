<script setup>
import { computed } from 'vue'
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'

const props = defineProps({
  /** { titre, message, detail?, aSignaler? } ; null ferme le dialogue. */
  document: { type: Object, default: null },
  isSignaling: { type: Boolean, default: false }
})

const emit = defineEmits(['signaler', 'fermer'])

const visible = computed({
  get: () => props.document !== null,
  set: (valeur) => { if (!valeur) emit('fermer') }
})
</script>

<template>
  <Dialog v-model:visible="visible" modal :header="document?.titre" :style="{ width: '520px' }" :closable="!isSignaling">
    <div v-if="document" class="text-center p-8 bg-gray-50 rounded-xl border border-dashed border-gray-300 m-2">
      <i class="pi pi-exclamation-triangle text-5xl text-amber-500 mb-4" style="display:block"></i>
      <p class="text-gray-700 font-medium text-lg mb-2">{{ document.message }}</p>
      <div v-if="document.aSignaler" class="bg-amber-50 border border-amber-200 text-amber-800 p-3 rounded-lg text-sm mb-4 text-left">
        <strong>À signaler :</strong> {{ document.aSignaler }}
      </div>
      <p v-if="document.detail" class="text-gray-500 text-sm mb-6">{{ document.detail }}</p>
      <Button label="Signaler au Superviseur" icon="pi pi-send" severity="danger" :loading="isSignaling" @click="emit('signaler')" />
    </div>
  </Dialog>
</template>
