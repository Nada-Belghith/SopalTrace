import apiClient from './apiClient'

export default {
  getExecution(execControleOfId, posteCode = null) {
    const params = posteCode ? { posteCode } : {}
    return apiClient.get(`/ExecPlanAssemblage/${execControleOfId}`, { params })
  },
  configurerHoraires(execControleOfId, data) {
    return apiClient.post(`/ExecPlanAssemblage/${execControleOfId}/config-horaires`, data)
  },
  saveResultat(execControleOfId, data) {
    return apiClient.post(`/ExecPlanAssemblage/${execControleOfId}/resultat`, data)
  },
  cloturerExecution(execControleOfId) {
    return apiClient.post(`/ExecPlanAssemblage/${execControleOfId}/cloturer`)
  }
}
