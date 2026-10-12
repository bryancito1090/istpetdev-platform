---
tipo: indice
estado: vigente
actualizado: 2026-10-09
tags: [hackathon, equipo]
---

# Mapa de la bóveda

## Contexto común

- [[Bienvenido]] — entrada del equipo.
- [[Contexto maestro]] — resumen para cualquier integrante o asistente.
- [[Hechos canonicos]] — versiones, cifras, rutas, nombres y estado de ADR; contradicciones abiertas.
- [[Convenciones y estados]] — cómo mantener notas y fuentes coherentes.
- [[Glosario]] — lenguaje del producto y del dominio.

## Reglas y fuentes

- [[Resumen del manual]], [[Reto 1 oficial]], [[Cronograma oficial]].
- [[Evaluacion y entregables]], [[Consultas para la organizacion]].
- [[Fuente y control documental]], [[Informacion inicial del equipo]], [[Manual oficial.pdf]].

## Producto

- [[Historia y propuesta de valor]], [[Alcance y prioridades]].
- [[Usuarios y flujos]], [[Requisitos y aceptacion]], [[Modelo de negocio y piloto]].

## Arquitectura

- [[Arquitectura del sistema]], [[Decisiones de arquitectura]].
- [[Backend y tiempo real]], [[Contratos API y eventos]].
- [[Frontend y componentes]], [[Frontend con Feature-Sliced Design]], [[Movil offline y sincronizacion]], [[Sistema de diseno y colores]].
- [[AWS y Terraform]], [[CI-CD y automatizacion de despliegue]], [[Hardening y seguridad de servidores]], [[Seguridad y evidencias]].
- [[RBAC y configuracion del sistema]] — roles, permisos, campos, procesos y plantillas administrables desde DB.
- [[Conexion PostgreSQL en DBeaver]] — conexiones de esquema/datos con los usuarios ya existentes.
- [[Verificacion de seguridad antes del lanzamiento]] — controles complementarios con evidencia previa a exposición.

## Datos y algoritmos

- [[Modelo de datos]], [[Inventario y prediccion]].
- [[Esquema completo de base de datos]] — catálogo objetivo de tablas, columnas/relaciones, restricciones, RLS y migraciones por módulos.
- [[Rutas y sobrecostos]], [[Simulador y metricas]], [[Dataset y escenarios]].
- [[n8n e IA]].

## Ejecución del equipo

- [[Equipo y acuerdos]], [[Plan de ejecucion]], [[Backlog]].
- [[Riesgos y decisiones pendientes]], [[Registro de trabajo previo y del evento]].
- [[Coordinacion migracion schema95]] — aplicación personal, evidencia y reparto de revisión/documentación entre agentes.
- [[Registro de mentorias]].

## Validación y presentación

- [[Plan de validacion]], [[Guion del pitch]], [[Checklist y contingencias]].
- [[Plus para ganar]].

## Skills y plantillas

- [[Catalogo y plan de skills]] — skills del equipo y reglas para asistentes de IA (documentación; los archivos no se versionan aquí).
- Contenido de cada skill: [[istpetdev-contexto]] · [[istpetdev-docs]] · [[istpetdev-contrato]] · [[istpetdev-backend]] · [[istpetdev-dbflow]] · [[istpetdev-datos]] · [[istpetdev-frontend]] · [[istpetdev-revision]] · [[istpetdev-prelaunch]] · [[istpetdev-producto-pitch]].
- [[Plantilla ADR]], [[Plantilla historia de usuario]].
- [[Plantilla evidencia]], [[Plantilla mentoria]].

## Lectura según el rol

| Rol | Ruta mínima |
|---|---|
| Todo el equipo | Contexto → reto oficial → alcance → backlog → pitch |
| Backend y datos | Arquitectura → modelo de datos → contratos → inventario → rutas |
| Web y UX | Usuarios → requisitos → frontend → contratos → métricas |
| Móvil | Usuarios → offline → contratos → seguridad → validación |
| Nube y automatización | AWS/Terraform → backend/SignalR → n8n/IA → riesgos |
| Producto y presentación | Evaluación → negocio/piloto → validación → plus → guion |

## Acuerdos operativos del 7 de octubre

- [[Repositorio de software y versiones]] — GitHub vacío, estructura local y toolchain.
- [[Entornos y operacion acordados]] — desarrollo/RDS compartido, red/IAM, backups/S3, n8n, observabilidad y deploy/rollback.
- [[Identidad OIDC y sesiones]] — ADR-10 Cognito, sesiones web/PWA y cliente de automatización.

Estas notas complementan el diseño incorporado por el compañero y registran decisiones posteriores de Bryan.

## Acceso seguro del equipo — 7 de octubre de 2026

- [[Credenciales y acceso del equipo]] — AWS por SSO/MFA, Secrets Manager, configuración local, OIDC de Actions y alta/baja de integrantes; aplica a los dos repositorios.

- [[Puesta en marcha del workspace y AWS dev]] — prompt para el agente de software y secuencia de consola/terminal para aprovisionar dev y conectar a los integrantes; ejecución pendiente.

- [[AWS temporal para la hackathon y cierre]] — escenario posterior vigente: conservar créditos en cuenta independiente, IAM/MFA con login temporal y eliminación de recursos del proyecto al terminar; leer antes de seguir los pasos SSO anteriores.
