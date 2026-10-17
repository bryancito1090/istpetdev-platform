---
tipo: guia-capacitacion
estado: propuesta
actualizado: 2026-10-16
tags: [capacitacion, roles, receptor, demo-quito, pitch, reto-1]
---

# Hoja de capacitación operativa: Receptor (Cliente y Farmacia)

**Objetivo del rol:** Recibir los insumos en el punto de entrega comprobando que coincidan con lo pedido, registrar cualquier daño o producto equivocado con una foto de respaldo y certificar la entrega en menos de dos minutos.

## 1. Qué aprende
- Escanear el código QR del bulto recibido con la cámara del celular o tableta (o escribir el código del bulto si la cámara no enfoca).
- Comprobar en pantalla la lista de productos, cantidades y números de lote que contiene el cargamento.
- Marcar la conformidad de cada línea con un toque en la casilla de verificación.
- Declarar un rechazo parcial si un producto llega roto, derramado o no corresponde, indicando el motivo y tomando una fotografía de evidencia con la misma cámara.
- Indicar el stock que queda en la percha física antes de recibir y la cantidad sugerida para el siguiente pedido.
- Guardar la recepción para cerrar la cadena de custodia formal.

## 2. Duración
- **20 minutos** de entrenamiento ágil en el área de recepción o farmacia.

## 3. Ejercicio práctico con el escenario Quito (`demo-quito-planificacion`)
- **Punto de recepción:** Hospital General Enrique Garcés (`PTO-0005`, Sur de Quito).
- **Cargamento recibido:** Caja con 10 galones de Hipoclorito de sodio (`PRD-0001`, Lote `L-0001-2609`).
- **Novedad simulada:** Uno de los envases presenta rotura de tapa con derrame de líquido durante el traslado.
- **Paso a paso en la aplicación móvil (módulo de Receptor):**
  1. Abrir la aplicación y verificar que la tarjeta principal indique **Recepción y Validación**.
  2. Pulsar **«Escanear QR con cámara»** y enfocar el rótulo de la caja que entrega el transportista (o ingresar el código en el campo de texto).
  3. Comprobar que en pantalla aparecen el nombre del hospital y el detalle del producto (`PRD-0001`, lote `L-0001-2609`, 10 unidades).
  4. Desmarcar la casilla de verificación de la línea para registrar una novedad.
  5. En el campo **«Cantidad aceptada»**, escribir `9`. El sistema calcula automáticamente 1 unidad rechazada.
  6. En el selector de motivos, elegir **«Daño»** (envase roto o derramado).
  7. Pulsar el botón de cámara bajo **«Foto de la novedad»** y capturar la imagen de la botella con la tapa dañada.
  8. Opcional: anotar el stock restante en la percha (ejemplo ilustrativo: `2` galones).
  9. Pulsar el botón **«Guardar recepción de la entrega completa»**: la aplicación confirma el cierre y genera el comprobante digital.

*(Nota: Cantidades y lotes corresponden a un ejemplo ilustrativo del escenario de prueba).*

## 4. Cómo se comprueba que aprendió
- El receptor escanea un bulto de prueba, aprueba las líneas conformes y registra un rechazo parcial con motivo y fotografía en menos de 3 minutos.
- Comprende que la fotografía protege al hospital al documentar exactamente qué producto llegó dañado para su reposición inmediata.

## 5. Referente de dudas por turno
- **Turno matutino y vespertino:** Analista de Servicio al Cliente y Recepciones de IstpetDev.
- **En el momento de la entrega:** Conductor asignado a la ruta o línea telefónica de soporte logístico.
