---
tipo: fuente-resumida
estado: vigente
actualizado: 2026-10-07
tags: [oficial, hackathon]
---

# Reto 1 oficial

**Fuente primaria:** [[Manual oficial.pdf]], p. 19, Anexo 3.

**Nombre:** Optimización y trazabilidad logística para el abastecimiento de insumos a nivel nacional.

**Área:** Gestión Inteligente de Procesos.

## Texto literal del manual (p. 19)

Copiado sin cambios. Ante cualquier duda, este texto prevalece sobre los resúmenes de la bóveda.

> **Planteamiento del Problema**
> Las empresas de servicios de limpieza enfrentan sobrecostos, mermas e interrupciones en la entrega puntual de insumos (químicos, consumibles, herramientas) a múltiples puntos de servicio repartidos a nivel nacional.
>
> **Desafío para los participantes**
> Diseñar un sistema inteligente de logística y gestión de inventarios que permita planificar rutas eficientes, predecir puntos de reabastecimiento crítico según la demanda de cada cliente y garantizar la trazabilidad de insumos desde el almacén central hasta la entrega final en cada punto de servicio.
>
> **Resultado esperado:** herramienta o prototipo demostrable que permita visualizar la trazabilidad de insumos y mejorar la planificación de abastecimiento. La validación deberá mostrar, en la medida de la información disponible, evidencia de mejora en rutas, inventarios, tiempos o continuidad del servicio.

**Alcance sectorial del área** (tabla p. 4, transcrita de imagen): retos comprendidos «logística; cotización; trazabilidad; optimización de operaciones y toma de decisiones»; enfoques «IoT, GPS, trazabilidad, algoritmos, analítica y herramientas digitales».

## Problema oficial, resumido

Las empresas de limpieza abastecen múltiples puntos nacionales y sufren sobrecostos, mermas e interrupciones en la entrega puntual de químicos, consumibles y herramientas.

## Desafío oficial, resumido

1. Planificar rutas eficientes.
2. Predecir puntos críticos de reabastecimiento según la demanda de cada cliente.
3. Garantizar trazabilidad del almacén central hasta la recepción final.

## Resultado y validación

Una herramienta o prototipo demostrable que visualice trazabilidad y mejore la planificación del abastecimiento. La validación debe mostrar, según los datos disponibles, evidencia de mejora en rutas, inventarios, tiempos o continuidad del servicio.

## Traducción a evidencia del equipo

| Necesidad oficial | Comportamiento propuesto | Evidencia a producir |
|---|---|---|
| Rutas eficientes | Seleccionar puntos críticos y asignar rutas factibles | Mapa, restricciones y comparación de km/costo |
| Reabastecimiento crítico | Calcular cobertura, lead time y prioridad | Alerta con datos de origen y cálculo |
| Trazabilidad completa | QR, custodia, cantidades y recepción | Línea temporal y prueba de entrega |
| Menos interrupciones | Simular políticas sobre la misma demanda | Quiebres, demanda no atendida y puntualidad |
| Mermas | Señalar anomalías y conciliar cantidades | Caso investigado y resultado |

## Límites

- Chat, DeepSeek, Terraform, autoscaling y UUID v7 son elecciones del equipo.
- El manual no exige AWS, IA, blockchain, app nativa ni una tecnología específica.
- Los 80 puntos, 90 días y cinco días de anticipación pertenecen al escenario propuesto.
- Los cinco indicadores del pitch son nuestra forma de medir el reto, no una lista obligatoria del manual.
- La infraestructura debe sostener el flujo; su complejidad por sí sola no demuestra impacto.

Ver [[Requisitos y aceptacion]] y [[Plan de validacion]].
