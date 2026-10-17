# #330 — Muestra sintética de novedades de campo

Fecha: 2026-10-17. Base de implementación: develop `c422063`. Trabajo aislado
en `ISTPETDEV-rutas`, sin modificar el checkout de la otra sesión.

## Alcance y método

Preparar y probar una carga aditiva, ensayarla con rollback, integrar por PR y
después aplicarla en desarrollo configurado y producción autorizada. Finalmente
verificar persistencia, idempotencia y conservación de stock/reservas. No reset,
reimportación ni migraciones. Ver `tools/FieldActivitySeed/README.md`.

La muestra está identificada como sintética. El consumo y el saldo actuales
determinan cantidades y cobertura; los escenarios de rechazo y ajuste son
demostrativos. No se atribuyen tasas observadas ni estadísticas reales externas.

## Inspección y respaldo antes de la ejecución

- Desarrollo configurado en `.env` y user-secrets: la misma base local
  `istpetdev_dev_01`, puerto 15432; no son dos bases diferentes. Antes: 15 puntos,
  3 productos, 11 planes, 7 entregas y ninguna recepción o reposición de campo.
- Producción RDS `istpetdev_integration`: 40 puntos, 12 productos, 24 planes,
  11 entregas y ninguna recepción o reposición de campo. Conserva el dataset
  importado previamente. Acceso AWS del proyecto renovado por el usuario;
  túnel SSM, TLS VerifyFull y CA oficial, sin cambios a grupos de seguridad.
- Respaldo productivo verificado: 39 tablas, 1.011.640 bytes, SHA256
  `1fbe2ac5344ecbbeaf0c4d6f4c40bbc326e6cff7acb1ccafbaaf060ec41e6a64`.
  `pg_restore --list` válido, fingerprints antes/después iguales y objeto S3
  validado por tamaño y checksum. Manifest y credenciales solo en almacenamiento
  privado. El primer intento con usuario backup falló; el respaldo efectivo usó
  el migrador validado y el mismo procedimiento oficial, sin rotar permisos.

## Prelaunch para la herramienta de datos

Revisión según `istpetdev-prelaunch`, `istpetdev-dbflow`, hechos canónicos,
modelo de datos y runbook. No es un despliegue ni un nuevo servicio expuesto.

| Control | Estado y evidencia |
| --- | --- |
| Identidad/entorno | Verificado: allowlist de nombres, comparación de conexión y VerifyFull obligatorio para producción. No se usa el perfil AWS default de otra cuenta. |
| Roles y asignaciones | Reutiliza FieldService y RoutePublisher, con actores técnicos sintéticos de roles fijos y sin identidad Cognito válida. Prueba HTTP de novedades con planificador autorizado. |
| Aislamiento | DemoCore de una organización; todos los queries de la carga filtran DemoOrganization. No se cambian grants/RLS; el catálogo Schema95 archivado no aplica. |
| QR y DTO | Tokens generados por el servicio, solo en memoria; no se imprimen ni exportan. No se incorporan campos administrables ni plantillas. |
| Consistencia | Transacción propia, rollback por defecto, lote idempotente, sin stock negativo y conservación de reservas cubiertos por integración PostgreSQL. |
| Datos/secretos | Datos sintéticos marcados, conexiones privadas fuera de Git y argumentos. No se cambia clasificación o cifrado por campo. |
| Dependencias | Sin nuevas dependencias. Build local verificado; no equivale a un escaneo de vulnerabilidades. Reporte de imagen productiva pendiente, no se sustituye esa imagen. |
| Runtime | Ensayos y aplicación en bases reales pendientes al escribir esta revisión. Pruebas de código no se presentan como ejecución real. |

## Verificación de código

98/98 pruebas de integración de campo aprobadas, incluidas tres nuevas: ensayo sin efectos persistidos, aplicación con
conservación de stock e idempotencia + consulta HTTP de novedades, y aborto sin
stock libre. Build de CLI: cero errores y advertencias. La regresión de campo y
los resultados reales se registrarán tras ejecutarse. Bóveda: cero errores,
cuatro avisos preexistentes; `git diff --check` correcto.

Ensayo real de desarrollo con rollback completo: 2 planes, 12 recepciones,
2 con rechazo, 2 con ajuste y 8 pedidos de reposición; 180 unidades despachadas,
178 aceptadas y 2 rechazadas (1,11% de esta muestra). El respaldo local posterior
contiene 40 tablas, 401.974 bytes y `pg_restore --list` válido; SHA256
`90d818d98e63e67d3aaabf6510b861f354ac1a360c0bbee1465b67ac32928b91`.
Antes de aplicar había 12 planes: otro flujo añadió uno desde la inspección
inicial. La carga no lo retira ni modifica.

## Ejecución real y despliegue

Pendiente: integración del código, ensayo
y aplicación productiva y recuentos finales. No se ha desplegado una aplicación
ni aplicado la migración de metadatos de catálogo mediante esta tarea.
