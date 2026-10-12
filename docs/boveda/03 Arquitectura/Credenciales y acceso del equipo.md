---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-07
tags: [arquitectura, seguridad, credenciales, aws, equipo]
---

# Credenciales y acceso del equipo

> **Escenario temporal actualizado:** para la hackathon, [[AWS temporal para la hackathon y cierre]] propone usuarios IAM individuales con MFA y `aws login`, sin Organizations, para conservar créditos. Tiene precedencia sobre las referencias humanas SSO/permission sets de esta nota. Se mantienen Secrets Manager, aislamiento por persona, credenciales temporales, OIDC de Actions y exclusión de secretos de ambos repositorios.

## Acuerdo y alcance

Bryan solicita que el equipo pueda usar AWS y las integraciones sin exponer credenciales en `istpetdev-hackathon-2026` ni en `istpetdev-platform`. Se adopta acceso individual a AWS y un almacén de secretos con permisos por entorno y persona. Aplica a ambos repositorios, aunque sean privados, y complementa [[Entornos y operacion acordados]], [[Identidad OIDC y sesiones]] y el aporte del compañero sin sustituirlo.

**Estado real:** política documentada y exclusiones locales de Git ampliadas. El alta de usuarios, permission sets, roles, secretos y workflows sigue pendiente de implementación. Esta nota no concede acceso ni demuestra que AWS esté configurado.

## 1. Acceso individual a AWS

