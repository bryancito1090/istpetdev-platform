---
tipo: demo
estado: propuesta
actualizado: 2026-10-07
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

Runbooks de referencia: [[Entornos y operacion acordados]] e [[Identidad OIDC y sesiones]].
