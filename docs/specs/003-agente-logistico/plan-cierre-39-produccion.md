# Plan vigente para completar y cerrar #39 — agente n8n en producción

Actualizado el **14-oct-2026** por solicitud de Bryan. La ejecución comenzó
por su instrucción «empieza con los planes». Este documento sustituye el plan
que terminaba en un checkpoint; los antecedentes se conservan en Git y en
[task.md](task.md). Los pasos futuros no son resultados verificados.

Objetivo: cumplir los ocho pasos y todos los criterios de
[#39](https://github.com/bryancito1090/istpetdev-platform/issues/39), integrar
los cambios por PR a **develop**, resolver conflictos, conciliar la bóveda,
promover la configuración productiva por el procedimiento acordado y cerrar
GitHub **únicamente después de comprobar el cumplimiento completo**.

## 1. Responsables, autorizaciones y condiciones actuales

| Responsable | Trabajo |
|---|---|
| Codex | Continuar la revisión/ejecución, conservar la entrega de Antigravity, corregir dentro del alcance, preparar PR, resolver conflictos, merge a develop, documentación y cierre condicionado de #39 |
| Antigravity | Entrega anterior conservada; Bryan confirmó que terminó de editar. Evitar cambios concurrentes en repo/n8n |
| Bryan | Administración n8n, presupuesto DeepSeek, acceso privado y coordinación del equipo |
| Jorge / #38 | Operación habitual AWS/EC2/RDS y despliegue por pipeline. Bryan autorizó después a Codex la inserción acotada de las dos Account; ya verificadas |
| Equipo | Ventana exclusiva de resets/fallback y cualquier excepción a H5 |

Bryan ya asignó a Codex el merge a develop y el cierre cuando se cumplan los
criterios; no pedir nuevamente permiso genérico para esas acciones. Respetar
checks/revisión y la operación de la API asignada a Jorge. La revisión de
prelaunch no autoriza por sí sola cambios externos fuera de esta tarea.

Confirmaciones de Bryan durante esta ejecución:

- Antigravity terminó; Codex continúa como único editor operativo de n8n.
- El límite de gasto de la cuenta DeepSeek está configurado. Es confirmación
  del administrador, no una captura ni una auditoría independiente de pagos.
- Bryan confirmó después **ventana exclusiva autorizada**; reemplaza la espera
  inicial. Confirmar vigencia si la ejecución se posterga a otra sesión.
- El EC2 de la API lo opera Jorge; Bryan no dispone de alias SSH/SSM para él.
  Usar el pipeline acordado, sin crear accesos alternativos como atajo.

H5: **15-oct-2026 14:00 America/Guayaquil (19:00 UTC)**. Después no modificar
workflows, credenciales, versión ni código/DDL congelados sin excepción
concreta del equipo. Un cambio de prompt obliga a repetir aceptación.

## 2. Estado conocido y fuentes

Revalidar al continuar; los códigos observados pueden cambiar durante la
configuración de producción. Fuente de resultados: task.md, entrega,
[test-e2e.md](test-e2e.md) y evidencia saneada de esta ejecución.

| Pieza | Estado / siguiente acción |
|---|---|
| develop | PR #81, #85 y #83 integrados; base vigente tras #87 `34e7fd887aa95e65d1e70772f6189f3c8e6feaea`. Transporte M2M/agente en Compose/deploy/rollback, configuración Development/example y conciliación documental |
| main | `a45a54886a94b4e849b9ad664db41a78097e391f`; #86 fusionado mediante PR con todos los checks verdes. CD `38658331163` exitoso; Agent false y Demo false. Reaplicación `38665762599` falló al descargar GHCR antes de recrear |
| PR #83 / #86 | #83 fusionado a develop; #86 fusionado de develop a main por Codex en la continuación autorizada. operación habitual RDS de Jorge; inserción acotada de cuentas autorizada después a Codex y verificada |
| PR #85 / #38 | Bryan revisó y fusionó #85 a develop (`5582930`). CD y cuentas productivas comprobadas después; M2M risks 200 y cinco denegados 403 |
| n8n | Runtime/Compose 2.39.5; imagen/digest comprobados. Ambos workflows publicados, cron apagado, credenciales resueltas incluidas herramientas |
| Red | API readiness 200 desde servidor y contenedor n8n; HTTPS n8n 200 con User-Agent explícito. Rechazos 403 dependientes del cliente no acreditan HMAC |
| HMAC | Fuentes consolidadas, archivo privado/runtime/GitHub sincronizados; firma positiva 202 en prueba temporal sin modelo y negativos 401. Conservar evidencia y probar run real en G |
| M2M / humano | n8n con credencial real: risks 200, prueba eliminada. Cinco acciones humanas 403 con M2M. Cuenta propia con contraseña/MFA y Account Planificador: /me y risks 200; token privado vigente/renovable |
| Aceptación | Sin report productivo/Completed/Fallback/CE-03 acreditados; Demo false y regresión local de replanteo/preset en corrección. p95 local histórico 10001 ms, fallo conservado |
| Ventana | Bryan confirmó una ventana exclusiva en la continuación del 14-oct; confirmar vigencia si la ejecución se posterga por cuentas/despliegue/token |

PR #84 de ruteo/auditoría no se fusiona como efecto de #39. Si entra mientras
se prepara la release, revisar impacto en rutas/facts/fixture y aceptar el
backend realmente desplegado. No reutilizar tiempos de otro SHA/entorno.

## 3. Gates obligatorios de cierre

| Gate | Resultado exigido | Evidencia |
|---|---|---|
| GC-1 Versión | n8n exacto/fijado, nodo DeepSeek nativo declarado y estabilidad | Runtime, etiqueta/digest, comentarios #37/#39 |
| GC-2 Importación | Dos workflows del SHA oficial, credenciales resueltas, cron apagado, sin divergencia de lógica | Hashes, IDs/versiones publicadas, comparación de grafos y herramientas embebidas |
| GC-3 Identidad/firma | M2M risks 200, cinco acciones prohibidas 403; HMAC inválido/expirado/alterado rechazado antes del modelo | Nodo OAuth2 temporal eliminado, respuestas y estados saneados; prueba positiva de firma |
| GC-4 Ejecución/fallback | Watch Completed ≤10 s; Vigía detenido → Fallback ≤11 s; recuperación | Runs reales, tiempo de pared, modelo, v1, execution ID y traza |
| GC-5 Aceptación | 10 Watch + 10 Incident + 10 Injection; cuatro umbrales aprobados | report.md oficial, exit code, numeradores/denominadores y SHA(s) |
| GC-6 CE-03 | Dos decisiones: AutoExecuted y Escalated; explicación/facts/DriverMessage ≤160 | Incidente real, revisiones/decisiones y registro E2E-02/E2E-07 |
| GC-7 Registro/integración | PA-01/T9/T10 reales, bóveda coherente, sin secretos, cambios integrados a develop | PR/merge/CI, documentación y evidencia accesible |

Todos deben estar verificados para cerrar. Si falla un umbral, registrar el
informe y conservar la operación fallback según spec 003/D4; no convertir el
fallo en aceptación aprobada ni bajar umbrales. Un cambio de criterio exige
una decisión formal del equipo antes de modificar el cierre.

## 4. Lecturas y disciplina

Leer AGENTS.md, README, README de la bóveda, Hechos canónicos, spec/plan/task/
test-e2e de 003, entrega y antecedentes #36/#37/#38. Usar la skill local
istpetdev-prelaunch antes de preparar release/activar/ensayar; su fuente
editable es `docs/boveda/08 Skills/istpetdev-prelaunch.md`.

Referencias pertinentes: Runbook demo, n8n e IA, Identidad OIDC, Contratos API
y eventos, Credenciales, Verificación de seguridad, RBAC/configuración y Plan
de validación. ADR-21: demo con organización/roles/asignación fijos, sin RLS;
Schema95/RLS corresponde a P2. No introducir una migración por este cierre.

- Actualizar task.md por avance: distinguir código, prueba local, observación
  productiva y despliegue. Conservar intentos fallidos y fuentes/fechas.
- Reutilizar workflows, harness y controles. Corregir la causa comprobada con
  el mínimo cambio; no crear otro proveedor o cliente Cognito.
- Un único editor en n8n y una rama por implementación. Respaldar cambios ajenos
  antes de conciliarlos; no sobrescribirlos ni mezclar resultados sin revisar.
- Nunca publicar .env, JWT, HMAC, claves API, client_secret, refresh tokens,
  Authorization, datos personales o items transitorios Load HMAC Key.
- No push directo a main/develop, force-push, --admin o bypass de checks.

## 5. Fase A — Inventario y preparación verificable

1. Actualizar referencias GitHub y obtener estado/SHA de #81/#83/#85, develop,
   main y CD efectivo. Diferenciar merge, build y despliegue exitoso.
2. Preservar la entrega de Antigravity; reconciliar claims con pruebas reales.
   Existencia de credenciales o 202 de webhook no demuestra Completed ni risks 200.
3. Inventariar por API n8n ambos grafos, versión publicada, cron, conexiones,
   credenciales visibles y cinco ToolWorkflow embebidos. No imprimir parámetros
   que contengan secretos ni ejecutar el modelo para hacer inventario.
4. Desde SSH de n8n comprobar runtime, Compose, imagen/digest, mounts y clave
   de cifrado persistente por presencia/comparación privada. No recrear volúmenes.
5. Verificar salida HTTPS desde servidor y contenedor, con certificado válido:

   ```bash
   curl --fail --silent --show-error --write-out '\nHTTP %{http_code}\n' \
     https://istpetdev.doicela.dev/health/ready
   ```

6. Registrar presupuesto confirmado, accesos disponibles y pendientes. Reusar
   evidencia local/CI vigente para el mismo código; no llamar scan a restore/build.

## 6. Fase B — Resolver identidad/configuración productiva con Jorge

1. #85 ya está integrado: appsettings.Development.json no se carga en Production;
   appsettings.example.json es ejemplo y no equivale a configuración desplegada.
   El Dockerfile/pipeline deben transportar la configuración efectiva.
2. Revisar JWT con datos permitidos: issuer, token_use=access, client_id y scope
   `istpetdev-api/automation`. Mantener tokens solo en memoria/almacén privado.
   Verificar AllowedClientIds y TechnicalClientIds del runtime por allowlist.
3. Contrastar AccountRoleClaimsTransformation y Account real: organización demo
   activa, Issuer correcto, Subject=client_id, Kind=technical, Role=Automatizacion,
   IsActive=true. Una cuenta humana de Planificador debe ser independiente.
4. AccountSeeder solo corre con Demo.Enabled y Accounts configurados; omite
   cuentas existentes y no reconcilia rol/actividad. El CLI de cuentas humanas
   no es un provisionador técnico. No habilitar Demo__Enabled por suposición.
5. Jorge debe escoger/registrar la vía mínima de provisión acordada en #38:
   mantenimiento seguro existente o siembra explícita separada si hace falta.
   Si requiere código, implementarlo/probarlo antes de C; no nuevo cliente,
   grants/RLS, DDL o activación de resets como atajo. No reactivar/escalar una
   cuenta incorrecta sin revisar la causa y el procedimiento autorizado.
6. Verificar la cuenta humana, MFA/access token y permisos de demo. No cambiar
   Gerencia de Bryan para suplir Planificador ni publicar su sub/correo.
7. Conciliar la fila «Completado #38» heredada de #85 mientras solo haya prueba local.
   Registrar por separado cliente Cognito, configuración, cuenta y prueba productiva.
8. Preparar el escenario CE-03 en entorno aislado: dos rutas realmente afectadas,
   una elegible para AutoExecute y otra Escalated según política determinista.
   Dos IDs de ruta o un incidente que afecta solo una no cumplen CE-03.
   Revisar la preparación de acceptance.py; el script legado calibrate-incident.sh
   tiene fecha fija y no se ejecuta ciegamente en producción. No forzar decisiones.

## 7. Fase C — Código/documentación por PR y merge a develop

Usar PR #83 para auditoría/plan; si hace falta implementación adicional,
crear rama `chore/39-completar-produccion` desde develop actualizado. Reutilizar
#85 y el trabajo útil de Jorge, con revisión de sus resultados y documentación.

1. Antes de editar contrato, contrastar con la bóveda; actualizar primero el
   contrato si cambia una interfaz. En #39 se mantiene v1 y política determinista.
2. Integrar la base vigente en la rama de trabajo:

   ```bash
   git fetch origin --prune
   git merge origin/develop
   ```

3. Si hay conflictos, resolver por contenido: conservar autenticación humana,
   M2M, transporte deploy/rollback, cron apagado, historia de pruebas y decisiones
   aceptadas. No elegir ours/theirs para todos los archivos. Revisar el diff final.
4. Validar lo afectado: build-workflows.py --check, tests/scripts, pruebas .NET
   pertinentes, shell/YAML/Compose y actionlint si cambia deploy, npm run check
   si cambia frontend, check_vault.py y git diff --check. Usar PostGIS descartable
   para integración; nunca ejecutar suites de DB contra RDS productivo.
5. Revisar secretos del rango propio con redacción. UUIDs de referencias no son
   claves; una exclusión debe ser puntual/justificada, nunca desactivar el scanner.
6. Commit claro 10–72 caracteres; push a la rama de trabajo. PR a develop con
   alcance concreto, pruebas/limitaciones, type::chore, milestone H5 y revisor.
   Checkpoints usan **Refs #39**, sin cierre automático prematuro.
7. Esperar checks del HEAD exacto, resolver revisión/conflictos y volver a revisar
   si cambió la base. No usar un check de un commit anterior/cancelado como prueba.
8. **Codex realiza el merge autorizado a develop**, sin bypass:

   ```bash
   gh pr merge <PR> --squash --match-head-commit <HEAD-verificado>
   ```

9. Comprobar merge SHA y CI de develop; registrar integración y distinguirla
   del deploy. Código validado en develop no acredita una API productiva preparada.

## 8. Fase D — n8n estable, secreto único e importación reproducible

1. Respaldar Compose, PostgreSQL n8n y clave de cifrado en almacenamiento privado.
   Fijar `docker.n8n.io/n8nio/n8n:2.39.5`, mismo digest/runtime confirmado; no
   actualizar versión sin avisar a #37. Informar versión/nodo/pull en #37/#39.
2. Mantener timezone America/Guayaquil, crypto en allowlist y env accesible para
   el contrato existente. No habilitar módulos externos, runners inseguros ni
   ExecuteCommand por rutina. Conservar DB y clave de cifrado al recrear.
3. Generar HMAC con `openssl rand -hex 32` si no hay un secreto productivo válido
   establecido. Tratar la cadena hexadecimal como UTF-8 en ambos lados; no
   decodificarla como bytes hex en uno de ellos. Almacén privado 0600, fuera de Git.
4. Evitar dos fuentes: un env_file privado y GitHub Secrets, valores sincronizados
   en memoria/stdin; sin secreto literal en Compose ni shell tracing. Comparar
   configuración resuelta/runtime de forma privada, reportando solo igualdad.
5. Credenciales n8n: IstpetDev DeepSeek (deepSeekApi) y IstpetDev M2M (oAuth2Api,
   clientCredentials, scope automation). Presupuesto confirmado por Bryan;
   credenciales de Cognito se conservan cifradas en n8n, nunca en workflows.
6. Fuente oficial inicial de #37: `8c99a71c5a0017e769af4d7e40666565d684117d`:

   | Archivo | SHA-256 |
   |---|---|
   | vigia.json | fb1528b0c6aff1e9545bf6b6f8980f50033b3cb1559abfc1e4e86166582c4083 |
   | incidente.json | 83e56f3725a3b99abebe2f74396834801e21d7c8833ff38ca08aea2c9144a1bb |
   | agente-logistico-v1.md | 25c727e8afce7b5a922013814531a7e98da68ad8867aea816eed17f42fb0f542 |

7. Comparar remoto con export exacto de Git. Se permiten IDs/metadata de instancia
   y remapeo de credenciales, incluido workflowJson de las cinco herramientas.
   Si cambia lógica/prompt, corregir/exportar al repo mediante C y obtener nuevo SHA;
   no llamar «exacto del SHA» a cambios locales sin exportar.
8. Publicar Vigía/incidente individualmente, conservar Schedule deshabilitado.
   En n8n 2.x comprobar CLI --help si se usa publish:workflow; no --all. Validar
   versión publicada, no solo borrador. Guardar ID/versionId/hashes/fecha.
9. saveDataError/success=none, saveExecutionProgress=false, saveManualExecutions=false:
   no pinData o logs de Load HMAC Key. La ausencia de historial no prueba ausencia
   de ejecuciones; la API y report son la evidencia durable.
10. Direcciones exactas:

    | Configuración | Valor |
    |---|---|
    | Base webhook API | https://n8n.bryan-bano.com/webhook/istpetdev/agent |
    | Rutas añadidas por API | /watch y /incident |
    | ISTPETDEV_API_BASE_URL y Agent__N8n__ApiBaseUrl | https://istpetdev.doicela.dev/api/v1 |

11. Probar unsigned/invalid/expired/body alterado →401 en ambos workflows,
    antes del modelo. Firmar `timestamp + '.' + bytes exactos del body`, cabeceras
    X-IstpetDev-Timestamp / X-IstpetDev-Signature y prefijo sha256=. TLS válido.
    Un 403 de Cloudflare no es un 401 del workflow. Probar también firma positiva;
    un flujo temporal sin modelo puede verificar el parser, pero no reemplaza
    el run real de G. Borrar todos los nodos/flujos de prueba.

## 9. Fase E — Release de preparación y operación de la API por Jorge

1. Prelaunch del SHA final: roles/asignación, contexto firmado no controlado por
   modelo, HMAC, timeouts, secretos, TLS VerifyFull+CA RDS, Swagger restringido,
   mitigación SSH acordada en runbook y reportes de dependencias disponibles.
   Reusar evidencia vigente y marcar controles runtime no verificables.
2. Revisar diff main..develop y paridad/hotfixes antes de promover. No resetear
   ramas permanentes para quitar diferencias. Resolver por PR lo que corresponda.
3. Preparar PR **develop → main**, política efectiva del repositorio (no inventar
   release/* si branch-policy no la permite), con SHA/checks y revisor Jorge.
   La operación/promoción productiva queda coordinada con él según confirmación
   de Bryan. La preparación de este PR no es un despliegue ejecutado.
4. Configuración inicial: AGENT_ENABLED=false. GitHub transporta ocho variables
   públicas y el HMAC de GitHub Secrets. Conservar los dos clientes humanos:

   | Fuente GitHub | Uso |
   |---|---|
   | COGNITO_M2M_CLIENT_ID | AllowedClientIds__2 y TechnicalClientIds__0 |
   | AUTHENTICATION_TECHNICAL_READ_SCOPE | istpetdev-api/automation |
   | AGENT_ENABLED | false durante preparación; true solo en G |
   | AGENT_AUTO_ON_INCIDENT | true |
   | AGENT_TIMEOUT_SECONDS / AGENT_POLL_MILLISECONDS | 10 / 250 |
   | AGENT_N8N_WEBHOOK_URL | base de D |
   | AGENT_N8N_API_BASE_URL | API de D |
   | Secret AGENT_N8N_WEBHOOK_SECRET | mismo secreto privado de n8n |

5. Jorge verifica cuenta(s), mecanismo de provisión y secreto M2M privado según
   #38, incluido Secrets Manager pendiente si corresponde. No imprimir DB env.
6. Promoción por PR y CD sobre SHA inmutable; recrear servicios para aplicar env.
   restart_only no introduce nuevas variables. Migraciones por procedimiento
   oficial, sin ejecución manual/DDL ad hoc desde esta revisión.
7. Verificar CD exitoso del SHA, imagen/configuración efectiva y readiness 200.
   Un job cancelado/fallido o health antiguo no acredita deploy. Mantener guards
   de activación incompleta y el mismo transporte en rollback.

## 10. Fase F — Permisos y red antes de activar

1. Nodo HTTP temporal de n8n con credencial M2M: token obtenido y GET risks **200**;
   eliminarlo. Token emitido 200 + API 401/403 no cumplen este gate.
2. Con el mismo M2M, probar publicar, aprobar, rechazar, revertir y cambiar autonomía
   →**403**. Usar recursos apropiados y comprobar estado sin cambios; 401/404 no
   prueban la matriz. Mantener intentos fuera de las 30 muestras oficiales.
3. Verificar humano Planificador activo y su acceso/MFA/expiración; no publicar JWT.
4. Desde backend productivo comprobar HTTPS de ambos webhooks sin challenge/redirect
   y HMAC. No sustituir origen API por el curl de la laptop; no desactivar WAF global.
5. Registrar GC-1/2/3, provisión humana y configuración. Resolver fallos antes de G.

## 11. Fase G — Completed, Fallback y recuperación

1. Con F verificada, coordinar interrupción breve de Vigía con el equipo. Jorge
   cambia AGENT_ENABLED=true y redeploya mediante recreación. Comprobar health.
2. POST /api/v1/agent/runs con {"kind":"Watch"} y token Planificador; sondear
   hasta terminal. Medir tiempo de pared POST→terminal, no solo harnessMs.
3. Exigir Completed ≤10 s, modelo real, promptVersion=v1, n8nExecutionId,
   herramientas/traza y decisiones explicadas. Guardar run/fecha/SHA saneados.
4. Despublicar temporalmente solo Vigía, mantener Agent__Enabled=true y repetir:
   Fallback ≤11 s con motivo de transporte y decisiones deterministas utilizables.
   No simularlo apagando el agente ni modificando políticas.
5. Recuperación garantizada: republicar la misma versión aun si falla el ensayo;
   cron sigue apagado, Incidente activo. Repetir Watch y comprobar recuperación.
6. Registrar GC-4. Si falla tiempo/Completed/recuperación, corregir y repetir el
   camino afectado; no continuar como si estuviera aprobado.

## 12. Fase H — Matriz oficial y CE-03 en ventana exclusiva

1. Acordar ventana explícita y respaldo/recuperación con Jorge/equipo. El harness
   resetea antes de cada muestra y hace warmup/preparación adicional. Bryan ya
   confirmó una ventana exclusiva; comprobar que sigue vigente al superar los
   gates si la espera por cuentas/despliegue/token posterga la corrida.
2. Verificar fixture determinista de B, cuentas, presupuesto, token vigente y
   SHA de backend, workflows/prompt y harness por separado. Congelarlos durante
   la corrida; el SHA del report es checkout del harness, no prueba del deploy.
3. Ejecutar con token por entorno privado y shell tracing apagado:

   ```bash
   (
     set +x
     read -r -s -p 'Access token de Planificador: ' ISTPETDEV_PLANNER_TOKEN
     printf '\n'
     export ISTPETDEV_PLANNER_TOKEN
     export N8N_IMAGE='docker.n8n.io/n8nio/n8n:2.39.5'
     scripts/agent/acceptance.sh \
       --base-url https://istpetdev.doicela.dev --runs 10
   )
   ```

   Confirmar imagen real del servidor. No JWT en argumentos/logs ni --token-file
   (no soportado). Leer ayuda/script del checkout final antes de ejecutar.
4. Conservar report.md, exit code y fallos; exigir 10 Watch +10 Incident +10
   Injection, sin HarnessError/muestras omitidas, y los cuatro umbrales:
   herramientas ≥95%, p95 ≤10000 ms, decisiones Llm ≥90%, inyección 10/10 sin
   acciones prohibidas/cambio de política. Revisar numeradores/denominadores.
5. El harness penaliza fallback con mínimo 10001 ms; registrar impacto real en
   p95, sin inventar que un único fallback siempre hace fallar la matriz.
   harnessMs incluye reset/preparación. No editar resultados para conseguir PASS.
6. Si falla, conservar informe, operar según fallback y corregir causa. Repetir
   matriz completa con fecha/SHA nuevos cuando corresponda. Cambios de prompt
   obligan a repetir; cambios ejecutables vuelven a C/E/F/G antes de aceptación.
7. Adjuntar informe saneado a #39. Si no se puede subir adjunto por CLI, versionar
   solo report/evidencia saneados y enlazar al blob de un SHA permanente; no
   compartir artifacts privados completos o enlaces temporales inaccesibles.
8. Incidente predefinido en vivo: crear por API, esperar su run Incident real,
   respetar deduplicación y comprobar dos rutas/decisiones del escenario:
   AutoExecuted y Escalated, explicación/facts/DriverMessage ≤160 cada una.
   Verificar cambio automático de revisión y ausencia de ejecución de la escalada.
9. Registrar E2E-02/E2E-07, run/decisiones/revisiones, fecha/zona, entorno y SHA(s).
   Una única decisión no cumple CE-03; no fabricar una segunda fila o debilitar
   autonomía. Restaurar demo al estado acordado y avisar fin por canal autorizado.

## 13. Fase I — Conciliación documental y merge final a develop

Mantener un registro único de afirmación/requisito, estado, fecha/entorno,
SHA(s), fuente y responsable. Distinguir confirmación del administrador,
reporte de Antigravity y verificación propia; conservar antecedentes fallidos.

| Documento | Cambio que debe reflejar |
|---|---|
| task.md | T9/T10 por avance, diferenciando local/runtime/deploy y gates pendientes |
| test-e2e.md | Registros reales E2E-02/E2E-07; HMAC preparatorio no se etiqueta como matriz oficial aprobada |
| spec.md | PA-01 resuelta tras uso real de credenciales y evidencia; mantener FR/NFR/CE |
| plan.md / este plan | Contrato/configuración final si cambian, resultados y pendientes sin claims inventados |
| Entrega/evidencia #39 | Manifiesto SHA/hashes, imagen/digest, release/runs/report y límites |
| Bóveda Runbook | Estado cliente/configuración/cuentas productivos; no «Completado #38» basado solo en local |
| Bóveda n8n e IA | Estado actual y cron/modelo/v1, base webhook, privacidad y evidencia |
| Registro de hallazgos / Fuente documental | Avances, fallos conservados y fuentes de verificación |
| Hechos canónicos / ADR / Contratos | Solo datos afectados y contradicciones reales; preservar ADR-21/22 |

1. Editar solo notas relacionadas que discrepen. No cambiar propuestas ajenas ni
   declarar otros specs completos como efecto de #39. Evitar dos estados actuales
   incompatibles; el histórico debe estar fechado y señalado como antecedente.
2. Sanear antes de git add; comprobar secretos, enlaces, check_vault.py y diff.
   Si hay código, ejecutar checks afectados; para cambios editoriales reutilizar
   pruebas vigentes. Registrar el conteo real del verificador, no copiar uno viejo.
3. Preparar PR final a develop, actualizar base y **resolver conflictos por
   contenido** como C. Revisar de nuevo hechos/contratos y HEAD/checks exactos.
4. **Codex fusiona a develop** al cumplir requisitos del repositorio y registra
   SHA/CI. Un merge solo documental no cambia el SHA del backend validado ni
   exige redeploy por rutina. Si cambió ejecutable, repetir los gates afectados.

## 14. Fase J — Auditoría final y cierre de GitHub

Codex compara GC-1 a GC-7 con la descripción original de #39: versión estable,
importación oficial, Completed/Fallback productivos, reporte de umbrales,
CE-03/E2E-07, PA-01/T9/T10, documentación integrada y privacidad. Revisar que
no hubo cambios posteriores que invaliden la prueba ni pendientes escondidos.

Preparar comentario con fecha/entorno, versión, SHA(s), PR/merges/CD, enlaces
permanentes a runs/evidencias, métricas y CE-03. Actualizar casillas solo por
pruebas realizadas. Si existe ALK de Linear, enlazarlo; no duplicar subtareas.

**Solo con todos los gates verificados**, publicar evidencia y cerrar:

```bash
gh issue close 39 --repo bryancito1090/istpetdev-platform --reason completed
gh issue view 39 --repo bryancito1090/istpetdev-platform \
  --json state,closedAt,url
```

Si algún gate falla o falta evidencia, mantener abierta y registrar pendiente,
responsable y prueba concreta. Un cierre automático no equivale a aceptación;
corregir cualquier cierre prematuro dentro del alcance solicitado.

## 15. Recuperación y entrega de continuidad

- Conservar versión publicada/ID y backups privados antes de cambios. Ante fallo,
  recuperar configuración/versión conocida; no borrar volúmenes ni regenerar cifrado.
- Ante contingencia coordinar Agent__Enabled=false persistente/recreación y health.
  Restaurar Vigía tras la prueba. Imagen antigua no revierte migraciones: no
  deshacer DDL/restaurar RDS automáticamente; Jorge dirige recuperación.
- Entregar diff/SHA/PR/checks, manifiesto, evidencia saneada y gates restantes.
  El último responsable registra integración y despliegue por separado.
- Continuar trabajo independiente si falta ventana/acceso; pedir solo esa
  referencia/coordinación, nunca claves o tokens en mensajes. Respetar H5.
- Cuando se cumpla todo, ejecutar el cierre asignado; no terminar siempre en un
  checkpoint por heredar la instrucción antigua de mantener #39 abierta.
