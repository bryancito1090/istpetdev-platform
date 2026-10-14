---
tipo: demo
estado: propuesta
actualizado: 2026-10-12
tags: [demo, validacion, istpetdev]
---

# Checklist y contingencias

## Antes del ensayo

- [ ] Duración, formato, turno y material permitidos confirmados.
- [ ] Dataset, runId y seed identificados; etiquetas de simulación visibles.
- [ ] Resultados exportados corresponden a la pantalla.
- [ ] Caja vacía con QR legible y enlace público limitado probado.
- [ ] Bodega/conductor/planificador con cuentas de demostración.
- [ ] Móvil tiene asignación descargada, batería y almacenamiento.
- [ ] HTTPS funciona en teléfono real; permisos de cámara probados.
- [ ] Foto/firma de prueba autorizada y sin datos reales innecesarios.
- [ ] Operaciones pendientes del ensayo anterior conciliadas o reseteadas.
- [ ] Incidente preparado modifica realmente rutas/restricciones.
- [ ] Fallback de IA probado; nadie depende del cron para iniciar a tiempo.
- [ ] Presentación PDF, video y resultados disponibles localmente.
- [ ] Prueba completa cronometrada y grabada.

## Durante la demo

- [ ] Mostrar problema y dato, no solo mapa.
- [ ] Identificar ruta antes/después y pendientes.
- [ ] Declarar incidente sintético.
- [ ] Capturar sin señal y mostrar estado local.
- [ ] Recuperar red, confirmar una sola recepción y archivos.
- [ ] Mantener consulta pública sin revelar firmas/datos privados.
- [ ] Presentar KPIs con unidad, período y denominador.
- [ ] Cerrar con resultado verificable y próximo piloto.

## Contingencias

| Fallo | Acción preparada | Cómo comunicar |
|---|---|---|
| Internet de sede | Conectividad de respaldo o entorno de ensayo conocido | Informar cambio de conectividad |
| API de mapas | Matriz almacenada permitida con proveedor/fecha | Aclarar que no es tráfico en vivo |
| DeepSeek | Explicación determinista | Explicar que el cálculo sigue disponible |
| n8n cron tarde | Activación manual autenticada de mismo flujo demo | Mantener runId y escenario |
| QR no enfoca | Segundo QR o apertura del enlace en dispositivo del equipo | Conservar mismo ID |
| Cámara/permiso móvil | Dispositivo de respaldo previamente probado | Usar captura real alternativa |
| Sin acceso cloud | Entorno local preparado o video de respaldo | Declarar modalidad y límites |
| Caída/bloqueo de backend | Disparo de `restart_only` vía GitHub Actions o `docker compose restart` (< 10s) | Recuperación inmediata sin rebuild |
| Regresión o bug en demo | Rollback vía `rollback.yml` con `target_sha` previo (objetivo < 30 s; medir en el ensayo) | Retorno transparente a versión estable previa |
| Sin sincronización | Mostrar registro local y usar evidencia grabada para el resto | No marcarlo recibido en servidor |
| Presentación bloqueada | PDF/video local | Evitar depender del navegador |
| Tiempo reducido | Historia → alerta → recepción → KPIs | Recortar extras y respetar tiempo |

El entorno local debe probarse en teléfono con origen compatible/HTTPS si usa cámara/service worker. No improvisar acceso HTTP por IP y asumir que será igual a localhost.

Un video de respaldo no se presenta como ejecución en vivo. No modificar a mano estados para simular una aceptación.

## Paquete final

- Presentación final y guion.
- Ficha de problema, solución, validación, viabilidad y negocio.
- URL/versión de prototipo según reglas confirmadas.
- Dataset/manifiesto y métricas compartibles.
- Video de respaldo y capturas.
- Declaración de recursos/autoría según organización.

Documentos personales y bancarios del premio permanecen en el expediente privado del equipo.

