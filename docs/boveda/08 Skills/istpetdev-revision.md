---
tipo: skill
estado: vigente
actualizado: 2026-10-09
tags: [istpetdev, ia, skills]
---

# IstpetDev — revisión, commits y pull requests

**Cuándo usarla:** Revisión de cambios de IstpetDev antes de commit, push o pull request. Usar para redactar mensajes de commit (Conventional Commits), preparar o revisar un PR hacia develop, aplicar el checklist de calidad, pruebas, seguridad y secretos, y comprobar que documentación, contrato y hechos canónicos siguen consistentes.

> Documentación de la skill `istpetdev-revision`. El archivo `SKILL.md` no se versiona en la bóveda: quien quiera usarla con su asistente de IA copia este contenido a la carpeta de skills de su herramienta (frontmatter con `name: istpetdev-revision` y la descripción de arriba). Catálogo y origen en [[Catalogo y plan de skills]].

Rutas relativas a la raíz del repositorio. Esta skill es la única definición de Conventional Commits del equipo; las demás la enlazan.

## Flujo de ramas

- Trabajo integrado en `develop`; se integra en `main` cuando está listo (`README.md`).
- Antes de empezar: `git fetch origin`, `git switch develop`, `git pull --ff-only`.
- Cambios grandes en una rama corta desde `develop`: `feat/<tema>`, `fix/<tema>`, `docs/<tema>`, `chore/<tema>`.
- PR hacia `develop` con la plantilla `.github/pull_request_template.md`. Al fusionar, **squash** para dejar un commit por tema.
- **No forzar el push** sobre `develop`, `main` ni ramas que otra persona use (`00 Inicio/Convenciones y estados.md`). Si hay divergencia, integrar conservando los cambios ajenos.
- No borrar ramas de otra persona sin su acuerdo.

## Conventional Commits

Formato:

```text
<tipo>(<alcance opcional>): <descripción en imperativo, 10 a 72 caracteres>

<cuerpo: qué cambia y por qué, con los IDs afectados>

<pie: BREAKING CHANGE, Refs, Co-authored-by>
```

| Tipo | Cuándo |
|---|---|
| `feat` | Nueva funcionalidad visible para usuarios o consumidores de la API |
| `fix` | Corrección de un defecto |
| `docs` | Solo documentación o bóveda |
| `refactor` | Cambio interno sin funcionalidad nueva ni corrección |
| `test` | Pruebas nuevas o corregidas |
| `chore` | Dependencias, configuración, skills, tareas de mantenimiento |
| `ci` | Workflows de integración y despliegue |
| `perf` | Mejora de rendimiento medida |
| `style` | Formato sin cambio de lógica |
| `revert` | Revierte un commit anterior |

No usar mensajes como «fix bug», «cambios», «WIP» o «update file». Alcances sugeridos: `boveda`, `skills`, `backend`, `web`, `movil`, `datos`, `infra`, `n8n`, `pitch`.

## Checklist antes de PR

Severidad de cada observación: 🔴 BLOCKER (no se fusiona) · 🟡 WARNING (corregir o registrar como deuda) · 🟢 SUGGESTION · ℹ️ INFO.

- [ ] `python .github/scripts/check_vault.py` termina sin errores.
- [ ] Ningún dato inventado: cifras, versiones, rutas y comandos tienen fuente o están en `00 Inicio/Hechos canonicos.md`.
- [ ] Contradicciones nuevas reportadas y registradas, no corregidas en silencio.
- [ ] Cada cambio de comportamiento cita su RF/RNF o ADR.
- [ ] Contrato actualizado antes que la implementación ([[istpetdev-contrato]]).
- [ ] Sin secretos, tokens, IPs públicas, datos personales ni rutas personales.
- [ ] Pruebas del camino feliz y de al menos un error; pasan en la máquina local antes del push (cuando exista código).
- [ ] Registro en `99 Fuentes/Fuente y control documental.md` si el cambio es relevante.

Checklist completo por categorías: sección «Referencia: checklist-revision» de esta nota. Principios de infraestructura y observabilidad para el piloto: sección «Referencia: infra-y-observabilidad» de esta nota.

Antes de declarar lista/exponer una versión o ejecutar su lanzamiento, usar [[istpetdev-prelaunch]]. Esta revisión aporta evidencia reutilizable; la skill nueva agrega únicamente los controles complementarios de runtime/configuración definidos en [[Verificacion de seguridad antes del lanzamiento]].

## Pruebas

- Unitarias para reglas puras; integración con base real (contenedor efímero) para comandos y consultas; extremo a extremo solo para el recorrido de la demo.
- Mocks solo para dependencias externas (mapas, LLM, S3); nunca para la lógica que se prueba.
- Datos de prueba deterministas (seed); nunca datos reales.
- Todo defecto corregido deja una prueba que lo habría detectado.
- Pruebas intermitentes se arreglan o se eliminan; no se repiten hasta que pasen.

## Seguridad del repositorio

- `.env` y secretos fuera de git (ya en `.gitignore`); variables documentadas en un `.env.example` sin valores reales.
- Escaneo de secretos antes del commit cuando exista el repositorio de código (por ejemplo `detect-secrets` o `gitleaks`; herramienta PENDIENTE).
- Protección de ramas en GitHub para `main` y `develop`: PR obligatorio, checks en verde, sin push forzado. Se configura en Settings → Branches.

## Prohibido inventar

- Resultados de pruebas que no se ejecutaron o comandos que no existen en el repositorio.
- Aprobaciones: un PR no se describe como revisado si nadie lo revisó.

