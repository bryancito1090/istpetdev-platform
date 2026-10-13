---
tipo: registro-ejecucion
estado: pendiente
actualizado: 2026-10-12
tags: [equipo, seguimiento, pruebas, hallazgos]
---

# Registro de seguimiento y hallazgos

Tablero editable para ejecutar [[Seguimiento de implementacion y pruebas]] desde el 13-oct. **La campaña integrada todavía no se ha ejecutado.** La evidencia local anterior de 001 se conserva como referencia; cada responsable actualiza aquí resultados reales. Los criterios completos pertenecen a los specs y [[Plan de validacion]].

## Cabecera de la campaña

| Campo | Valor a completar |
|---|---|
| Fecha/hora y zona | PENDIENTE; America/Guayaquil |
| Commit común y PR/checks | PENDIENTE |
| Imagen/digest, si aplica | PENDIENTE |
| Coordinación / revisor de salida | PENDIENTE / PENDIENTE |
| Entorno y alcance | PENDIENTE: local aislado / ensayo autorizado expuesto |
| Base operativa / simulación | PENDIENTE: nombres, sin cadenas de conexión |
| Fixture vivo / dataset simulado / matriz | PENDIENTE: versiones y hashes |
| Seeds / políticas / parámetros | PENDIENTE |
| Teléfono / navegador / URL sin token | PENDIENTE |
| Máquina dedicada del simulador | PENDIENTE |
| Revisión prelaunch | PENDIENTE: enlace, commit y límites |
| Racha de ensayos / material | PENDIENTE: contador y versión |

## Entrega de módulos

Estados operativos para este tablero: pendiente, entregado, integrado, verificado localmente, verificado en entorno de ensayo, bloqueado. El estado formal de cada spec se actualiza en su documento; «cerrado» requiere evidencia de sus criterios.

| Spec | Responsable / revisor | Estado de esta campaña | Commit/PR | Comando y evidencia | Pendientes |
|---|---|---|---|---|---|
| 001 | PENDIENTE / PENDIENTE | Pendiente de revalidación integrada | PENDIENTE | Referencia previa: [verification.md](../../specs/001-nucleo-delgado-importador/verification.md), 115 pruebas locales | Simulacro humano, identidad/entorno reales y cierre documentado |
| 002 | PENDIENTE / PENDIENTE | Pendiente | PENDIENTE | PENDIENTE | Entrega y E2E |
| 003 | PENDIENTE / PENDIENTE | Pendiente | PENDIENTE | PENDIENTE | Workflows, M2M y aceptación real |
| 004 | PENDIENTE / PENDIENTE | Pendiente | PENDIENTE | PENDIENTE | Entrega, teléfono/HTTPS y E2E |
| 005 | PENDIENTE / PENDIENTE | Pendiente | PENDIENTE | PENDIENTE | Panel, comparación y exportación |
| 006 | PENDIENTE / PENDIENTE | Pendiente | PENDIENTE | PENDIENTE | Preparación/ensayos; entrevistas y entrega en evento |

## Registro de pruebas

Una fila por ejecución o conjunto automatizado identificable. Usar referencias calificadas, como «004 E2E-03», para no mezclar IDs de diferentes specs. Resultados: aprobado, fallo, pendiente de entorno, no aplica con justificación.

| Fecha | Caso / requisito | Commit y entorno | Ejecutor / revisor | Esperado → observado | Resultado | Evidencia / hallazgo |
|---|---|---|---|---|---|---|
| PENDIENTE | PENDIENTE: primera ejecución | PENDIENTE | PENDIENTE | PENDIENTE | pendiente | PENDIENTE |

Adjuntar resúmenes con [[Plantilla evidencia]]: datos iniciales, pasos, valores/unidades, IDs de operación y archivos anonimizados. Guardar TRX/logs bajo `artifacts/seguimiento/<sha>/`; los registros compartidos enlazan evidencia accesible sin tokens, firmas, fotos privadas ni secretos. Conservar los archivos privados en almacenamiento autorizado.

## Hallazgos a confirmar al inicio

Estas filas provienen de [[Analisis de cobertura de specs]] y deben contrastarse con lo que entreguen los compañeros. **Son pendientes de revisión documental, no fallos runtime observados.** Cerrar los que ya tengan contrato/implementación/evidencia actualizados y crear hallazgos nuevos solo después de observarlos.

