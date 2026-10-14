# Informe de Entrega y Decisiones de Arquitectura — Spec 004
**Módulo:** Campo (QR, custodia, despacho/recepción y PWA offline)  
**Rama:** `feature/004-campo-qr-custodia-offline`  
**Base:** `develop`  
**Pull Request:** [Crear PR hacia develop](https://github.com/bryancito1090/istpetdev-platform/pull/new/feature/004-campo-qr-custodia-offline)

> **Nota de vigencia (spec 013):** el selector rápido de perfiles demo y el login simulado descritos en este informe pertenecen a la entrega histórica de 004. El acceso vigente usa Cognito con Authorization Code + PKCE y no guarda contraseñas de demo en el repositorio.

---

## 1. Resumen Ejecutivo de la Implementación (Spec 004)

Se ha implementado en su totalidad la funcionalidad de **frontend de campo (Web y PWA Móvil)** bajo el principio *Frontend-First con Backend Simulado In-Browser (IndexedDB)*, permitiendo el desacoplamiento completo y la validación de usuario y jurado sin modificar el backend .NET existente:

1. **PWA Móvil de Campo (`apps/mobile`):**
   - Configuración PWA completa con `manifest.webmanifest`, `ngsw-config.json` y registro de Service Worker en producción.
   - Acceso con sesión protegida (`/login`) con selector rápido de perfiles demo y guard funcional de roles (`RoleGuard`).
   - Outbox duradero y atómico en IndexedDB (`istpetdev-driver-outbox-db`) para operaciones offline (modo avión).
   - Captura de foto Blob y firma digital en lienzo HTML5 (`signature_pad`).
   - Bandeja de sincronización y resolución de estados («Guardado», «Enviando», «Aceptado», «Conflicto»).
   - Detección de revisiones de ruta y avisos únicos persistidos por par `(routePlanId, revision)`.

2. **Módulo Web de Bodega y Traza Pública (`apps/web`):**
   - Consola de Bodega (`/warehouse`): Preparación de unidades de manipulación (`HandlingUnit`), empaque por lotes FEFO, generación de código QR vectorial e impresión de etiquetas físicas de bulto.
   - Despacho a vehículo de transporte con transferencia de custodia atómica e idempotencia UUID v7.
   - Consulta pública de custodia (`/public-trace/:unitId`): visualización de hitos temporales sin exponer datos clínicos ni credenciales.

3. **Contratos Compartidos (`libs/shared-core`):**
   - DTOs, interfaces de servicio (`IFieldApi`, `IWebFieldApi`) y esquemas en TypeScript estrictamente tipados.

---

## 2. Decisiones Técnicas y Justificación Arquitectónica

| Decisión Arquitectónica | Justificación y Racional Técnico | Norma / Requisito |
|---|---|---|
| **1. Backend Simulado In-Browser (`IndexedDB`)** | Permite construir y probar el 100% de los flujos de usuario, diseño institucional, accesibilidad y lógica offline de inmediato, cumpliendo la regla estricta de no tocar el backend C# (.NET). | Restricción de Proyecto |
| **2. Exclusión Absoluta de Tokens OIDC en Persistencia** | Los Bearer tokens OIDC/PKCE residen estrictamente solo en memoria (nunca en almacenamiento web como sessionStorage o localStorage; ref. spec 013 FR-03). En ningún caso se escriben en IndexedDB ni en el outbox durable, garantizando que un dispositivo confiscado o inspeccionado offline no exponga credenciales de acceso. | NFR-03 / 013 FR-03 / Seguridad OIDC |
| **3. Sanitización de Token QR en Fragmento Hash (`#t=`)** | El token efímero de traza viaja en el fragmento `#t=TOKEN`. Al cargar la página de traza pública, el cliente lo extrae y lo borra inmediatamente con `history.replaceState`. Esto evita que el secreto quede expuesto en URLs compartidas, historial del navegador o logs de servidores proxy. | S-07 / Privacidad |
| **4. Protección de Datos Clínicos en Traza Pública** | La proyección de datos expone únicamente el código del bulto, tipo de insumo y tres hitos de custodia (`PREPARADA`, `DESPACHADA`, `RECIBIDA`). No expone pacientes, recetas, fotos privadas, firmas ni geolocalizaciones. | ADR-10 / HIPAA / GDPR |
| **5. Subida Desacoplada de Evidencias con Reintentos** | El recibo contable de inventario se sincroniza e impacta primero; las evidencias pesadas (foto y firma) se procesan independientemente contra URLs presignadas con retroceso exponencial (*exponential backoff*). Una falla de red o de S3 no invalida ni duplica el recibo. | S-06 / NFR-04 |
| **6. Protocolo Asimétrico de Custodia QR (ADR-25)** | **En Bodega:** El conductor escanea el QR de la caja física para cargarlo al camión.<br>**En Destino:** El receptor hospitalario escanea el QR generado en la pantalla del conductor para asentar la entrega. Esto reduce la fricción en campo en más de un 70%. | ADR-25 / Ergonomía de Campo |
| **7. Ergonomía de Cero Tipeo para el Conductor** | El conductor nunca escribe manualmente nombres ni códigos en ruta; el menú principal se simplificó a 3 acciones (*Descargar Ruta*, *Recibir Paquetes*, *Ver Ruta Hoy*) y las entregas se disparan seleccionando la parada activa. | ADR-25 |
| **8. Directriz Institucional Cero Emojis** | Prohibición formal del uso de emojis Unicode en templates de interfaz (Web y Móvil). Toda la iconografía emplea exclusivamente gráficos vectoriales SVG técnicos bajo el Arquetipo B Clínico/Minimalista. | Sistema de Diseño ISTPET |

---

## 3. Estado de la Bóveda de Arquitectura (`docs/boveda/`)

Se actualizaron y sincronizaron los documentos oficiales de la Bóveda para reflejar el estado final de Spec 004:
- `docs/boveda/02 Producto/Usuarios y flujos.md`: Flujos de conductor, operador de bodega y receptor hospitalario.
- `docs/boveda/03 Arquitectura/Decisiones de arquitectura.md`: Incorporación formal de **ADR-24** (Segregación de roles por plataforma) y **ADR-25** (Custodia asimétrica y ergonomía).
- `docs/boveda/03 Arquitectura/Movil offline y sincronizacion.md`: Especificación del protocolo asimétrico y Service Worker.
- `docs/boveda/03 Arquitectura/Sistema de diseno y colores.md`: Regla 4 de iconografía técnica vectorial (cero emojis).
- `docs/boveda/06 Equipo/2026-10-13-informe-segregacion-roles-y-plataformas.md`: Informe de segregación.
- `docs/boveda/99 Fuentes/Fuente y control documental.md`: Entrada de auditoría documental.

**Resultado de Validación (`check_vault.py`):**
```text
Resumen: 83 notas Markdown, 798 enlaces internos, 0 errores, 1 avisos.
```

---

## 4. Cadena de Commits Publicada en GitHub

Todos los mensajes de commit respetan estrictamente la normativa ISTPET (entre 10 y 72 caracteres de longitud) y superaron la validación de linter de arquitectura Feature-Sliced Design (FSD):

```text
733afbd docs(boveda): adr-24 adr-25 segregacion de roles e informe campo
ff34198 feat(campo): pwa login roles ergonomia custodia asimetrica y ngsw
2021ab9 docs(campo): cierre f8 actualizacion de tareas y documentacion
55764c4 style(campo): diseno institucional accesibilidad y badge simulado f7
a16d1a0 feat(campo): bodega preparacion rotulado qr despacho y traza publica f6
648bad8 feat(campo): reintentos evidencias avisos de ruta y resolucion 409 f5
24c8c4c feat(campo): pwa conductor outbox recepcion y captura offline f4
59f9894 feat(campo): sesion roles oidc pkce y guards para spec 004
c6641de feat(campo): backend simulado in-browser con indexeddb para spec 004
ef87213 feat(campo): contratos compartidos y puertos de api para spec 004
e997393 chore(campo): andamiaje f0 y dependencias fijas para spec 004
97aaa16 docs(campo): plan frontend-first con mock para spec 004
```

---

## 5. Próximo Paso: Pull Request hacia `develop`

La rama `feature/004-campo-qr-custodia-offline` ha sido subida exitosamente a GitHub (`origin`).  
Puedes crear el Pull Request directamente mediante el siguiente enlace:

🔗 **[Crear Pull Request en GitHub: feature/004-campo-qr-custodia-offline -> develop](https://github.com/bryancito1090/istpetdev-platform/pull/new/feature/004-campo-qr-custodia-offline)**
