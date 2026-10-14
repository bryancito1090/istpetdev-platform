---
tipo: ejecucion
estado: propuesta
actualizado: 2026-10-14
tags: [hackathon, istpetdev]
---

# Registro de trabajo previo y del evento

## Propósito

Hacer verificables autoría y evolución del proyecto. El equipo quiere llegar con el sistema avanzado; confirmar las reglas en [[Consultas para la organizacion]] y conservar preparación/evento por separado.

## Registro inicial

| Fecha | Fase | Resultado | Evidencia | Estado |
|---|---|---|---|---|
| 2026-10-06 | Preparación | Bóveda base de IstpetDev; resumen del manual y diseño del Reto 1 | Notas de esta bóveda y [[Fuente y control documental]] | Creado; revisar con equipo |
| Pendiente | Preparación | Código, datasets e infraestructura que se construyan | Commit/tag, manifiesto y evidencia | Sin registros aún |
| 2026-10-08 | Capacitación | Reglas, datos y feedback | [[Registro de mentorias]] | Pendiente |
| 2026-10-16 | Hackathon día 1 | Adaptación/implementación/validación | Cambios y pruebas reales | Pendiente |
| 2026-10-17 | Hackathon día 2 | Integración final, resultados y pitch | Versión/entregables | Pendiente |

## Qué conservar

- Fecha, contribución concreta, referencias de versión y evidencia.
- Dependencias y recursos de terceros con licencia/proveniencia.
- Asistencia de herramientas IA: herramienta, para qué se usó y qué partes generó. El manual pide usar «recursos de trabajo que puedan utilizar legítimamente» (§16.1, p. 14) y la ficha de inscripción incluye una declaración de originalidad y uso legítimo de recursos (Anexo 4, p. 23).
- Skills de IA de terceros o adaptadas: origen y licencia. El origen de las skills del equipo se documenta en [[Catalogo y plan de skills]].
- Datos sintéticos separados de datos aportados por empresas.
- Cambios provocados por mentores o ficha final del reto.

No inventar commits, pruebas o entrevistas. La documentación generada no implica que ya exista el software descrito.

## Inventario antes del evento

Capturar un tag/hash de la versión previa y lista de partes funcionales, simuladas y pendientes. Durante las jornadas registrar qué se adaptó y por qué. Presentar esta distinción según el formato que confirme la organización.

La propiedad intelectual se rige por el manual y los acuerdos específicos que efectivamente existan; ver [[Resumen del manual]].

## Registro de preparación — 7 de octubre de 2026

