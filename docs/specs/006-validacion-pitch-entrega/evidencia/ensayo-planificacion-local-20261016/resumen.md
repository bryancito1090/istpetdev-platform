# Resumen del Ensayo Técnico Automatizado — Módulo de Planificación (#285)

- **Entorno:** Local (PostGIS 16 + API IstpetDev net8.0)
- **Fecha y hora de ejecución:** 2026-10-17 04:20:07 UTC
- **Fixture:** `demo-quito-planificacion`
- **Configuración logística:** `data/fixtures/demo-quito-planificacion/appsettings.demo-quito.json`
- **Resultado global:** 6/6 pasos exitosos sin intervención manual

---

## 1. Cifras Clave del Caso del Pitch (A / B / C)

| Métrica | Opción A (Bodega Central) | Opción B (Bodega Alternativa) | Opción C (Operador Contratado) |
|---|---|---|---|
| **Tipo de Flota** | Propia | Propia | Contratada (`Operador Logístico Contratado (Quito)`) |
| **Costo Total Estimado** | **USD 57.89** | **USD 69.89** | **USD 66.90** |
| **Puntaje Motor (Score)** | **81.70** | **89.10** | **90.80** |
| **Recomendada** | No | No | Sí |

- **Opción seleccionada como recomendada:** `C`
- **Justificación del motor:** Se recomienda C: la flota propia deja 0 pedido(s) pendiente(s) (0 kg); el operador contratado cubre el 100% con tiempo de respuesta de 4 h y costo de faltante de USD 0.
- **Desglose de costos (Flota Propia - Opción recomendada):**
  - Combustible: **USD 5.63** (Precio: USD 3.11/gal, Vigencia: 2026-10-12, Fuente: Primicias (12-oct-2026; precio vigente 12-oct a 11-nov-2026))
  - Desgaste / Mantenimiento: **USD 3.91**
  - Conductor: **USD 38.64**
  - Ayudantes: **USD 53.19**
  - Merma esperada: **USD 3.83**

---

## 2. Resultados de los 6 Pasos del Ensayo

1. **Reset del Fixture (`01-reset.json`):**
   - Fixture `demo-quito-planificacion` cargado exitosamente (`applied: true`).
2. **Propuesta Sourcing A/B/C (`02-suggest-pitch-abc.json`):**
   - 4 pedidos sintéticos creados para el pitch (PTO-0004, PTO-0005, PTO-0006, PTO-0015).
   - Generadas opciones A, B y C. Desglose completo de 5 componentes > 0. Precio combustible fechado y con fuente.
3. **Factor Hora Pico (`03-evaluacion-horas-pico.json`):**
   - Salida a las 07:30 (hora pico, factor 0.64): **78 min**.
   - Salida a las 11:00 (fuera de pico, factor 1.0): **68 min**.
   - Incremento en hora pico: **+10 min (+14.71 %)**.
4. **Restricción Vehicular Horaria (`04-restriccion-camion-pesado.json`):**
   - Camión pesado (`VEH-0002`) descartado en franja 06:00-10:00 (Av. Simón Bolívar / Ruta Viva).
   - Confirmación bloqueada con HTTP 409 Conflict (`restriccion_horaria`).
5. **Incidente y Replanificación (`05-incidente-manifestacion-replan.json`):**
   - Incidente `Manifestacion` en centro histórico con factor de ralentización `0.3`.
   - Replanificación evaluada con HTTP 201 Created y opciones (`Escalated`).
6. **Guía de Retorno e Idempotencia (`06-guia-retorno-idempotencia.json`):**
   - Guía generada `GRT-3EC50383`.
   - Primera recepción en bodega confirmada (HTTP 200).
   - Segunda recepción duplicada rechazada con HTTP 409 Conflict (`AlreadyReceived`), garantizando protección de saldos.