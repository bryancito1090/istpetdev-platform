---
tipo: demo
estado: propuesta
actualizado: 2026-10-17
tags: [demo, pitch, guion]
---

# Guion del pitch v5 — 4 minutos 40 segundos

## 00:00–00:15 · Joseph · Diapositiva 1

¿Cómo se entera hoy una empresa de limpieza de que a un hospital le falta desinfectante? Cuando el hospital llama. Ese día ya es tarde.

## 00:15–00:45 · Joseph · Diapositiva 2

Lo escuchamos ayer en la charla del sector. El planificador repone por calendario o por urgencia; bodega pierde de vista el insumo cuando sale; y nadie puede probar qué se entregó. Cuando se apura una entrega, se decide mirando kilómetros y no el costo real: personal, combustible, descarga y producto que se daña. Y el que paga es el cliente final: un hospital o una clínica sin insumos de limpieza.

## 00:45–01:15 · Joseph · Diapositiva 3

IstpetDev une eso en una sola cadena. Detecta: revisa el inventario y avisa qué punto se quedará sin producto. Decide: compara cómo atenderlo con flota propia o con un operador contratado, sumando todo lo que cuesta servir. Entrega: arma la ruta respetando capacidad, horarios y jornada. Y prueba: cada caja viaja con QR. Bodega y conductor pueden guardar registros sin señal y sincronizarlos después; la recepción final del cliente requiere conexión.

## 01:15–01:50 · Bryan · Diapositiva 4

¿Qué es lo nuevo? Que no es otro inventario con alertas: la alerta termina en una decisión con costo y en una entrega comprobable. Lo calcula un motor con reglas, siempre igual con los mismos datos. Encima trabaja un agente de inteligencia artificial, orquestado con n8n, que lee esos resultados y le explica al planificador, en lenguaje simple, por qué conviene cada opción. La IA no inventa cifras: solo explica las del motor, y si falla, hay una explicación de respaldo. La persona aprueba. Y como n8n está separado del motor, podemos cambiar de modelo o agregar automatizaciones sin tocar el cálculo.

## 01:50–01:59 · Alexander · Diapositiva 5

Así se ve: una consola reúne el estado del stock, las alertas y las rutas para revisar qué requiere atención.

## 01:59–02:08 · Alexander · Diapositiva 6

En el mapa vemos los recorridos y sus paradas. Esta vista usa un escenario sintético de Ambato, distinto de la comparación de Quito.

## 02:08–02:15 · Alexander · Diapositiva 7

Y el QR identifica cada bulto para consultar su trazabilidad. Aquí mostramos el escáner.

## 02:15–03:05 · Alexander · Diapositiva 8

Y esta es la decisión. A, flota propia: la más barata, 57,89 dólares, pero llega a tiempo solo al 83 % de los críticos. B llega al 100 %, pero cuesta un 20,7 % más. C, un operador contratado, también llega al 100 % y cuesta un 4,3 % menos que B. El motor recomienda C con 90,8 puntos y la IA explica por qué. La más barata no siempre es la mejor: pondera costo y entregas críticas a tiempo por igual.

## 03:05–03:40 · Anthony · Diapositiva 9

Seamos claros con lo que hay. Construido: inventario con alertas, motor de rutas y costos, agente de IA con n8n y custodia con QR. Probado: el motor y la comparación A/B/C en un ensayo técnico con un escenario sintético de Quito de 40 puntos, y una corrida registrada del agente, incluida su caída al respaldo. Falta: ensayar el recorrido en teléfono sin señal, llevar la comparación a producción y medir con datos de una empresa real. Preferimos mostrarles lo que funciona de verdad que prometer lo que no hemos medido.

## 03:40–04:05 · Jorge · Diapositiva 10

El impacto que buscamos: menos faltantes que se descubren tarde, entregas críticas a tiempo y saber cuánto cuesta realmente cada entrega. No prometemos un ahorro: lo mediremos en un piloto contra una línea base. Cada indicador está escrito para que lo entienda cualquier rol, de bodega a gerencia.

## 04:05–04:20 · Jorge · Diapositiva 11

No necesita hardware nuevo: funciona en la web y en el teléfono del conductor. Proponemos implementación inicial más suscripción. Y si faltan datos, el sistema dice “sin dato” y no inventa.

## 04:20–04:40 · Joseph · Diapositiva 12

El siguiente paso: un piloto con una empresa de limpieza, una línea base e indicadores acordados. Buscamos ese aliado aquí. Detectar antes, decidir con el costo real y probar cada entrega. Gracias.

