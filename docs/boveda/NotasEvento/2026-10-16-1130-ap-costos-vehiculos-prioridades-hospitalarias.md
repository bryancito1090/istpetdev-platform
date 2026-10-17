---
tipo: nota-evento
estado: pendiente
actualizado: 2026-10-16
tags: [hackathon, evento, notas, costos, flota, sercop, n8n, combustible]
---

# Nota del evento — Costos de flota, prioridades contractuales hospitalarias y automatización con n8n

- **Registró:** Anthony Pilatasig (`ap`)
- **Registrado:** 2026-10-16 11:30 (America/Guayaquil)
- **Ocurrió:** 2026-10-16 11:25 (America/Guayaquil)
- **Contexto:** Hackathon Expo Clean Ecuador 2026. Definición y desglose de costos vehiculares (combustible, mantenimiento, mano de obra), tipos de rutas y vehículos, prioridades en contratos de limpieza hospitalaria (SERCOP) y viabilidad de orquestación mediante n8n.
- **Fuente del apunte:** dictado de un integrante / sesión técnica de definición

---

## Apunte transcrito

«Revisar los tipos de prioridades en las entregas, tipos de rutas con diferentes tipos de vehículos, si los contratos con los hospitales o las empresas tienen esas prioridades para revisar si necesitan rápido y el coste que les supone. Ver de dónde sacamos los costes de los vehículos como temas de la gasolina o de la movilización como tal, cómo calculamos eso y si podemos hacerlo con n8n. Documentar todo eso.»

---

## Resumen operativo

Se requiere estructurar y transparentar la procedencia de los datos económicos y logísticos del sistema:
1. **Prioridades de entrega:** Tipificación de la demanda según criticidad hospitalaria y compromisos contractuales públicos (SERCOP) o privados.
2. **Flota multimodal y perfiles de ruta:** Clasificación de vehículos (furgón pesado vs. utilitario liviano vs. moto de emergencia) y su relación con el tipo de ruta (consolidada regular vs. despacho urgente).
3. **Economía contractual:** Análisis de sanciones por incumplimiento (fichas SERCOP con multas de hasta el 1 por 1000 diario) frente al sobrecosto de un flete exclusivo de emergencia.
4. **Cálculo del costo vehicular:** Desglose analítico de los valores de `CostPerKm` (combustible en galones, desgaste mecánico, neumáticos) y costo horario de movilización de personal (`DriverHourlyCostUsd`).
5. **Arquitectura con n8n:** Implementación viable de flujos en n8n para calibrar costos dinámicos, monitorear precios de combustible y evaluar trade-offs de escalación logística en conjunto con DeepSeek.

---

## 1. Prioridades en entregas y contratos hospitalarios

### 1.1 Tipos de prioridad en el sistema
El sistema clasifica las solicitudes y necesidades de abastecimiento en tres niveles operativos:
- **Prioridad P0 (Emergencia / Quiebre inminente):** Puntos con cobertura inferior o igual al tiempo de reposición (`CoverageDays <= LeadTimeDays`) o saldo cero en insumos críticos de desinfección. Despacho inmediato sin esperar consolidación de jornada completa.
- **Prioridad P1 (Reposición regular programada):** Puntos dentro del umbral de seguridad (cobertura entre 1 y 2 veces el lead time). Se atienden en rutas multicliente optimizadas en días fijos o ventanas estándar.
- **Prioridad P2 (Rebalanceo / Stock preventivo):** Abastecimiento anticipado a bodegas secundarias o puntos de baja rotación para aprovechar capacidad ociosa de vehículos.

### 1.2 ¿Qué estipulan los contratos con hospitales (SERCOP)?
De acuerdo con las fichas técnicas oficiales recopiladas en [[SERCOP limpieza hospitalaria y sanciones]] (ej. Hospital General Docente de Calderón HGDC y Hospital General Enrique Garcés HGEG):
- **Obligación contractual de insumos:** El proveedor de limpieza está obligado a suministrar maquinaria, químicos y fungibles (gel desinfectante, cloro hospitalario, bolsas para desechos biológicos) con frecuencias de reposición fijadas por la casa de salud.
- **Cláusulas de sanción y multas:** Las penalizaciones por incumplimiento de especificaciones o desabastecimiento se aplican como fracciones del monto de la orden de compra (frecuentemente **1 por 1000 diario** sobre el valor contractual total mientras persista la infracción).
- **Impacto económico del quiebre:** Para un contrato de limpieza hospitalaria de 300.000 USD anuales (aprox. 25.000 USD mensuales), una multa de 1x1000 diario representa **25 USD por día de penalización directa**, sumada al riesgo de terminación unilateral del contrato y pérdida del registro en el catálogo electrónico de SERCOP.
- **Costo de urgencia frente a multa:** Ante un riesgo de quiebre en quirófano, asumir un sobrecosto de transporte urgente de 15 a 30 USD es financieramente preferible a soportar la sanción contractual y la suspensión administrativa.

