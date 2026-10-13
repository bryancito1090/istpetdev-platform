# ==============================================================================
# Dockerfile: Backend .NET 8 Web API (Clean Architecture)
# Multi-stage build con imagen Alpine segura y optimizada
# ==============================================================================

# Etapa 1: Compilación y restauración de dependencias
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copiar csproj para aprovechar caché de capas de Docker en restore
COPY ["src/Domain/IstpetDev.Domain.csproj", "Domain/"]
COPY ["src/Application/IstpetDev.Application.csproj", "Application/"]
COPY ["src/Infrastructure/IstpetDev.Infrastructure.csproj", "Infrastructure/"]
COPY ["src/Api/IstpetDev.Api.csproj", "Api/"]

RUN dotnet restore "Api/IstpetDev.Api.csproj"

# Copiar el código fuente completo
COPY src/ .

# Compilar y publicar artefactos en modo Release
WORKDIR "/src/Api"
RUN dotnet publish "IstpetDev.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime ligero y endurecido
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Instalar utilidades de diagnóstico mínimas requeridas
RUN apk add --no-cache curl tzdata

# Variables de entorno operativas
ENV ASPNETCORE_URLS=http://+:5000 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# Copiar artefactos compilados desde la etapa de build
COPY --from=build /app/publish .
COPY data/fixtures/ /app/data/fixtures/

# Ejecución bajo usuario no privilegiado (nativo en .NET 8 alpine)
USER $APP_UID

EXPOSE 5000

# Verificación de salud interna del contenedor
HEALTHCHECK --interval=15s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5000/health/ready || exit 1

ENTRYPOINT ["dotnet", "IstpetDev.Api.dll"]
