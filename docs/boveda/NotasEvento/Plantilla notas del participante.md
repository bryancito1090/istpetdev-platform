---
tipo: plantilla
estado: plantilla
actualizado: 2026-10-16
tags: [hackathon, equipo, evento, participante]
---

# Plantilla notas del participante

Esta nota concentra el contexto operativo esencial que cada integrante del equipo **IstpetDev** necesita tener a mano durante el **Hackathon Expo Clean Ecuador 2026**, junto con la ficha de registro personal y bitácora de actividades para las jornadas del 16 y 17 de octubre de 2026.

> [!TIP]
> **Instrucciones para los integrantes:**
> Puedes duplicar este archivo dentro de la carpeta `NotasEvento/` con el nombre de tu archivo personal (ejemplo: `Participante - Bryan Bano.md`, respetando la regla de nombres sin tildes) y completar tus datos, dispositivos, rol de demostración y bitácora individual; o utilizar esta plantilla como base de referencia continua.

---

## 1. Contexto maestro del proyecto y del evento

Para consultar el detalle completo de decisiones y especificaciones, remitirse a las notas maestras enlazadas.

### 1.1 Identidad del reto
- **Evento:** Hackathon Expo Clean Ecuador 2026 (16 y 17 de octubre de 2026).
- **Sede:** Centro de Exposiciones Quito (CEQ), America/Guayaquil (UTC−05:00).
- **Reto oficial:** Reto 1 — Optimización y trazabilidad logística para el abastecimiento de insumos a nivel nacional ([[Reto 1 oficial]]).
- **Área:** Gestión Inteligente de Procesos.
- **Equipo:** IstpetDev (5 integrantes: Bryan Baño, Jorge Doicela, Joseph Baño, Anthony Pilatasig, Alexander Martínez). Estructura flexible de desarrollo full stack ([[Equipo y acuerdos]]).

### 1.2 Misión y propuesta de valor
- **Problema central:** Ruptura de stock en puntos críticos (hospitales y clientes a nivel nacional) debido a despachos reactivos, falta de visibilidad en ruta y pérdidas de trazabilidad en la entrega.
- **Solución integral:** Plataforma predictiva y operativa que:
  1. **Anticipa el riesgo de desabastecimiento:** Detección de quiebres con 5 días de anticipación sobre 80 puntos de servicio.
  2. **Optimiza rutas de transporte:** Planificación inteligente (algoritmo Clarke-Wright / VRP) que reduce costo por kilómetro frente al baseline histórico ([[Rutas y sobrecostos]]).
  3. **Asegura la custodia en bodega y campo:** Despacho con etiquetas QR físicas únicas, recepción móvil offline (PWA) con firma táctil digital y captura fotográfica obligatoria.
  4. **Explicabilidad con Inteligencia Artificial:** Agente logístico (DeepSeek vía n8n) con función analítica y explicativa; no inventa datos ni toma decisiones numéricas no deterministas.

### 1.3 Cifras canónicas de la demo
- **Puntos simulados:** 80 puntos nacionales, 6 regiones, horizonte de 90 días con dataset sintético `national-v1` ([[Cifras del simulador 90 dias]]).
- **Escenario hospitalario del pitch:** Hospital de especialidades (consumo 12 L/día, lead time 2 días, margen de seguridad 3 días → alerta al llegar a 60 L de cobertura).
- **Recorrido en vivo:** Demostración con 1 bodega central, vehículos asignados y recepción física con comprobante firmado y sincronizado vía outbox.

---

## 2. Entornos, accesos y enlaces operativos

| Recurso | Dirección / Identificador | Notas de referencia |
|---|---|---|
| Repositorio oficial | `https://github.com/bryancito1090/istpetdev-platform` | Rama base: `develop` |
| Bóveda documental | `docs/boveda/` dentro del repositorio | [[Hechos canonicos]] |
| Plataforma Web (Demo/Prod) | `https://istpetdev.doicela.dev` | Angular 22, Arquetipo B |
| PWA Móvil (Campo) | `https://istpetdev.doicela.dev/campo/` | Ionic 8, IndexedDB offline |
| Automatización n8n | `https://n8n.bryan-bano.com/` | Servidor de workflows |
| Autenticación Cognito | User Pool `us-east-1_4Y2j5DINp` | OIDC Authorization Code + PKCE |
| Base de datos compartida | Amazon RDS PostgreSQL 16 + PostGIS 3.4 | Acceso vía túnel SSM |

### Cuentas y roles configurados para la demo
Las credenciales de acceso se distribuyen por canal privado seguro del equipo:
- **Gerencia:** Tablero directivo, visualización de indicadores y gestión de usuarios.
- **Planificador:** Matriz de riesgo, generación y publicación de planes de ruta a conductores.
- **Bodega:** Preparación de carga, confirmación de despacho e impresión de etiqueta QR física.
- **Conductor:** Consulta de entregas asignadas en ruta, escaneo y gestión de transporte.
- **Receptor:** Confirmación de recepción en punto de destino, dictamen, firma y fotografía.
- **Auditor:** Verificación de cadena de custodia, trazabilidad inmutable y simulador de 90 días.

