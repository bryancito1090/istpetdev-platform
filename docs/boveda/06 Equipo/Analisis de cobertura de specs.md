---
tipo: analisis-producto
estado: propuesta
actualizado: 2026-10-12
tags: [producto, specs, alcance, avance]
---

# Análisis de cobertura de specs

Análisis solicitado por Bryan: estimar qué porcentaje del objetivo aporta completar cada spec y señalar, solo a grandes rasgos, qué specs podrían faltar después. Los porcentajes son una **ponderación propuesta de capacidades**, no una medición de horas ni la matriz de puntuación del jurado. Esta nota conserva los límites del proyecto y no aprueba nuevos requisitos.

## Resultado principal y alcance del porcentaje

**Los seis specs existentes cubren el recorrido principal de la demo acotada. Recomiendo completar esa entrega antes de abrir nuevos specs funcionales.** Hay decisiones y contratos por completar dentro de los specs actuales, y trabajo transversal de infraestructura y seguridad que condiciona su cierre.

El 100% de esta estimación significa: cumplir el objetivo norte de [[Definiciones de producto y plan hasta el pitch]], demostrarlo con evidencia y completar los entregables de la hackathon. Incluye una organización, una bodega y dos vehículos en el recorrido regional; datos importables; riesgo y rutas deterministas; agente con límites; custodia y recepción offline; panel y comparación sintética nacional. La operación diaria de una empresa y la madurez nacional P2 constituyen entregas posteriores con otro alcance.

El recorte recordado por Bryan aparece en D3 y ADR-21: roles fijos, configuración en archivos, avisos al conductor, una instancia, PWA y media móvil. RF-17/18/19 y RNF-13 se proponen para P2. La evidencia de [001](../../specs/001-nucleo-delgado-importador/verification.md) y [[Fuente y control documental]] **ya registra autorización de Bryan para la sustitución local de Schema95**, con conservación de migraciones. A la vez, [[Hechos canonicos]] y [[Decisiones de arquitectura]] mantienen pendientes los Deciders colectivos. Debe sincronizarse ese registro; la autorización local no acredita aceptación colectiva de todas las definiciones ni transición de RDS.

Para el alcance original ampliado de [[Requisitos y aceptacion]], los seis specs no representan el 100%: siguen fuera la configuración administrable, RLS multiempresa y otras capacidades P2. Además, RF-15 conserva prioridad P1 sin un spec dueño de su investigación. No asigno un porcentaje a todo ese producto ampliado porque sus entregas futuras aún carecen de alcance y aceptación detallados.

## Ponderación recomendada al completar cada spec

| Spec | Aporte al proyecto acotado | Acumulado en orden numérico | Resultado que añade |
|---|---:|---:|---|
| [001 — Núcleo e importador](../../specs/001-nucleo-delgado-importador/spec.md) | 10% | **10%** | Datos canónicos, saldos conciliables, roles fijos, importación adaptable y reset reproducible. |
| [002 — Motor logístico](../../specs/002-motor-logistico/spec.md) | 25% | **35%** | Riesgo explicado, cantidades, rutas factibles, reservas, incidentes, revisiones y política de autonomía. |
| [003 — Agente logístico](../../specs/003-agente-logistico/spec.md) | 10% | **45%** | Orquestación n8n/DeepSeek, traza, explicación contrastada y fallback determinista. |
| [004 — Campo, QR y offline](../../specs/004-campo-qr-custodia-offline/spec.md) | 25% | **70%** | Login web/PWA, caja y custodia reales en el sistema, despacho, recepción durable, evidencia privada y consulta pública. |
| [005 — Panel y simulador](../../specs/005-panel-simulador/spec.md) | 25% | **95%** | Flujo visible sobre datos de API y comparación reproducible de 90 días contra dos baselines. |
| [006 — Validación y entrega](../../specs/006-validacion-pitch-entrega/spec.md) | 5% | **100%** | Validación sectorial, ensayos, afirmaciones sustentadas, pitch y paquete de siete entregables. |

La base recibe 10% porque habilita el producto, pero todavía no realiza una decisión logística ni una entrega. El motor, campo y panel/simulador concentran 75%: materializan las tres necesidades del [[Reto 1 oficial]] y su evidencia. El agente aporta 10% porque se apoya en el motor y tiene fallback. El 006 aporta 5% en este indicador de construcción/cierre: **su importancia para la evaluación es mayor**, pues sostiene varios criterios oficiales. Estos pesos no predicen la nota del jurado.

