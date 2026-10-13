---
tipo: informe
estado: vigente
actualizado: 2026-10-13
tags: [arquitectura, roles, rbac, frontend, mobile, adr-24, entrega]
---

# Informe Técnico: Segregación Canónica de Roles por Plataforma y Rediseño de Flujos Operativos

## 1. Resumen Ejecutivo

Durante la fase de revisión y preparación para la presentación y despliegue del sistema **IstpetDev Platform**, se detectaron inconsistencias entre la arquitectura de roles documentada en el modelo de dominio (`Roles.cs`, ADR-14/16) y la exposición de interfaces de usuario tanto en la plataforma web de escritorio (`apps/web`) como en la aplicación móvil de campo (`apps/mobile`).

Específicamente:
1. La plataforma web exponía un acceso público directo al panel operativo ("Ingresar al panel") antes de autenticarse, comprometiendo la privacidad de la Torre de Control.
2. Existía una sección de "Bodega y Despacho" en la web de escritorio, lo cual es inviable en la operación física de almacenes, donde los operadores se desplazan entre perchas y rampas con terminales móviles.
3. El perfil del **Receptor Hospitalario** no estaba diferenciado en el móvil y carecía del flujo ergonómico requerido en los puntos de entrega: escanear el bulto y declarar de forma binaria si la entrega se recibió conforme o si ocurrió alguna novedad o daño físico.
4. Existía confusión sobre el rol de **Automatización**, el cual no requiere ni debe tener una interfaz gráfica para usuarios humanos.

Este informe documenta las decisiones técnicas adoptadas, su justificación arquitectónica ("el por qué"), los archivos modificados y las pruebas de compilación que avalan la estabilidad de la solución.

---

## 2. El "Por Qué" de Cada Decisión

### 2.1. Eliminación del botón "Ingresar al panel" en el Landing Público
- **Decisión:** En la portada pública (`/`), se eliminó el botón "Ingresar al panel" de la cabecera y el enlace directo al panel del héroe, dejando exclusivamente el botón institucional **"Iniciar sesión"** (`/login`). Además, se ocultó la barra de navegación del sistema y el banner de datos simulados en la página de aterrizaje.
- **¿Por qué?**:
  - **Seguridad y Confidencialidad:** Una plataforma de logística hospitalaria no debe exponer accesos directos ni menús de inventarios a usuarios no autenticados.
  - **Principio de Mínima Sorpresa (POLA):** El usuario que ingresa a la URL raíz debe percibir un portal institucional seguro y ser conducido formalmente por el flujo de autenticación OIDC/PKCE.

### 2.2. Exclusión de Bodega de la Web y Asignación Exclusiva a Móvil
- **Decisión:** Se retiró el enlace de navegación y las acciones de bodega de la consola web (`apps/web`) y se concentraron exclusivamente en la PWA móvil (`apps/mobile`).
- **¿Por qué?**:
  - **Ergonomía Operativa Real:** En un almacén central de medicamentos y químicos de limpieza hospitalaria, el personal no opera sentado frente a una computadora de escritorio para hacer picking o cargar camiones; requiere un dispositivo móvil de mano con lector de códigos de barras/QR para validar bultos al pie del pallet o del furgón.
  - **Soporte Offline en Bodega:** Frecuentemente los galpones y patios de carga sufren de zonas muertas de señal Wi-Fi/4G. La app móvil cuenta con motor de sincronización offline mediante cola outbox en IndexedDB (`idb`), garantizando que ningún despacho se bloquee por falta de cobertura.

### 2.3. Delimitación de la Torre de Control Web para Planificador, Gerencia y Auditor
- **Decisión:** La web se restringió a roles de supervisión estratégica: Planificador (`SUPERVISOR`), Gerencia (`ADMIN`) y Auditor (`AUDITOR`).
- **¿Por qué?**:
  - **Visualización Compleja de Datos:** El cálculo de quiebre de stock a 5 días y la visualización de rutas VRP sobre capas cartográficas Leaflet/PostGIS requieren pantallas de alta resolución para un análisis cómodo de múltiples variables simultáneas.
  - **Separación de Responsabilidades (SoD):** Quien planifica y aprueba las rutas no debe ser quien captura físicamente la entrega en el hospital.

