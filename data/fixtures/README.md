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

## demo-region-caos (#117)

Variante de `demo-region` preparada para validar resiliencia, importaciones masivas y detección de corrupción:

- `inventario.csv`: Reproduce **ESC-07** teniendo dos lotes del mismo SKU (`DES-01`: lotes `L-DES-2610` y `L-DES-2611`) con fechas de vencimiento distintas y stock simultáneo en el mismo punto de servicio (`HOS-N01`).
- `consumos.csv`: Incluye consumos con fecha del día de la demo (`2026-10-16`) además de los históricos existentes (`2026-10-05`).
- `corruptos/`: Fixtures para validar rechazo seguro con localización de fila y campo en **ESC-21**:
  - `consumos-columnas-desfasadas.csv`: Fila 3 con columnas corridas/desplazadas; el importador rechaza con HTTP 422 señalando fila y campo (`date`).
  - `consumos-codificacion-mixta.csv`: Archivo en UTF-8 con una fila en Latin-1 (Windows-1252); el importador rechaza con HTTP 422 señalando fila y campo.
  - `consumos-sin-encabezado.csv`: Archivo sin fila de encabezados canónicos; el importador rechaza con HTTP 422 señalando fila 1 y columna requerida faltante.