---

## 3. Cronograma del evento y momentos de control

### Día 1 — Viernes 16 de octubre de 2026
- **08:00 – 09:00:** Acreditación de participantes y armado de mesa técnica.
- **09:00 – 09:30:** Sesión de apertura y directrices del evento.
- **09:30 – 10:00:** Presentación final de áreas y retos.
- **10:00 – 11:00:** Comprensión del problema y validación de contexto con organizadores.
- **11:30 – 13:00:** Sesión de ideación y afinamiento de alcance.
- **14:00 – 16:00:** Diseño de solución, arquitectura y propuesta de valor.
- **16:00 – 16:30:** **Mentoría 1** — Primera revisión obligatoria con mentores del sector.
- **16:30 – 18:30:** Prototipado, ensayos de simulación y calibración de demo.
- **18:30 – 19:00:** Checkpoint de avance del primer día.

### Día 2 — Sábado 17 de octubre de 2026
- **08:30 – 09:00:** Reingreso y sincronización del equipo.
- **09:00 – 10:30:** Desarrollo y ajustes técnicos de integración.
- **11:00 – 12:30:** Validación, recopilación de evidencias y ensayos de campo.
- **12:30 – 13:15:** **Mentoría 2** — Revisión final técnica y de negocio.
- **14:00 – 15:00:** Consolidación de viabilidad, costos y métricas de impacto.
- **15:00 – 16:00:** Ensayo general del Pitch y sincronización de roles en la escena.
- **15:45:** **Congelamiento interno:** Cierre de código y generación de versiones finales.
- **16:00:** **Entrega formal:** Subida de entregables a plataforma oficial.
- **16:15 – 18:15:** **Pitch y demostración en vivo** ante jurado evaluador (3 a 5 minutos).
- **18:15 – 18:45:** Deliberación del jurado y anuncio de proyectos premiados.

---

## 4. Reglas de oro durante las jornadas

1. **Gestión de Git:** Todo cambio se realiza en ramas de trabajo (`feature/*`, `fix/*`, `chore/*`, `docs/*`) originadas en `develop` y se integra exclusivamente mediante Pull Request. Nunca hacer push directo a `develop` ni a `main`.
2. **Seguridad estricta:** No subir credenciales, tokens personales, archivos `.env` ni claves privadas al repositorio.
3. **Verificación documental:** Antes de abrir un PR que toque `docs/boveda/`, ejecutar:
   ```bash
   python docs/boveda/.github/scripts/check_vault.py
   ```
4. **Veracidad y evidencias:** No inventar cifras, pruebas ni resultados no medidos. La evidencia manda.
5. **Plan de contingencia de conectividad:** En caso de corte o inestabilidad de red en la sede, recurrir inmediatamente a conexión por Hotspot móvil compartido o ejecutar el Plan B local standalone ([[Runbook de DevOps y base de datos para la demo]]).

---

## 5. Ficha individual del participante

*(Completar con los datos propios de cada integrante)*

### 5.1 Datos de contacto e identificación
- **Nombre completo:**
- **Cédula de identidad:**
- **Usuario de GitHub:**
- **Correo de contacto:**
- **Teléfono móvil / WhatsApp:**
- **Especialidad técnica principal en el evento:**

### 5.2 Equipamiento y conectividad en sede
- **Portátil de trabajo (Marca, modelo, SO):**
- **Dispositivo móvil para pruebas de PWA (Modelo, Android/iOS):**
- **Conectividad primaria utilizada (WiFi CEQ / Hotspot propio):**
- **Estado de compilación local (.NET / Web / Mobile):** [ ] Compila [ ] Pruebas en verde
- **Conexión a persistencia comprobada:** [ ] Local Docker [ ] Dev privada [ ] RDS

### 5.3 Rol en la escena de la Demo y Pitch
Consultar la coreografía descrita en [[Guion del pitch]] y [[Flujos de prueba]]:
- **Papel asignado en vivo:** *(Ejemplo: Expositor principal / Gerencia Web / Bodega QR / Conductor móvil / Receptor móvil / Soporte técnico)*
- **Dispositivo o pantalla física a cargo durante la presentación:**
- **Plan de relevo o contingencia si el dispositivo falla:**

---

## 6. Registro individual de mentorías y consultas

| Hora | Mentor / Organización | Pregunta o tema consultado | Recomendación recibida | Acción aplicada en el proyecto |
|---|---|---|---|---|
| | | | | |
| | | | | |

---

## 7. Bitácora de trabajo personal (16 y 17 de octubre)

| Jornada / Bloque | Tarea / Issue / Spec abordado | Rama y commit(s) | Evidencia o resultado | Dependencia / Bloqueo |
|---|---|---|---|---|
| Día 1 — Mañana | | | | |
| Día 1 — Tarde | | | | |
| Día 1 — Cierre | | | | |
| Día 2 — Mañana | | | | |
| Día 2 — Tarde | | | | |

---

## 8. Notas de campo y observaciones rápidas

- **Observaciones del jurado o visitantes en el stand:**
- **Preguntas frecuentes recibidas durante ensayos:**
- **Mejoras identificadas para la fase de pitch:**
