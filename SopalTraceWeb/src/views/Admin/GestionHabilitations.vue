<template>
  <div class="h-full flex flex-col bg-slate-50 relative">

    <!-- Top Header -->
    <header class="bg-white border-b border-slate-200 px-8 py-5 flex items-center justify-between sticky top-0 z-10 shadow-sm">
      <div>
        <h1 class="text-2xl font-bold text-slate-800 flex items-center gap-3">
          <i class="pi pi-shield text-blue-600"></i>
          Gestion des Habilitations
        </h1>
        <p class="text-slate-500 text-sm mt-1">
          Les employés demandent l'accès (matricule et e-mail de l'ERP) ; un administrateur valide la demande et attribue le rôle.
        </p>
      </div>

      <button @click="charger" :disabled="chargement"
              class="bg-white hover:bg-slate-50 text-slate-700 border border-slate-300 px-5 py-2.5 rounded-lg text-sm font-semibold transition-colors flex items-center gap-2 shadow-sm disabled:opacity-50">
        <i class="pi pi-refresh" :class="{ 'pi-spin': chargement }"></i> Actualiser
      </button>
    </header>

    <main class="flex-1 p-8 space-y-6">

      <!-- Demandes à traiter -->
      <div v-if="demandesATraiter > 0 && filtres.status !== 'PENDING_APPROVAL'"
           class="bg-amber-50 border border-amber-200 rounded-xl px-5 py-4 flex items-center justify-between gap-4" data-test="bandeau-demandes">
        <p class="text-sm text-amber-800 flex items-center gap-2">
          <i class="pi pi-inbox"></i>
          <strong>{{ demandesATraiter }}</strong> demande(s) d'accès en attente de votre décision.
        </p>
        <button @click="afficherOnglet('comptes'); filtrerParStatut('PENDING_APPROVAL')"
                class="px-4 py-2 text-sm font-semibold text-white bg-amber-600 rounded-lg hover:bg-amber-700">
          Traiter les demandes
        </button>
      </div>

      <!-- Onglets -->
      <div class="flex items-center gap-2 border-b border-slate-200">
        <button v-for="o in ONGLETS" :key="o.cle" @click="afficherOnglet(o.cle)"
                class="px-4 py-2.5 text-sm font-semibold border-b-2 -mb-px transition-colors"
                :class="onglet === o.cle ? 'border-blue-600 text-blue-700' : 'border-transparent text-slate-500 hover:text-slate-800'">
          <i :class="o.icone" class="mr-1.5"></i>{{ o.libelle }}
        </button>
      </div>

      <!-- ═══════════════ Comptes ═══════════════ -->
      <template v-if="onglet === 'comptes'">
        <div class="grid grid-cols-2 md:grid-cols-3 xl:grid-cols-6 gap-4">
          <button v-for="carte in cartes" :key="carte.statut" @click="filtrerParStatut(carte.statut)"
                  class="bg-white rounded-xl border p-4 shadow-sm flex items-center gap-3 text-left transition-all hover:shadow-md"
                  :class="filtres.status === carte.statut ? 'border-blue-400 ring-2 ring-blue-100' : 'border-slate-200'">
            <div class="w-10 h-10 rounded-full flex items-center justify-center text-lg shrink-0" :class="carte.couleur">
              <i :class="carte.icone"></i>
            </div>
            <div>
              <p class="text-xs text-slate-500 font-medium leading-tight">{{ carte.libelle }}</p>
              <p class="text-xl font-bold text-slate-800">{{ tableauDeBord ? tableauDeBord[carte.champ] : '—' }}</p>
            </div>
          </button>
        </div>

        <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">

          <!-- Filtres -->
          <div class="px-6 py-4 border-b border-slate-100 flex flex-wrap items-center gap-3 bg-slate-50/50">
            <div class="relative">
              <i class="pi pi-search absolute left-3 top-1/2 -translate-y-1/2 text-slate-400"></i>
              <input v-model="filtres.search" @input="rechercherAvecDelai" type="text" placeholder="Matricule, nom ou e-mail..."
                     class="pl-10 pr-4 py-2 border border-slate-200 rounded-lg text-sm focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500 w-72 bg-white">
            </div>
            <select v-model="filtres.role" @change="rechercher(1)"
                    class="py-2 px-3 border border-slate-200 rounded-lg text-sm bg-white focus:outline-none focus:border-blue-500">
              <option value="">Tous les rôles</option>
              <option v-for="r in roles" :key="r.code" :value="r.code">{{ r.label }}</option>
            </select>
            <select v-model="filtres.status" @change="rechercher(1)"
                    class="py-2 px-3 border border-slate-200 rounded-lg text-sm bg-white focus:outline-none focus:border-blue-500">
              <option value="">Tous les statuts</option>
              <option v-for="(s, code) in STATUTS_COMPTE" :key="code" :value="code">{{ s.libelle }}</option>
            </select>
            <button v-if="filtresActifs" @click="reinitialiserFiltres" class="text-sm text-slate-500 hover:text-slate-800 flex items-center gap-1">
              <i class="pi pi-times text-xs"></i> Effacer les filtres
            </button>
          </div>

          <!-- Comptes -->
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs uppercase tracking-wider font-semibold">
                <th class="px-6 py-4">Employé</th>
                <th class="px-6 py-4">Rôle</th>
                <th class="px-6 py-4">Statut du compte</th>
                <th class="px-6 py-4">Dernière connexion</th>
                <th class="px-6 py-4 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-slate-100">
              <tr v-if="!chargement && comptes.items.length === 0">
                <td colspan="5" class="px-6 py-10 text-center text-slate-400 text-sm">Aucun compte ne correspond à la recherche.</td>
              </tr>
              <tr v-for="c in comptes.items" :key="c.matricule" class="hover:bg-slate-50/80 transition-colors"
                  :class="{ 'bg-amber-50/40': c.status === 'PENDING_APPROVAL' }">
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                    <div class="w-8 h-8 rounded-full bg-slate-100 flex items-center justify-center text-slate-600 font-bold text-xs">{{ initiale(c.fullName) }}</div>
                    <div>
                      <p class="font-semibold text-slate-800">{{ c.fullName }} <span v-if="estMoi(c)" class="text-[10px] font-bold text-blue-600 ml-1">(vous)</span></p>
                      <p class="text-xs text-slate-500">{{ c.matricule }} · {{ c.email }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-6 py-4">
                  <template v-if="c.status === 'PENDING_APPROVAL' || c.status === 'REJECTED'">
                    <span class="text-xs text-slate-500">{{ c.roleLabel ? `Proposé : ${c.roleLabel}` : 'À attribuer' }}</span>
                  </template>
                  <span v-else class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">
                    {{ c.roleLabel || '—' }}
                  </span>
                  <p v-if="c.jobTitle" class="text-[11px] text-slate-400 mt-1">ERP : {{ c.jobTitle }}</p>
                </td>
                <td class="px-6 py-4">
                  <span class="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold border" :class="statut(c).classes">
                    <span class="w-1.5 h-1.5 rounded-full" :class="statut(c).point"></span> {{ statut(c).libelle }}
                  </span>
                  <p class="text-[11px] mt-1" :class="c.status === 'LOCKED' || c.status === 'REJECTED' ? 'text-red-500' : 'text-slate-400'">{{ precisionStatut(c) }}</p>
                </td>
                <td class="px-6 py-4 text-sm text-slate-600">{{ formaterDate(c.lastLoginAt) }}</td>
                <td class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-1">
                    <template v-if="c.status === 'PENDING_APPROVAL' || c.status === 'REJECTED'">
                      <button @click="ouvrirDecision(c, 'approuver')" :disabled="estMoi(c)"
                              class="px-3 py-1.5 text-xs font-semibold text-white bg-emerald-600 rounded-lg hover:bg-emerald-700 disabled:opacity-40">
                        <i class="pi pi-check mr-1"></i>{{ c.status === 'REJECTED' ? 'Reconsidérer' : 'Approuver' }}
                      </button>
                      <button v-if="c.status === 'PENDING_APPROVAL'" @click="ouvrirDecision(c, 'refuser')" :disabled="estMoi(c)"
                              class="px-3 py-1.5 text-xs font-semibold text-rose-700 bg-white border border-rose-200 rounded-lg hover:bg-rose-50 disabled:opacity-40">
                        <i class="pi pi-times mr-1"></i>Refuser
                      </button>
                    </template>
                    <template v-else>
                      <button v-if="c.status !== 'DISABLED'" @click="ouvrirChangementRole(c)" :disabled="estMoi(c)" title="Changer le rôle"
                              class="p-2 text-slate-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors disabled:opacity-30 disabled:pointer-events-none">
                        <i class="pi pi-user-edit"></i>
                      </button>
                      <button v-if="c.status === 'LOCKED'" @click="agir(c, 'unlock')" title="Déverrouiller"
                              class="p-2 text-slate-400 hover:text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors">
                        <i class="pi pi-lock-open"></i>
                      </button>
                      <button v-if="c.status === 'INVITED'" @click="agir(c, 'resendInvitation')" title="Renvoyer le lien d'activation"
                              class="p-2 text-slate-400 hover:text-sky-600 hover:bg-sky-50 rounded-lg transition-colors">
                        <i class="pi pi-envelope"></i>
                      </button>
                      <button v-if="c.activeSessions > 0 && !estMoi(c)" @click="agir(c, 'revokeSessions')" title="Fermer les sessions"
                              class="p-2 text-slate-400 hover:text-orange-600 hover:bg-orange-50 rounded-lg transition-colors">
                        <i class="pi pi-sign-out"></i>
                      </button>
                      <button v-if="!estMoi(c)" @click="agir(c, c.status === 'DISABLED' ? 'enable' : 'disable')"
                              :title="c.status === 'DISABLED' ? 'Réactiver le compte' : 'Désactiver le compte'"
                              class="p-2 rounded-lg transition-colors"
                              :class="c.status === 'DISABLED' ? 'text-slate-400 hover:text-emerald-600 hover:bg-emerald-50' : 'text-slate-400 hover:text-red-600 hover:bg-red-50'">
                        <i :class="c.status === 'DISABLED' ? 'pi pi-check-circle' : 'pi pi-ban'"></i>
                      </button>
                    </template>
                    <button @click="voirJournal(c)" title="Journal des connexions"
                            class="p-2 text-slate-400 hover:text-slate-700 hover:bg-slate-100 rounded-lg transition-colors">
                      <i class="pi pi-history"></i>
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>

          <!-- Pagination -->
          <div class="px-6 py-4 border-t border-slate-100 flex items-center justify-between text-sm text-slate-500 bg-white">
            <span>{{ comptes.total }} compte(s) · page {{ comptes.page }} / {{ nombrePages }}</span>
            <div class="flex items-center gap-1">
              <button @click="rechercher(comptes.page - 1)" :disabled="comptes.page <= 1"
                      class="px-3 py-1 border border-slate-200 rounded bg-white hover:bg-slate-50 disabled:opacity-50">Précédent</button>
              <button @click="rechercher(comptes.page + 1)" :disabled="comptes.page >= nombrePages"
                      class="px-3 py-1 border border-slate-200 rounded bg-white hover:bg-slate-50 disabled:opacity-50">Suivant</button>
            </div>
          </div>
        </div>
      </template>

      <!-- ═══════════════ Rôles pré-attribués ═══════════════ -->
      <div v-else class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">
        <div class="px-6 py-5 border-b border-slate-100 bg-slate-50/50">
          <p class="text-sm text-slate-600 mb-4">
            Préparez l'accès d'un employé avant son inscription : à sa demande (matricule et e-mail de l'ERP),
            il reçoit directement son lien d'activation avec ce rôle, sans passer par la file des demandes.
          </p>
          <form @submit.prevent="preattribuer" class="flex flex-wrap items-end gap-3">
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Matricule (ERP)</label>
              <input v-model="preattribution.matricule" type="text" required maxlength="20" placeholder="Ex. 45891"
                     class="py-2 px-3 border border-slate-300 rounded-lg text-sm w-40 focus:outline-none focus:border-blue-500 bg-white">
            </div>
            <div>
              <label class="block text-xs font-semibold text-slate-600 mb-1">Rôle</label>
              <select v-model="preattribution.role" required
                      class="py-2 px-3 border border-slate-300 rounded-lg text-sm w-64 focus:outline-none focus:border-blue-500 bg-white">
                <option value="" disabled>Choisir un rôle</option>
                <option v-for="r in roles" :key="r.code" :value="r.code">{{ r.label }}</option>
              </select>
            </div>
            <button type="submit" :disabled="enregistrement || !preattribution.matricule || !preattribution.role"
                    class="px-5 py-2 text-sm font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2">
              <i class="pi pi-user-plus"></i> Pré-attribuer
            </button>
          </form>
        </div>

        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-200 text-slate-500 text-xs uppercase tracking-wider font-semibold">
              <th class="px-6 py-4">Employé</th>
              <th class="px-6 py-4">Rôle prévu</th>
              <th class="px-6 py-4">Préparé par</th>
              <th class="px-6 py-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-100">
            <tr v-if="preattributions.length === 0">
              <td colspan="4" class="px-6 py-10 text-center text-slate-400 text-sm">Aucun rôle pré-attribué en attente d'inscription.</td>
            </tr>
            <tr v-for="p in preattributions" :key="p.matricule">
              <td class="px-6 py-4">
                <p class="font-semibold text-slate-800">{{ p.fullName }}</p>
                <p class="text-xs text-slate-500">{{ p.matricule }}<template v-if="p.email"> · {{ p.email }}</template></p>
              </td>
              <td class="px-6 py-4">
                <span class="inline-flex px-2.5 py-1 rounded-md text-xs font-semibold bg-blue-50 text-blue-700 border border-blue-200">{{ p.roleLabel }}</span>
              </td>
              <td class="px-6 py-4 text-sm text-slate-600">{{ p.assignedBy }} · {{ formaterDate(p.assignedAt) }}</td>
              <td class="px-6 py-4 text-right">
                <button @click="supprimerPreattribution(p)" title="Retirer la pré-attribution"
                        class="p-2 text-slate-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors">
                  <i class="pi pi-trash"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </main>

    <!-- Décision sur une demande d'accès -->
    <div v-if="decision.compte" class="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-0">
      <div class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" @click="decision.compte = null"></div>
      <div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <h2 class="text-lg font-bold text-slate-800 flex items-center gap-2">
            <i class="pi" :class="decision.type === 'approuver' ? 'pi-check-circle text-emerald-600' : 'pi-times-circle text-rose-600'"></i>
            {{ decision.type === 'approuver' ? 'Accorder l\'accès à' : 'Refuser la demande de' }} {{ decision.compte.fullName }}
          </h2>
          <button @click="decision.compte = null" class="text-slate-400 hover:text-slate-600 p-1 rounded-md hover:bg-slate-100">
            <i class="pi pi-times"></i>
          </button>
        </div>
        <div class="p-6 space-y-4">
          <p class="text-xs text-slate-500">
            Matricule {{ decision.compte.matricule }} · {{ decision.compte.email }}<template v-if="decision.compte.jobTitle"> · ERP : {{ decision.compte.jobTitle }}</template>
          </p>
          <template v-if="decision.type === 'approuver'">
            <p class="text-sm text-slate-500 leading-relaxed">
              L'employé reçoit un lien d'activation personnel, valable 48 h et utilisable une seule fois, pour choisir son mot de passe.
            </p>
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Rôle attribué</label>
              <select v-model="decision.role" data-test="role-decision"
                      class="w-full px-3 py-2.5 border border-slate-300 rounded-lg focus:outline-none focus:border-blue-500 text-slate-800 font-medium bg-white">
                <option value="" disabled>Choisir un rôle</option>
                <option v-for="r in roles" :key="r.code" :value="r.code">{{ r.label }}</option>
              </select>
            </div>
            <div>
              <label class="block text-sm font-semibold text-slate-700 mb-1.5">Commentaire <span class="font-normal text-slate-400">(facultatif, tracé)</span></label>
              <textarea v-model="decision.texte" rows="2" maxlength="500"
                        class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-blue-500"></textarea>
            </div>
          </template>
          <div v-else>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Motif du refus <span class="font-normal text-slate-400">(communiqué à l'employé et tracé)</span></label>
            <textarea v-model="decision.texte" rows="3" maxlength="500" data-test="motif-refus"
                      class="w-full px-3 py-2 border border-slate-300 rounded-lg text-sm focus:outline-none focus:border-blue-500"></textarea>
          </div>
        </div>
        <div class="px-6 py-4 border-t border-slate-100 bg-slate-50 flex items-center justify-end gap-3">
          <button @click="decision.compte = null" class="px-5 py-2.5 text-sm font-semibold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50">
            Annuler
          </button>
          <button @click="validerDecision" :disabled="enregistrement || !decisionValide" data-test="valider-decision"
                  class="px-5 py-2.5 text-sm font-semibold text-white rounded-lg shadow-sm flex items-center gap-2 disabled:opacity-50"
                  :class="decision.type === 'approuver' ? 'bg-emerald-600 hover:bg-emerald-700' : 'bg-rose-600 hover:bg-rose-700'">
            <i class="pi" :class="enregistrement ? 'pi-spinner pi-spin' : (decision.type === 'approuver' ? 'pi-send' : 'pi-times')"></i>
            {{ decision.type === 'approuver' ? 'Accorder et envoyer le lien' : 'Refuser la demande' }}
          </button>
        </div>
      </div>
    </div>

    <!-- Changement de rôle -->
    <div v-if="compteEdite" class="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-0">
      <div class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" @click="compteEdite = null"></div>
      <div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-lg overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <h2 class="text-lg font-bold text-slate-800 flex items-center gap-2">
            <i class="pi pi-user-edit text-blue-600"></i> Rôle de {{ compteEdite.fullName }}
          </h2>
          <button @click="compteEdite = null" class="text-slate-400 hover:text-slate-600 p-1 rounded-md hover:bg-slate-100">
            <i class="pi pi-times"></i>
          </button>
        </div>
        <div class="p-6 space-y-4">
          <p class="text-sm text-slate-500 leading-relaxed">
            Le nouveau rôle s'applique immédiatement : les sessions ouvertes de cet utilisateur sont fermées et il devra se reconnecter.
          </p>
          <div>
            <label class="block text-sm font-semibold text-slate-700 mb-1.5">Rôle</label>
            <select v-model="nouveauRole"
                    class="w-full px-3 py-2.5 border border-slate-300 rounded-lg focus:outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-500/20 text-slate-800 font-medium bg-white">
              <option v-for="r in roles" :key="r.code" :value="r.code">{{ r.label }}</option>
            </select>
          </div>
        </div>
        <div class="px-6 py-4 border-t border-slate-100 bg-slate-50 flex items-center justify-end gap-3">
          <button @click="compteEdite = null" class="px-5 py-2.5 text-sm font-semibold text-slate-600 bg-white border border-slate-300 rounded-lg hover:bg-slate-50">
            Annuler
          </button>
          <button @click="enregistrerRole" :disabled="enregistrement || nouveauRole === compteEdite.role"
                  class="px-5 py-2.5 text-sm font-semibold text-white bg-blue-600 rounded-lg hover:bg-blue-700 shadow-sm flex items-center gap-2 disabled:opacity-50">
            <i class="pi" :class="enregistrement ? 'pi-spinner pi-spin' : 'pi-save'"></i> Enregistrer
          </button>
        </div>
      </div>
    </div>

    <!-- Journal d'un compte -->
    <div v-if="journal.compte" class="fixed inset-0 z-50 flex items-center justify-center p-4 sm:p-0">
      <div class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm" @click="journal.compte = null"></div>
      <div class="relative bg-white rounded-2xl shadow-2xl w-full max-w-2xl overflow-hidden">
        <div class="px-6 py-4 border-b border-slate-100 flex items-center justify-between bg-slate-50/50">
          <h2 class="text-lg font-bold text-slate-800 flex items-center gap-2">
            <i class="pi pi-history text-blue-600"></i> Journal de {{ journal.compte.fullName }}
          </h2>
          <button @click="journal.compte = null" class="text-slate-400 hover:text-slate-600 p-1 rounded-md hover:bg-slate-100">
            <i class="pi pi-times"></i>
          </button>
        </div>
        <div class="max-h-[60vh] overflow-y-auto divide-y divide-slate-100">
          <p v-if="journal.lignes.length === 0" class="p-6 text-center text-sm text-slate-400">Aucun événement.</p>
          <div v-for="(l, i) in journal.lignes" :key="i" class="px-6 py-3 flex items-start justify-between gap-4">
            <div>
              <p class="text-sm font-semibold text-slate-700">{{ ACTIONS_JOURNAL[l.action] || l.action }}</p>
              <p v-if="l.details" class="text-xs text-slate-500">{{ l.details }}</p>
            </div>
            <span class="text-xs text-slate-400 whitespace-nowrap">{{ formaterDate(l.date) }}</span>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue';
import { useRoute } from 'vue-router';
import Swal from 'sweetalert2';
import adminService from '@/services/adminService';
import { useAuthStore } from '@/stores/authStore';
import { useAppToast } from '@/composables/useAppToast';
import { STATUTS_COMPTE, ACTIONS_JOURNAL, formaterDate } from '@/constants/comptes';

const toast = useAppToast();
const authStore = useAuthStore();
const route = useRoute();

const TAILLE_PAGE = 20;
const LONGUEUR_MIN_MOTIF = 5;
const ONGLETS = [
  { cle: 'comptes', libelle: 'Comptes et demandes', icone: 'pi pi-users' },
  { cle: 'preattributions', libelle: 'Rôles pré-attribués', icone: 'pi pi-user-plus' }
];

const onglet = ref('comptes');
const chargement = ref(false);
const enregistrement = ref(false);
const roles = ref([]);
const tableauDeBord = ref(null);
const comptes = ref({ items: [], total: 0, page: 1, pageSize: TAILLE_PAGE });
// Lien de l'e-mail envoyé aux administrateurs : …/habilitations?status=PENDING_APPROVAL
const statutInitial = STATUTS_COMPTE[route?.query?.status] ? route.query.status : '';
const filtres = reactive({ search: '', role: '', status: statutInitial });
const compteEdite = ref(null);
const nouveauRole = ref('');
const decision = reactive({ compte: null, type: 'approuver', role: '', texte: '' });
const journal = reactive({ compte: null, lignes: [] });
const preattributions = ref([]);
const preattribution = reactive({ matricule: '', role: '' });

const cartes = [
  { statut: 'PENDING_APPROVAL', champ: 'pendingApprovalAccounts', libelle: 'Demandes à traiter', icone: 'pi pi-inbox', couleur: 'bg-amber-50 text-amber-600' },
  { statut: 'INVITED', champ: 'invitedAccounts', libelle: 'Invitations envoyées', icone: 'pi pi-envelope', couleur: 'bg-sky-50 text-sky-600' },
  { statut: 'ACTIVE', champ: 'activeAccounts', libelle: 'Comptes actifs', icone: 'pi pi-users', couleur: 'bg-blue-50 text-blue-600' },
  { statut: 'LOCKED', champ: 'lockedAccounts', libelle: 'Verrouillés', icone: 'pi pi-lock', couleur: 'bg-red-50 text-red-500' },
  { statut: 'REJECTED', champ: 'rejectedAccounts', libelle: 'Demandes refusées', icone: 'pi pi-times-circle', couleur: 'bg-rose-50 text-rose-500' },
  { statut: 'DISABLED', champ: 'disabledAccounts', libelle: 'Désactivés', icone: 'pi pi-ban', couleur: 'bg-slate-100 text-slate-500' }
];

const nombrePages = computed(() => Math.max(1, Math.ceil(comptes.value.total / TAILLE_PAGE)));
const filtresActifs = computed(() => filtres.search || filtres.role || filtres.status);
const demandesATraiter = computed(() => tableauDeBord.value?.pendingApprovalAccounts || 0);
const statut = (c) => STATUTS_COMPTE[c.status] || STATUTS_COMPTE.ACTIVE;
const initiale = (nom) => (nom || '?').trim().charAt(0).toUpperCase();
const estMoi = (c) => c.matricule === authStore.user?.matricule;

const decisionValide = computed(() => decision.type === 'approuver'
  ? !!decision.role
  : decision.texte.trim().length >= LONGUEUR_MIN_MOTIF);

/** Ligne d'information sous le statut : traçabilité de la décision et échéances. */
const precisionStatut = (c) => {
  switch (c.status) {
    case 'PENDING_APPROVAL': return `Demandée le ${formaterDate(c.createdAt)}`;
    case 'REJECTED': return `Refusée par ${c.reviewedBy} le ${formaterDate(c.reviewedAt)}${c.reviewComment ? ` : ${c.reviewComment}` : ''}`;
    case 'INVITED': return `Lien valable jusqu'au ${formaterDate(c.invitationExpiresAt)}${c.reviewedBy ? ` · accordé par ${c.reviewedBy}` : ''}`;
    case 'LOCKED': return `jusqu'à ${formaterDate(c.lockoutEndsAt)}`;
    default:
      if (c.activeSessions > 0) return `${c.activeSessions} session(s) ouverte(s)`;
      return c.reviewedBy ? `Accès accordé par ${c.reviewedBy} le ${formaterDate(c.reviewedAt)}` : '';
  }
};

const rechercher = async (page = comptes.value.page) => {
  chargement.value = true;
  try {
    comptes.value = await adminService.searchUsers({ ...filtres, page, pageSize: TAILLE_PAGE });
  } catch (e) {
    toast.error(e.message);
  } finally {
    chargement.value = false;
  }
};

let delaiRecherche = null;
const rechercherAvecDelai = () => {
  clearTimeout(delaiRecherche);
  delaiRecherche = setTimeout(() => rechercher(1), 300);
};

const filtrerParStatut = (s) => {
  filtres.status = filtres.status === s ? '' : s;
  rechercher(1);
};

const reinitialiserFiltres = () => {
  Object.assign(filtres, { search: '', role: '', status: '' });
  rechercher(1);
};

const chargerPreattributions = async () => {
  try {
    preattributions.value = await adminService.getPreassignedRoles();
  } catch (e) {
    toast.error(e.message);
  }
};

const afficherOnglet = (cle) => {
  onglet.value = cle;
  if (cle === 'preattributions') chargerPreattributions();
};

const charger = async () => {
  try {
    const [r, tdb] = await Promise.all([adminService.getRoles(), adminService.getDashboard()]);
    roles.value = r;
    tableauDeBord.value = tdb;
  } catch (e) {
    toast.error(e.message);
  }
  if (onglet.value === 'preattributions') await chargerPreattributions();
  else await rechercher();
};

/** Le compte est enregistré même si l'e-mail n'est pas parti : l'administrateur le sait et peut renvoyer le lien. */
const signalerEnvoi = (resultat, succes) => {
  if (resultat && resultat.emailSent === false) {
    toast.warn(`${succes} L'e-mail n'a pas pu être envoyé : vérifiez la messagerie puis utilisez « Renvoyer le lien d'activation ».`, 'E-mail non envoyé');
  } else {
    toast.success(succes);
  }
};

// ── Demandes d'accès ──

const ouvrirDecision = (c, type) => {
  Object.assign(decision, { compte: c, type, role: c.role || '', texte: '' });
};

const validerDecision = async () => {
  const c = decision.compte;
  enregistrement.value = true;
  try {
    if (decision.type === 'approuver') {
      const resultat = await adminService.approve(c.matricule, decision.role, decision.texte.trim() || null);
      signalerEnvoi(resultat, `Accès accordé à ${c.fullName} : lien d'activation envoyé.`);
    } else {
      await adminService.reject(c.matricule, decision.texte.trim());
      toast.success(`Demande de ${c.fullName} refusée. L'employé est prévenu par e-mail.`);
    }
    decision.compte = null;
    await charger();
  } catch (e) {
    toast.error(e.message);
  } finally {
    enregistrement.value = false;
  }
};

// ── Comptes ──

const ouvrirChangementRole = (c) => {
  compteEdite.value = c;
  nouveauRole.value = c.role;
};

const enregistrerRole = async () => {
  enregistrement.value = true;
  try {
    await adminService.changeRole(compteEdite.value.matricule, nouveauRole.value);
    toast.success(`Rôle de ${compteEdite.value.fullName} modifié. Ses sessions ont été fermées.`);
    compteEdite.value = null;
    await charger();
  } catch (e) {
    toast.error(e.message);
  } finally {
    enregistrement.value = false;
  }
};

const ACTIONS = {
  disable: { question: 'Désactiver ce compte ? L\'utilisateur est déconnecté immédiatement.', appel: c => adminService.setDisabled(c.matricule, true), succes: 'Compte désactivé.', danger: true },
  enable: { question: 'Réactiver ce compte ? S\'il n\'a jamais défini son mot de passe, un nouveau lien d\'activation lui est envoyé.', appel: c => adminService.setDisabled(c.matricule, false), succes: 'Compte réactivé.' },
  unlock: { question: 'Déverrouiller ce compte ?', appel: c => adminService.unlock(c.matricule), succes: 'Compte déverrouillé.' },
  revokeSessions: { question: 'Fermer toutes les sessions de cet utilisateur ?', appel: c => adminService.revokeSessions(c.matricule), succes: 'Sessions fermées.', danger: true },
  resendInvitation: { question: 'Envoyer un nouveau lien d\'activation (valable 48 h) ? L\'ancien lien ne fonctionnera plus.', appel: c => adminService.resendInvitation(c.matricule), succes: 'Lien d\'activation envoyé.' }
};

const agir = async (c, cle) => {
  const action = ACTIONS[cle];
  const confirmation = await Swal.fire({
    title: c.fullName,
    text: action.question,
    icon: action.danger ? 'warning' : 'question',
    showCancelButton: true,
    confirmButtonColor: action.danger ? '#dc2626' : '#2563eb',
    confirmButtonText: 'Confirmer',
    cancelButtonText: 'Annuler'
  });
  if (!confirmation.isConfirmed) return;

  try {
    const resultat = await action.appel(c);
    signalerEnvoi(resultat, action.succes);
    await charger();
  } catch (e) {
    toast.error(e.message);
  }
};

const voirJournal = async (c) => {
  try {
    const page = await adminService.getConnectionLog({ matricule: c.matricule, page: 1, pageSize: 50 });
    journal.lignes = page.items;
    journal.compte = c;
  } catch (e) {
    toast.error(e.message);
  }
};

// ── Rôles pré-attribués ──

const preattribuer = async () => {
  enregistrement.value = true;
  try {
    const p = await adminService.setPreassignedRole(preattribution.matricule.trim(), preattribution.role);
    toast.success(`${p.fullName} recevra directement son lien d'activation à son inscription (rôle : ${p.roleLabel}).`);
    Object.assign(preattribution, { matricule: '', role: '' });
    await chargerPreattributions();
  } catch (e) {
    toast.error(e.message);
  } finally {
    enregistrement.value = false;
  }
};

const supprimerPreattribution = async (p) => {
  const confirmation = await Swal.fire({
    title: p.fullName,
    text: 'Retirer ce rôle pré-attribué ? Une inscription future passera alors par la validation d\'un administrateur.',
    icon: 'warning',
    showCancelButton: true,
    confirmButtonColor: '#dc2626',
    confirmButtonText: 'Retirer',
    cancelButtonText: 'Annuler'
  });
  if (!confirmation.isConfirmed) return;
  try {
    await adminService.deletePreassignedRole(p.matricule);
    toast.success('Pré-attribution retirée.');
    await chargerPreattributions();
  } catch (e) {
    toast.error(e.message);
  }
};

onMounted(charger);
</script>
