# demo-quito-planificacion (issue #243)

Escenario de Quito para probar el módulo de planificación, desde un día normal hasta la avería de un camión a mitad de ruta. Lo genera `scripts/data/generate-demo-quito-planificacion.py` con semilla fija, así que es reproducible.

> **Qué es real y qué es sintético.**
> - **Real:** las ubicaciones de los 12 establecimientos de salud públicos (OpenStreetMap, ODbL), las distancias y tiempos por calles (OSRM sobre OpenStreetMap), los modelos de vehículo y su capacidad (fichas comerciales), el precio del combustible del 12-oct-2026, las franjas de pico y placa y de restricción a camiones pesados, la velocidad en hora pico y los lugares y tipos de los incidentes (hechos publicados por la AMT y la prensa).
> - **Sintético:** la relación comercial con cada punto, los 28 negocios inventados, las cantidades, el consumo, el inventario, las matrículas, los kilometrajes, el rendimiento y los costos por km, el valor de los productos, el operador contratado y el factor de velocidad de cada incidente.
> - **Nada de esto es normativa ni precio oficial vigente** para usar fuera de la demo sin volver a verificar la fuente.

## Contenido

| Archivo | Filas | Qué contiene |
|---|---|---|
| `bodegas.csv` | 3 | Bodega Central Sur (Turubamba), Norte (Carcelén) y Valle (Conocoto) |
| `puntos.csv` | 40 | 10 hospitales y 2 centros de salud públicos reales, más 28 negocios sintéticos: 6 locales de comida, 5 oficinas, 4 clínicas privadas, 3 sucursales bancarias, 3 colegios, 2 centros comerciales, 2 comercios, 2 hoteles y 1 planta industrial. `region` es la zona de Quito (Norte, Centro, Centro Histórico, Sur, valles) |
| `productos.csv` | 12 | Insumos de limpieza con peso y volumen estimados, `unit_value` y `units_per_package`; el detergente en saco de 30 kg es el bulto de más de 25 kg |
| `lotes.csv` | 24 | Dos lotes por producto; un lote de hipoclorito vence en 20 días (prueba de vencimiento) |
| `vehiculos.csv` | 10 | Hino Dutro (City, 616, 716 y 816), Chevrolet N400 Max Cargo y BYD T35 eléctrico, con `vehicle_type` y `usage_scope` |
| `consumos.csv` | 18 103 | Consumo diario de 60 días (17-ago a 15-oct-2026), con fines de semana, un brote y un hueco de datos |
| `inventario.csv` | 397 | Saldos por bodega y punto, con cinco quiebres diseñados |
| `tramos.csv` | 1806 | Matriz dirigida completa entre los 43 lugares, con distancias y tiempos por calles reales |
| `incidentes.json` | 6 | Avería, dos accidentes, manifestación, inundación y congestión |
| `appsettings.demo-quito.json` | — | `ServiceCost`, `ContractedCarrier`, `Recommendation`, `TrafficBands` y `VehicleRestrictions` |
| `manifest.json` | — | Conteos, fuente de los tramos, ajuste de cada punto a la calle y SHA-256 de cada archivo |

**Códigos legibles y únicos:** `BOD-0001`, `PTO-0001`, `PRD-0001` y `VEH-0001`. El prefijo dice qué es cada cosa en cualquier tabla, y los cuatro dígitos permiten hasta 9999 elementos. La matrícula va aparte, en `matricula`, porque es lo que el conductor y el taller reconocen.

## Qué decisión muestra cada dato (criterios del #243)

