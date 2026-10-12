---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-07
tags: [arquitectura, repositorio, versiones, acuerdos]
---

# Repositorio de software y versiones

## Decisiones y alcance del 7 de octubre

Bryan confirmó .NET 8 y Angular 22 y encargó completar las recomendaciones técnicas. Se conserva el diseño incorporado por el compañero en el commit `4c3a9ef`: Clean Architecture/CQRS, FSD, Signals/RxJS, shared-core, los perfiles A/B y el pipeline GitHub Actions/GHCR. Esta nota concreta versiones y rutas; no acredita que las aplicaciones estén implementadas.

- Bóveda: [istpetdev-hackathon-2026](https://github.com/bryancito1090/istpetdev-hackathon-2026).
- Software: [istpetdev-platform](https://github.com/bryancito1090/istpetdev-platform), privado, creado vacío por instrucción de Bryan.
- Carpeta local: `~/Projects/ISTPETDEV`.
- Entrega inicial: directorios, `.gitkeep`, README, `.gitignore` y `.env.example`; sin proyectos generados, dependencias instaladas, commits ni push del esqueleto. Bryan incorporará colaboradores.

## Estructura acordada

```text
ISTPETDEV/
  .github/workflows/              CI, builds, deploy y rollback; implementación pendiente
  src/
    Api/                         HTTP, identidad, SignalR y composición
    Application/                 Casos de uso, comandos, consultas y DTOs
    Domain/                      Inventario, logística y custodia
    Infrastructure/              EF Core/Npgsql, S3, mapas y adaptadores
  apps/
    web/src/
      app/                       Arranque, rutas, providers y shell
      pages/                     Pantallas, incluida public-trace
      widgets/                   Composición, incluido route-map
      features/                  Acciones reutilizadas
      entities/                  Punto, producto, lote, ruta, entrega y riesgo
      shared/                    API base, UI, configuración y utilidades
    mobile/src/app/              Ionic/PWA; no imponer FSD móvil automáticamente
  libs/shared-core/src/
    contracts/
    models/
    units/
    validation/
  infra/
    terraform/
      bootstrap/
      environments/dev/          RDS compartido privado y acceso SSM
      environments/demo/         Perfil A: EC2/Compose y S3
      environments/prod/         Perfil B nacional; no aprovisionar por anticipación
      modules/
        vpc/
        security_groups/
        ec2_hardened/
        ecs_fargate/
        rds_postgres/
        s3_storage/
        iam/
    compose/                     Dependencias locales y stack del Perfil A
    nginx/
    cloud-init/
  automation/n8n/workflows/       Exportaciones sin credenciales del n8n existente
  scripts/
    base_datos/                  Migración y seed controlados
    deploy/                      Releases y rollback
    backup/                      Backup, restauración y comprobaciones
    ops/                         Encendido, apagado y acceso
  tests/
    unit/
    integration/
    e2e/
```

Los directorios vacíos son únicamente el mapa solicitado. No se generan slices, interfaces, casos de uso o módulos ficticios. El backend vive en `src/**`; los filtros de Actions deben usar esa ruta, no asumir `backend/**`. Los cambios en `libs/shared-core/**` afectan web y móvil.

## Versiones iniciales

| Componente | Selección | Criterio de implementación |
|---|---|---|
| .NET / ASP.NET Core | **8.0**, decisión de Bryan | Fijar SDK 8.0 y parche vigente en `global.json` al generar la solución; runtime e imágenes en la misma rama, con digest registrado |
| EF Core / Npgsql EF / NetTopologySuite | **8.x** | Misma generación que .NET/EF Core; fijar parches exactos en gestión central de paquetes y lockfiles |
| Angular core/compiler/router/forms/service-worker | **22.2.1** | Paquetes del framework alineados; web y móvil comparten versión |
| Angular CLI / build | **22.2.2** | Tooling compatible con Angular 22.2; registrar lockfile |
| Node.js | **22.23.3 LTS** | Conserva Node 22 del compañero; cumple el mínimo `22.22.3` de Angular 22 |
| npm | **10.9.9** | Versión distribuida con ese Node; instalaciones CI con `npm ci` |
| TypeScript | **6.0.3** | Angular build 22 exige `>=6.0 <6.1`; no usar automáticamente el latest 7 |
| RxJS | **7.8.2** | Signals para UI y RxJS para los flujos definidos en ADR-04 |
| Ionic Angular | **8.8.19** | Conserva Ionic 8; sus peer dependencies admiten Angular 22. Validar navegación, controles y build en la entrega vertical |
| Capacitor | **8.5.2** | Recomendación para empaquetado nativo posterior; core/CLI/android/ios alineados. La primera entrega es PWA |
| Tailwind CSS | **3.4.19** | Conserva la rama 3.4 del compañero y evita migración de estilos; compatible con el builder Angular 22 |
| PostgreSQL | **16**, último parche 16 soportado al aprovisionar | Misma rama en RDS y Docker; registrar el minor exacto y digest en el manifest |
| PostGIS | **3.x compatible con PostgreSQL 16 seleccionado** | Elegir la misma extensión disponible en RDS/local y registrar `postgis_full_version()`; no asumir UUID v7 nativo en PG16 |
| Redis | **7.4**, parche/digest fijado al crear Compose | Mantiene Redis 7 del diseño; backplane solo si hay varias API o una necesidad comprobada |
| Linux | **Ubuntu Server 24.04 LTS, amd64** | AMI oficial Canonical fijada por ID/región; actualizaciones de seguridad y reinicio ensayados |
| Docker Engine / Compose | **Engine soportado >=28; Compose v2 soportado** | Fijar versiones de paquetes en cloud-init; >=28 evita la exposición localhost de versiones anteriores |
| Terraform / AWS provider | **Terraform >=1.10 y AWS provider ~>5.50** | Conserva la selección del compañero; fijar release del CLI y `.terraform.lock.hcl` en el primer IaC |
| n8n | **Versión instalada en el servidor de Bryan** | Inventariar antes de importar workflows; esta tarea no cambia ni actualiza el servidor |

Las selecciones npm exactas se verificaron el 7 de octubre en los registros oficiales. Antes del scaffold comprobar disponibilidad, advisories y los peer dependencies completos; el proyecto todavía no tiene build ni validación de dispositivos. Los parches de AWS/AMI/PostGIS deben fijarse cuando exista la región y recurso real, no inventarse en el esqueleto.

.NET 8 se mantiene por instrucción expresa. Su soporte termina el 10 de noviembre de 2026: registrar antes de esa fecha una tarea de actualización para continuidad después del evento; no cambiar ahora a .NET 10.

## Flujo del repositorio

`feature/*`, `bugfix/*` y `chore/*` se integran por PR a `develop`. `main` recibe releases estabilizadas desde `develop`. CI valida PRs y `develop`; build publica imágenes en GHCR; los despliegues AWS de prueba se activan manualmente para evitar encender infraestructura por cada push.

El repositorio remoto permanece sin commits ni ramas materializadas. Después del primer commit del equipo, crear `main`/`develop`, configurar `develop` como rama de trabajo y proteger integraciones con revisión y checks disponibles en el plan de GitHub. No crear ahora issues, colaboradores o una plantilla institucional de GitLab: Bryan pidió GitHub vacío y un esqueleto exclusivamente local.

## Fuentes de compatibilidad

- [Angular: Node/TypeScript/RxJS](https://angular.dev/reference/versions).
- [Registro oficial Angular build](https://registry.npmjs.org/@angular%2fbuild/22.2.2), [Ionic Angular](https://registry.npmjs.org/@ionic%2fangular/8.8.19).
- [Distribuciones Node oficiales](https://nodejs.org/dist/index.json), [Capacitor 8](https://capacitorjs.com/docs/updating/8-0).
- [Npgsql EF 8](https://www.npgsql.org/efcore/release-notes/8.0.html), [soporte .NET](https://dotnet.microsoft.com/en-us/platform/support/policy).
- [PostGIS en RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Appendix.PostgreSQL.CommonDBATasks.PostGIS.html), [Docker: publicación de puertos](https://docs.docker.com/engine/network/port-publishing/).

Ver [[Entornos y operacion acordados]], [[Identidad OIDC y sesiones]], [[Frontend con Feature-Sliced Design]] y [[Backend y tiempo real]].
