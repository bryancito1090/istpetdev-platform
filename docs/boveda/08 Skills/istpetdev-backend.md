---
tipo: skill
estado: vigente
actualizado: 2026-10-09
tags: [istpetdev, ia, skills]
---

# IstpetDev — backend

**Cuándo usarla:** Implementar o revisar el backend C#/.NET de IstpetDev. Usar al escribir comandos, consultas, handlers, endpoints, reglas de inventario, reservas, despacho, recepción idempotente, custodia, autorización RBAC, SignalR, outbox o trabajos de fondo.

> Documentación de la skill `istpetdev-backend`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-backend` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio. Leer antes [[istpetdev-contexto]] y [[istpetdev-contrato]].

## Leer primero

- `03 Arquitectura/Backend y tiempo real.md`: estructura, CQRS práctico, concurrencia, idempotencia, SignalR y operación.
- `03 Arquitectura/Arquitectura del sistema.md`: módulos, propiedad de los datos y límite transaccional.
- `04 Datos y algoritmos/Modelo de datos.md`: entidades e invariantes.
- `03 Arquitectura/Seguridad y evidencias.md`: matriz RBAC.
- `02 Producto/Requisitos y aceptacion.md`: RF/RNF que cada cambio debe citar.
- `03 Arquitectura/Repositorio de software y versiones.md`: estructura `src/` y versiones exactas de paquetes.
- `03 Arquitectura/Identidad OIDC y sesiones.md`: validación de tokens Cognito y RBAC.
- `04 Datos y algoritmos/Esquema completo de base de datos.md` y `03 Arquitectura/RBAC y configuracion del sistema.md`: ADR-16, concesiones administrables, RLS y configuración/plantillas versionadas; leer lo afectado.

## Hechos que no se cambian sin decisión

- .NET 8 con EF Core/Npgsql 8.x (ADR-01). No subir a .NET 10 ni fijar parches que no estén en `03 Arquitectura/Repositorio de software y versiones.md`.
- Estructura del repositorio: `src/Api`, `src/Application`, `src/Domain`, `src/Infrastructure` (ADR-12). No crear otra ni usar `backend/`.
- PostgreSQL es la fuente de verdad. Una API modular y una base; CQRS lógico, no dos bases ni microservicios por tabla.

## Reglas

- **Command separado de Query.** Los comandos protegen invariantes en una transacción; las consultas devuelven DTOs proyectados y paginados, sin efectos.
- **DTO siempre.** No exponer entidades de dominio en la API.
- **Errores tipados** (por ejemplo un `Result` con código estable) traducidos a Problem Details; no usar excepciones como control de flujo de negocio.
- **Dominio sin infraestructura.** El dominio no conoce EF Core, AWS, n8n ni DeepSeek.
- **Escritura crítica en un commit:** permiso vigente + idempotencia + versión + movimiento del ledger + proyección del saldo + outbox.
- **Concurrencia:** decrementos con bloqueo, versión o actualización condicional; nunca leer el saldo y escribir sin protección.
- **Duplicado de negocio:** una clave nueva no permite recibir dos veces las mismas cantidades.
- **RBAC:** denegar por defecto; actor, organización y ámbito desde la sesión; las mismas políticas en API, SignalR, evidencias y reintentos offline.
- **Mediador opcional.** No agregar MediatR ni un repositorio genérico sobre EF Core sin una necesidad concreta. MediatR cambió a licencia comercial en 2025: verificar la licencia antes de agregarlo.
- **Trazabilidad:** cada handler o endpoint cita en un comentario el requisito que implementa, por ejemplo `// RF-06, RNF-02`. Si no hay requisito, se marca `// PENDIENTE: sin requisito` y se pregunta.
- **Cambios pequeños:** preferir diffs a reescribir archivos completos.
- **Operación:** logs estructurados con `correlationId` y `operationId`; no registrar firmas, tokens ni payloads sensibles; health checks de proceso y de dependencias; migraciones como paso único del despliegue.

## Prohibido inventar

- Reglas de stock, cobertura o rutas que no estén en `04 Datos y algoritmos/Inventario y prediccion.md` o `04 Datos y algoritmos/Rutas y sobrecostos.md`.
- Endpoints o eventos fuera del contrato.
- Comandos de build o test: PENDIENTES en `00 Inicio/Hechos canonicos.md` mientras el repositorio de código esté vacío.

## Verificar

- Pruebas unitarias para reglas de dominio y handlers (camino feliz y al menos un error).
- Pruebas de integración con PostgreSQL real para transacciones, concurrencia e idempotencia (casos V-01, V-02, V-09 y V-10 de `07 Demo/Plan de validacion.md`).
- Revisión con [[istpetdev-revision]] antes del PR.
- Antes de levantar una versión o preparar su exposición/despliegue, usar [[istpetdev-prelaunch]]; reutiliza evidencia de esta skill y agrega los controles complementarios, sin presentar documentación como checks ejecutados.

## Origen

Adaptada de las skills personales `backend-lead-cqrs` (Command/Query, DTO, errores tipados, diffs, trazabilidad a requisitos) y `arquitecto-clean-architecture` (dependencias hacia adentro). Se excluyeron, por contradecir la bóveda: preguntar el stack, MediatR y repositorios obligatorios, el catálogo `RN-xxx` y las plantillas de Spring Boot y Next.js.
