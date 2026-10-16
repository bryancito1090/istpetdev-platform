# Fixtures sintéticos de 001

Los archivos son datos ilustrativos del equipo, sin clientes ni mediciones reales.
`demo-base` contiene siete CSV y su perfil: una bodega, seis puntos, dos SKU,
un vehículo, dos lotes, nueve saldos y siete consumos. El caso `HOS-001`
declara 60 L de saldo y 12 L/día; el cálculo de riesgo corresponde a 002.

`ejemplo-externo` usa encabezados distintos, separador `;`, coma decimal y
galones por mes. Su perfil convierte 95 × 3.78541 a 359.614 L en 30 días.

Ambos entran mediante el importador; las migraciones no siembran estos datos.
Para subir un perfil como texto se usa `-F 'profile=<ruta/mapping.json'`.
Una validación en seco registra el lote y deja los datos operativos intactos.

`demo-quito-planificacion` (issue #243) es el escenario de Quito para el módulo de
planificación: 3 bodegas, 40 puntos (12 establecimientos de salud públicos con ubicación
real de OpenStreetMap), 12 productos, 10 vehículos, 60 días de consumo, tramos por
calles reales e incidentes basados en hechos publicados. Se regenera con
`scripts/data/generate-demo-quito-planificacion.py`; su README separa lo real de lo
sintético y cita las fuentes.
