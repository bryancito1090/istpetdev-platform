# Guion del pitch v8 — IstpetDev

Diez diapositivas · meta de ensayo: **4:50 (290 s)** · Reto 1, Expo Clean 2026.

Conserva las nueve láminas de la v7 y añade la invitación al jurado. El cierre pasa de 20 a 15 s y la nueva lámina dispone de 20 s. Son tiempos propuestos, aún sin ensayo cronometrado. Se mantienen las cifras y los límites de evidencia de la [v7](../../boveda/07%20Demo/Guion%20del%20pitch.md): escenario sintético, costos estimados y comparación A/B/C ensayada técnicamente; no se atribuye a una empresa real.

| # | Lámina | Voz | s | Texto para ensayar |
|---|---|---|---:|---|
| 1 | Portada con la pregunta y el rótulo del reto | Joseph | 15 | «¿Cómo se entera hoy una empresa de limpieza de que a un hospital le falta desinfectante? Cuando el hospital llama. Ese día ya es tarde.» |
| 2 | **El caso**: cuatro puntos, tres maneras de atenderlos | Joseph | 40 | «Pongámonos en un día cualquiera, en nuestro escenario de Quito, que es sintético. Cuatro pedidos, con seis entregas críticas, entre hospitales y otros clientes, necesitan insumos de limpieza hoy. Hay tres maneras de atenderlos. A: un solo viaje con flota propia. B: también flota propia, pero los críticos primero, en el primer turno. C: contratar un operador externo. Hoy esa decisión se toma mirando kilómetros. Pero lo que cuesta de verdad es el personal, el combustible, la descarga y el producto que se daña. Y lo que importa: que lo crítico llegue a tiempo.» |
| 3 | **La alerta**: monitoreo de cobertura (`alertas.png`) | Alexander | 15 | «Todo empieza antes de que el cliente llame: el sistema revisa inventario y consumo, y avisa qué punto entra en riesgo y cuánto reponer.» |
| 4 | **La decisión**: A / B / C, solo tres datos por opción | Alexander | 55 | «Y esta es la decisión, con costo completo y no solo kilómetros. A es la más barata: 57,89 dólares. Pero solo llega a tiempo a 5 de las 6 entregas críticas. B y C llegan al 100 %. Entre ellas, C, el operador contratado, cuesta 66,90 contra 69,89 de B. El motor recomienda C, con 90,8 puntos, y la inteligencia artificial le explica al planificador por qué, en palabras simples. La más barata no fue la mejor. Y la persona decide.» |
| 5 | **La ruta y la prueba**: ruta, seguimiento, QR y novedades | Alexander | 30 | «Elegida la opción, el sistema arma la ruta con capacidad, horarios y jornada, y el planificador la aprueba. En el camino queda registro: cada caja viaja con QR y los daños o errores de rotulado se anotan con su motivo. Así, cuando alguien pregunta qué se entregó, hay respuesta.» |
| 6 | **Por qué es nuevo** | Bryan | 35 | «¿Qué cambia? Lo que escuchamos en el sector es que se repone por calendario o por urgencia, y la entrega se decide por kilómetros. Nosotros hacemos con la entrega lo que se hace con una buena cotización: ponerle el costo completo antes de decidir. Un motor de reglas calcula, siempre igual con los mismos datos. La inteligencia artificial explica, no inventa cifras, y si falla hay una explicación de respaldo. La alerta no se queda en alerta: termina en una decisión con costo y en una entrega comprobable.» |
| 7 | **Construido / Probado / Siguiente fase** | Anthony | 25 | «Con honestidad. Construido: de la alerta a la prueba de entrega. Probado: la decisión A, B y C en un ensayo técnico sobre un escenario sintético de Quito de 40 puntos, y una corrida del agente de inteligencia artificial, incluida su caída al respaldo. Siguiente fase: usarlo en una empresa real, cargar sus datos y medir contra su línea base, y llevar la comparación A, B y C a producción.» |
| 8 | **Impacto y modelo** | Jorge | 40 | «En un piloto queremos medir tres cosas: menos faltantes descubiertos tarde, más entregas críticas a tiempo y cuánto cuesta realmente cada entrega. No prometemos un ahorro: lo medimos contra una línea base. No necesita hardware nuevo: funciona en la web y en el teléfono del conductor. Proponemos implementación inicial más suscripción, que validaremos. Y si falta un dato, el sistema dice “sin dato”. Esos indicadores alimentan informes ejecutivos: ya tenemos la sección Reportes.» |
| 9 | **Cierre** | Joseph | 15 | «Una buena cotización de limpieza suma el costo completo. Nosotros hacemos lo mismo con la entrega: la decidimos con su costo real, antes de hacerla, y dejamos la prueba. Buscamos una empresa de limpieza para un piloto: con su línea base, indicadores acordados y sus datos. Gracias.» |
| 10 | **Pruébenlo ustedes**: QR, cuenta Auditor y seguridad | Bryan | 20 | «Escaneen el QR y entren con la cuenta del jurado. Amazon Cognito, el servicio de acceso de Amazon en la nube, valida su identidad. Ustedes pueden consultar como auditores, sin modificar la operación. La conexión va cifrada. Dejamos esta pantalla para sus preguntas.» |

