<template>
  <div class="plan-read-view">

    <!-- ───────── BARRE D'OUTILS ───────── -->
    <div class="flex items-center justify-between mb-4 px-1 no-print">
      <span class="text-xs font-black text-slate-400 uppercase tracking-widest flex items-center gap-2">
        <i class="pi pi-eye text-blue-400"></i>
        Mode consultation — Vue Plan
      </span>
      <button @click="imprimer" class="flex items-center gap-2 px-4 py-2 bg-white border border-slate-200 rounded-lg shadow-sm text-xs font-bold text-slate-600 hover:bg-slate-50 hover:border-slate-300 transition-all">
        <i class="pi pi-print text-slate-500"></i> Imprimer
      </button>
    </div>

    <!-- ───────── TABLEAU ───────── -->
    <div id="plan-print-zone" class="overflow-x-auto rounded-xl border border-slate-300 shadow-sm bg-white">
      <table class="w-full border-collapse text-[12px] font-sans min-w-[900px]">

        <!-- ── En-tête colonnes ── -->
        <thead>
          <tr class="bg-[#1e3a5f] text-white text-center">
            <th v-for="col in computedColumns" :key="col.key" class="border border-slate-500 px-3 py-2.5 font-bold" :class="[col.width, col.key === 'caracteristique' ? 'text-left' : '']">
              {{ col.label }}
            </th>
          </tr>
        </thead>

        <tbody>
          <template v-for="section in sections" :key="section.id">

            <!-- ── Titre de section ── -->
            <tr class="section-row bg-[#dbeafe] border-t-2 border-[#1e3a5f]">
              <td colspan="100" class="border border-blue-300 px-4 py-2.5 text-center font-black text-[13px] text-[#1e3a5f]">
                {{ buildSectionTitle(section) }}
              </td>
            </tr>

            <!-- ── Lignes de la section ── -->
            <tr
              v-for="(ligne, lIdx) in (section.lignes || [])"
              :key="ligne.id"
              :class="lIdx % 2 === 0 ? 'bg-white' : 'bg-slate-50'"
              class="hover:bg-blue-50 transition-colors"
            >
              <td v-for="col in computedColumns" :key="col.key" class="border border-slate-200 px-3 py-2" :class="[col.key === 'caracteristique' || col.key === 'observations' ? 'text-left' : 'text-center', col.key === 'caracteristique' ? 'font-semibold text-slate-800' : 'text-slate-700', col.key === 'code_instrument' ? 'font-mono text-[11px]' : '']">
                <template v-if="col.isCustom">
                  {{ resolveCustomValue(ligne, col.key) }}
                </template>
                <template v-else-if="col.key === 'caracteristique'">
                  {{ resolveLibelle(ligne) }}
                </template>
                <template v-else-if="col.key === 'limite_spec'">
                  {{ ligne.limiteSpecTexte || ligne.limiteSpec || '—' }}
                </template>
                <template v-else-if="col.key === 'type_controle'">
                  {{ resolveTypeControle(ligne) }}
                </template>
                <template v-else-if="col.key === 'moyen_controle'">
                  {{ resolveMoyenControle(ligne) }}
                </template>
                <template v-else-if="col.key === 'code_instrument'">
                  {{ ligne.instrumentCode || '—' }}
                </template>
                <template v-else-if="col.key === 'observations'">
                  <span class="italic text-slate-600">{{ ligne.observations || '' }}</span>
                </template>
              </td>
            </tr>

            <!-- ── Sections sans lignes : ligne de séparation uniquement ── -->
            <!-- (pas de message "Aucune caractéristique" - c'est voulu) -->
          </template>

          <tr v-if="!sections || sections.length === 0">
            <td colspan="100" class="px-4 py-8 text-center text-slate-400 italic">Aucune section définie.</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- ───────── REMARQUES & LÉGENDE (après le tableau complet) ───────── -->
    <div v-if="remarques || legendeMoyens" class="mt-6 grid grid-cols-1 md:grid-cols-2 gap-4 no-print">
      <div v-if="remarques" class="bg-amber-50 border border-amber-200 rounded-xl p-4">
        <p class="text-[10px] font-black uppercase text-amber-600 tracking-widest mb-2">
          <i class="pi pi-info-circle mr-1"></i>Notes &amp; Remarques
        </p>
        <p class="text-xs text-slate-700 whitespace-pre-line">{{ remarques }}</p>
      </div>
      <div v-if="legendeMoyens" class="bg-slate-50 border border-slate-200 rounded-xl p-4">
        <p class="text-[10px] font-black uppercase text-slate-500 tracking-widest mb-2">
          <i class="pi pi-list mr-1"></i>Légende des moyens
        </p>
        <p class="text-xs text-slate-700 whitespace-pre-line">{{ legendeMoyens }}</p>
      </div>
    </div>

    <!-- Remarques pour impression (après le tableau complet) -->
    <div v-if="remarques || legendeMoyens" class="mt-4 print-only" style="display:none">
      <hr style="margin: 8px 0; border-color:#ccc"/>
      <p v-if="remarques" style="font-size:10px; margin-top:6px"><strong>Notes :</strong> {{ remarques }}</p>
      <p v-if="legendeMoyens" style="font-size:10px; margin-top:4px"><strong>Légende :</strong> {{ legendeMoyens }}</p>
    </div>

  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  sections:       { type: Array,  default: () => [] },
  remarques:      { type: String, default: '' },
  legendeMoyens:  { type: String, default: '' },
  configurationColonnes: { type: Array, default: () => [] },
  // Dictionnaires passés directement depuis le store parent
  typesSection:        { type: Array, default: () => [] },
  typesCaracteristique:{ type: Array, default: () => [] },
  typesControle:       { type: Array, default: () => [] },
  moyensControle:      { type: Array, default: () => [] },
  periodicites:        { type: Array, default: () => [] },
  reglesEchantillonnage: { type: Array, default: () => [] },
});

