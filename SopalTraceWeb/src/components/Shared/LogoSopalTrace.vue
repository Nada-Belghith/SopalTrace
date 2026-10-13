<template>
  <svg viewBox="0 0 64 64" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="SopalTrace">
    <rect width="64" height="64" rx="14" fill="#4A93EA" />
    <g fill="none" stroke="#10233F" stroke-linecap="round" stroke-linejoin="round">
      <!-- Chemin de traçabilité en S -->
      <path d="M44.5 14.9 H17 A8.9 8.9 0 0 0 17 32.7 H25.6 M36.9 32.7 H47 A8.35 8.35 0 0 1 47 49.4 H18.6"
            stroke-width="1.9" stroke-dasharray="3.4 2.4" />
      <!-- Diagonale qui relie les étapes -->
      <path d="M16.4 46 L27.4 36.1 M35.2 29.3 L46.4 18.3" stroke-width="1.9" stroke-dasharray="3.4 2.4" />
      <!-- Étapes -->
      <circle cx="49.8" cy="14.9" r="4.6" stroke-width="2.1" />
      <circle cx="12.9" cy="49.4" r="4.6" stroke-width="2.1" />
      <circle cx="31.3" cy="32.7" r="5.4" stroke-width="2.1" />
      <path d="M28.9 32.9 L30.7 34.7 L33.9 31.1" stroke-width="1.9" />
    </g>
  </svg>
</template>
