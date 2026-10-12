---
tipo: validacion
estado: propuesta
actualizado: 2026-10-09
tags: [demo, validacion, istpetdev]
---

# Plan de validación

Las pruebas se realizan sobre entregas reales de software. **Todavía no se han ejecutado**; esta nota define qué evidencia necesitamos.

## Matriz de validación

| ID | Caso | Resultado que debe comprobarse | Requisitos |
|---|---|---|---|
| V-01 | Lote/movimientos/consumo | Balance por lote/SKU y unidad correcto | RF-01/02 |
| V-02 | Dos despachos concurrentes | No exceden stock ni dejan saldo negativo | RNF-03 |
| V-03 | Cobertura cero/ausente/antigua | Cálculo o incertidumbre explícitos | RF-03 |
| V-04 | Pronóstico sin datos futuros | Solo entradas conocidas antes del corte | RF-03/14 |
| V-05 | Ruta con capacidad/horarios | Factible o pendientes con motivo | RF-04 |
| V-06 | Incidente tras entregas | Replanifica pendientes y conserva ejecutado | RF-11 |
| V-07 | Recepción parcial | Cantidades restantes y custodia conciliables | RF-05/06 |
| V-08 | Modo avión + reinicio | Captura y archivos recuperables | RNF-04 |
| V-09 | Respuesta perdida + reintento | Una operación, un movimiento | RNF-02 |
| V-10 | Dos dispositivos/nueva clave | Duplicado de negocio/conflicto controlado | RNF-02/03 |
| V-11 | Token/ruta cambiados offline | Cola conservada; reauth/conflicto visible | RF-06 |
| V-12 | Archivo pendiente/fallido | Reintento separado de estado operativo | RF-09 |
| V-13 | Acceso cruzado/QR inválido | Mismo rol en otra organización o asignación no accede; QR inválido se rechaza y QR público no concede escritura ni evidencia privada | RNF-01/11 |
| V-14 | Chat/reconexión | Historial recuperado; políticas RBAC impiden entrar/enviar a grupos ajenos y retiran acceso revocado | RF-10, RNF-01 |
| V-15 | Clientes en dos réplicas | Eventos llegan y reanudan tras reinicio | RNF-07 |
| V-16 | Timeout/JSON vacío IA | Cálculo y fallback preservados | RF-12 |
| V-17 | Repetir simulación con seed | Resultados idénticos para misma configuración | RF-14 |
| V-18 | Baseline/recursos/obligaciones | Comparación justa y denominadores explícitos | RF-08/14 |
| V-19 | Anomalía etiquetada | Alertas y merma confirmada diferenciadas | RF-15 |
| V-20 | Uso con usuario/mentor | Entiende alerta, ruta y estado offline | RNF-09 |
| V-21 | Carga y autoscaling | Volumen, p95, errores y conexiones medidos | RNF-10 |
| V-22 | Failover/restauración | Recuperación y tiempo medidos | RNF-12 |
| V-23 | Límites FSD Angular web | Sin imports a capas superiores ni deep imports entre slices; API pública y build/lint válidos | RF-16 |
| V-24 | Matriz RBAC y revocación | Cada rol permite y deniega acciones implementadas según matriz; cuenta sin rol o rol falsificado se rechaza; revocación antes de sincronizar conserva cola sin efectos en servidor | RNF-01 |

Priorizar invariantes de inventario, autorización, offline y comparación. Usar pruebas unitarias para reglas y de integración con PostgreSQL real para transacciones/concurrencia; recorrido móvil con dispositivo real para persistencia/archivos.

## Validación de usuario

Realizar una sesión con una persona que conozca abastecimiento/limpieza, si está disponible, o mentor claramente identificado como tal. Pedir que interprete una alerta, revise una ruta y confirme una recepción.

Registrar tiempo, errores, dudas y cambios sin inventar representatividad estadística. Una conversación con mentor no equivale a un piloto de empresa.

## Registro mínimo

Usar [[Plantilla evidencia]]: fecha, versión, entorno, dataset/seed, prerequisitos, acción, resultado esperado/observado, archivo/log/captura y límites.

Conservar manifest del run y hash del dataset. Para pruebas sensibles guardar evidencia anonimizada; no añadir tokens o firmas reales al informe.

## Puertas de salida

- **P0:** V-01/02/03/05/07/08/09/10/13/23/24 y comparación mínima válidas; completar V-13 para evidencias al implementar RF-09.
- **P1:** recorrido integrado, evidencia S3, incidente, n8n/IA y simulador; validar V-04/06/11/12/14/16/17/18/19/20.
- **Nuevas acciones P1:** repetir V-13/V-24 para evidencias, automatización y tiempo real al incorporarlos.
- **Varias réplicas:** V-15 antes de mostrar escalado SignalR.
- **Declaración de rendimiento/HA:** V-21/V-22 con condiciones publicables.
- **Configuración solicitada por ADR-16:** V-34/35/36/37 antes de declarar roles/plantillas/procesos configurables; V-38/39 y evidencia de controles delegados antes de exponer demo/release. El esquema documental no cierra estas pruebas.

No repetir pruebas amplias por rutina: repetir lo afectado por cambios y los recorridos críticos antes del cierre. El respaldo del pitch está en [[Checklist y contingencias]].

## Resultados

Sin resultados aún. Crear notas de evidencia reales y enlazarlas aquí cuando existan.

## Verificación de infraestructura complementaria — 7 de octubre de 2026

