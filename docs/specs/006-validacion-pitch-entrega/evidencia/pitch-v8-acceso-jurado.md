# Pitch v8 y acceso del jurado — 17-oct-2026 (#371)

Solicitud autorizada por Bryan: crear pitch v8, guion, última diapositiva con QR grande al dominio de producción, cuenta Auditor y su credencial visible para el jurado; explicar Cognito y seguridad en diez líneas. Después, PR a `develop`, resolver conflictos y aprobar/integrar con CI verde.

## Alcance y materiales

Se conservan las nueve diapositivas de v7 y se añade acceso del jurado, sin nuevas cifras comerciales. Fuente: [guion v8](../guion-pitch-v8.md), con meta propuesta de 290 s (4:50). El generador `../materiales/generar_v8.py` reutiliza los constructores v7 y añade notas del presentador. Editable con GIF/animaciones y PDF con capturas fijas. El QR solo contiene `https://istpetdev.doicela.dev/`; no incluye la contraseña ni tokens.

Los archivos locales `pitch-v8.pptx`, `pitch-v8.pdf` y `render-v8/` muestran la credencial solicitada y quedan excluidos en `.git/info/exclude`. El archivo privado de origen es `~/.config/istpetdev/private/jurado-pitch-v8.json` (0600), fuera del repositorio. La copia `pitch-v8-revision.*` sustituye la contraseña por «Entregada al jurado» y es la que se versiona. No se sustituye la v7 de `docs/boveda/Entregable/` por una copia que contenga una credencial.

## Validación de los materiales

- Diez láminas tanto en PPTX como en PDF; texto y tablas de las primeras nueve comparados con v7, sin diferencias.
- OpenCV decodificó el QR de la última página del PDF y de la imagen incrustada en el PPTX: ambos devuelven exactamente `https://istpetdev.doicela.dev/`.
- Diez líneas de seguridad, diez entradas de guion y suma de tiempos de 290 s comprobadas por el generador. Guion PDF de tres páginas y DOCX editable, ambos con los mismos textos fuente.
- Inspección de los ZIP de PPTX/DOCX y texto extraído de los PDF: la contraseña real está en la última lámina de la copia destinada al jurado y no aparece en las copias de revisión ni en el guion.
- Inspección visual de la última lámina: QR grande con margen blanco, cuenta legible, diez líneas completas y dominio en dorado sobre azul. La lámina aparece sin clics de animación.
- `python docs/boveda/.github/scripts/check_vault.py`: 126 notas, 1273 enlaces, **0 errores**, cuatro avisos anteriores ajenos a esta tarea. `git diff --check`: correcto.

SHA-256 de los archivos versionables de `../materiales/`:

| Archivo | SHA-256 |
|---|---|
| `pitch-v8-revision.pptx` | `5963754cab0b2153bdba4c7b53de52e2d8318fa349eb844a85fa3679be52dd28` |
| `pitch-v8-revision.pdf` | `c795a52d03e4cdc739d5d142580b4f71774a178a44b21e0987d4167eb117988e` |
| `guion-pitch-v8.docx` | `ae9ffe3e5bcce3b39391165d1ec8d50e65b8f7e6e2a3a01519100e3883a386b2` |
| `guion-pitch-v8.pdf` | `18f1bb79ff836ad26146abc053163f5f70cea7c8d4010077d7d7f18e45d739c3` |

## Revisión prelaunch del acceso afectado

