/**
 * Périodicités machine dont le moment de contrôle (PeriodiciteMachine.ControlMoment : DEMARRAGE, APRES_PAUSE,
 * FIN_POSTE) est celui demandé. Comparaison de codes : le libellé n'est jamais interprété.
 * @param {Array<{ id: string, controlMoment?: string }>} periodicites
 * @param {string} moment
 * @returns {string[]}
 */
export function idsDuMoment(periodicites, moment) {
  return (periodicites || []).filter(p => moment && (p.controlMoment || p.ControlMoment) === moment).map(p => p.id || p.Id)
}

/**
 * Un groupe d'échéances s'affiche-t-il ? Liste d'identifiants (moment imposé) prioritaire, puis identifiant
 * unique (filtre manuel), sinon tout s'affiche.
 * @param {string} periodiciteMachineId
 * @param {{ id?: string|null, ids?: string[]|null }} filtre
 */
export function periodiciteAffichee(periodiciteMachineId, { id = null, ids = null } = {}) {
  if (Array.isArray(ids)) return ids.includes(periodiciteMachineId)
  return !id || periodiciteMachineId === id
}
