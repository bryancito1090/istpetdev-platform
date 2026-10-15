---
tipo: demo
estado: vigente
actualizado: 2026-10-15
tags: [demo, validacion, pruebas, istpetdev]
---

# Flujos de prueba

Qué se puede probar **hoy (15-oct, tarde)** en `https://istpetdev.doicela.dev` (web) y `https://istpetdev.doicela.dev/campo/` (PWA), con qué cuenta y qué debe pasar. Los escenarios ESC-xx vienen del análisis de flujo E2E (#118); los E2E-xx, del spec 013. Se registra el resultado en #118 **sin correos ni contraseñas**.

## Antes de empezar

- **Producción = `main`.** Lo que solo está en `develop` no se ve en el teléfono hasta el PR `develop → main`. Hoy faltan en producción: escáner con cámara (#122), conciliación de stock en percha (#111) y aislamiento de capturas fallidas del outbox (#114).
- **Una cuenta por rol y un correo por cuenta.** Para tener dos roles se usan alias (`nombre+bodega@gmail.com`). Cuentas y asignación: #55.
- **Sin MFA** durante la hackathon (PR #98). La sesión sobrevive a F5 y a cerrar la PWA (D-013-S1).
- **Web:** Gerencia, Planificador y Auditor. **PWA:** Bodega, Conductor y Receptor. Un rol en la plataforma equivocada ve un mensaje y no entra.

## Recorrido principal (la demo)

Probar en este orden, cada paso con su cuenta. Es la cadena de custodia completa.

| Paso | Cuenta y lugar | Qué hacer | Debe pasar |
|---|---|---|---|
| 1 | Planificador · web `/datos` | Importar `demo-region` en seco y luego aplicar | Reporte sin errores; lote `Applied` (ESC-01) |
| 2 | Planificador · web `/alertas` y `/rutas` | Revisar riesgos, «Pedir al agente», publicar la ruta | Decisión en ≤ 10 s con traza (ESC-02); ruta publicada |
| 3 | Bodega · PWA | Elegir la entrega, crear el bulto, ver o imprimir su QR y despachar | Entrega `Dispatched`; el QR abre la traza pública «Despachada» (ESC-03) |
| 4 | Conductor · PWA | Descargar la ruta; ver el bulto en su entrega | Ruta real, sin datos de ejemplo |
| 5 | Conductor · PWA | Cola → «Sin señal (prueba)»; registrar una recepción con foto real y firma; volver a Online y sincronizar | Queda pendiente sin señal y luego «Completa» |
| 6 | Receptor · PWA | Pegar el enlace del QR (o escanearlo cuando llegue #122 a producción) → «Buscar bulto» → «Todo conforme» | Producto, lote y cantidad reales; recepción con identidad del Receptor (ESC-04) |
| 7 | Receptor · PWA | Buscar otra vez el mismo QR | «Este bulto ya fue recibido» |
| 8 | Planificador · web | Ver la entrega y la traza pública del bulto | Entregada; traza «Recibida» |
| 9 | Auditor · web | Recorrer Inicio, Alertas, Rutas y Simulador | Todo visible, ningún botón de acción (ESC-05) |
| 10 | Gerencia · web `/simulation` | Correr la comparación de 90 días | Cifras del run, no inventadas |

**Para el paso 6 sin escáner:** apuntar al QR con la cámara del teléfono, copiar el enlace `…/public-trace/…#t=…` y pegarlo en la tarjeta del Receptor.

Nota: en los pasos 5 y 6 la misma entrega no se recibe dos veces; usar entregas distintas para el Conductor y para el Receptor.

## Login y cuentas (spec 013)

| ID | Qué hacer | Debe pasar |
|---|---|---|
| E2E-02 | Login web con Planificador | Entra al panel sin pedir código |
| E2E-03 | F5; volver después de 15 minutos | Sigue dentro sin pedir contraseña |
| E2E-04 | Conductor en la web; Gerencia en `/campo/` | Mensaje de plataforma equivocada |
| E2E-06 | Desactivar una cuenta en «Usuarios» y entrar con ella | «Tu cuenta no está habilitada» |
| E2E-07 | «Forgot your password?» con una cuenta que ya entró antes | Llega un código y entra con la contraseña nueva |
| E2E-08 | Conductor sin señal con 2 capturas → cerrar sesión → entrar como Receptor | El Receptor no ve las capturas; el Conductor sí, al volver |
| E2E-11 | Gerencia crea un Conductor en «Usuarios» | Llega la invitación; aparece «Invitación pendiente» |
| — | Crear una cuenta con un correo que ya tiene otro rol | Rechazo que indica el rol actual (un correo = un rol) |

## Estado de los escenarios ESC

✅ listo para probar · 🟡 listo después del release `develop → main` · ⏳ issue abierto · 🔲 sin implementar

| ESC | Escenario (resumen) | Estado |
|---|---|---|
| 01, 12, 13, 21 | Importación: aplicar, supuestos o galones, filas repetidas, CSV corrupto → 422 | ✅ |
| 02, 16, 17, 18, 20 | Agente: decisión, n8n caído, timeout, cifra inventada, inyección | ✅ (automatizados; 02 en vivo) |
| 03 | Bodega empaca y despacha | ✅ (#121) |
| 04 | Receptor recibe con su identidad | ✅ (#128, #110) |
| 05, 14 | Auditor solo lectura; Gerencia no despacha desde la PWA | ✅ (#112) |
| 15, 27 | Cuenta desactivada con outbox; Cognito caído | ✅ |
| 07, 08, 09, 10 | Dos lotes del mismo SKU, segunda recepción del día, stock en percha mayor, dos reposiciones | 🟡 (#111) |
| 23 | Captura que falla siempre se aísla | 🟡 (#114) |
| 06 | Entrega parcial con motivo de catálogo y foto obligatoria | ⏳ #115, #117 (hoy el motivo es texto libre) |
| 19 | Fallo de un nodo de n8n queda en la traza | ⏳ #113 |
| 22 | 10 000 filas en < 30 s | ⏳ #117 |
| 28 | Reimportar un corte anterior a una recepción | ⏳ (007 FR-10) |
| 11 | Receptor de otro punto recibe una entrega ajena → 403 | 🔲 Límite declarado: el Receptor no está ligado a un punto; lo autoriza tener el QR del bulto (spec 014) |
| 24, 25, 26 | Carreras: replanificar mientras se recibe, desactivar mientras sincroniza, doble envío Conductor + Receptor | 🔲 No se muestran en la demo |

## Issues abiertos relacionados

| Issue | De qué trata | ¿Bloquea la demo? |
|---|---|---|
| #124 | Limpieza total de restos de demo (Jorge) | No, pero conviene antes del pitch |
| #118 | Ejecutar esta matriz con las 6 cuentas y registrar resultados | Es la ronda de pruebas misma |
| #115 | Receptor: motivo de catálogo, foto obligatoria en rechazo, outbox por identidad | No (P2) |
| #116 | Web: excepciones de recepción y pedidos de campo para el Planificador | No (P2) |
| #117 | Catálogo de motivos, fixture de caos y archivo masivo | No (P2) |
| #113 | n8n: workflow de error del agente | No (P2) |
| #46 | Pitch y paquete de entrega | Solo afirmar lo que esté ✅ en esta nota |

Consultar [[Plan de validacion]] y [[Checklist y contingencias]].
