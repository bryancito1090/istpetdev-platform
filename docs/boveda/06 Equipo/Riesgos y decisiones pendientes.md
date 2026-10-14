---
tipo: ejecucion
estado: propuesta
actualizado: 2026-10-14
tags: [hackathon, istpetdev]
---

# Riesgos y decisiones pendientes

## Riesgos principales

| ID | Riesgo | Efecto | Respuesta |
|---|---|---|---|
| R-01 | Regla de trabajo previo desconocida | Preparación no evaluable o mal declarada | Resolver O-01/O-02 y registrar proveniencia |
| R-02 | Alcance demasiado amplio para el tiempo | Funciones aisladas sin recorrido | Integrar P0 primero, luego P1 |
| R-03 | Datos de consumo insuficientes | Cobertura con falsa precisión | Mostrar fuente/frescura y supuestos |
| R-04 | Simulación sesgada | Ahorro indefendible | Calendario común, baseline razonable y exportaciones |
| R-05 | Offline duplicado/conflictivo | Stock incorrecto o recepción perdida | **Mitigado:** Persistencia outbox atómica en IndexedDB, idempotencia UUID v7 y conciliación transaccional de cantidades en backend C# `FieldService.cs`. |
| R-06 | SignalR falla con varias réplicas | Mensajes/eventos invisibles | Backplane, afinidad y recuperación REST |
| R-07 | Dependencia de APIs/red | Demo interrumpida | Matriz permitida cacheada, fallback y video |
| R-08 | Costos cloud/APIs sin límite | Viabilidad débil y gasto innecesario | Definir cuenta/region/límite y medir |
| R-09 | .NET 8 próximo a fin de soporte | Continuidad requiere actualización | ADR-01 antes del scaffold |
| R-10 | Historial llamado inmutable sin garantía | Pérdida de credibilidad técnica | Describir append-only y protección probada |
| R-11 | QR/evidencias exponen información | Acceso indebido | **Mitigado:** Vista pública limitada solo con hitos de custodia, token sanitizado en fragmento `#t=`, y rate limiter por IP (`PublicTraceRateLimiter`). |
| R-12 | Evento exige restricciones nuevas | Cambios tardíos | Parámetros versionados y fixture adaptable |
| R-13 | Asistentes de IA reciben datos contradictorios o incompletos | Código y documentos con datos inventados | [[Hechos canonicos]], [[Catalogo y plan de skills]] y `check_vault.py` en CI |
| R-14 | Sin acceso al sector de limpieza antes del evento | Comprensión del problema e Impacto sin evidencia | Entrevistas en la feria el 16 y validación con el prototipo el 17 (D2); kit previo; decirlo con honestidad si no se logra |
| R-15 | Cifras, puntos, rutas y relato fijos en el widget de rutas y dashboard web («418 km», «Ahorro: 24.8%») | Pérdida de credibilidad si el jurado pregunta su origen | **Resuelto:** `RoutePlansController.cs` calcula dinámicamente revisiones y paradas (`stopsCount`), y `rutas.page.ts` consume la API real con estado reactivo y feedback dinámico. |
| R-16 | 2,5 días para construir el P0 completo | Demo incompleta o sin ensayar | Congelamiento el 15-oct 14:00, hitos H1–H5 y plan B por componente (D8) |
| R-17 | El incidente predefinido no produce «uno solo, uno escalado» con las rutas reales | Se pierde el momento central de la demo | Calibración tras implementar el planificador y prueba E2E que fija el resultado (spec 002) |
| R-18 | LLM lento, caído o manipulado por texto de los datos importados | Agente que falla o actúa fuera de la política en escena | Política determinista en el backend, fallback por plantilla en ≤ 10 s y datos delimitados (ADR-22) |
| R-20 | DeepSeek no cumple los umbrales de aceptación (latencia desde Ecuador o uso de herramientas en el nodo de n8n) | Agente que no responde a tiempo en la demo | Prueba de aceptación en H1; fallback determinista a los 10 s; nodo compatible con OpenAI como alternativa de conexión |
| R-21 | Credenciales filtradas en workflows de n8n exportados al repositorio | Exposición de claves de DeepSeek o Cognito | Prueba de CI de secretos y credenciales solo en el almacén de n8n |
| R-22 | El cliente técnico de Cognito no existe y depende de un `terraform apply` | n8n no puede autenticarse contra la API | Recurso definido en el spec 003; coordinar el apply antes de H3 |
| R-24 | Login PKCE y publicación HTTPS de la PWA sin terminar antes de H3 (dependen de Terraform, nginx y compose del equipo de infraestructura) | Ningún teléfono puede operar contra la API en el ensayo | **Mitigado:** PWA móvil configurada con Service Worker, guards funcionales de roles y autenticación local por perfiles (`498edcf`). |
| R-25 | Límite de consultas del QR por IP con jurados detrás del mismo NAT | Respuesta 429 en pleno pitch | **Mitigado:** `PublicTraceRateLimiter` implementado con umbrales configurables y pruebas unitarias/HTTP aprobadas en `PublicTraceUnitTests.cs`. |
| R-26 | Las 1080 jornadas de política requieren planificación por región y pueden exceder el plazo antes de H4 | No hay cifras de impacto defendibles para el pitch | Benchmark de corrida completa ≤60 min con matriz local, trabajo asíncrono y prioridad por encima de niveles adicionales del agente; no sustituir con cifras fijas |
| R-27 | Corrida de simulación escribe en `demo-region` o una política ve demanda futura | Demo inconsistente o comparación sesgada | Base de simulación distinta, importador de 001, calendario exógeno común y pruebas de aislamiento y revelación horaria |
| R-28 | La corrida oficial del simulador se ejecuta en la instancia de la demo (plantilla `t3.micro`, CPU por créditos) | Plazo de H4 incierto y API del pitch lenta | Máquina dedicada e importación verificada del ZIP (FR-21 del spec 005); decidir la máquina (PA-03 del 005) |
| R-29 | Guion presenta un CE documental como capacidad funcional o una cifra sin run | Credibilidad y nota de demo/impacto | Tarjetas de afirmación, doble revisión del PDF, retirada de frase si falta ejecución; V-42 del 006 |
| R-30 | Entrevista sin consentimiento, relación con jurado o cita fuera de contexto | Pérdida de confianza y conflicto de evaluación | Evitar organizaciones cauteladas, consentimiento separado y nota anonimizada; V-40 del 006 |
| R-31 | Tiempo/formato oficial se confirma tarde o materiales difieren de lo ensayado | Entrega o pitch fuera de plazo | O-04/O-06, cortes 3/5/7, PDF local, diez ensayos, manifiesto y congelamiento 15:45; V-41/V-43 del 006 |
| R-23 | IndexedDB, instalación PWA o S3 fallan en el teléfono de demo | Se pierde el momento físico o queda evidencia pendiente | **Mitigado:** Outbox durable desacoplado de evidencias, recepción hospitalaria con `saveReceiptAtomic` probada, y conexión local a API backend en localhost:5000. |
| R-19 | SSH abierto a `0.0.0.0/0` por defecto en el grupo de seguridad de Terraform | Exposición del host de demo | Reportado el 12-oct al equipo de infraestructura: lista vacía por defecto o despliegue por SSM; [issue #14](https://github.com/bryancito1090/istpetdev-platform/issues/14). El SG lo comparte el nodo del túnel SSM ([[Runbook de DevOps y base de datos para la demo]]) |
| R-32 | `reset-to-demo-core.sql` se ejecuta sin respaldo verificado o sin congelar despliegues | Pérdida de datos de integración o API caída por migraciones fallidas | Respaldo y restauración de prueba obligatorios ([issue #15](https://github.com/bryancito1090/istpetdev-platform/issues/15)) y secuencia de la sección 4 del [[Runbook de DevOps y base de datos para la demo]] |
| R-33 | Host de la app fuera de Terraform y SG compartido con el nodo del túnel; un `terraform apply` sin revisar el plan quita la salida total del SG | Despliegue roto o nodo del túnel expuesto | No aplicar sin `terraform plan` revisado; importar el host a Terraform con SG propio ([issue #14](https://github.com/bryancito1090/istpetdev-platform/issues/14)) |
| R-34 | Ruteo limitado a un único almacén sin considerar stock por bodega ni pedidos reales | Rutas inviables en operación multialmacén o despachos sin stock | **Resuelto (ADR-28, PR #59):** Sourcing multialmacén FEFO, matching de vehículos con validación de peso/volumen, evaluación de paradas pickup/delivery y rebalanceo proactivo. |

## Decisiones pendientes

| Tema | Momento de resolución | Estado / Resolución |
|---|---|---|
| Versiones .NET (ADR-01) y PostgreSQL (ADR-03) | Antes del primer scaffold | **Resuelto:** .NET 8 y PostgreSQL 16 PostGIS en producción/dev |
| Stack Web/Móvil (ADR-04) y Mapas (ADR-05) | Diseño frontend | **Resuelto:** Angular 22, Ionic 8, Leaflet con OpenStreetMap |
| Proveedor de identidad (ADR-10, ADR-18, ADR-26) | Antes de recepción autenticada | **Resuelto:** Cognito OIDC PKCE, roles fijos y rol Auditor |
| Segregación de plataformas y ergonomía (ADR-24, ADR-25) | Implementación de campo | **Resuelto:** Web Torre de Control vs PWA móvil de campo |
| Integración de campo y revisiones dinámicas (ADR-27) | Integración frontend-backend | **Resuelto:** Endpoints C#, adaptadores HTTP y outbox atómico |
| Ruteo multiparada por pedidos y bodegas (ADR-28) | Operativa y visualización de rutas | **Resuelto:** Sourcing multialmacén, VehicleMatcher y rebalanceo |
| Modelo/prompt/cuotas IA (ADR-08, ADR-22) | Configuración n8n | En curso (DeepSeek con fallback determinista en backend) |
| Costos reales y tamaño del piloto | Con información empresarial | P2 (Spec 007 a 012) |

No faltan nombres ni especialidades del equipo: IstpetDev ya confirmó cinco integrantes con experiencia transversal.

## Regla para declaraciones públicas

| Afirmación | Evidencia requerida |
|---|---|
| Ahorra X% | Run exportado, baseline y denominadores |
| Evita quiebres | Demanda común, claves de quiebre y casos nuevos |
| Funciona offline | Reinicio y sincronización en dispositivo real |
| Es altamente disponible | Configuración y prueba de fallo/recuperación |
| Detecta mermas | Conciliación confirmada, no solo anomalía |
| Trazabilidad completa | Cobertura de líneas y evidencia requerida |
| Está validado por empresa | Entrevista/piloto real documentado |

Revisar [[Plan de validacion]] y [[Plus para ganar]].

## Estado actualizado — 7 de octubre de 2026

Cuenta/presupuesto disponibles, repositorio software creado vacío, .NET 8 y Angular 22 confirmados y n8n existente. Red/IAM/Cognito/migraciones/backups/S3/observabilidad/rollback quedaron definidos para implementación en [[Entornos y operacion acordados]], [[Identidad OIDC y sesiones]] y [[Repositorio de software y versiones]].

Siguen pendientes: dominio institucional y su DNS/TLS/CDN, ejecución de Actions/Compose por el compañero, carga privada de credenciales/identificadores, ADR-05 mapas, reglas del evento y evidencia real de compatibilidad, seguridad, offline y recuperación. R-08 pasa de cuenta/presupuesto desconocidos a controlar horas/recursos persistentes; R-06 conserva su prueba de dos réplicas B-30; R-09 mantiene .NET 8 elegido y actualización posterior al evento. Los acuerdos no equivalen a recursos o pruebas realizados.
