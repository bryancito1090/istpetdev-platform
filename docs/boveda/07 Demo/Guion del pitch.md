---
tipo: demo
estado: propuesta
actualizado: 2026-10-16
tags: [demo, validacion, istpetdev]
---

# Guion del pitch

**Turno anunciado oralmente el 16-oct:** cinco minutos por equipo, según la [[2026-10-16-1109-bb-charla-limpieza-logistica|charla aportada por Bryan]]. **Meta interna:** 4:40 (280 s) para el guion maestro; reservar 20 s no es una instrucción de la organización, sino un margen operativo interno de seguridad. Falta confirmar si demo, preguntas y transiciones caben en los cinco minutos (O-04 en [[Consultas para la organizacion]]). Los cortes de 3/7 minutos quedan como variantes internas, no turnos anunciados.

> [!important] Versión maestra T3 (16-oct-2026, issue #200)
> Actualización oficial del guion maestro al turno de 5 minutos (meta 4:40 / 280 s) estructurado según el **Anexo 5.7** (p. 25 del manual oficial) e integrando la solución completa del [[Entregable Solucion y propuesta de valor]]: datos stock/consumo → riesgo → stock por bodega → vehículo → ruta priorizada → aprobación humana → despacho QR → recepción con/sin señal → impacto 90 días.
> 
> **Reglas de veracidad y restricciones técnicas:**
> 1. Cada frase y cifra se deriva estrictamente de la [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md).
> 2. **CRÍTICO:** La «prioridad de entrega con hora límite para clientes críticos» (issues #193 y #196) **ya está en `develop`** (PR #205, issues cerrados el 16-oct). Se anuncia como operativa en vivo **solo si pasa el ensayo de la ronda E2E (#118)**; si no, se presenta como en construcción.
> 3. Las capacidades sin ensayo conectado o evidencia en hardware real (como recepción offline sin red en teléfono) llevan explícitamente la etiqueta `[PENDIENTE: evidencia]`.
> 4. Se distribuye la presentación entre los **cinco papeles visibles**: Narración, Web, Móvil, Técnica y Negocio.

Protagonista: una empresa de servicios de limpieza hospitalaria y comercial; objeto conductor: una caja de desinfectante destinada a un hospital norte. El caso de la demo en vivo es sintético (`demo-region`, 15 puntos, 1 bodega, 2 vehículos) hasta contar con validación empresarial en terreno.

> [!important] Norte del pitch desde el 16-oct, 17:30
> **Dolor elegido: el módulo de planificación.** Concepto de la charla de logística: una empresa contrata logística cuando su flota propia no alcanza o no conviene. Frase central: «Para cada día de entregas, el sistema compara rutas con flota propia y contratada, suma todo lo que cuesta servir (kilómetros, combustible, personas que descargan y mermas), revisa horarios, tráfico y restricciones de Quito, y recomienda una opción con su justificación. La IA explica; el planificador decide.» Diseño en [[Planificacion potenciada - propuesta del evento]].
>
> **Cuatro capas que deben aparecer** (insumos por issue en el comentario de #200):
> 1. **Lo que ya funciona**, solo lo verificado en el ensayo.
> 2. **Lo implementable a futuro**: guía de retorno, tráfico en vivo y normativa municipal, pronóstico con estacionalidad, tipos de vehículo en el catálogo, marketplace de operadores contratados y calibración con viajes reales.
> 3. **Riesgos y estrategias de mitigación, con énfasis**: la charla lo valoró especialmente. Usar las tres tablas de la nota de planificación.
> 4. **Cómo lo implementamos**: dataset propio sintético, cálculo determinista con IA que explica, control humano y PR con pruebas.
>
> Cada issue de la noche (#235–#249) trae una sección «Para el pitch» con la frase si se hace y qué decir si no llega. El caso de la demo pasa a `demo-quito-planificacion` (#243) solo si se ensaya antes del sábado 11:30.

**Estructura oficial del pitch (Anexo 5.7):**
1. Problema (30 s) → 2. Solución (35 s) → 3. Prototipo / demo (145 s) → 4. Impacto (25 s) → 5. Viabilidad (15 s) → 6. Modelo de negocio (17 s) → 7. Próximo paso (13 s). Total: 280 s (4:40).

---

## 1. Guion maestro de 5 min (meta 4:40 / 280 s) — Versión T3

### Tabla de escena por bloques, tiempos y papeles

| Bloque (Anexo 5.7) | Tiempo (s) | Papel | Acción visible / Dispositivo | Etiqueta | Lo que se dice (guion palabra por palabra) | Matriz / Evidencia |
|---|---|---|---|---|---|---|
| **1. Problema** | 0:00–0:30 (30 s) | **Narración** | Diapositiva 1: Caso sintético, cláusula [[SERCOP limpieza hospitalaria y sanciones|SERCOP]] e ícono de caja → hospital. Clicker. | PROPUESTO + fuente documental | «En contratos públicos de limpieza hospitalaria, el proveedor está obligado a mantener los químicos de desinfección y los insumos fungibles con frecuencias de reposición definidas por el hospital. Nuestra hipótesis es que hoy la reposición empieza cuando el hospital ya avisó la falta; los pedidos imprevistos rompen la planificación de rutas. Les mostramos un caso sintético: una caja de desinfectante que va a un hospital.» | A-01 (usable doc.), A-02 (propuesta), A-03 (retirada: caso sintético) |
| **2. Solución** | 0:30–1:05 (35 s) | **Narración → Web** | Diapositiva 2: Cadena completa del [[Entregable Solucion y propuesta de valor]]: stock → riesgo → bodega → vehículo → ruta → aprobación → despacho QR → recepción. | PROPUESTO | *(Narración 0:30–0:48)* «Frente a esa operación reactiva, IstpetDev integra la cadena completa: toma datos de stock y consumo, evalúa riesgo de rotura, busca disponibilidad por bodega, asigna vehículo, calcula la ruta priorizada y acompaña la entrega con custodia QR. El cálculo es determinista y verificable; la IA asiste y explica, pero no aprueba nada.»<br/>*(Web 0:48–1:05)* «El planificador ve por qué cada punto está en riesgo, desde qué bodega se abastece y qué vehículo lo atenderá, antes de que el hospital sufra un desabastecimiento. Mostramos cómo funciona en vivo en nuestro escenario `demo-region`.» | A-06 (usable), A-10 sust. (propuesta), A-11 sust. (retirada / propuesta) |
| **3. Prototipo / demo** | 1:05–3:30 (145 s) | **Web, Técnica, Móvil** | Pantallas de la plataforma web, terminal técnica y caja física con móvil. | VIVO PREVISTO / PROPUESTO | *(Desglosado en 5 momentos continuos de la cadena logística)* | Ver desglose abajo (3a–3e) |
| *3a. Datos y Riesgo* | 1:05–1:35 (30 s) | **Web** | Pantalla Web: Lista de alertas de riesgo en `demo-region` y panel de desglose explicativo. | VIVO PREVISTO | «En nuestro escenario sintético hay una bodega, 15 puntos y dos vehículos. El hospital norte tiene cinco días de cobertura de desinfectante: riesgo alto. No es una caja negra: el sistema desglosa los datos que explican la alerta: stock disponible, consumo diario y lead time de entrega.» *(Relevo: «Con el riesgo identificado, el sistema analiza bodega, vehículo y ruta»)* | A-03 sust., A-04 (usable), A-05 (usable), A-06 (usable) |
| *3b. Bodega, Vehículo y Ruta* | 1:35–2:05 (30 s) | **Web** | Pantalla Web: Selector de bodega base, asignación de vehículo y visualización de rutas con paradas. | VIVO PREVISTO | «El sistema busca disponibilidad en bodega respetando stock de seguridad y vencimiento, y asigna el vehículo según capacidad de peso y volumen. Propone una ruta respetando capacidad, horarios de atención y jornada.<br/>*Nota de transparencia técnica:* La prioridad de entrega con hora límite para clientes críticos (issues #193 y #196) está **en construcción** y no se muestra en vivo; hoy las paradas se ordenan por criticidad y holgura en el motor determinista.» *(Relevo: «[Técnica], simulemos un imprevisto en la vía»)* | A-07 (usable), Entregable 2 (pasos 3, 4, 5). Issues #193 y #196 en construcción |
| *3c. Agente e Imprevisto vial* | 2:05–2:30 (25 s) | **Técnica** | Disparo de incidente vial (`POST /api/v1/demo/incidents`) y panel de ruta actualizada / umbrales. | VIVO PREVISTO | «Cerramos una vía principal. Ante el incidente, el sistema conserva las paradas ya realizadas y genera una nueva versión de ruta para las pendientes. Si el cambio supera los límites de costo o retraso, no se auto-ejecuta: el asistente con IA explica el impacto y deja la decisión esperando aprobación de una persona. Si el servicio de IA no estuviese disponible, el motor logístico continúa operando por reglas.» *(Relevo: «La ruta está aprobada. [Móvil], ¿cómo sale la caja?»)* | A-09 (usable), A-10 sust. (propuesta), A-12 sust. (propuesta) |
| *3d. Despacho y Custodia QR* | 2:30–2:55 (25 s) | **Móvil** | Muestra de caja física con rótulo QR impreso y flujo de despacho en móvil. | PROPUESTO / FÍSICO | «Así se prepara la caja en bodega: queda ligada a su lote, cantidades y entrega mediante su código QR único. Así registramos la custodia física antes del despacho. `[PENDIENTE: evidencia de ensayo conectado E2E-04 en vivo: hoy mostramos la etiqueta física y el flujo como diseño funcional probado a nivel de componentes]`.» *(Relevo: «El conductor llega a entregar al hospital»)* | A-13 sust. (propuesta: diseño probado, falta E2E-04 conectado) |
| *3e. Recepción y Trazabilidad* | 2:55–3:30 (35 s) | **Móvil → Web** | *(Móvil)* PWA de campo; *(Web)* Consulta pública de trazabilidad `/public-trace/{unitId}`. | PROPUESTO (Móvil) / VIVO PREVISTO (API) | *(Móvil 2:55–3:15)* «El conductor registra la recepción en la app con firma y foto. Cada recepción lleva una clave única de operación para evitar duplicados. La captura sin señal y sincronización offline en la PWA está diseñada e implementada, pero está `[PENDIENTE: evidencia de ensayo en teléfono real sin red]`.»<br/>*(Web 3:15–3:30)* «Cualquier auditor puede consultar el QR: la API pública devuelve producto, estado e hitos de custodia sin exponer datos privados ni cantidades sensibles. Un token alterado recibe 404 en nuestras pruebas de API.» *(Relevo Web: «¿Y si esto se repite durante 90 días?»)* | A-14 (propuesta: clave única), A-15 retirada (registro en app; offline [PENDIENTE: evidencia]), A-16 sust. (usable API pública) |
| **4. Impacto** | 3:30–3:55 (25 s) | **Web → Narración** | Diapositiva 4 / Pantalla: Resultados canónicos del simulador de 90 días ([[Cifras del simulador 90 dias]]). | SIMULADO (12 corridas verificables) | «Diseñamos la comparación antes de ver resultados: 90 días sintéticos de demanda idéntica sobre 80 puntos nacionales, tres semillas base y un escenario adverso aparte con 20% más demanda y un vehículo menos. Comparamos contra reposición reactiva y calendario fijo semanal. Contamos los quiebres evitados y también los nuevos, sin disfrazarlos tras una resta neta. Según las [[Cifras del simulador 90 dias]] verificables, IstpetDev reduce un 9,8% los kilómetros frente al calendario fijo manteniendo la reposición preventiva.» *(Relevo: «¿Es viable llevar esto a la práctica? [Técnica]»)* | A-19 (propuesta/usable según archivo verificable), A-20 (usable como método), A-21 (mediana 7.037,5 km vs 7.800 km) |
| **5. Viabilidad** | 3:55–4:10 (15 s) | **Técnica** | Diapositiva 5: Viabilidad técnica, perfil de importación y benchmark local. | VIVO PREVISTO (pruebas locales) | «Usa datos que una empresa ya tiene: incorporar otro formato de archivo se resuelve escribiendo un perfil de mapeo, sin programar. En pruebas locales, el cálculo de rutas para 80 puntos toma menos de cinco segundos. Y declaramos un límite honesto: usamos heurísticas factibles con ventanas horarias y capacidad; no prometemos la ruta óptima.» *(Relevo: «¿Cómo se sostiene como negocio? [Negocio]»)* | A-22 (usable), A-23 sust., A-24 (usable con límite), A-33 (heurística, nunca ruta óptima) |
| **6. Modelo de negocio** | 4:10–4:27 (17 s) | **Negocio** | Diapositiva 6: Estructura de clientes y flujo de ingresos por suscripción / servicio. | PROPUESTO | «Nuestro cliente por validar es la empresa de servicios de limpieza que atiende contratos multisede con clientes como hospitales y oficinas. Proponemos cobrar una tarifa de implementación e integración inicial más una suscripción periódica por bodega y puntos atendidos. El precio final y los costos operativos mensuales están `[PENDIENTE: evidencia de costos reales y disposición de pago en piloto]`, por lo que no anunciamos cifras fijas.» *(Relevo: «Para validar esto, tenemos un plan concreto»)* | A-26 sust. (retirada USD 37), A-30 sust. (retirado precio fijo), A-31 sust. (propuesta) |
| **7. Próximo paso** | 4:27–4:40 (13 s) | **Negocio → Narración** | Diapositiva 7: Hoja de ruta del piloto de 4 semanas y llamada a la acción. | PROPUESTO | *(Negocio 4:27–4:35)* «Proponemos un piloto de cuatro semanas con una bodega base y un grupo de puntos acordados, para medir en terreno la reducción de faltantes y afinar costos.»<br/>*(Narración 4:35–4:40)* «Buscamos empresas del sector dispuestas a medir esta mejora operativa con nosotros. Muchas gracias.» | A-32 (propuesta: piloto de 4 semanas, nunca cliente comprometido) |

*Margen de reserva:* 4:40 a 5:00 (20 segundos reservados para contingencias de escena, aplausos o inicio de preguntas del jurado).

---

### Texto continuo para ensayo cronometrado (Lectura de 280 s)

> **Voz de Narración (0:00–0:30):**  
> «En contratos públicos de limpieza hospitalaria, el proveedor está obligado a mantener los químicos de desinfección y los insumos fungibles con frecuencias de reposición definidas por el hospital. Nuestra hipótesis es que hoy la reposición empieza cuando el hospital ya avisó la falta; los pedidos imprevistos rompen la planificación de rutas. Les mostramos un caso sintético: una caja de desinfectante que va a un hospital.»
> 
> **Voz de Narración (0:30–0:48):**  
> «Frente a esa operación reactiva, IstpetDev integra la cadena completa: toma datos de stock y consumo, evalúa riesgo de rotura, busca disponibilidad por bodega, asigna vehículo, calcula la ruta priorizada y acompaña la entrega con custodia QR. El cálculo es determinista y verificable; la IA asiste y explica, pero no aprueba nada.»
> 
> **Voz de Web (0:48–1:05):**  
> «El planificador ve por qué cada punto está en riesgo, desde qué bodega se abastece y qué vehículo lo atenderá, antes de que el hospital sufra un desabastecimiento. Mostramos cómo funciona en vivo en nuestro escenario `demo-region`.»
> 
> **Voz de Web (1:05–1:35):**  
> «En nuestro escenario sintético hay una bodega, 15 puntos y dos vehículos. El hospital norte tiene cinco días de cobertura de desinfectante: riesgo alto. No es una caja negra: el sistema desglosa los datos que explican la alerta: stock disponible, consumo diario y lead time de entrega. Con el riesgo identificado, el sistema analiza bodega, vehículo y ruta.»
> 
> **Voz de Web (1:35–2:05):**  
> «El sistema busca disponibilidad en bodega respetando stock de seguridad y vencimiento, y asigna el vehículo según capacidad de peso y volumen. Propone una ruta respetando capacidad, horarios de atención y jornada. *Nota de transparencia:* La prioridad de entrega con hora límite para clientes críticos (issues #193 y #196) está en construcción y no se muestra en vivo; hoy las paradas se ordenan por criticidad y holgura en el motor determinista. [Técnica], simulemos un imprevisto en la vía.»
> 
> **Voz de Técnica (2:05–2:30):**  
> «Cerramos una vía principal. Ante el incidente, el sistema conserva las paradas ya realizadas y genera una nueva versión de ruta para las pendientes. Si el cambio supera los límites de costo o retraso, no se auto-ejecuta: el asistente con IA explica el impacto y deja la decisión esperando aprobación de una persona. Si el servicio de IA no estuviese disponible, el motor logístico continúa operando por reglas. La ruta está aprobada. [Móvil], ¿cómo sale la caja?»
> 
> **Voz de Móvil (2:30–2:55):**  
> «Así se prepara la caja en bodega: queda ligada a su lote, cantidades y entrega mediante su código QR único. Así registramos la custodia física antes del despacho. `[PENDIENTE: evidencia de ensayo conectado E2E-04 en vivo: hoy mostramos la etiqueta física y el flujo como diseño funcional probado a nivel de componentes]`. El conductor llega a entregar al hospital.»
> 
> **Voz de Móvil (2:55–3:15):**  
> «El conductor registra la recepción en la app con firma y foto. Cada recepción lleva una clave única de operación para evitar duplicados. La captura sin señal y sincronización offline en la PWA está diseñada e implementada, pero está `[PENDIENTE: evidencia de ensayo en teléfono real sin red]`.»
> 
> **Voz de Web (3:15–3:30):**  
> «Cualquier auditor puede consultar el QR: la API pública devuelve producto, estado e hitos de custodia sin exponer datos privados ni cantidades sensibles. Un token alterado recibe 404 en nuestras pruebas de API. ¿Y si esto se repite durante 90 días?»
> 
> **Voz de Web y Narración (3:30–3:55):**  
> «Diseñamos la comparación antes de ver resultados: 90 días sintéticos de demanda idéntica sobre 80 puntos nacionales, tres semillas base y un escenario adverso aparte con 20% más demanda y un vehículo menos. Comparamos contra reposición reactiva y calendario fijo semanal. Contamos los quiebres evitados y también los nuevos, sin disfrazarlos tras una resta neta. Según las [[Cifras del simulador 90 dias]] verificables, IstpetDev reduce un 9,8% los kilómetros frente al calendario fijo manteniendo la reposición preventiva. ¿Es viable llevar esto a la práctica? [Técnica].»
> 
> **Voz de Técnica (3:55–4:10):**  
> «Usa datos que una empresa ya tiene: incorporar otro formato de archivo se resuelve escribiendo un perfil de mapeo, sin programar. En pruebas locales, el cálculo de rutas para 80 puntos toma menos de cinco segundos. Y declaramos un límite honesto: usamos heurísticas factibles con ventanas horarias y capacidad; no prometemos la ruta óptima. ¿Cómo se sostiene como negocio? [Negocio].»
> 
> **Voz de Negocio (4:10–4:27):**  
> «Nuestro cliente por validar es la empresa de servicios de limpieza que atiende contratos multisede con clientes como hospitales y oficinas. Proponemos cobrar una tarifa de implementación e integración inicial más una suscripción periódica por bodega y puntos atendidos. El precio final y los costos operativos mensuales están `[PENDIENTE: evidencia de costos reales y disposición de pago en piloto]`, por lo que no anunciamos cifras fijas. Para validar esto, tenemos un plan concreto.»
> 
> **Voz de Negocio y Narración (4:27–4:40):**  
> *(Negocio)* «Proponemos un piloto de cuatro semanas con una bodega base y un grupo de puntos acordados, para medir en terreno la reducción de faltantes y afinar costos.»  
> *(Narración)* «Buscamos empresas del sector dispuestas a medir esta mejora operativa con nosotros. Muchas gracias.»

---

## 2. Puntos de atención crítica y contrastes con la matriz de afirmaciones

1. **Hora límite para críticos (#193 y #196):**
   - La funcionalidad de hora límite para paradas críticas y comparación de costos entre plan base y priorizado fue desarrollada en las ramas `feat/196-...` y `fix/201-...` (ver [[Integracion ramas 196 y 201]]), pero al momento de este guion **no está fusionada en develop**.
   - **Regla estricta:** En el pitch se menciona **únicamente como en construcción**. Bajo ninguna circunstancia debe afirmarse que ya funciona en vivo.
2. **Recepción offline y sincronización sin red (A-15):**
   - Aunque la PWA tiene persistencia local y sincronización implementadas (specs 004 y 015), en `develop` no existe un registro de ensayo en teléfono físico sin conexión (modo avión) contra el entorno desplegado.
   - **Regla estricta:** Se anuncia que el conductor registra la entrega en la app; la recepción sin señal se presenta como arquitectura diseñada, marcada como `[PENDIENTE: evidencia de ensayo en teléfono real sin red]`.
3. **Escaneo del jurado (A-16):**
   - La consulta pública está demostrada por API (`PublicTraceHttpTests` en EJ-07); sin embargo, no se dispone de conectividad externa ni HTTPS validado en el recinto de la feria (O-08).
   - **Regla estricta:** No se invita al jurado a escanear en vivo; se explica la prueba de API y se muestra en pantalla controlada o diapositiva.
4. **Resultados del simulador (A-19, A-20, A-21):**
   - Las cifras del 9,8% de reducción de kilómetros frente a calendario fijo semanal (`fixed7-v1`) y el comportamiento en escenario adverso provienen del fixture canónico `national-v1` documentado en [[Cifras del simulador 90 dias]]. Se citan siempre como simulación sintética de 90 días, nunca como ahorro comercial observado.

---

## 3. Variantes de tiempo internas (3 min y 7 min)

Estas variantes son respaldos internos para imprevistos en la agenda o preguntas extendidas. El turno oficial anunciado es de 5 minutos.

### Corte de 3 min (meta 160 s)

Mismo orden de 7 bloques y mismos 5 papeles visibles; una acción por sistema.

| Bloque (Anexo 5.7) | Tiempo (s) | Papel visible | Lo que se conserva | Matriz |
|---|---|---|---|---|
| 1. Problema | 0:00–0:18 (18 s) | Narración | Obligación contractual hospitalaria e hipótesis de reposición reactiva. Caso sintético. | A-01 + A-02 |
| 2. Solución | 0:18–0:38 (20 s) | Narración → Web | Cadena integrada: calcula riesgo, propone ruta y custodia QR. La IA explica; humano aprueba. | A-06, A-10 sust., A-11 sust. |
| 3. Demo | 0:38–1:52 (74 s) | Web, Técnica, Móvil | Web: riesgo 5 días (A-04) y ruta (A-07). Técnica: incidente y aprobación (A-09, A-10 sust.). Móvil: caja con QR y registro en app (A-13 sust., A-14). *Hora límite en construcción.* | A-04, A-07, A-09, A-10 sust., A-13 sust., A-14 |
| 4. Impacto | 1:52–2:10 (18 s) | Web → Narración | 90 días sintéticos: IstpetDev reduce 9,8% de km frente a calendario fijo; quiebres evitados y nuevos. | A-19, A-20, A-21 |
| 5. Viabilidad | 2:10–2:22 (12 s) | Técnica | Carga por perfil sin programar; 80 puntos en <5 s local. Heurística factible, no ruta óptima. | A-22, A-24, A-33 |
| 6. Modelo de negocio | 2:22–2:32 (10 s) | Negocio | Empresa de limpieza cliente por validar. Implementación + suscripción; precio en piloto. | A-31 sust., A-30 sust. |
| 7. Próximo paso | 2:32–2:40 (8 s) | Negocio → Narración | Piloto de cuatro semanas con una bodega y puntos acordados. | A-32 |

### Corte de 7 min (meta 400 s)

Mismo hilo conductor; amplía detalles técnicos, supuestos y límites de la cadena de valor sin inventar nuevas historias.

| Bloque (Anexo 5.7) | Tiempo (s) | Papel visible | Qué se añade al maestro de 5 min |
|---|---|---|---|
| 1. Problema | 0:00–0:45 (45 s) | Narración | Contexto de contratos de limpieza hospitalaria y hallazgos preliminares de la feria ([[Hallazgos de la charla para el Reto 1]]). `[CITA DE LA FERIA]` solo con consentimiento expreso (A-29). |
| 2. Solución | 0:45–1:35 (50 s) | Narración → Web | Detalle de los 10 pasos del [[Entregable Solucion y propuesta de valor]]: desde importación hasta trazabilidad. Qué decide la regla determinista y qué asiste el agente. |
| 3. Demo | 1:35–5:00 (205 s) | Web, Técnica, Móvil | Distribución de riesgos 1 crítico y 4 altos (A-05); asignación FEFO por bodega y capacidad de vehículo; detalle del recálculo ante incidente; token de trazabilidad pública con verificación 404. Estado en construcción de #193/#196. |
| 4. Impacto | 5:00–5:35 (35 s) | Web → Narración | Justificación metodológica de los dos baselines (reactivo vs fijo) y desglose de las 12 corridas canónicas ([[Cifras del simulador 90 dias]]). |
| 5. Viabilidad | 5:35–6:00 (25 s) | Técnica | Detalle del importador desacoplado, integración con n8n/DeepSeek como fallback determinista (A-12 sust.), benchmark en máquina local. |
| 6. Modelo de negocio | 6:00–6:22 (22 s) | Negocio | Análisis de los costos de operación a medir en el piloto ([[Modelo de negocio y piloto]]). |
| 7. Próximo paso | 6:22–6:40 (18 s) | Negocio → Narración | Cronograma de 4 semanas del piloto (semana 1 datos/baseline, semana 2 bodega piloto, semanas 3–4 ruta asistida). |

---

## 4. Estructura de las siete diapositivas (Anexo 5.7)

Las diapositivas acompañan el turno de 5 min. Si el proyector falla, el equipo expone el mismo guion con soporte físico o verbal (ver [[Checklist y contingencias]]).

| # | Bloque | Título en diapositiva | Contenido visual y textual | Rótulo visible obligatorio |
|---|---|---|---|---|
| 1 | Problema | Reponer antes de la llamada | Cláusula contractual [[SERCOP limpieza hospitalaria y sanciones|SERCOP]] de insumos y desinfectantes; hipótesis de reposición reactiva; ícono de caja hacia hospital. | `Caso sintético` |
| 2 | Solución | Cadena logística completa y explicable | Flujo de 7 pasos del [[Entregable Solucion y propuesta de valor]]: Stock/Consumo → Riesgo → Bodega → Vehículo → Ruta → Aprobación humana → Custodia QR. «La IA explica; una persona aprueba». | `Esquema de solución` |
| 3 | Prototipo / demo | De la bodega al hospital norte | Panel web de riesgos (`demo-region`), mapa de ruta y paradas, replanificación por incidente vial, caja con QR de custodia y consulta de trazabilidad. | `Demo en vivo` / `Captura de respaldo` |
| 4 | Impacto | 90 días simulados: Comparación controlada | Resumen canónico de [[Cifras del simulador 90 dias]]: 12 corridas verificables (3 semillas + adverso), comparación vs reposición reactiva y fija semanal. -9,8% km frente a fijo. Quiebres evitados y nuevos por separado. | `Simulación — 12 corridas verificables` |
| 5 | Viabilidad | Factibilidad técnica y límites transparentes | Carga de formatos con perfil de mapeo; benchmark local de 80 puntos en <5 s; aclaración explícita: heurística logística factible, no ruta óptima global. | `Pruebas locales y arquitectura` |
| 6 | Modelo de negocio | Cliente objetivo y propuesta de valor | Cliente por validar: empresas de servicios de limpieza multisede; modelo de implementación más suscripción por bodega/puntos activos; costos y precio por validar en piloto. | `Hipótesis de negocio` |
| 7 | Próximo paso | Piloto en terreno de cuatro semanas | Plan de piloto en 4 semanas (datos, bodega piloto, operación asistida, medición); búsqueda de empresa aliada. | `Propuesta de validación` |

---

## 5. Banco de 24 preguntas difíciles (Respuestas de hasta 30 s)

Ensayar cada respuesta con cronómetro (≤30 s). Cada afirmación técnica responde únicamente a lo probado en código o registrado en la matriz.

| # | Pregunta previsible | Respuesta ensayada (≤ 30 segundos) | Matriz / Límite |
|---:|---|---|---|
| 1 | ¿La IA predice el consumo? | No; el motor calcula el riesgo mediante una regla determinista basada en saldo actual, consumo promedio conocido y lead time. El asistente con IA (n8n + DeepSeek) organiza y explica la alerta con las cifras del motor, pero no inventa números. | A-06, A-11 sust. |
| 2 | ¿La IA aprueba o replanifica rutas sola? | No. Las decisiones se rigen por políticas deterministas. Si un incidente supera los límites fijados de retraso o sobrecosto, el sistema exige aprobación de una persona. El LLM no tiene permisos de aprobación en la arquitectura. | A-10 sust., A-11 sust. |
| 3 | ¿Qué ocurre si DeepSeek o el agente fallan? | El motor logístico y las explicaciones por plantilla fija son el respaldo base del sistema. Si el servicio de IA no responde, la propuesta de ruta y las alertas se generan normalmente desde el backend determinista. | A-12 sust. |
| 4 | ¿La ruta calculada es la óptima matemáticamente? | Es una heurística factible que respeta capacidades de peso/volumen, ventanas horarias de atención y jornada laboral; no demostramos matemáticamente un óptimo global. | A-07, A-33 |
| 5 | ¿Qué datos mínimos requiere la plataforma? | Catálogo de productos, puntos de servicio, bodegas con stock inicial, consumos históricos y flota de vehículos con sus capacidades. Los datos faltantes se reportan como supuestos. | Spec 001 |
| 6 | ¿Qué pasa si el cliente entrega un Excel con otro formato? | La plataforma cuenta con un importador configurable mediante perfiles de mapeo JSON/YAML. Permite vincular columnas y tipos de datos sin reprogramar la aplicación. | A-22, A-23 sust. |
| 7 | ¿Cómo conoce el inventario en los puntos de servicio? | A través de los saldos iniciales y los registros de entrega y consumo reportados. La plataforma muestra la frescura temporal del dato y no asume inventario en tiempo real sin lectura. | A-06 |
| 8 | ¿Qué ocurre cuando no hay señal en el punto de entrega? | La aplicación móvil está construida como PWA con almacenamiento local para permitir el registro offline. Al recuperar conectividad, sincroniza la operación. *(Nota interna: capacidad en PWA; prueba en teléfono real en recinto: `[PENDIENTE: evidencia]`)*. | A-15 |
| 9 | ¿Cómo evitan que una entrega se duplique al sincronizar? | Cada operación en campo genera un identificador único idempotente (`operationId`), transacción de base de datos y validación de cantidades entregadas. | A-14 sust. |
| 10 | ¿Cualquier persona puede ver datos sensibles escaneando el QR? | No. La consulta pública del QR devuelve únicamente estado general del bulto e hitos de custodia mediante un token de acceso temporal, sin mostrar precios, cantidades confidenciales ni firmas privadas. | A-16 sust. |
| 11 | ¿La plataforma funciona a escala nacional con 80 puntos en vivo? | En la demo en vivo operamos `demo-region` con 15 puntos, una bodega y dos vehículos para asegurar estabilidad. La escala nacional de 80 puntos y múltiples bodegas está probada y evaluada en el simulador de 90 días. | A-03 sust., A-24, A-25 |
| 12 | ¿Qué demuestra exactamente la simulación de 90 días? | Compara el comportamiento de tres políticas logísticas bajo una misma demanda sintética idéntica durante 90 días; no es un ahorro medido históricamente en una empresa en producción. | A-19 sust. |
| 13 | ¿Por qué eligieron esas políticas de comparación? | Representan las prácticas habituales del sector: reposición reactiva tras la llamada de emergencia y reposición fija por calendario semanal. Ambas fueron fijadas con reglas claras antes de comparar. | [[Cifras del simulador 90 dias]] |
| 14 | ¿Por qué muestran quiebres evitados y nuevos por separado? | Porque una política preventiva puede evitar roturas críticas pero generar desajustes menores en otros puntos. Mostramos ambas cifras transparentemente sin esconder pérdidas tras un promedio neto. | A-20 |
| 15 | ¿Por qué evalúan tres semillas y un escenario adverso? | Las tres semillas muestran que el resultado no depende de una coincidencia estadística; el escenario adverso prueba la resiliencia del sistema ante un 20% más de demanda y menor capacidad de flota. | [[Cifras del simulador 90 dias]] |
| 16 | ¿Cuánto ahorra una empresa de limpieza en dinero real? | Está `[PENDIENTE: evidencia de ahorro observado en piloto real]`. En el simulador nacional canónico observamos un 9,8% de reducción de kilómetros frente a un calendario fijo semanal. | A-21, A-26 sust. |
| 17 | ¿Cuál es el costo y precio comercial de la plataforma? | Cobraríamos una tarifa de implementación más una suscripción mensual por bodega y puntos. El precio exacto está `[PENDIENTE: evidencia de costos en piloto y disposición de pago]`. | A-30 sust., A-31 sust. |
| 18 | ¿El sistema evita multas contractuales en hospitales? | El análisis documental del SERCOP demuestra que los contratos contemplan sanciones por incumplimiento de reposición; nuestra plataforma busca reducir ese riesgo operativo, pero no cuantificamos multas legales evitadas. | A-01, A-33 |
| 19 | ¿Qué validación real tienen con el sector? | Contrastamos el problema y los supuestos con las ponencias y expositores de la feria ([[Hallazgos de la charla para el Reto 1]]); las métricas económicas concretas serán validadas durante el piloto. | A-29 sust. |
| 20 | ¿Qué es trabajo previo y qué se desarrolló durante la hackathon? | La arquitectura base y lineamientos vienen de la preparación del equipo identificada con etiquetas; la integración de la cadena completa de 10 pasos, el simulador canónico y las pruebas se consolidaron durante el evento. | Matriz / Historial git |
| 21 | ¿A quién atienden primero si no alcanza el camión? | El motor prioriza mediante un orden determinista y transparente: primero evalúa el nivel de riesgo (Crítico antes que Alto), luego la menor holgura de tiempo (cobertura menos lead time), después la criticidad del punto de servicio, y finalmente el código. Si no alcanza la capacidad, los puntos menos urgentes quedan formalmente clasificados como no atendidos con su motivo explícito (`ReplenishmentAllocator` y `RoutePlanner`). | AMP-01, Spec 002 |
| 22 | ¿Qué pasa ante un cierre vial? | Se ejecuta una replanificación determinista que conserva las paradas ya realizadas y genera una nueva versión de ruta. El motor dispone de modos configurables: `Strict`, `LateService` si admite demoras, o `PrioritizeCritical`, que descarta paradas de consolidación para proteger las entregas críticas. Si el plan resultante supera los límites de costo o jornada, no se auto-ejecuta y escala a aprobación humana. | AMP-03, Spec 002 |
| 23 | ¿Por qué no Dijkstra? | Dijkstra halla el camino mínimo entre nodos aislados en un grafo estático. En nuestro sistema, el servicio OSRM ya calcula distancias y tiempos mínimos sobre la red vial real de OpenStreetMap, respetando sentidos de giro y velocidades reales. Sobre esa matriz real, el algoritmo del motor resuelve el problema complejo de ruteo vehicular con ventanas horarias, jornadas y capacidades físicas. | AMP-10, OSRM |
| 24 | ¿Qué pasa si nos dan otro Excel? | El sistema dispone de un importador canónico con perfiles de mapeo declarativos. Permite homologar archivos con distintas columnas, orden o unidades hacia el esquema interno sin modificar una sola línea de código fuente. Su funcionamiento está probado y validado con datasets externos, permitiendo incorporar nuevos datos estructurados con reglas de validación previas. | Spec 001, ADR-22 |

---

## 6. Frases retiradas y su sustitución obligatoria

Para proteger la integridad y honestidad de la presentación, las siguientes expresiones quedan estrictamente prohibidas y sustituidas según la [matriz de afirmaciones](../../specs/006-validacion-pitch-entrega/matriz-afirmaciones.md):

| Expresión prohibida / retirada | Código | Motivo | Sustitución obligatoria en escena |
|---|---|---|---|
| «80 puntos del país en vivo» | A-03, A-25 | El vivo usa `demo-region` (15 puntos, 1 bodega, 2 vehículos); 80 puntos es simulación. | «En nuestro escenario sintético: una bodega, 15 puntos y dos vehículos». |
| «La IA detecta / aprueba / replanifica sola» | A-11, A-33 | El cálculo es determinista. El LLM asiste y redacta; una persona aprueba cambios sensibles. | «El cálculo es una regla determinista verificable; la IA asiste y explica, no aprueba nada». |
| «El conductor recibe sin señal en vivo» | A-15 | No hay ensayo registrado en teléfono real sin red en el entorno de la feria. | «El conductor registra la recepción en la app. La operación offline está [PENDIENTE: evidencia de ensayo en teléfono]». |
| «Escaneen el QR con sus teléfonos» (al jurado) | A-16 | Conectividad y HTTPS externo no verificados en el recinto (O-08). | Mostrar la consulta en pantalla o diapositiva; explicar que la API pública devuelve estado sin datos privados. |
| «Ahorro económico demostrado de X%» | A-21, A-26 | No hay piloto previo con contabilidad real; el ahorro es de kilómetros en simulación canónica. | «En simulación sintética de 90 días, reduce un 9,8% los km frente al calendario fijo». |
| «Cuesta USD 37 al mes operarlo» | A-26 | La cotización preliminar no incluía base de datos gestionada ni costos reales de red. | «El costo de operación se medirá y calibrará en el piloto». |
| «Cualquier precio de suscripción en dólares» | A-30 | Hipótesis sin validación comercial de disposición de pago. | «El precio se definirá tras conocer los costos reales y la disposición de pago en el piloto». |
| «Ruta óptima» / «Multas evitadas» / «Ahorro real» | A-33 | Afirmaciones absolutas no demostradas matemáticamente ni con contratos reales. | «Heurística factible respetando ventanas y capacidad»; «prevenir faltantes antes del quiebre». |
| «La prioridad con hora límite ya funciona en vivo» | Issues #193, #196 | No está fusionada en `develop`. | «La prioridad de entrega con hora límite para críticos está en construcción». |

---

## 7. Antecedentes históricos

### Recorrido de 180 segundos (Borrador inicial T1)

| Tiempo | Lo que contamos | Lo que mostramos |
|---|---|---|
| 00:00–00:20 | «Cuando el hospital llama por falta de desinfectante, la emergencia ya ocurrió» | Punto y problema operacional |
| 00:20–00:45 | «Detectamos riesgo antes del faltante y sabemos qué datos lo explican» | Cobertura, lead time, fecha/frescura y cantidad propuesta |
| 00:45–01:15 | «Planificamos una ruta viable y respondemos a un cierre de vía» | Tradicional/optimizada; incidente y nueva versión |
| 01:15–02:05 | «Esta caja conserva su custodia aunque el conductor pierda la señal» | QR, despacho, recepción offline y sincronización |
| 02:05–02:40 | «Comparamos ambas políticas bajo la misma demanda durante 90 días simulados» | KPIs, denominadores y manifiesto del run |
| 02:40–03:00 | «Cuesta [costo verificado] operarlo, lo paga [cliente] y el siguiente paso es un piloto acotado con una empresa» | Viabilidad, modelo de negocio y próximo paso (piloto) |

### Texto base inicial

«Una empresa de limpieza atiende 80 puntos del país. En nuestro escenario, un hospital tiene cinco días de cobertura. Hoy una operación reactiva espera a la llamada; nuestra plataforma identifica el riesgo, explica stock y consumo, y propone abastecimiento dentro del tiempo necesario.

Aquí vemos la ruta, su capacidad y sus horarios. Si se bloquea una vía, conserva lo ya entregado y propone el cambio para las paradas pendientes.

Esta caja se escanea en bodega y queda asociada a su lote y cantidades. El conductor puede capturar la recepción en la app. Al conectarse, la operación se registra una sola vez y la evidencia completa la trazabilidad.

Comparamos 90 días de demanda sintética idéntica para ambas políticas. Los resultados de esta ejecución son los que aparecen en pantalla y pueden exportarse.

Buscamos validar el sistema con un piloto de cuatro semanas, empezando por una bodega y un grupo de puntos.»

---

## Material de apoyo para preguntas del jurado

- **Protocolos de adopción y mitigación de resistencia al cambio:** [[Protocolos de capacitacion por rol]]. Hojas de una sola página por puesto de trabajo con prácticas sobre el escenario de Quito:
  - [[Capacitacion - Planificador]]: Comparación de opciones A/B/C, ventanas horarias estrictas y relevo ante avería vial.
  - [[Capacitacion - Bodega]]: Preparación, doble verificación de lote/vencimiento, bultos pesados >25 kg y rótulo QR.
  - [[Capacitacion - Conductor]]: Descarga de ruta para trabajo sin red, escaneo de custodia y entrega.
  - [[Capacitacion - Receptor]]: Validación de bulto, rechazo con motivo y foto de evidencia en farmacia/cliente.
  - [[Capacitacion - Gerencia]]: Panel ejecutivo «Hoy», lectura de los 8 KPIs y comparación propia frente a contratada.
  - [[Capacitacion - Auditor]]: Trazabilidad forense de bultos (HU), inspección de firmas y fotos en modo solo lectura.
- **Indicadores en lenguaje sencillo:** [[Indicadores de operacion en lenguaje sencillo]], con los 6 indicadores de la charla técnica más costo por entrega y propia vs. contratada, explicando su pregunta sencilla, fórmula y origen en la app.

---

Notas relacionadas: [[Entregable Solucion y propuesta de valor]], [[Cifras del simulador 90 dias]], [[Checklist y contingencias]], [[Plus para ganar]], [[Plan de validacion]], [[Historia y propuesta de valor]], [[Modelo de negocio y piloto]], [[Protocolos de capacitacion por rol]], [[Indicadores de operacion en lenguaje sencillo]].

