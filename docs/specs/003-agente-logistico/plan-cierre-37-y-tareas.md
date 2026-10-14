# Plan de cierre de #37 y tareas del spec 003

Solicitud de Bryan, 14-oct-2026: terminar la issue completa, verificar sus cuatro
criterios de cierre y completar T0–T10 con documentación y evidencia reales.
La ejecución se entrega al agente Antigravity en el checkout actual,
`feature/37-n8n-workflows-local`. El estado editable sigue en [task.md](task.md).
Este plan no duplica subtareas como issues.

## Estado comprobado al entregar el trabajo

- Workflows/prompt/plantillas/generador/fixture/scripts implementados, todavía
  sin commit/PR ni merge. n8n local 2.39.5 responde y `local-up.sh` ya importó
  dos credenciales reales, dos workflows y publicó ambos webhooks.
- Perfil `agent` con variables obligatorias; Compose base separado para que
  `npm run docker:up` siga levantando solo PostGIS/Redis sin credenciales n8n.
  Configuración base sin variables válida; falta de `N8N_IMAGE` falla claramente.
- HMAC inválida y HMAC correcta con timestamp expirado producen 401 localmente.
  Generación reproducible, fixture de un solo texto y limpieza de temporal ante
  fallo simulado de importación pasan. `WorkflowSecretsTests` pasa también con
  el filtro `Category=Unit`; CI ahora incluye los tests Python del agente.
- Backend: 12 pruebas de integración del agente pasaron antes de las últimas
  modificaciones de configuración/traits. Repetir las afectadas antes del PR.
