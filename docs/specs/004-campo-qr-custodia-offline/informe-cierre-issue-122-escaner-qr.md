# Informe de Cierre — Issue #122: Escáner QR con Cámara para Todos los Roles de la PWA

- **Issue:** [#122](https://github.com/bryancito1090/glaucus/issues/122) (`feat(campo): escáner QR con cámara para todos los roles de la PWA`)
- **Fecha:** 15 de octubre de 2026
- **Responsable:** Anthony Pilatasig (`@AnthonyPilatasig`)
- **Coordinación:** Joseph Baño (Andrés), Alexander Martínez, Bryan
- **Plataforma:** PWA de Campo (`apps/mobile`) — Angular 22 Standalone, Ionic 8, Tailwind CSS, Vite/Vitest
- **Estado:** ✅ Resuelto y verificado con compilación y pruebas al 100%

---

## 1. Problema Solventado

Previo a esta entrega, la PWA carecía de un escáner óptico real en hardware. Los botones de acción de los tres roles de campo (Receptor, Bodega y Conductor) dirigían a `apps/mobile/src/app/field/pages/scan/scan.page.ts`, el cual era únicamente un campo de texto con la etiqueta «Modo simulado activo · Fase 4 Demo» y atajos preprogramados (`hu-001`, `hu-002`, `del-001`) inexistentes en la base de datos real.

Asimismo, el backend genera etiquetas QR con URLs canónicas:
$$\texttt{https://istpetdev.doicela.dev/public-trace/\{unitId\}\#t=\{token\}}$$
La validación previa comparaba directamente contra códigos de prueba, rompiendo la compatibilidad con cualquier QR emitido por la API de `FieldService` (`POST /deliveries/{id}/handling-units`).

---

## 2. Arquitectura de la Solución

```mermaid
flowchart TD
    subgraph UI["apps/mobile/src/app"]
        SP["scan.page.ts (ScanPage)"] --> QR["shared/qr-scanner.component.ts"]
        SP --> UQR["field/unit-qr.ts (parseUnitQr / extractUnitId)"]
    end

    subgraph Hardware["Sensores y Decodificación"]
        QR --> CAM["navigator.mediaDevices.getUserMedia"]
        CAM --> BD{"BarcodeDetector nativo?"}
        BD -->|Sí (Chrome Android)| NATIVE["API nativa BarcodeDetector"]
        BD -->|No (Safari iOS)| JSQR["jsQR (Canvas 2D offscreen)"]
    end

    subgraph Router["Navegación Segregada por Rol"]
        SP -->|Conductor| DRV["Validar en outbox activeAssignment() → /receipt/:id"]
        SP -->|Receptor| REC["state: { receiverQr: { unitId, token } } → /home"]
        SP -->|Bodega| WH["state: { warehouseQr: { unitId } } → /home"]
    end
```

### 2.1 Componente Reutilizable: `QrScannerComponent`
- **Ubicación:** `apps/mobile/src/app/shared/qr-scanner/qr-scanner.component.ts`
- **Mecanismo:**
  1. Acceso a cámara trasera vía `getUserMedia({ video: { facingMode: 'environment' } })`.
  2. Compatibilidad con iOS WebKit mediante atributos `playsinline`, `muted`, `autoplay` y contexto 2D con `{ willReadFrequently: true }`.
  3. Detección híbrida: intenta primero la API nativa de alto rendimiento `BarcodeDetector` (disponible en Android Chrome) y degrada de manera transparente a `jsQR` (1.4.0) procesando fotogramas con `requestAnimationFrame`.
  4. Apagado estricto de tracks de hardware en `ngOnDestroy` y al capturar el código.
  5. Manejo accesible de permisos denegados con el mensaje oficial:
     > *"Activa el permiso de cámara en el navegador"*
  6. Feedback auditivo sutil a 880 Hz mediante Web Audio API nativa sin dependencias externas de audio.

### 2.2 Parser Puro y Seguridad: `unit-qr.ts`
- **Ubicación:** `apps/mobile/src/app/field/unit-qr.ts`
- **Funciones:**
  - `parseUnitQr(raw: string)`: Extrae `{ unitId, token }` validando la estructura de la URL de rastreo público.
  - `extractUnitId(raw: string)`: Extrae el UUID de la unidad ya sea desde una URL de traza o a partir de un UUID plano ingresado manualmente.
- **Regla de Seguridad (Spec 004 / Issue #122):** El token de acceso efímero (`#t=`) **nunca** se guarda en IndexedDB (outbox), logs de consola ni `localStorage`.

### 2.3 Refactorización de `ScanPage`
- **Ubicación:** `apps/mobile/src/app/field/pages/scan/scan.page.ts`
- **Mejoras:**
  - Retiro definitivo del banner de demo y atajos simulados.
  - Interfaz dual: Visor táctico con cámara en la sección superior y formulario de contingencia/validación manual en la inferior.
  - Resolución por rol activo (`authService.currentRole()` o query param `?mode=`):
    - **Conductor:** Valida si `unitId` pertenece a la ruta activa local (`outboxService.activeAssignment()`). Si pertenece, navega a `/receipt/{deliveryId}`; si no, informa *"Este bulto no pertenece a tu ruta"*; si no hay ruta descargada, indica *"Descarga tu ruta en Inicio"*.
    - **Receptor:** Regresa a `/home` pasando en el `state` de Angular Router `{ receiverQr: { unitId, token } }` para conectarse con la vista de recepción real de Joseph (#110).
    - **Bodega:** Regresa a `/home` pasando en el `state` `{ warehouseQr: { unitId } }` para la vista de Bodega de Alexander (#121).

---

## 3. Delimitación Estricta de Código (Cero Conflictos)

Para garantizar la no colisión con el trabajo paralelo de los compañeros antes del congelamiento de las 14:00:
- **`home.page.ts`:** **No tocado** (a cargo de Joseph en #110 y Alexander en #121).
- **`receipt.page.ts`:** **No tocado** (a cargo de Bryan).
- **Archivos bajo responsabilidad de Anthony:** `apps/mobile/src/app/field/pages/scan/scan.page.ts`, `apps/mobile/src/app/shared/qr-scanner/`, `apps/mobile/src/app/field/unit-qr.ts`.

---

## 4. Evidencias de Validación Técnica

| Comprobación | Comando | Resultado |
|---|---|---|
| **Pruebas Unitarias Móvil** | `npm run test:mobile` | **8 suites aprobadas, 41 pruebas verdes (0 fallos)** |
| **Pruebas Parser QR** | `vitest run unit-qr.spec.ts` | 7 pruebas aprobadas (URL válida, mayúsculas, sin token, UUID directo, texto inválido) |
| **Pruebas Componente Escáner** | `vitest run qr-scanner.component.spec.ts` | 3 pruebas aprobadas (inicialización, emisión `(scanned)`, apagado de tracks) |
| **Pruebas Pantalla Escáner** | `vitest run scan.page.spec.ts` | 6 pruebas aprobadas (flujos Conductor, Receptor y Bodega) |
| **Compilación PWA Producción** | `npm run build:mobile` | Generación completa en 7.0 s; chunk `scan-page` perezoso de solo 30.96 kB transferidos |
| **Validación Integral de CI** | `npm run check` | **Aprobada (0 errores, 0 advertencias)** en lint, no-mock-login, API .NET, shared-core, web, mobile y build PWA campo |
| **Base de Datos Local** | `dotnet ef database update` | Migraciones sincronizadas hasta `SimulationRunsAndEvents` en `istpetdev_dev_04` |
