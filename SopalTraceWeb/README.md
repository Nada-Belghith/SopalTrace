# SopalTraceWeb

Front de SopalTrace (Vue 3, Vite, Tailwind). Présentation de l'application, installation et lancement : voir le
[README principal](../README.md).

```sh
npm ci              # dépendances (versions du lockfile)
npm run dev         # développement : http://localhost:5173 (VITE_API_URL dans .env.local pour une API locale)
npm run lint:check  # lint
npm test            # tests unitaires (Vitest)
npm run build       # build de production (servi par nginx, voir Dockerfile et nginx.conf)
```