**Duración prevista:** 15 + 40 + 15 + 55 + 30 + 35 + 25 + 40 + 15 + 20 = **290 s**. Quedan 10 s de margen respecto a cinco minutos. La lámina 10 aparece sin animación y se mantiene visible durante las preguntas; no leer la contraseña en voz alta ni enumerar las diez líneas salvo que el jurado pregunte.

## Seguridad en diez líneas

1. Amazon Cognito es el servicio de acceso de AWS.
2. AWS es la nube de Amazon: funciona por Internet.
3. Cognito comprueba el usuario y la contraseña.
4. La contraseña la gestiona Cognito.
5. La aplicación recibe una sesión temporal.
6. Cada cuenta tiene permisos según su función.
7. El jurado entra con el rol Auditor.
8. Puede consultar; no modificar la operación.
9. La conexión usa HTTPS para cifrar los datos.
10. La cuenta puede desactivarse al terminar.

Fuente para Cognito: [documentación oficial de AWS](https://docs.aws.amazon.com/cognito/latest/developerguide/cognito-how-to-authenticate.html). Los permisos Auditor los comprueba la API de IstpetDev por separado. No afirmar MFA habilitado: el pool consultado el 17-oct tiene MFA desactivado.

## Materiales y acceso

- Generador: `materiales/generar_v8.py`; reutiliza v7 y lee la credencial desde el archivo privado local, sin incluirla en el código.
- Copia de revisión versionable: `materiales/pitch-v8-revision.pptx` y `materiales/pitch-v8-revision.pdf`, con el campo de contraseña sustituido por «Entregada al jurado». Se regenera con `python materiales/generar_v8.py --sin-credenciales`.
- Presentación: `materiales/pitch-v8.pptx` (GIF y animaciones) y `materiales/pitch-v8.pdf` (imágenes fijas); vistas en `materiales/render-v8/`.
- Guion: este Markdown, `materiales/guion-pitch-v8.docx` y `materiales/guion-pitch-v8.pdf`; los tres omiten la contraseña.
- QR: `https://istpetdev.doicela.dev/`. Solo contiene el dominio, sin contraseña, token ni acceso automático.
- Usuario: `jurado@istpetdev.doicela.dev`, nombre «Jurado Expo Clean 2026», rol `Auditor`.
- La contraseña está en la última diapositiva y en `~/.config/istpetdev/private/jurado-pitch-v8.json` (0600). Los archivos que la contienen se excluyen localmente de Git. No se sustituye la v7 versionada en `docs/boveda/Entregable/` por una copia con credenciales.
- Revocación: al terminar la evaluación, Gerencia puede desactivar la cuenta desde Usuarios. La cuenta no caduca automáticamente; queda pendiente que el responsable la desactive tras el evento.
- Registro del acceso productivo y sus límites: `evidencia/pitch-v8-acceso-jurado.md`. Issue #371.
