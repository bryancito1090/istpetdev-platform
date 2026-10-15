# Evidencia de limpieza de demo — issue #124

Fecha: 15-oct-2026. Rama: `chore/124-limpieza-restos-demo`.
Base inicial verificada: `cbdabf2` de `develop`. Actualización incorporada: `8259389` (CI de migraciones aditivas y evidencia documental de #108; sin cambios al frontend o backend).

## Dependencias verificadas en GitHub e incorporadas

| Dependencia | Evidencia |
|---|---|
| #121, Bodega | PR #133 fusionado |
| #122, escáner | PR #140 fusionado; incorporado durante esta limpieza |
| #123, Conductor | PR #127 fusionado |
| Receptor, spec 014 | PR #136 fusionado |

## Cambios

- Landing: retiradas cifras sin fuente, precio, formulario sin envío y pantallas
  con inventario, custodia y entregas ficticias. Los enlaces llevan al login y a /campo/.
- API web: `console.error` y aviso global accesible para puntos, bodegas,
  rutas, decisiones, datos importados, alertas y capturas rechazadas.
  Los fallos se conservan por recurso y se retiran solo tras una respuesta válida
  de ese recurso. Se mantienen los valores de retorno para compatibilidad,
  acompañados siempre del aviso de error.
- Eliminado `getPing` y su llamada al endpoint inexistente `/api/v1/ping`;
  los métodos reales `/health/live` y `/health/ready` permanecen.
- PWA: mock únicamente con `shouldMockFieldApi(isDevMode(), search)`,
  usando `URLSearchParams` para exigir `mock=true` exacto.
  Producción nunca lo activa. Sin exportaciones de mocks en el índice de campo;
  UUID separado en `field/uuid.ts` para que el outbox no importe el servicio mock.
- Plantillas CSV: solo cabeceras; eliminadas las filas de stock, consumo,
  productos, lotes, vehículos y ubicaciones inventadas que podían reimportarse.
  Los ejemplos de formato en columnas/placeholder son ayuda identificada como ejemplo;
  no se presentan como registros ni se precargan como valores.
- Simulador: conteo de corridas tomado de la API, retirada la cifra fija de 1080 jornadas.
- Documentación: corregidas referencias a recepción hospitalaria offline y mock
  como contingencia de red. El Receptor confirma en línea por spec 014.
  La matriz del pitch mantiene sus límites de evidencia.

## Validación ejecutada

| Comprobación | Resultado local |
|---|---|
| `npm run check` | Correcto: lint, guardia de login, .NET, shared, builds de producción web/PWA y artefactos /campo/ |
| `npm run test:frontend` | 150 pruebas: 24 shared + 64 web + 62 PWA |
| `dotnet test --logger "trx;LogFileName=issue-124.trx" --results-directory artifacts/issue-124` | 410 correctas, 0 fallos, 0 omitidas; PostGIS real descartable |
| `python docs/boveda/.github/scripts/check_vault.py` | 0 errores (avisos de lenguaje absoluto preexistentes) |
| `git diff --check` | Correcto |

Frontend validado con Node 22.23.3 y npm 10.9.9, las versiones del repositorio.
Las pruebas comprueban errores HTTP, recuperación, errores simultáneos,
listas realmente vacías, aviso renderizado y rechazo de mocks en producción
y de parámetros parciales.

## Pendientes de aceptación

- **Recorrido productivo con las seis cuentas:** no ejecutado en esta sesión.
  No hay sesiones autenticadas de esas seis personas a disposición del agente y
  estos cambios todavía requieren fusión y despliegue. El cierre de #55 no
  demuestra que este commit se haya recorrido en producción.
- **Revisión por otra persona:** pendiente de la revisión del PR.
- La issue #124 se mantiene abierta hasta esas verificaciones. No se certifica
  el cierre del recorrido productivo ni la aprobación humana con pruebas locales.

## Barrido solicitado y clasificación completa

Comando reproducible:

```sh
rg -n -i 'simulad|mockup|frontend-first|useMockApi|demo|ficticio|hu-00|del-00|HOSP-0|HU-2026|VH-EC-|Tungurahua|api\.istpetdev\.local|Math\.random' apps/web/src apps/mobile/src -g '*.ts' -g '*.html' -g '!*.spec.ts' -g '!**/mock/**' -g '!**/test-setup*'
```

Total: 34 coincidencias; todas clasificadas.

| Ubicación | Clasificación | Motivo |
|---|---|---|
| `apps/web/src/widgets/header/header.widget.ts:20` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/widgets/header/header.widget.ts:30` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/widgets/header/header.widget.ts:37` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/app/app.routes.ts:76` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/mobile/src/app/config/environment.production.ts:2` | Legítimo | Comentario sobre la publicación en /campo/ y API del mismo origen. |
| `apps/mobile/src/app/home/home.page.ts:71` | Desarrollo | Aviso dentro de @if (isMockApi()); solo proveedor mock explícito en desarrollo. |
| `apps/mobile/src/app/home/home.page.ts:76` | Desarrollo | Aviso dentro de @if (isMockApi()); solo proveedor mock explícito en desarrollo. |
| `apps/mobile/src/app/home/home.page.ts:79` | Desarrollo | Aviso dentro de @if (isMockApi()); solo proveedor mock explícito en desarrollo. |
| `apps/web/src/pages/simulation/simulation.page.ts:22` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/pages/simulation/simulation.page.ts:192` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/pages/simulation/simulation.page.ts:302` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/pages/simulation/simulation.page.ts:309` | Legítimo | Simulador de 90 días identificado como simulación; resultados y corridas leídos de la API. |
| `apps/web/src/shared/api/api.service.ts:622` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/api.service.ts:629` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/api.service.ts:630` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/api.service.ts:633` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/api.service.ts:634` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/field-api.interface.ts:10` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/field-api.interface.ts:11` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/field-api.interface.ts:24` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/http-web-field-api.service.ts:11` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/http-web-field-api.service.ts:12` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/http-web-field-api.service.ts:56` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/api/http-web-field-api.service.ts:57` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/field-api.interface.ts:17` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/field-api.interface.ts:18` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/field-api.interface.ts:34` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/http-field-api.service.ts:18` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/http-field-api.service.ts:19` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/http-field-api.service.ts:62` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/mobile/src/app/field/http-field-api.service.ts:63` | Legítimo | Contrato/adaptador de herramientas explícitas de demo: reinicio, incidente o avance por API real. |
| `apps/web/src/shared/ui/input/input.component.ts:66` | Legítimo | Identificador de elemento HTML; no genera métricas. |
| `apps/mobile/src/app/field/outbox/field-outbox.service.ts:495` | Legítimo | Jitter de reintento del outbox. |
| `apps/mobile/src/app/field/outbox/field-outbox.service.ts:577` | Legítimo | Jitter de reintento del outbox. |

No quedan datos operativos ficticios en las coincidencias del barrido.
También se conserva «Sin señal (prueba)» en la cola: es un interruptor de
prueba explícito, no un fallo de red oculto. Los mocks y sus fixtures en
`field/mock/` están excluidos del barrido y solo se seleccionan bajo
la condición de desarrollo documentada arriba.
