---
tipo: nota-evento
estado: pendiente
actualizado: 2026-10-16
tags: [hackathon, evento, notas, logistica]
---

# Operación de bodega, inventario y rutas

- **Registró:** Joseph Baño (`jb`)
- **Registrado:** 2026-10-16 11:02 (America/Guayaquil)
- **Ocurrió:** 2026-10-16; hora exacta PENDIENTE
- **Contexto:** preguntas sobre operación de insumos de limpieza; oradora y empresa PENDIENTE de confirmar
- **Fuente del apunte:** dictado de Joseph; respuestas y ejemplos oídos, sin material de apoyo adjunto

## Apunte transcrito

En logística, los productos llegan en lotes o volúmenes grandes y luego se dividen o porcionan en bodega. Joseph destacó como problema importante el proceso interno de bodega: dosificación, dilución y preparación de lo que se entrega. Surgió la idea de calcular cotizaciones o cantidades según litros de producto, metros cuadrados y uso; Joseph no sabe aún si esto entra en el alcance de IstpetDev.

Preguntaron si existe un lugar específico para esa preparación. Según el apunte, algunos productos se envían en bolsas y la mezcla o dilución puede hacerse en el sitio donde se usan o entregan; falta aclarar qué preparación ocurre en bodega y cuál en el punto de servicio. Se señaló que una entrega hospitalaria no es homogénea: un quirófano y un espacio de neonatos pueden requerir productos o protocolos distintos. No se registraron formulaciones ni instrucciones técnicas de mezcla.

En otra respuesta, la oradora describió como problema grave la variedad de inventario: mencionó aproximadamente **cinco fábricas** y **400 ítems**, múltiples desinfectantes, fragancias y presentaciones. La fábrica entregaría cajas, canecas u otros volúmenes; se dio como ejemplo una presentación de **500 ml en cajas de unas 48 unidades**. Al despachar se abren cajas y se subdivide el inventario; esto complica el registro de entradas y salidas, promociones y cotizaciones. Joseph oyó que un error del comercializador o del cotizador puede afectar el flujo logístico.

La fábrica lleva producto a la bodega. Algunos clientes no programan sus compras, lo que puede desbalancear despachos y rutas. Se habló de resolver la logística por zonas: rutas macro entre provincias y bodegas, y rutas internas dentro de una ciudad. Joseph planteó cómo podría manejar el prototipo estos casos.

## Resumen operativo

Paráfrasis: aparecen dos niveles de operación relacionados, pero distintos: **conversión física y contable** de envases/lotes en bodega o en el punto de uso, y **planificación de transporte** desde fábrica a bodega y desde bodega a cliente. Los pedidos no programados pueden alterar ambos. La nota no demuestra que una sola solución deba cubrir todo el ciclo.

## Hallazgos y certeza

- **Registrado con fuente:** problemas y ejemplos según el relato de Joseph. Las cifras de cinco fábricas, unos 400 ítems y 48 unidades son ejemplos oídos, **no datos verificados** de una empresa identificada.
- **Por validar:** unidad base del inventario, reglas de fraccionamiento, trazabilidad de lote tras abrir una caja, promociones, conversiones entre litros y unidades, y si las diluciones ocurren antes del despacho o en el lugar de limpieza.
- **Por validar:** quién opera las rutas entre provincias y cuáles son las restricciones para entregas dentro de la ciudad; no equiparar automáticamente fábrica→bodega con bodega→punto de servicio.
- **Límite de alcance:** el [[Reto 1 oficial]] pide abastecimiento, rutas y trazabilidad hasta el punto de servicio. Un cotizador por superficie o instrucciones de dilución podrían ser necesidades futuras; no se incorporan al prototipo ni al pitch como capacidad existente por este apunte.

## Acciones y preguntas abiertas

- [ ] Formular un ejemplo de entrada y salida de una caja que se abre: producto, lote, presentación, cantidad inicial, fracciones y destino; pedir validación sectorial con consentimiento. Responsable: equipo; plazo: PENDIENTE.
- [ ] Confirmar si el faltante se calcula en envases, litros concentrados, litros ya diluidos o consumo por superficie en el punto de servicio. Responsable: equipo; plazo: antes de redefinir demanda.
- [ ] Comprobar en el prototipo actual qué parte de fábrica→bodega, bodega→cliente, subdivisión y pedido imprevisto ya tiene evidencia; registrar brechas sin prometer funciones nuevas para mañana. Responsable: equipo; plazo: antes del pitch.
- [ ] Precisar el riesgo concreto de mezclar en el punto de uso y quién está autorizado; esta nota no contiene una pauta de seguridad química. Responsable: equipo; plazo: PENDIENTE.

## Seguimiento

El detalle de preparación y dilución puede informar hipótesis para fases posteriores. Las restricciones de ruta y custodia se comparan con [[Rutas y sobrecostos]], [[Inventario y prediccion]] y [[Contratos API y eventos]] sin tratarlas como implementadas.

## Correcciones posteriores

Ninguna registrada.
