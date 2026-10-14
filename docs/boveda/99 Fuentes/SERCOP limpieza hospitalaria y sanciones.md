---
tipo: fuente-resumida
estado: vigente
actualizado: 2026-10-14
tags: [fuentes, sercop, limpieza-hospitalaria]
---

# SERCOP: limpieza hospitalaria y sanciones

**Consulta:** 12-oct-2026. Investigación documental para el kit del spec 006. Se buscaron pliegos de servicio de limpieza hospitalaria con sanción por falta de insumos; las fuentes primarias recuperables y pertinentes fueron fichas técnicas históricas enlazadas por SERCOP. No se verificó un pliego particular que cuantifique un quiebre de desinfectante. Las fichas no son contratos de un cliente de IstpetDev ni multas efectivamente cobradas.

| Fuente primaria | Hallazgo verificable | Límite para el pitch |
|---|---|---|
| [SERCOP, ficha técnica Hospital General Docente de Calderón](https://portal.compraspublicas.gob.ec/sercop/wp-content/uploads/2021/03/ficha_limpieza_hgdc.pdf), atributo de multas y sanciones | Entre las infracciones figura «Utilizar materiales o insumos, productos químicos de limpieza y desinfección de mala calidad, caducados o de características diferentes a las establecidas por la Entidad Hospitalaria»; contempla medidas de rectificación y posible exclusión del catálogo. | Trata calidad y ejecución del servicio; no cuantifica el costo de quedarse sin desinfectante. |
| [SERCOP, ficha técnica Hospital General Enrique Garcés](https://portal.compraspublicas.gob.ec/sercop/wp-content/uploads/2017/04/Ficha_limpieza_Hospital_General_Enrique_Garces_pb_2.pdf), atributo 19 | Exige provisión de productos químicos y describe multas por ciertos incumplimientos del servicio. | La base y la infracción dependen de esa orden de compra; no se puede trasladar una tasa a otro contrato. |
| Ambas fichas, obligaciones del proveedor (HGEG cláusulas 19 y 24; HGDC sección equivalente) | «El proveedor se obliga a proveer del número mínimo de maquinarias, equipos, productos químicos de limpieza y desinfección y herramientas establecidos por área hospitalaria»; el servicio incluye insumos fungibles (papel higiénico, fundas, toallas de papel y gel desinfectante) con «frecuencias de cambio o colocación» definidas por el hospital. | Respalda que mantener insumos es una **obligación contractual del proveedor de limpieza** en hospitales públicos. No cuantifica cuánto cuesta incumplirla. |
| HGEG, atributo 19 (tabla de infracciones) | Las sanciones se expresan como fracciones diarias del monto de la orden de compra, por ejemplo «Multa de 1 x 1000 diario del monto total de la orden de compra mientras persistan las condiciones de la infracción»; entre las infracciones figura el uso de productos químicos de mala calidad, caducados o distintos a los establecidos. | La ficha no asigna una tasa específica al desabastecimiento de insumos; no se usa como «costo de un quiebre». |
| [SERCOP, servicio de limpieza hospitalaria](https://portal.compraspublicas.gob.ec/sercop/servicio-de-limpieza-hospitalaria/) | Enlaza fichas y pliegos del catálogo para limpieza hospitalaria. | Página de entrada; no aporta frecuencia observada de quiebres ni pérdidas monetarias. |

**Verificación del 12-oct (revisión del spec 006):** las tres URL responden y el texto citado se comprobó en los PDF descargados.

**Verificación del 14-oct (kit T1 del spec 006, hecha por IA):** las tres URL respondieron HTTP 200 (dos `application/pdf`, una HTML que sigue enlazando fichas técnicas de limpieza hospitalaria). En el texto de **ambos** PDF aparecen literalmente «se obliga a proveer del número mínimo de maquinarias», «frecuencias de cambio o colocación», «gel desinfectante», «1 x 1000 diario del monto total de la orden de compra» y la infracción citada arriba sobre productos «de mala calidad, caducados o de características diferentes». Corrección: la fila de HGDC parafraseaba esa infracción; ahora la cita textualmente, y se precisa que figura en las dos fichas. No se descargaron otros pliegos.

**Comprobación humana en 2 minutos:** abrir cada PDF y buscar (Ctrl+F) «se obliga a proveer» y «frecuencias de cambio». Si una URL deja de responder, la afirmación usable se retira del pitch hasta conservar copia local fechada.

**Afirmación usable en el pitch:** «En contratos públicos de limpieza hospitalaria, el proveedor está obligado a mantener los químicos de desinfección y los insumos fungibles con frecuencias de reposición definidas por el hospital (fichas técnicas de SERCOP)». Es una afirmación documental, no una cifra.

**Conclusión:** hay sustento documental para preguntar por obligaciones y consecuencias contractuales. No hay dato útil para afirmar «un quiebre cuesta X» o «IstpetDev evita multas». Cualquier cifra contractual concreta queda fuera del pitch hasta contar con el pliego del posible piloto, interpretación responsable y una ejecución enlazada que permita modelar el caso. La investigación no sustituye una entrevista ni valida disposición de pago.
