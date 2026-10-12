---
tipo: ejecucion
estado: propuesta
actualizado: 2026-10-10
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
