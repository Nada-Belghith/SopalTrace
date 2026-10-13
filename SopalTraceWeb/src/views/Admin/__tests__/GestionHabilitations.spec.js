import { describe, it, expect, vi, beforeEach } from 'vitest'
import { mount, flushPromises } from '@vue/test-utils'
import { setActivePinia, createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import ToastService from 'primevue/toastservice'

vi.mock('@/router', () => ({ default: { push: vi.fn() } }))
vi.mock('@/services/authService', () => ({ authService: {} }))
vi.mock('@/composables/useConfirmation', () => ({ confirmer: vi.fn().mockResolvedValue({ confirme: true, valeur: null }) }))
vi.mock('@/services/adminService', () => ({
  default: {
    getRoles: vi.fn(),
    getDashboard: vi.fn(),
    searchUsers: vi.fn(),
    approve: vi.fn(),
    reject: vi.fn(),
    resendInvitation: vi.fn(),
    changeRole: vi.fn(),
    setDisabled: vi.fn(),
    unlock: vi.fn(),
    revokeSessions: vi.fn(),
    getPreassignedRoles: vi.fn(),
    setPreassignedRole: vi.fn(),
    deletePreassignedRole: vi.fn(),
    getConnectionLog: vi.fn()
  }
}))

import adminService from '@/services/adminService'
import { useAuthStore } from '@/stores/authStore'
import GestionHabilitations from '../GestionHabilitations.vue'

const compte = (matricule, nom, status, extra = {}) => ({
  matricule, fullName: nom, email: `${matricule}@sopal.tn`, role: 'OPERATEUR', roleLabel: 'Opérateur',
  jobTitle: null, status, createdAt: '2026-10-01T08:00:00', lastLoginAt: null, lockoutEndsAt: null, activeSessions: 0,
  invitationExpiresAt: null, reviewedBy: null, reviewedAt: null, reviewComment: null, ...extra
})

const monter = () => mount(GestionHabilitations, { global: { plugins: [PrimeVue, ToastService] } })
const bouton = (wrapper, texte) => wrapper.findAll('button').find(b => b.text().includes(texte))

describe('GestionHabilitations', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    setActivePinia(createPinia())
    useAuthStore().user = { matricule: '66666', role: 'ADMIN' }
    adminService.getRoles.mockResolvedValue([{ code: 'OPERATEUR', label: 'Opérateur' }, { code: 'MAGASINIER', label: 'Magasinier' }])
    adminService.getDashboard.mockResolvedValue({
      activeAccounts: 3, pendingApprovalAccounts: 1, invitedAccounts: 1, rejectedAccounts: 0, lockedAccounts: 1, disabledAccounts: 0
    })
    adminService.searchUsers.mockResolvedValue({
      items: [
        compte('77777', 'Nouvel Employé', 'PENDING_APPROVAL', { role: null, roleLabel: null, jobTitle: 'Technicien' }),
        compte('55555', 'Opérateur Test', 'ACTIVE', { activeSessions: 2 }),
        compte('22222', 'Qualité Test', 'LOCKED'),
        compte('66666', 'Admin Doc', 'ACTIVE', { role: 'ADMIN', roleLabel: 'Administrateur' })
      ],
      total: 4, page: 1, pageSize: 20
    })
    adminService.getPreassignedRoles.mockResolvedValue([])
  })

  it('affiche les comptes, leur statut et les demandes à traiter', async () => {
    const wrapper = monter()
    await flushPromises()

    const texte = wrapper.text()
    expect(texte).toContain('Opérateur Test')
    expect(texte).toContain('Verrouillé')
    expect(texte).toContain('2 session(s) ouverte(s)')
    expect(texte).toContain('Demande à traiter')
    expect(texte).toContain('À attribuer')
    expect(wrapper.find('[data-test="bandeau-demandes"]').text()).toContain('1')
  })

  it("l'administrateur ne peut pas modifier son propre rôle", async () => {
    const wrapper = monter()
    await flushPromises()

    const lignes = wrapper.findAll('tbody tr')
    const boutonRole = (ligne) => ligne.find('button[title="Changer le rôle"]')
    expect(boutonRole(lignes[1]).attributes('disabled')).toBeUndefined()
    expect(boutonRole(lignes[3]).attributes('disabled')).toBeDefined()
    expect(lignes[3].text()).toContain('(vous)')
  })

  it('approuver une demande exige un rôle puis envoie le lien', async () => {
    adminService.approve.mockResolvedValue({ account: {}, emailSent: true })
    const wrapper = monter()
    await flushPromises()

    await bouton(wrapper, 'Approuver').trigger('click')
    const valider = wrapper.find('[data-test="valider-decision"]')
    expect(valider.attributes('disabled')).toBeDefined()

    await wrapper.find('[data-test="role-decision"]').setValue('MAGASINIER')
    await valider.trigger('click')
    await flushPromises()

    expect(adminService.approve).toHaveBeenCalledWith('77777', 'MAGASINIER', null)
  })

  it('refuser une demande exige un motif', async () => {
    adminService.reject.mockResolvedValue({})
    const wrapper = monter()
    await flushPromises()

    await bouton(wrapper, 'Refuser').trigger('click')
    const valider = wrapper.find('[data-test="valider-decision"]')
    await wrapper.find('[data-test="motif-refus"]').setValue('non')
    expect(valider.attributes('disabled')).toBeDefined()

    await wrapper.find('[data-test="motif-refus"]').setValue('Prestataire externe sans besoin')
    await valider.trigger('click')
    await flushPromises()

    expect(adminService.reject).toHaveBeenCalledWith('77777', 'Prestataire externe sans besoin')
  })

  it('déverrouiller un compte appelle l\'API puis recharge la liste', async () => {
    adminService.unlock.mockResolvedValue({})
    const wrapper = monter()
    await flushPromises()
    adminService.searchUsers.mockClear()

    await wrapper.find('button[title="Déverrouiller"]').trigger('click')
    await flushPromises()

    expect(adminService.unlock).toHaveBeenCalledWith('22222')
    expect(adminService.searchUsers).toHaveBeenCalled()
  })

  it('changer un rôle envoie le nouveau rôle choisi', async () => {
    adminService.changeRole.mockResolvedValue({})
    const wrapper = monter()
    await flushPromises()

    await wrapper.findAll('button[title="Changer le rôle"]')[0].trigger('click')
    // La fenêtre de changement de rôle ajoute son propre sélecteur, après les filtres.
    await wrapper.findAll('select').at(-1).setValue('MAGASINIER')
    await bouton(wrapper, 'Enregistrer').trigger('click')
    await flushPromises()

    expect(adminService.changeRole).toHaveBeenCalledWith('55555', 'MAGASINIER')
  })

  it('pré-attribuer un rôle à un matricule avant son inscription', async () => {
    adminService.setPreassignedRole.mockResolvedValue({ matricule: '88888', fullName: 'Futur Employé', roleLabel: 'Magasinier' })
    const wrapper = monter()
    await flushPromises()

    await bouton(wrapper, 'Rôles pré-attribués').trigger('click')
    await flushPromises()
    expect(adminService.getPreassignedRoles).toHaveBeenCalled()

    const formulaire = wrapper.find('form')
    await formulaire.find('input').setValue(' 88888 ')
    await formulaire.find('select').setValue('MAGASINIER')
    await formulaire.trigger('submit')
    await flushPromises()

    expect(adminService.setPreassignedRole).toHaveBeenCalledWith('88888', 'MAGASINIER')
  })
})
