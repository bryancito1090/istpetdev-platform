---
tipo: ejecucion
estado: vigente
actualizado: 2026-10-06
tags: [hackathon, istpetdev]
---

# Equipo y acuerdos

## IstpetDev

**Cinco integrantes**, todos con amplia experiencia en desarrollo full stack: backend, móvil, frontend, datos, logística y otras necesidades del proyecto. Trabajaremos con participación flexible por entregables.

Esta bóveda **no asigna personas a áreas ni reparte especialidades**. Define resultados, dependencias y criterios compartidos para que cualquier integrante pueda avanzar.

## Acuerdos de ejecución propuestos

- Una sola fuente de contrato para backend, web, móvil y n8n.
- Coordinar quién está editando el mismo archivo o interfaz, sin convertirlo en una especialidad fija.
- Resolver cambios de alcance/arquitectura antes de implementar variantes incompatibles.
- Mantener el flujo integrado funcionando al cerrar cada entrega.
- Registrar evidencia y contribuciones por cambios concretos.
- Mantener visibles simulación, datos sintéticos y supuestos.
- Usar las mismas versiones acordadas y seed para reproducir errores.
- Cerrar trabajo con criterios del backlog, no con una lista de tareas superficiales.

## Recursos por confirmar

| Recurso | Estado |
|---|---|
| Nombre/cantidad de integrantes | Confirmado por el equipo: IstpetDev, 5 |
| Reto inscrito | Reto 1 declarado; conservar confirmación oficial |
| Repositorio de la bóveda | [bryancito1090/istpetdev-hackathon-2026](https://github.com/bryancito1090/istpetdev-hackathon-2026), privado |
| Repositorio de código | Pendiente |
| Método de compartir/sincronizar la bóveda | Git/GitHub; instrucciones en README.md; sesiones personales excluidas |
| Cuenta AWS y límite de gasto | Pendiente |
| Región/dominio/certificados | Pendiente |
| Cuenta/proveedor de mapas | Pendiente |
| Cuenta/modelo DeepSeek | Pendiente |
| Entorno n8n | Pendiente |
| Dispositivos para pruebas y demo | Pendiente |
| Caja vacía, QR y conectividad de respaldo | Pendiente |

El manual exige un representante/responsable de equipo para inscripción/administración; registrarlo en los instrumentos del evento sin usarlo para repartir el desarrollo. La documentación sensible del premio se mantiene fuera de la bóveda.

## Ritmo de integración

Revisar brevemente: qué entrega funciona, qué interfaz cambia, qué bloquea la integración y qué evidencia falta. Los cambios de implementación se registran en [[Registro de trabajo previo y del evento]].

Entradas operativas: [[Plan de ejecucion]], [[Backlog]] y [[Riesgos y decisiones pendientes]].

## Recursos confirmados por Bryan — 7 de octubre de 2026

Este registro complementa la tabla inicial sin sustituir el aporte del compañero.

| Recurso | Estado actualizado |
|---|---|
| Cuenta AWS y presupuesto | Disponibles y confirmados por Bryan; valores privados por cargar al aprovisionar |
| Región | `us-east-1` seleccionada para el diseño; latencia y recursos reales por verificar |
| Repositorio software | [istpetdev-platform](https://github.com/bryancito1090/istpetdev-platform), privado y vacío; Bryan añadirá integrantes |
| Esqueleto | Local en `~/Projects/ISTPETDEV`; sin commits/push de software |
| Backend / frontend | .NET 8 y Angular 22 confirmados; recomendaciones en [[Repositorio de software y versiones]] |
| Trabajo diario | Cada integrante ejecuta sus aplicaciones; RDS dev privado compartido con aislamiento y una base de integración |
| Dominio | Se solicitará a la institución; un compañero profundizará DNS/TLS/CDN |
| GitHub Actions/Compose | Implementación pendiente del compañero que aportó CI/CD a develop |
| n8n | Servidor existente https://n8n.bryan-bano.com/; Bryan cargará credenciales |
| Identidad | Cognito definido en [[Identidad OIDC y sesiones]]; aún no provisionado |

No cambia el trabajo flexible por entregables. Configuración detallada, pruebas y campos privados pendientes en [[Entornos y operacion acordados]].

## Acceso y credenciales — 7 de octubre de 2026

Se acepta [[Credenciales y acceso del equipo]] para ambos repositorios: cuentas GitHub individuales, AWS SSO/MFA y secretos dev autorizados por persona/servicio. Bryan carga las credenciales reales en su almacén correspondiente; el equipo recibe acceso limitado, sin compartir su token AWS, usuario root, master PostgreSQL ni cuenta administrativa n8n. Alta/baja y rotación se registran sin valores secretos. La configuración efectiva y las pruebas siguen pendientes en B-35/V-32.
