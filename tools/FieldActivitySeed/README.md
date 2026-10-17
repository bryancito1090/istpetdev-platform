# Novedades de campo: muestra sintética

Herramienta operativa explícita para desarrollo y producción autorizada. No migra,
reinicia ni sustituye el dataset existente. Identifica el lote con `SYN-NOV-V1-`
y los actores con `[Demo sintética]`; no crea credenciales de acceso.

```sh
dotnet build tools/FieldActivitySeed/FieldActivitySeed.csproj
# Obtener FIELD_SEED_CONNECTION y FIELD_SEED_DATABASE del gestor privado del entorno.
dotnet tools/FieldActivitySeed/bin/Debug/net8.0/FieldActivitySeed.dll
# Tras revisar el ensayo y verificar un respaldo:
dotnet tools/FieldActivitySeed/bin/Debug/net8.0/FieldActivitySeed.dll --apply
```

Sin `--apply` ejecuta publicación, FEFO, despacho, custodia, recepción y reposición
dentro de una transacción que revierte completamente. La aplicación confirma una
sola transacción; repetir el lote devuelve `AlreadyApplied` sin duplicarlo.
Solo admite `istpetdev_dev_01` y `istpetdev_integration`, con identidad explícita;
producción exige TLS `VerifyFull`. Aplicar allí solo después del merge a develop.

La selección usa los puntos con menor cobertura y el consumo ponderado de los
últimos 14 días disponibles. Entrega hasta dos días de consumo, conserva un 10%
de seguridad y limita la muestra al 10% del saldo libre posterior a ese margen.
Respeta las reservas existentes, capacidades, ventanas y evaluación de rutas.
Los pedidos solicitan la diferencia hasta diez días de cobertura tras recibir.
Se incluyen pocos rechazos de una unidad y diferencias pequeñas de percha;
las tasas resultantes describen únicamente esta muestra, no una tasa real del sector.

Los registros recorren los servicios de dominio existentes: sus movimientos,
saldos, lotes y pedidos son coherentes con las reglas de la aplicación. Los
metadatos de catálogo no se consultan ni se modifican; permite operar antes o
después de su migración aditiva, sin cambiar el esquema.

Pruebas: `dotnet test tests/IstpetDev.Tests --filter FullyQualifiedName~FieldActivitySeedTests`.
