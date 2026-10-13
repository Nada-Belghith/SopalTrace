<template>
  <div class="h-full flex flex-col bg-slate-50 text-slate-800 p-8 overflow-y-auto custom-scrollbar">

    <!-- En-tête -->
    <div class="bg-white rounded-xl border border-slate-200 p-6 flex flex-col sm:flex-row items-start sm:items-center justify-between shadow-sm mb-8 gap-4">
      <div class="flex items-center gap-4">
        <div class="w-12 h-12 rounded-lg bg-blue-50 flex items-center justify-center border border-blue-100">
          <i class="pi pi-chart-line text-blue-600 text-2xl"></i>
        </div>
        <div>
          <h1 class="text-2xl font-bold text-slate-800 tracking-tight">Dashboard Administrateur</h1>
          <p class="text-sm text-slate-500 mt-1">Comptes, sessions et connexions de SopalTrace</p>
        </div>
      </div>

      <div class="flex items-center gap-3">
        <router-link :to="{ name: 'admin-habilitations' }"
                     class="bg-blue-600 hover:bg-blue-700 text-white px-5 py-2.5 rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm">
          <i class="pi pi-shield"></i> Gérer les habilitations
        </router-link>
        <button @click="charger" :disabled="chargement"
                class="bg-white hover:bg-slate-50 text-slate-700 border border-slate-300 px-5 py-2.5 rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm disabled:opacity-50">
          <i class="pi pi-refresh" :class="{ 'pi-spin': chargement }"></i> Actualiser
        </button>
      </div>
    </div>

    <!-- Indicateurs -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
      <div class="bg-white rounded-xl border border-slate-200 p-6 shadow-sm">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-slate-500 uppercase tracking-wider">Comptes actifs</h3>
          <div class="w-8 h-8 rounded-full bg-blue-50 flex items-center justify-center"><i class="pi pi-users text-blue-600"></i></div>
        </div>
        <div class="flex items-baseline gap-3">
          <span class="text-4xl font-black text-slate-800">{{ d.activeAccounts ?? '—' }}</span>
          <span class="text-xs font-semibold text-slate-500">sur {{ d.totalAccounts ?? '—' }} comptes</span>
        </div>
        <div class="flex flex-wrap gap-2 mt-4 text-xs font-semibold">
          <router-link :to="{ name: 'admin-habilitations', query: { status: 'PENDING_APPROVAL' } }"
                       class="px-2 py-1 rounded-md bg-amber-50 text-amber-700 hover:bg-amber-100">{{ d.pendingApprovalAccounts ?? 0 }} demande(s) à traiter</router-link>
          <span class="px-2 py-1 rounded-md bg-sky-50 text-sky-700">{{ d.invitedAccounts ?? 0 }} invitation(s) en cours</span>
          <span class="px-2 py-1 rounded-md bg-red-50 text-red-600">{{ d.lockedAccounts ?? 0 }} verrouillé(s)</span>
          <span class="px-2 py-1 rounded-md bg-slate-100 text-slate-500">{{ d.disabledAccounts ?? 0 }} désactivé(s)</span>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 p-6 shadow-sm">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-slate-500 uppercase tracking-wider">Sessions ouvertes</h3>
          <div class="w-8 h-8 rounded-full bg-orange-50 flex items-center justify-center"><i class="pi pi-bolt text-orange-500"></i></div>
        </div>
        <div class="flex items-baseline gap-3">
          <span class="text-4xl font-black text-slate-800">{{ d.usersWithOpenSession ?? '—' }}</span>
          <span class="text-xs font-semibold text-slate-500">utilisateur(s) connecté(s) (7 jours max.)</span>
        </div>
      </div>

      <div class="bg-white rounded-xl border border-slate-200 p-6 shadow-sm">
        <div class="flex items-center justify-between mb-4">
          <h3 class="text-xs font-bold text-slate-500 uppercase tracking-wider">Connexions (24 h)</h3>
          <div class="w-8 h-8 rounded-full bg-emerald-50 flex items-center justify-center"><i class="pi pi-sign-in text-emerald-600"></i></div>
        </div>
        <div class="flex items-baseline gap-3">
          <span class="text-4xl font-black text-slate-800">{{ d.successfulLogins24h ?? '—' }}</span>
          <span class="text-xs font-bold px-2 py-1 rounded-md" :class="(d.failedLogins24h ?? 0) > 0 ? 'bg-red-50 text-red-600' : 'bg-slate-100 text-slate-500'">
            {{ d.failedLogins24h ?? 0 }} échec(s)
          </span>
        </div>
      </div>
    </div>

    <div class="grid grid-cols-1 lg:grid-cols-3 gap-6 flex-1">
      <!-- Répartition par rôle -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-100 bg-slate-50/50">
          <h2 class="text-sm font-bold text-slate-700">Comptes par rôle</h2>
        </div>
        <div class="p-6 space-y-4">
          <div v-for="r in d.usersByRole || []" :key="r.role">
            <div class="flex justify-between text-sm mb-1">
              <span class="font-semibold text-slate-700">{{ r.roleLabel }}</span>
              <span class="text-slate-500">{{ r.users }}</span>
            </div>
            <div class="h-2 bg-slate-100 rounded-full overflow-hidden">
              <div class="h-full bg-blue-500 rounded-full" :style="{ width: pourcentage(r.users) + '%' }"></div>
            </div>
          </div>
        </div>
      </div>

      <!-- Journal des connexions -->
      <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden lg:col-span-2 flex flex-col">
        <div class="px-6 py-4 border-b border-slate-100 bg-slate-50/50 flex flex-wrap items-center justify-between gap-3">
          <h2 class="text-sm font-bold text-slate-700">Journal des connexions et de l'administration</h2>
          <select v-model="filtreAction" @change="chargerJournal(1)"
                  class="py-1.5 px-3 border border-slate-200 rounded-lg text-sm bg-white focus:outline-none focus:border-blue-500">
            <option value="">Tous les événements</option>
            <option v-for="(libelle, code) in ACTIONS_JOURNAL" :key="code" :value="code">{{ libelle }}</option>
          </select>
        </div>
        <table class="w-full text-left text-sm">
          <thead>
            <tr class="text-slate-500 text-xs uppercase tracking-wider border-b border-slate-100">
              <th class="px-6 py-3">Date</th>
              <th class="px-6 py-3">Utilisateur</th>
              <th class="px-6 py-3">Événement</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="journal.items.length === 0"><td colspan="3" class="px-6 py-8 text-center text-slate-400">Aucun événement.</td></tr>
            <tr v-for="(l, i) in journal.items" :key="i">
              <td class="px-6 py-3 text-slate-500 whitespace-nowrap">{{ formaterDate(l.date) }}</td>
              <td class="px-6 py-3">
                <p class="font-semibold text-slate-700">{{ l.fullName || 'Inconnu' }}</p>
                <p class="text-xs text-slate-400">{{ l.matricule }}</p>
              </td>
              <td class="px-6 py-3">
                <p class="font-semibold" :class="l.action.includes('ECHEC') || l.action.includes('REFUSEE') || l.action === 'SESSION_REVOQUEE' ? 'text-red-600' : 'text-slate-700'">
                  {{ ACTIONS_JOURNAL[l.action] || l.action }}
                </p>
                <p v-if="l.details" class="text-xs text-slate-500">{{ l.details }}</p>
              </td>
            </tr>
          </tbody>
        </table>
        <div class="px-6 py-3 border-t border-slate-100 flex items-center justify-between text-sm text-slate-500 mt-auto">
          <span>{{ journal.total }} événement(s)</span>
          <div class="flex gap-1">
            <button @click="chargerJournal(journal.page - 1)" :disabled="journal.page <= 1"
                    class="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 disabled:opacity-50">Précédent</button>
            <button @click="chargerJournal(journal.page + 1)" :disabled="journal.page * TAILLE_JOURNAL >= journal.total"
                    class="px-3 py-1 border border-slate-200 rounded hover:bg-slate-50 disabled:opacity-50">Suivant</button>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue';
import adminService from '@/services/adminService';
import { useAppToast } from '@/composables/useAppToast';
import { ACTIONS_JOURNAL, formaterDate } from '@/constants/comptes';

const toast = useAppToast();
const TAILLE_JOURNAL = 15;

const chargement = ref(false);
const d = ref({});
const journal = ref({ items: [], total: 0, page: 1 });
const filtreAction = ref('');

const pourcentage = (n) => d.value.totalAccounts ? Math.round((n / d.value.totalAccounts) * 100) : 0;

const chargerJournal = async (page = 1) => {
  try {
    journal.value = await adminService.getConnectionLog({ action: filtreAction.value || undefined, page, pageSize: TAILLE_JOURNAL });
  } catch (e) {
    toast.error(e.message);
  }
};

const charger = async () => {
  chargement.value = true;
  try {
    d.value = await adminService.getDashboard();
    await chargerJournal(1);
  } catch (e) {
    toast.error(e.message);
  } finally {
    chargement.value = false;
  }
};

onMounted(charger);
</script>
