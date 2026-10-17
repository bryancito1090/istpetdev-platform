# #280 — consulta y recepción de guías de retorno

## Alcance de la verificación previa

Rama `fix/280-retorno-solo-lectura-idempotente`, basada en `develop` (5f0f0c6).
Se aplica `istpetdev-prelaunch`: exclusivamente pruebas locales con API de pruebas,
JWT firmado de prueba y PostgreSQL/PostGIS efímero de Testcontainers. Sin conexiones
a RDS, cambios de usuarios/roles compartidos, migraciones, configuración productiva
ni despliegue. Los cambios de rol de las pruebas se limitan a cuentas sintéticas.

- Autorización y custodia: se verificaron por HTTP los seis roles, ausencia de
  escrituras del GET y recepción/idempotencia bajo bloqueo de organización.
- Consultas: LINQ parametrizado, filtro de organización demo y número completo
  `GRT-` más ocho dígitos hexadecimales mayúsculos; colisiones dan 409.
- Campos del servidor: actor obtenido de la identidad; DTO de recepción conserva
  solo número, UUID de operación y hora opcional del dispositivo.
- RLS/RBAC administrable y plantillas: no aplican a este cambio en DemoCore;
  aislamiento por filtros y roles fijos, según el runbook vigente de la demo.
- QR público, archivos, cifrado, proxy/TLS y límites externos: no modificados;
  no se declara verificado el entorno desplegado mediante estas pruebas locales.
- Dependencias: sin nuevas dependencias ni cambios de lockfiles. Se ejecutó la
  compuerta de auditoría existente y las verificaciones exigidas por CONTRIBUTING.

## Cambio y contrato

La proyección compartida `BuildReturnGuideAsync` usa consultas sin seguimiento y
no escribe. Solamente `POST /generate` registra la custodia de generación; también
se serializa para no competir con la recepción ni alterar bultos ya devueltos.
`GET` permite Conductor, Bodega, Planificador, Gerencia y Auditor; Receptor queda
fuera y Auditor no recibe permisos de escritura.

Recepción resuelve el número y consulta `FindReplayAsync` dentro de la transacción
con bloqueo de organización existente. El fingerprint conserva acción, plan y
cuerpo; la respuesta se guarda junto con saldos, movimientos, custodia y pedido.
Se concilian únicamente líneas de bultos cuyo estado no sea `Returned`.

| Situación | Respuesta |
|---|---|
| Número fuera de `GRT-[0-9A-F]{8}` completo | 422 `INVALID_GUIDE_NUMBER` |
| Número válido sin plan | 404 `UNIT_NOT_FOUND` |
| Dos planes de la organización con el mismo número | 409 `AMBIGUOUS_GUIDE_NUMBER` |
| Misma cuenta, operación y cuerpo ya aceptados | 200 con la respuesta original |
| Operación reutilizada con otro cuerpo o acción | 409 `IDEMPOTENCY_CONFLICT` |
| `OperationId` vacío | 422 `INVALID_OPERATION_ID` |
| Todos los bultos de la guía ya devueltos | 409 `ALREADY_RECEIVED` |
| Guía sin bultos de retorno | 409 `NO_HANDLING_UNITS` |

Los bultos `Returned` permanecen visibles en la consulta y la guía muestra
`ReceivedAtWarehouse` cuando todos fueron recibidos. Se conserva el formato de
ocho dígitos: las colisiones se rechazan; no se elige un plan arbitrariamente.
No se crea una entidad nueva de guía ni se modifica el esquema.

## Regresión reproducida antes de la corrección

Sobre el código base 5f0f0c6, conservando las nuevas pruebas y los dos códigos de
error para compilarlas, se ejecutaron ocho casos de aceptación: **8 fallaron**.
Los cinco roles permitidos modificaban `Dispatched` a `NotDelivered` al consultar;
Receptor obtenía 200; los dos reintentos del mismo UUID (secuencial y concurrente)
terminaban en la restricción única del número de pedido en lugar de devolver la
respuesta original. Los archivos corregidos se restauraron después del ensayo.

## Verificación final

Regresión de campo: **121 pruebas pasaron, 0 fallos**, incluidas **24 pruebas HTTP
nuevas** de guías. Se ejecutó:

```bash
dotnet test tests/IstpetDev.Tests/IstpetDev.Tests.csproj --no-restore \
  --filter 'FullyQualifiedName~Integration.Field|FullyQualifiedName~Unit.Field' \
  --logger 'trx;LogFileName=issue280-field.trx'
```

| Criterio / caso adicional | Evidencia HTTP y de base real |
|---|---|
| GET sin efectos | Dos GET por cada uno de los cinco roles; snapshot idéntico de bultos, eventos, saldos, movimientos, pedidos e idempotencia |
| Mismo UUID | Secuencial y concurrente: dos 200 y cuerpos idénticos; bodega 3 → 13, vehículo 10 → 0; un movimiento, evento, pedido y registro idempotente |
| UUID distinto sobre la misma guía | 409 `ALREADY_RECEIVED`; snapshot sin modificaciones; incluye dos solicitudes simultáneas con UUID distintos |
| Receptor / Auditor | Receptor GET 403; Auditor GET 200 y POST generate/receive 403 |
| Formato y colisión | Seis números inválidos en GET y receive; número válido inexistente 404; dos planes con los mismos ocho dígitos dan 409 en GET, generate y receive |
| Devolución parcial / histórica | Solo se concilian los bultos pendientes de una guía mixta; una guía ya devuelta sin registro idempotente también da 409 |
| Reutilización incorrecta | UUID vacío 422; cuerpo cambiado o acción previa distinta 409; sin efectos |
| Generación explícita | Generar y consultar repetidamente conserva un único evento; generar después de recibir no modifica los bultos devueltos |

`npm run check`: verde (lint, API .NET, shared-core, web, móvil y artefactos PWA).
Hook de precommit ejecutado manualmente: verde. Después de reforzar las aserciones
de ubicación y eventos, se repitieron las 24 pruebas HTTP: 24 pasaron, 0 fallos.
Verificación documental: 117 notas, 1212 enlaces, 0 errores y 4 avisos preexistentes.
La auditoría .NET (`dotnet list IstpetDev.sln package --vulnerable
--include-transitive --format json`) no reportó paquetes vulnerables en los cinco
proyectos. `npm run audit:check` pasó con siete avisos cubiertos por excepciones
aprobadas vigentes. No se validaron imágenes desplegadas.

Esta evidencia no acredita un despliegue. No se publica nada en #200, de acuerdo
con el alcance expreso de #280.
