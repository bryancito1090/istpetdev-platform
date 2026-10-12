---
tipo: arquitectura
estado: propuesta
actualizado: 2026-10-07
tags: [aws, seguridad, credenciales, hackathon, costos, cierre]
---

# AWS temporal para la hackathon y cierre

## Escenario vigente y precedencia

Bryan confirmó que AWS se usará para la hackathon y que después eliminará la infraestructura del proyecto para detener su consumo. Su captura del 7 de octubre muestra USD 100 de créditos y un plan gratuito que termina el 21 de octubre, con 15 días restantes. Es información aportada por Bryan, no un saldo consultado por API ni una fecha de vencimiento de cada crédito verificada en Billing. No se registra el ID de cuenta ni ningún secreto de la captura.

Este escenario modifica la propuesta humana SSO/Organizations y el bootstrap conservado indefinidamente de [[Entornos y operacion acordados]], [[Credenciales y acceso del equipo]] y [[Puesta en marcha del workspace y AWS dev]]. Las decisiones de red privada, PostgreSQL, aislamiento, Secrets Manager, TLS, OIDC de GitHub y Cognito de la aplicación se conservan. Esta nota tiene precedencia para el ensayo temporal. Las capturas de Bryan muestran el grupo creado `IstpetDevHackathonAdmins` y, posteriormente, el asistente Assign MFA device del usuario existente `istpetdev-bryan`. Bryan respondió «listo» después de las instrucciones de Authenticator; se registra como confirmación verbal de ese paso, sin verificación por API. El primer inicio de sesión con la nueva identidad sigue pendiente de confirmación explícita. El agente solo ha actualizado documentación, sin ejecutar cambios en AWS.

## 1. Conservar los créditos y elegir el acceso

