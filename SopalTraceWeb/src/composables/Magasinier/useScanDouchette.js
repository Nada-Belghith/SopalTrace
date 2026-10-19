import { ref, onMounted, onBeforeUnmount, nextTick } from 'vue';

/**
 * Saisie à la douchette (scanner en émulation clavier : il « tape » le code puis Entrée).
 *
 * - Entrée valide la saisie ; le contenu brut est transmis tel quel (le serveur le nettoie et le lit).
 * - Focus permanent : une frappe faite hors d'un champ de saisie (focus perdu après un clic ou une notification)
 *   est redirigée vers le champ de scan actif, pour qu'aucun scan ne soit perdu.
 * - Anti double scan : le même code relu dans le délai `delaiDoublonMs` est ignoré.
 * - File d'attente : un scan arrivé pendant le traitement du précédent est traité ensuite, dans l'ordre.
 * - Rafale ou frappe : l'intervalle entre les touches indique si le code vient de la douchette ou du clavier (affichage).
 * - Retour sonore et vibration : succès (bip aigu) ou erreur (double bip grave).
 *
 * @param {object} options
 * @param {(contenu: string) => Promise<void>} options.onScan   traitement d'un scan (lève une erreur en cas de refus)
 * @param {() => boolean} [options.estActif]                    ce champ reçoit-il les frappes redirigées ?
 * @param {number} [options.delaiDoublonMs]
 */
export function useScanDouchette({ onScan, estActif = () => true, delaiDoublonMs = 1500 }) {
  const valeur = ref('');
  const champ = ref(null);
  const enCours = ref(false);
  /** 'DOUCHETTE' ou 'CLAVIER' pour la dernière saisie validée */
  const origine = ref(null);

  const INTERVALLE_RAFALE_MS = 35;
  let frappes = [];
  let dernier = { contenu: null, instant: 0 };
  const file = [];

  const focus = () => nextTick(() => champ.value?.focus());

  const estChampDeSaisie = (el) =>
    el && (el.tagName === 'INPUT' || el.tagName === 'TEXTAREA' || el.tagName === 'SELECT' || el.isContentEditable);

  const traiterFile = async () => {
    if (enCours.value) return;
    enCours.value = true;
    try {
      while (file.length > 0) {
        const contenu = file.shift();
        try {
          await onScan(contenu);
          signal(true);
        } catch {
          signal(false);
        }
      }
    } finally {
      enCours.value = false;
      focus();
    }
  };

  /** Valide un contenu (saisi, scanné ou lu par la caméra). */
  const soumettre = (brut) => {
    const contenu = (brut ?? '').replace(/[\r\n\t]/g, '').trim();
    valeur.value = '';
    if (!contenu) return;

    const maintenant = Date.now();
    if (contenu === dernier.contenu && maintenant - dernier.instant < delaiDoublonMs) return;
    dernier = { contenu, instant: maintenant };

    file.push(contenu);
    traiterFile();
  };

  /** À brancher sur @keydown du champ de scan. */
  const surTouche = (e) => {
    frappes.push(performance.now());
    if (e.key !== 'Enter') return;
    e.preventDefault();
    const intervalles = frappes.slice(1).map((t, i) => t - frappes[i]);
    const moyenne = intervalles.length ? intervalles.reduce((a, b) => a + b, 0) / intervalles.length : Infinity;
    origine.value = intervalles.length >= 3 && moyenne < INTERVALLE_RAFALE_MS ? 'DOUCHETTE' : 'CLAVIER';
    frappes = [];
    soumettre(valeur.value);
  };

  // Frappes hors champ : redirigées vers le champ actif (la douchette « tape » même si le focus est ailleurs).
  const redirigerFrappe = (e) => {
    if (!estActif() || estChampDeSaisie(e.target) || e.ctrlKey || e.altKey || e.metaKey) return;
    if (e.key === 'Enter') {
      if (valeur.value) { e.preventDefault(); surTouche(e); }
      return;
    }
    if (e.key.length !== 1) return;
    e.preventDefault();
    frappes.push(performance.now());
    valeur.value += e.key;
    focus();
  };

  let audio = null;
  const bip = (frequence, debut, duree) => {
    try {
      audio ??= new (window.AudioContext || window.webkitAudioContext)();
      const osc = audio.createOscillator();
      const gain = audio.createGain();
      osc.frequency.value = frequence;
      gain.gain.value = 0.15;
      osc.connect(gain).connect(audio.destination);
      osc.start(audio.currentTime + debut);
      osc.stop(audio.currentTime + debut + duree);
    } catch { /* pas de son disponible : le retour visuel suffit */ }
  };

  const signal = (succes) => {
    if (succes) {
      bip(1200, 0, 0.08);
    } else {
      bip(300, 0, 0.15);
      bip(300, 0.22, 0.15);
      navigator.vibrate?.([200, 100, 200]);
    }
  };

  onMounted(() => window.addEventListener('keydown', redirigerFrappe));
  onBeforeUnmount(() => window.removeEventListener('keydown', redirigerFrappe));

  return { valeur, champ, enCours, origine, surTouche, soumettre, focus };
}