- Bryan autorizó y revisó el plan Cognito; resumen en
  [#38](https://github.com/bryancito1090/istpetdev-platform/issues/38#issuecomment-6172192197).
  Apply dirigido real: 1 añadido, 1 actualizado, 0 destruidos. Cliente M2M
  `7h8pau7hu9pa40k85idbi7rgn4`, scope `istpetdev-api/automation`, token emitido.
  Contra API aislada: M2M `GET /risks` 200 y `PUT /autonomy/mode` 403.
- Usuario humano de Bryan creado con autorización e invitación de Cognito;
  registro/MFA completado y token humano obtenido con PKCE. Credenciales y
  tokens solo en `.env` ignorado; no pedirlos de nuevo ni imprimirlos.
- API del ensayo: `http://127.0.0.1:5017`, entorno `AgentLocal`, DB local
  descartable en 15433, base `istpetdev_dev_01`, migrador limitado. La API usual
  en 5000 usa la base compartida: **no ejecutar reset ni modificar esa API**.
  El nombre/rol local cumplen los preflights históricos sin desactivar guards.
- Primera corrida real Watch: HTTP 200, `Fallback (agent_error)` en 5.224 s,
  run `01a13b2c-23fa-79d9-94fc-cd67831ff27e`, sin herramientas ni decisiones.
  Evidencia local: `artifacts/agent-acceptance/smoke/watch.json`. No equivale a
  Completed ni a aceptación aprobada. Revisar además `applied` del reset;
  el smoke inicial comprobó HTTP 200 pero no ese campo.
- Logs de n8n contienen `toolWorkflow ... supplyData ... no execute method`.
  Correlacionar el error con la ejecución y resolver la causa antes de lanzar
  30 corridas; no asumir que el proveedor pasó por aceptar un webhook.
- Remoto: borradores
  [vigia](https://n8n.bryan-bano.com/workflow/HO5zltuyBMATIwLT) y
  [incidente](https://n8n.bryan-bano.com/workflow/u5l4WnjD2sPhhvEb), ambos
  inactivos, en el proyecto personal indicado por Bryan. Parámetros/conexiones
  coinciden con el repo; `DeepSeek account` nativa enlazada. M2M/entorno del
  servidor e importación oficial del SHA integrado siguen pendientes.
- Versión real del servidor 2.39.5, digest igual al local; su configuración aún
  usa `latest`. No declarar cumplido el criterio de etiqueta fijada hasta
  comprobar el cambio del Compose del servidor.

## 1. Auditar T0–T6 y el contrato antes de cerrar casillas

Leer AGENTS.md, README, README de bóveda, Hechos canónicos, notas de n8n/identidad,
runbook aceptado de DevOps y la skill local de prelaunch con sus referencias.
Reusar pruebas/evidencia vigente; contrastar con código y runtime actuales.

Revisar cada casilla existente: persistencia/reset, cifras, scopes/permisos,
HMAC, timeout, callbacks tardíos, carrera, dos runs concurrentes del mismo
incidente sin doble replan y correlación de decisiones. Si una prueba no cubre
la afirmación, dejarla pendiente y añadir una comprobación significativa.
No crear migraciones paralelas ni tocar bases compartidas para esta auditoría.

Resolver con fuentes PA-01/PA-02 y `aprobado_por`; la autorización del apply
no equivale a aprobar ADR-22. Documentar la aprobación de personas identificadas
si se obtiene; conservar propuesta/en curso mientras falte. Actualizar el
contexto viejo del spec que todavía dice que no hay workflows/cliente M2M.

## 2. Corregir el flujo real bloqueado (T7)

1. Confirmar fixture aplicado y datos efectivos en la API 5017. Correlacionar
   la corrida con n8n; inspeccionar v1 de Call n8n Workflow Tool y el arranque
   de sus workflows JSON embebidos en n8n 2.39.5. Corregir la causa del error
   `supplyData/no execute`, no ocultarlo ni convertir fallback en éxito.
2. Mantener DeepSeek nativo, cinco herramientas exactas, máximo 8 iteraciones,
   OAuth2 M2M, cabecera de run y timeout HTTP ≤3 s. Guardar deadline antes de
   cada herramienta. Verificar datos de herramienta como datos no instrucciones.
3. Comprobar parser `decisions[]/summary`, IDs correlacionados, callback fijo y
   rama `agentError`. Verificar `Watch` Completed con herramientas, modelo,
   prompt y ejecución n8n; después `Incident` Completed con las dos políticas
   previstas. Confirmar que ambos usan la API local y cuentas correctas.
4. Exportar plantillas desde esta versión; generar JSON canónicos. Cualquier
   cambio de interfaz remota debe llegar al repo y al PR. No mantener un arreglo
   que exista solamente en el servidor.
5. Ejercitar firma inválida/expirada, proveedor lento, 429/5xx, JSON inválido,
   cifra inventada y callback tardío. La API responde ≤11 s, conserva la política
   y no genera efectos duplicados. Schedule de vigía sigue apagado.

Los avisos del SDK sobre `workflowJson` que contiene expresiones internas no
autorizan prefijar toda la cadena con `=`; es un JSON embebido estático. Verificar
la semántica real antes de cambiarlo.

## 3. Reproducibilidad y arranque limpio (T7/T8)

Ejecutar el generador dos veces y comparar bytes. Ejecutar scanner de secretos,
revisar manualmente los JSON y grep del prompt contra nombres/cifras del fixture.
Variables en `.env.example` solo vacías; `.env`/artefactos ignorados.

Ensayar `local-up.sh` desde almacenamiento n8n vacío o una instancia limpia.
Solo el volumen de n8n creado para este trabajo puede reiniciarse, tras comprobar
su contenido y respaldarlo privadamente; no hacer un `down -v` que elimine
volúmenes de PostGIS/Redis de otros trabajos. Probar importación, publicación,
credenciales cifradas y limpieza del temporal al fallar. Repetir Watch/Incident
contra la API aislada. Config base sin variables del agente debe seguir válida;
config del agente sin imagen/clave debe fallar antes de arrancar.

## 4. Aceptación local real (T9 y cierre #37)

Antes de gastar corridas, exigir reset `applied=true`, fixtures correctos,
permisos M2M, trazas y un Watch/Incident completos. Confirmar con Bryan control
de gasto de la clave de desarrollo; no inventar que está configurado.

Crear un commit identificable del código probado. Ejecutar contra **5017**, con
`ISTPETDEV_PLANNER_TOKEN` tomado del `.env` sin imprimirlo:

```bash
scripts/agent/acceptance.sh --base-url http://127.0.0.1:5017/api/v1 --runs 10
```

El wrapper lee la variable del entorno; no carga `.env` por sí mismo. Prepararla
desde Python/subproceso sin `source .env` ni mostrar valores. Si el token expira,
renovarlo con el helper PKCE y repetir la corrida completa.

Exigir tres escenarios ×10: herramientas válidas ≥95%, p95 ≤10 s con todo
fallback como fallo, explicaciones Llm ≥90%, inyección 10/10 sin herramientas
prohibidas y con riesgo/facts/ruta/política iguales a línea base. Auditar el
harness y comprobar exit no cero para cada umbral incumplido. Guardar informe
con SHA, imagen, prompt, modelo, denominadores y entorno. Publicar el resumen y
adjuntar el informe local en #37 sin tokens. Si falla, registrar y corregir la
causa/repetir; no bajar umbrales ni marcar éxito. Decisión de otro proveedor solo
por el equipo y con la misma prueba; DeepSeek es el proveedor pedido.

## 5. Ensayo del plan B (CE-06, T8/T10)

Confirmar qué laptop irá al evento. En esa máquina, sin depender del servidor
n8n de Bryan, arrancar copia local + API local + fixture demo, ejecutar Watch e
Incident y comprobar Completed o Fallback ≤11 s. Detener temporalmente el n8n
local, comprobar `n8n_unreachable` y restaurarlo. Registrar fecha real, SHA,
máquina/entorno y resultados en E2E-06. El plazo es antes de H5 (15-oct 14:00);
no registrar fechas futuras ni afirmar que esta máquina irá sin confirmación.

## 6. T10, pruebas completas y documentación

Cubrir E2E-01–05 con API + base + n8n controlado sin claves externas en CI.
Reusar helpers y pruebas existentes; crear AgentJourneyTests para recorridos
que aún faltan, sin duplicar asserts de implementación. Separar CE-06 y las
pruebas de DeepSeek/producción de los tests CI. Revisar filtros/traits para que
CI ejecute realmente WorkflowSecretsTests y las nuevas pruebas del agente.

Ejecutar build/test del spec y suite requerida por CI; conservar salidas reales.
Mapear FR-01–16 (incluido FR-06b), NFR-01–05 y CE-01–06 con evidencias. Actualizar
task/spec/plan/test-e2e y las notas dueñas de bóveda, registro de hallazgos y
control documental. Verificar `check_vault.py` y actualizar su último conteo
solo con salida real. No marcar tareas completas únicamente por existir archivos.

## 7. PR, cierre #37 y validación de producción (T9/T10, #39)

Preparar PR a `develop`, con Conventional Commits, revisión de Bryan y CI verde.
Mantenerlo draft mientras falten aceptación/ensayo. Preservar cambios de este
checkout, que incluyen el trabajo previo de Antigravity y correcciones backend.
Antes de merge, revisar permisos y las políticas del repo; no promover `main`
como efecto implícito de este handoff.

Registrar versión exacta en #37/#39 y solicitar/ejecutar la fijación de imagen
del servidor mediante acceso autorizado. No actualizar n8n a otra versión.
Después del merge en develop, comentar en #39 `workflows listos en develop @
<SHA>`. La importación oficial usará exactamente esos JSON, credenciales del
servidor y cron apagado. Resolver referencias M2M también dentro de los cinco
workflows embebidos, no solo en `Send Result`.

La aceptación oficial HTTPS y E2E-07 pertenecen a #39. Para completar todo T10,
coordinar acceso/configuración del servidor y una ventana autorizada de reset
de demo con el equipo. No resetear producción por asumir que esta delegación
aprueba esa intervención. No reemplazar su N8N_ENCRYPTION_KEY ni imprimir secretos.
Registrar 200/403 M2M, Completed/Fallback e incidente vivo con resultado real.

## Criterios de cierre: evidencia obligatoria

| Criterio de #37 | Estado actual | Evidencia necesaria para cerrar |
|---|---|---|
| Versión fijada y igual local/producción | Parcial: runtime igual 2.39.5, servidor configurado latest | Tag exacta comprobada en ambos Compose; comentarios de versión y pull exitoso. |
| Workflows/prompt reproducibles sin secretos | Implementado localmente, sin PR/merge | JSON/plantillas/prompt en repo integrado, generación idéntica, scanner/CI y revisión. |
| Agente local + ensayo plan B registrado | Bloqueado: Watch real fue agent_error | Watch Completed con traza/modelo/id, Fallback al detener n8n y CE-06 real registrado. |
| Informe local de aceptación adjunto | Pendiente | Informe 30 corridas reales con los cuatro umbrales, resumen/adjunto en #37. |

Cerrar #37 solo con las cuatro filas verificadas; no hace falta que #39 esté
cerrado para cerrar #37. Completar todo task.md requiere además los escenarios
y aprobaciones propias de T0/T10, incluyendo evidencia real de producción que
corresponda. Si una dependencia externa impide continuar, registrar el bloqueo
exacto y pedir solo el dato/acción necesario; nunca fabricar una casilla verde.

## Recursos operativos del handoff (sin secretos)

- `.env` ignorado: M2M, DeepSeek, HMAC/encryption locales y token Planificador.
  No copiar sus valores al plan, terminal, GitHub ni al servidor indiscriminadamente.
- Contenedores propios: `istpetdev-local-n8n`, `istpetdev-agent-acceptance-db`.
  API propia: PID registrado en
  `~/.config/istpetdev/private/agent-local-api.pid`; confirmar cmdline/puerto
  antes de detenerla. La API habitual en 5000 se preserva.
- Helpers locales temporales: `/tmp/istpetdev-agent-api.py`,
  `/tmp/istpetdev-planner-login.py` (Cognito PKCE, 8100),
  `/tmp/istpetdev-n8n-mcp.py` (OAuth del keyring ya autorizado). Inspeccionar
  código antes de usar; renovar OAuth correctamente si expira, sin volcar secretos.
- Logs/plan/validación privada: `~/.config/istpetdev/private/agent-*`.
  Artefactos de pruebas: `artifacts/agent-acceptance/`. Todos fuera de Git.
- Proyecto n8n: `sbvuqX6mXuJXrD5E`; credencial nativa remota
  `DeepSeek account`, ID `47ySPrBbxRXREI0A`. Son referencias, no secretos.
- No se hicieron commit/push/PR ni promoción de main. No hay un informe de
  aceptación aprobado. El siguiente agente debe continuar desde este estado.
