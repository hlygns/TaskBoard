# 1) Derleme: TypeScript kontrolü + Vite ile statik dosyalar (dist/).
FROM node:22-alpine AS build
WORKDIR /app

# Önce sadece paket dosyaları: bağımlılıklar değişmedikçe "npm ci" katmanı cache'ten gelir.
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# 2) Sunma: nginx statik dosyaları verir, /api ve /hubs isteklerini API container'ına iletir.
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
