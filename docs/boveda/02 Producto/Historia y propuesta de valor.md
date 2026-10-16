---
tipo: especificacion-producto
estado: propuesta
actualizado: 2026-10-16
tags: [producto, reto-1]
---

# Historia y propuesta de valor

## Problema y protagonista

Una empresa de limpieza necesita abastecer insumos a muchos puntos con consumo, acceso y horarios diferentes. Su planificador dispone de información tardía; bodega pierde visibilidad después del despacho; el conductor resuelve imprevistos por canales separados; el cliente recibe sin una cadena de evidencia fácil de consultar.

El escenario de **80 puntos** y un hospital sin desinfectante proviene de [[Informacion inicial del equipo]]. Debe presentarse como ilustración hasta que una empresa lo valide.

## Necesidades recogidas en la charla — 16-oct-2026

**Documento de consulta completo:** [[Hallazgos de la charla para el Reto 1]]. Contiene las preguntas y respuestas, los tres tipos de operación y el detalle de empaques, promociones y rutas de las últimas intervenciones; enlaza la [[Charla del sector limpieza - transcripcion recibida|transcripción completa recibida]].

Fuente: [[2026-10-16-1109-bb-charla-limpieza-logistica|charla sectorial aportada por Bryan]]. **Síntesis interpretativa de tres dolores**, no lista literal del expositor ni entrevista al comprador. La pregunta se refiere a empresas que contratan limpieza; la respuesta procede de la perspectiva del prestador y después distingue comercializadoras y fabricantes.

| Dolor | Necesidad que expresa el testimonio | Relación con IstpetDev |
|---|---|---|
| Aplicación correcta y verificable | Capacitación, producto y dosificación según área; la supervisión visual posterior no comprueba qué se aplicó | La custodia hasta recepción aporta evidencia logística; verificar dosificación y eficacia requiere otro alcance |
| Consumo y cantidades por área | Relacionar litros, dilución y m² para saber cuánto suministrar y comprobar su uso | Contrastar los consumos declarados con datos reales; la plataforma no obtiene rendimiento por superficie sin medición y ficha técnica |
| Abastecimiento y transporte viables | Conciliar empaques/unidades y promociones, pedidos no programados y rutas consolidadas por zona | Es el vínculo directo con el Reto 1: inventario, reposición, capacidad, ventanas y trazabilidad; no acredita impacto todavía |

El abastecimiento semanal/quincenal es una práctica relatada que permite discutir el baseline de calendario fijo; no valida exactamente sus parámetros. Las casi 400 referencias, cinco fábricas, promociones 6+1/12+2 y mínimo personal de 15 entregas por camión son ejemplos del expositor, no datos del escenario ni límites globales. La cifra aproximada de USD 1.000 y la capacidad del camión requieren aclaración en la fuente.

**Límites del hallazgo:** no se obtuvo un número de días de anticipación ni una regla de prioridad ante cierre vial. No se validaron los 80 puntos, el caso concreto del hospital, ahorro, disposición de pago ni aceptación del producto. Dosificador, protocolos químicos, conversión a m² y facturación de promociones quedan como necesidades por evaluar, sin ampliar el alcance implementado.

## Hilo conductor

> Una empresa atiende 80 puntos. Hoy se entera de que falta desinfectante cuando el hospital llama. Con nuestra plataforma identifica el riesgo con anticipación, conoce los datos que lo explican, prepara una ruta viable y sigue el insumo desde bodega hasta recepción.

La frase «cinco días antes» solo se usa si el escenario demostrado realmente entrega esa anticipación. No confundir cobertura de cinco días con mejora de cinco días respecto del proceso actual.

## Propuesta de valor

**Para operaciones de empresas de limpieza**, centralizamos inventario, riesgo de faltantes, decisiones de abastecimiento y custodia. Cada alerta propone una acción comprobable y cada entrega deja evidencia, incluso cuando la captura ocurre sin señal.

| Usuario      | Dolor                        | Resultado esperado                            |
| ------------ | ---------------------------- | --------------------------------------------- |
| Planificador | Rutas fijas y urgencias      | Prioridad y ruta justificadas                 |
| Bodega       | Despachos sin conciliación   | Cantidades, lotes y responsable identificados |
| Conductor    | Falta de señal e incidentes  | Trabajo local y sincronización recuperable    |
| Receptor     | Entrega difícil de verificar | Consulta y aceptación de lo recibido          |
| Gerencia     | Sobrecostos sin explicación  | KPIs con origen y comparación verificable     |

## Diferenciación defendible

La propuesta conecta **riesgo → decisión → ruta → custodia → impacto**. Usar IA o QR aisladamente no constituye la diferenciación. La continuidad offline y la explicación del cálculo deben verse en la misma operación.

## Hipótesis para validar

- H-P1: inventario actualizado permite adelantar la planificación respecto de llamadas reactivas.
- H-P2: priorizar por cobertura y restricciones reduce urgencias sin empeorar el servicio.
- H-P3: registrar cantidades y recepción reduce la búsqueda de información al conciliar.
- H-P4: el conductor puede terminar una recepción offline sin asistencia.
- H-P5: la empresa pagaría por evitar interrupciones y medir ahorro.

No son resultados comprobados. Validar con [[Plan de validacion]] y [[Modelo de negocio y piloto]].