## Origen

Adaptada de las skills personales `code-review-assistant` (Conventional Commits, severidades, checklist), `qa-testing-engineer` (pirámide de pruebas, datos deterministas, pruebas intermitentes), `devsecops-github-guardian` (secretos, protección de ramas), `gitlab-workflow-istpet` (asunto de 10 a 72 caracteres, plantilla de MR) y `tech-writer` (plantilla de PR). Se excluyó el commit único con `amend` y `--force-with-lease`, porque contradice la regla del equipo de no forzar el push.

## Referencia: checklist-revision

*Checklist de revisión por categorías*

Marcar cada ítem con ✅, ❌ o ⚠️. Un ❌ en trazabilidad, seguridad o datos inventados es BLOCKER.

**PR:** número y título · **Autor:** · **Revisor:** · **Requisitos o ADR:** · **Fecha:**

### 1. Trazabilidad y veracidad

| Ítem | Estado |
|---|:---:|
| Cada cambio de comportamiento cita RF/RNF o ADR | |
| Ninguna cifra, versión, ruta o comando sin fuente o fuera de los hechos canónicos | |
| Datos sintéticos y resultados simulados etiquetados | |
| Contradicciones nuevas reportadas y registradas | |

### 2. Arquitectura

| Ítem | Estado |
|---|:---:|
| Dominio sin dependencias de infraestructura | |
| Comandos y consultas separados | |
| Sin lógica de negocio en controladores ni componentes de UI | |
| DTOs en la API; entidades de dominio no expuestas | |
| Imports FSD hacia capas inferiores y por API pública | |

### 3. Datos e invariantes

| Ítem | Estado |
|---|:---:|
| Movimiento, saldo e idempotencia en una transacción | |
| Sin saldo negativo ni recepción mayor a lo despachado | |
| Migración reversible o riesgo documentado; ninguna migración aplicada fue editada | |
| Cantidades con unidad base | |

### 4. Calidad de código

| Ítem | Estado |
|---|:---:|
| Sin duplicación entre archivos del cambio | |
| Nombres en el lenguaje del dominio (tabla de nombres en código) | |
| Errores con tipos explícitos | |
| Sin código comentado ni código de depuración | |
| `TODO` y `PENDIENTE` con motivo | |

### 5. Pruebas

| Ítem | Estado |
|---|:---:|
| Camino feliz y al menos un error cubiertos | |
| Integración con base real si hay transacciones | |
| Pasan en la máquina local antes del push | |

### 6. Seguridad

| Ítem | Estado |
|---|:---:|
| Autenticación y autorización RBAC en cada endpoint o acción SignalR | |
| Entradas validadas | |
| Sin secretos, tokens, IPs públicas ni datos personales | |
| Logs sin datos sensibles | |

### 7. Rendimiento

| Ítem | Estado |
|---|:---:|
| Sin consultas N+1 | |
| Listados paginados | |
| E/S asíncrona | |

### 8. Documentación

| Ítem | Estado |
|---|:---:|
| Mensaje en Conventional Commits | |
| Contrato y bóveda actualizados si cambió la interfaz o un dato | |
| `check_vault.py` sin errores | |
| Skills sincronizadas si se tocaron | |

### Decisión

- [ ] Aprobado
- [ ] Aprobado con observaciones (WARNING documentados)
- [ ] Requiere cambios (hay al menos un BLOCKER)

Origen: adaptado de la skill personal `code-review-assistant` (checklist de code review).

## Referencia: infra-y-observabilidad

*Infraestructura y observabilidad*

Principios para revisar cambios de despliegue. El diseño de infraestructura vive en `03 Arquitectura/AWS y Terraform.md`, `03 Arquitectura/CI-CD y automatizacion de despliegue.md` y `03 Arquitectura/Hardening y seguridad de servidores.md`; esos documentos son ejemplos no probados hasta que exista evidencia.

### Para la demo del hackathon

- Imágenes Docker multi-etapa: compilar en una etapa y ejecutar en otra mínima.
- El proceso principal del contenedor no corre como `root`.
- `.dockerignore` excluye `.env`, `.git` y dependencias locales.
- Servicios internos (API, PostgreSQL, Redis, n8n) publicados solo en `127.0.0.1` o en la red interna de Docker.
- Health checks que comprueban dependencias (base de datos), no solo que el proceso responde.
- Rollback probado en el ensayo, con el tiempo medido y anotado; sin medición no se afirma «menos de 30 segundos».
- Respaldo de la base antes de migrar, y que el pipeline falle si el respaldo falla.

### Para el piloto (no bloquea la demo)

- Ambientes separados de desarrollo, staging y producción, con credenciales distintas.
- Infraestructura como código revisada en PR, igual que el software.
- Secretos en un gestor (por ejemplo AWS Secrets Manager), con rotación.
- Recursos etiquetados por proyecto y ambiente; autoescalado siempre con límite máximo.
- Despliegues sin interrupción (rolling o blue-green) cuando el servicio lo requiera.

### Observabilidad

- Logs estructurados en JSON con nivel, hora, `correlationId` y `operationId`.
- Métricas de errores y latencia p95/p99; el promedio oculta los peores casos.
- Cada error con contexto suficiente para reproducirlo.
- Nunca registrar contraseñas, tokens, firmas, fotos ni datos personales, tampoco en modo depuración.
- Pregunta de cierre para cada flujo crítico: si falla en producción, ¿quién se entera y con qué contexto?

Origen: adaptado de las skills personales `infra-devops-cloud` y `observability-performance`.
