---
tipo: analisis-producto
estado: vigente
actualizado: 2026-10-16
tags: [producto, reto-1, charla, logistica, validacion]
---

# Logística integral para el Reto 1 — hallazgos y evidencia

**Fuente:** [[Charla de logistica integral - transcripcion recibida]], aportada por Bryan el 16-oct-2026 y señalada por él como la charla más valiosa que tuvieron para el Reto 1. Esta nota organiza todo su contenido útil; las secciones de aplicación y evidencia son **interpretaciones y propuestas del equipo**, pendientes de comprobar en software y operación. Expositor, empresa, título oficial y fecha/hora de exposición: **PENDIENTES**. No se atribuye esta intervención al mentor asignado por coincidencia temática.

Se complementa con [[Hallazgos de la charla para el Reto 1]], que corresponde a **otra intervención**. El [[Reto 1 oficial]] sigue siendo la fuente del requisito: rutas, reabastecimiento crítico y trazabilidad desde almacén central hasta entrega. Ampliar el análisis a proveedores ayuda a comprender las causas; no incorpora automáticamente compras, contratos, nuevas integraciones ni gestión de flota al alcance aprobado.

## Idea central y causas del problema

La exposición relaciona la **logística fragmentada** con sobrecostos, distribución con errores, entregas interrumpidas y menor eficiencia y satisfacción del cliente. Propone observar la cadena completa: **proveedor o producción propia → almacén/centro de distribución → transporte → punto de servicio/cliente**.

| Frente de la charla | Causa descrita | Consecuencia descrita |
|---|---|---|
| Almacén | Esperar el pedido sin conocer necesidades, consumo y movimientos; requisiciones incompletas y poca visibilidad | Faltantes, inventario insuficiente y abastecimiento tardío |
| Transporte | Cronograma inadecuado, personal sin capacitación, flota no apropiada; ignorar distancias, capacidad, horarios y acceso | Esperas de vehículo y personal, retrasos, combustible y horas extras sin imputar |
| Punto de servicio | Enviar cantidad o producto inadecuado, llegar fuera de ventana, sobrecargar y dañar empaques | Quiebre o sobrestock, merma, producto no conforme, reentrega e insatisfacción |

El objetivo expuesto es **anticipar la necesidad del cliente**, en vez de depender únicamente de lo que pida. Entregar antes o más cantidad no demuestra por sí solo mejor servicio: puede producir sobrestock o una recepción inviable. La charla relaciona servicio con fidelización y recomendación; no aporta una medición de esa relación ni de satisfacción de nuestros usuarios.

## Los cuatro componentes de una operación visible

La transcripción salta en la numeración; se agrupan aquí por tema sin reconstruir diapositivas ausentes.

1. **Logística e inventarios desde el proveedor.** Acordar día, hora, especificaciones, características, empaque y unidad de recepción. Un incumplimiento al ingresar a bodega puede retrasar toda la distribución. Seleccionar/calificar proveedores también por su capacidad para adaptarse a la operación.
2. **Rutas según demanda y restricciones.** Dividir en norte, sur y centro no demuestra factibilidad. Considerar tráfico, capacidad, ventanas y acceso antes de asignar toda una zona a un vehículo; contabilizar combustible, horas extras y tiempos muertos.
3. **Reabastecimiento anticipado.** Usar consumo, movimientos, históricos y conocimiento del punto para prever faltantes en el cliente y en el almacén central, incluidos pedidos fuera de programa. No se proporciona un algoritmo, precisión, horizonte ni lead time calibrado.
4. **Trazabilidad de origen a destino.** Conocer salida, ruta, entrega programada y recepción para contrastar puntualidad, cantidades completas y calidad del servicio. La exposición pide visibilidad del conjunto de la cadena, además del recorrido del camión.

### Negociar presentaciones también cambia la logística

El ejemplo del proveedor plantea adaptar cajas/presentaciones para reducir dificultades de manipulación, peso, espacio y abastecimiento. Se mencionan «12 unidades por galón», «12 litros», «6 litros» y «45 por 60», sin consistencia suficiente para convertirlas en una ficha técnica. **No son equivalencias de unidad, dimensiones completas ni límites ergonómicos verificados.**

Para aplicarlo hacen falta catálogo uniforme, unidad base, conversión validada, peso, volumen, presentación y condiciones de manipulación. Comercial y operación deben compartir esa información para saber qué pueden ofrecer y su costo. Ver [[Inventario y prediccion]]; la charla no sustituye las especificaciones del producto ni aporta normas de seguridad de carga.

