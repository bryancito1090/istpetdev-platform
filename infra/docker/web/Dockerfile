# ==============================================================================
# Dockerfile: Frontend Web Angular 22 (SPA)
# Multi-stage build: Node 22 LTS -> Nginx Alpine
# ==============================================================================

# Etapa 1: Compilación de contratos y aplicación Angular
FROM node:22-alpine AS build
WORKDIR /app

# Copiar manifiestos de dependencias del monorepo
COPY package*.json ./
COPY libs/shared-core/package*.json libs/shared-core/
COPY apps/web/package*.json apps/web/
COPY apps/mobile/package*.json apps/mobile/

# Instalación limpia de dependencias
RUN npm ci

# Copiar y compilar la librería compartida
COPY libs/shared-core/ libs/shared-core/
RUN npm run build:shared

# Copiar archivos de configuración del monorepo (ESLint y Nx)
COPY eslint.config.mjs nx.json ./

# Copiar y compilar la aplicación Web
COPY apps/web/ apps/web/
RUN npm run build:web

# Etapa 2: Servidor web Nginx para entrega de archivos estáticos
FROM nginx:alpine AS runtime

# Limpiar archivos por defecto de nginx
RUN rm -rf /usr/share/nginx/html/*

# Copiar bundle de Angular compilado (soporta estructura @angular/build:application)
COPY --from=build /app/apps/web/dist/web/browser /usr/share/nginx/html/

# Copiar configuración Nginx optimizada para SPA
COPY infra/docker/web/nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=20s --timeout=3s --retries=3 \
  CMD wget --quiet --tries=1 --spider http://localhost/ || exit 1

CMD ["nginx", "-g", "daemon off;"]
