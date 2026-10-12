---
tipo: estrategia
estado: propuesta
actualizado: 2026-10-07
tags: [istpetdev, demo, diferenciacion]
---

# Plus para ganar

## Propuesta de IstpetDev

Nuestro plus debe ser visible, útil y demostrable en el Reto 1: **una decisión logística explicable que sigue funcionando cuando cambia la carretera o se pierde la señal, con resultados que el jurado puede verificar**.

El jurado puntúa siete criterios; demo práctica pesa 20% y es el primer desempate. La innovación y el impacto necesitan evidencia, no un listado de tecnologías. El jurado tiene tres representantes del sector de limpieza, dos técnicos, un académico y uno de LACIF (§11.3): el plus debe entenderse sin conocimientos técnicos. Ver [[Evaluacion y entregables]].

## Los cuatro plus prioritarios

| Plus | Mejora del sistema | Cómo demostrarlo | Prerrequisito |
|---|---|---|---|
| 1. Decisión explicable de abastecimiento | Cada alerta muestra stock, consumo, cobertura, lead time, frescura y acción viable | Abrir alerta del hospital y seguir su dato hasta ruta/entrega | Cálculo y snapshot fiables |
| 2. Modo crisis con replanificación | Un cierre de vía recalcula pendientes y explica costo/ETA/compromisos | Activar incidente demo y comparar versión anterior/nueva | Planner con restricciones |
| 3. Pasaporte logístico QR + continuidad offline | Consulta limitada de custodia, captura sin red y sincronización sin duplicar | Caja real, modo avión, reinicio, envío y consulta por jurado | Recepción durable y QR autorizado |
| 4. Evidencia de impacto exportable | Cada KPI abre fórmula, denominador, seed y resultado | Descargar manifiesto/resultados del mismo run mostrado | Simulación justa y exportaciones |

Los cuatro se construyen sobre P0/P1; no necesitan servicios independientes.

## Plus 1 — por qué actuar ahora

Mostrar una tarjeta «riesgo → causa → acción»:

- Cobertura calculada y fecha de observación.
- Lead time y margen configurado.
- Cantidad disponible/comprometida.
- Qué reposición propone y qué capacidad/stock la permite.
- Incertidumbre por datos faltantes o antiguos.
- Explicación breve de IA con fallback determinista.

La diferenciación es la acción trazable. La IA no sustituye el cálculo. Caso ilustrativo: 60 L / 12 L por día = 5 días, con lead time/margen que justifican actuar ahora.

## Plus 2 — control ante un imprevisto

El incidente no es solo una animación: altera la planificación. Mantener entregas ejecutadas, mostrar ruta pendiente recalculada y explicar qué cambia.

El jurado puede ver: restricción afectada, incremento de tiempo/costo, nueva ETA y punto que requiere decisión. Si no hay solución, declarar inviabilidad y alternativa; una respuesta honesta es mejor evidencia que una ruta imposible pintada como óptima.

El generador n8n está etiquetado como simulación controlada; el motor de respuesta es funcional.

## Plus 3 — experiencia física de extremo a extremo

1. Mostrar caja vacía de demostración con QR.
2. Escanear despacho con cuenta de bodega.
3. Mostrar custodia y ruta.
4. Capturar recepción/foto/firma autorizada en modo avión.
5. Cerrar/reabrir app para probar persistencia si el tiempo lo permite.
6. Recuperar red y confirmar un solo efecto en inventario.
7. Abrir el QR desde un teléfono ajeno para consultar el resumen permitido.

Usar caja vacía y productos ficticios evita depender de transporte de químicos. La firma de demostración es voluntaria. Preparar un teléfono del equipo si el jurado prefiere observar.

## Plus 4 — cifras defendibles

Dashboard con métricas de [[Simulador y metricas]]. Abrir el detalle de un resultado, mostrando número de pedidos/obligaciones y comparación con las mismas condiciones.

Ejecutar varias semillas y al menos un escenario adverso antes del pitch. Si «cero quiebres» solo ocurre en un escenario, decirlo. Mostrar también pendientes, quiebres nuevos y costo del sistema cuando se discuta beneficio neto.

No exhibir cifras fijas como resultados. El archivo exportado debe corresponder al run de la pantalla.

## Extras si lo principal ya está probado

| Extra | Valor | Condición para incluir |
|---|---|---|
| What-if de demanda/flota/lead time | Gerencia ve decisiones bajo presión | Simulador validado; parámetros y escenario visibles |
| FEFO y vencimiento | Reduce material desperdiciado | Datos de lote y saldo compatibles |
| Conciliación de merma | Diferencia error, consumo y pérdida confirmada | Evidencia/cierre humano; sin acusaciones automáticas |
| Indicador ambiental | Vincula ahorro de transporte con sostenibilidad | Factor, fuente, combustible y límites verificables; no inventar CO₂ |
| Piloto empresarial concreto | Hace creíble la continuidad | Plan de cuatro semanas e interés documentado |
| Sello de integridad verificable | Facilita auditar historial exportado | Digest protegido y alcance de garantía comprobado |

**Orden recomendado:** cuatro plus prioritarios → evidencia de usuario/mentor y piloto → what-if/FEFO → ambiental/integridad avanzada.

## En qué no invertir antes del cierre

Blockchain, múltiples agentes IA, interfaces decorativas o modelos propios sin datos. También evitar una arquitectura difícil de explicar que no cambie el resultado de una entrega.

## Conectar con evaluación

| Criterio | Evidencia del plus |
|---|---|
| Comprensión | Usuario reconoce urgencias y problemas de conciliación |
| Innovación | Riesgo explicado se convierte en acción y custodia |
| Viabilidad | Fallo de red/proveedor manejado, contratos y despliegue |
| Impacto | Comparación reproducible y próximo piloto |
| Demo | Caja/QR, captura offline y cambio de ruta real en el sistema |
| Presentación | Historia de una sola operación con números claros |
| Equipo | Registro de decisiones, feedback y cambios del evento |

## Criterio de elección final

Una mejora entra al pitch si funciona, cabe en el tiempo confirmado y su evidencia está disponible. La demo del flujo integrado tiene prioridad sobre mostrar todos los extras.

Próximas notas: [[Guion del pitch]], [[Plan de validacion]] y [[Checklist y contingencias]].