Cada integrante tendrá su usuario de **IAM Identity Center**, MFA y permisos de desarrollo. Obtendrá credenciales temporales con SSO; no compartiremos las access keys, el usuario root, la contraseña ni la sesión de Bryan. El perfil local `istpetdev-dev` usa la identidad de quien inicia sesión en esa máquina. La API local utilizará ese perfil mediante el proveedor de credenciales del SDK, con soporte SSO verificado al implementar. [Referencia IAM de AWS](https://docs.aws.amazon.com/IAM/latest/UserGuide/best-practices.html).

Antes de configurar SSO, verificar una **instancia de organización** de Identity Center con acceso a cuentas AWS y permission sets. Una instancia de cuenta orientada a aplicaciones no cubre este acceso; si la cuenta está independiente, revisar la habilitación con AWS Organizations. No reorganizar otras cuentas de Bryan como parte del desarrollo. [Tipos de instancia](https://docs.aws.amazon.com/singlesignon/latest/userguide/identity-center-instances.html).

El permiso común habilita SSM al nodo dev y las operaciones dev necesarias. La lectura de secretos personales se asigna mediante permission sets o roles diferenciados para `dev_01` a `dev_05`; un único rol compartido que lea todos los secretos personales no cumpliría el aislamiento acordado. Administración de infraestructura se mantiene en un rol separado.

Ser colaborador de GitHub, poder iniciar sesión en la aplicación con Cognito y poder administrar recursos AWS son tres accesos diferentes. Cognito/ADR-10 identifica usuarios de nuestra aplicación; Identity Center identifica al equipo frente a AWS.

## 2. Dónde guardar cada credencial

Los secretos de integraciones que necesite el backend se guardan en **AWS Secrets Manager**, separados entre `dev`, `demo` y `prod`. El propietario carga los valores; el equipo recibe permiso de lectura exclusivamente sobre los secretos dev necesarios. Preferir una clave de proveedor por integrante o integración, con límites de uso y caducidad cuando el proveedor lo permita. Si solo existe una clave dev compartida, autorizarla explícitamente y rotarla al retirar a alguien que pudo leerla.

| Recurso | Almacenamiento y uso | Acceso del equipo |
|---|---|---|
| Sesión AWS de una persona | SSO y caché local fuera del repo | Cada uno usa su propia sesión; no se distribuyen tokens AWS |
| PostgreSQL personal | `/istpetdev/dev/db/dev_01` … `dev_05`, con usuario/contraseña propios | Solo la persona asignada y el administrador; conexión por SSM/TLS |
| PostgreSQL de integración | Un secreto por usuario autorizado bajo `/istpetdev/dev/db/integration/<integrante>` | Acceso coordinado a integración; sin entregar el usuario master |
| Token de proveedor consumido por API | `/istpetdev/dev/integrations/<proveedor>/<integrante-o-servicio>` | Solo quienes desarrollen esa integración; cuotas y permisos limitados |
| Credenciales demo/prod | Secretos y roles del entorno correspondiente | Runtime y operadores autorizados; sin acceso dev por defecto |
| DeepSeek y cliente técnico de n8n | Almacén de credenciales del servidor n8n existente | Bryan carga los valores; el equipo prueba workflows sin recibir necesariamente la clave |
| Clave de cifrado y acceso administrativo n8n | Configuración/backup privado de Bryan en su servidor | No se distribuyen al equipo ni se exportan con workflows |
| Llave SSH del deploy demo / token técnico GHCR si hace falta | Secrets del workflow autorizado o Secrets Manager obtenido por OIDC | Solo el job que lo necesita; credencial limitada a esa finalidad |
| GitHub personal | Autenticación individual de Git/CLI, fuera del repo | Cada colaborador usa su propia cuenta; no se comparte el PAT de Bryan |

Los nombres son el contrato propuesto, no secretos creados. Los valores iniciales, ARN efectivos, persona asignada y fechas de revisión los registra el administrador en su control privado. Compartir el nombre/ARN de un secreto permite localizarlo; la autorización IAM sigue siendo necesaria para leerlo.

IAM concede `secretsmanager:GetSecretValue` sobre los ARN exactos asignados, sin escritura/borrado ni lectura general de producción. Cuando se use una clave KMS administrada por nosotros, añadir únicamente el `kms:Decrypt` correspondiente. Leer el propio secreto no concede administrar la DB, el bucket ni Terraform. [Control de acceso de Secrets Manager](https://docs.aws.amazon.com/secretsmanager/latest/userguide/auth-and-access.html).

Mantener un registro privado con propietario, servicio, entorno, consumidores, alcance, caducidad y última rotación; **sin copiar el valor**. Secrets Manager y otros recursos persistentes tienen costos aunque el stack completo esté apagado; incluirlos en el presupuesto operativo.

## 3. Incorporación y uso local

1. Bryan da acceso a los repositorios con la cuenta GitHub individual del integrante.
2. El administrador da de alta su identidad AWS con MFA y asigna su permission set dev, su base PostgreSQL y los secretos de integración necesarios.
3. Comparte las instrucciones del portal SSO, región, perfil, nodo SSM, endpoint/CA de RDS y nombres de secretos por el canal privado del equipo. No manda una contraseña común ni tokens por el chat, issues o documentos.
4. El integrante instala AWS CLI v2 y Session Manager plugin, configura su perfil e inicia sesión. La región SSO es la del directorio real; no asumir que siempre coincide con `us-east-1` del proyecto.

```bash
aws configure sso --profile istpetdev-dev
aws sso login --profile istpetdev-dev
```

El asistente solicita portal/región SSO, cuenta y permission set asignados. La configuración y caché quedan en el perfil del usuario, fuera de los repositorios. Al expirar la sesión se vuelve a ejecutar `aws sso login`. [Configuración oficial de CLI](https://docs.aws.amazon.com/cli/latest/userguide/cli-configure-sso.html).

5. Obtiene únicamente su secreto autorizado y lo carga en configuración local. Ejemplo **para cuando exista el secreto**, sin mostrar su valor ni pasarlo como argumento de línea de comandos:

```bash
umask 077
private_dir="$HOME/.config/istpetdev/private"
mkdir -p "$private_dir"
chmod 700 "$private_dir"
aws secretsmanager get-secret-value \
  --profile istpetdev-dev \
  --region us-east-1 \
  --secret-id '/istpetdev/dev/db/dev_01' \
  --query SecretString \
  --output text > "$private_dir/db-dev.json"
chmod 600 "$private_dir/db-dev.json"
```

Sustituir `dev_01` por la asignación individual. El contrato de ese secreto es JSON, con campos `host`, `port`, `dbname`, `username` y `password`. El archivo privado es entrada para el launcher/proveedor de configuración que se implementará; .NET **no lo carga automáticamente** por existir. Si falla la lectura, no iniciar la API con un archivo vacío. [Lectura por CLI](https://docs.aws.amazon.com/secretsmanager/latest/userguide/retrieving-secrets_cli.html).

El launcher adapta host/puerto al túnel y construye la conexión con validación TLS según [[Entornos y operacion acordados]]. Una connection string que incluya contraseña es un secreto completo. Se entrega por este acceso autorizado, no se publica como documentación del equipo.

Para la API .NET, usar **user-secrets en Development** o inyección explícita de variables por el launcher/Compose. User-secrets mantiene los valores fuera del árbol del proyecto, pero no los cifra; `.env` privado tampoco es un almacén cifrado. Proteger el equipo y los archivos. Inicializar user-secrets cuando exista `src/Api`, importar mediante entrada estándar/archivo privado y evitar pegar valores en comandos que queden en el historial. `.env.example` publica solo nombres y ejemplos sin credenciales. [Secretos de desarrollo en .NET](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0).

No imprimir `.env`, conexiones completas, respuestas de Secrets Manager, `dotnet user-secrets list` ni la caché SSO en logs, capturas o conversaciones con agentes. Eliminar la copia privada cuando deje de necesitarse; nunca moverla a la bóveda como respaldo.

## 4. GitHub Actions y despliegues

El workflow de despliegue obtiene acceso AWS mediante **OIDC y un rol temporal**, sin guardar `AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY` de Bryan en GitHub. Restringir audiencia, repositorio y environment/rama autorizados; los permisos concretos siguen el CI/CD del compañero. No dar ese rol a jobs de PR no confiables. [OIDC de GitHub para AWS](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws).

**Comprobación al implementar:** GitHub documenta un formato de subject con IDs inmutables para repositorios nuevos desde el 15 de julio de 2026 o que lo hayan activado. Obtener el formato/IDs reales de `istpetdev-platform` y construir la condición exacta; los ejemplos previos `repo:owner/repo:...` necesitan esta verificación. Si el job usa environment, el subject identifica ese environment y las ramas se restringen con sus reglas. Nunca resolver una discrepancia usando un wildcard de todo el repositorio.

Para integraciones, preferir que el job autorizado lea el secreto necesario de Secrets Manager con ese rol. GitHub Actions Secrets queda como opción para credenciales que el workflow necesite, por ejemplo SSH según el diseño vigente; se carga en Settings/CLI, **no en archivos versionados**. Usar environment secrets cuando el plan del repositorio privado lo soporte y, en su defecto, repository secrets con jobs/permisos restringidos. No duplicar todas las claves del equipo en GitHub. [Uso de secrets en Actions](https://docs.github.com/en/actions/how-tos/write-workflows/choose-what-workflows-do/use-secrets).

Actions Secrets sirve para inyectar valores a jobs; no es el mecanismo para que los integrantes recuperen claves y trabajen localmente. Quien pueda modificar un workflow que use un secreto puede intentar extraerlo: revisar esos cambios, limitar consumidores y no confiar en el enmascaramiento del log como control suficiente.

El host/runtime AWS usa su rol para recuperar sus secretos; un `.env` materializado queda fuera de Git, con modo `0600`. No incluir credenciales en imágenes, capas Docker, artefactos, manifest de release, backups sin protección ni state/planes Terraform accesibles al equipo general. Los permisos y estado real del workflow siguen pendientes del compañero responsable.

## 5. Angular, móvil y n8n

Angular/PWA solo recibe configuración pública: URL de API, issuer y client ID OIDC. Sus archivos `environment`, bundles y configuración servida al navegador son legibles por usuarios; una variable de entorno usada durante el build no oculta una clave. Las llamadas a proveedores con credenciales privadas pasan por backend o n8n. Si un proveedor exige una clave publicable de mapas en el navegador, verificar que sea publicable y restringir orígenes, API y cuotas según sus reglas.

El servidor **https://n8n.bryan-bano.com/** conserva las credenciales cargadas por Bryan. Exportar workflows sanitizados, con referencias de credenciales y sin tokens incrustados en nodos, headers, URLs ni datos fijados de ejemplo. La clave `N8N_ENCRYPTION_KEY` queda solo en configuración y backup privado del servidor. No compartir su cuenta administrativa para que el equipo pruebe integraciones; habilitar únicamente el acceso a workflows que soporte la edición instalada o probar mediante la API/webhook autenticados ya definidos.

## 6. Protección de Git y retirada de accesos

En ambos repositorios se excluyen `.env` privados, directorios de secretos, copias `.aws`, llaves privadas y material sensible de Terraform. Un `.env.example` puede versionarse solo con valores públicos/campos vacíos. `.gitignore` evita nuevas inclusiones ordinarias; **no elimina archivos ya rastreados, commits anteriores ni protege frente a `git add -f`**.

Antes de cada publicación revisar los archivos staged y las exportaciones de n8n. Añadir detección de secretos al CI que implementará el compañero y activar secret scanning/push protection cuando el plan del repo lo permita. Un repo privado también tiene colaboradores, clones e historial: no debe contener credenciales reales.

Al salir alguien del equipo: retirar acceso GitHub, asignaciones AWS y acceso n8n; revocar sesiones activas y privilegios PostgreSQL según corresponda, cerrar túneles, y rotar claves/passwords dev compartidos o que hubiera podido copiar. Quitar `GetSecretValue` no invalida una clave de proveedor ya conocida. La rotación PostgreSQL actualiza tanto contraseña efectiva como secreto/configuración; no basta editar el JSON de Secrets Manager.

Si aparece un secreto real en Git, logs o artefactos: primero revocarlo/rotarlo en su proveedor, después retirar las copias y tratar el historial coordinadamente. Borrar el archivo o hacer privado el repositorio no invalida el secreto. No reescribir el historial ni modificar recursos externos como parte de esta actualización documental.

## 7. Implementación pendiente y criterio de cierre

Registrar B-35 y V-32 en [[Backlog]] y [[Plan de validacion]]. El agente que prepare el workspace debe usar el perfil autorizado existente, crear/leer secretos sin imprimir valores y entregar nombres/ubicaciones privadas; no solicitar pegar tokens en el prompt ni publicar conexiones reales. No versionar archivos privados aunque el usuario solicite publicar el esqueleto en `develop`.

El cierre requiere: dos identidades AWS distintas; cada una lee su secreto dev y no el ajeno/prod; conexión PostgreSQL propia por SSM/TLS; API local sin claves AWS estáticas; un workflow OIDC con trust exacto probado cuando se implemente; rotación/revocación ensayadas sin fuga en logs ni artefactos. Registrar evidencia anonimizada, sin afirmar que estas pruebas ya se realizaron.