| Dato | Decisión que hace visible | Issue |
|---|---|---|
| `PTO-0013`, centro comercial con ventana 07:00–09:30, a 10,9 km de la Bodega Norte, cuando hay puntos a menos de 1,1 km | La ventana estrecha se atiende primero, aunque no sea el punto más cercano | #238 |
| Hospitales con ventana de mañana (07:00–11:00; el Carlos Andrade Marín 06:30–10:30) | Hora límite de críticos y orden por prioridad | #193, #197 |
| Tres puntos en el Centro Histórico con ventana 06:00–08:30 | Entrega antes del tráfico y de la manifestación | #246 |
| Sucursales bancarias con ventana de una hora (08:00–09:00) | Ventanas muy estrechas | — |
| Colegios con ventana de tarde (13:30–16:00) | Una segunda vuelta en el día | — |
| `PRD-0003`, saco de 30 kg | La parada necesita ayudantes (regla de bultos de más de 25 kg) | #240 |
| `unit_value` en cada producto | Merma esperada en USD | #240 |
| Tres tipos de vehículo y dos usos, y dos eléctricos con 270 km de autonomía | Costo por perfil, restricción horaria y comparación con flota eléctrica | #240, #244 |
| `PCD-5540` (716 de 2018, 214 900 km) en `mantenimiento_proximo` y `PCG-8830` (816 de 2016, 301 250 km) | Riesgo por kilometraje: cuál conviene no mandar lejos | — |
| Bodega Valle sin guantes | Origen sugerido desde otra bodega | #194 |
| `ContractedCarrier` y `Recommendation` | Opción contratada y recomendación que no siempre es la más barata | #241 |

## Fuentes reales usadas