### Caso de la ventana de un centro comercial

El expositor usa una recepción **07:00–09:30** para explicar que la secuencia puede necesitar atender primero un destino con ventana estrecha, aunque otro esté más cerca de la bodega. Atender primero el cercano puede hacer perder el ingreso al centro comercial, obligando a regresar y rehacer el recorrido con tiempo y kilómetros adicionales.

**Lectura para el equipo:** el vehículo parte de su depósito real; lo que cambia es el orden de atención. La ventana es un ejemplo oral, no un horario general de centros comerciales ni una regla que obligue siempre a visitar primero el más restringido. Hay que evaluar conjuntamente viaje, servicio, capacidad, acceso y jornada. Ver [[Rutas y sobrecostos]].

## Seis palancas de mejora descritas

| Palanca | Recomendación del expositor | Aplicación que conviene evaluar |
|---|---|---|
| Rutas eficientes | Mapear secuencia, franjas, kilómetros por flota y costo por pedido | Comparar orden geográfico con secuencia factible; mostrar pendientes y motivos |
| Inventario en tiempo real | Actualizar saldo con cada descarga y con las mermas | Registrar movimientos y frescura; conciliar físico/digital. Sin señal, distinguir captura local y confirmación del servidor |
| Pronósticos | Usar datos históricos para anticipar demanda | Mostrar origen, corte temporal, calidad e incertidumbre; validar fuera del período usado para estimar |
| Trazabilidad origen–destino | Conocer salida, ruta y entrega prevista, incluso mediante plataformas del transportista | Evaluar acceso a datos del proveedor durante la negociación; no asumir que ya existe una integración o GPS en vivo |
| Alertas tempranas | Conocer novedades pronto y trabajar preventivamente | Distinguir riesgo anticipado de incidente ya ocurrido; medir tiempo disponible para decidir y capacidad real de actuar |
| Cobertura multipunto | Usar subcentros ante tráfico, bloqueos o dificultad de cruzar la ciudad; ejemplo de bodega en el valle y acceso al sur | Comparar alternativas con stock, flota, acceso y costo reales; enlazar el diseño multialmacén de ADR-28 sin dar por acreditada su ejecución |

La exposición presenta la cobertura multipunto principalmente para empresas medianas y grandes, y los principios generales como escalables a pequeñas, medianas y grandes. Ninguna palanca prueba por sí sola ausencia de quiebres: dependen de disponibilidad, capacidad, datos y acceso.

## Indicadores y costos que no deben quedar ocultos

El expositor pide medir costo por kilómetro, tiempos de recorrido, faltantes, costo de retroceso, exactitud, mermas y servicio. **No entrega fórmulas completas ni resultados numéricos.** «Costo negativo» se interpreta aquí como costo no identificado que perjudica el margen, no como importe contable menor que cero. «Retroceso» se usa en el relato para el viaje/corrección necesario para cubrir un faltante o error; debe aclararse su definición empresarial.

| Indicador mencionado | Dato que debe recogerse para poder evaluarlo | Límite de la fuente |
|---|---|---|
| Costo por km | Kilómetros, período, vehículo y componentes incluidos; tarifa o consumo con fuente | No se conocen costos reales ni fórmula de ajuste de combustible |
| Tiempo y costo de ruta | Salida/llegada, conducción, servicio, espera, personal, combustible y recargos | No confundir estimación con viaje medido; evitar doble conteo de personal o combustible |
| Faltantes | Producto, cantidad pedida/atendida, punto, fecha y demanda no cubierta | Un faltante no es necesariamente merma; distinguir falta en bodega y en cliente |
| Retroceso/reentrega | Incidente de origen, km, tiempo y costo adicional atribuible a la corrección | No sumar otra vez el mismo tramo incluido en el costo total |
| Exactitud | Comparación física/digital y pedido/despacho/recepción, con unidad y denominador declarados | La charla no precisa a cuál exactitud se refiere ni su fórmula |
| Merma/no conformidad | Rotura de químicos/empaque, producto dañado o distinto a lo solicitado, cantidad, valor y evidencia | Una alerta no confirma pérdida; hace falta conciliación y cierre responsable |
| Servicio | Entrega completa y a tiempo, continuidad y, si se obtiene, percepción del cliente | Puntualidad o trazabilidad no sustituyen una medición de satisfacción/fidelización |