Con 001–005 completos estaría cubierto el software del recorrido definido; el 5% restante corresponde al cierre del proyecto y sus entregables. El acumulado presupone que también están completos los specs anteriores y que las dependencias de entorno aplicables tienen evidencia. Un documento redactado, tablas creadas o pruebas de un solo módulo no acreditan el aporte entero.

El orden de ejecución puede ser distinto del numérico: después de 001/002, conviene avanzar campo y simulador; el simulador depende del importador y el motor, mientras el panel consume también 003/004. Las migraciones requieren coordinación en serie. El 006 puede preparar su kit desde ahora y cerrar con las ejecuciones finales.

## Estado observado hoy

Revisión local sobre `b6b7b11`, en `feature/001-nucleo-delgado-importador`, el 12-oct-2026. Se leyeron los seis `spec.md`, sus planes/tareas/E2E en las secciones pertinentes, objetivos, prioridades, requisitos, contratos, fórmulas, validación, riesgos y runbook de la bóveda. Se contrastaron el inventario local de implementaciones y los contadores de los TRX existentes. No se reejecutaron pruebas de aplicación ni se consultaron AWS, el host o el estado remoto de las issues.

| Bloque | Evidencia disponible | Cierre que falta acreditar |
|---|---|---|
| 001 | Estado `en curso`; tareas implementadas y evidencia local. TRX: 66 unitarias, 48 de integración y 1 E2E aprobadas, **115 en total**. | Simulacro humano de dataset sorpresa obligatorio E2E-03; Cognito real y ensayo expuesto E2E-04/05; registro final de decisiones e integración compartida. |
| 002 | Spec, plan, tareas y siete escenarios E2E escritos; tareas pendientes. | Implementación y evidencia del motor, factibilidad, reservas, incidentes y rendimiento. |
| 003 | Cuatro documentos; tareas y aceptación del proveedor pendientes. | Workflows, identidad M2M, ejecución real, fallback y prueba de aceptación. |
| 004 | Cuatro documentos y diez escenarios E2E; tareas pendientes. | Custodia/recepción real, login, PWA HTTPS, teléfono offline y evidencia privada. |
| 005 | Cuatro documentos y diez escenarios E2E; tareas pendientes. | Panel conectado, simulador ejecutado, invariantes, ZIP y benchmark completo. |
| 006 | Cuatro documentos; kit, entrevistas, ensayos y entrega pendientes. | Evidencia humana, materiales y paquete final. |

**Estimación orientativa de avance funcional local: aproximadamente 8–10% del proyecto acotado.** El único bloque sustancialmente implementado y probado que se acredita aquí es 001; todavía tiene condiciones de cierre pendientes. Este rango usa el peso de 001 y un descuento cualitativo por esas condiciones: no es un porcentaje auditado. La documentación no permite acreditar aún ningún spec como formalmente `cerrado` ni la demo integrada.

Los TRX locales `artifacts/spec001/Unit.trx`, `Integration.trx` y `E2E.trx` corroboran los contadores de [verification.md](../../specs/001-nucleo-delgado-importador/verification.md). Son artefactos de ejecución fuera de Git; el resumen versionado de evidencia está en esa nota. La tabla T8 de 001 todavía dice 114; el índice de specs todavía presenta 001 como borrador. También hay encabezados anteriores que describen la implementación como pendiente. Se debe dar precedencia a la evidencia concreta y actualizar esos resúmenes al cerrar el módulo. La procedencia de las ejecuciones debe quedar ligada al commit efectivo; el registro E2E cita `06f2911`, identificado como base por T0, mientras la implementación figura en `1f7bf0d`.

## Cobertura contra los objetivos de la bóveda

