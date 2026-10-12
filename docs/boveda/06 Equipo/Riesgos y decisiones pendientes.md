---
tipo: ejecucion
estado: propuesta
actualizado: 2026-10-07
tags: [hackathon, istpetdev]
---

# Riesgos y decisiones pendientes

## Riesgos principales

| ID | Riesgo | Efecto | Respuesta |
|---|---|---|---|
| R-01 | Regla de trabajo previo desconocida | Preparación no evaluable o mal declarada | Resolver O-01/O-02 y registrar proveniencia |
| R-02 | Alcance demasiado amplio para el tiempo | Funciones aisladas sin recorrido | Integrar P0 primero, luego P1 |
| R-03 | Datos de consumo insuficientes | Cobertura con falsa precisión | Mostrar fuente/frescura y supuestos |
| R-04 | Simulación sesgada | Ahorro indefendible | Calendario común, baseline razonable y exportaciones |
| R-05 | Offline duplicado/conflictivo | Stock incorrecto o recepción perdida | Persistencia, idempotencia y control de cantidades |
| R-06 | SignalR falla con varias réplicas | Mensajes/eventos invisibles | Backplane, afinidad y recuperación REST |
| R-07 | Dependencia de APIs/red | Demo interrumpida | Matriz permitida cacheada, fallback y video |
| R-08 | Costos cloud/APIs sin límite | Viabilidad débil y gasto innecesario | Definir cuenta/region/límite y medir |
| R-09 | .NET 8 próximo a fin de soporte | Continuidad requiere actualización | ADR-01 antes del scaffold |
| R-10 | Historial llamado inmutable sin garantía | Pérdida de credibilidad técnica | Describir append-only y protección probada |
| R-11 | QR/evidencias exponen información | Acceso indebido | Vista pública limitada y S3 privado |
| R-12 | Evento exige restricciones nuevas | Cambios tardíos | Parámetros versionados y fixture adaptable |
| R-13 | Asistentes de IA reciben datos contradictorios o incompletos | Código y documentos con datos inventados | [[Hechos canonicos]], [[Catalogo y plan de skills]] y `check_vault.py` en CI |

## Decisiones pendientes

| Tema | Momento de resolución |
|---|---|
| Trabajo previo, entrega y tiempo de pitch | Capacitación del 8 de octubre |
| Versiones .NET (ADR-01) y PostgreSQL (ADR-03); revisar Angular, Ionic y Capacitor fijados en ADR-04 (Angular 18 sin soporte) y Terraform | Antes del primer scaffold |
| Región/cuenta/límite AWS | Antes de aprovisionar |
| Motor RDS/PostGIS y generadores UUID | Antes de migraciones |
| Proveedor vial/costo/almacenamiento permitido | Antes de matrices y comparativa |
| Proveedor de identidad y sesión móvil/offline (ADR-10) | Antes de recepción autenticada; el modelo RBAC ya está aceptado en ADR-14 |
| PWA vs nativo para demo | Antes de pruebas en dispositivos |
| Repositorio del software | Antes de empezar el código; la bóveda ya usa Git/GitHub |
| Modelo/prompt/cuotas IA | Antes de configurar n8n |
| Costos reales y tamaño del piloto | Con información empresarial |

No faltan nombres ni especialidades del equipo: IstpetDev ya confirmó cinco integrantes con experiencia transversal.

## Regla para declaraciones públicas

| Afirmación | Evidencia requerida |
|---|---|
| Ahorra X% | Run exportado, baseline y denominadores |
| Evita quiebres | Demanda común, claves de quiebre y casos nuevos |
| Funciona offline | Reinicio y sincronización en dispositivo real |
| Es altamente disponible | Configuración y prueba de fallo/recuperación |
| Detecta mermas | Conciliación confirmada, no solo anomalía |
| Trazabilidad completa | Cobertura de líneas y evidencia requerida |
| Está validado por empresa | Entrevista/piloto real documentado |

Revisar [[Plan de validacion]] y [[Plus para ganar]].

## Estado actualizado — 7 de octubre de 2026

Cuenta/presupuesto disponibles, repositorio software creado vacío, .NET 8 y Angular 22 confirmados y n8n existente. Red/IAM/Cognito/migraciones/backups/S3/observabilidad/rollback quedaron definidos para implementación en [[Entornos y operacion acordados]], [[Identidad OIDC y sesiones]] y [[Repositorio de software y versiones]].

Siguen pendientes: dominio institucional y su DNS/TLS/CDN, ejecución de Actions/Compose por el compañero, carga privada de credenciales/identificadores, ADR-05 mapas, reglas del evento y evidencia real de compatibilidad, seguridad, offline y recuperación. R-08 pasa de cuenta/presupuesto desconocidos a controlar horas/recursos persistentes; R-06 conserva su prueba de dos réplicas B-30; R-09 mantiene .NET 8 elegido y actualización posterior al evento. Los acuerdos no equivalen a recursos o pruebas realizados.
