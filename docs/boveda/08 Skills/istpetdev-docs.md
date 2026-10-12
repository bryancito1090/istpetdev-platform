---
tipo: skill
estado: vigente
actualizado: 2026-10-07
tags: [istpetdev, ia, skills]
---

# IstpetDev — editar la documentación

**Cuándo usarla:** Editar la bóveda Obsidian de IstpetDev sin crear redundancia ni datos inventados. Usar al crear o modificar notas, registrar una decisión (ADR), una mentoría, una evidencia o una respuesta de la organización, al actualizar los hechos canónicos, al resolver una contradicción entre notas o antes de publicar cambios de documentación.

> Documentación de la skill `istpetdev-docs`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-docs` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio. Leer antes [[istpetdev-contexto]].

## Leer primero

- `00 Inicio/Convenciones y estados.md`: frontmatter, estados, tipos de afirmación y reglas contra la invención.
- `00 Inicio/Mapa de la boveda.md`: dónde vive cada tema.
- `00 Inicio/Hechos canonicos.md`: datos vigentes y contradicciones abiertas.

## Flujo para cualquier cambio

1. **Buscar la nota que ya trata el tema** (búsqueda de texto en la bóveda). Si el concepto ya está explicado, se edita esa nota y las demás solo la enlazan. No crear una segunda explicación.
2. **Cambiar primero la fuente.** Orden: manual o acta → ADR → `00 Inicio/Hechos canonicos.md` → notas que lo usan → skills afectadas.
3. **Frontmatter.** Toda nota tiene `tipo`, `estado`, `actualizado` y `tags`. Actualizar `actualizado` con la fecha del cambio (AAAA-MM-DD).
4. **Estado honesto.** `propuesta` no se convierte en `aceptado` sin Deciders; `aceptado` no se convierte en `validado` sin evidencia enlazada.
5. **Enlaces.** Dentro de la bóveda, enlaces de Obsidian con doble corchete y el nombre exacto de la nota. En `AGENTS.md`, `CLAUDE.md`, `GEMINI.md` y en las skills, rutas relativas (otros asistentes no resuelven wikilinks).
6. **Registrar el cambio** relevante en `99 Fuentes/Fuente y control documental.md` (tabla «Registro de versiones documentales»).
7. **Verificar** con el script (abajo) y corregir todos los errores antes del commit.

## Plantillas

| Para registrar | Plantilla |
|---|---|
| Decisión de arquitectura | `09 Plantillas/Plantilla ADR.md` (lleva Deciders y «reemplazado por») |
| Prueba, entrevista o simulación | `09 Plantillas/Plantilla evidencia.md` |
| Mentoría o respuesta de la organización | `09 Plantillas/Plantilla mentoria.md` |
| Historia de usuario | `09 Plantillas/Plantilla historia de usuario.md` |

Detalle de ADR, propuestas y PRD breves: sección «Referencia: adr-y-propuestas» de esta nota.

## Reglas

- Una decisión nunca se borra: se crea la nueva y la anterior pasa a «reemplazado por ADR-XX».
- Los IDs se definen solo en su nota dueña: RF/RNF en Requisitos, V en Plan de validacion, B y D en Backlog, S en Dataset, ADR en Decisiones, O en Consultas, R en Riesgos, EXP en Simulador, WF en n8n e IA. Las demás notas los citan.
- Datos del manual: citar página; si se copia una tabla-imagen, marcarla «transcrito de imagen, p. X».
- Código dentro de una nota: es ejemplo no probado salvo que enlace evidencia.
- No escribir datos personales, credenciales, IPs públicas ni rutas personales de un equipo.
- Las tablas de la bóveda pueden editarse en Obsidian por cualquiera: antes de reescribir una nota, releerla.

## Contradicción entre notas

No elegir una versión en silencio. Usar el protocolo de [[istpetdev-contexto]], añadir la fila en «Contradicciones abiertas» de `00 Inicio/Hechos canonicos.md` y escribir en ambas notas «contradicción abierta en» seguido del wikilink a la nota Hechos canonicos.

## Verificar

```bash
python .github/scripts/check_vault.py
```


El script falla con enlaces rotos, frontmatter incompleto, IDs fuera de su nota dueña o duplicados, rutas personales, bloques de código sin cerrar, conteos desactualizados en «Fuente y control documental» y skills no portables. Al cambiar el número de notas o enlaces, actualizar la línea «Verificación del…» de esa nota con los valores del resumen.

## Origen

Adaptada de las skills personales `tech-writer` (documentación del repositorio, no duplicar definiciones), `product-manager` (plantilla ADR con Deciders y estados) y `continuidad-narrativa` (no reparar contradicciones en silencio).

## Referencia: adr-y-propuestas

*ADR, propuestas y PRD breves*

Plantillas para registrar decisiones y propuestas en la bóveda. La plantilla de Obsidian equivalente está en `09 Plantillas/Plantilla ADR.md`.

### Cuándo usar cada una

| Documento | Cuándo | Dónde |
|---|---|---|
| ADR | Una decisión difícil de revertir: stack, versiones, topología, datos, seguridad, repositorio | Tabla y sección en `03 Arquitectura/Decisiones de arquitectura.md` |
| Propuesta (RFC breve) | Antes de un cambio con impacto en contratos, datos o infraestructura | Nota nueva con estado `propuesta` o comentario en el PR |
| PRD breve | Una funcionalidad nueva con usuario, requisitos y aceptación | `02 Producto/Requisitos y aceptacion.md` (filas RF/RNF) y `09 Plantillas/Plantilla historia de usuario.md` |

### ADR

```markdown
## ADR-XX — Título de la decisión

**Estado:** propuesta | aceptado | reemplazado por ADR-YY
**Fecha:** AAAA-MM-DD
**Deciders:** nombres o roles (obligatorio para «aceptado»)
**Origen:** acta, mentoría o instrucción de una persona identificada
**Registrado por (commit):** usuario de git y hash

### Contexto
Situación, requisito afectado (RF/RNF) y fuerzas en juego: técnicas, de negocio, de tiempo.

### Opciones
| Opción | Ventaja | Costo o límite |
|---|---|---|
| A | | |
| B | | |

### Decisión
La elección en una frase y su motivo concreto.

### Consecuencias
Positivas, negativas aceptadas y cambios en datos, contratos, UX, infraestructura, costos o pruebas.

### Alternativas rechazadas
| Alternativa | Razón |
|---|---|

### Verificación
Evidencia que permite aceptar la decisión y límites conocidos.
```

Reglas: un ADR no se borra; si cambia, se crea otro y el anterior pasa a «reemplazado por». Al aceptar, actualizar la tabla de ADR de `00 Inicio/Hechos canonicos.md`.

### Propuesta (RFC breve)

```markdown
## Propuesta: título

**Estado:** borrador | en revisión | aceptada | rechazada
**Autor:** nombre o rol · **Fecha:** AAAA-MM-DD

- Motivación: por qué y por qué ahora.
- Alternativas: mínimo dos, con ventajas y desventajas.
- Propuesta: qué se cambia y en qué notas, contratos o archivos.
- Riesgos y mitigación.
- Criterio de éxito verificable.
```

### Priorización MoSCoW

Para decidir alcance con poco tiempo:

| Funcionalidad | Must | Should | Could | Won't |
|---|:---:|:---:|:---:|:---:|
| Descripción breve | ✓ | | | |

«Must» corresponde a P0 en `02 Producto/Alcance y prioridades.md`. Lo que se marque «Won't» se registra como fuera de alcance, no se borra.

Origen: adaptado de la skill personal `product-manager` (plantillas PRD, RFC y ADR).