---

## 2. Tipos de rutas y catálogo de vehículos

El motor de emparejamiento de flota (evaluado en [[Decisiones de arquitectura]] bajo ADR-28) y el ruteador ([[Rutas y sobrecostos]]) distinguen perfiles de transporte según la capacidad requerida:

| Tipo de vehículo | Capacidad peso | Capacidad volumen | Costo variable referencial | Tipo de ruta recomendada | Escenario de uso |
|---|---|---|---|---|---|
| **Camión / Furgón Pesado (VEH-01)** | 1.200 kg a 3.500 kg | 8,0 a 14,0 m³ | 0,25 a 0,35 USD/km | **Ruta Consolidada Troncal:** 6 a 12 paradas por circuito. | Reposición semanal masiva de bidones químicos, bultos de papel y contenedores. |
| **Utilitario / Van Liviana (VEH-02)** | 180 kg a 800 kg | 1,5 a 4,0 m³ | 0,15 a 0,20 USD/km | **Ruta de Reparto Urbano / Mixta:** 4 a 8 paradas en radio céntrico. | Distribución en centros de salud urbanos con calles estrechas y ventanas de descarga cortas. |
| **Mensajería / Moto (Flota de apoyo)** | 20 kg a 40 kg | 0,1 a 0,2 m³ | 0,08 a 0,10 USD/km | **Ruta Punto a Punto Express:** 1 a 2 paradas directas. | Despacho urgente de emergencia de químicos dosificados, reactivos o insumos livianos en quiebre crítico. |

---

## 3. Modelo de costos vehiculares: ¿De dónde salen las cifras?

El costo de transporte no se inventa; se modela dividiendo el costo total en dos vectores: **costo por distancia (`CostPerKm`)** y **costo por tiempo (`DriverHourlyCostUsd`)**.

```text
Costo Total de Ruta = (Distancia_Total_Km * CostPerKm) + (Tiempo_Jornada_Horas * DriverHourlyCostUsd) + Peajes
```

### 3.1 Desglose del costo por kilómetro (`CostPerKm`)
Basado en la estructura económica de transporte en Ecuador (precios de referencia de combustibles regulados y mantenimiento vehicular):

1. **Gasto de combustible:**
   - *Precio de combustible en Ecuador:* Diésel automotriz regulado a aprox. 1,75 - 1,90 USD/galón; Gasolina Extra/Ecopaís a aprox. 2,55 - 2,70 USD/galón (esquema de bandas).
   - *Rendimiento vehicular promedio:*
     - Furgón mediano diésel: 28 km/galón -> `1,80 USD / 28 km = 0,064 USD/km`.
     - Camión 3,5t diésel: 16 km/galón -> `1,80 USD / 16 km = 0,113 USD/km`.
     - Utilitario liviano a gasolina: 35 km/galón -> `2,60 USD / 35 km = 0,074 USD/km`.
2. **Mantenimiento y neumáticos:**
   - Cambio de aceite y filtros cada 5.000 km (aprox. 50 USD) -> `0,010 USD/km`.
   - Juego de neumáticos cada 45.000 km (aprox. 400 - 600 USD) -> `0,011 USD/km`.
   - Amortización de frenos, suspensión y fluidos menores -> `0,015 USD/km`.
3. **Depreciación del activo vehicular:**
   - Estimada en `0,040 a 0,060 USD/km` según vida útil contable de 250.000 km.
