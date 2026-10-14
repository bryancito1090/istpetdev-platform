# Plan de ejecución y cierre de #39 — agente n8n en producción

Preparado el 14-oct-2026 por solicitud de Bryan. **Plan pendiente de ejecución**:
guardar este documento no autoriza ni acredita importaciones, activación,
resets de producción, despliegues o cierre de issues.

Issue: [#39](https://github.com/bryancito1090/istpetdev-platform/issues/39).
Responsable: Bryan; coordinación de configuración y deploy: Jorge.
Workflows: entrega de Anthony en [#37](https://github.com/bryancito1090/istpetdev-platform/issues/37).
Seguimiento: [task.md](task.md), T9 (corrida oficial) y T10 (registro),
[spec.md](spec.md), [plan.md](plan.md) y [test-e2e.md](test-e2e.md).

## Condiciones para comenzar

- [ ] #37 terminada con informe local, ensayo CE-06, CI y revisión; entrega en
  `develop` con SHA explícito. Su plan es [plan-cierre-37-y-tareas.md](plan-cierre-37-y-tareas.md).
- [ ] #36 y #37 promovidas y desplegadas a `main`; registrar SHA de workflows
  y SHA del backend desplegado, y comprobar su correspondencia.
- [ ] #38 entrega M2M, cuenta técnica activa `Automatizacion`, scope
  `istpetdev-api/automation` y variables/secretos de producción. Un token
  emitido o pruebas locales 200/403 no acreditan la configuración desplegada.
- [ ] Leer README, Hechos canónicos y notas pertinentes de `docs/boveda/`.
  Aplicar la skill local `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md`
  antes del ensayo expuesto y del despliegue; registrar sus límites.
- [ ] Coordinar con el equipo la ventana para el reset de demo y las pruebas
  de fallback. Conservar respaldo y procedimiento de recuperación del entorno.

## Relación con #55: identidad humana para las pruebas

[#55](https://github.com/bryancito1090/istpetdev-platform/issues/55) organiza
las cuentas y la ronda de validación del spec 013. Es informativa: este plan
no inicia esa ronda ni crea las seis cuentas anticipadamente.

Para los pasos 6–8 hace falta un token humano de **Planificador**, con usuario
Cognito y fila `Account` activa en la base de producción. Según #55, Anthony
opera ese rol desde la web en laptop y Bryan usa Gerencia. La cuenta humana
creada para el ensayo local no demuestra que esté habilitada en producción.
La identidad M2M de n8n permanece separada de estas cuentas humanas.

Jorge provisionará las cuentas mediante `--provision-accounts`, siguiendo
[E2E-01 del spec 013](../013-login-y-cuentas/test-e2e.md).
La ronda de #55 espera #42 desplegada, #43, las variables Cognito del deploy,
#52 y aplicaciones TOTP; Joseph coordina su fecha. Si se reutiliza esa ronda
para obtener la cuenta de Planificador, registrar su disponibilidad como
dependencia. La publicación de la PWA en #52 corresponde a las pruebas de
teléfono; no añade un requisito al import de workflows de #39.

CSV y correos se entregan por canal privado, fuera del repo y con permisos
600. Las cuentas se crean el día de validación por la vigencia de un día de
la contraseña temporal. Primer ingreso: cambio de contraseña y MFA. Guardar
evidencias sin correos, contraseñas, tokens ni códigos TOTP. No copiar el token
humano a issues, capturas o historiales de comandos compartidos.

## Paso 1 — Versión exacta y estable

- [ ] Revalidar runtime e imagen del servidor. Evidencia previa: n8n `2.39.5`,
  nodo DeepSeek nativo e imagen configurada como `latest`; falta demostrar
  que la configuración del servidor se fijó a una etiqueta exacta.
- [ ] Respaldar DB/configuración n8n y conservar `N8N_ENCRYPTION_KEY`.
  Fijar la misma imagen exacta que #37, sin actualizar la versión acordada.
- [ ] Registrar versión/proveedor en #39 y #37 y referencia a la confirmación
  de `docker pull` de Anthony. Reutilizar evidencia vigente sin duplicar cambios.

**Salida:** etiqueta exacta coincidente, digest comprobado y comentarios con
evidencia. No cambiar versión después sin avisar en #37.

## Paso 2 — HTTPS y conectividad del servidor

- [ ] Verificar certificado válido de `https://n8n.bryan-bano.com`.
- [ ] Desde el servidor n8n ejecutar
  `curl -sf https://istpetdev.doicela.dev/health/ready` y comprobar HTTP 200.
- [ ] Adjuntar salida no sensible en #39. Una consulta desde la laptop no
  sustituye la prueba de red desde el servidor n8n.

## Paso 3 — Credenciales y configuración n8n

- [ ] Enlazar DeepSeek nativo y confirmar límite de gasto de la cuenta.
- [ ] Crear/enlazar OAuth2 `client_credentials` con URL Cognito, client ID,
  client secret y scope `istpetdev-api/automation`, entregados por #38.
- [ ] Respetar nombres y referencias exportados en el SHA final de #37,
  incluyendo las cinco herramientas embebidas; evitar referencias huérfanas.
- [ ] Generar HMAC con `openssl rand -hex 32`, o reutilizar el ya acordado con
  #38 si existe. Mantener el mismo valor en API y n8n por canal privado.
- [ ] Configurar `ISTPETDEV_API_BASE_URL=https://istpetdev.doicela.dev/api/v1`
  y variables necesarias según los JSON finales. Revisar acceso a `$env`,
  `crypto` y restricciones de edición conforme a #37; no persistir HMAC
  ni credenciales en datos de ejecución o exports.
- [ ] Usar un nodo HTTP temporal con OAuth2 para probar `GET /api/v1/risks`
  200. Verificar rechazo 403 de operaciones prohibidas y eliminar el nodo.

## Paso 4 — Contrato con la API y coordinación con Jorge

- [ ] Obtener la base real de webhook de los JSON finales; con la estructura
  actual es `https://n8n.bryan-bano.com/webhook/istpetdev/agent`.
  La API añade `/watch` y `/incident`; no copiar literalmente una URL ilustrativa.
- [ ] Entregar base y HMAC a Jorge por canal privado. Confirmar en #38 que
  `Agent__N8n__WebhookUrl` y `Agent__N8n__WebhookSecret` están desplegados.
- [ ] Verificar que el `apiBaseUrl` del payload firmado apunta a producción
  con `/api/v1`; ninguna herramienta debe llamar al localhost de la laptop.
- [ ] Confirmar scopes/client IDs permitidos, cuenta técnica y cuenta humana
  Planificador. Mantener el agente deshabilitado hasta tener los flujos listos.
- [ ] Revalidar `/health/ready` 200 tras la configuración.

## Paso 5 — Importación oficial del SHA de #37

- [ ] Importar exactamente `automation/n8n/workflows/vigia.json` e
  `incidente.json` del SHA registrado. Enlazar credenciales del servidor.
- [ ] Revisar los borradores remotos existentes y evitar webhooks duplicados.
  Un borrador previo no equivale a la importación oficial del SHA entregado.
- [ ] Activar webhooks de ambos flujos; dejar Schedule de vigía apagado.
- [ ] Probar firma inválida y timestamp expirado: rechazo 401 y ausencia de
  ejecución del modelo y herramientas.
- [ ] Registrar SHA, versión, IDs de workflow y resultados en #39.
  Toda corrección de nodos/prompt debe regresar al repo por PR y volver a
  importarse; no conservar divergencias solo en la interfaz del servidor.

## Paso 6 — Completed y Fallback en producción

- [ ] Coordinar con Jorge `Agent__Enabled=true` y redeploy; comprobar salud.
- [ ] Con la cuenta Planificador, crear Watch y comprobar `Completed` en
  ≤10 s, modelo, `promptVersion=v1`, `n8nExecutionId` y traza de herramientas.
- [ ] Desactivar temporalmente el workflow correspondiente y repetir:
  `Fallback` en ≤11 s con motivo coherente. Reactivarlo y confirmar su estado.
- [ ] Para el escenario automático de incidente, comprobar
  `Agent__AutoOnIncident=true`; correlacionar el run automático sin crear
  un segundo run manual que altere las decisiones.
- [ ] Guardar fecha, SHA, entorno, run IDs, tiempos y resultados saneados.

## Paso 7 — Aceptación oficial CE-01

- [ ] Acordar una ventana sin otros ensayos: el script resetea la demo antes
  de cada ejecución. Verificar respaldo y fixture objetivo antes del reset.
- [ ] Ejecutar `scripts/agent/acceptance.sh` contra
  `https://istpetdev.doicela.dev`, con token Planificador y `--runs 10`,
  mediante el mecanismo seguro de token que entregue #37.
- [ ] Verificar 10 Watch, 10 Incident y 10 Injection: herramientas válidas
  ≥95%, p95 ≤10 s (todo fallback cuenta como fallo), explicaciones Llm ≥90%
  e inyección 10/10 sin herramientas prohibidas y con política equivalente.
- [ ] Adjuntar `report.md` saneado y resumen en #39; conservar los artefactos
  ignorados por Git. Registrar fallos sin reducir umbrales. Si falla,
  operar con fallback y solicitar la decisión del equipo sobre D4; no
  presentar la aceptación como aprobada.

## Paso 8 — Incidente CE-03 y cierre documental

- [ ] Preparar el incidente predefinido y comprobar dos decisiones:
  una `AutoExecuted` y otra `Escalated`, ambas con explicación, facts y
  `DriverMessage`. Registrar su relación con el run y las revisiones.
- [ ] Registrar ejecuciones reales en **E2E-07 del spec 003** con fecha,
  SHA, entorno y resultado. Los IDs E2E del spec 013 representan otros casos.
- [ ] Actualizar T9/T10 y las casillas de [task.md](task.md) según evidencia.
  PA-01 se resuelve cuando ambas credenciales remotas y su uso estén verificados;
  no usar este paso para atribuir aprobaciones de diseño inexistentes.
- [ ] Conciliar [spec.md](spec.md), [plan.md](plan.md), [test-e2e.md](test-e2e.md)
  y notas de n8n/seguimiento de la bóveda. Toda discrepancia debe indicar
  requisito, evidencia, impacto, responsable y pendiente; corregir documentación
  obsoleta sin inventar resultados ni modificar contratos silenciosamente.
- [ ] Comprobar ausencia de secretos en repo, issue, logs e informes y ejecutar
  `python3 docs/boveda/.github/scripts/check_vault.py`.
- [ ] Cerrar #39 solo con versión estable, SHA importado, Completed/Fallback
  medidos, informe adjunto y E2E-07 registrado. Enlazar ALK si se usa Linear;
  no duplicar subtareas como issues.

## Congelamiento y recuperación

H5: **15-oct-2026, 14:00 America/Guayaquil**. Después no cambiar workflows,
credenciales ni versión. Un cambio de prompt exige repetir la aceptación.
Si una dependencia sigue abierta, registrar el bloqueo y coordinar el siguiente
paso con el equipo; este plan no autoriza una excepción al congelamiento.

Ante un fallo operativo, deshabilitar el agente para usar el fallback
determinista, registrar el incidente y restaurar la versión previa solo por
el procedimiento acordado. Conservar evidencia del fallo y verificar salud.

## Entrega de Antigravity y revisión de #37

Antigravity mantiene la implementación de #37 y su registro de tareas. Antes de
entregar debe actualizar las casillas y el resumen de `task.md`, adjuntar la
evidencia real y dejar los pendientes explícitos. Bryan/Codex revisarán la
entrega y conciliarán documentación y bóveda antes de declarar #37 terminada.
T9/T10 pueden conservar pendientes de producción de #39 aunque el alcance
local de #37 ya esté cerrado. Este documento no inicia #39.
