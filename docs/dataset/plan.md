# Plan de organización y generación de datasets

**Actualizado:** 2026-10-15

**Estado:** ejecutado; fixture `national-v1` generado y agregado ACESS incluido (hallazgo H-01 aplicado)

## Objetivo

Organizar bajo `docs/dataset/` los datos que ayudan a probar IstpetDev y las fuentes públicas que pueden aportar contexto geográfico real, dejando clara la diferencia entre datos sintéticos, registros públicos y datos operativos.

## Revisión del sistema y la bóveda

- El sistema ya tiene `ScenarioGenerator.GenerateFromScenarioFileAsync` en `src/Application/Simulation/ScenarioGenerator.cs`. Su configuración `data/fixtures/simulation-national/scenario.json` describe `national-v1`: seis regiones, 80 puntos sintéticos, 12 vehículos, dos productos y 90 días.
- El generador produce los CSV canónicos de bodegas, puntos de servicio, productos, vehículos, lotes, stock, consumos históricos, tramos, y además `calendar.json` y `manifest.json` con seed y SHA-256. `mapping.json` ya describe el formato de importación.
- `data/fixtures/demo-base`, `demo-region`, `ejemplo-externo` y `simulation-national` ya existen y son usados por importación, rutas, simulación y pruebas. Se conservan en sus rutas actuales; el dataset generado de esta tarea será una exportación documentada, no un reemplazo de esos fixtures.
- La bóveda propone coordenadas públicas o sintéticas y exige procedencia de matrices. No hay permiso para interpretar ubicaciones públicas como clientes, ni para presentar demanda, inventario, tiempos o ahorro sintéticos como observaciones reales.

## Organización propuesta

```text
docs/dataset/
├── README.md
├── plan.md
├── test/
│   ├── README.md
│   └── national-v1-seed-11/       # salida del generador existente; todo sintético
└── reales/
    ├── README.md
    └── acess-2026-09/            # agregado público anonimizado, no operativo
```

`test/` contiene el dataset reproducible que se puede usar para pruebas del sistema. `reales/` conserva muestras públicas pequeñas o derivadas, con fuente, fecha, licencia, transformaciones y limitaciones. Los datos públicos no se insertan automáticamente en la base de demo.

## Fuentes públicas

| Fuente | Descarga / acceso | Uso propuesto y límites |
|---|---|---|
| ACESS, permisos de funcionamiento de establecimientos de salud | [Catálogo de Datos Abiertos Ecuador](https://www.datosabiertos.gob.ec/dataset/permisos-de-funcionamiento) · [CSV publicado 2026-09](https://www.datosabiertos.gob.ec/dataset/63c8ff4f-5de6-4989-96a1-f2baa9b3de27/resource/de9a8097-3802-4ee1-80e2-b307cab28733/download/acess_permisosdefuncionamiento_2026_09.csv) | Descarga a revisar y resumir por provincia, cantón y tipo/categoría. El agregado no debe conservar RUC, razón social, nombre del establecimiento ni número de permiso. No mide consumo ni demanda logística. Licencia indicada por el catálogo: Creative Commons Attribution; conservar atribución.
| MSP GeoSalud | [Página oficial de información georreferenciada](https://www.salud.gob.ec/informacion-georreferenciada/) · [GeoSalud](https://geosalud.msp.gob.ec/) | Fuente oficial mensual para ubicar establecimientos de salud; ofrece consulta/descarga interactiva. Verificar licencia, campos y fecha antes de incorporar extractos.
| OpenStreetMap vía Geofabrik | [Descarga de Ecuador](https://download.geofabrik.de/south-america/ecuador.html) · [PBF actual](https://download.geofabrik.de/south-america/ecuador-latest.osm.pbf) | Red vial y lugares mapeados. El extracto nacional ronda 126 MB al 2026-10-15; mantener el enlace y descargar solo una selección acotada si hace falta. Atribuir a OpenStreetMap contributors y respetar ODbL 1.0. Geometría vial por sí sola no verifica distancias/tiempos de una ruta de proveedor.
| INEC, Recursos y Actividades de Salud 2020 | [Conjunto de datos y archivos CSV/diccionario](https://www.datosabiertos.gob.ec/dataset/registro-estadistico-de-recursos-y-actividades-de-salud-2020) | Fuente estadística histórica con 4.136 registros y 1.915 variables, licencia Creative Commons Attribution. No descargar el archivo amplio para probar rutas; usarlo solo si una pregunta analítica concreta requiere esas variables.
| INEC, cartografía censal | [Servicio REST de cartografía censal](https://idgn.ecuadorencifras.gob.ec/server/rest/services/Cartografia_Censal_WMS_2022/MapServer) | Límites administrativos consultables en JSON/GeoJSON. Confirmar capa, sistema de coordenadas y condiciones de reutilización antes de guardar una copia.

## Pasos de ejecución

1. Crear el fixture de prueba con la configuración y el generador existentes: versión `national-v1`, seed `11`, escenario `base`. Guardar salida en `docs/dataset/test/national-v1-seed-11/` junto a un README que identifique origen sintético, esquema, unidades y comando reproducible.
2. Descargar temporalmente el CSV ACESS 2026-09, validar su encabezado/diccionario y producir un agregado por provincia, cantón y tipo/categoría con dos medidas: `establecimientos` (identificador de establecimiento usado solo para deduplicar, no conservado) y `permisos` (filas del archivo). Eliminar del agregado todo identificador personal, tributario, institucional o de permiso. No versionar el CSV original si contiene RUC u otros identificadores.
3. Guardar con el agregado un manifiesto de procedencia que incluya URL de la fuente, fecha de descarga, licencia, método de agregación, número de filas de entrada/salida y SHA-256 del origen y del resultado. El hash del archivo original no expone su contenido.
4. Añadir `docs/dataset/README.md` y los README de `test/` y `reales/`, explicando qué archivo descargar, cómo se generó cada muestra y dónde se utiliza en el sistema.
5. Actualizar la nota de la bóveda sobre dataset/escenarios y la guía de datos para reflejar que ya hay una salida sintética generada, sin declarar resultados del simulador medidos.
6. Verificar que los CSV/JSON se leen, comprobar conteos y hashes del manifiesto, ejecutar las pruebas de `ScenarioGenerator` y el verificador de la bóveda. Revisar el diff para confirmar que no haya cambios de aplicación, migraciones ni datos reales identificables.

## Criterios de aceptación

- Existen `docs/dataset/test/` y `docs/dataset/reales/` con instrucciones y fuentes navegables.
- La carpeta de prueba contiene la salida del generador actual y el manifiesto declara 80 puntos, 6 bodegas, 12 vehículos, 2 productos, seed 11 y horizonte de 90 días.
- El agregado real se puede rastrear al archivo ACESS publicado; no guarda RUC, razón social, nombres de centros ni números de permiso, y distingue 2590 establecimientos únicos de 2601 permisos (11 permisos adicionales en 10 grupos).
- Licencias, fecha de corte, hash y límites de uso están documentados. Datos públicos y sintéticos no se etiquetan como demanda, inventario o rutas observadas.
- Las pruebas pertinentes y `python docs/boveda/.github/scripts/check_vault.py` pasan.
- Se entrega en una rama creada desde `develop` actualizado y mediante PR hacia `develop`.

## Exclusiones

No incluye cambios a contratos, esquemas, migraciones, API, datos de producción, despliegue ni resultados de simulación. Una futura importación de ubicaciones del MSP u OSM deberá revisar primero campos, licencia y transformaciones y conservar la fuente original fuera del fixture sintético.
