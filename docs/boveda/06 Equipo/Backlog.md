---
tipo: ejecucion
estado: propuesta
actualizado: 2026-10-14
tags: [hackathon, istpetdev]
---

# Backlog

Backlog compartido de IstpetDev, **sin asignación fija de integrantes**. Todas las capacidades de software empiezan en pendiente; actualizar solo con evidencia de avance.

## P0 — flujo evaluable

| ID | Entrega | Depende de | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-01 | Cerrar versiones y contrato | ADRs | Versiones/contrato revisados | **Completado** (ADR-01 a ADR-28) |
| B-02 | Fixture de 6 puntos/2 SKU | B-01 | Unidades y balance verificables | **Completado** (demo-base / demo-region) |
| B-29 | Identidad y RBAC en servidor | B-01, ADR-10/16, B-37 | RNF-01/RF-17; cuentas demo, roles/permisos/ámbitos administrables, delegación/campos y revocación comprobados; V-13/V-24/V-34 | **Completado** (ADR-21 roles fijos, ADR-26 Auditor, Spec 013) |
| B-03 | Catálogo, lotes y saldos | B-02/29 | RF-01/02, concurrencia válida | **Completado** (Spec 001) |
| B-04 | Consumo y cobertura | B-03 | RF-03, casos cero/desactualizado | **Completado** (Spec 002) |
| B-05 | Planner inicial | B-04, mapas | RF-04, inviables explícitos | **Completado** (Spec 002 / ADR-28) |
| B-06 | Unidades QR y custodia | B-03 | RF-05, líneas por lote | **Completado** (Spec 004) |
| B-07 | Despacho/recepción online | B-06/29 | Cantidades parciales y permisos RBAC/asignación comprobados | **Completado** (Spec 004) |
| B-08 | Persistencia y sincronización móvil | B-07 | RF-06, reinicio/reintento/duplicados y revocación offline de V-24 | **Completado** (Spec 004) |
| B-09 | Panel integrado | B-04/05/07 | RF-07, datos/versiones consistentes | **En curso** (Rutas, bodegas y mapas interactivos en develop, PR #59) |
| B-10 | Baseline/comparación pequeña | B-05 | RF-08, mismos datos/recursos | Pendiente (Spec 005) |

## P1 — sistema completo para demo

| ID | Entrega | Depende de | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-11 | S3 foto/firma | B-08 | RF-09, privada y reintentable | **Completado** (Spec 004) |
| B-12 | Consulta pública QR | B-06/11 | RF-13, datos limitados y revocación | **Completado** (Spec 004) |
| B-13 | SignalR y chat | B-07/29 | RF-10, políticas RBAC compartidas, historial y reconexión; V-14 | Pendiente |
| B-14 | Incidente/replanificación | B-05/13 | RF-11, lo ejecutado se conserva | **Completado** (Spec 002) |
| B-15 | n8n riesgo/explicación | B-04 | RF-12, fallback/JSON/error | **Completado** (Backend C# y triggers Spec 003) |
| B-16 | n8n incidente demo | B-14/15 | Credencial limitada y caso etiquetado | Pendiente |
| B-17 | Dataset 80 puntos/90 días | B-02 | Escenarios, seed y hash | Pendiente |
| B-18 | Simulador/KPIs/exportaciones | B-10/17 | RF-14, balance y denominadores | Pendiente |
| B-19 | Anomalía/investigación | B-04/17 | RF-15, alerta distinta de merma | Pendiente |
| B-20 | Hosting HTTPS y Hardening Linux | B-01, recursos | RNF-06, Perfil A aprovisionado con UFW, Nginx TLS 1.2/1.3, Fail2ban, loopback binding y Lynis > 80/100 | Pendiente |
| B-21 | Pipeline CI/CD DAG y rollback rápido | B-20 | ADR-15, GitHub Actions DAG con GHCR, retención de 3 versiones y rollback < 30s probado en vivo | **Completado en develop** (Issue #35 / PR #70: readiness estricto, fail-fast y rollback aditivo en [[Runbook de DevOps y base de datos para la demo]]) |
| B-22 | Ensayo físico y contingencias | B-08/12/14/18/20 | Recorrido completo y video | Pendiente |
| B-23 | Evidencia de usuario/mentor | B-09/22 | Validación documentada y cambios | Pendiente |
| B-24 | Paquete/pitch final | B-18/22/23 | Entregables consistentes con evidencia | Pendiente |

## P2 — operación nacional

| ID | Entrega | Criterio | Estado |
|---|---|---|---|
| B-25 | Multi-bodega/flota avanzada | Restricciones reales y benchmark | **Completado anticipadamente en develop** ([[Decisiones de arquitectura#ADR-28 — Ruteo Logístico Multiparada basado en Pedidos, Abastecimiento Multialmacén y Asesor de Rebalanceo|ADR-28]], PR #59) |
| B-26 | Pronóstico calibrado | Backtesting frente a base determinista | Pendiente |
| B-27 | Carga/autoscaling/recuperación | Volumen y tiempos comprobados | Pendiente |
| B-28 | Integración empresarial/piloto | Acuerdo y resultados observados | Pendiente |

## Documentación y skills

- D-01: bóveda inicial creada el 6 de octubre; validación de enlaces al cierre.
- D-02: actualizar reglas tras capacitación del 8.
- D-03: completar ADRs y convenciones con la primera entrega.
- D-04: generar skills según [[Catalogo y plan de skills]].
- D-05: registrar evidencia y cambios del evento.

Una fila se marca completa cuando cumple su criterio y tiene enlace a resultado. Las pruebas se planifican en [[Plan de validacion]].

## Ampliación de datos, configuración y seguridad — 9 de octubre

Diseño documental creado por pedido de Bryan; el esquema está aplicado en su base personal y publicado en un PR como borrador. Las entregas descomponen ADR-16 sin convertir la aplicación del esquema en evidencia de funcionalidades completas o despliegue.

| ID | Entrega | Depende de | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-37 | Migraciones del esquema completo e aislamiento | B-01, ADR-16 | Mapear/preservar migración inicial, FKs por organización, identidad/ámbitos/configuración, RLS/FORCE/grants probados con rol API; cobertura por módulo de [[Esquema completo de base de datos]]; V-37 | En curso: 95 tablas en dev_01, integridad RDS, aislamiento/PostGIS descartable, cliente EF/Npgsql y pool secuencial aprobados; PR #1 como borrador. JWT/HTTP, concurrencia/fallos del pool, recorrido/aislamiento RDS, revisión e integración pendientes; [[Coordinacion migracion schema95]] |
| B-38 | Gestión de plantillas/versiones/formularios | B-37/29 | RF-18, CRUD/publicación/activación, acceso a campos y captura histórica/offline; V-35 | Pendiente |
| B-39 | Parámetros, catálogos y workflows administrables | B-37/29 | RF-19, validación/activación sin solapamiento, handlers admitidos, reproducción de histórico; V-36 | Pendiente |
| B-40 | Integrar revisión previa a lanzamiento | B-20/21/29/37/38 | RNF-14, skill instalada y activación comprobada; checks/gates de software con evidencia vigente, controles complementarios V-38/V-39 y controles delegados existentes | Pendiente |

La aplicación personal del esquema es un avance de B-37; no cierra las pruebas de aislamiento ni la propagación a integración. La skill en la bóveda no cierra B-40. Evidencia y pendientes en [[Coordinacion migracion schema95]]. Acceso de Bryan preparado en [[Conexion PostgreSQL en DBeaver]], con dos roles ya documentados por bootstrap.

## Convención frontend FSD

B-09 implementa el panel con [[Frontend con Feature-Sliced Design]]. RF-16 y V-23 verifican estructura, dependencias y API pública; la decisión está aceptada por IstpetDev y no depende de seleccionar versiones.

## Complemento de entregas de infraestructura — 7 de octubre de 2026

Se conservan B-20 y B-21 del compañero. Cuenta/presupuesto, .NET 8/Angular 22, repositorio vacío y propuestas operativas ya se definieron; B-01 sigue pendiente hasta validar contratos/compatibilidad. B-29 tiene proveedor decidido (Cognito), pero implementación y pruebas siguen pendientes. No marcar B-20/B-21 completos por documentación.

| ID | Entrega | Prioridad/dependencia | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-30 | Prueba de dos API y backplane | Al habilitar varias réplicas; B-13/20; RNF-07 se conserva | V-15 con eventos/reconexión y afinidad; ALB probado en Perfil B antes de declarar escalado AWS | Pendiente |
| B-31 | RDS compartido y acceso dev | Antes de trabajo conjunto contra DB | SSM/TLS verificados, bases/credenciales aisladas y migraciones de integración controladas; V-30 | Pendiente |
| B-32 | Backup externo y restauración | Antes de depender de demo; B-20 | RPO/RTO medidos, backup obligatorio y V-26 aprobado. Desde el 12-oct es obligatorio **antes** de `reset-to-demo-core.sql` ([issue #15](https://github.com/bryancito1090/istpetdev-platform/issues/15), [[Runbook de DevOps y base de datos para la demo]]). 15-oct: herramienta `database-task.py` corregida, ensayo de volcado y restauración verificado en PostGIS desechable y agregado a CI; `backup-db.sh` respalda `istpetdev_integration` en S3. Falta la ejecución contra RDS con evidencia, el RPO/RTO medido y V-26 | En curso |
| B-33 | Dominio institucional y HTTPS | Antes de demo en teléfono; gestión por compañero | DNS/TLS/renovación, orígenes y CDN si aplica probados | Pendiente |
| B-34 | Configurar n8n existente | B-15/16 y API accesible | Workflows exportados, credenciales cargadas por Bryan, M2M y fallback; sin nueva instancia obligatoria | Pendiente |

B-20 incorpora V-25/V-29 y los entornos de [[Entornos y operacion acordados]]. B-21 implementará el compañero: manifest, respaldo obligatorio, health checks que fallan, retención por releases y V-27/V-28; meta rollback <30 s con imágenes precargadas. El esqueleto local no contiene estos workflows.

## Acceso seguro del equipo — 7 de octubre de 2026

| ID | Entrega | Prioridad/dependencia | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-35 | Acceso individual y gestión de credenciales | Antes de B-31/34; OIDC de Actions al implementar B-21 | SSO/MFA, permisos individuales, secretos dev por persona/servicio, sin claves AWS estáticas compartidas; V-32 con revocación y sin fugas en ambos repositorios | Pendiente |

La política y exclusiones de Git se documentaron en [[Credenciales y acceso del equipo]]. Crear roles/usuarios/secretos, cargar valores y validar permisos no se consideran completados por esta documentación.

## Ensayo temporal y cierre — 7 de octubre de 2026

B-35 usa ahora la propuesta de [[AWS temporal para la hackathon y cierre]]: IAM individual/MFA y login temporal, sin Organizations para conservar créditos. El criterio de aislamiento y ausencia de claves AWS compartidas permanece.

| ID | Entrega | Prioridad/dependencia | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-36 | Cierre de infraestructura de la hackathon | Preparar antes del ensayo; ejecutar al finalizar | Exportación privada verificada, retiro del stack y residuos de DB/backups/EBS/S3/logs/secrets, backend/KMS al final y V-33; sin afectar n8n ni recursos personales | Pendiente |

## Reestructuración propuesta — 12 de octubre de 2026

Propuesta de la sesión de definición registrada en [[Definiciones de producto y plan hasta el pitch]] (Deciders PENDIENTE). Si se acepta ADR-21, B-29 se cierra con roles fijos y B-37 a B-40 pasan a P2. Los specs del repositorio (`docs/specs/`) detallan cada entrega.

| ID | Entrega | Depende de | Criterio de cierre | Estado |
|---|---|---|---|---|
| B-41 | Núcleo delgado, importador canónico y reset de demo (spec 001) | ADR-21 | `dotnet test` y `test-e2e.md` del spec 001; simulacro de dataset sorpresa ≤ 60 min | Pendiente |
| B-42 | Motor logístico: riesgo, ruta de 2 vehículos, replanificación y política de autonomía (spec 002) | B-41, ADR-22, ADR-23 | Criterios CE-01 a CE-07 del spec 002 | Pendiente |
| B-43 | Fixture `demo-region` con tramos viales e incidente predefinido calibrado | B-42 | El incidente produce una decisión ejecutada sola y otra escalada | Pendiente |
| B-44 | Agente niveles A y B con n8n (spec 003) y prueba de aceptación de DeepSeek | B-42 | Informe de aceptación: llamadas válidas ≥ 95%, p95 ≤ 10 s, explicaciones coincidentes ≥ 90%, inyección 10/10; fallback visible | Pendiente |
| B-45 | Kit previo de validación y pitch (spec 006), preparado con IA sin horas de construcción del equipo | — | Guion y hoja consentida de feria, esqueleto 3/5/7, modelo de negocio con precio pendiente, veinte respuestas ≤30 s e investigación SERCOP; revisión humana de unos 15 min registrada; CE-01 del 006 | Kit preparado por IA y aprobado por Joseph el 14-oct en revisión de 15 min; CE-01 registrado en E2E-01 |
| B-46 | Evidencia del sector, ensayos y paquete en el evento (spec 006) | B-45, B-41 a B-44 y 005 | ≥3 conversaciones antes del 17-oct 12:30, ≥1 con reacción al prototipo; diez ensayos consecutivos, dos sin wifi; siete entregables y tag previo; V-40 a V-43 | Pendiente de evento y de CE ejecutados; hojas, escena y registro de ensayos preparados el 14-oct (task 006, T3–T5) |
| B-47 | Campo: QR, despacho, custodia, recepción offline, evidencia y consulta pública (spec 004) | B-41, B-42; avisos de B-44 | Criterios CE-01 a CE-10 del [spec 004](../../specs/004-campo-qr-custodia-offline/spec.md) (incluye login PKCE, PWA por HTTPS y `advance`); teléfono real en modo avión, reinicio y reintento | Pendiente |
| B-48 | Panel web conectado a riesgos, rutas, decisiones y entregas (spec 005) | B-41, B-42, B-44, B-47; lecturas de 002/004 | CE-01 a CE-03 del [spec 005](../../specs/005-panel-simulador/spec.md); sin cifras/puntos fijos y acciones confirmadas por API antes de H4 | Pendiente |
| B-49 | Simulador nacional de 90 días y comparación reproducible (spec 005) | B-41, B-42; importador y matriz homogénea | CE-04 a CE-08 del spec 005: tres seeds base, una adversa, dos baselines, exportación/hash y benchmark regional completo ≤60 min antes de H4 | Pendiente |
