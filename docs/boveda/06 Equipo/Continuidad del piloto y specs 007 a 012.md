---
tipo: plan-producto
estado: propuesta
actualizado: 2026-10-12
tags: [producto, piloto, specs, continuidad]
---

# Continuidad del piloto y specs 007 a 012

Planificación solicitada por Bryan el 12-oct para continuar después de comprobar el núcleo 001–006. Las intenciones de estas entregas están resumidas en [[Analisis de cobertura de specs]]; esta nota define **entrada, primera entrega, prueba de salida y seguimiento**. Los IDs 007–012 son propuestos: sus carpetas y specs detallados todavía no se han creado.

## Punto de entrada

Usar el pase técnico de [[Seguimiento de implementacion y pruebas]] y su evidencia en [[Registro de seguimiento y hallazgos]]. Se puede preparar documentación futura mientras se corrige el núcleo; comenzar nuevas funcionalidades sin cerrar sus defectos esenciales dificulta localizar regresiones.

Los resultados del evento que faltan en 006 permanecen pendientes hasta ocurrir. Esa distinción permite planificar continuidad sin declarar completa la hackathon. Conservar la versión de demo y el congelamiento ya documentado; las nuevas funcionalidades no se incorporan automáticamente a lo que se ensayará el 16–17.

Para una primera empresa, la ruta propuesta es **007 + operación mínima de 008 → 009**. 010–012 se preparan como evolución por necesidades verificadas. Los números organizan temas; no obligan a implementar todo el diseño amplio antes de un piloto pequeño ni prohíben adelantar 012 si los datos justifican esa mejora.

## Cómo convertir cada entrega en un spec implementable

1. Registrar problema concreto, usuario, dato disponible, alcance mínimo y límite con las notas fuente enlazadas.
2. Redactar `spec.md` según [la plantilla](../../specs/_plantilla/spec.md), con aceptación observable y contradicciones expresas.
3. Revisar con una persona distinta de quien lo redactó y registrar quién decide las reglas que aún falten. La instrucción de continuar esta planificación no proporciona datos empresariales ni umbrales todavía desconocidos.
4. Preparar `plan.md`, un único `task.md` y `test-e2e.md` para la primera entrega concreta. Mantener contratos y migraciones compatibles con lo existente; no crear todos los módulos/tablas por anticipado.
5. Implementar cuando corresponda con sus pruebas, integrar, repetir el tramo afectado del núcleo y registrar evidencia. Aplicar prelaunch para el entorno real que se ensaye.
6. Evaluar si esa entrega resuelve el problema antes de activar la siguiente. Registrar lo diferido y el costo/esfuerzo observado sin estimaciones inventadas.

## 007 — Inventario cotidiano y conciliación

**Entrada:** núcleo técnico probado y acuerdo sobre quién registra consumos/entradas, con qué frecuencia y qué dato tiene la empresa. Fuentes: [[Usuarios y flujos]], [[Contratos API y eventos]], [[Inventario y prediccion]] y RF-02/15.

**Primera entrega sugerida:** un punto/SKU puede registrar consumo operativo por lote, consultar su movimiento/saldo y ver cómo cambia el riesgo. Separar consumo operativo de la serie histórica importada: importar historia no vuelve a descontar stock. Reusar unidades, idempotencia y autorización existentes.

Después incorporar entradas de bodega y solicitudes de reposición; luego ajustes justificados, bloqueo/vencimiento e investigación de diferencias. Definir qué se cuenta como anomalía y quién puede confirmar merma. La devolución física que 004 ya exige sigue siendo responsabilidad de 004, aunque 007 la muestre en conciliación.

**Prueba de salida:** registro autorizado → un movimiento → saldo conciliable → riesgo actualizado; reintento no duplica, consumo excesivo/ajeno se rechaza y una corrección conserva historia. Para anomalías, alerta e investigación/cierre humano quedan diferenciados según V-19.

**Pendientes antes del detalle:** unidad de captura, lote conocido o regla declarada de imputación, demanda solicitada/no atendida y frescura. Definir con la contraparte; evitar fórmulas o automatismos que aparenten conocer datos ausentes.

**Límite:** una empresa/bodega y puntos acordados; sin compras, facturación o contabilidad de ERP. Si RF-15 se mantiene obligatorio en la demo, su entrega mínima debe resolverse en el alcance actual y no esconderse como futuro.