| Referencia local | Pendiente a contrastar | Dueño del tema | Responsable | Estado / resolución |
|---|---|---|---|---|
| SEG-01 | Estados y alcance de ADR-21; conservar RNF-14/B-40 y distinguir roles fijos de administración configurable | Coordinación / 001 | PENDIENTE | Por confirmar |
| SEG-02 | Transición preservando migraciones frente a instrucciones anteriores de recreación de bases | 001 / infraestructura | PENDIENTE | Por confirmar |
| SEG-03 | Interfaz de Bodega para preparación y despacho de la escena | 004 | PENDIENTE | Por confirmar |
| SEG-04 | Contrato e interfaz de devolución física ya exigida por 004 | 004 | PENDIENTE | Por confirmar |
| SEG-05 | Vía de upload de evidencias, límites QR y lectura de cuenta/rol para guards | 004 / infraestructura | PENDIENTE | Por confirmar |
| SEG-06 | Cliente M2M, credenciales autorizadas y versión de n8n local | 003 / infraestructura | PENDIENTE | Por confirmar |
| SEG-07 | Obligaciones comunes, matriz, máquina dedicada y benchmark completo | 005 / datos | PENDIENTE | Por confirmar |
| SEG-08 | Tratamiento explícito de RF-15/V-19: alcance actual o entrega 007 | Producto / coordinación | PENDIENTE | Por confirmar |
| SEG-09 | Evidencia de navegador/PWA y filtros CI de módulos/workflows, además de build web | 003/004/005 / CI | PENDIENTE | Por confirmar |

## Registro de defectos y necesidades

Severidad:

- **Bloqueador:** pérdida/duplicado de inventario o captura, acceso indebido, flujo esencial roto, simulación inválida o evidencia inexistente que impide el pase.
- **Mayor:** un caso obligatorio falla con alternativa limitada; corregir antes del cierre de ese requisito o registrar recorte explícito con su efecto.
- **Menor:** problema de presentación/usabilidad que permite completar el caso; priorizar según uso y dejar responsable/plazo.

| ID local | Tipo / severidad | Caso y spec dueño | Esperado / observado | Commit/entorno y pasos | Responsable / siguiente revisión | Estado | Evidencia / corrección / retest |
|---|---|---|---|---|---|---|---|
| PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE | PENDIENTE | Sin hallazgos observados en esta campaña aún | PENDIENTE |

Tipos: defecto, contrato incompleto, entorno/acceso, falta de evidencia, mejora futura. Estados: abierto, en corrección, pendiente de revalidación, cerrado con evidencia, diferido por decisión enlazada. Los IDs locales SEG/HAL organizan esta campaña; no sustituyen RF/RNF/V/B ni se incorporan a nombres de código.

## Registro de decisión de salida

| Puerta | Estado inicial | Evidencia / pendientes | Responsable y fecha |
|---|---|---|---|
| Versión integrada y contratos | Pendiente | PENDIENTE | PENDIENTE |
| Suite automática y CI correspondiente | Pendiente | PENDIENTE | PENDIENTE |
| Recorrido sin agente y teléfono real | Pendiente | PENDIENTE | PENDIENTE |
| n8n, incidentes, fallback y aceptación | Pendiente | PENDIENTE | PENDIENTE |
| Fallos, roles fijos/asignación y prelaunch | Pendiente | PENDIENTE | PENDIENTE |
| Simulación completa, invariantes y exportación | Pendiente | PENDIENTE | PENDIENTE |
| Preparación del 006 y ensayos | Pendiente | PENDIENTE | PENDIENTE |
| **Pase técnico del núcleo** | **Pendiente** | Bloqueadores abiertos: PENDIENTE de revisión | PENDIENTE |
| **Cierre final del 006/hackathon** | **Pendiente de criterios y evento** | Entrevistas, validación y entrega 16–17 oct | PENDIENTE |

Con cada salida guardar decisión, evidencia y límites: «continuar correcciones», «verificado localmente», «demo con limitaciones» o «pase técnico aprobado». Un pendiente de entorno no se convierte en aprobado por el paso del tiempo.

## Cierre de jornada y siguiente entrega

Copiar este bloque para cada día:

- Fecha y versión repetible:
- Módulos integrados y casos aprobados:
- Hallazgos abiertos y responsables:
- Pendientes de entorno y fecha/evento:
- Cambios de contrato/alcance registrados:
- Próxima prueba:
- Primera entrega de continuidad y criterio de entrada:
- Responsable de la siguiente revisión:

El orden de continuidad está en [[Continuidad del piloto y specs 007 a 012]]. Las pruebas nuevas y decisiones se enlazan desde los specs y [[Plan de validacion]] al ejecutarse; no mantener estados contradictorios en distintos índices.
