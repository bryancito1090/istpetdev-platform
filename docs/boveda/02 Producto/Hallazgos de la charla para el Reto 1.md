---
tipo: analisis-producto
estado: vigente
actualizado: 2026-10-16
tags: [producto, reto-1, charla, logistica, inventario]
---

# Hallazgos de la charla para el Reto 1

Documento de consulta del equipo para usar las preguntas y respuestas de la charla en el **Reto 1: abastecimiento, inventario, rutas y trazabilidad**. La [[Charla del sector limpieza - transcripcion recibida]] conserva el texto completo aportado por Bryan, con nombres de terceros reservados. [[2026-10-16-1109-bb-charla-limpieza-logistica]] conserva el registro inicial con extractos y contexto. Esta nota reúne las respuestas, su alcance y lo que falta preguntar; la charla no modifica por sí sola el [[Reto 1 oficial]] ni acredita funcionalidades del software.

## Lo más útil de las preguntas finales

**Otra charla complementaria:** [[Logistica integral para el Reto 1 - hallazgos y evidencia]] desarrolla proveedores, inventario confiable, ventanas de acceso, retrocesos, costos, capacitación e integración gradual. Su fuente completa se conserva por separado; no se mezclan los dos testimonios ni sus preguntas finales.

1. **El abastecimiento se organiza por períodos y puntos.** Se describen fichas de abastecimiento y recorridos semanales o quincenales para que el punto tenga producto durante el período. La necesidad a investigar es anticipar cuándo esa programación deja de cubrir la demanda.
2. **Hay tres operaciones que conviene distinguir:** prestador que abastece puntos de limpieza, comercializadora que recibe y fracciona inventario, y fabricante que produce y despacha por rutas. Sus frecuencias, actores y restricciones no son idénticos.
3. **Los pedidos imprevistos alteran producción y transporte.** Un pedido fuera de programa puede obligar a apurar producción y cambiar una ruta costa por una necesidad en el centro. El expositor relata capacidad insuficiente para despachar todos los días o cada dos días.
4. **Una ruta agrupa varios clientes.** Se describen zonas o líneas de entrega, con varios destinos por camión para distribuir el costo del transporte. El ejemplo personal es no salir con menos de 15 entregas; no es un mínimo general del sector.
5. **Peso y volumen importan además del recorrido.** Una ruta corta no basta si no puede transportar la carga o no resulta viable económicamente. Faltan capacidades y costos reales para calibrar el caso empresarial.
6. **La salida de inventario difiere de su empaque de entrada.** Se reciben cajas o canecas y se abren/reagrupan para despachos con presentaciones y promociones. La cantidad física enviada debe poder conciliarse con el inventario, aunque una unidad se facture como bonificada.
7. **Dos preguntas clave quedaron sin respuesta concreta:** cuántos días antes necesitan anticipar el agotamiento y cómo deciden a quién atender/reprogramar ante retraso o cierre vial. La charla describe contexto, pero no permite fijar un lead time ni copiar una política empresarial de prioridad.

## Preguntas, respuestas y vacíos

Las preguntas se parafrasean para facilitar la consulta. «Respondida» significa que hay contenido en la transcripción, no que se verificó la práctica en una empresa.

| Pregunta o asunto planteado | Respuesta registrada | Alcance y pendiente |
|---|---|---|
| ¿Cuáles son los tres dolores de las empresas que contratan limpieza? | El cliente espera que el prestador se haga cargo del área con responsabilidad; se destaca capacitación, dosificación y aplicación del producto | Se desarrolló especialmente un dolor; no hubo lista literal de tres. La síntesis de tres grupos de abajo es interpretación del equipo |
| ¿Hay departamentos, personal o un lugar específico para dosificar? | Se describe envío de producto en bruto y preparación en el espacio asignado al personal, con recipientes y sin dosificador en la operación relatada | No se establece una estructura universal de personal/departamentos. Una palabra de la presentación de entrada es dudosa en el texto |
| ¿Cómo relacionan el producto con el área que se limpia? | No disponen de una cuantificación clara de litros, dilución y m²; se pregunta cuánto cubren 20 litros | No se entrega una fórmula ni rendimiento verificado por producto, superficie o procedimiento |
| ¿Con cuántos días de anticipación necesitan conocer un agotamiento y cómo lo controlan? | La respuesta se centra en diferencias entre áreas hospitalarias, instrucciones por producto y supervisión manual posterior | **Sin número de días ni regla de alerta.** No confundir esta respuesta con validación de los cinco días de nuestro escenario |
| ¿Cómo arman actualmente las rutas? | Para servicios, fichas de abastecimiento y programación semanal/quincenal; para comercialización/fábrica, inventario fraccionado, pedidos imprevistos y rutas por zona | Respuesta descriptiva; faltan calendario de una empresa, ventanas, flota, tiempos y costos |
| ¿Qué hacen si un camión se retrasa o se cierra una vía? ¿A quién atienden primero y a quién reprograman? | Se explica la programación y los problemas de coordinar pedidos y despachos | **Sin protocolo de incidente ni orden de prioridad.** Las reglas del sistema siguen siendo decisiones propias, no respuestas validadas del sector |
| ¿Un mismo transportista puede atender hospitales, tiendas y otros destinos en una salida? | Se responde que las rutas llevan varios clientes simultáneamente; se describen corredores y consolidación de entregas | Confirma el relato multiparada. **No especifica compatibilidad de químicos, segregación, manejo hospitalario ni que cualquier mezcla de carga esté permitida** |
| ¿Qué hace viable económicamente una salida? | El expositor indica un mínimo personal de 15 entregas por camión y menciona aproximadamente USD 1.000 de mercadería | El tonelaje falta y la frase final de rentabilidad es defectuosa. No son parámetros generales ni ahorro medido |

