# Guía de Prueba de Punta a Punta: Integración 002 (Motor) y 004 (Campo)

> **Autenticación vigente (spec 013):** esta guía conserva el recorrido de 004, pero sus instrucciones antiguas de usuarios/botones demo quedaron retiradas. Web y PWA usan la página administrada de Cognito (Authorization Code + PKCE) con cuentas aprobadas y aprovisionadas; las contraseñas no se comparten en este documento ni se guardan en el repositorio. La API local en `Development` no valida tokens del User Pool real, así que el recorrido autenticado de punta a punta requiere una API desplegada y configurada con Cognito. No uses las instrucciones locales de abajo para validar llamadas autenticadas.

## 1. ¿Para qué se hizo esta integración? (Propósito y Contexto)

Antes de esta integración, el sistema tenía dos módulos que funcionaban en silos aislados:
1. **El Motor Logístico (002):** Calculaba rutas óptimas, asignaba vehículos y reservaba stock con FEFO, pero al momento de llegar a una parada en el hospital, la parada se marcaba como completada "a mano" con un botón simulado provisional (`POST /stops/{seq}/complete`), sin saber si realmente se entregó medicina.
2. **El Módulo de Campo (004):** Tenía una interfaz móvil y de bodega pero operaba contra una base simulada en memoria del navegador (`IndexedDB / MockFieldApiService`), sin hablar con el backend ni mover inventario real.

### El impacto de haberlos conectado
Con la integración realizada para la **Issue #33**:
* Una ruta publicada por el motor logístico (002) genera entregas (`Delivery`) con lotes reservados (`RouteStockReservation`).
* El personal de **Bodega** prepara la caja física, genera un código QR criptográfico seguro y registra el despacho, lo cual **traslada el inventario real de bodega al camión** en un commit transaccional.
* El **Conductor** descarga en su teléfono (PWA) exactamente las entregas asignadas a su vehículo y conductor.
* El conductor puede **entrar a modo avión**, entregar las medicinas en el hospital, registrar la firma del receptor y una foto, y cuando recupera señal y sincroniza:
  * El stock pasa del camión al hospital.
  * La parada de la ruta (`RouteStop`) pasa automáticamente a **`Completed`**.
  * El jurado o receptor puede escanear el QR desde cualquier teléfono ajeno y ver la traza pública verificada en tiempo real.

---

## 2. Preparación del Entorno (Cómo arrancar los servicios)

Para arrancar la API y servir las apps localmente, abre **tres terminales** en la raíz del proyecto. Esto sirve para desarrollo y para revisar el inicio de sesión en Cognito; no permite completar el flujo de API autenticado descrito abajo. Para esa prueba usa apps y API de un entorno desplegado y conectado al mismo User Pool.

### Terminal 1: Backend API (.NET 8)
```powershell
npm run dev:api
```
* **URL:** `http://localhost:5000`
* **Swagger:** `http://localhost:5000/swagger`

### Terminal 2: Web de Gestión y Operaciones (Angular)
```powershell
npm run dev:web
```
* **URL:** `http://localhost:4200`
* **Destinado a:** Gerencia, Planificador y Auditor.

### Terminal 3: PWA Móvil de Campo (Ionic / Angular)
```powershell
npm run dev:mobile
```
* **URL:** `http://localhost:8100`
* **Destinado a:** Bodega, Conductor y Receptor en teléfono o emulador móvil.

---

## 3. Cuentas y Roles para la Prueba

Usa cuentas de prueba Cognito entregadas por el equipo y aprovisionadas como cuentas activas de la API. En el acceso redirigido a Cognito, inicia sesión con la cuenta asignada al rol requerido; no hay acceso rápido ni usuarios con contraseñas locales.

| Rol Institucional | Plataforma | Identidad | Inicio de sesión | Propósito en el flujo |
| :--- | :--- | :--- | :--- | :--- |
| **Gerencia** | Web | Cuenta asignada por el equipo | Página administrada de Cognito | Supervisión general del panel |
| **Planificador** | Web | Cuenta asignada por el equipo | Página administrada de Cognito | Proponer y publicar rutas con lotes FEFO |
| **Auditor** | Web | Cuenta asignada por el equipo | Página administrada de Cognito | Consultar la operación en modo de solo lectura |
| **Bodega** | PWA | Cuenta asignada por el equipo | Página administrada de Cognito | Preparar caja física, emitir QR y despachar |
| **Conductor** | PWA | Cuenta asignada por el equipo | Página administrada de Cognito | Descargar ruta, recibir en hospital y firmar |
| **Receptor** | PWA (rol de 013; sin sesión separada en esta demo) | Receptor hospitalario | Firma en el dispositivo del Conductor asignado | Validar y firmar; el Conductor autenticado envía la recepción |

---

## 4. Paso a Paso del Recorrido de Prueba (Dónde hacer clic)

### Paso 1: Preparar el escenario y publicar la ruta (Planificador)
1. Para el recorrido integrado, abre la web del entorno desplegado que valida Cognito y está conectado a su API. `http://localhost:4200` sirve para revisar la interfaz y el retorno desde Cognito, pero su API local en `http://localhost:5000` corre en `Development` y no valida esos tokens.
2. Inicia sesión en la página administrada de Cognito con la cuenta de Planificador asignada por el equipo y vuelve a la web desplegada.
3. Dirígete a la pestaña **"Rutas"** en el menú de navegación superior.
4. Si la base está vacía o deseas el escenario fresco:
   * En el entorno de prueba, abre el Swagger de su API y ejecuta `POST /api/v1/demo/reset` solo si el reset de demo está habilitado. Si está deshabilitado, carga el fixture con el procedimiento aprobado para ese entorno. No envíes el token Cognito a Swagger de la API local.
