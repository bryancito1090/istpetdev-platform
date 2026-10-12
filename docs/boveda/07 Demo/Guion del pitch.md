---
tipo: demo
estado: propuesta
actualizado: 2026-10-07
tags: [demo, validacion, istpetdev]
---

# Guion del pitch

**Meta interna:** tres minutos. Duración oficial y tiempo de preguntas pendientes de confirmar en [[Consultas para la organizacion]]. Ajustar este guion a lo que informe la organización.

Protagonista: una empresa de limpieza; objeto conductor: una caja destinada a un hospital. El caso es sintético hasta contar con validación empresarial.

**Estructura oficial (Anexo 5.7, p. 25):** Problema → Solución → Prototipo / demo → Impacto → Viabilidad → Modelo de negocio → Próximo paso. El recorrido de abajo cubre las siete partes; viabilidad, modelo de negocio y próximo paso comparten el cierre de 20 segundos y deben nombrarse los tres.

**Audiencia:** cinco de los siete jurados no son perfiles técnicos (§11.3, p. 10). Cada tecnología mencionada debe ir unida a lo que cambia para la operación.

## Recorrido de 180 segundos

| Tiempo | Lo que contamos | Lo que mostramos |
|---|---|---|
| 00:00–00:20 | «Cuando el hospital llama por falta de desinfectante, la emergencia ya ocurrió» | Punto y problema operacional |
| 00:20–00:45 | «Detectamos riesgo antes del faltante y sabemos qué datos lo explican» | Cobertura, lead time, fecha/frescura y cantidad propuesta |
| 00:45–01:15 | «Planificamos una ruta viable y respondemos a un cierre de vía» | Tradicional/optimizada; incidente y nueva versión |
| 01:15–02:05 | «Esta caja conserva su custodia aunque el conductor pierda la señal» | QR, despacho, recepción offline y sincronización |
| 02:05–02:40 | «Comparamos ambas políticas bajo la misma demanda durante 90 días simulados» | KPIs, denominadores y manifiesto del run |
| 02:40–03:00 | «Cuesta [costo verificado] operarlo, lo paga [cliente] y el siguiente paso es un piloto acotado con una empresa» | Viabilidad, modelo de negocio y próximo paso (piloto) |

Total: 180 segundos. Ensayar tiempos reales: si QR/firma tarda más, recortar interacción, no fingir el resultado.

## Texto base

«Una empresa de limpieza atiende 80 puntos del país. En nuestro escenario, un hospital tiene cinco días de cobertura. Hoy una operación reactiva espera a la llamada; nuestra plataforma identifica el riesgo, explica stock y consumo, y propone abastecimiento dentro del tiempo necesario.

Aquí vemos la ruta, su capacidad y sus horarios. Si se bloquea una vía, conserva lo ya entregado y propone el cambio para las paradas pendientes.

Esta caja se escanea en bodega y queda asociada a su lote y cantidades. El conductor puede capturar la recepción sin señal. Al conectarse, la operación se registra una sola vez y la evidencia completa la trazabilidad.

Comparamos 90 días de demanda sintética idéntica para ambas políticas. Los resultados de esta ejecución son los que aparecen en pantalla y pueden exportarse.

Buscamos validar el sistema con un piloto de cuatro semanas, empezando por una bodega y un grupo de puntos.»

Personalizar con resultados medidos y tiempo confirmado. Decir «cinco días» solo si S-02 y el run lo sostienen.

## Campos que deben sustituirse antes del pitch

| Campo | Origen |
|---|---|
| Km/costo total y por entrega | Resultado validado del run |
| Ahorro absoluto y % | Fórmula con baseline y denominador |
| Quiebres evitados/nuevos | Comparación de claves punto–SKU–día |
| Puntualidad | Obligaciones comunes vencidas |
| Trazabilidad completa | Líneas entregadas con evidencia requerida |
| Anomalías/mermas | Alertas etiquetadas y casos conciliados por separado |
| Costos del sistema | Configuración/precios verificados |
| Validación de usuario | Entrevista/mentoría efectivamente documentada |

No usar porcentajes de ejemplo como resultados. Si una métrica está pendiente, cerrar la medición o retirarla del texto.

## Preguntas previsibles

- ¿La predicción viene de la IA? El cálculo numérico es verificable; el LLM explica y contextualiza.
- ¿La ruta es óptima? Es una heurística factible comparada con una política documentada; no prueba óptimo global.
- ¿Qué ocurre sin señal? Se captura localmente y se valida al sincronizar; conflictos quedan visibles.
- ¿Cómo evitan duplicados? operationId, transacción y control de cantidades/duplicados de negocio.
- ¿El ahorro es real? Declarar si corresponde a simulación, entrevista o piloto; no mezclar niveles.
- ¿Qué hace escalable el sistema? Servicios sin estado, límites/pools y diseño de tiempo real; mostrar solo pruebas efectivamente realizadas.
- ¿Cómo tratan privacidad y auditoría? QR limitado, evidencia privada y historial append-only con garantías declaradas.

## Interacción física

El jurado puede consultar el QR en su teléfono si acepta y hay conectividad. La prueba de firma puede hacerse con una persona del equipo y datos ficticios. Preparar consulta ya abierta y un dispositivo de respaldo; no consumir el pitch configurando un teléfono.

Ver [[Checklist y contingencias]], [[Plus para ganar]] y [[Plan de validacion]].
