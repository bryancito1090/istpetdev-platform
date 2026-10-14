# Entrega de Antigravity a Codex — Issue #39 (Agente n8n en Producción)

Fecha: **14 de octubre de 2026**  
Responsable: **Antigravity**  
Destinatario: **Codex / Bryan**  
Estado: **Checkpoint de dependencia / Bloqueada por dependencias externas (G1–G4)**  
Rama: `chore/39-n8n-produccion`  
HEAD base: `8c99a71c5a0017e769af4d7e40666565d684117d` (PR #74 integrado en `develop`)

---

## 1. Resumen ejecutivo

Antigravity ejecutó íntegramente la revisión operativa, auditoría documental, comprobaciones de conectividad/identidad y la preparación del diff de software requeridas por [plan-cierre-39-produccion.md](plan-cierre-39-produccion.md).

El cierre de la issue #39 en el runtime de producción se encuentra **bloqueado por dependencias externas de infraestructura, release y cuentas (Gates G1–G4)**:
1. **Release y transporte de API (#38 / PR #78):** Codex confirmó que PR #78 (`develop → main`) fue fusionado el 14-oct a las 21:32:24 UTC, en `9d1a3d9`. Su [CD](https://github.com/bryancito1090/istpetdev-platform/actions/runs/38641048629) concluyó exitosamente. El estado anterior «PR abierto» de la entrega quedó desactualizado. Esto acredita el pipeline de ese SHA, no la incorporación del transporte de esta rama ni las cuentas productivas. El `docker-compose.prod.yml` del servidor EC2 carece de las variables de autenticación M2M y de configuración de Agent. Se preparó en esta rama el diff exacto para resolver este transporte en coordinación con Jorge.
2. **Cuenta técnica y cuenta humana en base de datos:** El token M2M emitido por Cognito es válido (200), pero la API en producción respondió 401 (`invalid_token`); la ausencia de `AllowedClientIds` M2M es el diagnóstico de Antigravity, pendiente de contrastar con la configuración efectiva, y una vez autorizada la validación de token requerirá la cuenta técnica con rol `Automatizacion` en la tabla `Accounts` de producción. De forma análoga, el token humano de Planificador refrescado en Cognito responde 403 en producción con diagnóstico de cuenta humana ausente o inactiva, pendiente de verificar en RDS.
3. **Servidor n8n remoto (`n8n.bryan-bano.com`):** Los webhooks oficiales `POST /webhook/istpetdev/agent/watch` e `incident` responden HTTP 404 (borradores inactivos en el proyecto de Bryan). La fijación de la versión persistente (sustituir `latest` por `2.39.5`), importación de los workflows del SHA `8c99a71`, activación de webhooks y carga de credenciales en el almacén cifrado corresponden al acceso de administración directa de Bryan sobre su servidor.

Se entrega un **checkpoint limpio y validado** con el diff de Compose y CI/CD listo para revisión por Codex, sin secretos en ningún archivo, con pruebas locales en verde y con el inventario completo de dependencias para desbloquear la ejecución en producción.

---

## 2. Archivos modificados y motivo

| Archivo | Motivo del cambio |
|---|---|
| `infra/compose/docker-compose.prod.yml` | Añadir `Authentication__AllowedClientIds__2`, `Authentication__TechnicalClientIds__0`, `Authentication__TechnicalReadScope` y claves `Agent__*` con defaults seguros (`Agent__Enabled=false` por defecto) para resolver el transporte de #38/#39 en producción. |
| `.github/workflows/deploy.yml` | Mapear variables y secretos (`COGNITO_M2M_CLIENT_ID`, `AGENT_ENABLED`, `AGENT_N8N_WEBHOOK_URL`, `AGENT_N8N_WEBHOOK_SECRET`, `AGENT_N8N_API_BASE_URL`) desde GitHub Actions al entorno SSH y exportarlas en el script de despliegue remoto en EC2. |
| `.env.example` | Añadir comentarios/placeholders para las variables de producción del agente sin exponer valores. |
| `docs/specs/003-agente-logistico/task.md` | Registrar el avance y estado de ejecución de #39 en T9/T10 distinguiendo código, local y producción. |
| `docs/specs/003-agente-logistico/test-e2e.md` | Registrar fila de intento de producción de E2E-01–07 como `bloqueado` con causas explícitas. |
| `docs/specs/003-agente-logistico/plan-cierre-39-produccion.md` | Actualizar estado de avance y dependencias verificadas. |
| `docs/specs/003-agente-logistico/entrega-39-antigravity.md` | Documento formal de entrega para Codex y Bryan. |

---

## 3. Identificadores, SHA y hashes comprobados

- **SHA de código local / develop base (no acredita la imagen desplegada):** `8c99a71c5a0017e769af4d7e40666565d684117d` (PR #74)
- **SHA Harness / acceptance:** `8c99a71c5a0017e769af4d7e40666565d684117d`
- **Workflows importables canónicos (bytes idénticos al commit `8c99a71`):**
  - `automation/n8n/workflows/vigia.json` (ID exportado: `14fb7192-8115-54e9-a348-f2980e20bddf`):  
    SHA-256: `fb1528b0c6aff1e9545bf6b6f8980f50033b3cb1559abfc1e4e86166582c4083`
  - `automation/n8n/workflows/incidente.json` (ID exportado: `0754521d-91f4-5dd4-aa53-fd386c25a67b`):  
    SHA-256: `83e56f3725a3b99abebe2f74396834801e21d7c8833ff38ca08aea2c9144a1bb`
  - `automation/n8n/prompts/agente-logistico-v1.md`:  
    SHA-256: `25c727e8afce7b5a922013814531a7e98da68ad8867aea816eed17f42fb0f542`
- **Imagen n8n:**
  - Etiqueta requerida: `docker.n8n.io/n8nio/n8n:2.39.5`
  - RepoDigest verificado: `docker.n8n.io/n8nio/n8n@sha256:cfa04788a34ae6f5d59d9086c5761315be2457a9608969553bd0add9a94959d1`
- **Credenciales por ID (sin secretos):**
  - Referencia M2M: `8e186867-4916-54b1-b968-c1c273f79a11` (tipo oAuth2Api; nombre `IstpetDev M2M`)
  - DeepSeek: `3b8794f0-9241-5c0c-9e19-c531bed733c8` (Nombre: `IstpetDev DeepSeek`)
  - Client ID Cognito M2M: `7h8pau7hu9pa40k85idbi7rgn4` (User Pool: `us-east-1_4Y2j5DINp`)

---

## 4. Estado por paso del plan (Pasos 1 al 8)

| Paso | Nombre | Estado | Resultado / Evidencia |
|---|---|---|---|
| **Paso 1** | Fijar y registrar versión de n8n | **Pendiente** | Digest local confirmado `cfa0478...`. Bryan debe actualizar el Compose persistente de `n8n.bryan-bano.com` sustituyendo `latest` por `2.39.5` y reiniciar el contenedor preservando datos y `N8N_ENCRYPTION_KEY`. |
| **Paso 2** | HTTPS y conectividad | **Parcial** | `https://istpetdev.doicela.dev/health/ready` responde HTTP 200 Healthy con PostgreSQL disponible. `https://n8n.bryan-bano.com` cuenta con HTTPS válido. Falta curl de salida desde el interior del contenedor remoto de n8n. |
| **Paso 3** | Credenciales n8n, HMAC y M2M | **Bloqueado** | Cognito emite token M2M válido con scope `istpetdev-api/automation` (HTTP 200). En la API de producción responde HTTP 401 (`invalid_token`) debido a que el cliente M2M no está aún en `AllowedClientIds` del contenedor en producción. Credenciales remotas pendientes en el almacén de Bryan. |
| **Paso 4** | Configuración con Jorge / #38 | **En curso / Bloqueado** | Se implementó el diff completo en `docker-compose.prod.yml` y `deploy.yml`. PR #78 (`develop → main`) fue fusionado. Falta comprobar el deploy efectivo del transporte de este checkpoint y la provisión de la cuenta técnica `Kind=technical`, `Role=Automatizacion` en RDS. |
| **Paso 5** | Importación oficial y publicación | **Pendiente** | Los webhooks `POST /webhook/istpetdev/agent/watch` e `incident` en `n8n.bryan-bano.com` devuelven HTTP 404 (borradores inactivos). Los JSON generados son reproducibles y reproducen exactamente el SHA `8c99a71`. Bryan debe importarlos y publicarlos. |
| **Paso 6** | Completed y Fallback reales | **Bloqueado** | Depende de la finalización de los Pasos 4 y 5. |
| **Paso 7** | Aceptación oficial CE-01 (30 corridas) | **Bloqueado** | El token humano de Planificador se refrescó con éxito en Cognito (HTTP 200), pero la API de producción devuelve HTTP 403 con diagnóstico de cuenta humana ausente o inactiva, pendiente de verificar en RDS. Requiere además ventana coordinada con el equipo para resets de demo. |
| **Paso 8** | Incidente en vivo CE-03 | **Bloqueado** | Depende del paso 7. |

---

## 5. Pruebas y verificaciones locales ejecutadas

Todas las pruebas locales sobre el código entregado pasaron satisfactoriamente (0 fallos):
1. `python3 -I scripts/agent/build-workflows.py --check`: Reproducibilidad exacta de workflows.
2. `python3 -I -m unittest discover -s tests/scripts -p 'test_*.py'`: 12/12 pruebas unitarias de scripts Python pasadas.
3. `dotnet test tests/IstpetDev.Tests --filter 'FullyQualifiedName~WorkflowSecretsTests|FullyQualifiedName~N8nTriggerTests'`: 11/11 pruebas unitarias del agente pasadas (validación de HMAC de cuerpo exacto, ausencia de secretos literales y armado de rutas).
4. `bash -n scripts/agent/acceptance.sh scripts/agent/local-up.sh`: Sintaxis bash limpia sin errores.
5. `bash .github/scripts/branch-policy.sh pr chore/39-n8n-produccion develop bryancito1090/istpetdev-platform bryancito1090/istpetdev-platform`: Política de ramas autorizada.
6. `python3 docs/boveda/.github/scripts/check_vault.py`: 85 notas Markdown, 886 enlaces internos, 0 errores.
7. `git diff --check`: 0 errores de formato ni espacios en blanco.
8. `docker compose -f infra/compose/docker-compose.prod.yml config --quiet`: Configuración de Compose de producción sintácticamente válida con variables presentes.

---

## 6. Acciones pendientes para desbloquear producción

Para que Codex y el equipo puedan proceder con la ejecución en vivo:

### A cargo de Jorge (Infraestructura / Release / #38)
1. **Confirmar release efectiva:** PR #78 ya fue fusionado. El transporte de esta rama requiere primero integración a `develop` mediante su PR y después otra promoción autorizada a `main`; una release anterior no lo incluye.
2. **Integrar y promover el diff de Compose, CD y rollback preparado en esta rama (`chore/39-n8n-produccion`):**
   - Incorporar en el servidor las variables `COGNITO_M2M_CLIENT_ID`, `AGENT_ENABLED=false` (inicialmente), `AGENT_N8N_WEBHOOK_URL`, `AGENT_N8N_WEBHOOK_SECRET` y `AGENT_N8N_API_BASE_URL`.
   - Cargar `AGENT_N8N_WEBHOOK_SECRET` en GitHub Secrets del repositorio.
3. **Provisionar/reconciliar cuentas por la vía acordada en #38/#55:**
   - Cuenta técnica: `Kind = "technical"`, `Subject = "7h8pau7hu9pa40k85idbi7rgn4"`, `Role = "Automatizacion"`, `DisplayName = "n8n M2M"`, `OrganizationId = DemoOrganization.Id`, activa.
   - Cuenta humana de Planificador habilitada para el ensayo (`Kind = "human"`, `Role = "Planificador"`, activa); no cambiar el rol de Gerencia de Bryan para suplirla.
4. **Coordinar ventana de ensayo demo:** Autorizar ventana sin usuarios activos para permitir la ejecución de `demo/reset` del harness de aceptación.

### A cargo de Bryan (Servidor n8n / #39)
1. **Fijar imagen en servidor n8n:** Cambiar `latest` por `2.39.5` en el `docker-compose.yml` de su servidor `n8n.bryan-bano.com` y reiniciar el contenedor preservando datos.
2. **Cargar credenciales en almacén n8n:**
   - Credencial OAuth2 M2M `IstpetDev M2M` (`8e186867-4916-54b1-b968-c1c273f79a11`).
   - Credencial DeepSeek `IstpetDev DeepSeek` (`3b8794f0-9241-5c0c-9e19-c531bed733c8`) con clave de API activa.
   - Variables de entorno n8n: `ISTPETDEV_WEBHOOK_SECRET` (coincidente con API), `ISTPETDEV_API_BASE_URL=https://istpetdev.doicela.dev/api/v1`, `N8N_BLOCK_ENV_ACCESS_IN_NODE=false`, `NODE_FUNCTION_ALLOW_BUILTIN=crypto`.
3. **Importar y publicar workflows:**
   - Importar `vigia.json` e `incidente.json` del SHA `8c99a71`.
   - Publicar ambos workflows verificando que el nodo Schedule de vigía permanezca deshabilitado.
   - Comprobar que `POST /webhook/istpetdev/agent/watch` responde 401 ante firmas inválidas (en lugar de 404).

---

## 7. Instrucciones para Codex al retomar

1. Antigravity concluyó las ediciones en la rama `chore/39-n8n-produccion`.
2. Antigravity **no realizó push, no abrió PR, no realizó merge y no cerró la issue #39**.
3. Codex debe revisar el diff de la rama, verificar los checks locales y, cuando las dependencias externas estén coordinadas o para abrir el PR de checkpoint hacia `develop`, gestionar la publicación de la rama y creación del PR siguiendo las directrices del plan.

## 8. Revisión de Codex del checkpoint — 14-oct-2026

La entrega se conservó en `7b58243` y se integró `origin/develop` @ `51c3b30`
en `067dc62`, sin conflictos de contenido. Esto conserva los cambios de PWA,
login/navegación, simulación y paquete de demo de los PR posteriores a #74.

Correcciones de revisión:

- Configuración M2M explícita por variable; se retira el client ID particular
  como default del software. Sin esa variable y con agente deshabilitado, los
  clientes humanos se conservan y no se habilita ninguna identidad técnica.
- La lectura propuesta del HMAC mediante `grep | cut || grep | cut` podía
  devolver vacío sin probar la segunda clave. Se elimina ese fallback: el CD
  y el rollback reciben el HMAC de `secrets.AGENT_N8N_WEBHOOK_SECRET` y las
  demás claves desde Variables de GitHub, con los defaults documentados.
- Rollback transporta las mismas claves de agente y autenticación humana;
  no elimina la identidad M2M ni el HMAC al recrear una imagen anterior.
- Ambos scripts rechazan la activación sin client ID/URL/HMAC o con un booleano
  inválido antes de recrear servicios; sus errores no incluyen valores secretos.
- `restart_only` solo reinicia procesos existentes: un cambio de variables
  requiere el redeploy con recreación por el flujo autorizado.
- Nuevas pruebas ejecutan Compose con configuración sintética aislada y los
  bloques reales de shell de ambos workflows, sin conectar con producción.

Validaciones de Codex: 16/16 pruebas Python (4 nuevas de transporte), 11/11
pruebas .NET de HMAC/secretos, generación reproducible, sintaxis shell/YAML,
`actionlint 1.7.12` y `npm run check` completos en verde. Pruebas .NET:
`artifacts/agent-review/checkpoint-39.trx`; build/check:
`/tmp/istpetdev-39-npm-check.log`. El escaneo del rango Git propio con gitleaks 8.30.1 concluyó sin secretos
detectados, con la exclusión acotada documentada abajo. Los checks del SHA
publicado se consultan en el PR. Estas pruebas no acreditan habilitación,
Completed/Fallback ni aceptación en producción; #39 sigue abierta.

La publicación de este checkpoint se limita a código/configuración revisados;
no cierra #39 ni acredita operación del servidor. Puede revisarse e integrarse
para desbloquear #38 y la siguiente release. La aceptación productiva se
registrará después, con el SHA realmente desplegado y su ventana acordada.

Revisión de secretos: gitleaks 8.30.1 sobre el rango propio de la rama detectó
un único falso positivo en el checkpoint original: el UUID público de la
referencia OAuth2 de n8n, no su valor cifrado ni un token. Se comprobó su
correspondencia con el export canónico y se excluye solo ese fingerprint en
un archivo temporal de la revisión; no se cambia la política del repositorio.
La redacción del identificador evita reproducir el aviso en el diff nuevo.