| Fecha | Fase | Resultado | Evidencia | Estado |
|---|---|---|---|---|
| 2026-10-07 | Preparación | Repositorio software GitHub privado creado vacío por instrucción de Bryan | [istpetdev-platform](https://github.com/bryancito1090/istpetdev-platform) | Creado; sin commits/ramas materializadas ni colaboradores agregados |
| 2026-10-07 | Preparación | Esqueleto exclusivamente local de proyectos/infra/automatización | `~/Projects/ISTPETDEV`, README/.env.example/directorios | Creado; sin scaffold, instalación, commit o push |
| 2026-10-07 | Preparación | Completar acuerdos de infraestructura y ADR-10 conservando el aporte `4c3a9ef` | [[Repositorio de software y versiones]], [[Entornos y operacion acordados]], [[Identidad OIDC y sesiones]] | Documentado; sin apply AWS ni modificación del n8n externo |

Bryan confirmó recursos/versiones y delegó recomendaciones. El compañero implementará Actions/Compose y otro compañero profundizará dominio institucional. Conservar autoría del diseño anterior; la preparación documentada no se presenta como código/pruebas de las jornadas.

## Registro de preparación — 9 de octubre de 2026

| Fecha | Fase | Resultado | Evidencia | Estado |
|---|---|---|---|---|
| 2026-10-09 | Preparación | Migración CompleteSystemSchema95 aplicada en la base personal de Bryan; InitialSchema conservada, 95 tablas con ENABLE/FORCE RLS, credenciales API restauradas | Informe del agente de software, evidencia privada leída y resumen en [[Coordinacion migracion schema95]] | Aplicación personal registrada; no acredita despliegue, aislamiento funcional ni aplicación en integración |
| 2026-10-09 | Preparación | Reparto de trabajo entre agente de software (pruebas, documentación técnica y PR) y agente de bóveda (estado y guías compartidas) | [[Coordinacion migracion schema95]] | Encargo transmitido por Bryan y respuesta recibida; documentación de la bóveda verificada; envío automático por Orca bloqueado por FUSE |
| 2026-10-09 | Preparación | Commit bc85792f5ae94eeda79f93482f61cb77ced3b5dd publicado; PR #1 hacia develop como borrador; integridad RDS y aislamiento/integridad/PostGIS SQL descartable aprobados | [PR #1](https://github.com/bryancito1090/istpetdev-platform/pull/1), respuesta del agente de software y evidencias resumidas en [[Coordinacion migracion schema95]] | Revisión y pruebas .NET/HTTP/aislamiento RDS pendientes; no merge ni actualización de integración |
| 2026-10-10 | Preparación | Commit 8c6791cc6bf5c730b0447d343897fef4fae0e4e7: pool Npgsql secuencial, EF/Npgsql/NTS y filtro/controlador aprobados en PostgreSQL 16/PostGIS descartable; matriz JWT/HTTP preparada | Respuesta y huellas de evidencia privada comprobadas, resumidas en [[Coordinacion migracion schema95]] | API detenida y PR como borrador según informe; HTTP/Cognito, concurrencia y recorrido/aislamiento RDS pendientes; migraciones e integración sin cambios |

## Registro de preparación — 12 de octubre de 2026

| Fecha | Fase | Resultado | Evidencia | Estado |
|---|---|---|---|---|
| 2026-10-12 | Preparación | Análisis del repositorio unificado y diagnóstico frente a la matriz de evaluación | [[Definiciones de producto y plan hasta el pitch]] | Registrado |
| 2026-10-12 | Preparación | Plantilla de specs y spec 001 (núcleo delgado e importador) | PR #8 fusionado en `develop` (`docs/specs/`) | Documentado; sin implementar |
| 2026-10-12 | Preparación | Definiciones D1–D8, spec 002 (motor logístico) y ADR-21 a ADR-23 como propuestas | PR de `docs/definiciones-producto` | Documentado; Deciders pendientes |
| 2026-10-12 | Preparación | Spec 003 (agente logístico con n8n y DeepSeek) y complemento de ADR-22 | PR de `docs/definiciones-producto` | Documentado; sin implementar |
| 2026-10-12 | Preparación | Revisión cruzada de los specs 001–004 y correcciones: login PKCE y PWA por HTTPS con dueño, endpoint de demo `advance`, prueba de tablas de 001, token del QR en el fragmento y código de pruebas clave del 004 | PR de `docs/correcciones-specs-001-004` | Documentado; sin implementar |
| 2026-10-12 | Preparación | Revisión del spec 005: ruteo común y calendario escalonado de los baselines antes de la primera corrida, corrida oficial en máquina dedicada con importación verificada, lecturas de 002/004 en sus tareas | PR de `docs/correcciones-spec-005` | Documentado; sin implementar |
| 2026-10-12 | Preparación | Brainstorming y propuesta documental del spec 005: panel por API, baselines congelados, simulación asíncrona aislada, matriz de dependencias 001–004 y presupuesto de corrida completa | [spec 005](../../specs/005-panel-simulador/spec.md), [plan 005](../../specs/005-panel-simulador/plan.md), PR de `docs/spec-005-panel-simulador` | Propuesta; sin implementación, corrida ni cifras medidas; Deciders pendientes; tareas y E2E se añadieron después |
| 2026-10-12 | Preparación | Tareas T0–T11 y diez recorridos E2E del spec 005 redactados por petición posterior del usuario; la revisión detectó que T7 del 001 borraba runs/métricas al reset y corrigió ese contrato documental | [task 005](../../specs/005-panel-simulador/task.md), [test E2E 005](../../specs/005-panel-simulador/test-e2e.md), PR #11 | Documental; tareas y pruebas no ejecutadas, sin cifras medidas ni aprobación de Deciders |
| 2026-10-12 | Preparación | Propuesta documental 006: guiones 3/5/7, entrevista consentida, piloto, banco de preguntas, auditoría CE y búsqueda SERCOP | [spec 006](../../specs/006-validacion-pitch-entrega/spec.md), [[Guion del pitch]], [[SERCOP limpieza hospitalaria y sanciones]] | Borrador redactado con IA; revisión de 15 min del equipo, video, tag, entrevistas, ensayos y entrega pendientes; Deciders pendientes |

## Registro de preparación — 14 de octubre de 2026

| Fecha | Fase | Resultado | Evidencia | Estado |
|---|---|---|---|---|
| 2026-10-14 | Preparación | Implementación de T1–T9 del spec 013: login real OIDC con Amazon Cognito (PKCE), rol Auditor (ADR-26), endpoint `/api/v1/me`, aprovisionamiento idempotente (`--provision-accounts`), lógica de sesión en `shared-core`, retiro de login simulado en web/PWA y guardián de CI | [PR #56](https://github.com/bryancito1090/istpetdev-platform/pull/56) fusionado en `develop`; suites de prueba en verde | Completado |
| 2026-10-14 | Preparación | Implementación de T11–T13 del spec 013 (H4): API y pantalla web de gestión de cuentas por Gerencia (alta, edición, desactivación/reactivación y reenvío de invitación) | [PR #65](https://github.com/bryancito1090/istpetdev-platform/pull/65) fusionado en `develop`; 40 pruebas web y tests backend en verde | Completado |
| 2026-10-14 | Preparación | Kit previo T1 del spec 006 revisado y corregido por IA (Claude Code, Opus 5.5) a pedido de Joseph Baño: hoja de entrevista con permisos separados, esqueleto 3/5/7, resumen de negocio, banco de veinte preguntas y verificación SERCOP; estado de O-04/O-06 registrado como pendiente | [task 006, T1](../../specs/006-validacion-pitch-entrega/task.md), rama `feature/46-kit-previo-006` | Preparado; sin horas de construcción del equipo |
| 2026-10-14 | Preparación | T2 del spec 006 como borrador, preparado por IA (Claude Code, Opus 5.5) a pedido de Joseph Baño: matriz de 33 afirmaciones, guion 3/5/7 ajustado y estructura de siete diapositivas sin archivo; 105 pruebas automatizadas ejecutadas sobre `7aed1eb` para sostener la matriz | [matriz](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md), [[Guion del pitch]], rama `feature/46-fase2-guion-auditoria` | Borrador aprobado por Joseph; segundo revisor, ensayos y O-04/O-06 pendientes |
| 2026-10-14 (hora no registrada) | Preparación | Joseph Baño (Andrés) revisó durante 15 min y aprobó los cinco componentes del kit T1 del PR #71; no informó correcciones adicionales | [hoja de revisión T1](../../specs/006-validacion-pitch-entrega/task.md#hoja-de-revisión-humana-de-t1-unos-15-min), [E2E-01](../../specs/006-validacion-pitch-entrega/test-e2e.md#e2e-01--kit-utilizable), commit `228ab72` | CE-01 registrado; tiempo de construcción por IA no atribuido al equipo |
| 2026-10-14 | Preparación | Endurecimiento de CI/CD, flujo de base de datos RDS vs dev, política Fail-Fast en arranque, formalización de Plan B del evento y paridad de migraciones (Issue #35) | [PR #70](https://github.com/bryancito1090/istpetdev-platform/pull/70) fusionado en `develop`; [[Runbook de DevOps y base de datos para la demo]] y [[Evidencias y ensayos de base de datos y despliegue]] | Completado |
| 2026-10-14 | Preparación | T3–T5 del spec 006 preparados por IA (Claude Code, Opus 5.5) a pedido de Joseph Baño: escena con cinco papeles sin nombres, relevo y sustitución por parte, contraste con la matriz y cinco contingencias; hojas anonimizadas para el 16 y el retorno del 17, con lista de la corrida oficial de 005; registro de ensayos con racha y ronda de veinte respuestas; segunda revisión de la matriz pedida en la issue #46 | [escena](../../specs/006-validacion-pitch-entrega/escena-y-contingencias.md), [hojas de feria](../../specs/006-validacion-pitch-entrega/hojas-feria.md), [registro de ensayos](../../specs/006-validacion-pitch-entrega/registro-ensayos.md), rama `feature/46-fase2-guion-auditoria` | Solo preparación: sin ensayos, video, entrevistas, corrida de 005 ni segunda revisión; papeles sin confirmar; O-04/O-06 pendientes |
| Pendiente | Preparación | Cuentas creadas por rol y fecha (demo): registro de cuentas creadas en el pool antes del 16-oct | Registro sin correos personales ni identificadores sensibles (G8) | Pendiente (se completa en Fase B tras E2E-01 / aprovisionamiento) |

**Asistencia de IA:** los specs, esta reestructuración y las notas asociadas se redactaron con Claude Code (modelo Opus) a partir de preguntas y decisiones de un integrante del equipo. El código incluido en los specs es un ejemplo no ejecutado hasta que una tarea lo compile y pruebe (ver [[Convenciones y estados]]).