| Objetivo o requisito | Specs que lo cubren | Límite relevante |
|---|---|---|
| Catálogo, puntos y existencias; RF-01/02 | 001; 002 para reservas; 004 para transferencias | Importación histórica y saldos absolutos no completan el registro cotidiano de consumo por lote. |
| Reabastecimiento crítico; RF-03 | 002, visible en 005 | Media móvil declarada; cinco días corresponde al caso ilustrativo, no a una garantía general. |
| Rutas eficientes y factibles; RF-04 | 002, visible en 005 | Una bodega por planificación y dos vehículos probados en vivo; heurística, sin prueba de óptimo global. |
| Custodia, recepción offline y evidencia; RF-05/06/09/13 | 004, con datos de 001/002 | Receptor firma en dispositivo del conductor; consulta pública limitada, sin sesión propia del receptor en la demo. |
| Dashboard y comparación; RF-07/08/14 | 005 sobre 001/002/003/004 | Impacto sintético con calendario común, tres seeds base y caso adverso; ahorro empresarial observado pendiente de piloto. |
| Incidentes y automatización; RF-11/12 | 002/003/004/005 | Nivel C del agente y chat libre quedan fuera del cierre inicial. |
| Frontend FSD y usabilidad; RF-16, RNF-09 | Transversal; principalmente 004/005 | Comprobar contratos, móvil real, legibilidad y estados además del build. |
| Acceso por rol/asignación, idempotencia y transacciones; RNF-01/02/03/04/11 | 001/002/004, con límites M2M en 003 | Roles fijos para una organización; revocación y asignaciones deben probarse al sincronizar. |
| Infraestructura y seguridad previa a exposición; RNF-06/08/14 | Runbook y controles transversales, con cambios propios de 001/003/004/005 | La infraestructura existente necesita verificación del entorno final; RNF-14/B-40 se conservan. |
| Anomalías y merma investigada; RF-15/V-19 | **Sin cobertura completa en 001–006** | FEFO, cantidades rechazadas y balance de simulación aportan controles, pero no constituyen investigación/cierre de anomalías. |
| Roles, plantillas y procesos configurables; RF-17/18/19 | Dirección de ADR-16; fuera de estos specs | Diferidos para la demo según ADR-21; no se pierden como objetivo del producto ampliado. |
| Operación nacional, recuperación e integraciones; P2 | Diseño general, sin specs implementables equivalentes | La simulación de 80 puntos no acredita operación nacional real ni disponibilidad comprobada. |

## Qué completar dentro de los specs actuales

Estas brechas requieren decisiones, contratos o tareas concretas; no justifican crear nuevos módulos de producto antes de terminar la demo.

1. **Coherencia del alcance.** Sincronizar el permiso local registrado para 001 con los estados de ADR-21/Hechos canónicos y distinguir lo todavía pendiente del equipo. Resolver explícitamente el destino de RF-15/V-19: diferir investigación de anomalías al piloto o añadir un caso mínimo con dueño y prueba. D3 no lo corta expresamente, aunque [[Plus para ganar]] trata conciliación de merma como extra.
2. **Transición de base de datos.** El runbook y E2E-05 de 001 aún describen recreación de bases, mientras el plan revisado, la implementación y su evidencia conservan migraciones y usan una migración posterior con guard. Unificar el procedimiento antes de ejecutar cualquier transición externa. No se acredita aquí respaldo, restauración ni compatibilidad de rollback.
3. **Interfaz de Bodega en 004.** PA-03 sigue abierta: web o PWA. T2 tiene etiqueta imprimible y T3 backend de despacho, pero falta concretar la pantalla que selecciona entrega, prepara unidades, escanea y confirma despacho para la escena física.
4. **Devolución física en 004.** FR-08, CE-04, T4 y E2E-04 ya la exigen; no falta concebir el comportamiento. Falta fijar su ruta HTTP, cuerpo, permisos e interfaz: no figura un endpoint de devolución en la tabla de contratos. Completar 004 y conservar su idempotencia/custodia.
5. **Evidencias y acceso en 004.** PA-04 propone decidir entre upload por API y URL prefirmada, pero plan/T6 ya describen presignadas. Elegir una vía y alinear requisitos/contratos/tareas/infraestructura. Resolver PA-05 sobre límites de QR. Además, T1b pide guards con rol leído de API: definir el contrato que entrega la cuenta/rol resuelto en servidor, actualmente sin endpoint explícito en estos specs.
6. **Dependencias de 003.** Confirmar dueño y disponibilidad del cliente Cognito M2M, credenciales privadas y versión de n8n local. La defensa por política y fallback ya está especificada; lo pendiente es implementarla y medirla.
7. **Comparación de 005.** Fijar dataset nacional, matriz permitida, máquina dedicada y versiones de baselines antes de correr. Concretar cómo se obtienen las obligaciones comunes para puntualidad: su denominador debe existir independientemente de los pedidos creados por cada política. Resolver la diferencia entre NFR-01/CE-07 que nombran el entorno de demo y FR-21 que exige máquina dedicada; el benchmark debe identificar su entorno real.
8. **Seguridad y operación para el cierre.** ADR-21 todavía dice que B-40 se difiere, pero el plan revisado de 001 y el registro de implementación conservan RNF-14/B-40. Mantener esa revisión e incorporar su evidencia al cierre del 006 y al runbook. Verificar los pendientes documentados de TLS hacia RDS, Swagger, acceso al host, respaldo y base de simulación según [[Verificacion de seguridad antes del lanzamiento]]. El estado actual de las issues externas no fue consultado en este análisis.

La existencia de estos pendientes impide interpretar la tabla de porcentajes como una declaración actual de versión lista. Los bloqueos aplicables se resuelven con los dueños ya definidos y evidencia de la versión integrada.