4. **Consolidación del costo por km:**
   - **Vehículo liviano (VEH-02):** `0,074 (combustible) + 0,036 (mantenimiento/llantas) + 0,040 (depreciación) = 0,15 USD/km`.
   - **Vehículo pesado (VEH-01):** `0,113 (combustible) + 0,057 (mantenimiento/llantas) + 0,080 (depreciación) = 0,25 USD/km`.
   - *Esto respalda la calibración histórica documentada en [[Rutas y sobrecostos]] (VEH-01 a 0,25 USD/km y VEH-02 a 0,15 USD/km).*

### 3.2 Desglose del costo horario de movilización (`DriverHourlyCostUsd`)
El valor base fijado en el sistema es de **6,00 USD/hora**, obtenido de la estructura laboral ecuatoriana:
- Salario Básico Unificado (SBU) chofer profesional categoría D/E: ~500 USD/mes.
- Aportes patronales IESS (12,15%) + décimo tercero + décimo cuarto + fondos de reserva: ~220 USD/mes.
- Costo mensual empleador: ~720 USD/mes.
- Jornada laboral legal de 160 horas al mes: `720 / 160 = 4,50 USD/hora`.
- Factor de recargo por horas extras diurnas/nocturnas, viático de alimentación y supervisión de estiba: `+1,50 USD/hora`.
- **Total operativo:** `6,00 USD/hora` de tiempo activo de servicio y conducción.

---

## 4. Implementación y automatización con n8n

¿Es viable y conveniente calcular y gestionar esto con n8n? **Sí, como orquestador de parámetros externos y evaluador de decisiones, manteniendo las reglas de [[n8n e IA]]:**

### 4.1 Arquitectura de flujos sugeridos en n8n

```text
[Servicio Externo / API Combustible] 
             │
             ▼
[n8n WF: Calibrador de Costos] ──> Valida datos ──> [API IstpetDev: PATCH /configuration]
                                                            │
[Alerta de Riesgo Hospitalario]                            ▼
             │                                     [Motor de Ruteo]
             ▼                                             │
[n8n WF: Evaluador de Emergencia (DeepSeek)] ──────────────┘
  - Evalúa multa contractual vs. sobrecosto flete
  - Genera recomendación en lenguaje natural
  - Presenta opción de despacho al Planificador
```

### 4.2 Casos de uso de n8n para costos y prioridades
1. **Flujo WF-COSTOS (Actualización de precios de combustible y peajes):**
   - Un nodo cron mensual en n8n consulta el tarifario oficial de combustibles publicado por Petroecuador o la Agencia de Regulación.
   - Aplica la fórmula paramétrica del `CostPerKm` por tipo de motor y envía un payload firmado con HMAC a la API (`POST /configuration/vehicles/costs`).
2. **Flujo WF-EVALUADOR-URGENCIAS (Trade-off de flete vs. multa hospitalaria):**
   - Cuando la API detecta un punto hospitalario en riesgo crítico (`CoverageDays < 1`), dispara un webhook a n8n.
   - n8n invoca al agente DeepSeek pasándole los datos duros:
     - Costo de despacho express exclusivo: ej. 28 USD (50 km ida y vuelta + 2 h chofer).
     - Multa contractual estimada SERCOP: ej. 25 USD/día + demérito en expediente del hospital.
   - DeepSeek genera el texto de recomendación para el Planificador: *«Punto Hospital Calderón en riesgo crítico de cloro. Flete de emergencia estimado en 28 USD evita riesgo de sanción contractual SERCOP (25 USD/día). Se sugiere autorizar salida de VEH-02»*.
   - El resultado se inyecta en el buzón del Planificador para su confirmación humana.

---

## Acciones y preguntas abiertas

- [ ] Confirmar si en la demo se simulará una fluctuación de precio de combustible mediante un trigger de n8n para mostrar la adaptación del costo proyectado.
- [ ] Incorporar en las notas de pitch la justificación real del `0,25 USD/km` y `6,00 USD/h` para responder con solvencia técnica las preguntas del jurado.
- [ ] Revisar si se incluye el perfil de vehículo tipo moto o courier urbano en la tabla de flota de la base de datos para entregas de emergencia hiperlocales.

---

## Seguimiento

- [[Notas del evento]]
- [[2026-10-16-1115-ap-flexibilidad-roles-rutas-productos]]
- [[SERCOP limpieza hospitalaria y sanciones]]
- [[Rutas y sobrecostos]]
- [[Simulador y metricas]]
- [[Modelo de negocio y piloto]]
- [[n8n e IA]]
