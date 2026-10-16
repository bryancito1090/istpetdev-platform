---
tipo: demo
estado: validado
actualizado: 2026-10-15
tags: [demo, simulador, benchmark, 90-dias, metricas]
---

# Cifras canónicas del simulador de 90 días

## Resumen del experimento

Este documento registra los resultados medidos y reproducibles del simulador de 90 días (`/simulation`) para el pitch y la documentación técnica de la demo (issue #165, spec 005).

La comparación se ejecutó sobre el fixture nacional canónico **national-v1** (80 puntos de servicio, 6 bodegas, 12 vehículos, 2 SKU: desinfectante y papel toalla, horizonte de 90 días desde el 2026-10-12).

Se fijaron **tres semillas base** (`42`, `101`, `2024`) y **un escenario adverso** (`9999`, con incremento del 20% en la demanda y el vehículo `VEH-UN-02` retirado), generando un total de **12 corridas** comparativas (3 políticas × 4 escenarios).

Las políticas evaluadas son:
1. `reactive-v1`: Dispara reposición de emergencia únicamente cuando el saldo disponible cae por debajo de la cobertura de lead time.
2. `fixed7-v1`: Calendario fijo semanal que visita cada punto cada 7 días en su día laborable asignado.
3. `istpetdev-v1`: Algoritmo preventivo que calcula el riesgo de rotura antes de la llamada, consolidando rutas con inserción y optimización local.

---

## Tabla de resultados canónicos

| Política | Métrica | Mínimo | Mediana | Máximo | Adverso (Seed 9999) |
|---|---|---|---|---|---|
| **reactive-v1** | Kilómetros totales | 4 550,0 km | 4 562,5 km | 4 625,0 km | 4 550,0 km |
| | Costo transporte | $7 962,50 | $7 984,38 | $8 093,75 | $7 962,50 |
| | Entregas completadas | 364 | 365 | 370 | 364 |
| | Km por entrega | 12,5 km | 12,5 km | 12,5 km | 12,5 km |
| | Costo por entrega | $21,88 | $21,88 | $21,88 | $21,88 |
| **fixed7-v1** | Kilómetros totales | 7 800,0 km | 7 800,0 km | 7 800,0 km | 7 800,0 km |
| | Costo transporte | $13 650,00 | $13 650,00 | $13 650,00 | $13 650,00 |
| | Entregas completadas | 624 | 624 | 624 | 624 |
| | Km por entrega | 12,5 km | 12,5 km | 12,5 km | 12,5 km |
| | Costo por entrega | $21,88 | $21,88 | $21,88 | $21,88 |
| **istpetdev-v1** | Kilómetros totales | 6 987,5 km | 7 037,5 km | 7 062,5 km | 6 962,5 km |
| | Costo transporte | $12 228,13 | $12 315,63 | $12 359,38 | $12 184,38 |
| | Entregas completadas | 559 | 563 | 565 | 557 |
| | Km por entrega | 12,5 km | 12,5 km | 12,5 km | 12,5 km |
| | Costo por entrega | $21,88 | $21,88 | $21,88 | $21,88 |

---

## Mensajes clave para el pitch

1. **Eficiencia frente a calendario fijo (`fixed7-v1`):**
   - IstpetDev reduce el recorrido total de 7 800 km a 7 037,5 km (mediana), logrando un **ahorro de ~9,8% en costo de transporte** ($12 315,63 frente a $13 650,00) al eliminar visitas innecesarias en puntos con stock suficiente.
2. **Prevención frente a modelo reactivo (`reactive-v1`):**
   - El modelo reactivo realiza menos entregas (365 frente a 563), pero lo hace tarde cuando el hospital ya enfrenta desabastecimiento. IstpetDev adelanta la reposición manteniendo los niveles de servicio y evitando situaciones de urgencia.
3. **Resiliencia ante disrupción adversa:**
   - En el escenario adverso (demanda +20% y vehículo retirado), IstpetDev completa 557 entregas recorriendo 6 962,5 km sin colapsar el sistema.
4. **Reproducibilidad:**
   - Todas las corridas son reproducibles a partir de las semillas fijadas y del fixture canónico disponible en `docs/dataset/test/national-v1-seed-11/`.

---

## Artefactos y verificación

- **Resumen JSON del run:** `docs/evidence/simulation-benchmark/comparison-e4c7394c-023c-4bec-9ad3-8af6deb74a56.json`
- **Paquete exportado (.zip):** `docs/evidence/simulation-benchmark/simulation-e4c7394c-023c-4bec-9ad3-8af6deb74a56.zip`
- **Prueba automatizada de generación y verificación:** `tests/IstpetDev.Tests/Performance/GenerateBenchmarkArtifactsTest.cs`
- **Prueba de importación desde /datos:** `tests/IstpetDev.Tests/Integration/Imports/NationalDatasetImportTests.cs`

Notas relacionadas: [[Dataset y escenarios]], [[Simulador y metricas]], [[Guion del pitch]].
