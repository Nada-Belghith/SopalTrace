<script setup>
/** Choix d'un fichier : un vrai bouton, le nom du fichier choisi (ou « aucun fichier »), et le changer. */
import { ref } from 'vue'

const fichier = defineModel({ type: [File, null], default: null })
defineProps({
  libelle: { type: String, required: true },
  accept: { type: String, required: true },
  icone: { type: String, default: 'pi-file' }
})
const entree = ref(null)

const choisir = (e) => {
  fichier.value = e.target.files?.[0] ?? null
  e.target.value = ''
}
</script>

<template>
  <div class="flex flex-wrap items-center gap-3">
    <input ref="entree" type="file" :accept="accept" class="sr-only" tabindex="-1" aria-hidden="true" @change="choisir" />
    <button type="button" @click="entree?.click()"
            class="min-h-[44px] px-4 rounded-lg border-2 border-dashed border-blue-300 bg-white hover:bg-blue-50 text-blue-700 font-bold text-sm cursor-pointer flex items-center gap-2">
      <i class="pi" :class="icone"></i> {{ fichier ? 'Changer de fichier' : libelle }}
    </button>
    <span v-if="fichier" class="text-sm font-semibold text-slate-700 flex items-center gap-1 min-w-0">
      <i class="pi pi-check-circle text-emerald-600 shrink-0"></i><span class="truncate">{{ fichier.name }}</span>
    </span>
    <span v-else class="text-sm text-slate-500">Aucun fichier choisi</span>
  </div>
</template>
