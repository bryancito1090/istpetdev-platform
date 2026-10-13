---
tipo: especificacion-producto
estado: propuesta
actualizado: 2026-10-12
tags: [producto, reto-1]
---

# Modelo de negocio y piloto

**Estado:** hipótesis para validar con usuarios y mentores; no representa un acuerdo comercial.

## Cliente y beneficiarios

Comprador propuesto: empresa de servicios de limpieza con operación distribuida. Decisor posible: operaciones o gerencia. Usuarios: planificación, bodega y conductores. Beneficiarios: puntos que mantienen servicio y equipos que evitan urgencias.

## Propuesta comercial

Piloto acotado sobre una bodega y un conjunto de puntos. Medir gastos de transporte, quiebres, puntualidad y trazabilidad antes de proponer una expansión.

Ingreso propuesto: implementación inicial y suscripción por operación/puntos activos. El precio se decide después de validar ahorro, disposición de pago y costo real de soporte; no asignar un precio ficticio como dato confirmado.

## Costos a levantar

| Componente | Evidencia necesaria |
|---|---|
| Hosting: Perfil A (EC2 + Docker, demo y piloto según ADR-09) o Perfil B (ECS/ALB/RDS/Redis, operación nacional) y n8n | Región, horas, tamaños y configuración; Perfil A t3.medium ≈ USD 37/mes verificado el 7-oct-2026 en [[AWS y Terraform]] |
| S3 y transferencia | Cantidad/tamaño de evidencias, retención y tráfico |
| Mapas | Proveedor, matrices, rutas y llamadas por día |
| DeepSeek | Modelo, tokens, frecuencia y reintentos |
| Desarrollo/soporte | Horas de integración, capacitación y operación |
| Operación logística | Costo/km, conductor, urgencias y penalizaciones documentadas |

No incluir estimaciones de AWS o APIs sin fecha y fuente verificadas. El presupuesto sigue pendiente en [[Riesgos y decisiones pendientes]].

## Modelo económico

- Ahorro operativo = costo tradicional − costo con plataforma para un nivel comparable de servicio.
- Beneficio neto = ahorro operativo − costo incremental del sistema y su operación.
- ROI = beneficio neto / inversión relevante; especificar período y qué integra la inversión.
- Separar ahorro simulado de ahorro observado en una empresa.

Una ruta más corta puede generar peor puntualidad; no vender ahorro si traslada el costo a faltantes o a más horas de trabajo.

## Piloto propuesto de cuatro semanas

1. Semana 1: observar y conciliar inventario/rutas actuales sin cambiar la operación.
2. Semana 2: ejecutar recomendaciones en paralelo, con aprobación humana.
3. Semana 3: operar un grupo pequeño con trazabilidad y revisión diaria.
4. Semana 4: comparar resultados, incidencias y carga de trabajo; decidir expansión.

Tamaño del grupo y metas se acuerdan con la empresa. La evaluación del hackathon puede usar simulación; no presentarla como piloto ya realizado.

## Preguntas de entrevista

- ¿Cuánto tiempo tarda hoy en enterarse de un faltante?
- ¿Cómo calcula reposición y quién aprueba urgencias?
- ¿Qué insumos tienen vencimiento o condiciones especiales?
- ¿Cómo se conoce el costo por ruta y la cantidad realmente recibida?
- ¿Qué registro de consumo y conectividad existe en los puntos?
- ¿Qué resultado justificaría pagar por un piloto?

Registrar respuestas y cambios con [[Plantilla evidencia]].

## Complemento propuesto — ficha de piloto y negocio del spec 006, 12-oct-2026

**Deciders PENDIENTE.** Comprador hipotético: empresa de limpieza con operación distribuida; decisor, necesidad y presupuesto se verifican en la feria y con una contraparte de piloto. El modelo de ingreso propuesto sigue siendo implementación inicial más suscripción por operación/puntos activos. **Precio:** [PENDIENTE: costos de soporte y disposición de pago]. No hay cliente comprometido, venta ni ROI observado.

| Campo de ficha | Registro para negociar antes de iniciar |
|---|---|
| Alcance | Una bodega y un grupo de puntos/SKU acordado por contraparte; acceso, consentimiento, responsables y duración de cuatro semanas. |
| Semana 1 | Observar rutas, inventario, demanda, urgencias, horas de trabajo y costos del proceso actual; registrar datos faltantes. |
| Semana 2 | Propuestas paralelas con aprobación humana; medir aceptación/rechazo y motivo, sin cambiar operación por defecto. |
| Semana 3 | Operar un grupo pequeño con QR, custodia y revisión diaria de conflictos, conexión y esfuerzo. |
| Semana 4 | Comparar contra baseline acordado y decidir continuar, cambiar o parar; registrar efecto negativo además del positivo. |
| Indicadores | Quiebres evitados y nuevos, demanda no atendida, costo de transporte y puntualidad, cada uno con unidad, período, numerador y denominador; trazar trabajo adicional. |
| Umbral de avance | [PENDIENTE: acordar con contraparte antes del piloto], incluyendo tolerancia a quiebres nuevos y carga operativa. |
| Recursos y costos | Nube, evidencias, mapa, IA, integración, capacitación y soporte con fuente fechada y uso real; no copiar costo de demo como costo de piloto. |

La simulación sintética de 005, aun si produce un rango verificable, solo justifica una hipótesis para este piloto. Las entrevistas de feria no fijan precio. [[SERCOP limpieza hospitalaria y sanciones]] documenta obligaciones posibles en fichas específicas; no convierte una interrupción de insumos en multa evitada.