Las fórmulas vigentes siguen en [[Rutas y sobrecostos]] y [[Simulador y metricas]]. Los importes sintéticos del proyecto no se convierten en tarifas empresariales por esta charla.

## Qué cuenta como evidencia según la charla

Se proponen tres comprobaciones: **detectar faltantes, comparar escenarios y rastrear de extremo a extremo**. El expositor pide separar beneficios declarados de resultados demostrados, verificar un prototipo funcional/MVP, usar inventarios y condiciones reales cuando estén disponibles y producir indicadores comprobables que entienda todo el equipo, incluida la persona que estiba.

Los siguientes son **casos propuestos a partir de la charla**, sin ejecución acreditada en esta tarea. Se vinculan a los criterios existentes de [[Plan de validacion]]; no redefinen sus IDs ni certifican cobertura actual del software.

| Caso propuesto | Evidencia que permitiría evaluar la recomendación | Criterios existentes / pendiente |
|---|---|---|
| Necesidad antes del pedido | Histórico y saldo al corte, alerta explicable y posibilidad real de reponer; incluir agotamiento en bodega | RF-03; V-03/V-04. Lead time y horizonte empresariales pendientes |
| Centro comercial con ventana 07:00–09:30 | Mismas paradas y recursos, dos secuencias, ETA, espera, factibilidad y costo; declarar ventana ilustrativa | RF-04/08; V-05/V-18 |
| Vehículo incompatible o ruta sobrecargada | Capacidad/acceso de fuente conocida, descarte o pendiente con motivo, demanda restante visible | RF-04; V-05. Restricciones urbanas reales pendientes |
| Conteo físico y merma | Ítems elegidos, saldo digital fechado, conteo, diferencia, evidencia y ajuste responsable | RF-02/15; V-01/V-19 |
| Entrega con faltante y corrección | Cantidades despachadas/recibidas, actores y tiempos, incidente, reentrega y costo adicional sin duplicación | RF-05/06; V-07/V-09; contabilidad de retroceso pendiente |
| Bloqueo y alternativa de subcentro | Misma demanda, stock y flota por bodega, estado de acceso, alternativa viable o causa de imposibilidad | RF-04/11; V-05/V-06/V-18; contraste con ADR-28 |
| Operario interpreta el flujo | Persona de operación explica alerta, preparación/estiba, ruta y recepción; registrar dudas y cambios | RNF-09; V-20. Sesión y responsable pendientes |

Para cualquier resultado: versión, entorno, dataset, fuente, supuestos, baseline, acciones, resultado observado y límites. Un ensayo sintético solo respalda ese escenario. La charla aporta contexto operativo, no una prueba de nuestra aplicación ni una encuesta representativa. No se ha levantado, ensayado ni desplegado una versión por este trabajo documental.

## Riesgos y mitigaciones operativas de la exposición

| Riesgo | Mitigación descrita | Seguimiento para el equipo |
|---|---|---|
| Catálogos o registros inconsistentes | Estandarizar catálogo, depurar registros y mantener evidencia técnica de cada acción | Validar unidad, presentación y responsables; preservar trazabilidad de correcciones |
| Inventario digital que no refleja lo físico | Conteos físicos de validación por muestreo de uno o varios ítems | No asumir exactitud por tener un dashboard; registrar muestra, fecha y diferencia |
| Resistencia al cambio | Capacitar, involucrar operación, simplificar procesos y comunicar objetivos en cascada | Evaluar comprensión y carga operativa con usuarios; no solo con quien dirige |
| Rutas poco realistas | Validar restricciones, capacidad, tráfico y condiciones de acceso antes de ejecutar | Distinguir matriz estimada, dato antiguo y tráfico actual; no inventar condiciones |
| Sistemas aislados | Definir interfaces, responsables de datos y plan gradual por operación/cliente | Levantar fuentes, permisos, periodicidad y propiedad del dato antes de comprometer integración |
| Proveedor que incumple | Negociar horarios, empaque, especificaciones y apertura al requerimiento | Medir cumplimiento y su efecto en la salida; gestión contractual fuera del alcance automático |
| Pronósticos imprecisos (solo en diapositiva) | Iniciar con modelos de referencia, medir el error y actualizar los parámetros periódicamente | Coincide con nuestro cálculo determinista comparado con baselines; falta medir el error del pronóstico con datos fuera del período usado |
| Costos de implementación superiores a lo previsto (solo en diapositiva) | Comenzar con un MVP y priorizar funcionalidades que demuestren beneficios medibles | Presentar el prototipo como MVP y ligar cada pantalla a uno de los seis indicadores de la lámina de evidencia |

