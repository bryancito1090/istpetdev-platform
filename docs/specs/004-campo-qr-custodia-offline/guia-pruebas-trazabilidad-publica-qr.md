# Guía Operativa de Pruebas: Trazabilidad Pública Segura (QR)

> **Módulo:** Campo, Custodia Offline y Trazabilidad (Spec 004 / Issue #32)  
> **Requisitos:** FR-02, FR-13, FR-14, PA-05  
> **Audiencia:** Desarrolladores, Evaluadores, Jurados del Pitch y Personal de Bodega  

---

## 1. ¿Para qué se hizo este cambio? (Contexto y Problema Resuelto)

### El Problema Anterior (Vulnerabilidad de Privacidad)
Antes de este fix, el endpoint `GET /api/v1/public/trace/{unitId}` era completamente anónimo y no exigía ningún token. Cualquier persona o actor malicioso que adivinara o conociera un identificador de entrega (ej. `CAJA-001` o `HU-EC-2026-001`) podía consultar los datos de la carga sin control ni autorización. Además, el servicio respondía con datos simulados fijos de demostración y buscaba órdenes de entrega completas (`Delivery`) en lugar de unidades de manipulación físicas (`HandlingUnit`).

### La Solución Implementada (Protocolo Seguro QR)
1. **Privacidad Médica (ADR-10 / HIPAA):** El paciente o receptor del bulto no debe ver fotos, firmas, datos de clientes ni cantidades exactas de inventario. Solo debe ver la trazabilidad física: estado de custodia, insumos contenidos (SKU y unidad base) y los hitos con fecha/hora oficial del servidor.
2. **Token Efímero en el Fragmento `#t=` (S-07):** Cuando se imprime la etiqueta física del bulto, el código QR contiene una URL con un token aleatorio en el **fragmento hash**:
   ```text
   http://localhost:4200/public-trace/{unitId}#t={token}
   ```
   * **¿Por qué en el fragmento `#`?** Porque los estándares de Internet establecen que los navegadores **nunca envían el fragmento al servidor**. Esto evita que el token quede registrado en los logs de acceso de Nginx, proxies o historiales del servidor.
3. **Extracción y Sanitización Inmediata en el Cliente:**
   * La aplicación web Angular lee el parámetro `:unitId` y extrae el token del fragmento `#t=...`.
   * **Inmediatamente** ejecuta `window.history.replaceState` para borrar el token de la barra de direcciones del navegador. Así, si alguien mira la pantalla o comparte la URL, el secreto ya no está expuesto.
4. **Envío por Cabecera Segura:**
   * La web envía el token a la API dentro de la cabecera HTTP `X-Trace-Token`.
5. **Validación Criptográfica en el Backend:**
   * El token se transforma a hash SHA-256 y se compara en **tiempo constante** (`CryptographicOperations.FixedTimeEquals`) contra la base de datos para prevenir ataques de temporización.
   * Si el token es alterado, está vencido (vence a los 7 días), o fue revocado, la API responde un **404 Not Found idéntico al de una unidad inexistente**, impidiendo deducir si el bulto existe.
6. **Protección contra Abuso (Rate Limiting PA-05):**
   * Se limita la frecuencia de consultas por token para evitar ataques de denegación de servicio o fuerza bruta, permitiendo holgadamente escaneos simultáneos del jurado (HTTP 429 si se excede).

---

## 2. Preparación del Entorno Local

Para probarlo localmente necesitas levantar el backend y el frontend:

### Terminal 1: Iniciar API .NET
```powershell
npm run dev:api
```
* La API estará corriendo en `http://localhost:5000` (o `https://localhost:5001`).

### Terminal 2: Iniciar Web Angular
```powershell
npm run dev:web
```
* La web estará disponible en `http://localhost:4200`.

---

## 3. Escenarios de Prueba en el Frontend (Paso a Paso)

### Escenario A: Simulación de Escaneo de QR Físico (Flujo Real de la Demo)

Este es el flujo que experimenta el jurado o cualquier persona que escanea con su celular la etiqueta física pegada a una caja de insumos:

1. **Abre tu navegador** y pega una URL que simule el escaneo de un código QR:
   ```text
   http://localhost:4200/public-trace/CAJA-001#t=tok-demo-hu-001
   ```
2. **Observa la barra de direcciones de inmediato:**
   * **Resultado esperado:** En menos de 1 segundo, la URL en el navegador pasa automáticamente a:
     ```text
     http://localhost:4200/public-trace/CAJA-001
     ```
     El fragmento `#t=tok-demo-hu-001` desaparece de la vista gracias a `history.replaceState`.
3. **Observa la pantalla:**
   * El formulario se autocompleta con el código de la unidad y el token extraído.
   * La pantalla se dispara automáticamente y muestra el certificado de custodia:
     * **Identificador Verificado:** Código de bulto consultado.
     * **Estado de Custodia:** Badge verde (ej. `Preparada`, `Despachada` o `Recibida`).
     * **Historial de Eventos:** Hitos del servidor con fecha y hora.
     * **Insumos Verificados:** Listado de productos (SKU y unidad).
     * **Pie de Privacidad:** Aviso de verificación criptográfica sin exposición de pacientes ni actores.

---

### Escenario B: Navegación Manual desde la Aplicación Web

1. **Ingreso a la plataforma:**
   * Ve a `http://localhost:4200`.
   * En la barra de navegación superior (Header) haz clic en el enlace **"Trazabilidad QR"** (o en la pantalla de Login, haz clic en el botón inferior *"Consulta pública de bulto"*).
   * Te llevará a `http://localhost:4200/public-trace`.
2. **Formulario en blanco:**
   * Verás dos campos de texto:
     * **Campo 1:** *Código o ID de Unidad de Manipulación (HU)*.
     * **Campo 2:** *Token Secreto de Traza (de la URL QR #t=...)*.
3. **Prueba con datos válidos:**
   * Ingresa en Código: `CAJA-001` (o el código generado al preparar bultos en Bodega).
   * Ingresa en Token: El token asignado a esa caja.
   * Haz clic en el botón azul **"Verificar"**.
   * Se cargará el certificado verde de trazabilidad pública verificada.

---

### Escenario C: Token Alterado o Inválido (Criterio de Aceptación 1)

1. En el formulario de `http://localhost:4200/public-trace`:
   * Ingresa en Código: `CAJA-001`.
   * Ingresa en Token: `token-inventado-o-adivinado-xyz`.
2. Haz clic en **"Verificar"** (o navega a `http://localhost:4200/public-trace/CAJA-001#t=token-falso`).
3. **Resultado esperado:**
   * Se muestra una alerta roja:
     > **Unidad no encontrada**  
     > *El token de traza no es válido, ha sido revocado, ha vencido o la unidad no existe en el sistema.*
   * **Seguridad:** La respuesta HTTP es un código `404 Not Found`. El sistema **no revela** si la caja `CAJA-001` existe ni qué contiene.

---

### Escenario D: Token Ausente (Intento de Consulta Directa)

1. Navega directamente a `http://localhost:4200/public-trace/CAJA-001` (sin incluir `#t=...`).
2. Deja el campo *Token Secreto de Traza* en blanco y haz clic en **"Verificar"** (el botón se deshabilita si falta algún campo).
3. Si intentas consultar por API sin la cabecera `X-Trace-Token`:
   ```bash
   curl -i http://localhost:5000/api/v1/public/trace/CAJA-001
   ```
4. **Resultado esperado:**
   * La API retorna `404 Not Found`.

---

### Escenario E: Token Revocado (Reimpresión de Etiqueta o Incidente)

Cuando Bodega reimprime una etiqueta o revoca un enlace QR:
1. El token previo queda con fecha de revocación en la base de datos (`RevokedAtUtc`).
2. Al ingresar el token revocado en el formulario:
   * **Resultado esperado:** Retorna el mismo mensaje uniforme `404 Not Found`. El enlace antiguo queda inmediatamente inutilizado.

---

### Escenario F: Límite de Consultas / Rate Limiting (Criterio de Aceptación 5 / PA-05)

1. Si un bot o usuario malicioso intenta realizar más de 60 consultas en menos de un minuto con el mismo token:
2. **Resultado esperado:**
   * La API responde `429 Too Many Requests`.
   * La interfaz web muestra una alerta amarilla especial:
     > **Límite de Consultas Excedido**  
     > *Se ha superado el número de consultas públicas permitidas para este token. Por favor espere unos segundos e intente de nuevo.*

---

## 4. Matriz de Resultados Esperados

| Prueba | Entrada | HTTP API | Interfaz Visual | Cumplimiento |
| :--- | :--- | :---: | :--- | :---: |
| **Token Válido** | URL con `#t=<token-valido>` | `200 OK` | Certificado Verde con hitos de custodia e insumos | FR-02, FR-13 |
| **Limpieza de URL** | Fragmento en `#t=...` | — | La barra de direcciones borra `#t=...` de inmediato | FR-14, S-07 |
| **Token Falso** | Token alterado | `404 Not Found` | Alerta roja «Unidad no encontrada» | FR-13, Escenario 2 |
| **Token Revocado** | Token anulado por Bodega | `404 Not Found` | Alerta roja «Unidad no encontrada» | FR-02, FR-13 |
| **Sin Token** | Consulta directa sin cabecera | `404 Not Found` | Botón deshabilitado / Alerta de token requerido | FR-13 |
| **Token en Query** | `GET /trace/ID?token=...` | `404 Not Found` | Bloqueado explícitamente en el controlador | FR-13 |
| **Exceso de Tasa** | > 60 consultas / minuto | `429 Too Many` | Alerta amarilla «Límite de Consultas Excedido» | PA-05 |
