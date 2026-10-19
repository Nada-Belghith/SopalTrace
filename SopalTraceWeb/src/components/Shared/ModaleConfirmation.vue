<script setup>
import Dialog from 'primevue/dialog'
import Button from 'primevue/button'
import Textarea from 'primevue/textarea'
import { useConfirmation } from '@/composables/useConfirmation'

const { etat, valider, annuler } = useConfirmation()

// Fermeture par la croix ou Échap = annulation
const surVisibilite = (visible) => { if (!visible) annuler() }
</script>

<template>
  <Dialog :visible="etat.visible" @update:visible="surVisibilite" modal :header="etat.options.titre"
          :style="{ width: '460px' }" :breakpoints="{ '575px': '92vw' }" :draggable="false">
    <div class="flex items-start gap-3 mt-1">
      <span class="shrink-0 w-10 h-10 rounded-full flex items-center justify-center"
            :class="etat.options.danger ? 'bg-red-50 text-red-600' : 'bg-blue-50 text-blue-600'">
        <i :class="etat.options.danger ? 'pi pi-exclamation-triangle' : 'pi pi-question-circle'" class="text-lg"></i>
      </span>
      <div class="min-w-0 flex-1 pt-1.5">
        <p v-if="etat.options.message" class="m-0 text-slate-600 leading-relaxed">{{ etat.options.message }}</p>
        <div v-if="etat.options.details?.length" class="mt-2 flex flex-wrap gap-1.5">
          <span v-for="d in etat.options.details" :key="d"
                class="px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 text-sm font-semibold">{{ d }}</span>
        </div>

        <div v-if="etat.options.saisie" class="mt-4">
          <label for="confirmation-saisie" class="block text-sm font-semibold text-slate-700 mb-1">
            {{ etat.options.saisie.libelle }}<span v-if="etat.options.saisie.obligatoire" class="text-red-600"> *</span>
          </label>
          <Textarea id="confirmation-saisie" v-model="etat.valeur" rows="3" autoResize class="w-full"
                    :maxlength="etat.options.saisie.longueurMax" :placeholder="etat.options.saisie.placeholder"
                    :invalid="!!etat.erreur" @input="etat.erreur = ''" />
          <small v-if="etat.erreur" class="text-red-600">{{ etat.erreur }}</small>
        </div>
      </div>
    </div>

    <template #footer>
      <div class="flex justify-end gap-2 mt-4">
        <Button :label="etat.options.libelleAnnuler || 'Annuler'" icon="pi pi-times" text severity="secondary" @click="annuler" />
        <Button :label="etat.options.libelleConfirmer || 'Confirmer'" :icon="etat.options.icone || 'pi pi-check'"
                :severity="etat.options.danger ? 'danger' : undefined" autofocus @click="valider" />
      </div>
    </template>
  </Dialog>
</template>
