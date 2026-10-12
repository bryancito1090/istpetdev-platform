---
tipo: fuente
estado: vigente
actualizado: 2026-10-07
tags: [istpetdev, documentacion]
---

# Información inicial del equipo

**Fuente:** briefing del 6 de octubre de 2026, registrado por bryancito1090 (commit 765c532). Registro estructurado de su propuesta, separado de los requisitos oficiales. Las afirmaciones de rendimiento/impacto se tratan como objetivos a validar.

## Intención

IstpetDev está inscrito en el Reto 1 y quiere construir un sistema completo antes de las jornadas, para dedicar el evento principalmente a adaptación, validación y ajustes. La bóveda compartida será la base del equipo y de sus asistentes.

## Historia central recibida

Una empresa de limpieza atiende **80 puntos a nivel nacional**. Se entera de que un hospital agotó desinfectante cuando el hospital llama. El sistema debe anticiparlo **cinco días antes**, explicar por qué, preparar la ruta, resolver sobrecostos y seguir el insumo de bodega a receptor.

Se quiere contar el recorrido en **tres minutos** y demostrar **90 días de operación simulada**, comparando proceso tradicional y plataforma.

## Stack y capacidades propuestas

| Área | Propuesta original |
|---|---|
| Backend | .NET 8/C#, API RESTful, Clean Architecture, CQRS |
| Comunicación | SignalR para chat gerencia–conductor e incidentes en vivo |
| Cloud | Docker, ECS Fargate para API/frontend, ALB y autoscaling con CPU 70% |
| Base | RDS PostgreSQL/PostGIS Multi-AZ, UUID v7 para lotes/transacciones |
| Evidencias | S3 para fotos y firmas |
| Rutas | API de mapas, distancias viales, capacidad y horarios |
| Dashboard | Comparar ruta tradicional/optimizada en km, tiempo y costo |
| Automatización | Cron n8n, DeepSeek y alertas explicativas |
| Incidentes | n8n inyecta mock de vía bloqueada para mostrar reacción |
| Móvil | Angular/Ionic offline-first con SQLite/IndexedDB, QR, foto y firma |
| Web | Angular standalone y Tailwind |
| Infra como código | Terraform |
| Skills futuras | Componentes, backend, frontend, móvil, cloud, n8n y arquitectura |

## Demostración imaginada

Generar lote/QR, pegarlo a una caja, escanear en bodega, mostrar tránsito y ruta, permitir consulta desde teléfono del jurado y capturar recepción. Cerrar con km/costo por entrega, quiebres evitados, puntualidad, trazabilidad y mermas detectadas.

Estos indicadores se definen operativamente en [[Simulador y metricas]]. El manual no los enumera como cinco KPIs exactos obligatorios.

## Aclaraciones posteriores (registradas por bryancito1090 en los commits 765c532 y 5905073)

- **Equipo:** IstpetDev.
- **Integrantes:** cinco.
- **Experiencia:** amplia en full stack, backend, móvil, frontend, datos y logística.
- **Forma de trabajo:** todos realizan lo necesario; no enfatizar ni repartir quién hace qué.
- **Frontend:** adoptar **Feature-Sliced Design (FSD)** y comunicarlo en la bóveda.
- **Autorización:** usar **RBAC** y agregarlo explícitamente a la bóveda; decisión aceptada el 6 de octubre de 2026 en ADR-14, con permisos por rol y ámbito en [[Seguridad y evidencias]].
- **Plus:** mantener una nota específica con mejoras para competir; archivo [[Plus para ganar]].

## Precisión técnica incorporada

- CQRS implica separación de operaciones; no exige separación física de bases.
- UUID v7 no garantiza que la base sea la más rápida ni evita por sí solo saturación.
- Offline necesita cola/persistencia y reintentos; el service worker no resuelve todo.
- Varias réplicas SignalR necesitan distribución de eventos y configuración de conexión.
- Predicción y merma no se confirman solo porque un LLM produzca un texto.
- La trazabilidad «inmutable» necesita declarar y comprobar sus garantías.
- Los objetivos de anticipación/ahorro/continuidad dependen de datos y restricciones.

La propuesta tecnológica se conserva en [[Contexto maestro]], con ajustes y pendientes en [[Decisiones de arquitectura]]. Esta nota es un registro histórico: sus afirmaciones («inmutable», «cinco días antes», .NET 8) se conservan tal como se recibieron; los valores vigentes están en [[Hechos canonicos]].
