/**
 * DTOs - Data Transfer Objects
 * Définition des structures de données pour les requêtes/réponses API
 */

// ============================================
// AUTH DTOs
// ============================================

export class LoginRequestDTO {
  constructor(email, password) {
    this.email = email
    this.password = password
  }
}

export class RegisterRequestDTO {
  constructor(email, password, name) {
    this.email = email
    this.password = password
    this.name = name
  }
}

export class AuthResponseDTO {
  constructor(token, refreshToken, user, expiresIn) {
    this.token = token
    this.refreshToken = refreshToken
    this.user = user
    this.expiresIn = expiresIn // Secondes
  }
}

export class UserDTO {
  constructor(id, email, name, roles = [], avatar = null) {
    this.id = id
    this.email = email
    this.name = name
    this.roles = roles // ['admin', 'user', etc.]
    this.avatar = avatar
    this.createdAt = new Date()
  }
  
  hasRole(role) {
    return this.roles.includes(role)
  }
  
  hasAnyRole(roles) {
    return roles.some(role => this.roles.includes(role))
  }
  
  hasAllRoles(roles) {
    return roles.every(role => this.roles.includes(role))
  }
}

export class RefreshTokenRequestDTO {
  constructor(refreshToken) {
    this.refreshToken = refreshToken
  }
}

export class TokenResponseDTO {
  constructor(token, expiresIn) {
    this.token = token
    this.expiresIn = expiresIn
  }
}

// ============================================
// TRACE DTOs
// ============================================

export class TraceDTO {
  constructor(id, name, status, date, userId = null) {
    this.id = id
    this.name = name
    this.status = status // 'Complet', 'En attente', 'Erreur'
    this.date = date
    this.userId = userId
  }
}

export class TraceListResponseDTO {
  constructor(items = [], totalCount = 0, pageNumber = 1, pageSize = 10) {
    this.items = items
    this.totalCount = totalCount
    this.pageNumber = pageNumber
    this.pageSize = pageSize
    this.totalPages = Math.ceil(totalCount / pageSize)
  }
}

export class CreateTraceRequestDTO {
  constructor(name, description = null) {
    this.name = name
    this.description = description
  }
}

// ============================================
// ERROR DTOs
// ============================================

export class ErrorResponseDTO {
  constructor(message, code = null, details = null) {
    this.message = message
    this.code = code
    this.details = details
    this.timestamp = new Date()
  }
}

export class ValidationErrorDTO {
  constructor(fieldErrors = []) {
    this.message = 'Erreur de validation'
    this.errors = fieldErrors // Array of {field, message}
  }
}

// ============================================
// PAGINATION DTO
// ============================================

export class PaginationRequestDTO {
  constructor(pageNumber = 1, pageSize = 10, sortBy = null, sortOrder = 'asc') {
    this.pageNumber = pageNumber
    this.pageSize = pageSize
    this.sortBy = sortBy
    this.sortOrder = sortOrder // 'asc' ou 'desc'
  }
}

export class PaginatedResponseDTO {
  constructor(data, totalCount, pageNumber, pageSize) {
    this.data = data
    this.totalCount = totalCount
    this.pageNumber = pageNumber
    this.pageSize = pageSize
    this.totalPages = Math.ceil(totalCount / pageSize)
    this.hasNextPage = pageNumber < this.totalPages
    this.hasPreviousPage = pageNumber > 1
  }
}

// ============================================
// FILTER DTOs
// ============================================

export class FilterRequestDTO {
  constructor(filters = {}, search = '', pagination = null) {
    this.filters = filters
    this.search = search
    this.pagination = pagination || new PaginationRequestDTO()
  }
}