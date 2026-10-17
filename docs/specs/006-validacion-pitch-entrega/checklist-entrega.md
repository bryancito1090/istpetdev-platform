# Checklist de entrega — Reto 1

**Ventana oficial:** sábado 16:00–16:15. **Congelamiento interno:** 15:45. Canal comunicado: carpeta de Drive del capitán; O-06 sigue sin URL y formatos confirmados. No marcar recepción como realizada hasta recibir acuse.

## Secuencia minuto a minuto

| Hora | Acción | Responsable propuesto | Hecho |
|---|---|---|---|
| 15:30 | Abrir la carpeta de Drive del capitán y confirmar acceso; descargar una copia local de todos los entregables candidatos. | Jorge | [ ] |
| 15:31 | Confirmar con organización/capitán la ruta final de carpeta y los formatos admitidos según O-06. | Joseph | [ ] |
| 15:32 | Confirmar que están las versiones editables y exportadas a PDF; apartar borradores y renders internos. | Joseph | [ ] |
| 15:33 | Comprobar nombres, apertura de PPTX/DOCX/PDF y que no haya errores de lectura. | Alexander | [ ] |
| 15:34 | Revisar el contenido de cada entregable contra la matriz de afirmaciones. | Bryan + revisor distinto | [ ] |
| 15:35 | Confirmar el estado de la captura A/B/C; no reemplazar el marcador con mockups o cifras sin fuente. | Alexander | [ ] |
| 15:36 | Copiar archivos finales al directorio de entrega; excluir fuentes y archivos internos si no los solicitan. | Jorge | [ ] |
| 15:37 | Ejecutar `sha256sum -c` sobre la lista congelada del manifiesto y conservar salida. | Bryan | [ ] |
| 15:38 | Comparar todos los hashes con el manifiesto; si un archivo cambia, recalcular y actualizar el manifiesto antes de copiar. | Bryan + Joseph | [ ] |
| 15:39 | Revisar nombres y pesos de los archivos en Drive, sin asumir límites no confirmados. | Jorge | [ ] |
| 15:40 | Probar que cada enlace/archivo de Drive abre desde una sesión distinta o dispositivo del equipo. | Alexander | [ ] |
| 15:41 | Revisar que los PDF corresponden a los editables congelados y que se leen correctamente. | Joseph | [ ] |
| 15:42 | Preparar USB con el mismo paquete y lista de hashes; probar apertura desde el USB. | Jorge | [ ] |
| 15:43 | Confirmar conectividad del hotspot y disponer el correo del capitán para contingencia. | Anthony | [ ] |
| 15:44 | Revisión final de lista oficial, responsables y versiones; registrar el commit y hora del corte. | Joseph + Bryan | [ ] |
| 15:45 | Congelar materiales: no modificar archivos entregables; guardar copia inmutable local. | Todo el equipo | [ ] |
| 15:46 | Abrir los archivos desde la carpeta final y verificar nuevamente los SHA-256 cargados. | Bryan + Jorge | [ ] |
| 15:47 | Confirmar con el capitán que el enlace de Drive está accesible para la organización. | Jorge | [ ] |
| 15:48 | Confirmar que quien entregará conoce la secuencia de envío y el plan B. | Joseph | [ ] |
| 15:49 | Verificar espacio disponible y batería/conexión del dispositivo de entrega. | Anthony | [ ] |
| 15:50 | Dejar el paquete listo, sin volver a generar documentos. | Todo el equipo | [ ] |
| 15:51 | Revisar mensajes o indicaciones de la organización sobre canal y recepción. | Joseph | [ ] |
| 15:52 | Si hay cambio de canal o formato, anotarlo y aplicar únicamente la instrucción confirmada. | Joseph + Jorge | [ ] |
| 15:53 | Confirmar hashes del USB y del paquete local frente al manifiesto. | Bryan | [ ] |
| 15:54 | Confirmar que el video de respaldo está disponible solo si fue grabado y verificado; no presentarlo como existente si falta. | Anthony | [ ] |
| 15:55 | Preparar hoja de acuse para anotar hora, receptor y elementos recibidos. | Joseph | [ ] |
| 15:56 | Confirmar presencia del capitán y de la persona designada para entrega. | Jorge | [ ] |
| 15:57 | Mantener Drive abierto y observar estado de carga/sincronización. | Jorge | [ ] |
| 15:58 | Verificar que no haya archivos pendientes de sincronización. | Jorge | [ ] |
| 15:59 | Abrir checklist y manifiesto junto al equipo para cotejo final. | Bryan | [ ] |
| 16:00 | Iniciar entrega por el canal confirmado; compartir carpeta o cargar archivos según instrucción. | Capitán + Jorge | [ ] |
| 16:01 | Confirmar que la organización puede ver los archivos. | Jorge | [ ] |
| 16:02 | Cotejar nombres de archivos recibidos con el manifiesto. | Bryan | [ ] |
| 16:03 | Verificar PDFs/editables en destino si el canal permite abrirlos. | Alexander | [ ] |
| 16:04 | Entregar el enlace o ubicación a Mónica Sevilla y William Chamorro, según instrucción de recepción. | Capitán | [ ] |
| 16:05 | Pedir confirmación de acceso y recepción; anotar literalmente cualquier incidencia. | Joseph | [ ] |
| 16:06 | Si no hay acceso a Drive, activar hotspot y reintentar una vez desde otro dispositivo. | Anthony + Jorge | [ ] |
| 16:07 | Si persiste la falla, ofrecer USB con el mismo paquete y hashes. | Jorge | [ ] |
| 16:08 | Si USB no es aceptado o no es posible, enviar por correo del capitán solo con autorización de recepción. | Capitán | [ ] |
| 16:09 | Confirmar por voz qué archivos/canal fueron recibidos y registrar nombre del receptor. | Joseph | [ ] |
| 16:10 | Cotejar acuse o registrar que aún no se recibió. | Joseph | [ ] |
| 16:11 | Resolver una incidencia de acceso sin regenerar ni editar contenido congelado. | Jorge + Bryan | [ ] |
| 16:12 | Confirmar que el canal alterno, si se usó, contiene el mismo conjunto de archivos. | Bryan | [ ] |
| 16:13 | Guardar evidencia del acuse (mensaje/correo) en la carpeta interna del equipo, sin datos sensibles innecesarios. | Joseph | [ ] |
| 16:14 | Cerrar la entrega y comunicar al equipo el estado factual. | Joseph | [ ] |
| 16:15 | Fin de ventana: registrar resultado final y cualquier archivo pendiente o rechazado. | Joseph | [ ] |

## Verificación de hashes

Antes del congelamiento, actualizar el manifiesto con hashes de los archivos finales. Copiar sus valores a una lista de comprobación, ejecutar `sha256sum -c` y revisar que cada resultado sea `OK`. Volver a calcular tras copiar/exportar si el archivo cambió; nunca editar después de congelar sin repetir el cotejo y registrar la nueva versión.

## Plan B si falla Drive o la red

1. **Drive inaccesible:** intentar desde otro dispositivo con el hotspot del equipo y confirmar acceso con el capitán.
2. **Red del recinto caída:** usar hotspot/datos móviles. Si no hay conectividad, entregar el USB previamente probado, con editables, PDF y manifiesto/hash list.
3. **USB no aceptado o falla del puerto:** ofrecer envío desde el correo del capitán al correo indicado por la organización, solicitando confirmación expresa del canal. No asumir que el correo sustituye el canal oficial.
4. Mantener siempre el mismo conjunto congelado y cotejar hashes en el medio alterno. Anotar la incidencia y el canal realmente aceptado.

## Registro de acuse

| Hora | Quién recibió (nombre/rol) | Qué se entregó (archivos, formatos y hashes/versión) | Canal | Acuse / observación literal |
|---|---|---|---|---|
| — | — | — | — | — |
