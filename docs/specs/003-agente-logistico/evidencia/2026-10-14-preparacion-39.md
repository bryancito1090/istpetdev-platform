# Preparación de #39 — 14 de octubre de 2026

Registro de Codex; entorno n8n de Bryan y API HTTPS productiva. Preparación
operativa y documental, **sin aceptación oficial ni resets productivos**.
Bryan confirmó que aún no hay ventana exclusiva, que Antigravity terminó,
que el límite de gasto DeepSeek está configurado y que Jorge opera EC2/API.

## Fuentes e integración

- Workflows/prompt: `8c99a71c5a0017e769af4d7e40666565d684117d`, PR #74/#37.
- Transporte de configuración: PR #81, develop `730d90c`.
- PR #85 fusionado por Bryan: develop
  `5582930f30c32ecfec958c3a8acc28a6cd775104`.
- chore/39-verificar-cierre incorporó `5582930` mediante merge, sin conflictos
  de contenido. La conciliación documental distingue local/integrado/productivo.
- main consultado: `9d1a3d9bec8d759bb338368388ce9ccbbe7b3016`; CD previo
  exitoso. Falta release del transporte de #81 y configuración de #85.

## n8n y conectividad comprobados

Imagen y runtime **2.39.5**, DeepSeek Chat Model nativo. Pull del tag oficial
exitoso y digest igual al contenedor previo:
`sha256:cfa04788a34ae6f5d59d9086c5761315be2457a9608969553bd0add9a94959d1`.
Compose quedó fijado; recreación exclusivamente del servicio n8n, con mounts
y clave de cifrado preservados. Se conservaron backups privados de Compose,
PostgreSQL de n8n y configuración de cifrado. No hubo down -v.

API readiness respondió **200** desde servidor y contenedor n8n, 22:45 UTC,
por HTTPS con validación TLS. Healthz de n8n respondió 200 con User-Agent
explícito. El cliente Python por defecto obtuvo 403 en otra prueba; ese rechazo
no se interpreta como validación HMAC ni como prueba del origen backend.

| Workflow | ID | Versión publicada comprobada | Estado |
|---|---|---|---|
| Vigía | HO5zltuyBMATIwLT | 0937a96e-df74-4a10-aec2-69227c735ff2 | Activo, igual al borrador; Schedule deshabilitado |
| Incidente | u5l4WnjD2sPhhvEb | 35133a2f-65bf-468c-ac8d-037375fd6202 | Activo, igual al borrador |

Comparación de parámetros/conexiones con Git: diferencias de IDs de
credenciales, resueltas en nodos visibles y en los cinco workflowJson
embebidos. El grafo/lógica coincide con la fuente oficial. Referencias:
IstpetDev M2M (`mZiC4AJxPhcKtANv`) y DeepSeek (`5aBicMrj0xaH7Nfl`).
No se cambiaron prompt, modelo, política o workflows versionados.

## HMAC y privacidad

Se generó la clave productiva con OpenSSL (32 bytes aleatorios), guardada en
archivo privado 0600 y entregada a GitHub Secrets mediante stdin. Durante
las ediciones concurrentes apareció otro valor literal en Compose, que
prevalecía sobre env_file: una firma calculada con el archivo privado recibió
401. Una prueba sin modelo confirmó que bytes/timestamp eran correctos y
las claves diferían; no se atribuyó el fallo al verificador ni se cambió código.

Tras confirmar Bryan el fin de Antigravity, se eliminó ese valor literal y
se consolidó la fuente privada. Configuración resuelta y runtime coincidieron
por comparación privada (23:18 UTC); mounts/cifrado preservados.

| Prueba | Origen / alcance | Resultado |
|---|---|---|
| Sin firma | Dos webhooks reales, loopback n8n | 401 |
| Firma incorrecta | Dos webhooks reales, HTTPS; cabeceras del contrato | 401 |
| Firma válida con timestamp expirado | Dos webhooks reales, HTTPS | 401 |
| Cuerpo alterado después de firmar | Dos webhooks reales, HTTPS | 401 |
| Firma positiva tras sincronización | Probe con los mismos seis nodos HMAC, sin modelo/herramientas; 23:25 UTC | 202, firma/bytes/clave coinciden |

El probe fue despublicado y eliminado. La prueba positiva no es un AgentRun
productivo ni acredita Completed. Antigravity reportó además suite 10/10 de
los dos webhooks; se conserva atribuida a su entrega. Se exige run real en G.

Persisten saveDataError/success=none, progreso/manual=false; no se compartieron
secretos, firmas completas, JWT ni datos de ejecución con hmacKey.

## Cognito/API: gate pendiente

Las propiedades públicas de la credencial n8n coinciden con el cliente,
endpoint y scope esperados; grant clientCredentials y autenticación header.
Se comprobó en privado la coherencia del secreto con la configuración local.
Tokens Cognito de acceso emitidos, issuer/scope/client correctos, sin publicar JWT.

Nodo HTTP temporal n8n con esa credencial → GET productivo /api/v1/risks:
**401** (23:25 UTC); flujo eliminado. El diagnóstico anterior desde cliente
había obtenido 403; conservar ambos con fecha/origen. No atribuir la causa
solo al estado ni declarar roles/cuenta productivos verificados.

#85 configura Development/example; estos archivos no acreditan carga en
Production. AccountSeeder está condicionado por Demo.Enabled, y una cuenta
verificada localmente no demuestra su existencia/estado en RDS. Jorge debe
comprobar configuración efectiva, cuenta técnica y Planificador mediante el
procedimiento acordado de operación/release. No hay risks 200 ni matriz real
de cinco acciones denegadas 403.

## Preparación para el deploy y límites