## 008 — Continuidad operativa mínima

**Entrada:** arquitectura efectiva identificada y primer entorno de piloto definido. Los controles existentes de demo ya tienen evidencia; este trabajo amplía continuidad, no posterga la seguridad previa. Fuentes: [[Runbook de DevOps y base de datos para la demo]], [[Entornos y operacion acordados]], [[Verificacion de seguridad antes del lanzamiento]] y V-26/27/28.

**Primera entrega sugerida:** checklist/runbook con respaldo externo privado, una restauración comprobada en una base separada, una persona responsable de incidencias y acceso a logs/estado. Medir qué datos se recuperan, cuánto tarda y qué pérdida temporal se tolera; los objetivos RPO/RTO se acuerdan antes del piloto.

Añadir actualización/rollback compatibles con el esquema, permisos de conexión acotados, altas/bajas de cuentas, alertas útiles y retención de evidencias. Las fotos/firmas y su carga ya pertenecen a 004; aquí se define conservación, acceso, recuperación y retiro según el uso real.

**Prueba de salida:** restaurar una copia, comprobar saldos/recibos/custodia y referencias de archivos, registrar tiempos y probar el procedimiento de recuperación de una versión compatible. El responsable sabe detectar y atender una recepción atascada sin editar saldos manualmente ni borrar capturas.

**Pendientes antes del detalle:** datos/retención autorizados, horario/canal de atención, responsable, entorno y tolerancia a interrupciones. Un backup sin restauración probada queda pendiente.

**Límite:** no requiere pantalla de soporte, servicio 24/7, infraestructura de alta disponibilidad ni duplicar herramientas. Puede entregarse ampliando el runbook existente; el spec 008 detallado solo se abre si hay cambios de software/infraestructura que requieran su propio plan.

## 009 — Piloto real e integración mínima

**Entrada:** contraparte identificada, acuerdo sobre alcance/datos/usuarios, núcleo probado y operación mínima de 008. La ejecución operativa usa 007 o un procedimiento de importación/manual explícitamente acordado que mantenga datos fiables. El 006 entrega la propuesta; no acredita un cliente ni un piloto realizado.

**Primera entrega sugerida:** ficha acordada de una empresa, una bodega y puntos/SKU definidos, baseline previo y responsables. Integrar primero por el importador canónico; añadir un conector únicamente si la fuente real lo necesita.

Seguir las cuatro semanas de [[Modelo de negocio y piloto]]: observar y conciliar; recomendaciones en paralelo; operar grupo pequeño con revisión; comparar y decidir. Mantener aprobación humana al principio y registrar propuestas aceptadas/rechazadas, errores y esfuerzo adicional.

**Prueba de salida:** datos/usuarios reales autorizados, métricas observadas comparables y registro de qué mejoró o empeoró: quiebres, demanda no atendida, transporte y puntualidad, junto a carga operativa/costo del sistema. La contraparte decide continuar, cambiar o parar según umbrales acordados antes de medir.

**Pendientes antes del detalle:** empresa, grupo de puntos, acceso/consentimiento, baseline, metas, responsables, costos y precio. Una cifra sintética del 005 no reemplaza estos acuerdos.

**Límite:** no prometer ahorro ni hacer una integración empresarial general. La expansión requiere evidencia del piloto y conserva los efectos negativos en el informe.

## 010 — Multiempresa y configuración administrable

**Entrada:** necesidad concreta de aislar otra organización o permitir configuración autónoma, y una decisión registrada sobre qué parte de ADR-16 se activa. Fuentes: [[RBAC y configuracion del sistema]], [[Esquema completo de base de datos]], RF-17/18/19 y RNF-13.

**Primera entrega sugerida:** aislamiento comprobado de dos organizaciones sintéticas sobre los módulos realmente usados, con rol de ejecución apropiado, FKs y contexto seguro. Después abrir administración de permisos/ámbitos; parámetros versionados; plantillas/workflows solo cuando un proceso real lo requiera.

**Prueba de salida por entrega:** V-37 para lecturas/escrituras, tablas hijas y pool con contexto vacío/reutilizado; V-34 para delegación sin autoescalación; V-35/36 para versiones, captura histórica y configuración sin apagar invariantes. Conservar compatibilidad de datos y operaciones offline.

