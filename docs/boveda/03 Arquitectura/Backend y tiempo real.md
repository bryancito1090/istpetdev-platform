---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-09
tags: [arquitectura, implementacion]
---

# Backend y tiempo real

## Plataforma y estructura

Base solicitada: **.NET 8**. Antes del primer proyecto, resolver ADR-01: .NET 8 termina soporte el **10 de noviembre de 2026** y .NET 10 LTS tiene soporte hasta el **14 de noviembre de 2028**. Mantener 8 para el evento si es la decisión del equipo, registrando actualización posterior. [Política oficial de .NET](https://dotnet.microsoft.com/en-us/platform/support/policy).

Estructura propuesta del repositorio de software:

```text
src/
  Api/              HTTP, autenticación, SignalR, composición
  Application/      casos de uso, comandos, consultas, DTOs
  Domain/           reglas de inventario, custodia y logística
  Infrastructure/   EF Core/Npgsql, S3, mapas y adaptadores
apps/
  web/
  mobile/
infra/
  terraform/
automation/
  n8n/
tests/
  integration/
```

Estos directorios son una convención a decidir, no archivos ya creados en la bóveda. **No coinciden** con las rutas que filtra el diseño de [[CI-CD y automatizacion de despliegue]] (`backend/**`, `apps/web/**`): la estructura definitiva es PENDIENTE de ADR-12 y se registra en [[Hechos canonicos]]. Usar EF Core con PostgreSQL y consultas proyectadas; no añadir repositorios genéricos que repitan sus operaciones.

## Autorización RBAC

ADR-14 adopta RBAC con el catálogo de [[Seguridad y evidencias]]. La API vincula la identidad validada a roles por organización y aplica políticas por acción junto con comprobaciones del recurso en consultas y comandos. Denegar por defecto; no confiar en roles o actores del payload.

ADR-16 amplía esta definición el 9 de octubre: roles, asociaciones, ámbitos y permisos de campo se gestionan desde DB con delegación auditada. Plantillas/configuración/workflows versionados en [[RBAC y configuracion del sistema]]; tablas y RLS en [[Esquema completo de base de datos]]. Una concesión no habilita un handler inexistente ni omite invariantes. La revisión anterior al arranque/deploy se coordina con [[istpetdev-prelaunch]].

Reutilizar las mismas políticas en SignalR, evidencias y reintentos offline. Comprobar permisos vigentes antes de devolver un resultado idempotente o efectuar cambios; ningún rol evita restricciones de stock, versión o asignación. Web/móvil reflejan permisos; n8n usa el rol técnico limitado. Cierre: B-29 y RNF-01.

## CQRS práctico

Comandos: registrar consumo, reservar stock, despachar, recibir, crear incidente y publicar ruta. Consultas: cobertura, dashboard, historial de QR y métricas.

- Los comandos protegen invariantes en transacciones.
- Las consultas devuelven DTOs y paginación; evitar cargar el dominio entero.
- Separación por casos de uso; un mediador es opcional, no requisito del patrón.
- Una base y un despliegue al inicio.
- La proyección de saldos cambia en el mismo commit; no introducir inconsistencia eventual en stock crítico.

## Concurrencia e idempotencia

Guardar clave de operación, huella del contenido y resultado en la misma transacción del negocio. La misma clave y contenido devuelve el resultado previo; una misma clave con contenido diferente produce conflicto.

Validar también duplicados de negocio: una nueva clave no habilita recibir dos veces las mismas cantidades. Para decrementos usar bloqueo/versionado o actualización condicional, junto a restricciones de saldo. No validar saldo con una lectura desprotegida seguida de escritura.

## Tiempo real

SignalR comunica cambios y chat; mensajes/eventos relevantes quedan persistidos antes de notificar. Al reconectar, recuperar historial y versión por REST.

**Varias réplicas:** configurar Redis backplane y afinidad del ALB cuando se usa negociación/transporte de fallback. La alternativa de solo WebSockets con negociación omitida debe decidirse y probarse expresamente. Redis no conserva mensajes perdidos durante una interrupción; el almacenamiento del historial y recuperación son de nuestra API. [Backplane oficial de SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane?view=aspnetcore-10.0), [escalado de SignalR](https://learn.microsoft.com/uk-ua/aspnet/core/signalr/scale?view=aspnetcore-10.0).

- Grupos por organización y asignación, creados desde autorización en servidor.
- No permitir entrar a un grupo solo enviando su ID.
- Chat: texto, actor derivado de sesión, asignación, hora servidor y ID.
- Notificaciones: ID de evento y versión; deduplicar y refrescar estado.
- Escalado considera conexiones y memoria además de CPU.
- Logout, token expirado y reasignación invalidan permisos del canal.

## Trabajos de fondo

El cálculo de rutas o simulaciones grandes devuelve una referencia de trabajo. Un procesador persistente recupera pendientes y resultados.

Para el outbox, un proceso alojado puede reclamar registros de PostgreSQL con bloqueo y marca temporal; si hay varias réplicas, usar reclamación exclusiva y reintentos. Los cron de n8n no se duplican por escalar la API.

## Operación

Endpoints de salud separados para proceso y disponibilidad de dependencias. Logs con correlationId/operationId; no registrar firmas, credenciales o payloads sensibles completos. Migraciones como tarea única del despliegue, no ejecutadas simultáneamente por cada contenedor.

Interfaces y errores en [[Contratos API y eventos]]; validación en [[Plan de validacion]].

## Decisión posterior de implementación — 7 de octubre de 2026

Bryan confirma **.NET 8**, manteniendo la recomendación de actualización para continuidad después del 10 de noviembre. EF Core/Npgsql 8 y PostgreSQL 16/PostGIS compatible; base por integrante y base de integración en RDS dev privado. Migración única por release, pool acotado, fixture idempotente y backups antes de cambios: [[Entornos y operacion acordados]]. Identidad y validación de tokens Cognito en [[Identidad OIDC y sesiones]]. Se conserva SignalR con historial REST; dos réplicas requieren la evidencia de B-30/V-15, no se considera satisfecho por el host único.