Skill aplicada: `docs/boveda/.local-skills/istpetdev-prelaunch/SKILL.md`. Se contrastaron sus referencias de verificación de seguridad, esquema, RBAC, Plan de validación, dbflow y runbook; se reutiliza como antecedente la [revisión #286](revision-seguridad-286.md), sin presentarla como auditoría del runtime actual.

Base local del cambio: `704b558cfbd689e25accca0c78f6a63aea7a000f` (`origin/develop`). El navegador observó el despliegue vigente del dominio, cuya identidad de imagen/SHA no se inspeccionó; no se equipara ese runtime al código local de la rama. Se consultaron doce migraciones existentes en RDS, hasta `20261017152159_AddMetadataJsonCatalog`; no se aplicó ninguna migración.

| Control | Estado y evidencia | Límite |
|---|---|---|
| Identidad productiva | Verificado. Pool `us-east-1_4Y2j5DINp`, región `us-east-1`, cuenta AWS `747456040427`; usuario `jurado@expo.ec` (`sub=84d81448-a031-70fb-7d39-2a42371be95f`), nombre «Jurado Expo Clean 2026», estado `CONFIRMED`, habilitado. Alta con `MessageAction=SUPPRESS`, sin envío de correo; contraseña permanente simplificada para ingreso rápido del jurado. | Cuenta compartida para evaluación; no sirve para atribuir acciones a una persona. No se afirma verificación de propiedad de un buzón. |
| Cuenta en base de producción | Verificado. RDS `istpetdev-rds-dev`, base `istpetdev_integration`, organización fija `01990000-0000-7000-8000-000000000001`, Account `01a14b8d-ccfe-77e7-a8fc-f09c0f435f91`, `Kind=human`, `Role=Auditor`, activa. Inserción transaccional con conflicto por `(Issuer, Subject)` sin cambiar cuentas existentes. | Solo alta de Account, autorizada expresamente. No se creó un login SQL para el jurado; usa navegador y API. No se guarda contraseña en PostgreSQL. |
| Conexión administrativa RDS | Verificado. Túnel SSM temporal al nodo `i-0bba84773e794de36`, puerto local 15433; libpq `sslmode=verify-full` y CA oficial de RDS; `current_database()=istpetdev_integration`, sesión SSL activa. | No se alteraron esquema, políticas, grants ni reglas de red. |
| Inicio de sesión del jurado | Verificado en navegador Chromium/Playwright contra el dominio real con `jurado@expo.ec`. Landing 200, botón «Torre de Control», formulario Cognito, retorno OIDC a `/auth/callback` y llegada a `/inicio`. `/api/v1/me`: 200, Account esperada, rol `Auditor`, plataforma `web`; la interfaz muestra Auditor. | Comprobación del 17-oct, 20:43 UTC (15:43 Ecuador). No es prueba de teléfono físico ni de la red del recinto. |
| Permisos de lectura | Verificado con la misma sesión real: GET `route-plans?limit=1`, `risks` y `deliveries?limit=1` respondieron 200. | Se verifican estas rutas, no todas las lecturas posibles. |
| Escrituras bloqueadas | Verificado: POST `route-plans`, `incidents`, `accounts`, `demo/reset`, PUT `route-plans/{uuid-nulo}/driver` y POST `route-plans/{uuid-nulo}/publish`: seis respuestas 403. Cuerpos vacíos y UUID nulo para evitar operaciones válidas incluso ante una regresión. | No se provocaron movimientos de inventario ni cambios de planificación. |
| Interfaz de solo lectura | Verificado en `/rutas`: cero botones visibles coincidentes con aprobar/publicar, generar plan o cancelar ruta. Capturas privadas `jurado-v8-inicio.png` y `jurado-v8-rutas.png`. | La autorización decisiva es la API; la observación de botones no sustituye esos 403. |
| HTTPS y cabeceras | Verificado HTTPS con certificado validado; respuesta pública 200 con `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN` y `Referrer-Policy: strict-origin-when-cross-origin`. | No se observó CSP ni HSTS en la respuesta consultada. No se cambiaron proxy ni cabeceras; no es declaración de hardening completo. |
| MFA | Inspeccionado: `MfaConfiguration=OFF`, contraseña mínima 14, con mayúscula, minúscula, número y símbolo. La credencial creada cumple esa política. | No afirmar segundo factor habilitado. No se cambió la configuración del pool para esta tarea. |
| RLS, campos, plantillas y archivos | No se modifican. La demo usa organización y roles fijos conforme ADR-21/26; el diseño Schema95/RLS es P2. | No se atribuye a esta alta una revalidación de RLS, cifrado por campo o cargas de evidencias. |
| Dependencias y despliegue | Sin cambios de dependencias/lockfiles de la aplicación ni despliegue. Herramientas de documentos instaladas en un venv temporal. | No se repitió un scan completo de la imagen productiva; no se declara lista una nueva release. |

La evidencia de ejecución sin tokens ni contraseña queda en `~/.config/istpetdev/private/jurado-pitch-v8-verification.json`; el script privado de prueba utiliza la credencial desde el archivo privado y nunca la imprime. Los tokens de la sesión de prueba solo se usaron en memoria.

## Pendientes operativos

Ensayo hablado de 4:50 y escaneo con teléfono físico, evaluación del jurado y posterior desactivación de la cuenta desde Usuarios por Gerencia. La cuenta permanece activa a petición del usuario y **no tiene caducidad automática**. La revisión no autoriza cambios adicionales de producción ni acredita una auditoría integral de la aplicación.

Fuente del texto sobre Cognito: [AWS, How authentication works with Amazon Cognito](https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-how-to-authenticate.html). Cognito autentica; la aplicación aplica por separado los permisos de `Account.Role`.
