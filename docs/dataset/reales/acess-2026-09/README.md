# ACESS 2026-09 — permisos de funcionamiento (agregado)

Agregado público de los permisos de funcionamiento de establecimientos de
salud publicados por la Agencia de Aseguramiento de la Calidad de los Servicios
de Salud y Medicina Prepagada (ACESS) en Datos Abiertos Ecuador.

Atribución: **ACESS — Permisos de Funcionamiento**, Catálogo de Datos Abiertos
Ecuador. Licencia **Creative Commons Attribution (CC BY)**.

## Procedencia

| Campo | Valor |
|---|---|
| Fuente | ACESS, Agencia de Aseguramiento de la Calidad de los Servicios de Salud y Medicina Prepagada |
| Conjunto | Permisos de Funcionamiento |
| Catálogo | <https://www.datosabiertos.gob.ec/dataset/permisos-de-funcionamiento> |
| CSV descargado | `acess_permisosdefuncionamiento_2026_09.csv` (no versionado) |
| Fecha de descarga | 2026-10-15 |
| Actualización del catálogo | 2026-10-14 |
| Licencia | Creative Commons Attribution (CC BY) |

El archivo original contiene `UNICODIGO`, `INSTITUCION`, `RUC`, `RAZONSOCIAL`,
`NOMBRECOMERCIAL`, `NUMERO_PERMISO`, fechas y zona; por eso no se conserva en
el repositorio. Su tamaño y SHA-256 quedan registrados en `manifest.json`.

## Contenido

`acess-2026-09-agregado.csv` agrupa las 2601 filas del original por provincia,
cantón, tipo de establecimiento y categoría, con dos medidas por grupo:

| Columna | Descripción |
|---|---|
| `provincia` | Provincia del establecimiento |
| `canton` | Cantón del establecimiento |
| `tipo_establecimiento` | Tipología regulada del establecimiento |
| `categoria` | `PRIVADO` o `PÚBLICO` |
| `establecimientos` | Establecimientos únicos (identificador `UNICODIGO` distinto) en esa combinación |
| `permisos` | Permisos emitidos (filas del archivo fuente) en esa combinación |

Resultado: 1007 filas, 24 provincias, 152 cantones, 97 tipos y 2 categorías.
La suma de `establecimientos` es 2590 y la de `permisos` es 2601: hay 11
permisos adicionales en 10 grupos, porque un mismo establecimiento puede tener
más de un permiso emitido en el archivo. `UNICODIGO` se usa solo temporalmente
para deduplicar y no se conserva como columna ni valor en el CSV. El número de
permiso se cuenta como fila fuente, no como identificador global deduplicado
(en el origen un mismo número aparece en 3 filas). No se conserva ningún
identificador individual; solo los conteos por combinación de dimensiones.

## Transformación y límites

La transformación exacta, los conteos y los SHA-256 del origen y del resultado
están en `manifest.json`. La descarga se hizo temporalmente y se validó el
encabezado de 13 campos contra el diccionario de datos 2026 de ACESS.

- El agregado cuenta establecimientos únicos y permisos de funcionamiento
  emitidos; no mide consumo, demanda logística ni actividad de los
  establecimientos.
- No es un padrón de clientes ni una lista de establecimientos.
- No se inserta automáticamente en la base de demo.