Las dos últimas filas y el objetivo «insumos adecuados, en las cantidades necesarias y en el momento oportuno» provienen de las diapositivas fotografiadas, no de la transcripción oral. Ver la sección «Diapositivas fotografiadas» de [[Charla de logistica integral - transcripcion recibida]]. El error de rotulado o identificación de empaque también aparece solo en pantalla, como problema del punto de servicio.

## Preguntas finales: combustible y flota

**Combustible y márgenes.** Una persona pregunta por fluctuaciones del combustible y atribuye una regla al «decreto 444». La identificación, contenido y vigencia normativa **no se verificaron**. El expositor responde desde su operación: negociar sobre un costo promedio, consolidar mayor capacidad de entrega en menos rutas, usar logística contratada y propia, y migrar parte de la flota a eléctrica. Menciona logística inversa, pero la interrupción del texto impide reconstruir una política concreta. Se alude a una fórmula de ajuste por combustible **sin proporcionarla**.

El «90%» de flota diésel es una afirmación oral sin fuente ni población definida. No se usará como estadística nacional ni parámetro del motor. Para una futura comparación hacen falta tarifa fechada, componentes, consumo, utilización, contrato, regla de revisión y efecto observado sobre margen. El costo promedio es una práctica relatada; no demuestra que elimine el riesgo.

**Tipos de flota, eléctricos y mantenimiento.** La pregunta incluye automóviles, motos, camiones de distintas capacidades y mantenimiento. La respuesta menciona camiones, camionetas y una tercera categoría transcrita como «bancos», **PENDIENTE de aclarar**, y selección según restricciones de ciudad/cliente. Relata un plan de al menos **dos unidades 100% eléctricas a finales de «este año»** y una autonomía de **300 km**. Año de referencia, modelos, carga, condiciones de autonomía y ejecución del plan: **PENDIENTES**; no son características de nuestra flota ni una compra confirmada. La pregunta de mantenimiento no obtuvo un procedimiento, frecuencia ni costos concretos.

## Consultas y decisiones que siguen abiertas

Todos estos puntos quedan **PENDIENTES**, con responsable y fecha de contraste por asignar. Se conservan para la siguiente conversación sectorial; no bloquean guardar la fuente ni autorizan cambios de alcance.

- Identificar la intervención: título, expositor/organización, momento y, si existe acceso autorizado, diapositivas o audio para aclarar pasajes.
- Precisar necesidades por punto/SKU, mínimos/máximos, horizonte útil de alerta, lead time y frescura aceptable del inventario; distinguir almacén y cliente.
- Confirmar presentaciones, unidades, conversiones, peso, volumen y restricciones de manipulación; aclarar el ejemplo de cajas y dimensiones.
- Obtener ventanas y restricciones por punto/vehículo y su fuente vigente en Quito, Guayaquil y Cuenca; cuantificar tiempos de servicio y esperas. No cargar el ejemplo 07:00–09:30 como restricción universal.
- Aclarar quién prioriza y aprueba dejar una entrega pendiente; la charla recomienda prevención pero no fija esa política empresarial.
- Definir costo/km y costo/hora, componentes incluidos, período de tarifas, ajuste de combustible, criterio de retroceso, exactitud, merma y servicio; conseguir registros para calibrar.
- Precisar logística inversa, tercer tipo de vehículo, mantenimiento y condiciones de flota eléctrica; separar plan de implementación real.
- Identificar plataformas de transportistas disponibles, interfaces, permisos y responsables; acordar integración gradual, sin prometer acceso aún.
- Identificar una persona de operación para revisar prototipo, conteo físico y trazabilidad; aplicar los permisos de [[Registro de mentorias]] si se realizan entrevistas.

## Uso en producto y presentación

La charla da un hilo para explicar el Reto 1: **necesidad visible → alerta anticipada → ruta factible → entrega conciliada → efecto medible en continuidad y costo**. Los registros confiables, capacitación y responsabilidad sobre datos sostienen ese flujo. Los proveedores y subcentros aportan contexto de dependencia y contingencia.

En el pitch, atribuir estas recomendaciones a la charla y mostrar por separado la evidencia propia. No afirmar ahorros, ausencia de quiebres, satisfacción, integración con transportistas, telemetría ni electrificación sin comprobación. Las propuestas se contrastarán con [[Requisitos y aceptacion]], [[Alcance y prioridades]] y los specs antes de cambiar contratos o prioridades.