5. En la vista de **Rutas**, selecciona la propuesta de ruta generada y haz clic en el botón **"Publicar Ruta"**.
6. **¿Qué ocurrió en el backend?** 
   * Se crearon las entregas `DEL-...` con sus líneas de lote y reservas activas en `RouteStockReservation`.
   * La entrega quedó asignada al conductor `driver-1`.

---

### Paso 2: Preparar la caja y despachar (Bodega)
1. Abre la PWA móvil del entorno configurado con Cognito y la API desplegada.
2. Cuando Cognito lo solicite, inicia sesión con la cuenta de Bodega asignada por el equipo.
3. Verás la lista de entregas pendientes de preparación:
   * Selecciona la entrega publicada.
   * Haz clic en **"Preparar Caja"**.
   * Se abrirá el modal de composición con los lotes FEFO reservados. Haz clic en **"Confirmar y Generar QR"**.
4. Se generará la unidad `HU-DEL-...` con su etiqueta imprimible y el enlace con token `#t=...`.
5. Pulsa el botón **"Confirmar Despacho"**.
6. **¿Qué ocurrió en el backend?**
   * La reserva pasó a estado `Consumed`.
   * El stock de bodega se descontó y se acreditó al vehículo en `StockBalance`.
   * Se registró el evento de custodia `Dispatched`.
   * La entrega pasó a estado `Dispatched`.

---

### Paso 3: Descargar asignación en el teléfono (Conductor)
1. En el navegador del móvil, cierra sesión e inicia sesión en Cognito con la cuenta de Conductor asignada por el equipo.
2. Al ingresar a la pantalla principal, la PWA ejecutará automáticamente `GET /api/v1/mobile/assignments`:
   * Verás tu vehículo asignado (`VEH-01`).
   * Verás la ruta del día con las paradas en los hospitales correspondientes.
   * Las cajas preparadas por bodega aparecerán listadas para cada parada.

---

### Paso 4: Simular pérdida de señal y recepción offline (Modo Avión)
1. Abre las Herramientas de Desarrollador de tu navegador (`F12`), ve a la pestaña **Network** (Red) y selecciona el selector de velocidad en **"Offline"** (simulando modo avión en la carretera).
2. En la PWA:
   * Toca en la parada correspondiente al hospital de destino.
   * Toca en **"Recibir Entrega"**.
   * Escanea o selecciona la caja `HU-...`.
   * El receptor hospitalario valida las cantidades aceptadas (p. ej. 10 L) y dibuja su firma táctil en el dispositivo del Conductor asignado.
   * El Conductor registra la recepción desde la PWA bajo su propia sesión de Cognito.
   * Toca en **"Guardar en el teléfono"**.
3. **¿Qué ocurrió en la PWA?**
   * Todo se guardó atómicamente en IndexedDB (`istpetdev-driver-outbox-db`).
   * La PWA muestra la insignia **"Guardado en el teléfono"**. Si recargas la página con F5 (reinicio), la captura y la firma permanecen intactas.

---

### Paso 5: Recuperar conexión y sincronizar
1. En las Herramientas de Desarrollador (`F12`), vuelve a poner la red en **"Online"** (o "No throttling").
2. En la PWA, ve a la pestaña de **Sincronización** (`/sync`) o pulsa el botón **"Sincronizar"**:
   * Observarás la transición visual: `"Guardado en el teléfono"` ➔ `"Enviando"` ➔ `"Aceptado por el servidor"`.
3. **¿Qué ocurrió en el backend?**
   * La PWA envía `POST /api/v1/deliveries/{id}/receipts` con la sesión del Conductor asignado, el `operationId` y la clave de idempotencia; la firma del Receptor y la foto quedan asociadas a esa recepción.
   * El stock se movió del vehículo al centro de salud.
   * Se registró el evento de custodia `Received`.
   * La entrega pasó a `Delivered`.
   * **¡La parada `RouteStop` se completó de forma real!**

---

### Paso 6: Verificación de la parada completada y QR del Jurado
1. Vuelve al panel web del mismo entorno desplegado en la pestaña **"Rutas"**:
   * Observa la parada del hospital: ya no está pendiente, ahora figura con el check verde de **"Completada"** con hora de llegada real.
2. Abre la URL del QR de la caja que generó bodega:
   * Ejemplo: `<origen-web-del-entorno>/public-trace/{unitId}#t={token}`
   * Verás la pantalla pública de auditoría:
     * Código de caja y estado `Verificada`.
     * Lista de productos (SKU y unidad base).
     * Los hitos temporales reales: `Preparada` ➔ `Despachada` ➔ `Recibida`.
     * Cero datos sensibles clínicos, sin nombres personales ni cantidades visibles (cumpliendo estrictamente el guardarraíl de privacidad).

---

## 5. Validación Rápida por Pruebas Automatizadas

Si no deseas hacer los clics manuales y quieres verificar que todo funciona a nivel de integración:

```powershell
# 1. Pruebas unitarias de toda la lógica de campo, seguridad y hashing de tokens
dotnet test tests/IstpetDev.Tests --filter "Category=Unit"

# 2. Verificación de compilación de producción en Frontend y Backend
npm run check
```
Ambos comandos deben reportar **0 fallos**.
