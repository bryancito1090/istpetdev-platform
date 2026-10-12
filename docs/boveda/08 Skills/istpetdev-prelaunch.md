---
tipo: skill
estado: vigente
actualizado: 2026-10-09
tags: [istpetdev, skills, seguridad, lanzamiento]
---

# IstpetDev — verificación previa al lanzamiento

**Cuándo usarla:** Antes de levantar una versión de IstpetDev para pruebas, declararla lista, ejecutar un ensayo expuesto o preparar su despliegue. Verificar los controles complementarios de seguridad con evidencia del código y runtime, especialmente RLS y configuración/plantillas administrables.

> Documentación de la skill `istpetdev-prelaunch`. El archivo local SKILL.md se genera con `name: istpetdev-prelaunch` y la descripción anterior, siguiendo el catálogo. La bóveda mantiene la fuente; no una segunda copia editable.

## Contexto mínimo

Localizar la bóveda y el repositorio de software según README y `03 Arquitectura/Repositorio de software y versiones.md`. Rutas siguientes relativas a la bóveda:

- `03 Arquitectura/Verificacion de seguridad antes del lanzamiento.md`: controles complementarios, pruebas y criterio de salida.
- `04 Datos y algoritmos/Esquema completo de base de datos.md`: catálogo de tablas y contrato RLS; leer las secciones relevantes al cambio.
- `03 Arquitectura/RBAC y configuracion del sistema.md`: roles administrables, campos y versiones de plantillas, cuando ese flujo esté afectado.
- `07 Demo/Plan de validacion.md`: V-34 a V-39 y casos ya existentes afectados.

## Reutilizar lo ya cubierto

Obtener la evidencia vigente de `08 Skills/istpetdev-revision.md`, `08 Skills/istpetdev-backend.md`, `08 Skills/istpetdev-frontend.md` y `08 Skills/istpetdev-dbflow.md` para la misma revisión/entorno. Usar sus reglas originales; no volver a copiar sus checklists aquí. Si falta evidencia o cambió el código correspondiente, ejecutar la revisión delegada leyendo su nota. Controles de identidad y credenciales siguen sus dueños existentes.

## Verificación complementaria

1. Identificar alcance real: entorno local aislado, demo expuesta o release; SHA/migraciones/configuración de proxy y lockfiles efectivos. Verificar solo los caminos implementados/expuestos, registrando lo pendiente; no inventar resultados ni pedir secretos en el prompt.
2. **RLS:** comparar catálogo de tablas tenant con DB real; comprobar políticas, ENABLE/FORCE y usuario API. Probar dos organizaciones y contexto vacío con conexión reutilizada, incluyendo escritura y tablas hijas. Lectura correcta con el migrador no acredita aislamiento; contexto ausente debe denegar. Usar la definición y límites de la nota del esquema.
3. **Campos y plantillas:** intentar cambiar campos de servidor/privilegios mediante DTO, configuración y formulario; revisar proyección de campos restringidos. Probar contenido ejecutable/expresiones no admitidas, publicación y captura con versión antigua. Administración configurable no debe habilitar autoescalación.
4. **Datos clasificados:** verificar cifrado por campo donde la clasificación lo exige, permisos de descifrado y manejo de versión de clave. Datos sintéticos para la prueba; cifrado de volumen no acredita protección en lectura SQL/export.
5. **Caminos especiales:** revisar parametrización del SQL manual/filtros dinámicos; confirmación de contenido de archivos; límites por identidad/organización y abuso del QR; cabeceras de la respuesta final del navegador. Condicionar la prueba al componente existente y justificar no aplica; parámetros del cliente no identifican un origen de proxy confiable.
6. **Dependencias:** consultar reporte .NET/npm y de imagen desplegada cuando exista. Buscar comandos/herramientas realmente disponibles; no afirmar que restore/build son scans. Evaluar exposición de hallazgos y corregir sin upgrades incompatibles rutinarios.

Los detalles y fuentes de estos controles viven en `03 Arquitectura/Verificacion de seguridad antes del lanzamiento.md`, no duplicarlos en referencias o scripts de esta skill.

## Resultado y actuación

Entregar un registro breve por control: estado verificado/fallo/pendiente/no aplica, revisión/entorno, prueba o evidencia, riesgo práctico y corrección necesaria. Reusar evidencia válida; repetir lo afectado por cambios, con runtime real para los controles que lo requieren. Si herramientas/red/DB están bloqueadas, marcar pendiente y describir exactamente qué no se comprobó.

Corregir lo local autorizado y repetir la prueba afectada. No cambiar grants/RLS de una base compartida, crear recursos cloud, rotar credenciales ni reescribir historial Git como efecto implícito de la revisión: esos cambios necesitan pertenecer a la tarea autorizada. No ampliar el trabajo a servicios ajenos.

Fallo o pendiente explotable de aislamiento, acceso/campos, ejecución de contenido o dependencia crítica/alta accesible impide declarar listo un release/demo expuesto. Un entorno local aislado puede levantarse para implementar las pruebas faltantes, con alcance explícito y sin presentarlo como lanzamiento seguro. Registrar el resultado antes de ejecutar los comandos de arranque/despliegue previstos en la tarea; la revisión no autoriza publicar por sí misma.

## Origen y mantenimiento

Creada por solicitud directa de Bryan el 9 de octubre de 2026. Completa los huecos de las skills actuales; el criterio de seguridad y sus fuentes se mantienen en la nota referenciada. Revalidar activación del asistente y referencias locales según `08 Skills/Catalogo y plan de skills.md`. La skill organiza verificación; B-40 añade los checks/gates reales del software.
