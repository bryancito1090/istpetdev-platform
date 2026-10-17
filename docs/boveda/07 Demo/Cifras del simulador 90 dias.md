---
tipo: demo
estado: validado
actualizado: 2026-10-17
tags: [demo, simulador, benchmark, 90-dias, metricas, issue-319]
---

# Cifras canónicas del simulador de 90 días

## Resumen del experimento

Corrida comparativa ejecutada sobre el dataset **demo-quito-planificacion importado en la base**,
sin `Demo:Enabled` y sin leer archivos del disco. El simulador toma puntos, productos, stock, consumos,
vehículos y matriz vial desde las tablas de la organización.

- **Dataset:** `demo-quito-planificacion` · hash `D2FBFD3CD3B768EA6212E947ED9BB37F4F3BE0D906BD8FB799802133582BFA3A`
- **Comparación:** `2987aaea-f068-4906-b5a4-96ef41c600a1`
- **Semillas base:** 42, 101, 2024 · **Adversa:** 9999
- **Corridas:** 12 (3 políticas × 4 escenarios), todas completadas
- **Horizonte:** 90 días desde el día siguiente al último consumo importado
- **Etiqueta:** cifras simuladas y medidas en esta corrida; no son ahorro observado

## Resultados por política

Rango mínimo–mediana–máximo de las tres semillas base. El adverso se informa aparte.

| Política | Costo transporte | Kilometraje | Entregas | Puntualidad | Quiebres | Lead time mín. |
|---|---:|---:|---:|---:|---:|---:|
| IstpetDev | $6,077.22 | 6,571.2 km | 939 | 38.6 % | 16,589 | 1 días |
| Reactiva | $8,507.86 | 9,699.4 km | 1,966 | 38.6 % | 16,608 | 1 días |
| Semanal fija | $4,889.23 | 5,589.1 km | 1,611 | 40.4 % | 16,117 | 1 días |

### Detalle de rangos

| Política | Métrica | Mínimo | Mediana | Máximo | Adverso (9999) |
|---|---|---:|---:|---:|---:|
| IstpetDev | Costo transporte | 5,956.47 | 6,077.22 | 6,112.42 | 6,676.75 |
| IstpetDev | Kilometraje | 6,442.1 | 6,571.2 | 6,596.2 | 7,107.4 |
| IstpetDev | Entregas | 934 | 939 | 941 | 1,037 |
| IstpetDev | Puntualidad % | 38.4 | 38.6 | 38.7 | 32.4 |
| IstpetDev | Quiebres | 16,576 | 16,589 | 16,660 | 18,287 |
| IstpetDev | Lead time mínimo (días) | 1 | 1 | 1 | 2 |
| Reactiva | Costo transporte | 8,484.86 | 8,507.86 | 8,623.84 | 7,990.22 |
| Reactiva | Kilometraje | 9,645.5 | 9,699.4 | 9,977.7 | 9,006.8 |
| Reactiva | Entregas | 1,964 | 1,966 | 1,973 | 2,398 |
| Reactiva | Puntualidad % | 38.5 | 38.6 | 38.7 | 32.8 |
| Reactiva | Quiebres | 16,561 | 16,608 | 16,640 | 18,171 |
| Reactiva | Lead time mínimo (días) | 1 | 1 | 1 | 2 |
| Semanal fija | Costo transporte | 4,889.23 | 4,889.23 | 4,889.23 | 4,792.82 |
| Semanal fija | Kilometraje | 5,589.1 | 5,589.1 | 5,589.1 | 5,445.9 |
| Semanal fija | Entregas | 1,611 | 1,611 | 1,611 | 1,599 |
| Semanal fija | Puntualidad % | 40.1 | 40.4 | 40.6 | 31.7 |
| Semanal fija | Quiebres | 16,068 | 16,117 | 16,205 | 18,455 |
| Semanal fija | Lead time mínimo (días) | 1 | 1 | 1 | 2 |

## Quiebres frente a las baselines

Clave de quiebre: `punto|SKU|día`. Evitados y nuevos se presentan por separado; la diferencia neta no se etiqueta como “evitados”.

| Baseline | Evitados (mediana) | Nuevos (mediana) | Diferencia neta (mediana) | Adverso evitados / nuevos / neto |
|---|---:|---:|---:|---:|
| Reactiva | 1,733 | 1,714 | -20 | 1,985 / 2,101 / -116 |
| Semanal fija | 1,375 | 1,854 | -459 | 2,277 / 2,109 / 168 |

## Demanda no atendida por SKU y unidad

Litros y kilogramos (u otras unidades base) nunca se suman entre sí.

| SKU | Unidad | Mediana base | Adverso |
|---|---:|---:|---:|
| PRD-0001 | GALON | 9,533.442 | 12,082.597 |
| PRD-0002 | GALON | 3,932.191 | 5,249.063 |
| PRD-0003 | SACO | 89.735 | 197.504 |
| PRD-0004 | GALON | 3,316.701 | 4,390.781 |
| PRD-0005 | FRASCO | 15,588.107 | 19,331.071 |
| PRD-0006 | PAQUETE | 26,178.057 | 31,961.058 |
| PRD-0007 | CAJA | 3,023.695 | 3,959.074 |
| PRD-0008 | PAQUETE | 5,800.804 | 7,418.646 |
| PRD-0009 | PAQUETE | 6,553.825 | 8,509.631 |
| PRD-0010 | CAJA | 10,991.267 | 13,546.487 |
| PRD-0011 | CANECA | 38.793 | 83.249 |
| PRD-0012 | UNIDAD | 806.783 | 1,306.182 |

## Lectura honesta de la corrida

- IstpetDev recorrió **menos kilómetros y tuvo menor costo de transporte que Reactiva**, pero también completó menos entregas y su diferencia neta de quiebres fue desfavorable (-20 en la mediana base).
- Frente a Semanal fija, IstpetDev tuvo **mayor costo, mayor kilometraje y más quiebres netos en el rango base**; en el adverso la diferencia neta de quiebres se invierte (+168).
- La puntualidad se calcula por líneas de demanda atendidas, no sumando cantidades de unidades incompatibles.
- El resultado no se presenta como ahorro empresarial. Es una comparación reproducible sobre datos sintéticos.

## Reproducibilidad y evidencia

- **Resumen JSON:** `docs/evidence/simulation-quito/comparison-2987aaea-f068-4906-b5a4-96ef41c600a1.json`
- **Dataset:** `demo-quito-planificacion`, manifiesto en `data/fixtures/demo-quito-planificacion/manifest.json`.
- **Historia enriquecida:** reglas en el README del fixture y en `historyRules` del manifiesto; la ventana de decisión de 14 días se conserva.
- **Vehículo adverso retirado:** `VEH-0001`.
- **A/B/C del pitch:** verificado sin cambios: A 57,89; B 69,89; C 66,90 (recomendada), en `EnsayoPlanificacionIntegrationTests`.
- **Commit de implementación:** pendiente de registrar en el PR de #319.

Notas relacionadas: [[Dataset y escenarios]], [[Simulador y metricas]], [[Guion del pitch]].