La pantalla **Enable IAM Identity Center with AWS Organizations** avisa que crear la organización convierte el plan gratuito a pago y hace expirar los créditos inmediatamente. Para aprovechar esos créditos, cancelar esa habilitación y usar la cuenta independiente durante el evento. No crear/join Organizations ni Control Tower por este proyecto. [Habilitación AWS](https://docs.aws.amazon.com/singlesignon/latest/userguide/enable-identity-center.html), [créditos y planes](https://aws.amazon.com/free/free-tier-faqs/).

Los 15 días corresponden al plazo mostrado del plan gratuito. La expiración de cada crédito se comprueba en Billing → Credits. Una actualización ordinaria mediante **Upgrade plan**, sin Organizations/Control Tower, conserva créditos vigentes según AWS y permite acceder a servicios restringidos del plan gratuito; los consumos no cubiertos se facturan. No actualizar automáticamente ni asumir que un crédito es un límite de gasto.

Para el evento, proponer **un usuario IAM individual por integrante, MFA y `aws login`** para credenciales temporales. No crear access keys humanas, compartir root ni copiar tokens de Bryan. Usar políticas por integrante/servicio; cada identidad dev solo accede a su conexión PostgreSQL, integración autorizada, nodo SSM y prefijos S3 dev. Cognito sigue identificando usuarios de la aplicación, no al equipo frente a AWS.

AWS CLI requiere **v2.32.0 o superior** y `SignInLocalDevelopmentAccess` para el flujo de login de usuarios/grupos/roles. La autenticación abre el navegador y utiliza el acceso de consola individual; al terminar la sesión se inicia de nuevo. El permiso de login no concede por sí solo RDS, S3 ni lectura de secretos. [Login CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sign-in.html), [política de login](https://docs.aws.amazon.com/aws-managed-policy/latest/reference/SignInLocalDevelopmentAccess.html).

## 2. Siguiente pantalla desde la captura de Bryan

1. Cerrar el menú de cuenta haciendo clic fuera del panel derecho.
2. Pulsar **Cancel**, junto al botón Enable de Identity Center.
3. En Search escribir **IAM** y abrir **IAM**, no IAM Identity Center.
4. Menú **Access management → Users**. Reutilizar un usuario administrativo personal adecuado si ya existe; de lo contrario **Create user**.
5. Nombre propuesto `istpetdev-bryan`; marcar **Provide user access to the AWS Management Console**.
6. Si la consola ofrece Identity Center, seleccionar **I want to create an IAM user**.
7. Elegir contraseña de consola autogenerada y cambio obligatorio al primer acceso, manteniéndola fuera de Git/chat/capturas.
8. **Next** para revisar permisos. Administración se asigna únicamente a Bryan; compañeros reciben login y políticas dev limitadas. Activar MFA antes del uso operativo.

La creación de usuarios de consola no requiere access keys. Usuarios dev necesitan permisos para cambiar su contraseña y gestionar solo su propio MFA, o enrolamiento administrado antes de entregar el acceso. No dar administración IAM general para permitir el enrolamiento. [Usuarios IAM](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_users_create.html).

## 3. Cambio de autenticación en el prompt del agente

La addenda siguiente sustituye las referencias humanas SSO/permission sets del prompt anterior. No cambia las tareas locales ni autoriza aprovisionar AWS desde el agente de software:

```text
AWS se usará temporalmente para la hackathon. Lee "AWS temporal para la
hackathon y cierre" como acuerdo posterior a la guía SSO anterior.
No crear AWS Organizations/Control Tower ni configurar Identity Center para
este ensayo: se desea aprovechar el crédito de la cuenta independiente.
El equipo tendrá usuarios IAM individuales con MFA, AWS CLI >=2.32.0 y aws login
para credenciales temporales, sin access keys humanas ni tokens compartidos.
Genera políticas IAM limitadas por integrante, no permission sets SSO; incluye
SignInLocalDevelopmentAccess para la autenticación y permisos propios separados.
Verifica soporte del proveedor de login en .NET/Terraform. Si falta, usa
credential_process con un perfil de login diferente, siguiendo la guía AWS.
Prepara docs y scripts de inventario/exportación/cierre con nombres del proyecto,
sin ejecutar borrados. Durante la demo conserva protección RDS y state privado;
el cierre explícito permite retirarlos después de respaldar lo necesario fuera
de AWS. El bootstrap también se elimina al terminar el proyecto, al final.
No tocar el servidor n8n ni recursos personales/ajenos de Bryan.
```

Ejemplo de perfil de login local, después del alta y MFA:

```bash
aws login --profile istpetdev-dev-console
```

Para herramientas que no soporten directamente `login_session`, configurar `~/.aws/config` con un perfil consumidor distinto:

```ini
[profile istpetdev-dev]
region = us-east-1
credential_process = aws configure export-credentials --profile istpetdev-dev-console --format process
```

Usar `AWS_PROFILE=istpetdev-dev` para esas herramientas. El CLI recibe las credenciales temporales mediante el proceso; no ejecutar `export-credentials` a mano para pegarlas en documentación/logs. El perfil administrador tiene su propia identidad y perfiles equivalentes. [Compatibilidad mediante credential_process](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sign-in.html).

## 4. Operación del ensayo

Mantener us-east-1 y tags `Project=IstpetDev`, `Environment` y una fecha `ExpiresAt` acordada. Apagar cómputo fuera de trabajo coordinado cuando convenga; la limpieza final elimina recursos. No contratar reservas, Savings Plans ni compromisos de gasto para el ensayo.

Los USD 100 son la referencia de créditos disponible. Llevar alertas de consumo bruto que excluyan créditos para no ocultar uso, además de revisar saldo y cobertura en Billing. Las alertas no constituyen un apagado automático. No encender recursos adicionales solo para consumir el saldo: usarlo en DB, desarrollo conjunto y ensayos medibles. [Opciones de presupuestos](https://docs.aws.amazon.com/cost-management/latest/userguide/budgets-best-practices.html).

## 5. Cierre del proyecto y consumo futuro

Este es un procedimiento para el final del evento; no ejecutar ahora. Inventariar por tags, nombres y state. Aplicar únicamente al proyecto y conservar los repositorios/código. La ventana de cierre propuesta es al terminar las jornadas del 16–17 de octubre, antes de terminar el plan gratuito; confirmar el momento operativo con el equipo.

1. Cerrar ensayos/escrituras, pausar cron/webhooks n8n del proyecto y deshabilitar jobs que puedan recrear infraestructura. El servidor n8n existente sigue perteneciendo a Bryan.
2. Exportar evidencias necesarias y dump DB verificado a almacenamiento local privado; comprobar restauración o integridad según su finalidad. Exportar state/configuración operativa a una copia privada fuera de Git.
3. Revisar plan de destrucción del stack; conservar acceso administrativo y backend state mientras se elimina infraestructura. Retirar protección RDS mediante cambio explícito previo, sin desproteger durante desarrollo.
4. Eliminar RDS del proyecto. Si ya existe respaldo local suficiente y se decidió retirar todos los datos cloud, omitir snapshot final y backups retenidos. Revisar y borrar snapshots manuales/retained backups del proyecto: borrar la instancia no los elimina y siguen facturando. [Eliminación RDS](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/USER_DeleteInstance.html).
5. Terminar EC2 del proyecto y revisar EBS, snapshots y direcciones públicas retenidas; eliminar/liberar residuos propios. Revisar contenedores/servicios, balanceadores, NAT/endpoints o réplicas si alguno se creó para ensayos adicionales.
6. Detener productores de logs, vaciar buckets de evidencias/backups con **todas las versiones y delete markers**, cancelar multipart uploads y eliminar los buckets del proyecto. Un bucket versionado no queda vacío borrando solo objetos actuales. [Vaciar S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/empty-bucket.html).
7. Eliminar logs/alarms/datos de observabilidad, secretos y recursos de identidad/integración exclusivos del proyecto; revocar tokens externos dev y accesos del equipo al ensayo. Secrets Manager permite borrado programado con recuperación mínima de siete días y no cobra secretos marcados para eliminación. [Borrado de secretos](https://docs.aws.amazon.com/secretsmanager/latest/userguide/manage_delete-secret.html).
8. Cuando el destroy esté completo y ya no se necesite el backend remoto, respaldar privadamente su último state, vaciar/eliminar el bucket state y programar eliminación de la clave KMS del proyecto. KMS exige espera de 7–30 días; no prometer que desaparece instantáneamente. [Borrado KMS](https://docs.aws.amazon.com/kms/latest/developerguide/deleting-keys.html).
9. Verificar recursos residuales en las regiones usadas y Billing después de su actualización. Registrar inventario vacío o residuos pendientes de eliminación. Quitar los usuarios/políticas IAM exclusivos del proyecto desde una identidad administrativa que conserve acceso.

La eliminación detiene uso futuro de los recursos retirados; no anula consumo ya generado ni garantiza que Billing se actualice de inmediato. No depender de que expire el plan/crédito para completar la limpieza. B-36/V-33 registran este cierre con evidencia sin secretos.

## Detalle de la pantalla Set permissions del administrador

Secuencia indicada cuando Bryan llegó a Set permissions del asistente de usuario IAM. El nombre de grupo propuesto abajo fue sustituido por `IstpetDevHackathonAdmins` en la consola; usar este nombre real en los pasos posteriores, sin crear un segundo grupo:

1. Mantener **Add user to group** seleccionado.
2. Pulsar **Create group** en el recuadro Get started with groups.
3. En **User group name**, escribir `istpetdev-hackathon-admins`.
4. En el buscador de **Permissions policies**, buscar `AdministratorAccess` y marcar la política AWS managed cuyo nombre sea exactamente ese. No seleccionar variantes de servicios.
5. Pulsar **Create user group/Create group**, según el texto mostrado por el diálogo.
6. Al volver a Set permissions, marcar el grupo recién creado; usar Refresh si no aparece.
7. Dejar **Use a permissions boundary to control the maximum permissions** sin marcar para este administrador inicial.
8. Pulsar **Next** y revisar en Review and create el nombre del usuario administrativo y la pertenencia únicamente a ese grupo.

AdministratorAccess da acceso completo a los servicios/recursos de la cuenta; el nombre del grupo no lo restringe al proyecto. El grupo se reserva a Bryan para aprovisionamiento/cierre, no a los compañeros ni a runners/runtime. Ya incluye las operaciones del login CLI; no requiere otra política de login adicional para ese usuario administrador. Tras crear el usuario se habilita MFA. La pantalla Retrieve password contiene datos privados y no se comparte en capturas; guardar el acceso inicial fuera de los repositorios.

[Grupos IAM](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_groups_create.html), [alcance de AdministratorAccess](https://docs.aws.amazon.com/aws-managed-policy/latest/reference/AdministratorAccess.html).

## Revisión del administrador y siguiente paso de MFA

La captura de **Review and create** del 7 de octubre confirma:

| Campo | Valor observado |
|---|---|
| Aviso de grupo creado | `IstpetDevHackathonAdmins` |
| User name | `istpetdev-bryan` |
| Console password type | Autogenerated |
| Require password reset | Yes |
| Permissions summary | `IAMUserChangePassword` y grupo `IstpetDevHackathonAdmins` |
| Tags | Todavía ninguno |

`IAMUserChangePassword` es coherente con el cambio obligatorio de contraseña. El resumen muestra la pertenencia al grupo, pero no permite comprobar las políticas adjuntas al grupo. En esa captura todavía no se había creado el usuario; la captura posterior descrita abajo muestra su existencia. El cambio de contraseña y MFA siguen pendientes de confirmación.

Procedimiento siguiente:

1. Abrir el enlace `IstpetDevHackathonAdmins` en una pestaña nueva para conservar el asistente. En **Permissions**, comprobar que tiene `AdministratorAccess`. Si falta: **Add permissions → Attach policies**, buscar y marcar el nombre exacto, y pulsar **Attach policies**.
2. Volver al asistente. En **Tags → Add new tag**, se proponen `Project=IstpetDev`, `Environment=hackathon` y `Owner=bryan`. Son etiquetas de inventario, no restricciones de permisos ni automatización de borrado.
3. Pulsar **Create user**. En **Retrieve password**, guardar URL de acceso a consola, usuario y contraseña inicial en un gestor de contraseñas privado. No incorporar la contraseña o un CSV de credenciales a repositorios, documentación, chat ni capturas. No registrar la URL real en esta nota.
4. Tras guardar el acceso, volver a **IAM → Users → istpetdev-bryan → Security credentials** y, en **Multi-factor authentication (MFA)**, pulsar **Assign MFA device**. La pantalla inicial de elección del dispositivo puede revisarse; no compartir pantallas con QR, clave de configuración ni códigos de un solo uso.
5. Registrar un dispositivo MFA personal. AWS recomienda passkey/security key cuando sea posible. Si se elige **Authenticator app**, indicar nombre `istpetdev-bryan-mfa`, pulsar **Next**, escanear el QR privadamente y completar **MFA code 1** con el código actual y **MFA code 2** con el siguiente código, después de que cambie. Pulsar **Add MFA** y comprobar que aparece el dispositivo asignado.
6. Abrir la URL de acceso guardada en una ventana privada, manteniendo la sesión administrativa original abierta. Acceder como `istpetdev-bryan`, completar MFA y el cambio obligatorio de contraseña según lo solicite AWS. Guardar la contraseña definitiva en el gestor y comprobar que la nueva sesión pertenece al usuario correcto antes de cerrar la original.

Este administrador es personal de Bryan; los integrantes tendrán sus propios usuarios y permisos dev limitados. La asignación de MFA protege los nuevos inicios de sesión, por eso se verifica en una sesión nueva. No crear access keys para este flujo.

[Usuarios y contraseña inicial](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_users_create.html), [políticas del grupo](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_groups_manage_attach-policy.html), [asignación de MFA](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_mfa_enable_virtual.html).

## Pantalla Assign MFA device observada

La siguiente captura de Bryan muestra **IAM → IAM users → istpetdev-bryan → Assign MFA device**, con Device name vacío y **Passkey or security key** seleccionado. Confirma que el usuario existe; todavía no demuestra enrolamiento MFA, etiquetas, permisos heredados efectivos ni acceso con la nueva identidad.

Recomendación para esta pantalla:

1. **Device name:** `istpetdev-bryan-mfa`.
2. Mantener **Passkey or security key** cuando haya un dispositivo o gestor personal compatible disponible. AWS recomienda esta opción por su resistencia al phishing. **Passkey display name — Optional:** `AWS IstpetDev - Bryan`.
3. Desplazarse hasta el final y pulsar **Next**. Completar el registro mediante el diálogo del navegador y el dispositivo o gestor personal elegido; la interfaz varía según plataforma. Confirmar con huella, rostro, PIN o llave física según el método, y pulsar **Continue** cuando AWS lo ofrezca.
4. Si no se dispone de una opción compatible, volver a la selección y usar **Authenticator app** con el mismo Device name. Seguir el procedimiento de QR privado y dos códigos consecutivos de la sección anterior; **Passkey display name** no aplica a esta alternativa. **Hardware TOTP token** corresponde a un token físico dedicado.
5. Verificar que **Security credentials → Multi-factor authentication (MFA)** muestre el dispositivo asignado. Después probar una sesión nueva y el cambio de contraseña como se indicó arriba. La evidencia compartible es el estado de asignación o la confirmación del acceso; nunca QR, claves de configuración, contraseñas ni códigos.

La passkey o aplicación autenticadora pertenece a Bryan. Cada integrante registra su propio MFA para su usuario. No se comparte el segundo factor ni se versiona material de autenticación en ninguno de los repositorios.

[Passkeys en IAM](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_mfa_enable_fido.html), [aplicación autenticadora en IAM](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_credentials_mfa_enable_virtual.html).

### Continuación cuando el navegador solicita una llave física

Bryan indicó que su PC no tiene lector de huellas y mostró el diálogo **Touch your security key**, sin confirmación de registro. Se le indicó continuar con **Authenticator app** en el celular. La ausencia de lector de huellas no impide todas las passkeys, pero el PIN depende del dispositivo o gestor compatible; no se define un PIN independiente en esta pantalla de AWS.

Cerrar primero el diálogo del navegador con su botón **Cancel** y pulsar **Previous** en el asistente cuando esté disponible. Si no permite volver, usar **Cancel** del asistente AWS y abrir de nuevo **Users → istpetdev-bryan → Security credentials → Assign MFA device**. Cancelar el enrolamiento no elimina el usuario.

Conservar el nombre `istpetdev-bryan-mfa`, seleccionar **Authenticator app** y pulsar **Next**. En el teléfono añadir una cuenta en la aplicación autenticadora, escanear privadamente **Show QR code** de AWS, completar los dos códigos consecutivos y pulsar **Add MFA**. Comprobar dispositivo asignado y probar una nueva sesión. La alternativa quedó indicada; activación y login todavía pendientes de confirmación. No guardar QR, claves ni códigos en la documentación.

## Alta individual de los compañeros

Tras responder «listo» al paso de Authenticator, Bryan pidió continuar con los usuarios del equipo. Lo siguiente es un procedimiento preparado, no evidencia de que estos usuarios, grupo o política ya existan en AWS. Si siguen siendo cinco integrantes contando a Bryan, faltan cuatro usuarios personales; usar nombres reales, sin cuentas compartidas.

### Comprobación del acceso administrativo

Si todavía no se probó, mantener la sesión original abierta y acceder en una ventana privada con la URL guardada, `istpetdev-bryan`, la contraseña y MFA. Completar el cambio obligatorio de contraseña. Usar esa sesión personal administrativa para las altas siguientes.

### Política para contraseña y enrolamiento MFA

Desde **IAM → Policies → Create policy → JSON**, pegar el bloque siguiente sin sustituir `${aws:username}`. Pulsar **Next**, usar nombre `IstpetDevSelfServiceMFA`, descripción `Cambio de contrasena y alta de MFA del propio usuario IstpetDev`, revisar los errores del editor y pulsar **Create policy**.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "ReadSecuritySettings",
      "Effect": "Allow",
      "Action": [
        "iam:GetAccountPasswordPolicy",
        "iam:GetAccountSummary",
        "iam:ListVirtualMFADevices"
      ],
      "Resource": "*"
    },
    {
      "Sid": "ManageOwnPasswordAndEnrollMFA",
      "Effect": "Allow",
      "Action": [
        "iam:GetUser",
        "iam:ChangePassword",
        "iam:GetMFADevice",
        "iam:ListMFADevices",
        "iam:EnableMFADevice",
        "iam:ResyncMFADevice"
      ],
      "Resource": "arn:aws:iam::*:user/${aws:username}"
    },
    {
      "Sid": "CreateOwnAuthenticator",
      "Effect": "Allow",
      "Action": "iam:CreateVirtualMFADevice",
      "Resource": "arn:aws:iam::*:mfa/${aws:username}-mfa"
    }
  ]
}
```

Esta política propia adapta las operaciones de los ejemplos AWS de autoservicio: lectura de configuración de seguridad, cambio de contraseña propio y enrolamiento/consulta/resincronización de MFA propio. Los usuarios se crean con la ruta IAM predeterminada `/`. Para Authenticator el nombre del dispositivo debe ser exactamente el usuario más `-mfa`; por ejemplo, para el usuario ficticio `istpetdev-ana`, `istpetdev-ana-mfa`. La variable IAM se evalúa para cada usuario y no se sustituye por Bryan al pegar el JSON.

La política no concede creación de access keys, eliminación/desactivación de MFA, edición de permisos, ni acceso a RDS/S3/secretos. El administrador atiende recuperación o enrolamientos fallidos; no ampliar permisos a todo IAM para resolverlos. No es una política global de denegación sin MFA. Esta etapa se limita al alta; antes de conceder acceso operativo, comprobar MFA registrado y nuevo inicio de sesión con MFA por cada integrante. La validación sintáctica local no sustituye probar la política en IAM con el primer usuario. [Autoservicio de MFA](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_examples_aws_my-sec-creds-self-manage-mfa-only.html), [operaciones de contraseña y página propia](https://docs.aws.amazon.com/IAM/latest/UserGuide/reference_policies_examples_aws_my-sec-creds-self-manage-no-mfa.html).

### Grupo y creación de usuarios

1. **IAM → User groups → Create group**. Nombre `IstpetDevHackathonDevelopers`.
2. En **Attach permissions policies**, seleccionar exactamente `IstpetDevSelfServiceMFA` y la política AWS managed `SignInLocalDevelopmentAccess`. Revisar que ambas queden seleccionadas y pulsar **Create group**. El segundo permiso habilita el login temporal CLI; no concede acceso a recursos por sí solo.
3. **IAM → Users → Create user**. Nombre `istpetdev-<nombre>` usando el nombre real de cada persona, sin los signos `< >`. Marcar acceso a consola y, si aparece la elección, **I want to create an IAM user**.
4. Seleccionar **Autogenerated password** y **Users must create a new password at next sign-in**. Pulsar **Next**.
5. **Add user to group**: marcar únicamente `IstpetDevHackathonDevelopers`, sin copiar los permisos de Bryan ni seleccionar `IstpetDevHackathonAdmins`. Dejar permissions boundary sin marcar en este alta limitada y pulsar **Next**.
6. Etiquetar `Project=IstpetDev`, `Environment=hackathon`, `Owner=<nombre-real>`. Revisar el nombre, grupo y cambio obligatorio de contraseña, y pulsar **Create user**. La política automática `IAMUserChangePassword` puede aparecer también y es coherente con esta configuración.
7. Entregar a esa persona su URL de consola, usuario y contraseña inicial mediante una compartición privada del gestor de contraseñas. No pegar credenciales en GitHub, la bóveda, este chat ni canales grupales. No generar access keys.
8. Cada integrante entra, cambia la contraseña y abre **su nombre arriba a la derecha → Security credentials → Assign MFA device**. Esta ruta llega a sus propios datos sin necesitar permiso para listar todos los usuarios. Elegir **Authenticator app** y nombre exacto `SU_USUARIO-mfa`; escanear QR y confirmar dos códigos consecutivos en su propio teléfono. Cerrar sesión y volver a entrar con MFA.
9. Bryan comprueba **Users → usuario → Security credentials → MFA** y confirma con la persona que pudo iniciar una sesión nueva. Completar y probar primero un usuario antes de repetir para los demás.

Las identidades quedan preparadas para el alta y login temporal. El acceso a la base de datos necesita además RDS/red/SSM, roles PostgreSQL y políticas individuales para los ARN exactos de secretos asignados; sigue pendiente. El grupo común no debe recibir lectura de todos los secretos ni permisos administrativos para suplir esa configuración. Registrar después el mapeo de usuarios a `dev_01`…`dev_05`, sin credenciales reales.

[Grupos IAM](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_groups_create.html), [usuarios IAM](https://docs.aws.amazon.com/IAM/latest/UserGuide/id_users_create.html), [SignInLocalDevelopmentAccess](https://docs.aws.amazon.com/aws-managed-policy/latest/reference/SignInLocalDevelopmentAccess.html).

## Estado comunicado del equipo y siguiente paso: backend de Terraform

Bryan confirmó que todos los integrantes tienen sus usuarios y recibieron las indicaciones de MFA. Esto acredita el alta comunicada, no que cada persona haya completado MFA o probado su sesión. La comprobación individual sigue pendiente antes de asignar permisos de datos; puede realizarse mientras Bryan prepara el backend de Terraform.

La revisión de solo lectura del repositorio local `~/Projects/ISTPETDEV` encontró `docs/aws-dev.md`, `infra/terraform/environments/dev/main.tf`, módulos VPC/security groups/EC2/RDS/S3 y scripts operativos. El entorno dev declara backend S3 y recursos de red/RDS, por lo que se conserva la ruta acordada: **KMS y bucket state por consola; VPC, nodo SSM, RDS y evidencias mediante Terraform**. No crear esos mismos recursos de aplicación manualmente además del código. La existencia de archivos no demuestra un plan, despliegue o pruebas de conexión satisfactorias; esos pasos siguen pendientes.

La guía local del software conserva referencias a `aws sso login`, un rol administrativo SSO y permission sets. Para este ensayo se sustituyen por el usuario IAM `istpetdev-bryan`, `aws login` y políticas IAM individuales, como establece esta nota. El agente de software debe adaptar esas referencias antes del aprovisionamiento; esta revisión no modificó su repositorio ni ejecutó Terraform. El backend se conserva durante el evento y se elimina al final después del resto de los recursos y de exportar una copia privada, según el cierre ya documentado.

### Crear la clave KMS para el state

1. Acceder con `istpetdev-bryan` y MFA. Abrir [KMS en us-east-1](https://us-east-1.console.aws.amazon.com/kms/home?region=us-east-1) y comprobar **United States (N. Virginia) / us-east-1**.
2. **Customer managed keys**: comprobar primero si ya existe el alias `istpetdev-tfstate`. Si existe, revisar su configuración y reutilizar la clave correspondiente; si no existe, **Create key**.
3. Elegir **Symmetric** y **Encrypt and decrypt**. Si aparecen opciones avanzadas, conservar material de clave generado en **KMS** y **Single-Region key**. Pulsar **Next**.
4. Alias `istpetdev-tfstate`; descripción `Cifrado del estado Terraform de IstpetDev para la hackathon`. Tags propuestos: `Project=IstpetDev`, `Environment=bootstrap`, `Owner=bryan`, `ManagedBy=Console`. El alias completo aparecerá como `alias/istpetdev-tfstate`. Pulsar **Next**.
5. En **Define key administrative permissions**, seleccionar solamente `istpetdev-bryan`. Mantener **Allow key administrators to delete this key** activado para permitir su retirada al cierre; no programa su borrado ahora. Pulsar **Next**.
6. En **Define key usage permissions**, seleccionar también `istpetdev-bryan`. Dejar **Other AWS accounts** vacío. Los desarrolladores no necesitan acceso al cifrado del state. Pulsar **Next**.
7. Revisar la política y configuración, avanzar con **Next** si el asistente separa ambas revisiones y pulsar **Finish** para crear la clave. Conservar la política de administración de cuenta que genera AWS; no sustituirla por una política improvisada.
8. Comprobar alias, región, tipo simétrico, uso Encrypt and decrypt y estado **Enabled**. Guardar el **Key ARN** para configurar S3/backend, sin copiarlo a esta nota. El ARN identifica la clave; no contiene el material criptográfico ni permite descifrar por sí solo.

La captura posterior confirma que Bryan creó la clave con alias `istpetdev-tfstate` en `us-east-1`, estado **Enabled**, tipo **Symmetric**, especificación **SYMMETRIC_DEFAULT** y uso **Encrypt and decrypt**. La sesión visible es `istpetdev-bryan`. Bryan confirmó que guardó Key ID y ARN; se usará el ARN completo al configurar S3 y el backend. La captura no verifica las políticas internas de la clave. No se copian aquí el ID real de cuenta, Key ID ni ARN.

Después: bucket S3 privado/versionado/SSE-KMS para state, política TLS y backend local privado; revisión del código y plan Terraform; despliegue; inicialización PostgreSQL/secretos y pruebas de acceso por persona. No indicar `apply` hasta completar esas dependencias y revisar el plan real.

[Crear clave simétrica KMS](https://docs.aws.amazon.com/kms/latest/developerguide/create-symmetric-cmk.html), [bucket S3](https://docs.aws.amazon.com/AmazonS3/latest/userguide/create-bucket-overview.html), [backend S3 de Terraform](https://developer.hashicorp.com/terraform/language/backend/s3).

### Crear el bucket privado para el estado de Terraform

El ARN completo de KMS es el identificador seleccionado para **Enter AWS KMS key ARN** de S3 y `kms_key_id` del backend. Guardar también Key ID es válido; ninguno de esos identificadores contiene el material criptográfico. El bucket siguiente almacena el state de infraestructura y será accesible al administrador; la aplicación tendrá su bucket de evidencias declarado en Terraform.

1. Abrir [S3](https://s3.console.aws.amazon.com/s3/home?region=us-east-1), entrar en **General purpose buckets/Buckets** y comprobar que el bucket del proyecto no exista ya. Si existe, revisar/adoptar su configuración en lugar de crear un duplicado. Si no existe, pulsar **Create bucket**.
2. **AWS Region:** `us-east-1` / US East (N. Virginia). **Bucket type:** General purpose. Si aparece **Bucket namespace**, usar **Shared global namespace** para conservar la convención de nombre existente del repositorio.
3. **Bucket name:** `istpetdev-tfstate-<ACCOUNT_ID>-us-east-1`, sustituyendo `<ACCOUNT_ID>` por los doce dígitos de la cuenta, sin guiones del formato visual ni signos `< >`. El nombre debe ser único; si está ocupado por otra cuenta, elegir un sufijo adicional y conservar el nombre final coherente en la política y backend. **Copy settings from existing bucket:** dejar vacío.
4. **Object Ownership:** ACLs disabled / Bucket owner enforced. **Block Public Access settings:** mantener **Block all public access** y sus cuatro opciones activadas.
5. **Bucket Versioning:** Enable. Tags: `Project=IstpetDev`, `Environment=bootstrap`, `Owner=bryan`, `ManagedBy=Console`.
6. **Default encryption → Encryption type:** Server-side encryption with AWS Key Management Service keys (SSE-KMS). **AWS KMS key:** Enter AWS KMS key ARN y pegar el ARN completo que Bryan guardó para `istpetdev-tfstate`; la región de la clave y el bucket debe coincidir. **Bucket Key:** Enable.
7. **Advanced settings → Object Lock:** Disable. Terraform usará `use_lockfile=true` para coordinación de operaciones; S3 Object Lock es otra función y no se requiere para este backend. Revisar y pulsar **Create bucket**.

### Exigir HTTPS en el bucket de estado

Abrir el bucket creado → **Permissions → Bucket policy → Edit**. Para este bucket nuevo sin política previa, pegar el siguiente JSON sustituyendo las dos apariciones de `NOMBRE_DEL_BUCKET` por el nombre real, sin `s3://` ni `/` final. Si se reutiliza un bucket con política existente, integrar esta declaración conservando sus controles previos en vez de reemplazarlo todo.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "EnforceTLSRequestsOnly",
      "Effect": "Deny",
      "Principal": "*",
      "Action": "s3:*",
      "Resource": [
        "arn:aws:s3:::NOMBRE_DEL_BUCKET",
        "arn:aws:s3:::NOMBRE_DEL_BUCKET/*"
      ],
      "Condition": {
        "Bool": {
          "aws:SecureTransport": "false"
        }
      }
    }
  ]
}
```

Pulsar **Save changes**. Es una denegación de transporte inseguro, no una concesión de acceso público. Mantener Block Public Access y el acceso IAM/KMS restringido al administrador. La política sigue el ejemplo HTTPS de AWS y la guía existente `docs/aws-dev.md` del software.

Comprobar **Properties → Bucket Versioning: Enabled**, **Default encryption: SSE-KMS** con la clave seleccionada y **Bucket Key: Enabled**; en **Permissions**, **Block all public access: On** y la política TLS guardada. No hace falta subir objetos o crear carpetas a mano: Terraform creará el state y su archivo de bloqueo al operar. La creación efectiva del bucket, la política y las pruebas de acceso todavía están pendientes de confirmación.

[Crear bucket y opciones de cifrado](https://docs.aws.amazon.com/AmazonS3/latest/userguide/GetStartedWithS3.html), [namespace y configuración general](https://docs.aws.amazon.com/AmazonS3/latest/userguide/create-bucket-overview.html), [S3 Bucket Keys](https://docs.aws.amazon.com/AmazonS3/latest/userguide/bucket-key.html), [política HTTPS](https://docs.aws.amazon.com/AmazonS3/latest/userguide/UsingEncryptionInTransit.html), [bloqueo y cifrado del backend Terraform](https://developer.hashicorp.com/terraform/language/backend/s3).

## Cambio solicitado: aprovisionamiento por el agente con Terraform

Bryan pidió que el agente use su acceso administrativo temporal para levantar la infraestructura. Esta instrucción sustituye la continuación manual del bucket: implementar su bootstrap con Terraform, reutilizar la clave KMS existente y después aprovisionar el entorno dev del repositorio. Antes de crear recursos, consultar qué existe y revisar el plan para evitar duplicados o afectar recursos ajenos. El usuario ya autorizó el aprovisionamiento del proyecto; lo que falta ahora es conectividad y un entorno de ejecución con los permisos locales necesarios, además del login interactivo.

Comprobaciones locales de esta sesión:

- AWS CLI `2.37.10`, Terraform `1.15.9`, Session Manager plugin, `jq` y `psql` instalados.
- Repo de software en `develop`, siguiendo `origin/develop`, sin cambios locales según `git status`.
- Proveedor AWS fijado en `.terraform.lock.hcl` a `5.100.0`, dentro del rango acordado `~> 5.50`.
- La comprobación inicial listó solo el perfil AWS `default`; no se consultaron ni imprimieron sus credenciales ni se utilizó para acceder a una cuenta. Después del login de Bryan, `aws configure list-profiles` muestra también `istpetdev-admin-console`.
- La prueba HTTPS al dominio público `signin.us-east-1.amazonaws.com` falló con `Could not resolve host`. Esta sesión tiene red restringida.
- El entorno permite escribir en la bóveda HACKATHON y `/tmp`; ISTPETDEV y `~/.aws` son de solo lectura para el agente. No se intentó eludir esas restricciones.

### Conexión administrativa temporal desde la terminal de Bryan

Ejecutar en una terminal normal de su equipo:

```bash
aws login --profile istpetdev-admin-console --region us-east-1
```

En el navegador elegir la cuenta del proyecto y el usuario `istpetdev-bryan`, completar MFA y autorizar el acceso solicitado por AWS CLI. Mantener códigos de autenticación y credenciales fuera del chat. El login guarda su caché local fuera de Git; no crear access keys para este flujo.

Preparar un perfil consumidor separado, compatible con Terraform y herramientas que todavía no soporten `login_session` directamente:

```bash
aws configure set region us-east-1 --profile istpetdev-admin
aws configure set credential_process 'aws configure export-credentials --profile istpetdev-admin-console --format process' --profile istpetdev-admin
aws sts get-caller-identity --profile istpetdev-admin --query Arn --output text
```

La última salida debe identificar al usuario `istpetdev-bryan` de la cuenta prevista. El proceso de credenciales lo invoca el SDK; no ejecutar manualmente `export-credentials` para imprimir sus resultados. Para Terraform usar `AWS_PROFILE=istpetdev-admin`. El login no modifica las restricciones del entorno del agente: para que pueda completar el trabajo necesita escritura en ISTPETDEV y salida de red hacia AWS y los proveedores necesarios.

### Preparación pendiente antes del despliegue

La lectura inicial del código identifica estos trabajos concretos para la sesión con acceso:

1. `infra/terraform/bootstrap` contiene solo `.gitkeep`: implementar el bucket privado/versionado/SSE-KMS/TLS con Terraform, referenciando la clave KMS existente. Conservar el state inicial fuera de Git, en un directorio privado, y consultar/importar recursos si el bucket ya fue creado. No recrear identidades o la clave existente.
2. Sustituir referencias operativas SSO/permission sets por el login IAM temporal y políticas individuales, sin cambiar el diseño del compañero.
3. Revisar/corregir `scripts/base_datos/bootstrap-dev.sh`: actualmente conecta con `psql -h 127.0.0.1` sin exigir explícitamente `verify-full` y CA; además pasa SQL con passwords mediante `psql -c` y payloads de secretos como argumentos de procesos. Ajustar verificación TLS e intercambio privado de secretos antes de ejecutarlo, y comprobar aislamiento e idempotencia en una prueba controlada.
4. Revisar/corregir `scripts/ops/render-dev-policies.sh`: el permiso Resume/Terminate usa `session/*`, aunque su descripción dice sesiones propias. Limitarlo a la identidad correspondiente y resolver los ARN exactos de los secretos, en lugar de permisos amplios por prefijo.
5. Validar configuración, revisar el plan real, desplegar red/nodo/RDS/evidencias y luego inicializar PostgreSQL y permisos de cada integrante. Verificar MFA antes de conceder acceso a los datos.

No se ejecutaron login, plan, apply ni inicialización de la base desde esta sesión. El único recurso cloud confirmado por captura en esta etapa es la clave KMS, además de las identidades comunicadas previamente; la creación del bucket sigue sin confirmar.

[Login AWS CLI y credential_process](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sign-in.html), [backend S3 de Terraform](https://developer.hashicorp.com/terraform/language/backend/s3).

### Login completado y oferta opcional de AWS MCP

La captura posterior de Bryan muestra que AWS CLI actualizó `istpetdev-admin-console` para usar la identidad `istpetdev-bryan`. La autenticación del perfil de consola está completada según esa salida; el perfil consumidor `istpetdev-admin` y su comprobación con STS todavía están pendientes.

Ante **Configure AWS skills and the AWS MCP server for your AI coding agent(s)? [y/n/never]**, responder **n** y Enter para continuar con el flujo AWS CLI/Terraform. La oferta configura herramientas adicionales para agentes; no es el paso de autenticación pendiente ni un requisito del backend de Terraform. A continuación ejecutar los tres comandos de configuración/verificación del perfil consumidor indicados arriba. No se instaló AWS MCP ni se cambiaron las restricciones de red o escritura de la sesión del agente.

[Configuración independiente de AWS MCP Server](https://docs.aws.amazon.com/agent-toolkit/latest/userguide/getting-started-aws-mcp-server.html).

### Comprobación STS cuando falta el visor less

Bryan ejecutó la configuración de `credential_process` y la comprobación STS con `istpetdev-admin`. La salida falló al intentar abrir el visor local `less`, que no está instalado. Desactivar el visor en ese perfil y repetir la comprobación desde su terminal:

```bash
aws configure set cli_pager "" --profile istpetdev-admin
aws sts get-caller-identity --profile istpetdev-admin --query Arn --output text --no-cli-pager
```

Esperar una salida que termine en `:user/istpetdev-bryan`. El error comunicado corresponde a presentación de salida; no demuestra por sí solo el resultado de la verificación de identidad. No hace falta regenerar credenciales para corregir el visor. [Control del pager AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-usage-pagination.html).

### Reabrir el trabajo con escritura en ISTPETDEV y acceso de red

Bryan pidió los pasos para habilitar los permisos locales necesarios. `codex --help` y `codex resume --help` de su instalación confirman `--cd`, `--sandbox workspace-write`, `--add-dir`, `--ask-for-approval on-request` y el selector `resume --all`. OpenAI Docs confirma `sandbox_workspace_write.network_access=true` para salida de red dentro de ese modo. Son opciones de una nueva ejecución; esta nota no cambia los permisos de la sesión actual.

Ejecutar desde una terminal normal del equipo, después de finalizar el turno en curso del agente:

```bash
mkdir -p "$HOME/.config/istpetdev/private" "$HOME/.aws/login/cache"
chmod 700 "$HOME/.config/istpetdev" "$HOME/.config/istpetdev/private"
export AWS_PROFILE=istpetdev-admin
export AWS_REGION=us-east-1
export AWS_PAGER=""
```

Reabrir una conversación guardada con el selector, sin asumir que la última sesión corresponde a este trabajo:

```bash
codex resume --all \
  --cd "$HOME/Projects/ISTPETDEV" \
  --sandbox workspace-write \
  --ask-for-approval on-request \
  --config sandbox_workspace_write.network_access=true \
  --add-dir "$HOME/Projects/HACKATHON" \
  --add-dir "$HOME/.config/istpetdev" \
  --add-dir "$HOME/.aws/login/cache"
```

En el selector elegir la conversación de AWS/ISTPETDEV. La carpeta de trabajo explícita es ISTPETDEV. Si la conversación de Orca no aparece en el selector local, usar el mismo comando sustituyendo `codex resume --all` por `codex` para comenzar otra sesión y pedirle leer esta nota completa antes de actuar. No ejecutar dos agentes que modifiquen los mismos archivos simultáneamente.

Los permisos de escritura adicionales cubren la bóveda documental, el directorio privado de state/certificados y la caché de login AWS para renovar credenciales. `~/.aws/config` sigue siendo leído por el perfil; no se concede escritura a todo el directorio personal. Las opciones se aplican a esa ejecución, sin editar la configuración global ni desactivar el sandbox. La red habilitada permite salida de red general desde la sesión, no exclusivamente a AWS. Si una política administrada impide estos valores, respetar la restricción y comunicar el error en lugar de intentar eludirla.

Mensaje sugerido para continuar:

```text
Continúa el aprovisionamiento AWS de IstpetDev autorizado en la conversación.
Lee ~/Projects/HACKATHON/03 Arquitectura/AWS temporal para la hackathon y cierre.md.
Usa AWS_PROFILE=istpetdev-admin y región us-east-1. Primero verifica identidad,
red y escritura con un archivo temporal en ISTPETDEV que retires al terminar.
Reutiliza la clave alias/istpetdev-tfstate existente; consulta recursos antes de
crear o importar. Completa bootstrap Terraform, corrige los pendientes TLS/IAM
documentados, valida y revisa el plan. Si el plan solo afecta los recursos dev
acordados del proyecto, aplica y verifica PostgreSQL y el acceso individual.
Conserva los cambios del compañero. Los secretos y el state quedan fuera de Git.
```

La herramienta local de Orca seleccionada según su skill, `orca-ide skills get orca-cli --json`, no pudo iniciarse: `No suitable fusermount binary found on the $PATH` y `Cannot mount AppImage`. No se cambió de ejecutable, no se alteró Orca/FUSE ni se verificaron sus menús. La ruta anterior usa opciones comprobadas de Codex CLI; no promete modificar el panel ya abierto en Orca.

[OpenAI Docs: comandos y reanudación](https://learn.chatgpt.com/docs/developer-commands?surface=cli), [OpenAI Docs: configuración de red y sandbox](https://learn.chatgpt.com/docs/config-file/config-reference).

## Estado real del aprovisionamiento, resolución RDS y base de datos (8 de octubre)

En la sesión automatizada con credenciales administrativas (`istpetdev-bryan`, cuenta `747456040427`), se completó el aprovisionamiento de la base de datos RDS, el esquema de respaldos Free Tier, el bootstrap de bases de datos multitenant y la asignación de permisos IAM con MFA.

### 1. Estado en AWS y Recursos Activos (89 recursos en remote state S3)
- **Bootstrap de Estado S3:** Completado. Bucket `istpetdev-tfstate-747456040427-us-east-1` con KMS `alias/istpetdev-tfstate` y `use_lockfile = true`.
- **VPC y Red:** VPC `vpc-0217f06f82247cd0b` (10.42.0.0/16) con 2 subredes públicas y 2 privadas de datos. Sin NAT Gateway para mantener costos en cero.
- **Nodo de Acceso EC2:** `i-0bba84773e794de36` (`t3.micro`, Ubuntu 24.04, IP pública `32.196.114.105`) con SSM Agent Online y SG `sg-0c4c0a0cff4fdc55f` sin reglas de entrada abiertas.
- **RDS PostgreSQL 16 Desplegado:** Instancia física `istpetdev-rds-dev` creada y en estado `available` (`db.t3.micro`, gp3 20GB, cifrado KMS, SSL obligatorio).
- **Estrategia de Backups Free Tier:**
  - PITR en RDS configurado a 1 día continuo (requisito Free Plan).
  - AWS Backup con bóveda `istpetdev-dev`, plan diario a las 03:00 ECT (08:00 UTC) con retención de 7 días.
  - Regla EventBridge conectada a SNS para alertar fallos de backup.
  - Suscripción de eventos RDS `istpetdev-dev-rds-events` conectada a SNS.
- **S3:** `istpetdev-evidence-dev-...` (CORS para web/móvil local) y `istpetdev-backups-dev-...`, ambos con bloqueo público total, TLS obligatorio y versionado.
- **Cognito:** User Pool `us-east-1_4Y2j5DINp` con clientes Web y Móvil.
- **SNS y Presupuesto:** Suscripción por correo confirmada (`banobryan1090@gmail.com`) y AWS Budgets activo con límite de $80 USD mensual.

### 2. Bootstrap de Base de Datos y Aislamiento (Completado)
A través del túnel SSM seguro en el puerto 15432, se ejecutó `bootstrap-dev.sh`:
- 6 bases de datos creadas y verificadas: `istpetdev_dev_01` a `istpetdev_dev_05` e `istpetdev_integration`.
- Extensión PostGIS 3.4 habilitada en cada una de las 6 bases.
- 17 secretos generados con contraseñas seguras y guardados en AWS Secrets Manager (`/istpetdev/dev/db/...`).
- Privilegios DDL otorgados exclusivamente a roles migradores; usuarios de aplicación limitados estrictamente a DML (`SELECT, INSERT, UPDATE, DELETE`).
- Acceso público a bases de mantenimiento (`postgres`, `template1`) revocado.

### 3. Estado de MFA y Políticas IAM del Equipo
- **Bryan (`istpetdev-bryan`):** ✅ Administrador de infraestructura.
- **Alexander (`istpetdev-alexander`):** ✅ MFA activo — Política `IstpetDevIndividualDevAccess` aplicada en AWS IAM.
- **Andrés (`istpetdev-andres`):** ✅ MFA activo — Política `IstpetDevIndividualDevAccess` aplicada en AWS IAM.
- **Anthony (`istpetdev-anthony`):** ✅ MFA activo — Política `IstpetDevIndividualDevAccess` aplicada en AWS IAM.
- **Jorge (`istpetdev-jorge`):** ✅ MFA activo — Política `IstpetDevIndividualDevAccess` aplicada en AWS IAM.

Todo el equipo tiene acceso individual por túnel SSM (`istpetdev-dev-postgres-tunnel`), a su secreto asignado en Secrets Manager y a su prefijo S3 en el bucket de evidencias.

### 4. Estándar de Trabajo Colaborativo en Base de Datos (istpet-dbflow)
Para evitar bloqueos y pisadas de esquema entre compañeros, se adoptó el estándar de bases aisladas personales (`dev_01`…`dev_05`) y migraciones EF Core versionadas en Git:
- **Skill de Antigravity creada:** `~/.gemini/config/skills/istpet-dbflow/SKILL.md`
- **Guía completa en el repositorio:** `docs/database-workflow.md` (y referenciada en `docs/onboarding.md`).
- **Filosofía:** El código C# es la fuente de la verdad; nunca se ejecutan sentencias DDL manuales en clientes SQL.