**Pendientes antes del detalle:** número de organizaciones, administración delegada y primeros campos/parámetros que justifican configuración. Revisar transición desde `Account.Role` fijo y migraciones con datos existentes.

**Límite:** no restaurar las 95 tablas como objetivo de avance ni bloquear el piloto de una organización por este diseño. El uso de RBAC administrable/RLS comienza aquí cuando se activa; no se exige en la campaña de demo.

## 011 — Logística nacional con restricciones reales

**Entrada:** piloto muestra necesidad de varias bodegas/flotas y aporta capacidades, calendarios, matrices y restricciones reales. Fuentes: [[Rutas y sobrecostos]], P2 de [[Alcance y prioridades]] y B-25/27.

**Primera entrega sugerida:** seleccionar origen y asignar entregas entre las bodegas/vehículos de un caso real acotado, manteniendo reservas, custodia y versiones. Ampliar días/traslados/transferencias solo si forman parte de ese caso.

**Prueba de salida:** demanda, stock, capacidad, ventanas y jornadas factibles o pendientes con motivo; incidente conserva lo ejecutado, cada línea tiene origen/custodia y el benchmark registra volumen, latencia, errores y recuperación bajo la carga pactada. Comparar frente a la política existente con restricciones iguales.

**Pendientes antes del detalle:** cobertura geográfica, volumen, tipos de vehículos/productos, rutas permitidas y nivel de servicio. La simulación de 80 puntos del 005 aporta metodología; no acredita operación real a esa escala.

**Límite:** no introducir Fargate, autoscaling o un solver nuevo por anticipado. Conservar motor y alojamiento existentes mientras el caso y las mediciones los permitan.

## 012 — Calidad del pronóstico con datos reales

**Entrada:** consumo/demanda y frescura suficientes para evaluar una predicción sin información futura. Fuentes: [[Inventario y prediccion]] y B-26.

**Primera entrega sugerida:** medir error de la media móvil actual por punto/SKU y período, identificar ceros/faltantes/demanda censurada y comprobar cobertura de datos. Añadir calibración de ventana, margen o patrón semanal antes de considerar otro modelo.

**Prueba de salida:** backtesting con corte temporal explícito frente a la base determinista, MAE y cobertura, impacto en faltantes/servicio y parámetros/versiones reproducibles. Un método nuevo se incorpora solo si demuestra mejora pertinente y sus límites quedan visibles.

**Pendientes antes del detalle:** historial disponible, periodicidad, criterio de mejora y costos de captura/mantenimiento. Se puede adelantar este trabajo a 010/011 si el piloto ofrece datos y lo prioriza.

**Límite:** no exige ML entrenado, proveedores nuevos ni una promesa de aviso fijo de cinco días para cualquier punto. El ejemplo hospitalario conserva su carácter ilustrativo.

## Seguimiento de continuidad

Completar después del pase técnico; todas las entregas comienzan pendientes. Las estimaciones de tiempo se añaden al conocer alcance, personas y datos, no por el número de spec.

| Entrega | Responsable / revisor | Entrada acreditada | Primer resultado acordado | Estado | Evidencia / próxima revisión |
|---|---|---|---|---|---|
| 007 | PENDIENTE / PENDIENTE | PENDIENTE | PENDIENTE | Planificación propuesta | PENDIENTE |
| 008 mínimo | PENDIENTE / PENDIENTE | PENDIENTE | PENDIENTE | Planificación propuesta; revisar runbook existente | PENDIENTE |
| 009 | PENDIENTE / PENDIENTE | PENDIENTE: contraparte y condiciones | PENDIENTE | Planificación propuesta | PENDIENTE |
| 010 | PENDIENTE / PENDIENTE | PENDIENTE: necesidad multiempresa/configuración | PENDIENTE | P2, por activar | PENDIENTE |
| 011 | PENDIENTE / PENDIENTE | PENDIENTE: caso y restricciones reales | PENDIENTE | P2, por activar | PENDIENTE |
| 012 | PENDIENTE / PENDIENTE | PENDIENTE: datos para evaluación | PENDIENTE | P2, por activar | PENDIENTE |

En cada revisión comprobar: problema resuelto, evidencia, regresión del núcleo, datos pendientes y siguiente entrega más pequeña. La preparación y ejecución comercial del piloto no se confunden con el cierre de la hackathon; ambas quedan trazadas en sus registros.