Ocho variables públicas de GitHub y Secret AGENT_N8N_WEBHOOK_SECRET preparados
para el transporte de #81. Agent deshabilitado (`AGENT_ENABLED=false`),
timeout 10 s, poll 250 ms y AutoOnIncident=true. Bases: webhook
`https://n8n.bryan-bano.com/webhook/istpetdev/agent` y API
`https://istpetdev.doicela.dev/api/v1`. Clientes humanos preservados.

Eso no acredita carga de esas variables en EC2; falta promoción/deploy con
Jorge. También faltan risks 200/denegados 403, humano Planificador,
Completed/Fallback/recuperación, reporte oficial de 30 muestras, CE-03 de dos
decisiones, E2E-07 completo y resolución PA-01/T9/T10. #39 sigue abierta.

Plan de continuidad completo: [plan-cierre-39-produccion.md](../plan-cierre-39-produccion.md).

## Continuación autorizada después de #83

Las observaciones previas conservan su fecha y entorno. #83 fusionado en
develop `a5a8c706eb7e3baed86fbc04535a4fd6f8075310`. #86 promovió ese HEAD
por PR a main `a45a54886a94b4e849b9ad664db41a78097e391f` (15-oct 00:08 UTC,
14-oct 19:08 Guayaquil), sin conflictos y con todos los checks aprobados.
CD `38658331163` iniciado; no se deduce éxito del deploy a partir del merge.
AGENT_ENABLED=false durante preparación; no se hicieron accesos nuevos al host.

Revalidación de ambos workflows: publicados iguales al borrador, versiones
sin cambios, conexiones/parámetros/credenciales remapeadas coinciden con
`8c99a71`; cron apagado. Curl desde el servidor n8n:

```text
{"status":"Healthy","timestamp":"2026-10-14T23:55:35.2155268Z","entries":[{"name":"database_ready","status":"Healthy","description":"PostgreSQL database is available.","durationMs":35.8328}]}
HTTP 200
```

23:56 UTC: probe HMAC sin modelo/herramientas 202, firma/bytes/clave coinciden;
nodo HTTP con credencial OAuth2 n8n GET risks 401. Ambos probes eliminados.
Este resultado se obtuvo antes de #86 y exige repetición tras CD.

Prelaunch de preparación: npm audit aprobado con excepciones vigentes;
consulta de paquetes NuGet (incluidas transitivas) sin hallazgos en los feeds
consultados. No son un scan de imagen ni acreditan runtime RDS/roles.
RBAC fijo/organización demo, validación de identidad desde Account, contexto
firmado y límites deterministas conservan pruebas CI del HEAD promovido.
Compose mantiene VerifyFull/CA y Swagger false; configuración efectiva en
EC2, permisos/cuentas y resultados reales aún deben verificarse.

Bryan confirmó ventana exclusiva en esta continuación. Renovación humana
autorizada: la sesión privada anterior no permitió refresh; Cognito aceptó
credenciales en login habitual y exige MFA del titular. Ningún token nuevo
disponible aún; no se deshabilitó MFA ni se publicaron datos de acceso.
No se corrió acceptance.sh, resets, Completed/Fallback ni CE-03.

## Cuentas y permisos productivos — continuación, 15-oct 01:13 UTC

CD [38658331163](https://github.com/bryancito1090/istpetdev-platform/actions/runs/38658331163)
exitoso para main `a45a54886a94b4e849b9ad664db41a78097e391f`. Bryan terminó
la contraseña/MFA de su cuenta propia y autorizó insertar exclusivamente dos
Account: humana Planificador y técnica Automatizacion. Se comprobó mediante
actividad de conexión que el backend EC2 usa RDS `istpetdev_integration`,
y se insertaron allí las dos filas en una transacción, con guardas de
issuer/subject/organización/rol. La conexión personal DBeaver `istpetdev_dev_01`
no es ese destino. Se conserva `VerifyFull`; sin migraciones, cambios de
grants, contraseñas DB o MFA de cuentas ajenas. Túnel SSM propio cerrado.

| Comprobación real | Resultado |
|---|---|
| Cuenta propia, GET /api/v1/me | 200, Planificador |
| Cuenta propia, GET /api/v1/risks | 200 |
| n8n, nodo HTTP con credencial IstpetDev M2M, GET risks | 200, 01:13:28 UTC |
| M2M, GET risks desde cliente de verificación | 200 |
| M2M, publicar / aprobar / rechazar / revertir / cambiar autonomía | 403 en las cinco acciones |

El nodo HTTP temporal se eliminó. Las cuatro acciones sobre recursos usaron
UUID inexistentes; cambiar autonomía fue rechazado antes de ejecución. No
hubo modificación efectiva durante las pruebas de denegación. Identidades
privadas y JWT quedan en archivos 0600 fuera del historial; no se adjuntan.

Se detectó además que la referencia Secrets Manager del migrador de integración
no autentica; **no se rotó ni se cambió la credencial del backend**. La transacción
acotada utilizó la credencial maestra administrada por RDS en memoria. Jorge debe
conciliar esa referencia privada de operación. No afecta los 200 ya observados.

Nuevo gate: POST demo/incidents (stub sin escritura, empleado solo para comprobar
Demo.Enabled) devuelve 404 con Planificador. La aceptación necesita habilitar
la demo mediante pipeline para la ventana exclusiva ya autorizada. Agente sigue
false. La regresión local del preset detectó un solo vehículo afectado y un stub
en ReplanAsync que omitía prefijo/efectos/modos. Se verifica su corrección local
antes de promover. No hay todavía resets productivos ni aceptación oficial.

Registro público: [#39](https://github.com/bryancito1090/istpetdev-platform/issues/39#issuecomment-6182176395),
[#38](https://github.com/bryancito1090/istpetdev-platform/issues/38#issuecomment-6182176708).