| ID | Caso | Evidencia requerida |
|---|---|---|
| V-25 | Aprovisionamiento reproducible | Plan revisado, versions/digests/variables sin secrets y entorno recuperado desde IaC |
| V-26 | Restauración de backup externo | DB nueva, checksum/roles/migraciones/saldos/recibos/evidencias comprobados; RPO/RTO reales |
| V-27 | Deploy con fallo de backup/salud | Job rojo, backup obligatorio, no migrar sin respaldo y recuperar última release compatible |
| V-28 | Rollback sin descarga | Tres manifests/imágenes locales, `--pull never`, schema compatible y tiempo host <30 s; espera runner separada |
| V-29 | Red y hardening funcional | Scan externo de puertos, DB/Redis privados, egress/bridge válidos, SSH acotado/SSM y salud tras reinicio |
| V-30 | RDS dev aislado | Túnel SSM con TLS hostname validado, usuario no accede a otra base y migración personal no altera integración |
| V-31 | Toolchain y dispositivos | .NET 8 y Angular 22/Ionic 8/Node/TS acordados compilan y funcionan en el dispositivo objetivo |

V-15/RNF-07 siguen vigentes en B-30; no se eliminan por reemplazar la antigua B-21. Incorporar V-25/26/27/28/29 al cierre de B-20/21/32 según corresponda. Estos casos no se han ejecutado; la revisión del esqueleto/documentación no es prueba de software ni de AWS.

## Validación de credenciales — 7 de octubre de 2026

| ID | Caso | Evidencia requerida |
|---|---|---|
| V-32 | Acceso individual y secretos sin exposición | Dos identidades SSO/MFA: cada una lee su secreto personal y recibe denegación sobre el ajeno/prod; DB propia por SSM/TLS; API local sin claves AWS estáticas; workflow OIDC con subject real y permisos acotados al implementar B-21; revocación/rotación efectiva; sin valores secretos en repositorios, exportaciones n8n, logs o artefactos |

Pendiente de ejecución para B-35 y la parte correspondiente de B-21. Registrar identidad/entorno/resultado anonimizado; nunca capturar respuestas completas de Secrets Manager, contraseñas, connection strings ni tokens. Criterios y alta/baja en [[Credenciales y acceso del equipo]].

## Escenario temporal y cierre — 7 de octubre de 2026

V-32 se realiza con dos identidades IAM/MFA y credenciales temporales de `aws login`, según [[AWS temporal para la hackathon y cierre]]; no exige Organizations/SSO durante este ensayo. Se conserva la prueba de aislamiento y acceso limitado.

| ID | Caso | Evidencia requerida |
|---|---|---|
| V-33 | Retiro completo del proyecto | Exportación privada comprobada, inventario por tags/state y regiones, recursos del stack eliminados, sin snapshots/backups/volúmenes/versiones S3 facturables olvidados, secretos/KMS en eliminación documentada, bootstrap retirado al final y facturación revisada; sin tocar n8n/recursos ajenos |

## Configuración y lanzamiento — 9 de octubre

| ID | Caso | Resultado esperado | Requisito |
|---|---|---|---|
| V-34 | RBAC administrable y delegación | Rol nuevo permite solo acciones concedidas y dentro de ámbito; no autoconcede access.manage ni permisos/ámbitos ajenos; bootstrap conserva un administrador activo; revocación cambia AuthorizationVersion y bloquea próxima operación/SignalR/offline | RF-17, RNF-01 |
| V-35 | Plantilla, campos y versiones | Crear/publicar/activar versión; captura conserva versión/hash; cambios crean otra; usuario no lee/escribe campo restringido; contenido ejecutable rechazado/escapado; captura antigua aceptada o conflicto explícito, sin pérdida | RF-18 |
| V-36 | Configuración y workflow efectivos | Prioridad punto/ubicación/organización determinista; intervalos sin solapamiento; valores fuera de rango/handler desconocido rechazados; histórico reproducible y transición no viola stock/recepción | RF-19 |
| V-37 | RLS real y conexión reutilizada | Catálogo tenant completo con RLS/FORCE/policies y rol API sin bypass; dos organizaciones/contexto vacío prueban SELECT/INSERT/UPDATE/DELETE, FK cruzada, tabla hija, ámbitos de punto/entrega/chat y reset entre peticiones del mismo pool | RNF-13 |
| V-38 | Campos, contenido, cifrado y archivos | Intento de cambiar actor/organización/privilegios/saldo no tiene efecto; plantilla no habilita SQL/script/SSRF; datos clasificados cifrados y lectura acotada; archivo con MIME/checksum falsos y objeto pendiente no se sirve; SQL manual parametrizado | RNF-14, RF-18 |
| V-39 | Abuso, cabeceras y dependencias del release | Ráfagas limitadas por identidad/organización/acción y QR no enumerable; cabeceras reales de proxy/browser válidas; reporte para revisión/lockfiles/digest exactos y sin hallazgo alto/crítico explotable sin mitigación | RNF-14 |

Registrar pruebas reales con fixtures sintéticos, misma revisión y entorno. Controles ya definidos por las otras skills se reutilizan desde su evidencia; [[istpetdev-prelaunch]] añade los complementarios de [[Verificacion de seguridad antes del lanzamiento]]. Si falta acceso a DB/runtime, resultado pendiente, no aprobado por inspección documental.

Pendiente de ejecución al terminar la hackathon; documentar residuos con espera obligatoria y no afirmar factura cero por apagar EC2/RDS.