| Dato | Valor en el fixture | Fuente |
|---|---|---|
| Ubicación de hospitales y centros de salud | Coordenadas OSM, ajustadas a la calle más cercana | OpenStreetMap vía Nominatim, consultado el 16-oct-2026 (ODbL; atribución: «© OpenStreetMap contributors») |
| Distancias y tiempos | Matriz OSRM | `router.project-osrm.org`, red vial OpenStreetMap, 16-oct-2026 |
| Hino Dutro City, 616, 716 y 816 | 3,5 / 4,5 / 5,5 / 6,5 t | [Patiotuerca, Teojama Comercial (28-jun-2022)](https://ecuador.patiotuerca.com/blog/teojama-comercial-ya-ofrece-la-renovada-serie-300-de-hino/) |
| Chevrolet N400 Max Cargo | 720 kg y 3,1 m³ | [Patiotuerca, presentación de la N400](https://ecuador.patiotuerca.com/blog/chevrolet-presento-la-nueva-chevrolet-n400/) |
| BYD T35 eléctrico | 2,7 t y hasta 270 km de autonomía | [La República (Colombia)](https://www.larepublica.co/ocio/byd-lanza-t35-un-camion-electrico-4345654) |
| Diésel y gasolina Extra/Ecopaís | 3,11 y 3,17 USD/galón (12-oct a 11-nov-2026) | [Primicias](https://www.primicias.ec/economia/precios-gasolina-super-extra-diesel-guerra-medio-oriente-iran-134649/) |
| Hora pico (`TrafficBands`) | Factor 0,64 = 16 km/h ÷ 25 km/h, en 06:00–09:30 y 16:00–20:00 | Velocidad: [Primicias, Secretaría de Movilidad](https://www.primicias.ec/noticias/sociedad/quito-hoy-no-circula-multas). Franjas: las del pico y placa, [Extra](https://www.extra.ec/noticia/servicios-y-tramites/asi-sera-horario-pico-placa-quito-jueves-9-abril-2026-151064.html) |
| Franja del mediodía | Factor 0,80, 12:30–14:00 | El Plan Maestro de Movilidad marca las 13:00 como hora congestionada (misma nota de Primicias); **el factor 0,80 es un supuesto** |
| `pico_y_placa_dia` por vehículo | Según el último dígito de la matrícula: lunes 1-2, martes 3-4, miércoles 5-6, jueves 7-8, viernes 9-0 | [Extra, pico y placa 2026](https://www.extra.ec/noticia/servicios-y-tramites/asi-sera-horario-pico-placa-quito-jueves-9-abril-2026-151064.html). **Por verificar** si aplica a vehículos de carga comercial |
| `VehicleRestrictions` | Camión pesado restringido en la Av. Simón Bolívar y la Ruta Viva, 06:00–10:00 y 16:00–20:00 | Vigente desde el 10-nov-2025, [El Diario](https://www.eldiario.ec/quito/quito-cambia-las-reglas-para-el-transporte-pesado-conozca-los-nuevos-horarios-y-rutas-restringidas-11112025/). Exentos: camiones 2D de carga liviana hasta 7 t y 5 m. **Por verificar** cuál de nuestros Hino queda exento |
| Costo por hora de un ayudante | 4,13 USD/h | SBU 2026 de 482 USD ([Extra](https://www.extra.ec/noticia/servicios-y-tramites/asi-sera-horario-pico-placa-quito-jueves-9-abril-2026-151064.html)) + aporte patronal 12,15 % + décimo tercero, décimo cuarto y fondos de reserva ≈ 661 USD/mes ÷ 160 h. Cálculo del equipo |

### Incidentes basados en hechos publicados

| Preset | Tipo | Lugar | Hecho real en que se basa |
|---|---|---|---|
| `averia-matilde-alvarez` | `VEHICLE_BREAKDOWN` | Av. Matilde Álvarez, Quitumbe | Caso del equipo: el Hino Dutro City PCB-2214 se avería con 50 km recorridos. La calle existe (OSM) |
| `accidente-simon-bolivar-san-martin` | `Accidente` (0,5) | Av. Simón Bolívar, sector San Martín | Camioneta contra un árbol; la AMT cerró el carril derecho norte-sur ([El Diario, 30-mar-2026](https://www.eldiario.ec/seguridad/alerta-vial-en-el-sur-de-quito-choque-contra-un-arbol-provoca-cierres-y-deja-dos-heridos-30032026/)) |
| `accidente-redondel-zambiza` | `Accidente` (0,4) | Redondel de Zámbiza | Uno de los cinco puntos críticos de la Simón Bolívar; 256 siniestros en esa avenida de enero a julio de 2025 ([Primicias](https://primicias.ec/noticias/quito/avenida-simon-bolivar-problemas-vias)) |
| `manifestacion-centro-historico` | `Manifestacion` (0,3) | Plaza Grande | Marcha del FUT y la UNE desde la 10 de Agosto y Bogotá; cierres en Guayaquil, Flores y Montúfar ([Extra, 13-mar-2026](https://www.extra.ec/noticia/quito/marchas-quito-hoy-vias-afectadas-movilizaciones-viernes-148978.html)) |
| `inundacion-24-de-mayo` | `BloqueoVial` | Av. Maldonado y Av. 24 de Mayo | Acumulación de agua por lluvia; la AMT cerró el paso ([Extra, 6-mar-2026](https://www.extra.ec/noticia/quito/lluvias-tormentas-quito-desvios-cierres-viales-viernes-148474.html)) |
| `congestion-hora-pico-maldonado` | `Congestion` (0,64) | Av. Pedro Vicente Maldonado, salida de la Bodega Central Sur | 16 km/h en hora pico frente a 25 km/h fuera de ella (Secretaría de Movilidad) |

El número entre paréntesis es `slowdownFactor`: la velocidad cae a esa fracción. **Es un supuesto de la demo**; las notas citadas no dan ese dato. `Accidente`, `Manifestacion` y `Congestion` son los tipos que agrega #246: hasta que ese issue se fusione, el sistema solo aplica `BloqueoVial`, `ROAD_CLOSURE`, `DELAY` y `VEHICLE_BREAKDOWN`.

## Supuestos de prueba (cambiar cuando haya datos de una empresa)

- **Volumen útil de los Hino y del BYD:** depende de la carrocería; las fichas citadas no lo dan.
- **Rendimiento:** 18 a 26 km/galón en diésel y 30 a 32 en gasolina; el BYD gasta 0,35 kWh/km a 0,10 USD/kWh (precio de [BYD Ecuador](https://bydauto.ec/cuanto-cuesta-cargar-un-carro-electrico-en-ecuador/), PENDIENTE el pliego oficial de ARCONEL). De ahí sale `cost_per_km` = energía + mantenimiento.
- **`LitersPer100Km` de cada perfil:** calculado desde ese rendimiento (3,785 L ÷ km por galón × 100).
- **`ContractedCarrier`:** no se encontró un tarifario público. Se arma con el método que describe el gremio de transporte de carga: costo por hora en espera, ida con carga, retorno vacío y utilidad ([Expreso](https://www.expreso.ec/opinion/carta-de-lectores/transporte-carga-ecuador-gremio-pide-metodo-tecnico-fijar-tarifas-alza-diesel-287927.html)). Resultado: 25 USD base (cerca de 4 h × 6 USD/h), 0,55 USD/km (≈ 0,23 × 2 por el retorno × 1,2) y 0,01 USD/kg.
- **Pesos de `Recommendation`, % de merma y `LoadFuelFactor`:** decisión de diseño, tomada de los issues #240 y #241.

## Escenarios de prueba, del más simple al más difícil

| # | Escenario | Cómo se dispara | Qué debe hacer el sistema | Alerta y papel de la IA |
|---|---|---|---|---|
| E01 | Día normal | `POST /demo/reset?fixture=demo-quito-planificacion` y luego `POST /route-plans` | Rutas factibles desde las tres bodegas, que respetan ventanas, peso y volumen | Ninguna |
| E02 | Ventana estrecha primero | Plan del día | `PTO-0013` (07:00–09:30) entra temprano, aunque no sea el punto más cercano | — |
| E03 | Brote en el Enrique Garcés | Ya está en los consumos: el consumo de desinfección se multiplica por 2,4 en los últimos 5 días | `PTO-0005` / `PRD-0001` sale **Crítico** (cubierto por la prueba) | Alerta de riesgo; el agente propone la reposición |
| E04 | Quiebres diseñados | Inventario | Guantes en Calderón (0,5 días), fundas rojas en Guamaní, alcohol gel en el Isidro Ayora y papel en el centro comercial | Alertas de riesgo ordenadas por criticidad |
| E05 | Lote por vencer | Hipoclorito `L-0001-2609` vence en 20 días | Despachar primero ese lote | — |
| E06 | Bodega sin stock | La Bodega Valle no tiene guantes | Origen sugerido desde otra bodega | El agente explica de dónde sale el producto |
| E07 | Carga que no cabe | Pedidos grandes con una camioneta N400 de 720 kg | Carga parcial y remanente pendiente con motivo | — |
| E08 | Ruta con ayudante | Parada con sacos de 30 kg | Ayudantes según `LaborRules` | Requiere #240 |
| E09 | Hora pico | `TrafficBands` | La misma ruta a las 07:30 tarda más que a las 11:00 | Requiere #245 |
| E10 | Camión pesado en la Simón Bolívar | `VehicleRestrictions` | Descarta el camión pesado entre 06:00 y 10:00 y explica el motivo | Requiere #244 |
| E11 | Manifestación, accidente o congestión | `POST /incidents` con el preset | Sube la hora de llegada sin descartar la ruta; si un crítico sale de su ventana, ofrece `LateService` o `PrioritizeCritical` | Requiere #246. El agente explica el retraso con las cifras de la API |
| E12 | Inundación (bloqueo) | `POST /incidents` con `inundacion-24-de-mayo` | Replanifica evitando el tramo cerrado | El agente propone la replanificación; el planificador aprueba |
| E13 | **Avería a mitad de ruta** | Publicar una ruta y enviar `averia-matilde-alvarez` con el `vehicleCode` de esa ruta | La ruta del vehículo queda afectada con sus paradas pendientes (cubierto por la prueba) | Alerta inmediata al planificador. El agente busca un vehículo libre de la misma bodega o un operador contratado (#241) y propone a quién reasignar las paradas críticas primero. El planificador decide |

## Cómo usarlo

```bash
# Regenerar (distancias por calles con OSRM público; --sin-osrm usa línea recta × 1,35)
python scripts/data/generate-demo-quito-planificacion.py

# Prueba de integración del fixture
dotnet test tests/IstpetDev.Tests --filter "FullyQualifiedName~DemoQuitoPlanificacion"

# En una API con Demo:Enabled=true
POST /api/v1/demo/reset?fixture=demo-quito-planificacion
```

`ServiceCost` ya lo lee `ServiceCostOptions` (#240, en `develop`), con las mismas claves. `ContractedCarrier`, `Recommendation`, `TrafficBands` y `VehicleRestrictions` los leerán las clases de opciones de #241, #245 y #244; hasta entonces se ignoran sin error. Con la migración de #240, el importador guarda `vehicle_type`, `usage_scope`, `unit_value` y `units_per_package` (la prueba lo comprueba). La matrícula, la marca, el modelo, el kilometraje, el combustible, la autonomía y el día de pico y placa se informan como columnas ignoradas hasta que exista un campo para ellos.
