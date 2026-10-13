<template>
  <div class="min-h-screen bg-slate-50 text-slate-800 p-6 md:p-8 font-sans">
    <div class="max-w-7xl mx-auto space-y-8">

      <!-- HEADER -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-white p-6 rounded-2xl border border-slate-200 shadow-sm">
        <div class="flex items-center gap-4">
          <div class="p-3 bg-amber-100 text-amber-600 rounded-xl">
            <i class="pi pi-shield text-3xl"></i>
          </div>
          <div>
            <h1 class="text-2xl font-black text-slate-900 tracking-tight">Dashboard Superviseur</h1>
            <p class="text-slate-500 text-sm mt-1">Documents qualité actifs et archivés, génériques et spécifiques</p>
          </div>
        </div>
        <button @click="charger" :disabled="chargement"
                class="px-4 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 rounded-lg font-bold text-sm transition-colors flex items-center gap-2 border border-slate-200 disabled:opacity-50">
          <i class="pi pi-refresh" :class="{ 'pi-spin': chargement }"></i> Actualiser
        </button>
      </div>

      <p v-if="erreur" class="bg-red-50 border border-red-200 text-red-700 text-sm rounded-xl px-4 py-3">{{ erreur }}</p>

      <!-- COMPTEURS -->
      <div class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div v-for="carte in cartes" :key="carte.cle"
             class="bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden hover:shadow-md transition-shadow">
          <div class="p-6 border-b flex items-center justify-between gap-3" :class="carte.entete">
            <div class="flex items-center gap-3">
              <div class="p-2 rounded-lg" :class="carte.fondIcone"><i :class="carte.icone" class="text-xl"></i></div>
              <h2 class="text-lg font-bold" :class="carte.titre">{{ carte.libelle }}</h2>
            </div>
            <span class="text-xs font-semibold text-slate-500">{{ compte(carte.cle).total ?? 0 }} au total</span>
          </div>
          <div class="p-6 grid gap-4" :class="carte.statuts.length === 3 ? 'grid-cols-3' : 'grid-cols-2'">
            <div v-for="s in carte.statuts" :key="s.cle" class="bg-slate-50 p-4 rounded-xl border border-slate-100">
              <div class="flex items-center justify-between mb-2">
                <p class="text-slate-500 text-xs font-bold uppercase tracking-wider">{{ s.libelle }}</p>
                <i :class="s.icone"></i>
              </div>
              <p class="text-4xl font-black text-slate-900" :data-test="`${carte.cle}-${s.cle}`">{{ compte(carte.cle)[s.cle] ?? 0 }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- DÉTAIL PAR TYPE DE DOCUMENT -->
      <div class="bg-white border border-slate-200 rounded-2xl shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <h2 class="text-base font-bold text-slate-800">Par type de document</h2>
        </div>
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs uppercase tracking-wider font-semibold">
              <th class="px-6 py-3">Type de document</th>
              <th class="px-6 py-3">Portée</th>
              <th class="px-6 py-3 text-right">Actifs</th>
              <th class="px-6 py-3 text-right">Archivés</th>
              <th class="px-6 py-3 text-right">Brouillons</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="!chargement && parType.length === 0">
              <td colspan="5" class="px-6 py-8 text-center text-slate-400 text-sm">Aucun document enregistré.</td>
            </tr>
            <tr v-for="t in parType" :key="`${t.typeDocumentCode}-${t.portee}`" class="text-sm">
              <td class="px-6 py-3 font-semibold text-slate-700">{{ t.libelle }}</td>
              <td class="px-6 py-3">
                <span class="px-2 py-0.5 rounded text-xs font-semibold"
                      :class="t.portee === 'SPECIFIQUE' ? 'bg-emerald-50 text-emerald-700' : 'bg-blue-50 text-blue-700'">
                  {{ t.portee === 'SPECIFIQUE' ? 'Spécifique' : 'Générique' }}
                </span>
              </td>
              <td class="px-6 py-3 text-right font-bold text-slate-800">{{ t.comptes.actifs }}</td>
              <td class="px-6 py-3 text-right text-slate-600">{{ t.comptes.archives }}</td>
              <td class="px-6 py-3 text-right text-slate-400">{{ t.portee === 'SPECIFIQUE' ? t.comptes.brouillons : '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue';
import apiClient from '@/services/apiClient';

const ACTIFS = { cle: 'actifs', libelle: 'Actifs', icone: 'pi pi-check-circle text-emerald-500' };
const ARCHIVES = { cle: 'archives', libelle: 'Archivés', icone: 'pi pi-history text-slate-400' };
const BROUILLONS = { cle: 'brouillons', libelle: 'Brouillons', icone: 'pi pi-pencil text-amber-500' };

// Un document générique n'a pas de brouillon : il est actif ou archivé.
const cartes = [
  { cle: 'generiques', statuts: [ACTIFS, ARCHIVES], libelle: 'Documents Génériques', icone: 'pi pi-file text-blue-600', fondIcone: 'bg-blue-100', entete: 'bg-blue-50/50 border-blue-100', titre: 'text-blue-900' },
  { cle: 'specifiques', statuts: [ACTIFS, ARCHIVES, BROUILLONS], libelle: 'Documents Spécifiques', icone: 'pi pi-file-edit text-emerald-600', fondIcone: 'bg-emerald-100', entete: 'bg-emerald-50/50 border-emerald-100', titre: 'text-emerald-900' }
];

const chargement = ref(false);
const erreur = ref('');
const statistiques = ref(null);

const compte = (cle) => statistiques.value?.[cle] || {};
const parType = computed(() => statistiques.value?.parType || []);

const charger = async () => {
  chargement.value = true;
  erreur.value = '';
  try {
    const { data } = await apiClient.get('/hub/statistiques');
    statistiques.value = data;
  } catch (e) {
    erreur.value = e.message || 'Impossible de charger les statistiques.';
  } finally {
    chargement.value = false;
  }
};

onMounted(charger);
</script>
