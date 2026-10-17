---
tipo: guia-capacitacion
estado: propuesta
actualizado: 2026-10-16
tags: [capacitacion, roles, conductor, demo-quito, pitch, reto-1]
---

# Hoja de capacitación operativa: Conductor (Transportista)

**Objetivo del rol:** Cumplir las paradas del recorrido entregando cada bulto a tiempo, registrando la entrega con un toque en el celular y continuando el viaje con total tranquilidad aunque no haya señal en la vía.

## 1. Qué aprende
- Descargar la ruta del día en el teléfono celular antes de encender el motor y salir de la bodega.
- Escanear con la cámara del celular el código QR de cada bulto al cargarlo en el camión para asumir la custodia formal de la carga.
- Seguir el orden de paradas respetando los horarios de llegada de los clientes (llegar temprano a hospitales y centros con horario de descarga restringido).
- Registrar la entrega en el destino escaneando el bulto frente al cliente.
- Registrar y comunicar novedades viales (tráfico severo, calles cerradas o averías) para que la base coordine el apoyo.
- Trabajar en modo sin conexión durante todo el trayecto sin que se pierdan los datos de las entregas.

## 2. Duración
- **30 minutos** de práctica directa utilizando su propio teléfono inteligente.

## 3. Ejercicio práctico con el escenario Quito (`demo-quito-planificacion`)
- **Vehículo asignado:** Hino Dutro City, placa `PCB-2214` (`VEH-0001`, diésel).
- **Ruta asignada:** Salida desde Bodega Central Sur - Turubamba (`BOD-0001`).
- **Paradas del recorrido:**
  1. Hospital General Enrique Garcés (`PTO-0005`), ventana de atención 07:00–11:00.
  2. Centro Comercial Quicentro Norte (`PTO-0013`), ventana de descarga 07:00–09:30.
- **Paso a paso en la aplicación móvil (módulo de Conductor):**
  1. Abrir la aplicación y presionar el botón **«Descargar Ruta»**: los datos del viaje se guardan en el teléfono.
  2. En la rampa de carga, pulsar **«Escanear QR»** y apuntar a los bultos rotulados por bodega. Al terminar la carga, confirmar la salida.
  3. Al salir hacia la Av. Pedro Vicente Maldonado se experimenta congestión matutina (`congestion-hora-pico-maldonado`): verificar la hora estimada de llegada a la primera parada.
  4. Activar el **Modo Avión** en el teléfono móvil para simular la pérdida de cobertura en la zona de Quitumbe.
  5. Llegar al Hospital Enrique Garcés: abrir la parada correspondiente y pulsar **«Escanear QR»** para presentar la entrega al receptor.
  6. Comprobar que la entrega queda registrada con éxito y se acumula en la bandeja de salida local (indicador de entregas pendientes en la barra superior).
  7. Desactivar el Modo Avión: observar cómo la aplicación envía los datos guardados al servidor de forma transparente al recuperar la red.

*(Nota: Tiempos y distancias corresponden a un ejemplo ilustrativo del escenario de prueba).*

## 4. Cómo se comprueba que aprendió
- El conductor descarga su ruta, activa el modo avión en su equipo, completa un escaneo de entrega de prueba y comprueba que la información se sincroniza automáticamente al restablecerse la señal.
- Explica qué hacer en caso de que una calle esté cerrada o el vehículo presente un problema mecánico.

## 5. Referente de dudas por turno
- **En ruta y tráfico diario:** Monitor de Tráfico y Despacho (canal de radio o teléfono de guardia).
- **Auxilio mecánico en carretera:** Supervisor de Flota y Mantenimiento.
