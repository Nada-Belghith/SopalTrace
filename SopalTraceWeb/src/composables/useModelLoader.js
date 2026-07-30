import { nextTick } from 'vue';
import { modeleFabricationService as fabModeleService } from '@/services/modeleFabricationService';
import { parseFrequenceLibelle, resolveFrequencyFromPeriodiciteId } from '@/utils/frequencyUtils';
import {
  nettoyerNomSection,
  normalizeTypeSectionId,
  resolveSectionDisplayTitle
} from '@/utils/sectionTitleUtils';
import { createModeleSnapshot } from '@/utils/modelMapper';

export function useModelLoader(store, ctx) {
  const loadModelForEditing = async (id, toast, router) => {
    store.isLoading = true;
    store.isBeingLoaded = true;
    try {
      store.loadingMessage = "Chargement du modèle...";
      const res = await fabModeleService.getModelById(id);
      const data = res?.data?.data || res?.data || res;
      
      ctx.modeleEditionId.value = data.id;
      ctx.codeOriginal.value = data.nom;
      ctx.statut.value = data.statut;
      ctx.version.value = data.version;
      
      store.entete.code = data.code || data.nom;
      store.entete.operationCode = data.operationCode;
      store.entete.natureComposantCode = data.natureComposantCode || data.natureArticleCode;
      store.entete.typeRobinetCode = data.typeRobinetCode || data.libre1 || '';
      store.entete.libelle = data.libelle || data.designation;
      store.entete.notes = data.notes || data.remarques || '';
      store.entete.legendeMoyens = data.legendeMoyens || '';
      store.entete.posteCode = data.posteCode || '';
      store.entete.familleProduitCode = data.familleProduitCode || data.familleProduitFiniCode || '';
      store.entete.refFormulaireCodeReference = data.codeReferenceFormulaire || data.refFormulaireCodeReference || 'PRC';
      
      try {
        if (store.entete.natureComposantCode && store.entete.operationCode && ctx.statut.value === 'ARCHIVE') {
          const resExist = await fabModeleService.getModelsByFilters({
            natureComposantCode: store.entete.natureComposantCode,
            operationCode: store.entete.operationCode,
            familleProduitCode: store.entete.familleProduitCode
          });
          const existingArr = Array.isArray(resExist) ? resExist : resExist?.data || [];
          ctx.hasActiveVersion.value = existingArr.some(m => m.statut === 'ACTIF');
        } else {
          ctx.hasActiveVersion.value = false;
        }
      } catch (e) {
        console.warn("Impossible de vérifier les versions actives", e);
      }
      
      if (ctx.isArchiveEditing.value || ctx.isUpgradeMode.value) {
        store.syncConfigurationFromFormulaire();
      } else {
        let configParsed = null;
        if (data.configurationColonnesJson) {
          try {
            configParsed = JSON.parse(data.configurationColonnesJson);
          } catch(e) {
            console.error('Erreur parsing configurationColonnesJson:', e);
          }
        }

        if (configParsed && configParsed.length > 0) {
          store.entete.configurationColonnes = configParsed.map(c => ({
            key: c.cleColonne || c.key,
            label: c.labelAffiche || c.label,
            type: c.typeValeur || c.type || 'Texte',
            insertAfter: c.insertAfter || 'code_instrument'
          }));
        } else if (!ctx.isArchived.value) {
          store.syncConfigurationFromFormulaire();
        } else {
          store.entete.configurationColonnes = [];
        }
      }

      const sectionsTriees = [...(data.sections || [])].sort((a, b) =>
        (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
      );

      ctx.groupes.value = sectionsTriees.map(sec => {
        let freqData = { modeFreq: 'SANS', periodiciteId: null, freqNum: 1, typeVariable: 'HEURE', freqHours: 1 };
        if (sec.periodiciteId) {
          const resolved = resolveFrequencyFromPeriodiciteId(sec.periodiciteId, store.periodicites || []);
          if (resolved) {
            freqData = resolved;
          }
        }
        if (freqData.modeFreq === 'SANS') {
          const texteParse = sec.frequenceLibelle || sec.libelleSection || '';
          if (texteParse) {
            freqData = parseFrequenceLibelle(texteParse, store.periodicites || []);
          }
        }
        
        if (sec.regleEchantillonnageId) {
          freqData.modeFreq = 'FIXE';
          freqData.regleEchantillonnageId = sec.regleEchantillonnageId;
        }

        let typeSectionId = normalizeTypeSectionId(sec.typeSectionId || '', store.typesSection);
        if (!typeSectionId && sec.libelleSection) {
          const secLib = sec.libelleSection.trim().toLowerCase();
          let bestMatch = null;
          let maxLength = -1;

          store.typesSection.forEach(t => {
            const tLib = (t.libelle || t.nom || '').trim().toLowerCase();
            if (!tLib || secLib === 'section sans nom') return;

            if (secLib.includes(tLib)) {
              if (tLib.length > maxLength) {
                maxLength = tLib.length;
                bestMatch = t;
              }
            }
          });

          if (bestMatch) {
            typeSectionId = bestMatch.id;
          }
        }

        const libelleSection = resolveSectionDisplayTitle(
          { typeSectionId, libelleSection: sec.libelleSection, nom: sec.nom },
          store.typesSection
        );
        const nom = nettoyerNomSection(
          sec.libelleSection || libelleSection,
          typeSectionId,
          store.typesSection,
          sec.frequenceLibelle || '',
          sec.regleEchantillonnageLibelle || ''
        );

        const lignesTriees = [...(sec.lignes || [])].sort((a, b) =>
          (a.ordreAffiche || 0) - (b.ordreAffiche || 0)
        );

        return {
          id: sec.id,
          isFromDb: true,
          typeSectionId,
          nom,
          ...freqData,
          isNewFreq: false,
          libelleSection,
          lignes: lignesTriees.map(lig => ({ 
            id: lig.id,
            isFromDb: true,
            typeCaracteristiqueId: lig.typeCaracteristiqueId,
            libelleAffiche: lig.libelleAffiche || '',
            typeControleId: lig.typeControleId,
            moyenControleId: lig.moyenControleId,
            instrumentCode: lig.instrumentCode,
            instruction: lig.instruction || '',
            estCritique: lig.estCritique,
            unite: lig.unite || '',
            limiteSpecTexte: lig.limiteSpecTexte || '',
            observations: lig.observations || '',
            moyenTexteLibre: lig.moyenTexteLibre || '',
            imageBase64: lig.imageBase64 || null,
            valeursColonnesSpecifiques: lig.extraColonnes 
              ? Object.fromEntries(lig.extraColonnes.map(ec => [ec.cleColonne, ec.valeurColonne])) 
              : (lig.colonnesSupplementaires ? JSON.parse(lig.colonnesSupplementaires) : {})
          }))
        };
      });
      
      if (ctx.isUpgradeMode.value) {
        toast.add({ severity: 'info', summary: 'Mode Édition Activé', detail: 'Modifiez la structure (mise à niveau avec le PRC actif), puis cliquez sur "Enregistrer la Nouvelle Version".', life: 5000 });
      }
      
      if (!ctx.isForcedView.value) {
        setTimeout(() => {
          ctx.initializeSnapshot(createModeleSnapshot(store.entete, ctx.groupes.value));
        }, 100);
      }

    } catch (e) {
      console.error(e);
      toast.add({ severity: 'error', summary: 'Introuvable', detail: 'Modèle introuvable.', life: 5000 });
      router.push(ctx.returnUrl.value);
    } finally {
      store.isLoading = false;
      await nextTick();
      setTimeout(() => {
        store.isBeingLoaded = false;
      }, 50);
    }
  };

  const resetForNewModele = () => {
    ctx.modeleEditionId.value = null;
    ctx.codeOriginal.value = '';
    ctx.statut.value = 'BROUILLON';
    ctx.version.value = 0;
    const preservedRef = store.entete.refFormulaireCodeReference;
    const preservedCols = store.entete.configurationColonnes;

    store.entete = { 
      ...store.entete,
      operationCode: '', 
      natureComposantCode: '', 
      typeRobinetCode: '', 
      libelle: '', 
      notes: '', 
      legendeMoyens: '', 
      posteCode: '',
      familleProduitCode: '',
      refFormulaireCodeReference: preservedRef || '',
      configurationColonnes: preservedCols || []
    };
    ctx.groupes.value = [];

    store.applyFormulaireConfiguration();
    
    setTimeout(() => {
      ctx.initializeSnapshot(createModeleSnapshot(store.entete, ctx.groupes.value));
    }, 100);
  };

  return {
    loadModelForEditing,
    resetForNewModele
  };
}