### 2.4. Flujo Ultrarrápido del Receptor Hospitalario en Móvil
- **Decisión:** Se creó el perfil demo **Lic. Marco Receptor** en la PWA móvil con una interfaz enfocada exclusivamente en:
  1. Escanear el QR del bulto recibido (`HU-XXXX`).
  2. Dictaminar de forma inmediata: **✓ Todo Conforme** o **⚠️ Pasó algo malo**.
  3. Si pasó algo malo: marcar chips rápidos (*Empaque roto o golpeado*, *Sello de seguridad roto / alterado*, *Faltan galones / unidades*, *Lote o fecha no coincide*) y agregar observaciones.
  4. Confirmar y generar acta digital de recepción (`REC-2026-XXXX`).
- **¿Por qué?**:
  - **Fricción Mínima en Farmacia Hospitalaria:** Un químico farmacéutico o enfermero que recibe insumos médicos en emergencia o bodega hospitalaria no tiene tiempo para llenar formularios complejos de 20 campos.
  - **Trazabilidad de Cadena de Custodia (Spec 004):** Si el bulto tiene el precinto roto o se derramó producto durante el transporte, el sistema debe capturar el incidente en el segundo exacto de la transferencia de custodia, protegiendo tanto al hospital como al transportista ante sanciones de SERCOP.

### 2.5. Naturaleza del Rol de Automatización (Sin UI)
- **Decisión:** Se ratificó que el rol `Automatizacion` no tiene pantallas ni vistas en web ni en móvil.
- **¿Por qué?**:
  - **Identidad Técnica M2M:** La automatización es un daemon/worker en segundo plano (orquestador de eventos, cálculos de demanda y motor de reglas determinista/IA). Su función es procesar matrices y enviar avisos a conductores (*DriverNotice*) o alertas a planificadores, no interactuar visualmente.

---

## 3. Matriz Canónica de Roles y Plataformas

| Rol en Dominio | Plataforma Asignada | Tipo de Interfaz | Capacidades Principales |
| :--- | :---: | :---: | :--- |
| **Planificador** | 🖥️ **Web** (`apps/web`) | Torre de Control Operativa | Monitoreo predictivo de inventario (matriz 5 días), optimización de rutas VRP PostGIS, despacho preventivo. |
| **Gerencia** | 🖥️ **Web** (`apps/web`) | Panel Ejecutivo & Gobierno | Visión macro de abastecimiento, KPIs de cumplimiento, auditoría global y reportes directivos. |
| **Auditor** | 🖥️ **Web** (`apps/web`) | Auditoría y Trazabilidad | Consulta forense de bultos HU, eventos de custodia y trazabilidad QR. |
| **Bodega** | 📱 **Móvil** (`apps/mobile`) | PWA de Campo con Outbox | Picking físico, lectura QR de salida, despacho a furgones de custodia y encolado offline. |
| **Conductor** | 📱 **Móvil** (`apps/mobile`) | Navegación de Ruta de Custodia | Hoja de ruta offline, recepción de avisos de cambio (*DriverNotice*), paradas y captura de firma. |
| **Receptor** | 📱 **Móvil** (`apps/mobile`) | Lector Rápido Hospitalario | Escaneo QR de insumo recibido, verificación binaria ("Todo Conforme" vs "Pasó algo malo") y actas. |
| **Automatización** | ⚙️ **Worker** (Backend) | Daemon en Segundo Plano | Proyecciones matemáticas, evaluación de restricciones de tráfico y generación de alertas autónomas. |

---

## 4. Detalle de Archivos Modificados

### 4.1. Aplicación Web (`apps/web`)

1. **`apps/web/src/widgets/header/header.widget.ts`**:
   - Eliminado el botón "Ingresar al panel".
   - Ocultada la barra de navegación del sistema cuando la ruta es la raíz pública `/`.
   - Eliminado el enlace de "Bodega & Despacho".
   - Filtrada la barra de navegación por rol autenticado: el Auditor solo ve Trazabilidad QR y Rutas en modo lectura; el Planificador ve Stock, Rutas y Trazabilidad.

2. **`apps/web/src/pages/landing/landing.page.ts`**:
   - Modificado el botón de llamada a la acción principal del Hero de `/dashboard` a `/login` ("Iniciar sesión").

3. **`apps/web/src/app/ui/app.component.ts`**:
   - El banner superior de "Datos simulados" se oculta en la página pública `/`.
   - Reemplazado el enlace directo de bodega en el banner por "Torre de Control Web • Expo Clean 2026".

4. **`apps/web/src/features/login-form/login-form.component.ts`**:
   - Se actualizaron los perfiles rápidos del demo web a: Planificador (`SUPERVISOR`), Gerencia (`ADMIN`) y Auditor (`AUDITOR`).
   - Se añadió una nota aclaratoria informando que los roles operativos de campo (Bodega, Conductor, Receptor) deben autenticarse en la PWA móvil.