// ─── Colonnes Dynamiques ───
const baseModeleColumns = [
  { key: 'caracteristique', label: 'Caractéristique contrôlée', width: 'w-[22%]' },
  { key: 'limite_spec', label: 'Limite de spécification', width: 'w-[15%]' },
  { key: 'type_controle', label: 'Type de contrôle', width: 'w-[12%]' },
  { key: 'moyen_controle', label: 'Moyen de contrôle', width: 'w-[14%]' },
  { key: 'code_instrument', label: 'Code instrument de contrôle', width: 'w-[14%]' },
  { key: 'observations', label: 'Observations', width: '' }
];

const computedColumns = computed(() => {
  let cols = [...baseModeleColumns];
  let customCols = [];
  
  if (typeof props.configurationColonnes === 'string') {
    try { customCols = JSON.parse(props.configurationColonnes); } catch { /* ignore */ }
  } else {
    customCols = props.configurationColonnes || [];
  }
  
  customCols.forEach(cc => {
    const insertIdx = cols.findIndex(c => c.key === cc.insertAfter);
    const newCol = { key: cc.key, label: cc.label, width: 'w-[12%]', isCustom: true };
    if (insertIdx !== -1) {
      cols.splice(insertIdx + 1, 0, newCol);
    } else {
      cols.push(newCol);
    }
  });
  return cols;
});

function resolveCustomValue(ligne, colKey) {
  const dataSource = ligne.valeursColonnesSpecifiques || ligne.colonnesSupplementaires;
  if (!dataSource) return '—';
  try {
    const parsed = typeof dataSource === 'string' ? JSON.parse(dataSource) : dataSource;
    return parsed[colKey] || '—';
  } catch {
    return '—';
  }
}

// ─── Résolution des libellés ───

function buildSectionTitle(section) {
  // 1. Priorité absolue au libellé complet enregistré en base de données
  let titre = section.libelleSection || section.nom || '';

  // 2. Si vraiment vide, on utilise le type par défaut
  if (!titre || titre.toLowerCase() === 'section sans nom') {
    const ts = props.typesSection.find(t => t.id === section.typeSectionId);
    titre = ts ? ts.libelle : 'Section sans nom';
    
    // Si on a dû utiliser le type, on s'assure du préfixe
    if (!titre.toLowerCase().includes('caractéristiques')) {
        titre = `Caractéristiques à contrôler ${titre}`;
    }
  }

  // NOTE : On ne concatène plus rien ici car le libellé est déjà complet en base.
  return titre;
}

function resolveLibelle(ligne) {
  if (ligne.libelleAffiche) return ligne.libelleAffiche;
  const tc = props.typesCaracteristique.find(t => t.id === ligne.typeCaracteristiqueId);
  return tc ? tc.libelle : '—';
}

function resolveTypeControle(ligne) {
  const tc = props.typesControle.find(t => t.id === ligne.typeControleId);
  return tc ? tc.libelle : '—';
}

function resolveMoyenControle(ligne) {
  if (ligne.moyenTexteLibre) return ligne.moyenTexteLibre;
  const mc = props.moyensControle.find(m => m.id === ligne.moyenControleId);
  return mc ? mc.libelle : '—';
}

// ─── Impression ───
function imprimer() {
  const zone = document.getElementById('plan-print-zone');
  if (!zone) return;

  const remarquesHtml = [
    props.remarques ? `<p style="font-size:10px;margin-top:6px"><strong>Notes :</strong> ${props.remarques}</p>` : '',
    props.legendeMoyens ? `<p style="font-size:10px;margin-top:4px"><strong>Légende :</strong> ${props.legendeMoyens}</p>` : ''
  ].join('');

  const w = window.open('', '_blank');
  w.document.write(`
    <html><head>
      <title>Plan Qualité</title>
      <style>
        * { margin:0; padding:0; box-sizing:border-box; font-family: Arial, sans-serif; }
        body { padding: 20px; font-size: 11px; }
        table { width:100%; border-collapse:collapse; }
        th, td { border: 1px solid #333; padding: 5px 8px; }
        thead tr th { background:#1e3a5f; color:white; text-align:center; font-weight:bold; }
        .section-row td { background:#dbeafe; font-weight:bold; text-align:center; font-size:12px; color:#1e3a5f; border-top: 2px solid #1e3a5f; }
        .section-notes-row td { background:#f0f7ff; font-style:italic; text-align:center; font-size:11px; color:#1e5aad; }
        tr:nth-child(even) td { background:#f8fafc; }
        .section-row:nth-child(even) td { background:#dbeafe !important; }
      </style>
    </head><body>
      ${zone.innerHTML}
      ${remarquesHtml ? `<hr style="margin:10px 0;border-color:#ccc"/>${remarquesHtml}` : ''}
    </body></html>
  `);
  w.document.close();
  setTimeout(() => w.print(), 300);
}
</script>
