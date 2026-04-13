
// Auth guards removed — stubs kept so imports don't break. All guards are now pass-through.
export const canActivate = (to, from, next) => { next(); return true }
export const canActivateByRole = (to, from, next) => { next(); return true }
export const canActivateByPermission = (to, from, next) => { next(); return true }
export const canActivateByAllRoles = (to, from, next) => { next(); return true }
export const redirectIfAuthenticated = (to, from, next) => { next(); return true }