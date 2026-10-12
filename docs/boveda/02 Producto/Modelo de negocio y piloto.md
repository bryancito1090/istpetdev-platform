---
tipo: especificacion-producto
estado: propuesta
actualizado: 2026-10-07
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
