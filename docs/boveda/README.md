# IstpetDev — Hackathon Expo Clean Ecuador 2026

Bóveda compartida de Obsidian para el **Reto 1: optimización y trazabilidad logística para el abastecimiento de insumos a nivel nacional**.

IstpetDev está formado por cinco integrantes con experiencia transversal. El trabajo se organiza por entregables y dependencias.

## Abrir la bóveda

1. Clona este repositorio con tu cuenta de GitHub autorizada:

   ```bash
   gh repo clone bryancito1090/istpetdev-hackathon-2026
   ```

2. En Obsidian, selecciona **Abrir carpeta como bóveda** y elige la carpeta clonada.
3. Empieza por [Bienvenido.md](Bienvenido.md) y el [mapa de la bóveda](00%20Inicio/Mapa%20de%20la%20boveda.md).

Los enlaces internos de las notas están preparados para Obsidian. El manual oficial se incluye en el repositorio.

## Documentación principal

| Tema | Nota |
|---|---|
| Esquema completo y estado personal | [Esquema de base de datos](04%20Datos%20y%20algoritmos/Esquema%20completo%20de%20base%20de%20datos.md) |
| Coordinación de la migración de 95 tablas | [Estado y tareas entre agentes](06%20Equipo/Coordinacion%20migracion%20schema95.md) |
| RBAC, configuración y plantillas | [Configuración del sistema](03%20Arquitectura/RBAC%20y%20configuracion%20del%20sistema.md) |
| Conexiones PostgreSQL | [DBeaver](03%20Arquitectura/Conexion%20PostgreSQL%20en%20DBeaver.md) |
| Revisión antes de lanzar | [Skill de prelaunch](08%20Skills/istpetdev-prelaunch.md) |
| Contexto común | [Contexto maestro](00%20Inicio/Contexto%20maestro.md) |
| Datos que no se reinterpretan | [Hechos canónicos](00%20Inicio/Hechos%20canonicos.md) |
| Reglas del reto | [Reto 1 oficial](01%20Oficial/Reto%201%20oficial.md) |
| Alcance | [Alcance y prioridades](02%20Producto/Alcance%20y%20prioridades.md) |
| Arquitectura | [Arquitectura del sistema](03%20Arquitectura/Arquitectura%20del%20sistema.md) |
| Infraestructura AWS | [AWS y Terraform](03%20Arquitectura/AWS%20y%20Terraform.md) |
| Pipeline CI/CD | [CI/CD y despliegue](03%20Arquitectura/CI-CD%20y%20automatizacion%20de%20despliegue.md) |
| Hardening Host | [Seguridad de servidores](03%20Arquitectura/Hardening%20y%20seguridad%20de%20servidores.md) |
| Frontend FSD | [Feature-Sliced Design](03%20Arquitectura/Frontend%20con%20Feature-Sliced%20Design.md) |
| Trabajo pendiente | [Backlog](06%20Equipo/Backlog.md) |
| Diferenciación | [Plus para ganar](07%20Demo/Plus%20para%20ganar.md) |
| Skills del equipo | [Catálogo](08%20Skills/Catalogo%20y%20plan%20de%20skills.md) |
| Fuente oficial | [Manual PDF](99%20Fuentes/Manual%20oficial.pdf) |

## Compartir cambios

Trabajamos los cambios en `develop` y los integramos en `main` cuando están listos. Antes de editar, actualiza tu copia:

```bash
git fetch origin
git switch develop
git pull --ff-only
```

Guarda los cambios de Obsidian, revisa `git status` y publica cambios pequeños en `develop`:

```bash
git add --all
git commit -m "docs: actualizar documentacion del equipo"
git push
```

Si la actualización indica que las ramas divergen, conserva tus cambios y resuelve la integración antes de publicar. Evita editar simultáneamente la misma nota sin coordinarlo.