## Las tres operaciones descritas

| Operación | Flujo relatado | Dificultad que aporta al reto |
|---|---|---|
| Prestador de limpieza | Abastecer sus puntos según fichas semanales/quincenales; personal prepara y usa productos en el lugar asignado | Estimar necesidades por punto y actividad, evitar interrupciones y registrar qué se entregó; falta cuantificar consumo por área |
| Comercializadora | Recibir de fabricantes en cajas/presentaciones, abrirlas, formar pedidos y despachar unidades/promociones | Mantener cantidades y trazabilidad entre entrada y salida; gestionar un catálogo amplio y pedidos no programados |
| Fabricante | Producir y despachar hacia comercializadoras/clientes mediante corredores y frecuencias limitadas | Coordinar pedidos imprevistos, producción y disponibilidad de transporte; consolidar entregas con peso y volumen |

La charla da ejemplos de estos flujos, pero no permite atribuirlos todos a una misma empresa ni asumir que IstpetDev ya cubra cada eslabón. El texto oficial del Reto 1 se conserva separado del testimonio.

## Inventario, empaques, promociones y reparto: detalles conservados

| Dato del relato | Qué significa para entender el problema | Precisión pendiente |
|---|---|---|
| Comercialización de cinco fábricas y casi 400 ítems en el centro del país | Diversidad de proveedores y referencias | Identidad empresarial y catálogo permitido; no son datos del fixture |
| Desinfectante en litro, galón, caneca y tanque; 13 fragancias en el ejemplo | La presentación y variante forman parte de la identificación del producto | No multiplicar esas cifras para deducir un catálogo; faltan capacidades y equivalencias de cada presentación |
| Caja de 24 unidades de litro y caja de 6 galones | El empaque de entrada contiene unidades que luego pueden salir por separado | Confirmar unidad base por SKU y tamaño del galón; no usar una conversión universal |
| Presentaciones de 500/350 ml y cajas de hasta 48 unidades | Hay empaques con diferentes factores de conversión | El texto no permite asociar el factor 48 a todas las presentaciones |
| Abrir cajas y armar una orden de salida distinta de la entrada | Se requiere conciliar el contenido de lo preparado con lo recibido y lo que queda | Solicitar un ejemplo completo con producto, lote, cantidad, fracciones y destino |
| Promociones 6+1 y 12+2, descritas como «costo cero» en factura | Las unidades bonificadas forman parte del despacho físico y hoy su control se relata como manual | Aclarar precio facturado, valoración y costo real; la charla no define tratamiento contable |
| Fichas de abastecimiento semanales o quincenales | Existe programación periódica, útil como referencia empresarial para discutir el baseline fijo | El pasaje sobre frecuencia diaria/semanal es confuso; no se infieren porcentajes ni se reparametriza un experimento ya medido |
| Fabricante con despachos semanales o dos veces por semana | La reposición puede depender de calendario y capacidad de transporte, además de demanda | Confirmar cada proveedor y punto; no equivale automáticamente al tiempo total de reposición |
| Pedido no previsto que obliga a apurar producción o cambiar zona de despacho | La planificación debe poder representar una urgencia y sus compromisos | No se dio un algoritmo de prioridad ni un tiempo de respuesta |
| Corredor Quito–Cuenca y recorrido por Santo Domingo, Quevedo y Guaranda | El transporte agrupa destinos por zonas y corredores | Son ejemplos narrados, no geometrías, distancias ni recorridos optimizados comprobados |
| Varios clientes por salida; 15 entregas como mínimo en su caso | La consolidación afecta el costo por entrega | Solicitar costo por viaje, distancias, utilización y política de excepciones; 15 no se convierte en restricción global |
| Peso, volumen y referencia aproximada de USD 1.000 de carga | Conviene estudiar capacidad y viabilidad económica junto con las rutas | Tonelaje no transcrito y rentabilidad sin verificar; no se obtiene margen ni costo/km a partir de esa cifra |

