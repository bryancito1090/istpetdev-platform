---
tipo: skill
estado: vigente
actualizado: 2026-10-09
tags: [istpetdev, ia, skills]
---

# IstpetDev — datos, algoritmos y simulación

**Cuándo usarla:** Datos y algoritmos de IstpetDev. Usar al diseñar tablas o migraciones de PostgreSQL/PostGIS, cambiar el modelo de datos, implementar cobertura y riesgo de reabastecimiento, rutas y costos, generar el dataset sintético, ejecutar el simulador de políticas o calcular KPIs para el pitch.

> Documentación de la skill `istpetdev-datos`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-datos` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio. Leer antes [[istpetdev-contexto]].

## Leer primero

- `04 Datos y algoritmos/Modelo de datos.md`: entidades, unidades, invariantes, índices.
- `04 Datos y algoritmos/Esquema completo de base de datos.md`: catálogo físico objetivo, FKs por organización, RLS/FORCE, preservación del esquema inicial y fases; `03 Arquitectura/RBAC y configuracion del sistema.md` para datos configurables bajo ADR-16.
- `04 Datos y algoritmos/Inventario y prediccion.md`: fórmula de cobertura, datos insuficientes, anomalías.
- `04 Datos y algoritmos/Rutas y sobrecostos.md`: datos viales, heurística, baseline, costos, incidentes.
- `04 Datos y algoritmos/Dataset y escenarios.md`: dataset sintético y escenarios S-01 a S-12.
- `04 Datos y algoritmos/Simulador y metricas.md`: comparación reproducible, fórmulas de KPIs y experimentos EXP-01 a EXP-06.

## Hechos que no se cambian sin decisión

- Motor: PostgreSQL 16 con PostGIS 3.x compatible (ADR-03). UUID v7 se genera en la aplicación: PostgreSQL 16 no tiene `uuidv7()`.
- Entornos de base, migraciones y respaldos: `03 Arquitectura/Entornos y operacion acordados.md`.
- Nombres de entidades: tabla «Nombres en código» de `00 Inicio/Hechos canonicos.md`.
- No hay dataset generado ni resultados medidos.

## Modelo y migraciones

- Integridad en la base, no solo en la aplicación: claves foráneas, `CHECK` de cantidades positivas, saldo no negativo y recepción que no supera lo despachado.
- Normalizar por defecto; desnormalizar solo con una medición que lo justifique, documentada.
- Cantidades y dinero en `numeric` con escala definida; una unidad base por SKU; nunca sumar litros, kilos y cajas.
- Movimientos solo por inserción; correcciones con movimiento compensatorio y motivo. Ledger y saldo en el mismo commit.
- Índices solo para consultas reales, comprobados con `EXPLAIN`.
- Cada migración es reversible (tiene su paso de reversión) o documenta por qué no. Nunca editar una migración ya aplicada: crear otra.
- Una sola persona crea las migraciones para evitar conflictos (dueño PENDIENTE de acordar en `06 Equipo/Equipo y acuerdos.md`).
- Respaldo con restauración probada y RPO/RTO definidos por entorno antes de la demo.

## Riesgo, rutas y simulación

- El cálculo de riesgo es determinista y guarda `snapshotId`, corte de entrada, versión de fórmula y parámetros. El LLM solo explica.
- La ruta es una heurística factible: no llamarla óptima; devolver paradas atendidas, pendientes y motivo.
- Calendario exógeno común para ambas políticas; cada política solo conoce lo disponible en su momento simulado.
- Cada ejecución guarda dataset/hash, seed, versión de política, parámetros y matriz vial; exporta `metrics.json`, resumen, eventos y manifiesto.
- Varias seeds y al menos un escenario adverso antes de mostrar cifras. Mostrar quiebres evitados **y** nuevos, denominadores y N/A cuando el denominador sea cero.
- Datos sintéticos etiquetados como tales; coordenadas sintéticas o públicas, nunca clientes reales.

## Prohibido inventar

- Cifras de ahorro, quiebres, puntualidad o anticipación sin una ejecución exportada.
- Distancias o tiempos «de la API de mapas» que no provengan de una matriz registrada con proveedor y fecha.
- Merma confirmada a partir de una anomalía.

## Verificar

- Balance por SKU/lote: existencia final + consumo + pérdidas = inicial + entradas + ajustes.
- Misma seed y configuración → mismos resultados (V-17 de `07 Demo/Plan de validacion.md`).
- Casos V-01 a V-05 y V-18 del plan de validación.

## Origen

Adaptada de la skill personal `data-architect-dba` (integridad en la base, índices con `EXPLAIN`, migraciones reversibles, respaldo probado, RPO/RTO) y de `qa-testing-engineer` (datos de prueba deterministas con seed).