La configuración común de Obsidian se versiona. `.gitignore` excluye las sesiones personales (`.obsidian/workspace*.json`), las preferencias del grafo (`.obsidian/graph.json`), los logs, las cachés, la papelera y los temporales. Estos archivos permanecen en el disco de cada integrante.

## Asistentes de IA

Las skills y los archivos de configuración de asistentes (`AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, `.agents/`, `.claude/`) son locales y no se versionan; están documentados en el [catálogo de skills](08%20Skills/Catalogo%20y%20plan%20de%20skills.md). Antes de publicar cambios, verifica la bóveda:

```bash
python .github/scripts/check_vault.py
```

## Estado del proyecto

Esta es la base documental y de diseño. Los avances de software y las pruebas se registrarán con evidencia. Los datos sintéticos, resultados simulados y requisitos oficiales se distinguen en las notas.

Capacitación: **8 de octubre de 2026**. Jornadas: **16 y 17 de octubre de 2026**. Las consultas sobre trabajo previo y entrega están en [Consultas para la organización](01%20Oficial/Consultas%20para%20la%20organizacion.md).

## Acuerdos de implementación — 7 de octubre de 2026

El repositorio de software es [istpetdev-platform](https://github.com/bryancito1090/istpetdev-platform), privado y vacío. Su esqueleto está únicamente en `~/Projects/ISTPETDEV`; todavía no hay proyectos generados ni archivos publicados allí.

- [Repositorio y versiones](03%20Arquitectura/Repositorio%20de%20software%20y%20versiones.md): .NET 8, Angular 22 y estructura local.
- [Entornos y operación](03%20Arquitectura/Entornos%20y%20operacion%20acordados.md): aplicaciones locales, RDS compartido privado, AWS para ensayos, red/IAM, backups/S3, n8n existente, observabilidad y releases.
- [Identidad OIDC](03%20Arquitectura/Identidad%20OIDC%20y%20sesiones.md): ADR-10 Cognito y sesiones offline.

El contenido del compañero incorporado en `4c3a9ef` se conserva; las notas reciben complementos fechados con los acuerdos posteriores. Cuenta/presupuesto confirmados. Dominio institucional y workflows/Compose siguen a cargo de los compañeros indicados; credenciales por cargar por Bryan. Documentación y esqueleto no representan un despliegue real.

## Credenciales y acceso del equipo — 7 de octubre de 2026

La guía de [credenciales y acceso del equipo](03%20Arquitectura/Credenciales%20y%20acceso%20del%20equipo.md) aplica a esta bóveda y al repositorio de software. Cada integrante usa su propia sesión AWS SSO/MFA; los secretos de desarrollo necesarios se obtienen de Secrets Manager con permisos individuales. `.env.example` y la documentación incluyen solo nombres/valores públicos. No compartir claves AWS, contraseñas PostgreSQL ni tokens de integraciones mediante commits, issues o conversaciones.

Se ampliaron las exclusiones locales de Git; no sustituyen revisar el contenido antes de publicar ni revocar una credencial si se filtró. El alta AWS, los secretos y la configuración OIDC del CI siguen pendientes de implementación (B-35/V-32).

La [guía de puesta en marcha](03%20Arquitectura/Puesta%20en%20marcha%20del%20workspace%20y%20AWS%20dev.md) incluye el prompt para preparar/publicar ISTPETDEV y los pasos de consola/terminal para aprovisionar dev. Se propone bootstrap persistente de state por consola y red/RDS desde Terraform; scripts, instalaciones y recursos siguen pendientes de ejecución.

**Escenario posterior vigente:** leer [AWS temporal y cierre](03%20Arquitectura/AWS%20temporal%20para%20la%20hackathon%20y%20cierre.md) antes del prompt/guía anterior. Para aprovechar créditos, se propone IAM individual/MFA con `aws login` en la cuenta independiente, sin crear Organizations, y retiro de la infraestructura del proyecto al terminar la hackathon, incluido bootstrap al final. Configuración y cierre siguen pendientes de ejecución.