## Tres grupos de dolor — síntesis del equipo

1. **Aplicación correcta y verificable:** capacitar, dosificar y escoger el producto según el área. La supervisión visual después de la cuadrilla no demuestra qué producto ni cantidad se aplicó.
2. **Consumo y cantidades por actividad:** conocer cuánto producto concentrado, mezcla preparada y consumo necesita cada área para abastecer y controlar su uso. La charla no aporta rendimiento en m² ni una receta validada.
3. **Abastecimiento y distribución:** conciliar inventario y presentaciones, responder a pedidos imprevistos y consolidar entregas con restricciones de capacidad y costo.

La pregunta se hizo desde las empresas que **contratan** limpieza; la respuesta describe principalmente problemas del **prestador** y después de comercializadoras/fabricantes. No son tres testimonios independientes ni una entrevista directa a compradores. [[Historia y propuesta de valor]] recoge esta distinción.

## Uso de los hallazgos en nuestro trabajo

| Frente | Uso documental para IstpetDev | Límite |
|---|---|---|
| Inventario y demanda | Revisar unidades base, empaques, registros de consumo y frescura del saldo; preparar un caso de caja abierta y despacho con bonificación | Las conversiones y el rendimiento deben provenir de datos comprobables; ver [[Inventario y prediccion]] |
| Reabastecimiento | Contrastar riesgo y cantidad sugerida con fichas periódicas, consumo y disponibilidad del proveedor | Lead time y margen siguen por validar con una empresa; la charla no fija cinco días |
| Rutas y contingencias | Modelar destinos consolidados, zona, peso, volumen, ventanas y pedidos fuera de programa; demostrar qué cambia al aparecer un incidente | La política de prioridad/reprogramación del equipo debe presentarse como propia; ver [[Rutas y sobrecostos]] |
| Trazabilidad | Preparar evidencia de cantidades y responsables desde bodega hasta recepción, incluyendo unidades bonificadas en el caso por validar | La entrega del insumo no verifica su posterior dosificación ni eficacia sanitaria |
| Impacto | Comparar rutas y políticas con datos y baselines declarados; usar la charla para motivar el problema | Ningún porcentaje de ahorro ni mejora de nuestra aplicación fue medido en la charla |
| Evolución de producto | Evaluar posteriormente dosificador, protocolos por área, cotizador por m² o reglas de promociones | Registrar una necesidad no implementa una función ni amplía automáticamente los specs |

## Preguntas que conviene retomar con el sector

- ¿Cuántos días/horas antes necesitan la alerta en cada punto y producto? ¿Cuánto tarda realmente la reposición por proveedor y zona?
- Ante retraso o cierre, ¿qué criterio prima: interrupción, criticidad del área, ventana, compromiso contractual o costo? ¿Quién aprueba dejar un destino pendiente?
- ¿Qué dato consideran consumo: envase, concentrado, mezcla preparada o aplicación por m²? ¿Dónde lo registran?
- ¿Cómo concilian un lote recibido en cajas con unidades sueltas, bonificadas, rechazadas o devueltas?
- ¿Qué restricciones hay para transportar juntos productos y entregas hospitalarias/comerciales? ¿Qué capacidades y ventanas tiene cada vehículo/punto?
- ¿El mínimo de 15 entregas admite emergencias? ¿Qué costo por salida y qué capacidad permitirían comprobar el ejemplo de USD 1.000?

Responsables y fecha de las siguientes consultas: PENDIENTES. Las respuestas nuevas se registran con su fuente; no se completan por inferencia.

## Cobertura del resto de la charla

El texto completo también conserva las preguntas sobre prototipo físico/idea, pitch, cubículos, jurados, HDMI, acceso de barras, Drive, entrega a las 18:00, la mención no ratificada del sábado 20:00, conectividad y servicios en sede. Su seguimiento operativo está en [[Consultas para la organizacion]], [[Cronograma oficial]], [[Evaluacion y entregables]] y [[Guion del pitch]]. No se mezclan esos avisos de organización con evidencia empresarial del Reto 1.
