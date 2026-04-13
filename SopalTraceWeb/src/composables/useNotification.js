import { ref, computed } from 'vue'

export function useNotification() {
  const notifications = ref([])
  let id = 0

  const addNotification = (message, type = 'info', duration = 3000) => {
    const notificationId = id++
    const notification = {
      id: notificationId,
      message,
      type,
      timestamp: new Date()
    }

    notifications.value.push(notification)

    if (duration > 0) {
      setTimeout(() => {
        removeNotification(notificationId)
      }, duration)
    }

    return notificationId
  }

  const removeNotification = (notificationId) => {
    notifications.value = notifications.value.filter(
      (n) => n.id !== notificationId
    )
  }

  const clearAll = () => {
    notifications.value = []
  }

  const success = (message, duration = 3000) => {
    return addNotification(message, 'success', duration)
  }

  const error = (message, duration = 5000) => {
    return addNotification(message, 'error', duration)
  }

  const warning = (message, duration = 4000) => {
    return addNotification(message, 'warning', duration)
  }

  const info = (message, duration = 3000) => {
    return addNotification(message, 'info', duration)
  }

  return {
    notifications: computed(() => notifications.value),
    addNotification,
    removeNotification,
    clearAll,
    success,
    error,
    warning,
    info
  }
}