---
tipo: fuente
estado: vigente
actualizado: 2026-10-16
tags: [istpetdev, documentacion]
---

# Fuente y control documental

## Fuente oficial recibida

| Campo | Valor |
|---|---|
| Documento | Manual y documento operativo Hackathon Expo Clean Ecuador 2026 |
| Copia portable | [[Manual oficial.pdf]] |
| Páginas | 33, incluyendo portada |
| Ruta de origen | Carpeta de descargas de un integrante; ruta personal omitida. Nombre del archivo: «Manual HACKATON EXPO CLEAN Ecuador 2026.pdf» |
| Autor en metadatos | Coordinacion Jimcorpservi |
| Creación en metadatos | 23 de septiembre de 2026 |
| SHA-256 | ddcc6b738d0ee2c5c304c5b030686477fbc86f0e607442e70dfe3f55adc6672b |
| Revisión de esta bóveda | 6 de octubre de 2026 |

Se extrajo texto y se revisaron visualmente los cuadros de calendario, entregables, evaluación, jurado y premios. Las notas son resúmenes operativos, no una sustitución del manual.

La copia incluida evita depender de la ruta de Downloads de un integrante. La versión fue aportada por el usuario; no se presume que sea la última publicada.

## Fuentes internas

- [[Informacion inicial del equipo]]: propuesta tecnológica y narrativa recibida.
- Aclaración del 6 de octubre registrada por bryancito1090 (765c532): IstpetDev, cinco integrantes, experiencia transversal y sin reparto fijo de especialidades.
- Aclaración del 6 de octubre registrada por bryancito1090 (765c532): adoptar Feature-Sliced Design en frontend web.
- Instrucción del 6 de octubre registrada por bryancito1090 (5905073): comprobar RBAC y agregarlo si faltaba; modelo aceptado en ADR-14, implementación pendiente.
- [[Registro de trabajo previo y del evento]]: evolución y autoría.
- [[Registro de mentorias]]: respuestas y validación futuras.

## Fuentes técnicas primarias consultadas

Verificadas el **6 de octubre de 2026**; volver a comprobar al cerrar versiones/servicios.

