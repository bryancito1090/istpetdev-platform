---
tipo: skill
estado: vigente
actualizado: 2026-10-07
tags: [istpetdev, ia, skills]
---

# IstpetDev — contexto y reglas contra la invención

**Cuándo usarla:** Contexto obligatorio del proyecto IstpetDev (Reto 1 del Hackathon Expo Clean Ecuador 2026, plataforma de abastecimiento, rutas y trazabilidad de insumos de limpieza). Usar al iniciar cualquier tarea en este repositorio (código, documentación, datos, infraestructura o pitch), cuando falte un dato, cuando dos fuentes se contradigan o cuando una afirmación necesite fuente.

> Documentación de la skill `istpetdev-contexto`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-contexto` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Esta skill fija qué es el proyecto y cómo actuar cuando falta información. Las demás skills del equipo la suponen.

Las rutas de esta skill son relativas a la raíz del repositorio.

## Leer primero

1. `AGENTS.md`: reglas comunes e índice de notas.
2. `00 Inicio/Hechos canonicos.md`: versiones, cifras, rutas, nombres en código, estado de ADR y contradicciones abiertas.
3. `01 Oficial/Reto 1 oficial.md`: texto literal del reto (p. 19 del manual).
4. `02 Producto/Alcance y prioridades.md`: qué es P0, P1 y P2.
5. `00 Inicio/Convenciones y estados.md`: estados de nota y tipos de afirmación.

## Hechos que no se cambian

- El reto, los entregables, los pesos de evaluación y las fechas salen del manual (`99 Fuentes/Manual oficial.pdf`). Sus resúmenes están en `01 Oficial/`.
- No existe todavía software implementado. Ninguna propuesta de arquitectura certifica que algo funcione.
- Las cifras del escenario (80 puntos, 90 días, cinco días de cobertura) son propuesta del equipo, no datos de una empresa.
- No hay resultados medidos. Todo ahorro, porcentaje o mejora es PENDIENTE hasta que exista una ejecución con seed y manifiesto.

## Prohibido inventar

No escribir como hecho, ni en código ni en documentos:

- Versiones, rutas de carpetas, puertos, comandos de build o test que no estén en `00 Inicio/Hechos canonicos.md`.
- Endpoints, eventos, tablas o nombres de entidades que no estén en `03 Arquitectura/Contratos API y eventos.md` o en `04 Datos y algoritmos/Modelo de datos.md`.
- Herramientas, permisos, servidores o integraciones que no se hayan verificado en este entorno.
- Cifras de costo, ahorro, rendimiento o disponibilidad sin fecha y fuente.
- Respuestas de la organización, entrevistas, mentorías o pruebas que no estén registradas.
- Quién tomó una decisión. Si un ADR no registra Deciders, se dice que están pendientes.

## Si falta información

1. Escribir `PENDIENTE` en el lugar exacto del dato.
2. Hacer **una** pregunta concreta, con las opciones que existan en la bóveda.
3. No avanzar con una suposición disfrazada de decisión. Si hay que avanzar, dejar el valor como supuesto visible (⚠️) y anotar cómo se validará.

## Clasificar cada afirmación

| Etiqueta | Uso | Certeza |
|---|---|---|
| Oficial | Cita página y sección del manual | ✅ |
| Información del equipo | Cita `99 Fuentes/Informacion inicial del equipo.md` | ✅ si está registrada |
| Propuesta | Explica resultado esperado y aceptación | ⚠️ |
| Supuesto | Indica cómo se validará y qué cambia si falla | ⚠️ |
| Medido | Enlaza ejecución, dataset, parámetros y resultado | ✅ |
| Pendiente | Registra el dato faltante y cuándo se resuelve | ⚠️ |

## Contradicciones

Si un dato nuevo choca con uno establecido, no corregirlo en silencio. Detener la tarea y reportar:

> **⚠️ Contradicción detectada**
> - Hecho nuevo: …
> - Hecho establecido: … (nota y sección)
> - Impacto: …
> - Opciones: 2 o 3 alternativas

Después, registrarla en la tabla «Contradicciones abiertas» de `00 Inicio/Hechos canonicos.md` y esperar la decisión.

## Verificar

Después de cambiar documentación o skills:

```bash
python .github/scripts/check_vault.py
```

## Origen

Adaptada de las skills personales `business-analyst-reglas-negocio` (certeza ✅/⚠️, «nunca inventar reglas», una pregunta a la vez) y `continuidad-narrativa` (protocolo de contradicción), y del patrón «no inventar herramientas ni permisos» de la skill `automation` de Antigravity.
