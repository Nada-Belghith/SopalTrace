import apiClient from './apiClient';

const compte = (matricule) => `/admin/users/${encodeURIComponent(matricule)}`;

/** Administration des comptes (rôle ADMIN). L'administrateur agissant est identifié par son jeton côté serveur. */
const adminService = {
  getDashboard: () => apiClient.get('/admin/dashboard').then(r => r.data),
  getRoles: () => apiClient.get('/admin/roles').then(r => r.data),

  /** @param {{ search?: string, role?: string, status?: string, page?: number, pageSize?: number }} criteres */
  searchUsers: (criteres) => apiClient.get('/admin/users', { params: criteres }).then(r => r.data),

  // Demandes d'accès : renvoient { account, emailSent } pour approuver / renvoyer l'invitation.
  approve: (matricule, role, comment) => apiClient.post(`${compte(matricule)}/approve`, { role, comment }).then(r => r.data),
  reject: (matricule, reason) => apiClient.post(`${compte(matricule)}/reject`, { reason }).then(r => r.data),
  resendInvitation: (matricule) => apiClient.post(`${compte(matricule)}/resend-invitation`).then(r => r.data),

  changeRole: (matricule, role) => apiClient.put(`${compte(matricule)}/role`, { role }).then(r => r.data),
  setDisabled: (matricule, isDisabled) => apiClient.put(`${compte(matricule)}/disabled`, { isDisabled }).then(r => r.data),
  unlock: (matricule) => apiClient.post(`${compte(matricule)}/unlock`).then(r => r.data),
  revokeSessions: (matricule) => apiClient.post(`${compte(matricule)}/revoke-sessions`).then(r => r.data),

  // Rôles pré-attribués avant l'inscription de l'employé
  getPreassignedRoles: () => apiClient.get('/admin/preassigned-roles').then(r => r.data),
  setPreassignedRole: (matricule, role) => apiClient.put(`/admin/preassigned-roles/${encodeURIComponent(matricule)}`, { role }).then(r => r.data),
  deletePreassignedRole: (matricule) => apiClient.delete(`/admin/preassigned-roles/${encodeURIComponent(matricule)}`),

  /** @param {{ matricule?: string, action?: string, page?: number, pageSize?: number }} criteres */
  getConnectionLog: (criteres) => apiClient.get('/admin/connection-log', { params: criteres }).then(r => r.data)
};

export default adminService;
