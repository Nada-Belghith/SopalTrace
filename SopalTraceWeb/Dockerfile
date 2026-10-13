# Étape 1 : Construction (Build)
FROM node:20 AS build-stage
WORKDIR /app
# `npm ci` : versions exactes du lockfile (qui contient les binaires natifs de toutes les plateformes).
COPY package.json package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY . .
RUN npm run build

# Étape 2 : Production (Serveur Web)
# Image nginx sans droits root : écoute sur 8080 (port non privilégié).
FROM nginxinc/nginx-unprivileged:alpine AS production-stage
COPY --from=build-stage /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 8080
CMD ["nginx", "-g", "daemon off;"]
