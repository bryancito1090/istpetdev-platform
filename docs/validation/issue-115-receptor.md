# Issue #115: recepción PWA con identidad del Receptor

Fecha: 2026-10-15. Rama: `feature/115-receptor-recepcion-real`, basada en `origin/develop`.

## Comportamiento y contrato

La pantalla del Receptor consulta `GET /api/v1/receiver/handling-units/{id}` con `X-Trace-Token` y guarda una copia por cuenta y hash del token QR. No usa la asignación ni el endpoint del Conductor. Sin red solo puede recuperar un bulto consultado previamente por esa cuenta con ese mismo QR; errores 403/404 nunca habilitan el caché.

Cada línea captura cantidad aceptada, rechazo derivado, motivo V1, stock en percha ANTES de recibir (`remainingQuantity`) y pedido siguiente (`nextRequestedQuantity`). El catálogo usa los códigos de la rama de #117: `Faltante`, `Sobrante`, `Daño`, `PrecintoViolado`, `LoteNoCoincide`, `CadenaDeFrio`. Se reutiliza su catálogo de dominio sin incorporar sus fixtures.

Cuando hay rechazo, la PWA exige motivo de catálogo y una imagen JPEG/PNG/WebP de hasta 10 MB. Datos y Blob se guardan juntos en IndexedDB antes de enviar. El servidor también valida el motivo del Receptor; la obligatoriedad de la foto se comprueba en la captura PWA, pues el archivo se sube después de asentar el recibo según el contrato 004. La recepción puede estar asentada mientras su evidencia sigue pendiente.

El outbox conserva `receiverContext` (unidad y capacidad QR), `ownerAccountId`, UUID estable de operación, payload y hashes. No guarda tokens OAuth/access/id/refresh ni Bearer. La capacidad del QR es necesaria para reproducir la operación offline y queda asociada a su cuenta; no se muestra en la cola. Cambiar de cuenta impide enviar la siguiente petición. No se modifica el contenido de una operación ya enviada para resolverla con otra versión.

La sincronización llama `POST /api/v1/receiver/handling-units/{id}/receipt`, con idempotencia estable. La respuesta se conserva antes de subir la foto: reintentar evidencia no vuelve a asentar el recibo. Dos intentos concurrentes locales comparten petición. 401 o cuenta desactivada pasan a `SessionRequired`, con captura conservada; 409/422 pasan a revisión. Una nueva conexión reanuda las operaciones elegibles, o el usuario puede pulsar Reintentar en la cola.

## Almacenamiento real

`Evidence` contiene metadatos y referencia `ReceiptId`; el archivo queda en S3 privado, fuera de PostgreSQL. La autorización es exclusiva de quien registró el recibo (Conductor, Receptor o Gerencia), con ID determinista por recibo/tipo/operación. Otra cuenta recibe 403. Cambiar bytes/tipo/hash con la misma operación da 409.

Se genera PUT prefirmado de diez minutos con checksum SHA-256. La PWA realiza el PUT; no acepta las URLs ficticias `istpetdev.local`. La confirmación comprueba bytes, tipo y hash leyendo el objeto en S3 antes de marcar `Confirmed`. Sin archivo o sin bucket configurado devuelve 503 y se conserva el pendiente. No hay migración ni nueva tabla.

Configurar en el host `Evidence__S3__Bucket` con el output Terraform `evidence_bucket_name` y `Evidence__S3__Region`. El Compose de producción reenvía ambas variables. Las credenciales usan la cadena estándar AWS; para EC2, rol IAM con `s3:PutObject` y `s3:GetObject` sobre `arn:aws:s3:::<bucket>/evidences/*`. Mantener acceso público bloqueado. Terraform incluye los dos orígenes HTTPS de la aplicación en CORS PUT y permite `content-type`/`x-amz-checksum-sha256`. Los cambios de configuración/infraestructura se entregan para revisión; no se aplicó Terraform ni se desplegó producción.

## Verificación y límites

Pruebas automatizadas: formulario conforme/parcial, motivo/foto obligatorios, cantidades inválidas, Blob persistido, endpoint correcto, replay, reintento de foto, caché por identidad/QR, cambio de cuenta durante envío, sesión desactivada y almacenamiento privado. Los tests S3 usan credenciales ficticias para firmar localmente y respuestas de objetos controladas; no llaman a AWS ni acreditan una carga al bucket desplegado.

La validación física ESC-04, ESC-06 y ESC-15 en dos teléfonos sigue PENDIENTE. No se dispone de esos dispositivos desde esta sesión. Tampoco se acredita una carga contra el bucket real configurado.

## Resultados locales

- `npm run check`: correcto (lint, ausencia de login simulado, backend, contratos, web, PWA y artefactos /campo/).
- `npm run test:frontend`: 160 pruebas correctas: 24 compartidas, 64 web y 72 móvil. La PWA se volvió a compilar y sus 72 pruebas pasaron tras limpiar el formulario al cambiar de cuenta.
- `dotnet test tests/IstpetDev.Tests --no-build --logger "trx;LogFileName=backend.trx" --results-directory artifacts/issue-115`: 415/415 correctas, sin omitidas. La primera ejecución tuvo un fallo transitorio de conexión PostgreSQL; la repetición completa quedó verde.
- `python docs/boveda/.github/scripts/check_vault.py`: 87 notas, 928 enlaces, 0 errores y 4 avisos de lenguaje preexistentes.
- `git diff --check`: correcto.
- Sin Terraform instalado en esta estación: no se ejecutaron `terraform validate` ni `apply`. Se añadió exclusivamente CORS HTTPS a la lista existente.

## Guion para los dos teléfonos

1. Instalar la PWA publicada por HTTPS en dos dispositivos, uno con Conductor y otro con Receptor. Verificar que está configurado S3 y su CORS/rol IAM.
2. ESC-04: despachar un bulto de prueba y escanear su QR en Receptor. Aceptar todo, declarar stock en percha/pedido y guardar. Sincronizar; verificar actor Receptor, saldo actualizado una vez y cola completa. Reintentar sin crear otro recibo.
3. ESC-06: usar otro bulto de 10 unidades, consultar con red, quitar señal, aceptar 6 y rechazar 4 por `Daño`. Intentar sin foto: se impide guardar. Adjuntar foto y guardar; recargar la cola y comprobar captura. Recuperar señal y verificar recibo parcial, rechazo en custodia pendiente, foto `Evidence.ReceiptId` correcta, objeto privado existente y hash coincidente. Con fallo de carga, solo reintentar evidencia.
4. ESC-15: capturar offline con Receptor A; entrar con B y verificar que no ve ni sincroniza la captura de A. Volver con A, desactivar A desde Gerencia antes de enviar; verificar 403, `Sesión requerida`, Blob conservado y ausencia de reintentos automáticos. Reactivar y reautenticar la misma cuenta; reintentar sin duplicar inventario.
5. Registrar fecha, versión/commit, modelos de ambos teléfonos, resultados observados y evidencias anonimizadas. Solo entonces completar los criterios físicos del issue.