5. **`apps/web/src/pages/operations-dashboard/operations-dashboard.page.ts`**:
   - Personalizadas las cabeceras, descripciones y acciones según el rol autenticado: botones de sincronización de stock para Planificador, auditoría forense para Auditor y visión ejecutiva para Gerencia.

---

### 4.2. Aplicación Móvil (`apps/mobile`)

1. **`apps/mobile/src/app/auth/auth.models.ts`**:
   - Agregado el rol `HospitalReceiver` a `FieldUserRole`.
   - Definido el perfil demo `DEMO_RECEIVER_PROFILE`: "Lic. Marco Receptor (Demo) - Hospital Ambato".

2. **`apps/mobile/src/app/auth/auth.service.ts`**:
   - Incorporado el método `loginAsReceiver()` para iniciar sesión con credenciales volátiles de receptor hospitalario.

3. **`apps/mobile/src/app/auth/pages/login/login.page.ts`**:
   - Agregado el botón de acceso rápido de 1-clic para el perfil de Receptor Hospitalario.

4. **`apps/mobile/src/app/app.routes.ts`**:
   - Autorizado el acceso de `HospitalReceiver` a las rutas de escaneo (`/scan`) y cola de sincronización (`/sync`).

5. **`apps/mobile` (`HomePage` en `src/app/home`)**:
   - Implementado el renderizado dinámico condicional por rol:
     - **Receptor:** Interfaz de escaneo QR, visualización del bulto, selector binario de conformidad, chips de reporte de anomalías (empaque roto, precinto alterado, faltantes de unidades, lote inconsistente), observaciones libres y generación de acta `REC-2026-XXXX`.
     - **Bodega:** Módulo de preparación y verificación de salida de bultos hacia los furgones.
     - **Conductor:** Hojas de ruta, paradas asignadas, descarga offline y avisos *DriverNotice*.
   - Añadido botón de "Salir" (Logout) en el encabezado móvil para alternar cómodamente entre perfiles en la demo.

---

### 4.3. Bóveda Documental (`docs/boveda/`)

1. **`docs/boveda/02 Producto/Usuarios y flujos.md`**:
   - Actualizada la matriz de roles incorporando la columna de **Plataforma Asignada** y delimitando las responsabilidades de Bodega (Móvil), Planificador (Web), Conductor (Móvil), Receptor (Móvil), Gerencia (Web) y Automatización (Daemon).
   - Ajustados los pasos 7, 9 y 12 del **Flujo central** para reflejar la entrega y el dictamen binario de recepción en hospital.

2. **`docs/boveda/03 Arquitectura/Decisiones de arquitectura.md`**:
   - Registrada formalmente en la tabla principal y en el cuerpo del documento la decisión [[Decisiones de arquitectura#ADR-24 — Segregación de Roles por Plataforma y Flujo de Recepción Hospitalaria Simplificada|ADR-24]].

---

## 5. Pruebas y Verificación de Compilación

Ambos proyectos del monorepo fueron sometidos a compilación estricta y pruebas de linting:

1. **Compilación de la Aplicación Web (`apps/web`)**:
   ```bash
   npm run build:web
   ```
   - **Resultado:** Exitoso (código de salida 0).
   - **ESLint FSD:** Limpio (0 errores en capas Features, Widgets, Entities, Shared).
   - **Bundle Inicial:** 687.10 kB (161.73 kB transfer size estimado).

2. **Compilación de la Aplicación Móvil (`apps/mobile`)**:
   ```bash
   npm run build:mobile
   ```
   - **Resultado:** Exitoso (código de salida 0).
   - **Ionic / Angular Standalone:** Generación de bundle de producción completada en 2.6 segundos sin errores tipográficos ni de template.
   - **Bundle Inicial:** 968.29 kB (219.02 kB transfer size estimado).

---

## 6. Conclusión y Recomendación para el Pitch / Demo

Con esta segregación, el proyecto presenta una arquitectura de interfaces verosímil y alineada a las prácticas de la industria de salud y logística:
- En la laptop/proyector se expone la **Torre de Control Web** con la visión analítica (Planificador / Gerencia).
- En el teléfono celular o tablet se expone la **PWA de Campo** demostrando la agilidad del escaneo QR y la protección de la cadena de custodia tanto en el despacho de Bodega como en la recepción del Hospital.
