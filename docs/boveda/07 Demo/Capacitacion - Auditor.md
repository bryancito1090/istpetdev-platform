---
tipo: guia-capacitacion
estado: propuesta
actualizado: 2026-10-16
tags: [capacitacion, roles, auditor, demo-quito, pitch, reto-1]
---

# Hoja de capacitación operativa: Auditor (Control Interno y Calidad)

**Objetivo del rol:** Reconstruir la historia completa de cualquier bulto o entrega desde que salió del almacén central hasta que fue recibido por el cliente, verificando firmas, fotografías de novedades y sellos digitales en una vista de solo lectura.

## 1. Qué aprende
- Consultar la trazabilidad digital de un bulto o unidad de entrega mediante su código o escaneando su código QR.
- Verificar la cadena de custodia completa: quién preparó la caja en bodega, en qué fecha y hora se despachó, qué conductor y vehículo la transportaron, y quién la recibió en el destino final.
- Inspeccionar las novedades de campo, rechazadas o no conformidades, revisando la causa documentada (daño, rotulado, producto erróneo) y la fotografía adjunta por el receptor.
- Navegar por el sistema en modo seguro de **Solo Lectura** sin alterar accidentalmente rutas, inventarios ni pedidos en curso.

## 2. Duración
- **30 minutos** de práctica de auditoría y seguimiento forense de entregas.

## 3. Ejercicio práctico con el escenario Quito (`demo-quito-planificacion`)
- **Caso a auditar:** Entrega de insumos de desinfección al Hospital General Enrique Garcés (`PTO-0005`).
- **Paso a paso en la aplicación web (`/public-trace` y `/rutas`):**
  1. Ingresar al módulo de **Consulta Pública de Custodia (QR)** en `/public-trace`.
  2. En el campo de búsqueda, ingresar el código de la unidad de entrega (ejemplo ilustrativo: código de bulto o identificador de caja).
  3. Comprobar la secuencia cronológica de hitos:
     - Hito 1: Preparación física en Bodega Central Sur - Turubamba (`BOD-0001`), con detalle de producto y número de lote (`PRD-0001`, Lote `L-0001-2609`).
     - Hito 2: Salida en el camión Hino Dutro City `PCB-2214` conducido por el transportista asignado.
     - Hito 3: Llegada a la rampa del Hospital Enrique Garcés.
     - Hito 4: Acta digital de recepción con fecha, hora y confirmación del receptor.
  4. Ingresar al módulo de **Rutas** (`/rutas`) bajo el perfil de auditoría:
     - Constatar que el encabezado muestra la etiqueta **«Panel de Auditoría (Solo Lectura)»**.
     - Abrir la pestaña **«Bandeja de Capturas Rechazadas»**: auditar las líneas con novedades registradas en el día, revisando el motivo de rechazo (ejemplo: envase con daño) y la fotografía de respaldo tomada por el cliente.

*(Nota: Tiempos y códigos corresponden a un ejemplo ilustrativo del escenario de prueba).*

## 4. Cómo se comprueba que aprendió
- El auditor localiza un bulto seleccionado al azar en menos de 2 minutos y expone verbalmente su línea de tiempo completa: origen, responsable de empaque, vehículo de transporte, hora de entrega y resultado de la inspección física.
- Identifica correctamente una entrega con rechazo parcial y extrae la fotografía de respaldo que sustenta el reclamo al proveedor o transportista.

## 5. Referente de dudas por turno
- **Cumplimiento normativo y protocolos:** Oficial de Cumplimiento / Jefe de Control Interno.
- **Soporte de base de datos y registros:** Administrador de Plataforma IstpetDev.
