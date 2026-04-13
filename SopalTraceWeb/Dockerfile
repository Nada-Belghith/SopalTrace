# Étape 1 : Construction (Build)
FROM node:20-alpine AS build-stage
WORKDIR /app
COPY package.json ./
RUN npm install
COPY . .
RUN npm run build

# Étape 2 : Production (Serveur Web)
FROM nginx:alpine AS production-stage
# On copie le code compilé depuis l'étape 1 vers le dossier public de Nginx
COPY --from=build-stage /app/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
# On expose le port 80 (standard web)
EXPOSE 80
# Lancement de Nginx
CMD ["nginx", "-g", "daemon off;"]