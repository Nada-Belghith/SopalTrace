import { ref, computed } from 'vue'

export function usePagination(items = [], pageSize = 10) {
  const currentPage = ref(1)
  const itemsPerPage = ref(pageSize)
  const allItems = ref(items)

  const totalPages = computed(() =>
    Math.ceil(allItems.value.length / itemsPerPage.value)
  )

  const paginatedItems = computed(() => {
    const start = (currentPage.value - 1) * itemsPerPage.value
    const end = start + itemsPerPage.value
    return allItems.value.slice(start, end)
  })

  const onPageChange = (event) => {
    currentPage.value = event.page + 1
  }

  const setItems = (newItems) => {
    allItems.value = newItems
    currentPage.value = 1
  }

  const goToPage = (page) => {
    if (page >= 1 && page <= totalPages.value) {
      currentPage.value = page
    }
  }

  const nextPage = () => {
    if (currentPage.value < totalPages.value) {
      currentPage.value++
    }
  }

  const previousPage = () => {
    if (currentPage.value > 1) {
      currentPage.value--
    }
  }

  return {
    currentPage,
    itemsPerPage,
    totalPages,
    paginatedItems,
    onPageChange,
    setItems,
    goToPage,
    nextPage,
    previousPage
  }
}