---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-07
tags: [arquitectura, implementacion]
---

# Móvil offline y sincronización

## Plataforma y alcance

Angular/Ionic. **PWA con IndexedDB** como primer objetivo compartible por teléfono; SQLite se incorpora en un empaquetado nativo con plugin/versiones comprobados. No asumir que ambas opciones tienen el mismo soporte. ADR-04 fija Ionic 8 y Capacitor 6 mientras ADR-06 propone PWA primero: contradicción abierta en [[Hechos canonicos]].

La primera descarga requiere conexión. Antes de salir, el conductor descarga ruta/versiones, entregas asignadas, catálogo mínimo y QR necesarios. Un QR nunca descargado no puede consultar mágicamente la nube sin red.

El service worker de Angular aporta caché básica; no implementa nuestra sincronización de escrituras. Background Sync tiene compatibilidad limitada, por lo que la recuperación funciona al abrir la app, recuperar conectividad y pulsar sincronizar. [Angular service worker](https://angular.dev/ecosystem/service-workers), [Background Synchronization API](https://developer.mozilla.org/en-US/docs/Web/API/Background_Synchronization_API).

## Persistencia local

| Registro | Contenido |
|---|---|
| Asignaciones | IDs, líneas, cantidades autorizadas, versión y última descarga |
| Operaciones outbox | operationId, tipo, referencia, contenido, fecha local, intentos y estado |
| Archivos pendientes | Blob/ruta local, checksum, tamaño y recepción asociada |
| Confirmaciones | Resultado del servidor y hora de aceptación |
| Conflictos | Motivo, estado local y respuesta del servidor |

Guardar operación y su referencia a archivos antes de mostrar «guardada». Una foto temporal en memoria no cumple persistencia offline. Verificar cuota/espacio y avisar si no se puede guardar.

## Protocolo

1. Generar un ID de operación único en dispositivo; UUID v7 si existe implementación interoperable verificada.
2. Persistir recepción y evidencias localmente de manera recuperable.
3. Mostrar «guardada en el dispositivo; pendiente de sincronización».
4. Al poder conectarse, verificar sesión y enviar operación con la misma clave idempotente.
5. El backend valida asignación, versión y cantidades; responde aceptada, rechazo o conflicto.
6. Subir archivos con autorización obtenida de la API; confirmar metadatos.
7. Marcar recepción aceptada y evidencia completa por separado.
8. Eliminar copias locales según retención y solo después de confirmación durable.

La llegada del evento SignalR no reemplaza el acuse de la operación.

## Conflictos

| Caso | Política |
|---|---|
| Respuesta perdida | Reintentar mismo operationId |
| Token expirado | Pedir reautenticación, conservando cola |
| Ruta modificada | Validar entrega/asignación con versión; presentar conflicto si invalida acción |
| Cantidad excedida | Rechazar exceso; revisión humana, no último escritor gana |
| Misma entrega en dos dispositivos | Validar cantidades restantes y duplicado de negocio |
| Desorden de llegada | Validar prerequisitos; no inventar orden desde UUID/hora local |
| Archivo fallido | Reintentar carga; mantener evidencia pendiente |
| Operación rechazada definitivamente | Guardar motivo y permitir resolución auditada |

Cada registro guarda **occurredAtDevice** y **receivedAtServer**. El reloj móvil puede estar desajustado; no ordenar contabilidad únicamente por esa fecha.

## Qué puede hacerse offline

Consultar asignaciones descargadas, escanear unidades conocidas, registrar recepción, foto/firma y un incidente pendiente. El inventario global, nuevas asignaciones y cálculos viales requieren sincronización. El chat permite borrador/cola de envío; no entrega bidireccional sin red.

No prometer GPS continuo con app cerrada sin probar permisos y limitaciones del sistema operativo. Para demo, registrar pings en primer plano y declararlos.

## Pruebas obligatorias

- Cargar asignación, activar modo avión, capturar y reiniciar la app.
- Volver a la red y confirmar recepción y archivos.
- Perder respuesta del servidor y repetir: un solo efecto.
- Dos dispositivos y exceso de cantidad: conflicto visible.
- Probar Android y navegador objetivo; incluir iOS si va a usarse.
- Abrir desde HTTPS en teléfono real; HTTP por IP de red local no equivale a localhost.

Usar [[Plan de validacion]] y [[Checklist y contingencias]].

## Sesión y versión acordadas — 7 de octubre de 2026

PWA primero con Angular 22/Ionic 8 e IndexedDB; Capacitor 8 recomendado para el empaquetado nativo posterior. Cognito con PKCE; tokens no forman parte de la cola durable. Expiración, logout o rechazo de permisos conserva capturas y archivos aislados por identidad. Subida S3 privada después de reautenticación y autorización vigente. Ver [[Repositorio de software y versiones]], [[Identidad OIDC y sesiones]] y [[Entornos y operacion acordados]]; pruebas reales Android/iOS y HTTPS continúan pendientes.
