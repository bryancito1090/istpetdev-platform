# Revisión manual T5 — corrida guardada

**Estado:** implementación terminada; verificación técnica en curso; revisión manual del usuario pendiente.

## Qué cambia y cómo funciona

La comparación siempre inicia en **En vivo**. El botón **Corrida guardada** hace una petición explícita a `/corrida-abc.demo-quito.json`; un fallo deja el modo en guardado y presenta un mensaje para que el operador decida volver a vivo. No hay cambio automático de origen.

La respuesta guardada debe incluir el fixture `demo-quito-planificacion`, commit, fecha UTC, SHA-256 de configuración, alternativas A/B/C con modalidad contratada y precio de combustible con valor, fecha y fuente. La misma función `mapSourcingComparison` transforma respuestas en vivo y guardadas. Las operaciones de publicar, reordenar, mostrar mapa y confirmar una ruta se bloquean durante la vista guardada.

El generador de T4 escribe la captura original en `apps/web/src/pages/rutas/corrida-abc.demo-quito.json` y también copia el mismo contenido a `apps/web/public/corrida-abc.demo-quito.json`, carpeta que Angular incluye como asset estático. Ambos archivos se crearán únicamente cuando una corrida real termine correctamente; todavía no se generó ninguno.

## Archivos de esta etapa

- `apps/web/src/pages/rutas/rutas.page.ts`: selector manual, validación/carga, rótulo y guardas de solo lectura.
- `apps/web/src/shared/api/api.service.ts`: DTO y GET del archivo estático.
- `apps/web/src/pages/rutas/route-comparison.model.ts`: valida metadatos y usa el mapeo común.
- `scripts/demo/generar-corrida-abc.mjs`: copia la corrida real al directorio público.
- Pruebas de página y contrato; plan actualizado y esta guía.

## Comprobación manual

1. Iniciar API y web con el procedimiento habitual del repositorio y abrir **Rutas → Asistente de planificación → Comparación A/B/C**.
2. Confirmar que **En vivo** está activo al entrar. Si `/suggest` devuelve opciones, las cifras deben coincidir con su respuesta; si devuelve una lista vacía, aparece «Sin opciones calculadas para esta selección» con el motivo.
3. En DevTools → Network, bloquear la petición `route-plans/suggest` y recalcular. Debe aparecer el error de API y no debe cambiarse a corrida guardada ni aparecer otra tabla con cifras.
4. Pulsar **Corrida guardada**. Como aún no se generó una captura real en esta rama, el resultado esperado hoy es «No hay una corrida guardada disponible…». El modo no cambia por sí solo.
5. Cuando haya JSON real, volver a pulsar **Corrida guardada**: la tabla debe mostrar `Corrida guardada · demo-quito-planificacion · commit … · fecha UTC`, y las celdas de combustible deben llevar valor, fecha y fuente del JSON.
6. Con la corrida visible, probar seleccionar una columna, publicar, elegir otra, ver mapa y avanzar a confirmar. Ninguna acción debe crear, publicar ni cargar una ruta; la selección de columna solo cambia el resaltado.
7. Pulsar **En vivo**. La corrida desaparece; regresar al paso de selección y volver a calcular debe mostrar una respuesta nueva del motor.
8. Activar modo proyector y confirmar que el rótulo del origen sigue dentro de la tabla.

**Resultado del usuario:** pendiente. No se registra aprobación visual hasta que se complete el recorrido.

## Verificación técnica

`npm run test --workspace=apps/web` pasó: 27 archivos y 160 pruebas. `npm run build:web` pasó, incluidos ESLint y compilación de producción; queda el aviso preexistente de Leaflet CommonJS y la regla `boundaries/entry-point` deprecada. `node --check scripts/demo/generar-corrida-abc.mjs` pasó. La validación offline del generador encontró los cuatro pedidos y siete líneas del fixture, sin llamar a la API ni modificar la base. La búsqueda del repositorio no encontró los nombres retirados de las constantes ni del estado de maqueta. `git diff --check` pasa después de retirar una línea vacía final.

No se considera corrida real ni evidencia para la lámina 4 mientras el generador no produzca datos con la configuración acordada en #275 y la selección aprobada. La prueba de integración backend del reset sigue limitada por la falta de Docker documentada en T4.
