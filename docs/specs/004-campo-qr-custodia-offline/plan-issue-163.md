# Plan de resolución del issue #163

Fecha: 2026-10-15. Destino: `develop`. Rama: `fix/163-sincronizar-evidencias-receptor`.

## Problema detectado

1. **Sincronización silenciosa e incompleta**:
   - `syncAllPending()` en `apps/mobile/src/app/field/outbox/field-outbox.service.ts` solo procesa estados `SavedOnDevice`, `EvidencePending` y `Sending`.
   - Si `isSimulatingOffline()` está activo o no hay red (`!navigator.onLine`), la función retorna de inmediato sin emitir ningún aviso, advertencia o error.
   - Los errores individuales dentro del bucle de ítems se capturan en un `catch {}` vacío, y en `sync.page.ts` se invocan con `.catch(() => {})`, de modo que el usuario pulsa «Sincronizar» y visualmente no ocurre nada.
   - Los ítems en `RequiresReview`, `SessionRequired`, `Conflict` o ítems legados sin cuenta se contabilizan en el badge `pendingCount`, pero el botón no los procesa, provocando la sensación de «hay pendientes pero Sincronizar no hace nada».

2. **Falla en almacenamiento de evidencias (fotos) en producción**:
   - `infra/compose/docker-compose.prod.yml` declara `Evidence__S3__Bucket: ${Evidence__S3__Bucket:-}` sin fallback ni paso de credenciales de AWS (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_REGION`).
   - Sin bucket o credenciales, la solicitud de URL prefirmada falla con `503 EVIDENCE_STORAGE_UNAVAILABLE` o `500`.
   - El outbox clasifica la falla como transitoria, preserva el ítem en `SavedOnDevice` con la recepción local ya guardada pero la evidencia pendiente, provocando reintentos continuos que fallan en silencio.

3. **Congelamiento de la pantalla del Receptor**:
   - Tras registrar una recepción con `saveReceiptAtomic`, la pantalla fija `receiverSaved = true`, desactivando el botón de guardar sin ofrecer una acción para limpiar la pantalla, registrar otro bulto o navegar a la cola outbox.

4. **Falta de visibilidad de evidencias confirmadas**:
   - En la cola outbox de la PWA no se distingue visualmente si la foto o firma ya fue confirmada con el servidor (`photoEvidenceId` / `signatureEvidenceId`).
   - En la web, el detalle de entregas no expone el conteo de evidencias confirmadas en S3.

---

## Solución planificada

### 1. Infraestructura y configuración de S3 (`infra/compose/docker-compose.prod.yml`)
- Pasar variables de AWS (`AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY`, `AWS_SESSION_TOKEN`, `AWS_REGION`) al contenedor `istpetdev-backend`.
- Soportar `EVIDENCE_S3_BUCKET` además de `Evidence__S3__Bucket` para paridad con convenciones de variables de entorno del servidor.

### 2. Servicio de Outbox y retroalimentación (`apps/mobile/src/app/field/outbox/field-outbox.service.ts`)
- Refactorizar `syncAllPending()` para retornar un objeto detallado `SyncSummary`:
  ```typescript
  export interface SyncSummary {
    attempted: number;
    synced: number;
    failed: number;
    blockedBySimulation?: boolean;
    blockedByOffline?: boolean;
    blockedBySessionHeld?: boolean;
    errors: Array<{ operationId: string; code?: string; message: string }>;
  }
  ```
- Detectar bloqueo por simulación offline, desconexión o retención de sesión y reportarlo explícitamente en el resultado.
- Permitir sincronizar ítems en `SessionRequired` si el propietario coincide con la sesión autenticada activa.
- Diferenciar el conteo:
  - `sendableCount`: ítems listos para envío inmediato.
  - `reviewCount`: ítems que requieren revisión manual (`RequiresReview`, `Conflict`).
  - Métodos para ítems legados sin cuenta: `claimLegacyItems(currentAccountId)` y `discardLegacyItems()`.

### 3. Vista de Sincronización PWA (`apps/mobile/src/app/field/pages/sync/sync.page.ts`)
- Notificaciones visuales claras (banner reactivo / mensaje de estado):
  - Avisar si «Sin señal (prueba)» está encendido con acción directa para apagarlo.
  - Informar resultado de sincronización: «N enviadas con éxito, M con error: [CÓDIGO]».
- Distinción gráfica de evidencias:
  - Mostrar `✓ Foto confirmada en servidor` / `✓ Firma confirmada` cuando `photoEvidenceId` / `signatureEvidenceId` estén presentes.
  - Mostrar estado pendiente de subida cuando aún no tengan identificador de evidencia confirmado.
- Reintento manual para cualquier ítem fallido o guardado.
- Botones de acción para ítems legados sin dueño («Asignar a mi cuenta» o «Descartar»).

### 4. Limpieza de pantalla del Receptor (`apps/mobile/src/app/home/home.page.ts`)
- Al registrar con éxito:
  - Mostrar mensaje de confirmación y estado de la cola.
  - Agregar botón «Registrar otro bulto / Limpiar» que reinicie el formulario y las señales (`receiverSaved`, `receiverActiveBox`, `receiverPhoto`, `receiverLines`, `receiverConfirmedMessage`).
  - Agregar botón «Ver en Cola de Sincronización» para inspeccionar el estado de la foto y el recibo.

### 5. Visibilidad en Backend y Web
- En `DeliveryDetailResponse` (`src/Application/Field/Dtos/FieldContracts.cs`) y `FieldService.cs`:
  - Contabilizar evidencias confirmadas (`Status == "Confirmed"`) asociadas a los recibos de la entrega.
- En la web (`apps/web/src/pages/novedades/` y `apps/web/src/pages/rutas/`):
  - Indicar el estado real de almacenamiento de la evidencia confirmada (`✓ Confirmada en almacenamiento S3`) en lugar de mostrar que el backend no almacena archivos.

### 6. Pruebas Automatizadas
- Pruebas unitarias en `apps/mobile/src/app/field/outbox/field-outbox.service.spec.ts`:
  - Retorno de diagnóstico ante simulación offline.
  - Retroalimentación con códigos de error visibles.
  - Reintento de ítems y manejo de ítems legados.
- Pruebas unitarias en `apps/mobile/src/app/home/home.page.spec.ts`:
  - Acción de limpieza y restablecimiento del estado del receptor.
- Verificación completa del repositorio:
  - `dotnet test`
  - `npm run check`
  - `npm run test:frontend`
  - `python3 docs/boveda/.github/scripts/check_vault.py`
  - `.github/scripts/branch-policy.sh`

---

## Pasos de Ejecución

1. [x] Rama `fix/163-sincronizar-evidencias-receptor` creada desde `origin/develop`.
2. [x] Documentar plan en `docs/specs/004-campo-qr-custodia-offline/plan-issue-163.md`.
3. [ ] Configurar variables de AWS y S3 en `infra/compose/docker-compose.prod.yml`.
4. [ ] Actualizar contratos y métodos en `FieldService.cs` y DTOs para conteo de evidencias confirmadas.
5. [ ] Refactorizar `FieldOutboxService` con diagnóstico de sincronización, tipos de error y manejo de legados.
6. [ ] Actualizar `SyncPage` con mensajes de estado, badges de evidencia y reintento visible.
7. [ ] Actualizar `HomePage` para limpieza de pantalla del Receptor.
8. [ ] Actualizar vistas web para reflejar evidencias confirmadas.
9. [ ] Implementar y ejecutar pruebas unitarias frontend y backend.
10. [ ] Ejecutar verificaciones de calidad, audit y política de ramas.
11. [ ] Crear Pull Request, verificar CI, mergear a `develop`, eliminar ramas y cerrar issue #163.
