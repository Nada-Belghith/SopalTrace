import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { documentService as pfPlanService } from '@/services/documentService';
import { referentielsService } from '@/services/referentielsService';
import { parseFrequenceLibelle } from '@/utils/frequencyUtils';

export const usePfPlanStore = defineStore('pfPlan', () => {
  // --- DICTIONNAIRES ---
  const typesRobinet = ref([]);
  const famillesProduit = ref([]);
  const typesCaracteristique = ref([]);
  const typesControle = ref([]);
  const moyensControle = ref([]);
  const periodicites = ref([]);
  const typesSection = ref([]);
  const instruments = ref([]);
  const postes = ref([]);
  const reglesEchantillonnage = ref([]);
  const formulairesReferences = ref([]);
  const isDicosLoaded = ref(false);

  // --- ÉTAT DU PLAN ---
  const entete = ref({
    id: null,
    familleProduitFiniCode: '',
    familleProduitFiniLibelle: '',
    refFormulaireCodeReference: null,
    versionInitiale: null,
    version: 1,
    statut: 'ACTIF',
    dateApplication: null,
    creePar: '',
    creeLe: null,
    remarques: '',
    legendeMoyens: '',
  });

  const sections = ref([]);
  const isLoading = ref(false);

  // --- UTILITAIRES DE MAPPING ---
  const resolvePeriodiciteId = (s) => {
    if (s.periodiciteId) return s.periodiciteId;
    if (!s.frequenceLibelle && !(s.freqNum && s.typeVariable)) return null;

    // Try to find matching periodicite from the dictionary
    const periodicite = (periodicites.value || []).find(p => {
      const pid = p.id || p.Id;
      const pLib = (p.libelle || p.Libelle || '').toLowerCase();
      const pNum = p.frequenceNum ?? p.FrequenceNum;
      const pUnite = (p.frequenceUnite || p.FrequenceUnite || '').toUpperCase();

      if (s.frequenceLibelle && pLib === s.frequenceLibelle.toLowerCase()) return true;
      if (pNum !== undefined && pNum === s.freqNum && pUnite === (s.typeVariable || '').toUpperCase()) return true;
      return false;
    });

    return periodicite ? (periodicite.id || periodicite.Id) : null;
  };

  const mapSectionsForBackend = () => {
    return (sections.value || []).map((s, sIdx) => ({
      id: s.id,
      typeSectionId: s.typeSectionId || null,
      // libelleSection is the freshly computed label (set by verifierVariables + nextTick in PlanSectionHeader)
      libelleSection: s.libelleSection || s.nom || `Section ${sIdx + 1}`,
      regleEchantillonnageId: s.regleEchantillonnageId || null,
      regleEchantillonnageLibelle: s.regleEchantillonnageLibelle || null,
      // Resolve periodiciteId from dictionary if not already set (fixes null periodiciteId in DB)
      periodiciteId: resolvePeriodiciteId(s),
      frequenceLibelle: s.frequenceLibelle || null,
      notes: s.notes || null,
      ordreAffiche: sIdx + 1,
      lignes: (s.lignes || []).map((l, lIdx) => ({
        id: l.id,
        ordreAffiche: lIdx + 1,
        typeCaracteristiqueId: l.typeCaracteristiqueId || null,
        libelleAffiche: l.libelleAffiche || null,
        typeControleId: l.typeControleId || null,
        moyenControleId: l.moyenControleId || null,
        instrumentCode: l.instrumentCode || null,
        moyenTexteLibre: l.moyenTexteLibre || null,
        limiteSpecTexte: l.limiteSpecTexte || null,
        defauthequeId: l.defauthequeId || null,
        instruction: l.instruction || null,
        observations: l.observations || null,
        extraColonnes: Object.entries(l.valeursColonnesSpecifiques || {}).map(([k, v], idx) => ({
          cleColonne: k,
          valeurColonne: v ? String(v) : null,
          ordreAffiche: idx + 1
        }))
      }))
    }));
  };

  // --- ACTIONS ---
  const fetchDictionnaires = async () => {
    try {
      const response = await referentielsService.getDictionnaires();
      const data = response.data.data;

      typesRobinet.value = data.typesRobinet || [];
      famillesProduit.value = data.famillesProduit || [];
      typesCaracteristique.value = data.typesCaracteristique || [];
      typesControle.value = data.typesControle || [];
      moyensControle.value = data.moyensControle || [];
      periodicites.value = data.periodicites || [];
      typesSection.value = data.typesSection || data.typesSections || [];
      instruments.value = data.instruments || [];
      postes.value = data.postes || [];
      reglesEchantillonnage.value = data.reglesEchantillonnage || [];

      isDicosLoaded.value = true;
    } catch (apiError) {
      console.error("Erreur réseau (Dictionnaires):", apiError);
      throw apiError;
    }
  };

  const fetchFormulairesReferences = async (role) => {
    try {
      const response = await referentielsService.getFormulairesListByRole(role);
      formulairesReferences.value = response.data?.data || [];
    } catch (e) {
      console.error("Erreur fetch formulaires:", e);
    }
  };

  const getPlan = async (id) => {
    isLoading.value = true;
    try {
      const response = await pfPlanService.getById(id);
      const data = response.data?.data || response.data || response;

      entete.value = {
        id: data.id,
        familleProduitFiniCode: data.familleProduitFiniCode || '',
        familleProduitFiniLibelle: data.familleProduitFiniLibelle || '',
        version: data.version,
        statut: data.statut,
        dateApplication: data.dateApplication,
        creePar: data.creePar,
        creeLe: data.creeLe,
        remarques: data.remarques || '',
        legendeMoyens: data.legendeMoyens || '',
        configurationColonnes: (data.colonneDefs || []).map(c => ({
          key: c.cleColonne || c.key,
          label: c.labelAffiche || c.label,
          type: c.typeValeur || c.type || 'Texte',
          insertAfter: c.insertAfter || 'code_instrument'
        })),
      };

      sections.value = (data.sections || [])
        .sort((a, b) => (a.ordreAffiche ?? 9999) - (b.ordreAffiche ?? 9999))
        .map(s => {
          const hydrated = { ...s };

          if (s.regleEchantillonnageId) {
            const regle = reglesEchantillonnage.value.find(r => (r.id || r.Id) === s.regleEchantillonnageId);
            const libelle = regle ? (regle.libelle || regle.Libelle) : (s.regleEchantillonnageLibelle || '');

            // FIX: Si on a un ID de règle d'échantillonnage, on FORCE le mode FIXE
            hydrated.modeFreq = 'FIXE';
            hydrated.frequenceLibelle = libelle;
            hydrated.regleEchantillonnageLibelle = libelle;
          } else if (s.typeSectionId) {
            // Section avec un type défini
            hydrated.modeFreq = s.modeFreq || 'SANS';

            // Fallback: si periodiciteId n'est pas retourné par l'API (null en DB)
            // mais que libelleSection contient une fréquence entre parenthèses, on la parse
            if (!s.periodiciteId && !s.regleEchantillonnageId && s.libelleSection && s.libelleSection.includes('(')) {
              // Extraire SEULEMENT la dernière paire de parenthèses (ex: "(2 échantillons)")
              const match = s.libelleSection.match(/\(([^)]+)\)\s*$/);
              if (match) {
                const extractedFrequence = match[1].trim();
                if (extractedFrequence) {
                  const parsingResult = parseFrequenceLibelle(extractedFrequence, periodicites.value);
                  if (parsingResult && parsingResult.modeFreq && parsingResult.modeFreq !== 'SANS') {
                    Object.assign(hydrated, parsingResult);
                  }
                }
              }
            }
          } else if (s.libelleSection && s.libelleSection.includes('(') && !s.typeSectionId) {
            // Fallback uniquement pour les sections sans typeSectionId et sans règle
            hydrated.modeFreq = 'VARIABLE';
            const extractedFrequence = s.libelleSection.split('(').pop()?.replace(')', '');
            if (extractedFrequence) {
              const parsingResult = parseFrequenceLibelle(extractedFrequence, periodicites.value);
              Object.assign(hydrated, parsingResult);
            }
            hydrated.modeFreq = 'VARIABLE';
          } else {
            hydrated.modeFreq = s.modeFreq || 'SANS';
          }

          if (hydrated.lignes) {
            hydrated.lignes = hydrated.lignes
              .sort((a, b) => (a.ordreAffiche ?? 9999) - (b.ordreAffiche ?? 9999))
              .map(l => {
                const valeursColonnesSpecifiques = {};
                if (l.extraColonnes && l.extraColonnes.length > 0) {
                  l.extraColonnes.forEach(ec => {
                    valeursColonnesSpecifiques[ec.cleColonne] = ec.valeurColonne;
                  });
                } else if (l.colonnesSupplementaires) {
                  Object.assign(valeursColonnesSpecifiques, typeof l.colonnesSupplementaires === 'string' ? JSON.parse(l.colonnesSupplementaires) : l.colonnesSupplementaires);
                }
                return {
                  ...l,
                  valeursColonnesSpecifiques
                };
              });
          }

          return hydrated;
        });
    } finally {
      isLoading.value = false;
    }
  };

  const createPlan = async () => {
    isLoading.value = true;
    try {
      const payload = {
        nom: `PLAN_PF_${entete.value.familleProduitFiniCode}`,
        designation: entete.value.familleProduitFiniLibelle,
        typeDocumentCode: 'PLAN_PF',
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        versionInitiale: entete.value.versionInitiale,
        refFormulaireCodeReference: entete.value.refFormulaireCodeReference,
        colonneDefs: entete.value.configurationColonnes || [],
        sections: mapSectionsForBackend()
      };

      const response = await pfPlanService.create(payload);
      return response.data?.planId || response.planId || response;
    } finally {
      isLoading.value = false;
    }
  };

  const archiverPlan = async () => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      await pfPlanService.deleteDocument(entete.value.id);
      entete.value.statut = 'ARCHIVE';
    } finally {
      isLoading.value = false;
    }
  };

  const creerNouvelleVersion = async (motif) => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      const payload = {
        ancienId: entete.value.id,
        nom: `PLAN_PF_${entete.value.familleProduitFiniCode}`,
        designation: entete.value.familleProduitFiniLibelle,
        typeDocumentCode: 'PLAN_PF',
        familleProduitFiniCode: entete.value.familleProduitFiniCode,
        modifiePar: 'Admin',
        motifModification: motif,
        remarques: entete.value.remarques || '',
        legendeMoyens: entete.value.legendeMoyens || '',
        versionInitiale: entete.value.versionInitiale,
        refFormulaireCodeReference: entete.value.refFormulaireCodeReference,
        colonneDefs: entete.value.configurationColonnes || [],
        sections: mapSectionsForBackend()
      };
      const response = await pfPlanService.createNewVersion(payload.ancienId, payload);
      return response.data?.planId || response.planId || response;
    } finally {
      isLoading.value = false;
    }
  };

  const restaurerPlan = async (motif) => {
    if (!entete.value.id) return;
    isLoading.value = true;
    try {
      const payload = {
        documentArchiveId: entete.value.id,
        motifRestoration: motif
      };
      const response = await pfPlanService.restaurer(payload.documentArchiveId, payload);
      return response.data?.planId || response.planId || response;
    } finally {
      isLoading.value = false;
    }
  };

  const importerDepuisExcel = async (file) => {
    const formData = new FormData();
    formData.append('file', file);
    if (entete.value.configurationColonnes) {
      const configJson = typeof entete.value.configurationColonnes === 'string'
        ? entete.value.configurationColonnes
        : JSON.stringify(entete.value.configurationColonnes);
      formData.append('colonneDefsJson', configJson);
      formData.append('configurationColonnesJson', configJson);
    }

    isLoading.value = true;
    try {
      const parsedData = await pfPlanService.importExcel(formData);

      if (parsedData) {
        if (parsedData.remarques && parsedData.remarques.trim() !== '') {
          entete.value.remarques = (entete.value.remarques ? entete.value.remarques + '\n' : '') + parsedData.remarques.trim();
        }

        if (parsedData.sections) {
          sections.value = parsedData.sections.map(sec => {
            let modeFreq = sec.modeFreq || 'SANS';
            let regleEchantillonnageId = sec.regleEchantillonnageId || null;
            let freqNum = sec.freqNum || 1;
            let typeVariable = sec.typeVariable || 'HEURE';
            let freqHours = sec.freqHours || 1;

            if (sec.frequenceLibelle) {
              const perMatch = (reglesEchantillonnage.value || []).find(p => p.libelle === sec.frequenceLibelle);
              if (perMatch) {
                modeFreq = 'FIXE';
                regleEchantillonnageId = perMatch.id;
              } else {
                modeFreq = 'VARIABLE';
                const libelle = sec.frequenceLibelle.toLowerCase();

                if (libelle.includes('pièce') && libelle.includes('heure')) {
                  typeVariable = 'HEURE';
                  const match = libelle.match(/(\d+)\s*pièce.*\/\s*(\d+)\s*heure/);
                  if (match) {
                    freqNum = parseInt(match[1]);
                    freqHours = parseInt(match[2]);
                  } else {
                    const pieceMatch = libelle.match(/(\d+)\s*pièce/);
                    if (pieceMatch) {
                      freqNum = parseInt(pieceMatch[1]);
                      freqHours = 1;
                    }
                  }
                } else if (libelle.includes('échantillon')) {
                  typeVariable = 'ECHANTILLON';
                  const match = libelle.match(/(\d+)\s*échantillon/);
                  if (match) freqNum = parseInt(match[1]);
                } else if (libelle.includes('série')) {
                  typeVariable = 'SERIE';
                  const serieMatch = libelle.match(/série de (\d+) pièces/);
                  if (serieMatch) {
                    freqNum = parseInt(serieMatch[1]);
                  }
                }
              }
            }

            let typeSectionId = sec.typeSectionId || '';

            return {
              id: sec.id || crypto.randomUUID(),
              isFromDb: false,
              nom: sec.nom || '',
              libelleSection: sec.nom,
              typeSectionId,
              notes: sec.notes || '',
              regleEchantillonnageId,
              regleEchantillonnageLibelle: sec.frequenceLibelle,
              modeFreq,
              freqNum,
              typeVariable,
              freqHours,
              lignes: (sec.lignes || []).map(lig => ({
                id: lig.id || crypto.randomUUID(),
                isFromDb: false,
                typeCaracteristiqueId: lig.typeCaracteristiqueId,
                typeControleId: lig.typeControleId,
                moyenControleId: lig.moyenControleId,
                instrumentCode: lig.instrumentCode,
                unite: lig.unite || '',
                limiteSpecTexte: lig.limiteSpecTexte,
                observations: lig.observations,
                estCritique: lig.estCritique,
                libelleAffiche: lig.libelleAffiche,
                imageBase64: lig.imageBase64 || null,
                valeursColonnesSpecifiques: lig.colonnesSupplementaires ? (typeof lig.colonnesSupplementaires === 'string' ? JSON.parse(lig.colonnesSupplementaires) : lig.colonnesSupplementaires) : (lig.valeursColonnesSpecifiques || {})
              }))
            };
          });
        }

        await fetchDictionnaires();
      }
      return parsedData;
    } finally {
      isLoading.value = false;
    }
  };

  return {
    typesRobinet, famillesProduit, typesCaracteristique, typesControle, moyensControle,
    periodicites, typesSection, instruments, postes, isDicosLoaded,
    entete, sections, isLoading, reglesEchantillonnage, formulairesReferences,
    fetchDictionnaires, fetchFormulairesReferences, getPlan, createPlan, archiverPlan, creerNouvelleVersion, restaurerPlan, importerDepuisExcel
  };
});