## Specs futuros: solo la intención, sin desarrollarlos

La secuencia para integrar/probar el núcleo está en [[Seguimiento de implementacion y pruebas]]; la entrada, primera entrega y comprobación de cada etapa futura se desarrollan en [[Continuidad del piloto y specs 007 a 012]].

**Para terminar la demo del objetivo norte no propongo un spec 007 funcional obligatorio.** Los siguientes IDs son sugerencias de una futura secuencia; no se han creado carpetas ni se ha ampliado el compromiso de la hackathon.

Si después se quiere pasar a un piloto pequeño con una empresa, documentaría primero estos tres:

| ID sugerido | Spec futuro | Qué abarcaría por encima | Motivo y límite |
|---|---|---|---|
| 007 | **Inventario cotidiano y conciliación** | Entradas de bodega, consulta de saldos/movimientos, consumo por lote, demanda no atendida, solicitud de reposición, bloqueo/vencimiento, ajustes justificados e investigación de anomalías/mermas. | Cierra el ciclo que hoy se alimenta con importaciones. Fuentes: [[Usuarios y flujos]], [[Contratos API y eventos]], [[Inventario y prediccion]], RF-02/15 y V-01/19. Retoma devoluciones ya cubiertas en 004; una bodega y organización, sin compras/contabilidad de ERP. |
| 008 | **Operación sostenida de un piloto** | Roles de DB separados, backups y restauración medidos, actualización/rollback compatible, observabilidad, retención de evidencias, altas/bajas de cuentas y soporte operativo. | Extiende el runbook temporal a funcionamiento diario. Fuentes: [[Entornos y operacion acordados]], [[Plan de validacion]] y B-27/32/40. La seguridad básica previa a la demo sigue siendo exigible ahora. |
| 009 | **Piloto real e integración mínima** | Incorporar datos de una contraparte por los conectores realmente necesarios, baseline acordado, cuatro semanas de operación y KPI/costos observados con decisión de continuar/cambiar/parar. | 006 prepara la propuesta; no ejecuta un piloto. Fuentes: [[Modelo de negocio y piloto]] y B-28. Mantener una empresa/bodega y puntos acordados; no desarrollar integraciones hipotéticas. |

Los siguientes pertenecen a la dirección amplia/P2 de la bóveda y solo se detallan al activar esa etapa:

| ID sugerido | Spec futuro | Qué abarcaría por encima | Cuándo corresponde |
|---|---|---|---|
| 010 | **Acceso multiempresa y configuración administrable** | Aislamiento por organización/RLS, roles/ámbitos delegables, parámetros, catálogos, plantillas y transiciones versionadas sobre handlers existentes. | Retomar ADR-16 y RF-17/18/19/RNF-13. Usar [[RBAC y configuracion del sistema]] y [[Esquema completo de base de datos]] como referencias y definir entregas pequeñas; 95 tablas no equivalen a funciones terminadas. |
| 011 | **Logística nacional con restricciones reales** | Varias bodegas/flotas, regiones, abastecimiento entre centros y restricciones reales de transporte; carga y recuperación acordes al volumen contratado. | B-25/27 y P2 de [[Alcance y prioridades]]. Activarlo cuando el piloto justifique esa escala; la simulación nacional actual continúa siendo evidencia sintética. |
| 012 | **Calidad del pronóstico con datos reales** | Calidad/frescura de captura, medición de error, estacionalidad y backtesting frente a la media móvil. | B-26 y [[Inventario y prediccion]]. Calibrar primero la base existente; incorporar modelos adicionales solo si mejoran un caso comprobado. |

No agrego specs de ERP, cotización del Reto 2, marketplace, blockchain, sensores, app nativa o varios agentes: [[Alcance y prioridades]] y D3 mantienen esos límites. Chat libre, push y escalado multinodo tampoco son prerrequisitos del piloto pequeño aquí propuesto.

## Recomendación de trabajo

Terminar e integrar 001; completar 002 y avanzar 004 y la parte de simulación de 005; incorporar 003 y el panel cuando sus contratos estén disponibles; preparar 006 en paralelo y cerrarlo con evidencia. Resolver las ocho brechas anteriores dentro de sus documentos dueños. Después de la hackathon, documentar 007–009 solo si se activa un piloto y mantener 010–012 fuera de ese primer compromiso.

El proyecto necesita un final acotado y comprobable. Para esta estimación ese final son los seis specs existentes, sus dependencias operativas aplicables y el recorte explícitamente registrado, no todas las posibilidades contenidas en el diseño P2.
