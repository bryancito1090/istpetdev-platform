# Guía de Prueba de Punta a Punta: Integración 002 (Motor) y 004 (Campo)

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

Para probar el flujo completo en tu máquina, abre **tres terminales** en la raíz del proyecto:

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
* **Destinado a:** Planificación, Gerencia y Bodega de escritorio.

### Terminal 3: PWA Móvil de Campo (Ionic / Angular)
```powershell
npm run dev:mobile
```
* **URL:** `http://localhost:8100`
* **Destinado a:** Conductor y Receptor en teléfono o emulador móvil.

---

## 3. Cuentas y Roles para la Prueba

La API cuenta con un módulo de desarrollo (`DevJwtSecurity`) y cuentas precargadas en `appsettings.Development.json`:

| Rol Institucional | Usuario / Subject | Clave / Login rápido | Propósito en el flujo |
| :--- | :--- | :--- | :--- |
| **Planificador** | `planner-1` | Botón "Planificación" o `planner@istpetdev.local` | Proponer y publicar rutas con lotes FEFO |
| **Bodega** | `warehouse-1` | Botón "Bodega" o `warehouse@istpetdev.local` | Preparar caja física, emitir QR y despachar |
| **Conductor (C-01)** | `driver-1` | Botón "Conductor" o `driver@istpetdev.local` | Descargar ruta, recibir en hospital y firmar |
| **Gerencia** | `admin-1` | Botón "Gerencia" o `admin@istpetdev.local` | Supervisión general del panel |

---

## 4. Paso a Paso del Recorrido de Prueba (Dónde hacer clic)

### Paso 1: Resetear escenario y publicar la ruta (Planificador)
1. Abre tu navegador en **`http://localhost:4200`**.
2. En la pantalla de login, pulsa el botón **"Dra. Andrea Morales (Planificación)"** para iniciar sesión.
3. Dirígete a la pestaña **"Rutas"** en el menú de navegación superior.
4. Si la base está vacía o deseas el escenario fresco:
   * Abre Swagger en `http://localhost:5000/swagger` y ejecuta `POST /api/v1/demo/reset` (o haz clic en el botón de restablecer demo si está visible).
5. En la vista de **Rutas**, selecciona la propuesta de ruta generada y haz clic en el botón **"Publicar Ruta"**.
6. **¿Qué ocurrió en el backend?** 
   * Se crearon las entregas `DEL-...` con sus líneas de lote y reservas activas en `RouteStockReservation`.
   * La entrega quedó asignada al conductor `driver-1`.

---

### Paso 2: Preparar la caja y despachar (Bodega)
1. En la PWA móvil (**`http://localhost:8100`**) o en la web con rol **Bodega** (`warehouse-1`):
2. Inicia sesión como **"Elena Bodega (Operador de Almacén)"**.
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
1. En el navegador del móvil (**`http://localhost:8100`**), cierra sesión e inicia sesión como **"Carlos Mendoza (Conductor C-01)"**.
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
   * Confirma las cantidades aceptadas (p. ej. 10 L).
   * Dibuja una firma táctil en el recuadro de firma.
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
   * Se llamó a `POST /api/v1/deliveries/{id}/receipts` con el `operationId` y clave de idempotencia.
   * El stock se movió del vehículo al centro de salud.
   * Se registró el evento de custodia `Received`.
   * La entrega pasó a `Delivered`.
   * **¡La parada `RouteStop` se completó de forma real!**

---

### Paso 6: Verificación de la parada completada y QR del Jurado
1. Vuelve al panel web (**`http://localhost:4200`**) en la pestaña **"Rutas"**:
   * Observa la parada del hospital: ya no está pendiente, ahora figura con el check verde de **"Completada"** con hora de llegada real.
2. Abre la URL del QR de la caja que generó bodega:
   * Ejemplo: `http://localhost:4200/public-trace/{unitId}#t={token}`
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