| Tema | Fuente | Nota que aplica |
|---|---|---|
| Soporte .NET | [Política de Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy) | [[Backend y tiempo real]] |
| SignalR/Redis | [Backplane oficial](https://learn.microsoft.com/en-us/aspnet/core/signalr/redis-backplane?view=aspnetcore-10.0) | [[Backend y tiempo real]] |
| UUID v7 | [PostgreSQL 18](https://www.postgresql.org/docs/18/functions-uuid.html), [PostgreSQL 17](https://www.postgresql.org/docs/17/functions-uuid.html) | [[Decisiones de arquitectura]] |
| Angular offline | [Service workers](https://angular.dev/ecosystem/service-workers) | [[Movil offline y sincronizacion]] |
| Sincronización de fondo | [MDN](https://developer.mozilla.org/en-US/docs/Web/API/Background_Synchronization_API) | [[Movil offline y sincronizacion]] |
| ECS autoscaling | [AWS](https://docs.aws.amazon.com/AmazonECS/latest/developerguide/service-auto-scaling.html) | [[AWS y Terraform]] |
| PostGIS/RDS | [AWS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/Appendix.PostgreSQL.CommonDBATasks.PostGIS.html) | [[AWS y Terraform]] |
| Estado Terraform | [Backend S3](https://developer.hashicorp.com/terraform/language/backend/s3) | [[AWS y Terraform]] |
| Mapas | [Mapbox Matrix](https://docs.mapbox.com/api/navigation/matrix/), [Google Routes](https://developers.google.com/maps/documentation/routes/compute_route_matrix) | [[Rutas y sobrecostos]] |
| n8n | [Queue mode](https://docs.n8n.io/hosting/scaling/queue-mode) | [[n8n e IA]] |
| DeepSeek | [JSON Output](https://api-docs.deepseek.com/guides/json_mode/) | [[n8n e IA]] |
| FSD | [Overview](https://feature-sliced.design/docs/get-started/overview), [capas](https://feature-sliced.design/docs/reference/layers), [slices](https://feature-sliced.design/docs/reference/slices-segments), [API pública](https://feature-sliced.design/docs/reference/public-api) | [[Frontend con Feature-Sliced Design]] |
| Soporte Angular (7-oct-2026) | [Versiones con soporte](https://angular.dev/reference/releases) | [[Hechos canonicos]] |
| Costo EC2 e IPv4 (7-oct-2026) | [t3.medium us-east-1](https://calculator.holori.com/aws/ec2/t3.medium/us-east-1), [cargo IPv4 pública](https://aws.amazon.com/blogs/aws/new-aws-public-ipv4-address-charge-public-ip-insights) | [[AWS y Terraform]] |
| Sede publicada (7-oct-2026) | [Listado de Expo Clean Ecuador 2026](https://www.cantonfair.net/event/67411-ecuador-cleaning-expo-2026) | [[Hechos canonicos]] |
| Skills portables (7-oct-2026) | [Agent Skills en VS Code](https://code.visualstudio.com/docs/agent-customization/agent-skills), [Agent Skills en Cursor](https://cursor.com/docs/context/skills), [AGENTS.md entre herramientas](https://codex.danielvaughan.com/2026/04/07/agents-md-open-standard-cross-tool-portability) | `AGENTS.md`, [[Catalogo y plan de skills]] |

Estas fuentes verifican límites de plataforma y convenciones; no validan el ahorro de nuestro producto.

## Registro de versiones documentales

| Fecha | Cambio | Alcance |
|---|---|---|
| 2026-10-06 | Bóveda base, manual portable, requisitos y arquitectura | Documentación inicial, sin software implementado |
| 2026-10-06 | Identificación IstpetDev y trabajo flexible | Sin asignación fija de personas |
| 2026-10-06 | Frontend FSD aceptado y plus de competencia | Contexto, frontend, ADR-13 y catálogo de skills |
| 2026-10-06 | RBAC aceptado (registrado por bryancito1090, 5905073) | Matriz de seis roles en seguridad, ADR-14, datos/contratos, RNF-01, B-29 y V-24; sin software implementado |
| 2026-10-06 | Repositorio privado GitHub creado (bryancito1090) | bryancito1090/istpetdev-hackathon-2026; README y exclusión de sesiones personales de Obsidian |
| 2026-10-06 | CI/CD, hardening, AWS dual y frontend ampliados (commit 4c3a9ef, JorgeDoicela) | ADR-04, ADR-09 y ADR-15 aceptadas sin Deciders; 1.071 líneas en 15 notas; sin software implementado |
| 2026-10-07 | Bloques A (anti-invención) y C (skills portables) | [[Hechos canonicos]], manual transcrito, contradicciones marcadas, `AGENTS.md`, 8 skills en `.agents/skills/` y verificador `check_vault.py`; sin software implementado |
| 2026-10-07 | Integración de los bloques A y C con be0b127 (bryancito1090) | Hechos canónicos, AGENTS.md y skills actualizados con .NET 8, PostgreSQL 16, Angular 22, Cognito y repositorio istpetdev-platform; rutas `/home/…` sustituidas por `~/Projects/…`; ADR del complemento citados con «→» para no redefinirlos |
| 2026-10-07 | Skills y archivos de IA fuera de la bóveda | Se dejan de versionar `.agents/`, `.claude/`, `AGENTS.md`, `CLAUDE.md` y `GEMINI.md` (quedan en [[Catalogo y plan de skills]] como documentación, con una nota por skill en `08 Skills/`); el verificador pasa a `.github/scripts/check_vault.py` |
| 2026-10-08 | Skill istpetdev-dbflow incorporada a la bóveda | Flujo colaborativo de base de datos en RDS dev, aislamiento por integrante, túnel SSM y resolución de conflictos incorporado a `08 Skills/` y al catálogo |
| 2026-10-11 | Formalización y cierre de decisiones Frontend (ADR-04, ADR-05, ADR-17, ADR-18) | Jefe de Frontend y Bryan: Leaflet para mapas en shared/ui/map-view, Arquetipo B Técnico/Minimalista con Lucide Icons y tokens Tailwind, PWA offline con signature_pad e idb, cliente OIDC PKCE para Cognito |
| 2026-10-11 | Normativa cromática oficial Expo Clean Ecuador y tokens de diseño | Identidad gráfica canónica extraída del material oficial del evento (Clean Cyan `#00A7C4`, Expo Lime `#9EC200`, Energy Orange `#EA601F`) en [[Sistema de diseno y colores]] y configurada en Tailwind |
| Pendiente | Respuestas de capacitación | Actualizar reglas/alcance al recibirlas |

## Actualizar el manual

Conservar nueva versión sin sobrescribir silenciosamente la anterior. Registrar fecha, origen y hash; revisar cambios en retos, agenda, entregables y evaluación. Enlazar las notas afectadas y marcar qué versión sustenta el pitch.

## Verificación de la bóveda inicial

Revisión del 6 de octubre de 2026: **47 notas Markdown**, **252 enlaces internos resueltos**, metadatos YAML válidos, nombres de nota únicos y bloques de código balanceados. El hash de la copia del PDF coincide con el original recibido. Verificados los enlaces y referencias de FSD en contexto, arquitectura, ADR-13, requisitos, backlog, validación y catálogo de skills.

Esta revisión valida la documentación y navegación; las pruebas de software de [[Plan de validacion]] siguen pendientes de implementación.

Actualización RBAC del 6 de octubre: enlaces internos resueltos, metadatos YAML válidos, nombres únicos, bloques de código balanceados e IDs de ADR/backlog/validación sin duplicados. Revisión de formato con `git diff --check` sin errores. Las pruebas de permisos siguen pendientes de software.

## Fuente y actualización del 7 de octubre de 2026

Fuente de decisiones: instrucción directa de Bryan en la conversación del 7 de octubre. Confirmó cuenta/presupuesto, trabajo local con DB AWS compartida, .NET 8/Angular 22, n8n propio y creación de GitHub vacío/esqueleto local; delegó recomendaciones y pidió conservar el contenido incorporado por su compañero en `4c3a9ef`.

Se agregan [[Repositorio de software y versiones]], [[Entornos y operacion acordados]] e [[Identidad OIDC y sesiones]], con fuentes técnicas primarias fechadas y estados explícitos. Las notas anteriores se conservan y reciben complementos fechados; los ejemplos originales no sustituyen las condiciones posteriores de implementación. El repositorio software remoto se verifica vacío y el esqueleto permanece local. La validación documental revisa conservación del contenido anterior, enlaces, bloques y formato; no afirma pruebas reales de aplicaciones/AWS/n8n.

## Credenciales del equipo — 7 de octubre de 2026

Bryan pide usar AWS e integraciones en equipo sin exponer valores en la bóveda ni en el repositorio de software. Se agrega [[Credenciales y acceso del equipo]], con fuentes oficiales AWS/GitHub/Microsoft enlazadas junto a las decisiones: SSO/MFA, tipos de instancia Identity Center, acceso a Secrets Manager, configuración local y OIDC de Actions. Se documenta la verificación del subject con IDs inmutables indicado por GitHub para repositorios recientes. Se amplían exclusiones de Git y se agregan B-35/V-32; sin crear cuentas cloud, leer/cargar credenciales reales ni ejecutar pruebas AWS en esta actualización.

## Guía de preparación y aprovisionamiento — 7 de octubre de 2026

Se agrega [[Puesta en marcha del workspace y AWS dev]] por solicitud de Bryan: prompt del agente limitado a instalaciones locales, proyectos, IaC/scripts y publicación en develop; luego pasos de consola/terminal para identidad/bootstrap persistente, Terraform dev, PostgreSQL y acceso del equipo. Flujo propuesto y ejecución pendiente. Versiones npm seleccionadas verificadas en el registro oficial; pasos de consola contrastados con documentación primaria AWS/GitHub. No se crearon recursos ni se ejecutó el prompt durante esta entrega.

## Créditos y cierre temporal — 7 de octubre de 2026

Bryan aclara que quiere usar los USD 100 mostrados y retirar infraestructura después de la hackathon. Se agrega [[AWS temporal para la hackathon y cierre]], con advertencia de la consola/fuentes oficiales sobre expiración al crear Organizations, alternativa IAM/MFA con `aws login` y limpieza final. Captura indica término del plan gratuito el 21 de octubre, no prueba de expiración de cada crédito. Se actualiza precedencia de guías/prompt y B-35/V-32, con B-36/V-33 para cierre. No se registra ID de cuenta, no se accede a credenciales ni se ejecuta ninguna operación cloud.

## Verificación con el script

Verificación del 8 de octubre de 2026 con `check_vault.py`, después de incorporar la skill [[istpetdev-dbflow]]: **66 notas Markdown**, **492 enlaces internos resueltos**, frontmatter completo, IDs definidos solo en su nota dueña y sin duplicados, sin rutas personales y bloques de código balanceados. El conteo incluye la nota [[Hechos canonicos]], las seis notas nuevas de Arquitectura de be0b127 y las nueve notas de skills en `08 Skills/`; no incluye archivos locales de asistentes de IA. Desde esta fecha el conteo lo comprueba el script; actualizar esta línea cuando cambie.

## Ampliación de esquema y configuración — 9 de octubre de 2026

Fuente: solicitud directa de Bryan en esta conversación. La bóveda ya contenía modelo lógico, infraestructura y RBAC de seis roles fijos; se pide definir el esquema completo del sistema, hacer administrables roles/permisos/plantillas/parámetros y agregar revisión antes de lanzamiento sin repetir reglas de otras skills.

Se incorporan [[Esquema completo de base de datos]], [[RBAC y configuracion del sistema]], [[Conexion PostgreSQL en DBeaver]], [[Verificacion de seguridad antes del lanzamiento]] y [[istpetdev-prelaunch]]. ADR-16 registra ampliación de ADR-14, con RF-17/18/19, RNF-13/14, B-37 a B-40 y V-34 a V-39. El backend local inspeccionado contiene cinco entidades/migración inicial; no se consultó RDS ni se aplicaron migraciones durante esta entrega. El usuario de datos ya está documentado por bootstrap; la guía prepara su segunda conexión sin crear contraseñas nuevas.

Fuentes técnicas contrastadas: PostgreSQL 16, Microsoft ASP.NET Core 8, DBeaver y OWASP; enlaces específicos en las notas correspondientes. La skill complementa revisión/backend/frontend/dbflow, excluye del nuevo checklist sus reglas ya existentes y los mecanismos ajenos a la arquitectura elegida. Se genera una copia local validable y se enlaza desde las instrucciones locales del proyecto; no se presenta como gate CI/runtime ya implementado ni instalación global del asistente.

Validación del 9 de octubre: **71 notas Markdown**, **566 enlaces internos resueltos**, frontmatter, enlaces, bloques e IDs comprobados por el verificador. Pruebas de RLS/RBAC/runtime y despliegue continúan pendientes en el plan, no cerradas por documentación.

## Cierre de decisiones de Frontend — 11 de octubre de 2026

Fuente: acuerdo técnico liderado por el Jefe de Frontend junto a Bryan para habilitar la construcción de las interfaces de usuario web y móvil.
- **ADR-05 (Mapas):** Elección de Leaflet con OpenStreetMap para `shared/ui/map-view` y `widgets/route-map`, asegurando disponibilidad offline/local sin costo ni riesgo de cuotas/bloqueos de API keys en la demo.
- **ADR-04 (Arquetipo y UI Kit):** Ratificación del Arquetipo B Técnico/Minimalista (datos densos, estilo Linear/Vercel), Lucide Icons (`lucide-angular`) como sistema oficial de iconos y tokens semánticos de 3 capas en Tailwind CSS.
- **ADR-17 (Móvil Offline y Firma Digital):** Implementación de canvas táctil con `signature_pad` para recepción de insumos en hospital/bodega, persistencia local con IndexedDB (`idb`) e idempotencia con UUID v7.
- **ADR-18 (Cliente OIDC / Cognito PKCE):** Flujo seguro Authorization Code con PKCE S256 en Angular Standalone con inyección de Bearer tokens vía `AuthInterceptor`.

## Normativa cromática oficial Expo Clean Ecuador — 11 de octubre de 2026

Fuente: flyer promocional oficial y material de difusión de la Feria Internacional de Limpieza e Higiene Profesional — Expo Clean Ecuador 2026 (Centro de Exposiciones Quito). Se define formalmente [[Sistema de diseno y colores]], estableciendo los tokens semánticos en Tailwind CSS (Cyan `#00A7C4`, Lime `#9EC200`, Orange `#EA601F`, Slate `#2D3748`) para `apps/web` y `apps/mobile`, armonizados con [[Requisitos y aceptacion|RNF-09]] (accesibilidad multicanal de estados) y el Arquetipo B.

Validación del 11 de octubre: **72 notas Markdown**, **585 enlaces internos resueltos**, frontmatter, enlaces e integridad de bóveda comprobados por el verificador.


## Aplicación personal de schema95 y coordinación — 9 de octubre de 2026

Fuente: informe del agente de software compartido por Bryan y archivo privado de verificación leído por el agente de la bóveda, resumidos sin secretos en [[Coordinacion migracion schema95]]. Se registra CompleteSystemSchema95 aplicada en `istpetdev_dev_01`, historial inicial conservado y 95 tablas con ENABLE/FORCE RLS. La evidencia fue producida por el agente de software; el agente de la bóveda no repitió las consultas en RDS.

Se actualizan estado canónico, esquema, backlog y registro de preparación. La guía de dbflow conserva migraciones aplicadas incluso sin merge, corrige el perfil AWS de Bryan y distingue conectividad de autorización. El envío automático de la tarea al otro agente falló por falta de FUSE en el CLI de Orca; recepción del encargo, PR, pruebas funcionales e integración siguen pendientes. No se hizo commit, push, arranque de API ni operación cloud desde la sesión de la bóveda.

Validación de esta actualización: **72 notas Markdown**, **585 enlaces internos resueltos**. Los dos avisos de lenguaje de propuesta ya existentes se mantienen; el alcance de seguridad del sistema sigue sujeto a las verificaciones pendientes.

## Respuesta del agente de software y revisión de evidencia — 9 de octubre de 2026

Se recibió el archivo de respuesta del agente de ISTPETDEV y se comprobó localmente el commit `bc85792f5ae94eeda79f93482f61cb77ced3b5dd`. Las 35 rutas de código/esquema/pruebas conservan el contenido preparado; se actualizó la documentación técnica de resultados. El agente de software informó la publicación del PR #1 como borrador hacia `develop`.

Se leyeron las evidencias privadas de integridad en RDS y de aislamiento/integridad/PostGIS SQL en PostgreSQL 16/PostGIS descartable, conservando sus alcances separados. El hash del script de integridad coincide con el informe. No se repitieron estos ensayos ni se aprobó/fusionó el PR desde la bóveda. Resultados y pendientes del cliente .NET, HTTP e aislamiento RDS en [[Coordinacion migracion schema95]]. Integración continúa sin aplicar schema95.

Validación de la documentación tras esta respuesta: **72 notas Markdown**, **587 enlaces internos resueltos**; se mantienen únicamente los dos avisos de lenguaje ya existentes.

## Pruebas .NET en entorno descartable — 10 de octubre de 2026

Se revisaron la respuesta del agente de software, el commit 8c6791cc6bf5c730b0447d343897fef4fae0e4e7 y el verificador EF/Npgsql/NTS. Las cuatro huellas del registro privado coinciden con las fuentes actuales. Se registran PASS del pool secuencial de tamaño uno, materialización/consulta espacial y denegaciones directas del filtro/controlador, sin presentarlos como autenticación ni transporte HTTP. El ensayo no consultó RDS; no cambió producción ni migraciones. Alcance y pendientes en [[Coordinacion migracion schema95]]. La bóveda no repitió el ensayo ni aprobó el merge.

Validación del estado documental actualizado: **73 notas Markdown**, **607 enlaces internos resueltos**; permanecen los dos avisos previos de lenguaje de propuesta.

## Repositorio unificado — 12 de octubre de 2026

Fuente: instrucción directa de Bryan de integrar esta bóveda en `bryancito1090/istpetdev-platform`. ADR-20 reemplaza ADR-12 y registra `docs/boveda/` como ubicación vigente. Importación mediante Git subtree desde el commit documental `a2fdf1a`; su historial permanece alcanzable desde el historial del monorepo. La base de software utilizada es `2c53e3c` de `develop`.

Se conservan notas, adjuntos y configuración común de Obsidian. Se actualizan instrucciones de clonación, referencias de repositorio, flujo por PR y comandos de verificación. El workflow y la plantilla de PR se trasladan a `.github/` de la plataforma. Los archivos personales y cambios locales se preservan sin publicarlos. Esta verificación corresponde a la estructura y navegación documental; no ejecuta aplicaciones, migraciones ni despliegues.

Verificación de la integración: **73 notas Markdown**, **609 enlaces internos resueltos**, **0 errores**. Se mantienen los tres avisos de lenguaje de propuesta presentes en la bóveda original.

## Despliegue Perfil A Reforzado y persistencia en Amazon RDS — 12 de octubre de 2026

Fuente: implementación y validación en vivo del Perfil A Reforzado acordado por el equipo. Se incorpora [[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]] en `03 Arquitectura/`, documentando el cómputo stateless en AWS EC2 (`44.204.107.167`) desacoplado de la persistencia gestionada en Amazon RDS (`istpetdev-rds-dev` con PostgreSQL 16 + PostGIS) en la VPC del proyecto (`10.42.0.0/16`). Se audita la migración de las 95 tablas en la base central `istpetdev_integration` bajo el rol `migrator_integration`, las políticas RLS y la operatividad de los endpoints de observabilidad `/health/live` y `/health/ready`.

Verificación de la actualización: **74 notas Markdown**, **611 enlaces internos resueltos**, **0 errores**.

## Reestructuración propuesta y specs — 12 de octubre de 2026

Fuente: sesión de definición de un integrante del equipo con asistencia de Claude Code. Se incorpora [[Definiciones de producto y plan hasta el pitch]] con el objetivo norte, las definiciones D1–D8, los cortes de alcance, los guardarraíles contra el dataset del evento, los hitos y el plan B. Se registran ADR-21 a ADR-23 como propuestas (Deciders pendientes), las contradicciones correspondientes en Hechos canónicos, los riesgos R-14 a R-19, las entregas B-41 a B-46 y complementos fechados en alcance, plan de ejecución, plus y consultas. Los specs implementables viven fuera de la bóveda, en `docs/specs/`. Esta actualización es documental: no ejecuta código, migraciones ni despliegues.

Verificación de la actualización: **75 notas Markdown**, **643 enlaces internos resueltos**, **0 errores**. Se mantienen los tres avisos de lenguaje de propuesta ya existentes.

## Spec 003 del agente logístico — 12 de octubre de 2026

Fuente: brainstorming del spec 003 en la sesión de definición. Se actualiza D4 en [[Definiciones de producto y plan hasta el pitch]] (DeepSeek con prueba de aceptación, sin comparación de modelos) y se añaden las decisiones de diseño del agente; complemento de ADR-22 y nota en la fila de ADR-08; fila de IA, nombre `AgentRun` y contradicción de scopes en Hechos canónicos; complementos en n8n e IA e Identidad OIDC y sesiones; riesgos R-20 a R-22; criterio de B-44. Actualización documental: no ejecuta workflows, Terraform ni código.

Verificación de la actualización: **75 notas Markdown**, **648 enlaces internos resueltos**, **0 errores**. Se mantienen los tres avisos de lenguaje de propuesta ya existentes.

## Propuesta documental del spec 004 — 12 de octubre de 2026

Fuente: brainstorming de campo con decisiones de preparación QR en bodega, recepción por conductor, conflicto por validez operativa, parciales bajo custodia del conductor y resumen público limitado. Se redactaron spec y plan de 004; se actualizaron las notas de producto, arquitectura, demo, hechos canónicos, backlog y riesgos. No se ejecutó el recorrido ni se registraron Deciders. El equipo redactará `task.md` y `test-e2e.md`.

Verificación de esta actualización: **75 notas Markdown**, **653 enlaces internos resueltos**, **0 errores**. Se mantienen los tres avisos de lenguaje de propuesta ya existentes.

## Revisión cruzada de los specs 001–004 — 12 de octubre de 2026

Fuente: revisión de los specs 001–004 tras completar el equipo las tareas y pruebas de 002–004. Correcciones propuestas: dueño del login PKCE y publicación HTTPS de la PWA (spec 004), endpoint de demo `advance` que reemplaza el marcado provisional de paradas en los recorridos de 002 y 003, prueba de tablas de 001 que exige el núcleo sin Schema95 en lugar de un conteo cerrado, token del QR en el fragmento de la URL, orden de recorte y código de pruebas clave del 004. Se actualizan [[Definiciones de producto y plan hasta el pitch]], Hechos canónicos, Contratos API y eventos, Identidad OIDC y sesiones, riesgos R-24 y R-25, B-47 y el registro de trabajo previo. Documental: no ejecuta código.

Verificación de la actualización: **75 notas Markdown**, **656 enlaces internos resueltos**, **0 errores**. Se mantienen los tres avisos de lenguaje de propuesta ya existentes.

## Propuesta documental del spec 005 — 12 de octubre de 2026

Fuente: brainstorming con el usuario y contratos de los specs 001–004. Se redactaron [spec 005](../../specs/005-panel-simulador/spec.md) y [plan 005](../../specs/005-panel-simulador/plan.md); se registraron baselines, demanda y escenario adverso propuestos en [[Simulador y metricas]], dependencias y hitos en [[Definiciones de producto y plan hasta el pitch]], riesgos y backlog, contradicciones en [[Hechos canonicos]] y preparación en [[Registro de trabajo previo y del evento]]. Los contratos de lectura faltantes se añadieron a los specs productores 002 y 004. PR #10 estaba fusionado antes de crear la rama del 005 desde `develop`. Esta actualización es documental: no ejecutó simulación ni implementó software.

Verificación de esta actualización: **75 notas Markdown**, **664 enlaces internos resueltos**, **0 errores**; permanecen tres avisos previos de lenguaje en notas no editadas por este spec.

## Tareas y E2E del spec 005 — 12 de octubre de 2026

Fuente: petición posterior del usuario de completar `task.md` y `test-e2e.md` del 005. Se redactan doce tareas T0–T11 con pruebas rojas/verdes, contratos, comandos y dependencias, más diez escenarios de punta a punta y una tabla vacía para registrar ejecuciones. Se actualizan [plan 005](../../specs/005-panel-simulador/plan.md), [tareas 005](../../specs/005-panel-simulador/task.md), [E2E 005](../../specs/005-panel-simulador/test-e2e.md), el índice de specs y el registro de preparación. La revisión E2E detectó que el reset de la tarea T7 de 001 borraba resultados de simulación; se corrigieron documentalmente FR-15, el plan y la tarea de 001, y se anotó la contradicción. No se implementó el sistema ni se ejecutaron esas pruebas; las reglas y resultados siguen como propuesta hasta aprobación y evidencia.

Verificación de esta actualización: **75 notas Markdown**, **664 enlaces internos resueltos**, **0 errores**; permanecen tres avisos previos.

## Revisión del spec 005 — 12 de octubre de 2026

Fuente: revisión del spec 005 tras su integración. Correcciones previas a cualquier corrida: las tres políticas secuencian paradas con el planificador de 002 y `fixed7-v1` escalona sus visitas por día laborable; la corrida oficial se ejecuta en una máquina dedicada y su ZIP se importa en la demo con verificación de hashes (FR-21); las lecturas de rutas y entregas quedan en las tareas de 002 y 004. Se actualizan [[Simulador y metricas]], [[Definiciones de producto y plan hasta el pitch]], el riesgo R-28 y el registro de trabajo previo. Documental: no ejecuta código.

Verificación de la actualización: **75 notas Markdown**, **666 enlaces internos resueltos**, **0 errores**. Se mantienen los tres avisos de lenguaje de propuesta ya existentes.

## Propuesta documental del spec 006 — 12 de octubre de 2026

Fuente: brainstorming con el equipo sobre validación, pitch y paquete. Se redactaron los cuatro archivos del [spec 006](../../specs/006-validacion-pitch-entrega/spec.md), guion 3/5/7, hoja de entrevista consentida, banco de veinte preguntas, ficha de piloto, matriz de afirmaciones y contingencias. La investigación [SERCOP](SERCOP%20limpieza%20hospitalaria%20y%20sanciones.md) consultada el 12-oct encuentra obligaciones y sanciones en fichas oficiales, pero no un costo observado de quiebre ni ahorro atribuible al proyecto. Son propuestas con Deciders pendientes; no hay entrevistas, ensayos, aprobación de CE de 001–005 ni materiales entregados.

Verificación de esta actualización: **76 notas Markdown**, **676 enlaces internos resueltos**, **0 errores**. El aviso previo de lenguaje en el guion fue corregido; permanecen dos avisos anteriores en notas no editadas por este spec.

## Runbook de DevOps y base de datos — 12 de octubre de 2026

Fuente: revisión de `develop` (3cb6861) y `main` (88a2221), de `deploy.yml`, del compose de producción, del Terraform de `dev`, de `scripts/backup/backup-db.sh` y de los specs 001–005, pedida por el usuario.

**Hallazgos**
- Ningún commit corrige la conexión a RDS (`Trust Server Certificate=true`) ni Swagger público.
- El host de la app está fuera de Terraform y comparte el security group con el nodo del túnel SSM.
- El código del SG no refleja la salida real.
- `backup-db.sh` respalda la base `postgres` en el disco del host.

**Cambios en la bóveda**
- Se crea [[Runbook de DevOps y base de datos para la demo]], en borrador.
- Se añaden complementos fechados a las notas que presentaban Schema95 con RLS como vigente, más los riesgos R-32 y R-33 y una nota en B-32, RNF-13 y V-37.

**Issues abiertas**
- [#14](https://github.com/bryancito1090/istpetdev-platform/issues/14): TLS, Swagger, SSH y desalineación con Terraform. Asignada a Jorge Doicela.
- [#15](https://github.com/bryancito1090/istpetdev-platform/issues/15): respaldo y restauración antes del reset. Asignada a Bryan Baño.
- [#16](https://github.com/bryancito1090/istpetdev-platform/issues/16): validar el runbook único. Asignada a Jorge Doicela.

Es una actualización documental: no ejecuta Terraform, ni migraciones, ni respaldos. No se comprobó el estado real de AWS; los identificadores de recursos vienen de la nota Perfil A y del estado de despliegue del 8-oct.

Verificación de esta actualización: **77 notas Markdown**, **714 enlaces internos resueltos**, **0 errores**. Se mantienen dos avisos previos de lenguaje.

## Implementación y verificación local del spec 001 — 12 de octubre de 2026

Fuente: instrucción directa de Bryan para implementar y verificar el spec 001 y su núcleo de 27 tablas en la rama `feature/001-nucleo-delgado-importador`, aplicando [[istpetdev-prelaunch]] en entorno local aislado. Se registran las diferencias D1–D9 del plan como alcance técnico local:
1. **D1/D2:** Roles funcionales fijos y omisión de RLS para el escenario acotado de demo de una sola organización. RNF-13 y el modelo multiempresa se preservan para el producto general.
2. **D3:** Conservación íntegra de las migraciones históricas `InitialSchema` y `CompleteSystemSchema95` (verificadas 7/7 con SHA-256 idénticos y tag `schema95-archivo`); la transición se realiza mediante una migración posterior `DemoCore` con guard que bloquea bases con datos ajenos a los seeds.
3. **D4:** Cantidades `numeric(18,3)` y coordenadas `geometry(Point, 4326)` en el núcleo delgado.
4. **D5:** Autenticación y scopes técnicos M2M preservados; resolución de cuenta y rol en base de datos local descartable.
5. **D6:** Importación de consumo histórico por SKU y período con conversión estricta y sin doble descuento sobre saldos absolutos.
6. **D7:** Ventanas operativas acotadas al importador; contratos de optimización logística diferidos a specs subsiguientes (002–004).
7. **D8:** Conservación de RNF-14/B-40; B-29 se cierra únicamente en su alcance verificado con roles fijos locales, sin acreditar login web/PWA o SignalR.
8. **D9:** No se inventa aprobación colectiva del equipo ni confirmación de la organización para O-01; PA-02 sigue formalmente pendiente.

Resultados verificados en Release: 115 pruebas aprobadas (66 Unit, 48 Integration en PostGIS descartable, 1 E2E), `npm run check` aprobado, y contenedor backend `istpetdev-backend:spec001` construido y verificado con usuario no root y fixtures integrados. No se realizaron cambios ni consultas en Amazon RDS, AWS Cognito real ni despliegues expuestos.

Verificación documental de esta actualización: **77 notas Markdown**, **715 enlaces internos resueltos**, **0 errores**.

## Análisis de cobertura y avance de specs — 12 de octubre de 2026

Fuente: petición de Bryan de analizar a fondo los specs frente a los objetivos y límites de la bóveda. Se crea [[Analisis de cobertura de specs]] y se enlaza desde el mapa: ponderación propuesta de 001–006 para la demo acotada, evidencia local de 001, pendientes dentro de los specs actuales y posibles specs de piloto/P2 descritos solo por encima. Se contrastaron los contadores de los TRX existentes (115 pruebas aprobadas), sin reejecutar la aplicación ni consultar AWS o las issues remotas.

Los porcentajes son una estimación de capacidades, no una aprobación del alcance ni una declaración de versión lista. No se cambian requisitos, ADRs ni estados de cierre; no se crean specs futuros ni se realizan cambios de software o despliegue.

Verificación documental de esta actualización: **78 notas Markdown**, **736 enlaces internos resueltos**, **0 errores**. Se conservan dos avisos previos de lenguaje.

## Seguimiento integrado y continuidad del piloto — 12 de octubre de 2026

Fuente: instrucción de Bryan de dedicar esta sesión a planificación exhaustiva para el equipo del 13-oct, pruebas tras los seis specs, registro de faltantes y continuidad hacia 007–012. Se crean [[Seguimiento de implementacion y pruebas]], [[Registro de seguimiento y hallazgos]] y [[Continuidad del piloto y specs 007 a 012]]; se enlazan desde README, Bienvenido, mapa, índice de specs y planes del equipo. La guía separa pase técnico y cierre del evento, reutiliza los E2E existentes y mantiene el 008 mínimo apoyado en el runbook.

Ante la consulta de Bryan sobre RBAC, se contrastaron los roles fijos del código y el núcleo sin RLS con las notas. Se documenta que RBAC administrable/RLS pertenecen al diseño diferido, mientras autorización por rol/asignación y RNF-14 siguen vigentes. La copia local de prelaunch se regeneró desde [[istpetdev-prelaunch]] para conservar el complemento de alcance que faltaba; no se modifica ni se versiona como segunda fuente editable.

Planificación documental: no se ejecutaron aplicación, suites funcionales, ensayos, migraciones ni despliegues; no se crean los specs detallados de 007–012 ni se acreditan cierres anticipados. Se comprobaron 94 enlaces Markdown locales de las entradas y guías, sin enlaces rotos.

Verificación documental de esta actualización: **81 notas Markdown**, **793 enlaces internos resueltos**, **0 errores**. Se conservan dos avisos previos de lenguaje.

## Estrategia de tres entornos y ciclo de vida de persistencia — 13 de octubre de 2026

Fuente: instrucción directa de Jorge/Bryan de formalizar la estrategia de tres entornos de persistencia (Casa / Standalone, Laboratorio / On-Premise en red local `192.168.7.50:15432`, y Producción / AWS RDS `istpetdev_integration`) para eliminar la fricción operativa durante el desarrollo del hackathon. Se crea [[Estrategia de tres entornos y base de datos]] y se enlaza desde [[Entornos y operacion acordados]] y [[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]].

Verificación documental de esta actualización: **82 notas Markdown**, **808 enlaces internos resueltos**, **0 errores**. Se conserva un aviso documental previo de lenguaje.


## Asignación de specs 004 y 005 a Alexander Martínez — 13 de octubre de 2026

Fuente: solicitud de Alexander Martínez de asumir la implementación de los specs 004 (Campo, QR y PWA Offline) y 005 (Panel Web y Simulador 90 Días), sincronización con Linear (tareas ALK-12 a ALK-35) y formalización del plan de trabajo en la bóveda. Se crea [[Plan de trabajo specs 004 y 005]], se actualiza el mapa de la bóveda, el registro de seguimiento y los metadatos de los specs 004 y 005.

## Integración de estrategia de tres entornos en develop — 13 de octubre de 2026

Fuente: integración de la rama `fix/ci-despliegue-inmutable` en `develop` con la estrategia de persistencia en tres entornos, conmutador `switch-db.mjs` y actualización de configuración predeterminada de base de datos para el equipo.

Verificación documental de esta actualización: **83 notas Markdown**, **812 enlaces internos resueltos**, **0 errores**. Se conserva un aviso documental previo de lenguaje.



## Segregación de roles por plataforma y ADR-24 — 13 de octubre de 2026

Fuente: solicitud de segregar roles y pantallas entre Web (Torre de Control) y Móvil (PWA de Campo), incorporar el rol y flujo ágil de Receptor Hospitalario y retirar accesos públicos prematuros. Se actualiza [[Usuarios y flujos]], [[Decisiones de arquitectura]] con ADR-24 y ADR-25, y se genera [[2026-10-13-informe-segregacion-roles-y-plataformas]].

## Culminación e integración fullstack de Spec 004 y rutas dinámicas — 13 de octubre de 2026

Fuente: solicitud de Alexander Martínez de actualizar la Bóveda con la finalización del Spec 004 y los avances de la rama `test` / `feature/004-campo-qr-custodia-offline`:
- Cierre formal de todas las tareas pendientes de Spec 004 (T0 a T10): migración `FieldOperations` en EF Core (`20261013202540_FieldOperations`), endpoints en controladores C# (`DeliveriesController`, `HandlingUnitsController`, `EvidenceController`, `MobileAssignmentsController`, `PublicTraceController`, `DemoController`), adaptador `HttpFieldApiService` conectando Web y Mobile a la API local (`http://localhost:5000`), outbox atómico hospitalario con acta formal, sanitización de token QR en `#t=` y limitador de tasa `PublicTraceRateLimiter`.
- Enriquecimiento del panel web de rutas (`rutas.page.ts`) y motor logístico (`RoutePlansController.cs`): cálculo dinámico de revisiones de planes de ruta, conteo real de paradas (`stopsCount`), detalle de paradas con nombres legibles de puntos de servicio y banner reactivo de retroalimentación.
- Formalización de **ADR-27** (Integración Fullstack de Operaciones de Campo, Adaptadores HTTP y Revisiones Dinámicas de Rutas), cierre de contradicciones en [[Hechos canonicos]], actualización de [[Plan de trabajo specs 004 y 005]], mitigación de riesgos en [[Riesgos y decisiones pendientes]], cierre de hallazgos SEG-03/04/05 en [[Registro de seguimiento y hallazgos]], y actualización de entregas B-06, B-07, B-08, B-11 y B-12 en [[Backlog]].




## Rol Auditor (ADR-26) y spec 013 de login — 13 de octubre de 2026

Fuente: sesión de definición del spec 013 (login real y gestión de cuentas por Gerencia) con Joseph Baño. Se agrega ADR-26 (rol Auditor de solo lectura, aceptada por Joseph Baño) en [[Decisiones de arquitectura]] y en [[Hechos canonicos]]. Fuera de la bóveda se actualizan los specs 004 (FR-17) y 005 (FR-05) para que el login dependa de 013, y la fila D3 del índice de specs.

Verificación documental de esta actualización: **84 notas Markdown**, **818 enlaces internos resueltos**, **0 errores**. Se conserva un aviso documental previo de lenguaje.

## Despliegue de base de datos de desarrollo en AWS EC2 y centralización en .env — 13 de octubre de 2026

Fuente: instrucción directa de Bryan/Jorge de centralizar toda la persistencia de desarrollo del equipo en AWS EC2 con PostgreSQL 16 y PostGIS 3.4 (`istpetdev_dev_01`) con usuario único `migrator_dev_01`, eliminando la dependencia del servidor de laboratorio LAN. Para cumplir con las directrices de seguridad y control de versiones, se centraliza la configuración en el archivo `.env` en la raíz (ignorado por Git), dejando `src/Api/appsettings.json` local y completamente excluido de Git junto con `appsettings.*.json` mediante `.gitignore`. A GitHub viajan únicamente `!.env.example` y `!src/Api/appsettings.example.json`. El script `scripts/ops/switch-db.mjs` se refactoriza para leer de `.env` y ocultar contraseñas en terminal. Se actualizan [[Estrategia de tres entornos y base de datos]], [[Conexion PostgreSQL en DBeaver]], [[istpetdev-dbflow]], [[Runbook de DevOps y base de datos para la demo]], [[Entornos y operacion acordados]], [[AWS temporal para la hackathon y cierre]], [[Esquema completo de base de datos]], [[Credenciales y acceso del equipo]] y [[Perfil A Reforzado - Despliegue Stateless y Amazon RDS]].

Verificación documental de esta actualización: **84 notas Markdown**, **829 enlaces internos resueltos**, **0 errores**. Se conserva un aviso documental previo de lenguaje.


## Workflows, copia local y desbloqueo M2M del spec 003 — 14 de octubre de 2026

Fuente: solicitud de Bryan de terminar #37, mantener `task.md` al día y verificar
la coherencia con la bóveda. Se documentan versión n8n 2.39.5, workflows locales,
borradores inactivos en el servidor con DeepSeek nativo y límites de aceptación
sin inventar resultados. Bryan revisó y autorizó el plan dirigido de #38;
se publicó en GitHub y aplicó con 1 añadido, 1 actualizado y 0 destruidos.
Se comprobó emisión del token automation; secretos solo en configuración local
ignorada. La API de aceptación usa una base descartable y puerto separado para
preservar el entorno compartido. Se detectó que Cognito no tenía usuarios humanos: Bryan autorizó crear su cuenta,
recibió invitación y completó registro/MFA; se obtuvo token por PKCE. M2M local
responde 200/403, pero Watch real termina Fallback agent_error. Bryan solicitó
el plan y la delegación de #37/T0–T10 a Antigravity; aceptación real pendiente.

Seguimiento: [task.md](../../specs/003-agente-logistico/task.md),
[[n8n e IA]] y [[Registro de seguimiento y hallazgos]]. ADR-22 conserva su estado
hasta aprobación del equipo; no se declara el proveedor ni producción validados.

Verificación documental: **84 notas Markdown**, **831 enlaces internos resueltos**, **0 errores**; un aviso previo de lenguaje, sin modificar su alcance.
## Saneamiento de dependencias y ADR-04 — 14 de octubre de 2026

Fuente: ejecución de spec 000 (Issue #44). Se actualiza ADR-04 en [[Decisiones de arquitectura]] para formalizar `@lucide/angular` y la remoción de dependencias sin uso (`lucide-angular`, `@angular/animations`).

Verificación documental de esta actualización: **84 notas Markdown**, **831 enlaces internos resueltos**, **0 errores**.

## Saneamiento de credenciales versionadas y defaults locales — 14 de octubre de 2026

Fuente: atención a Issue #34 e Issue #35 (punto 7) por Joseph Baño. Se retira `POSTGRES_PASSWORD: 12345` de `infra/compose/docker-compose.lab.yml`, forzando su definición mediante variable de entorno obligatoria desde `.env` no versionado. Se restituye `src/Api/appsettings.Development.json` configurado exclusivamente con los parámetros por defecto de Docker local (`localhost:15432`, base `postgres`, usuario `postgres`, contraseña `postgres_dev_password_local`) permitiendo compilación y ejecución offline inmediata sin credenciales sensibles. Se refactoriza `scripts/ops/switch-db.mjs` y `.env.example` para sincronizar los parámetros locales sin contraseñas fijas en el código.

Verificación documental de esta actualización: **84 notas Markdown**, **831 enlaces internos resueltos**, **0 errores**.

## Ruteo por pedidos, bodegas, rebalanceo y ADR-28 — 14 de octubre de 2026

Fuente: desarrollo e integración por Alexander Martínez y Bryan Doicela en la rama `develop` a través del PR #59 (commits `370ffa1` y `22d7985`, merge `d4f35c9`):
- Formalización de **ADR-28** (Ruteo Logístico Multiparada basado en Pedidos, Abastecimiento Multialmacén y Asesor de Rebalanceo) en [[Decisiones de arquitectura]], con soporte multialmacén y optimización física de flota.
- Migración EF Core `20261014144239_OrderBasedRoutingAndWarehouseStock` incorporando las entidades `DeliveryOrder`, `DeliveryOrderLine`, `WarehouseStockPolicy` y `TransferSuggestion`.
- Servicios de aplicación de lógica determinista: `SourcingService`, `VehicleMatcher`, `RouteEvaluator`, `RebalancingAdvisor` y `EmergencyPlanner`.
- Controladores y endpoints expuestos en API: `DeliveryOrdersController`, `WarehousesController` y ampliación de `RoutePlansController`.
- Interfaces de usuario en Angular FSD: nueva página `/bodegas` (`bodegas.page.ts`), rediseño guiado de `/rutas` (`rutas.page.ts`) con Leaflet y ampliación de `/datos`.
- Blindaje de pruebas de persistencia: actualización de `LeanSchemaTests` (36 tablas núcleo) y `MigrationSafetyTests` (7 migraciones consecutivas validadas sin DDL en runtime).
- Sincronización en la bóveda: actualización de [[Hechos canonicos]], [[Rutas y sobrecostos]], [[Modelo de datos]], [[Backlog]], [[Riesgos y decisiones pendientes]], [[Arquitectura del sistema]], [[Registro de seguimiento y hallazgos]] y [[Plan de trabajo specs 004 y 005]].

Verificación documental de esta actualización: **84 notas Markdown**, **848 enlaces internos resueltos**, **0 errores**.

## Revisión e integración local de #37 — 14 de octubre de 2026

Fuente: entrega de Antigravity y solicitud de Bryan de comprobar y completar
las casillas de spec 003. Bryan confirmó la aprobación del diseño/ADR-22; se
actualizan spec, plan, tareas y hechos canónicos. La revisión completa caminos
locales faltantes de incidente automático, fallback, contexto firmado, traza,
concurrencia y recorrido E2E. Evidencia y pendientes en
[revision-integracion-37.md](../../specs/003-agente-logistico/revision-integracion-37.md).
Producción permanece en #39; la aceptación del árbol final y CI aún se verifican.

Verificación documental: **84 notas Markdown**, **847 enlaces internos resueltos**,
**0 errores**; cuatro avisos de lenguaje previos, fuera de este cambio.
## Endurecimiento de CI/CD, flujo de persistencia y Plan B — 14 de octubre de 2026

Fuente: atención y cierre de Issue #35 por Joseph Baño y Jorge Doicela a través del PR #70 (commit `0c13909`, merge `d2b7850`). Se endurecen `.github/workflows/deploy.yml` y `rollback.yml` exigiendo verificación estricta de `HTTP 200` en `/health/ready` (eliminando fallbacks permisivos a `/health/live`). Se formaliza en `src/Api/Program.cs` la política Fail-Fast para abortar el arranque de la API ante fallos de migración. En [[Runbook de DevOps y base de datos para la demo]] y [[Evidencias y ensayos de base de datos y despliegue]] se incorporan la matriz de alcance CI vs RDS, el procedimiento de verificación de `__EFMigrationsHistory` vía túnel SSM, la política de rollback aditiva y el Plan B local del evento (Laptop Standalone + WiFi Hotspot) sustituyendo la dependencia del laboratorio LAN. Se sincroniza la prueba de integración `MigrationSafetyTests.cs` para validar 8 migraciones canónicas de EF Core tras la incorporación de `SimulationRunsAndEvents`.

Verificación documental de esta actualización: **85 notas Markdown**, **856 enlaces internos resueltos**, **0 errores**.


## Kit previo T1 del spec 006 y estado de O-04/O-06 — 14 de octubre de 2026

Fuente: issue #46 (fase 1), pedido de Joseph Baño; trabajo hecho por IA (Claude Code, Opus 5.5) sobre `develop` en la rama `feature/46-kit-previo-006`. Sin entrevistas, ensayos, cifras, aprobaciones ni horas del equipo nuevas.
- [[Registro de mentorias]]: hoja de entrevista con tres permisos separados (notas, cita sin nombre, mención de rol/organización) y preguntas 2 y 4 sin presuponer el hecho.
- [[Guion del pitch]]: aviso de que el complemento del 006 sustituye en propuesta el recorrido de 180 s; pregunta 11 del banco sobre escala.
- [[Modelo de negocio y piloto]]: resumen según Anexo 5.6; contradicción registrada sobre el costo de hosting (USD 37/mes sin RDS); fila de mapas alineada con ADR-05/ADR-23.
- [[SERCOP limpieza hospitalaria y sanciones]]: URL y citas comprobadas el 14-oct; cita literal en la fila de HGDC.
- [[Consultas para la organizacion]]: O-04 y O-06 siguen pendientes; se registra qué dice el manual y que las «Indicaciones para pitch y demostración» (§7.2, p. 7) no están en el repositorio.
- Seguimiento en [[Registro de trabajo previo y del evento]], [[Backlog]] (B-45) y, fuera de la bóveda, `task.md` (T1) y `test-e2e.md` (E2E-01) del spec 006. CE-01 queda pendiente de la revisión humana de unos 15 min.

Verificación documental de esta actualización: **85 notas Markdown**, **867 enlaces internos resueltos**, **0 errores**.

## T2 del spec 006: matriz de afirmaciones y guion — 14 de octubre de 2026

Fuente: issue #46 (fase 2), pedido de Joseph Baño; borrador hecho por IA (Claude Code, Opus 5.5) sobre `develop` en `7aed1eb`, rama `feature/46-fase2-guion-auditoria`. Sin entrevistas, ensayos, cronometrajes ni aprobaciones nuevas.
- [[Guion del pitch]]: «Versión de trabajo T2» con cortes 3/5/7 (metas internas 160/280/400 s), cinco papeles, etiquetas vivo previsto/grabado/simulado/propuesto, frases retiradas y estructura de siete diapositivas sin archivo.
- [[Consultas para la organizacion]]: O-04 y O-06 revisadas de nuevo; siguen sin respuesta con fuente.
- [[Registro de trabajo previo y del evento]]: fila del borrador T2.
- Fuera de la bóveda: `matriz-afirmaciones.md` y `evidencia/2026-10-14-suites-fase2.md` del spec 006 (105 pruebas automatizadas, todas pasaron); `task.md` (T2) y `test-e2e.md` (E2E-03 y E2E-05) con estado. La matriz registra dos contradicciones: 005 figura cerrado sin corrida oficial ni ZIP, y 004 figura concluido sin prueba de integración de despacho/recepción ni ensayo en teléfono.

Verificación documental de esta actualización: **85 notas Markdown**, **872 enlaces internos resueltos**, **0 errores**.

## Revisión de Joseph para el kit y el borrador del guion — 14 de octubre de 2026

Joseph Baño (Andrés) informó que revisó durante 15 minutos y aprobó los cinco componentes del kit T1 del PR #71 (`228ab72`), sin correcciones adicionales comunicadas. No informó horas de inicio y fin. CE-01 quedó registrado en `task.md` y E2E-01 del spec 006 y en [[Registro de trabajo previo y del evento]]; B-45 se actualizó en [[Backlog]]. Joseph también aprobó el borrador T2 (`8693f73`). La revisión posterior ajustó las afirmaciones A-16/A-17/A-27 de `matriz-afirmaciones.md` para limitar lo dicho a la prueba de API; faltan segundo revisor, PDF y ensayos para CE-03/CE-06.

Verificación documental de esta actualización: **85 notas Markdown**, **880 enlaces internos resueltos**, **0 errores**.

## T3–T5 del spec 006: preparación de escena, feria y ensayos — 14 de octubre de 2026

Fuente: issue #46 (fase 3), pedido de Joseph Baño; preparación hecha por IA (Claude Code, Opus 5.5) en la rama `feature/46-fase3-escena-ensayos` (PR #75), sobre `f85b5e8` (PR #73 fusionado). Joseph informó que no hubo ensayos, video ni segunda revisión, y que los nombres de los papeles no están confirmados. Sin entrevistas, cronometrajes ni aprobaciones nuevas.
- [[Guion del pitch]] y [[Checklist y contingencias]]: avisos que remiten a la escena preparada y a las contingencias C-01–C-05.
- [[Consultas para la organizacion]]: O-04 y O-06 revisadas en la fase 3; siguen sin respuesta con fuente.
- [[Registro de trabajo previo y del evento]] y [[Backlog]] (B-46): fila y estado de la preparación.
- Fuera de la bóveda: `escena-y-contingencias.md`, `hojas-feria.md` y `registro-ensayos.md` del spec 006, hoja de segunda revisión en `matriz-afirmaciones.md`, y estado en `task.md` y `test-e2e.md`.

Verificación documental de esta actualización: **85 notas Markdown**, **885 enlaces internos resueltos**, **0 errores**.

## T6 del spec 006: manifiesto y presentación provisional — 14 de octubre de 2026, 16:00

Fuente: issue #46 (fase final), pedido de Joseph Baño; IA (Claude Code, Opus 5.5) en el PR #76. Se crearon `manifiesto-paquete.md` y `materiales/` (pptx y PDF provisionales con su generador y SHA-256 en el manifiesto) del spec 006. [[Registro de trabajo previo y del evento]] añade la tabla del tag `pre-evento` y la sección «Cambios durante el evento», vacías. En [[Consultas para la organizacion]], O-04/O-06 siguen pendientes. Sin ensayos, entrevistas, video, tag ni entrega.

Verificación documental de esta actualización: **85 notas Markdown**, **888 enlaces internos resueltos**, **0 errores**.


## Checkpoint de operación n8n #39 — 14 de octubre de 2026

Fuente: solicitud de Bryan y entrega de Antigravity para #39; revisión de Codex
sobre develop @ `51c3b30`. Se registra el transporte de configuración M2M,
callback API y HMAC en Compose/deploy/rollback, con agente false por defecto y
sin cambios de workflows/prompt. [[Registro de seguimiento y hallazgos]] y los
archivos spec/task/test-e2e/plan/entrega de 003 distinguen código probado,
diagnósticos productivos y aceptación pendiente. PR #78 y su CD `9d1a3d9`
ya concluyeron; el checkpoint requiere integración y promoción posteriores.
No se ejecutan deploy, siembra de RDS ni reset de producción en esta revisión.

Verificación documental: **85 notas Markdown**, **889 enlaces internos resueltos**, **0 errores**.


## Continuidad de #39 y conciliación tras #85 — 14 de octubre de 2026

Fuente: petición de Bryan de ejecutar el plan y sincronizar PR #83 después
de fusionar #85 (`5582930`). Codex conserva la entrega de Antigravity y
registra preparación n8n, sincronización privada del HMAC y sus verificaciones,
sin aceptación/reset productivo. Bryan confirmó límite de gasto DeepSeek,
fin de ediciones de Antigravity, operación EC2 por Jorge y ventana pendiente.

[[Runbook de DevOps y base de datos para la demo]], [[n8n e IA]] y
[[Registro de seguimiento y hallazgos]] distinguen código/local/producción:
se corrige el estado Completado de #38 mientras faltan deploy/cuenta/permisos
reales. Plan/task/entrega/test-e2e y evidencia de 003 incorporan los ocho pasos,
merge/conflictos, conciliación y cierre condicionado. La subprueba HMAC no
se presenta como E2E-07 oficial aprobado; riesgos 401 siguen registrados.

Verificación documental de esta actualización: **85 notas Markdown**, **895 enlaces internos resueltos**, **0 errores**.

## Promoción productiva de preparación de #39 — 14 de octubre de 2026

Fuente: Bryan pidió completar #39 y confirmó ventana exclusiva. #83 fusionado
en develop `a5a8c70`; Codex creó/fusionó #86 de develop a main `a45a548` con
checks aprobados, sin conflictos ni bypass, para desplegar mediante el pipeline
existente con agente deshabilitado. CD `38658331163` iniciado; pendiente de
comprobar éxito/configuración/cuentas/permisos reales. Jorge mantiene operación
RDS; actualización de coordinación en #38.

Revalidación n8n/grafos/cron, curl HTTPS 200 desde su servidor, probe HMAC 202
sin modelo y OAuth2 risks 401 anteriores a la promoción registrados en evidencia
003. Probes eliminados, sin aceptación/reset productivo. npm audit cumple
excepciones vigentes y NuGet no reporta paquetes vulnerables; no se atribuye
esa comprobación a imágenes ni permisos runtime. Login humano requiere MFA;
credenciales/JWT fuera de documentos, issues y diff.

Plan/task/test-e2e/entrega/evidencia y [[Runbook de DevOps y base de datos para la demo]],
[[n8n e IA]], [[Registro de seguimiento y hallazgos]] conciliados. No se resuelven
PA-01/T9/T10 ni #39 hasta cumplir todos los gates productivos.

Verificación documental: **85 notas Markdown**, **898 enlaces internos resueltos**, **0 errores**.

## Continuación de cuentas productivas #39 — 14 de octubre de 2026

Fuente: Bryan confirmó contraseña/MFA de su cuenta y autorizó registrar dos
Account en RDS. Transacción acotada sobre la base efectiva de EC2, verificada
por conexión; cuenta humana /me y risks 200, credencial M2M n8n risks 200,
cinco acciones humanas 403 con M2M. Sin correo/subject/token en documentación.
Túnel cerrado. CD 38658331163 exitoso; intento de reaplicación 38665762599 falló
antes de recreación por permiso GHCR. Agente false; Demo false; gates pendientes.
Transporte/permiso corregidos en rama; motor/calibración en verificación local.
Bóveda/spec 003 conciliados; esta evidencia reemplaza el pendiente de cuentas
del checkpoint anterior, sin convertir pruebas preparatorias en aceptación.

Verificación documental: **85 notas Markdown**, **899 enlaces internos resueltos**, **0 errores**.

## Análisis de flujo E2E por roles, n8n y datasets — 15 de octubre de 2026

Fuente: revisión de `develop` (`4545f50`) a partir del issue #55. Nueva nota
[[2026-10-15-analisis-flujo-e2e-roles-n8n-datasets]] en estado propuesta:
hallazgos verificados en código (HZ-01 a HZ-08), matriz de escenarios ESC-01 a
ESC-28 e issues propuestos ISS-01 a ISS-09. No modifica specs, ADR ni secretos;
los cambios propuestos quedan listados para decisión del equipo.

Verificación documental: **86 notas Markdown**, **918 enlaces internos resueltos**, **0 errores**.

## Limpieza de restos de demo en el flujo del Conductor (#123) — 15 de octubre de 2026

Fuente: Issue #123 de Joseph Baño (urgente por congelamiento). En `apps/mobile`
se retiró de `receipt.page.ts` la entrega inventada y la firma sintética de
respaldo: una entrega ausente de la asignación cargada ahora muestra el error
«No encontramos esta entrega en tu ruta» con botón a Inicio y nunca registra
recepción; la evidencia fotográfica sale solo de la cámara y la firma debe
dibujarse. Se retiró el botón «Foto Demo» y `generateSimulatedPhoto()`. En
`sync.page.ts` el interruptor de prueba offline se conserva con el texto
«Sin señal (prueba)». En `apps/web`, el menú del Auditor incluye «Simulador 90d»
(`/simulation`) y su prueba de navegación quedó actualizada.

Validación local: `npm run test:mobile` (25 pruebas), `npm run test:web` (52
pruebas), `npm run build:mobile`, `npm run check` y `npm run audit:check`, todos
en verde; grep de aceptación del issue sin restos. No se tocaron `scan.page.ts`
ni `home.page.ts`.

Verificación documental: **86 notas Markdown**, **918 enlaces internos resueltos**, **0 errores**.

## Reconciliación oficial de cuentas por rol y paridad de ambientes #55 y cierre #107 — 15 de octubre de 2026

Fuente: resolución y cierre formal de [issue #55](https://github.com/bryancito1090/istpetdev-platform/issues/55) e [issue #107](https://github.com/bryancito1090/istpetdev-platform/issues/107) (Spec 013). Se aprovisionaron y reconciliaron las 6 cuentas oficiales definitivas del equipo por rol mediante la CLI `--provision-accounts` con CSV privado fuera de Git, validando idempotencia con código 0. Se aplicaron contraseñas corporativas permanentes (distribuidas por canal privado) en AWS Cognito User Pool (`us-east-1_4Y2j5DINp`) cumpliendo la política de 14 caracteres de `identity.tf`. Se ejecutó safe deactivation de cuentas de prueba previas (`IsActive = false` en BD y `Enabled = false` en Cognito) para proteger la integridad referencial de auditoría. Mediante túnel SSM administrativo en puerto 15433 hacia `istpetdev-rds-dev`, se replicó la misma matriz en la base de producción Amazon RDS (`istpetdev_integration`), certificando paridad absoluta de entornos con `/health/ready` respondiendo `HTTP 200 Healthy` en ~36ms. Se validaron en vivo los escenarios E2E-07 (recuperación de clave con código en Gmail), E2E-04 (segregación Web vs PWA) y E2E-10 (acceso a Tablero Directivo y Despacho Móvil). Se actualizaron [[Identidad OIDC y sesiones]], [[Runbook de DevOps y base de datos para la demo]] y [[Evidencias y ensayos de base de datos y despliegue]].

Verificación documental de esta actualización: **86 notas Markdown**, **921 enlaces internos resueltos**, **0 errores**.

## Respaldo y restauración verificables (#15) — 15 de octubre de 2026

Fuente: auditoría de `develop` (`8f36d08`) y ensayo real con `scripts/base_datos/test-bootstrap.py`
(pasó el 15-oct 16:47 UTC, 76 s, PostGIS 16 desechable con TLS). Se reescribe el §4 de
[[Runbook de DevOps y base de datos para la demo]] y se actualiza B-32 en [[Backlog]]. Sin acceso
a AWS desde la sesión: el volcado y el `restore-check` contra RDS y el snapshot `pre-democore`
quedan registrados como pendientes, no como realizados.

Verificación documental: **86 notas Markdown**, **925 enlaces internos resueltos**, **0 errores**.

- 15-oct-2026, issue #124: retirada de cifras y pantallas ficticias de la landing, avisos de fallos de consulta y corrección del flujo en línea del Receptor según spec 014. Evidencia y límites en `docs/validation/issue-124-demo-cleanup.md`.

## Validación E2E de gestión de cuentas (#108) — 15 de octubre de 2026

Fuente: Issue #108 asignado a Bryan Baño; guía `docs/specs/013-login-y-cuentas/test-e2e.md`
(E2E-11 a E2E-13). Ejecución asistida contra producción `d739f43`
(`https://istpetdev.doicela.dev`, user pool `us-east-1_4Y2j5DINp`): alta de un
Conductor de prueba desde «Usuarios» («Invitación pendiente» → correo con
contraseña temporal → primer ingreso y cambio de contraseña en `/campo/` →
«Activa»); desactivación con 403 inmediato en `/api/v1/me` pese a la sesión
abierta, `Enabled=false` en Cognito, rechazo de ingreso («User is disabled.») y
reingreso tras «Reactivar»; reglas de Gerencia: fila propia sin acciones, correo
repetido rechazado y reenvío de invitación reciente. Capturas con correos
difuminados en `docs/specs/013-login-y-cuentas/evidencia/` y registro en
`test-e2e.md` y `task.md`. Salvedades: sin rutas publicadas a conductores (no se
comprobó «solo sus entregas») y sin prueba de caducidad de invitación de más de
un día; la cuenta de prueba quedó desactivada y Bryan cambiará la contraseña de
Gerencia al cerrar la campaña. Sin cambios de código.

Verificación documental: **86 notas Markdown**, **925 enlaces internos resueltos**, **0 errores**.


## 15-oct-2026 — Flujos de prueba

Se agrega [[Flujos de prueba]] (07 Demo): recorrido de la demo por rol, login del spec 013 y estado de ESC-01 a ESC-28 tras los merges del día (#121, #122, #123, #128, #110, #111, #112, #114).

Verificación documental: **87 notas Markdown**, **928 enlaces internos resueltos**, **0 errores**.


## 15-oct-2026 — Issue #115: recepción PWA y evidencia privada

La rama `feature/115-receptor-recepcion-real` integra captura parcial del Receptor con su endpoint, catálogo V1 compatible con #117, stock en percha/pedido, foto obligatoria en la captura con rechazo y outbox por cuenta. Sustituye la autorización ficticia S3 por PUT privado prefirmado y verifica el objeto antes de confirmar evidencia. Reutiliza la tabla Evidence; sin migración. Configuración de bucket y prueba física en dos teléfonos pendientes de ejecución en el entorno de destino; no afirmar ESC-04/06/15 físicamente validados. Guion y alcance de las pruebas automatizadas: `docs/validation/issue-115-receptor.md`.

## 15-oct-2026 — Custodia por turnos del issue #162 (revisión de un solo recibo)

Fuente: revisión del primer entregable del issue #162 (rama `fix/162-estados-bulto-custodia`, commits `fb4d877`, `ed3a925`, `45d73e5`) con correcciones bloqueantes del coordinador. Se alinean [[Contratos API y eventos]], [[Movil offline y sincronizacion]], [[Usuarios y flujos]] y [[Hechos canonicos]] con: recibo final único por entrega y cobertura exacta (parcial terminal, `ALREADY_RECEIVED` antes de versión), idempotencia por cuenta+operationId con validación de recurso en replay legado, despacho que cubre todos los bultos y lo planificado con ledger por línea-lote, y `advance` como simulación que respeta las guardas y atribuye las transiciones a la cuenta de demo. Verificación local en PostgreSQL efímero; sin ensayo físico, sin despliegue y sin mutar base compartida.

Verificación documental: **87 notas Markdown**, **932 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes.

## 15-oct-2026 — Dataset y cifras del simulador de 90 días (issue #165)

Fuente: resolución del issue #165 (rama `feature/165-dataset-simulador-90-dias`). Se agrega [[Cifras del simulador 90 dias]] (07 Demo) con los resultados canónicos medidos en 12 corridas comparativas (3 políticas × 3 semillas base y 1 adversa) sobre el fixture nacional sintético `national-v1` (80 puntos, 6 regiones, 90 días). Se actualizan [[Guion del pitch]] y [[Dataset y escenarios]] enlazando las cifras oficiales del simulador. Se habilita aislamiento de base de datos para simulación en backend y Docker Compose, y se verifica la importación limpia del dataset reproducible desde `/datos`.

Verificación documental: **88 notas Markdown**, **941 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Promoción develop → main (rutas al conductor, campo sin señal, custodia y simulador)

Fuente: PR fusionados en `develop` que faltan en `main`: #158 (spec 015), #159 (cierra #160), #168 (#162), #169 (#165), #170 y #173 (#161), #171 (#163) y #172 (#164). Las entradas de #162 y #165 ya están registradas arriba; aquí se registra el resto.

- **#159 / #160 — Publicación al conductor:**
  - La publicación exige un Conductor activo (`GET /api/v1/drivers`) y crea las entregas con las cargas de los pedidos del plan.
  - Cada entrega sale de la bodega de recogida o de la bodega del vehículo.
  - `PUT /api/v1/route-plans/{id}/driver` reasigna el conductor de las entregas pendientes.
  - La web y la PWA muestran bodega, vehículo y conductor. Se muestra el código del vehículo porque `Vehicle` no tiene campo de placa.
  - Detalle: spec 002, T13 y T13b. Se agregan las dos rutas a [[Contratos API y eventos]].
- **#158 — Arranque de la PWA sin señal:** con una sesión OIDC local, la PWA entra en modo sin conexión y retiene el outbox hasta renovar la sesión. No cambia la vigencia de los tokens. Detalle: `docs/specs/015-arranque-offline-pwa/`. Pendiente la prueba en un teléfono real (E2E-01/02).
- **#161 — Etiqueta QR:** el PDF se llama `etiqueta-<código>-<fecha>`, y el título vuelve solo con `afterprint` o al destruir el componente (#173 quita el temporizador del #170). Pendiente la prueba a mano en Chrome Android.
- **#163 — Sincronización:** el outbox informa por qué no envía y permite reintentar a mano. Las evidencias confirmadas se ven en campo y en novedades. `docker-compose.prod.yml` pasa las credenciales AWS y `EVIDENCE_S3_BUCKET` al backend. Detalle: `plan-issue-163.md`.
- **#164 — Detalle de la planificación** por ruta, producto y conductor en la web, con pruebas `RoutePlanDetailTests`.

**Migración:** `20261015234714_CustodyChainStates` (#162) quita la unicidad del índice `CustodyEvent(OrganizationId, OperationId)`. No borra datos.

**Infraestructura:**
- `docker-compose.prod.yml` agrega `ConnectionStrings__Simulation`, con la base `SIMULATION_DB_NAME` (por defecto `istpetdev_simulation`) (#165), y las variables AWS de evidencias (#163).
- La rama `hotfix/39-db-password-rollback` (#93) solo está en `main`; la fusión la conserva sin conflictos.

**Verificación local sobre `develop` (`97a0f11`):**
- Backend: 517 de 518. Falla `N8nErrorWorkflowTests`, que necesita un n8n real en local.
- Web: 76/76. Móvil: 117/117.
- No se probaron los roles con cuentas reales ni la pantalla de login de Cognito.

Verificación documental: **88 notas Markdown**, **942 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Plantilla de contexto y notas individuales del evento

Fuente: requerimiento operativo para el inicio de las jornadas del Hackathon Expo Clean Ecuador 2026. Se crea la carpeta `NotasEvento` y la nota [[Plantilla notas del participante]] con el contexto maestro sintetizado (misión, reto, cifras canónicas, stack, accesos, cronograma oficial y reglas de oro) y la estructura de llenado para los integrantes del equipo (datos de acreditación, equipamiento, conectividad, rol en la escena de la demo, registro de mentorías y bitácora de trabajo).

Verificación documental: **89 notas Markdown**, **951 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Ficha y notas de participante de Bryan Baño

Fuente: inicio de actividades de los integrantes en `NotasEvento`. Se incorpora la ficha personal de Bryan (eliminada después por su instrucción expresa) con los datos de contacto, rol en la escena de la demo (Gerencia Web y coordinación general), equipamiento técnico, registro de mentorías y bitácora de trabajo para las jornadas.

Verificación documental: **90 notas Markdown**, **959 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Registro colaborativo de notas del evento

Fuente: solicitud del equipo para recibir y transcribir en caliente los apuntes de sus integrantes. [[Notas del evento]] es el índice único de la carpeta y define un archivo por apunte, fecha y hora de registro, código de autor, tema y sufijo para colisiones; [[Plantilla nota del evento]] da el formato de captura. [[Plantilla notas del participante]] queda como referencia y deja de pedir datos de identificación y contacto en la bóveda compartida; también se retiran esos campos de la entonces existente ficha personal de Bryan, eliminada posteriormente por su instrucción expresa. El mapa y el README raíz enlazan el índice. El verificador calcula los conteos actuales sin exigir editar este registro histórico en cada PR de notas, evitando conflictos entre colaboradores. Aún no se han recibido apuntes de campo en archivos individuales.

Verificación documental: **92 notas Markdown**, **980 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Primeras observaciones orales del evento

Fuente: dictado de Joseph Baño durante la jornada. Se registran por separado [[2026-10-16-1102-jb-agenda-entregas-y-pitch]], [[2026-10-16-1102-jb-dolores-del-servicio-limpieza]], [[2026-10-16-1102-jb-bodega-inventario-y-rutas]] y [[2026-10-16-1102-jb-personal-especializado-y-emergencias]]. Son relatos de lo oído, no actas oficiales ni entrevistas con consentimiento registrado. Los avisos de horario, Drive y jurados se trasladan como confirmaciones pendientes a [[Consultas para la organizacion]] y al `task.md` del spec 006. Las reglas del manual permanecen vigentes hasta recibir instrucciones comprobables de la organización.

## 16-oct-2026 — Charla del sector y anuncios de organización (issue #184)

Fuente: transcripción compartida por Bryan, registrada en [[2026-10-16-1109-bb-charla-limpieza-logistica|nota de la charla]]. Se conservan extractos, ejemplos empresariales y pasajes dudosos; se distingue el testimonio de la síntesis de tres dolores: aplicación/dosificación, consumo por área y abastecimiento/distribución. No se presenta como entrevista individual consentida, medición, regla técnica de dilución ni validación de la plataforma.

[[Historia y propuesta de valor]], [[Contexto maestro]] y el README enlazan el nuevo contexto. [[Consultas para la organizacion]], [[Hechos canonicos]], [[Evaluacion y entregables]], [[Cronograma oficial]], [[Guion del pitch]] y la plantilla de preparación reciben las precisiones: turno oral de cinco minutos con meta interna 4:40, canal Drive para el capitán y pendientes sobre preguntas, horarios y paneles del jurado. Se conserva el manual y se registran contradicciones; no se alteran pesos, contratos, baselines ni umbrales del motor. Sin ensayo, subida de materiales, despliegue ni cambio de software.

Se integraron los apuntes de Joseph publicados simultáneamente en el PR #181, conservando sus archivos y el seguimiento del spec 006. Se resolvieron los conflictos de hechos canónicos, consultas y este registro: se enlazan ambas fuentes del jurado y se preserva el posible corte de las 13:00 de Joseph junto con los anuncios de la transcripción aportada por Bryan.

Verificación documental tras la integración: **98 notas Markdown**, **1034 enlaces internos resueltos**, **0 errores**; se mantienen los cuatro avisos de lenguaje preexistentes. Los nueve enlaces Markdown nuevos a las fuentes resuelven y `git diff --check` no reporta errores.

## 16-oct-2026 — Agenda general oficial y matriz de mentorías (issue #188)

Fuente: documento oficial de la organización del Hackathon Expo Clean Ecuador 2026 distribuido a los equipos participantes, registrado en [[2026-10-16-1126-bb-agenda-general-y-mentorias]]. Se incorpora la Agenda General completa para las jornadas de viernes 16 y sábado 17 de octubre, y la matriz oficial de mentorías técnicas para el Reto 1 (Optimización y trazabilidad logística del abastecimiento — 14 equipos).

[[Cronograma oficial]], [[Registro de mentorias]], [[Consultas para la organizacion]] y [[Hechos canonicos]] incorporan las actualizaciones: confirmación de IstpetDev en Reto 1 asignado al mentor Mauro Zurita (16:15–17:00 en supply chain y trazabilidad); resolución de la contradicción sobre cortes de entrega (checkpoint de avances el viernes 16 a las 18:30–19:00 y entrega de hitos el sábado 17 de 16:00 a 16:15 con Mónica Sevilla y William Chamorro); e identificación de conferencias magistrales programadas. No altera contratos, bases de datos ni software.


## 16-oct-2026 — Transcripción completa y hallazgos del Reto 1 (issue #189)

Fuente: revisión solicitada por Bryan al reiterar la charla y pedir eliminar su ficha personal. La incorporación del PR #186 conservaba extractos y síntesis; se completa con [[Charla del sector limpieza - transcripcion recibida]], que guarda el texto recibido íntegro de la charla, con los nombres de terceros reservados y sin corregir errores de transcripción. No es un cotejo contra audio.

[[Hallazgos de la charla para el Reto 1]] es el documento de consulta fuera de NotasEvento: preguntas y respuestas, tres operaciones (prestador/comercializadora/fabricante), inventario fraccionado, promociones, pedidos imprevistos, rutas consolidadas y ejemplos de peso/volumen/economía de transporte. Hace explícito que no se respondieron días de anticipación ni política de prioridad ante cierre vial; distingue la agrupación de tres dolores de una enumeración literal del expositor.

Se enlaza desde README, mapa, contexto, historia de producto, reto oficial e inventario/rutas. Por instrucción expresa de Bryan se elimina su ficha personal de NotasEvento y se retiran las referencias activas; la charla y sus fuentes permanecen. Se conservan los registros históricos de su alta y limpieza, con referencias en texto. No cambia software, contrato, baseline, umbral ni estado de implementación; no se despliega.

Verificación documental: **101 notas Markdown**, **1077 enlaces internos resueltos**, **0 errores**; se conservan cuatro avisos de lenguaje preexistentes. Se revisaron los enlaces Markdown de entrada/consulta, la presencia de los pasajes finales y la ausencia de enlaces a la ficha eliminada. `npm run check`, `npm run audit:check` y `git diff --check` completados; la auditoría conserva siete avisos cubiertos por excepciones vigentes.

## 16-oct-2026 — Agenda oficial en el plan, entregable 2 e ideas del evento (PR #192)

Fuente: archivo oficial `areas-menotres.xlsx` (hojas «AGENDA DE MENTORIAS», «AGENDA GENERAL VIERNES 16» y «SABADO 17»), cotejado con [[Cronograma oficial]]: las agendas coinciden. Se agrega la convocatoria a conferencias (IstpetDev no está convocado a cuatro conferencias; esos bloques quedan para ensayar) y los hitos del equipo.

Se actualizan los horarios del plan, que seguían el manual: «Días del evento» de [[Definiciones de producto y plan hasta el pitch]] (tabla vigente según la agenda oficial; la del 12-oct queda como antecedente), [[Plan de ejecucion]], [[Registro de mentorias]] y el spec 006 (validación del 17-oct de 11:00–12:30 a 11:30–13:15; congelamiento del código a las 11:30). Nuevas notas: [[Entregable Solucion y propuesta de valor]] (entregable oficial n.º 2, con PDF de una página en el spec 006) y [[Ampliacion de alcance - ideas del evento]]. Los issues #193–#201 ordenan el trabajo del evento con semáforo de prioridad. Redactado por Claude Code (Opus 5.5) a pedido de Joseph Baño; sin cambios de software.

## 16-oct-2026 — Consultas de abastecimiento de solo lectura para planificación y agente (issue #194)

Fuente: implementación y verificación del issue #194 siguiendo el [plan de consultas de abastecimiento](../../specs/003-agente-logistico/plan-194-consultas-abastecimiento.md) y el seguimiento en [task.md](../../specs/003-agente-logistico/task.md). Se agrega `LogisticsQueriesController` bajo `/api/v1/logistics` (`GET /stock`, `POST /sourcing`, `GET /vehicles/available`) sin modificar `RoutePlansController` (#201), reutilizando `SourcingService` (con descuento de `WarehouseSafetyStockPct` una sola vez por bodega y producto) y `VehicleMatcher` (con ventana diaria en la zona horaria operativa de la organización y tarifa de respaldo).

Se documentan los contratos, ejemplos, unidades, reglas de cálculo, códigos de error, RBAC (`Roles.Planning` y `Roles.Automatizacion` con scope `istpetdev-api/automation`) y límite de frescura sin reservas en [[Contratos API y eventos]]. La evidencia de pruebas locales en contenedor PostGIS descartable (`8/8` en `LogisticsQueriesTests` y `528/528` en la suite completa de backend) se registra en [2026-10-16-194-consultas-abastecimiento.md](../../specs/003-agente-logistico/evidencia/2026-10-16-194-consultas-abastecimiento.md). No ejecuta workflows de n8n (#195/#196) ni despliegues.

Verificación documental: **105 notas Markdown**, **1113 enlaces internos resueltos**, **0 errores**; se conservan cuatro avisos de lenguaje preexistentes.

Revisión final de #194 por Codex: se unificó la carga de pedidos para respetar
el remanente de #199 también en la consulta nueva y se corrigió el contrato
contra los DTOs reales. La regresión de cantidades parciales/completas falla
antes y pasa después de la corrección. La revisión final pasa 560/560 pruebas
locales, npm check/auditoría y bóveda (105 notas, 1116 enlaces, cero errores).
CI y cierre remoto se acreditan en la evidencia y el PR #215.

## 16-oct-2026 — Hora límite para puntos críticos en el planificador (issue #193)

Fuente: issue #193, que implementa AMP-01 de [[Ampliacion de alcance - ideas del evento]]. [[Contratos API y eventos]] registra el campo opcional `criticalDeadlineLocal` de `POST /api/v1/route-plans` y el error 400 `invalid_critical_deadline`; el spec 002 añade FR-25 y la tarea T16. Sin el campo, el plan no cambia. No altera baselines, umbrales ni datos; no se despliega.

Verificación documental (tras integrar develop con #197, #201 y #192): **105 notas Markdown**, **1115 enlaces internos resueltos**, **0 errores**; se conservan los cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Herramientas de abastecimiento del agente (issue #195)

Fuente: implementación en la rama `feature/195-herramientas-n8n-abastecimiento`
(checkout Orca aislado desde `develop` `1dcfb51`), plan y evidencia del issue.
Se actualizan [[n8n e IA]] con las ocho herramientas del prompt v2, los guards de
allowlist/deadline y la evidencia determinista en `AgentRun`, y
[[Contratos API y eventos]] con la ampliación de `AgentRunView`. Los contratos de
`/api/v1/logistics/*` corresponden a #194 y se conservan íntegros en su PR. Los
resultados históricos de cinco herramientas y prompt v1 permanecen como
históricos; no se reescriben. Verificación documental y estado real por separado
en la evidencia del issue; este registro no acredita por sí solo merge ni ensayo.


## 16-oct-2026 — Revisión de viabilidad de datasets coordinada por n8n (issue #209)

Fuente: issue #209 y el reporte del importador del spec 001. [[Contratos API y eventos]] documenta `status` y la sección determinista `feasibility` de `POST /api/v1/imports` (puntos sin coordenadas, bodegas sin flota, productos sin peso/volumen y déficit de stock por SKU), el veredicto y la autorización M2M acotada: `Roles.Automatizacion` solo ejecuta el dry-run (`dryRun=true&allowPartial=false`) y aplicar sigue siendo de `Roles.Planning`. El spec 001 actualiza FR-10 y [[n8n e IA]] registra el workflow `revision-dataset`, su prompt versionado y el fallback determinista ante fallo o cifras inventadas del LLM, sin `allowPartial=true`.

Evidencia: 566/566 pruebas backend (6 nuevas de `FeasibilityReportTests`), scripts n8n 8/8 archivos (7 nuevas del workflow), `npm run check`, `npm run audit:check`, bóveda y ejecución real en n8n 2.39.5 con puente HTTP local en [2026-10-16-209-revision-dataset.md](../../specs/003-agente-logistico/evidencia/2026-10-16-209-revision-dataset.md). Sin despliegue.

Verificación documental: **106 notas Markdown**, **1139 enlaces internos resueltos**, **0 errores**; se conservan cuatro avisos de lenguaje preexistentes.

## 16-oct-2026 — Charla de logística integral para el Reto 1 (issue #232)

Fuente: texto aportado por Bryan en la conversación, conservado íntegro en [[Charla de logistica integral - transcripcion recibida]], con una mención de tercero reservada y errores de transcripción explícitos. [[Logistica integral para el Reto 1 - hallazgos y evidencia]] organiza cadena proveedor–almacén–transporte–cliente, cuatro componentes, seis palancas, indicadores, ejemplos, riesgos, mitigaciones y las preguntas de combustible/flota, con pendientes de aclaración. Se enlazan inventario, rutas, validación, reto oficial, hallazgos previos, hechos canónicos y los índices.

Cambio documental: sin cambios de software, contratos, prioridades aprobadas ni despliegue. No acredita prueba funcional, estudio empresarial, normativa urbana/combustible ni ejecución de planes de flota. La fecha es de recepción; autoría y momento de la exposición siguen pendientes. Verificación local: `python docs/boveda/.github/scripts/check_vault.py` — **108 notas Markdown, 1166 enlaces internos, 0 errores y los 4 avisos de lenguaje preexistentes**; `git diff --check`, `npm run check` y `npm run audit:check` terminan correctamente. La auditoría conserva 7 avisos cubiertos por excepciones vigentes; el build muestra avisos de la regla ESLint deprecada y Leaflet CommonJS. Estos checks comprueban documentación/compilación, no los casos operativos propuestos en la charla.

## 16-oct-2026 — Costo total estimado de servir (issue #240)

Fuente: issue #240 y coordinación #253, con fórmulas sintéticas de la propuesta
#239. [[Contratos API y eventos]] y [[Rutas y sobrecostos]] describen combustible
según carga con precio fechado, desgaste, conductor, ayudantes y merma, las
columnas opcionales del importador y el fallback compatible. `ServiceCostCatalog`
es una migración EF real y aditiva del repositorio, no una aplicación implícita
sobre RDS. Las pruebas incluyen base vacía y copia de la demo local existente.

[Plan y configuración](../../specs/003-agente-logistico/plan-240-costo-servicio.md),
[evidencia y límites](../../specs/003-agente-logistico/evidencia/2026-10-16-240-costo-servicio.md).
La captura sale del resultado calculado sobre demo-base, con parámetros de prueba
sintéticos. No se presenta como pantalla #242 ni como dataset Quito #243.
Sin despliegue; el estado remoto se comprueba en el PR de #240. Anthony conserva
la integración del controlador compartido; #244/#245 rebasan después.