Ver [[Evaluacion y entregables]] y [[Registro de trabajo previo y del evento]].

## Preparación operativa acordada — 7 de octubre de 2026

- [ ] Encender demo con antelación, esperar DB/health checks y pausar alarmas solo en apagados planificados.
- [ ] Confirmar manifest actual/anterior, tres imágenes precargadas y rollback compatible sin download.
- [ ] Confirmar backup S3 verificado y restauración ensayada, sin depender del disco EC2.
- [ ] Verificar login Cognito y asignaciones descargadas antes de modo avión.
- [ ] Usar n8n de Bryan con API HTTPS alcanzable y cron/credencial demo; pausar sus workflows al apagar AWS.
- [ ] Guardar tiempos reales de restart/rollback por separado de cola GitHub; los tiempos de la tabla anterior son metas pendientes de medida.
- [ ] Registrar endpoint institucional, certificado y renovación una vez resueltos por el compañero.

> [!info] 14-oct-2026 (fase 3 del issue #46): las tarjetas C-01–C-05 (agente, mapa, conexión, cámara/QR y ZIP del simulador ausente) y el protocolo del ensayo funcional previo al video están en [escena y contingencias](../../specs/006-validacion-pitch-entrega/escena-y-contingencias.md). Ninguna está ensayada.

## Complemento propuesto — spec 006, 12-oct-2026

**Deciders PENDIENTE.** El operador de cada escena lleva una tarjeta: disparador, línea de contingencia y segundo exacto de salida. Los cinco papeles están definidos en [[Guion del pitch]]; nombres y suplentes pendientes. Una alternativa solo se usa si fue ensayada en el mismo dispositivo y versión del material.

| A los | Momento | Si falla | Línea y transición |
|---|---|---|---|
| 1:05 | Riesgo/agente | 003 no pasa | «Este riesgo sale de una regla verificable». Mostrar solo 002 con evidencia; si falta, marcarlo propuesta y avanzar a caja. |
| 1:35 | Ruta | Mapa no carga | Lista ordenada de paradas y restricciones probadas; no decir «óptima». |
| 2:05 | Incidente | n8n/LLM no responde | Replanificación determinista del 002 si CE-04 pasó; de otro modo omitir automatismo. |
| 2:30 | Caja/móvil | Offline o cámara falla | Recepción en línea probada o video del 15-oct rotulado «grabado»; no afirmar sincronización en vivo. |
| 3:10 | QR público | teléfono externo o HTTPS falla | Teléfono del equipo en hotspot si CE-07 pasó; si no, captura rotulada y pasar a impacto a 3:30. |
| 3:30 | Impacto | ZIP/hash/FR-21 no pasa | Retirar rango y ahorro; explicar comparación propuesta sin números. |

La tabla corresponde al guion de 5 min; usar puntos equivalentes de los cortes 3/7. Un fallo interrumpe la racha de ensayos aunque la contingencia permita terminar el pitch. La racha exigida es diez ensayos completos consecutivos sin fallo, los dos últimos sin la wifi del recinto; el tiempo es oficial menos 20 s. Registrar ejecución en el [E2E del 006](../../specs/006-validacion-pitch-entrega/test-e2e.md).

**Puertas con hora:** 15-oct congelamiento funcional a las 14:00 y video de respaldo después de ensayo funcional; tag `pre-evento` del último commit antes del 16; 17-oct código congelado a las 10:30 salvo bloqueo, evidencia hasta 12:30, materiales a 15:45, entrega oficial 16:00–16:15. El video conserva fecha, commit y etiqueta de grabación. En el paquete, cada archivo tiene hash, responsable y estado `funcional`/`grabado`/`simulado`/`propuesto`; abrir el PDF y video sin red antes de entregar. O-04/O-06 determinan tiempo y formato oficiales.

Runbooks de referencia: [[Entornos y operacion acordados]] e [[Identidad OIDC y sesiones]].
