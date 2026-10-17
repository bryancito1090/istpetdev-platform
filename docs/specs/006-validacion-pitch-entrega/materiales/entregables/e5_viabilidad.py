"""Entregable 5 — Viabilidad (Manual §9 y Anexo 5.5). Uso: python3 e5_viabilidad.py"""
from lib import *

doc = nuevo(5, "Viabilidad", "Tecnología, recursos,", "implementación y riesgos", "Manual §9 y Anexo 5.5")

h1(doc, "1", "¿Cómo se implementaría?")
para(doc, "IstpetDev no necesita hardware nuevo: funciona en la web y en el teléfono del conductor. La implementación "
          "se plantea como un piloto acotado de cuatro semanas sobre una bodega y un grupo de puntos, antes de cualquier expansión.", after=6)
tabla(doc, ("Semana", "Qué se hace", "Qué cambia en la operación"), [
    ("1", "Observar y conciliar inventario y rutas actuales", "Nada: solo se mide la línea base"),
    ("2", "Ejecutar las recomendaciones en paralelo, con aprobación humana", "Nada por defecto; se registra aceptación o rechazo y su motivo"),
    ("3", "Operar un grupo pequeño con QR, custodia y revisión diaria", "Un grupo acotado trabaja con el sistema"),
    ("4", "Comparar resultados, incidencias y carga de trabajo", "Decisión: continuar, cambiar o parar"),
], (Cm(1.8), Cm(8.0), Cm(6.8)))
para(doc, "El tamaño del grupo, las metas y el umbral de avance se acuerdan con la empresa contraparte. La evaluación del "
          "Hackathon usa simulación; no la presentamos como un piloto ya realizado.", 9.5, False, GREY, after=6)

h1(doc, "2", "Tecnología")
tabla(doc, ("Capa", "Tecnología", "Por qué"), [
    ("Servidor (API y motor)", "C# .NET 8", "Motor determinista de riesgo, rutas y costos; reglas de autonomía"),
    ("Web y campo", "Angular 22 y Ionic 8 (PWA)", "Dos superficies: Torre de Control y app de campo que funciona sin señal"),
    ("Datos", "PostgreSQL 16 con PostGIS; Redis 7", "Inventario por lote, geografía de rutas y eventos en tiempo real"),
    ("Mapas", "Leaflet con OpenStreetMap; distancias OSRM", "Sin costo de licencia ni cuotas; distancias por calles reales"),
    ("Agente de IA", "n8n con DeepSeek", "Explica los resultados del motor; se puede cambiar de modelo sin tocar el cálculo"),
    ("Identidad", "Amazon Cognito (OIDC con PKCE)", "Seis roles fijos y rol Auditor; sin permisos dinámicos en esta versión"),
    ("Evidencias", "Amazon S3 privado con URL presignadas", "Fotos y firmas de entrega con acceso temporal (hasta 15 min)"),
], (Cm(3.6), Cm(5.2), Cm(7.8)), tam=9)
h2(doc, "Principios de diseño que reducen el riesgo técnico")
bullet(doc, [("El motor calcula, la IA explica, la persona decide: ", True), ("la IA no inventa cifras y, si falla, hay una explicación de respaldo.", False)])
bullet(doc, [("Idempotencia: ", True), ("una operación repetida tras un corte de señal produce un solo movimiento.", False)])
bullet(doc, [("Reproducibilidad: ", True), ("mismas entradas y semilla, mismos resultados.", False)])

h1(doc, "3", "Recursos y costos")
para(doc, "Infraestructura en dos perfiles; la demostración y el piloto usan el perfil A.", after=4)
tabla(doc, ("", "Perfil A · demo y piloto", "Perfil B · operación nacional"), [
    ("Cómputo", "Un servidor EC2 t3.medium (2 vCPU, 4 GB) con Docker", "ECS Fargate con 2 a 6 réplicas"),
    ("Base de datos", "Amazon RDS PostgreSQL 16", "RDS Multi-AZ con réplica"),
    ("Entrada", "Nginx con TLS", "Balanceador de carga con firewall web"),
    ("Despliegue", "GitHub Actions con reversión a la versión anterior", "GitHub Actions con despliegue azul-verde"),
    ("Estado", "En uso en el prototipo", "Diseñado; no desplegado"),
], (Cm(3.0), Cm(6.8), Cm(6.8)), tam=9)
aviso(doc, "Costo verificado, y su alcance:", "el servidor del perfil A cuesta unos USD 37 al mes (cómputo t3.medium, disco de 40 GB e IP pública, "
     "tarifa de AWS verificada el 7-oct-2026). Esa cifra NO incluye la base de datos en RDS, el almacenamiento S3, el modelo de IA "
     "ni el soporte; el costo total del perfil A es PENDIENTE de levantar con fuente fechada.")
tabla(doc, ("Componente de costo", "Evidencia que falta"), [
    ("Base de datos RDS y almacenamiento S3", "Tamaño, retención y tráfico reales del piloto"),
    ("Modelo de IA (DeepSeek)", "Tokens, frecuencia y reintentos medidos"),
    ("Distancias propias (OSRM) o proveedor", "Decisión de piloto y su costo"),
    ("Desarrollo, integración, capacitación y soporte", "Horas estimadas con la empresa contraparte"),
    ("Operación logística", "Costo por kilómetro, conductor, urgencias y penalizaciones documentadas"),
], (Cm(7.8), Cm(8.8)))
para(doc, "Equipo: cinco integrantes con experiencia transversal (desarrollo, datos, infraestructura y producto). "
          "No incluimos estimaciones de nube o de APIs sin fecha y fuente.", 9.5, False, GREY)

h1(doc, "4", "Riesgos y cómo los gestionamos")
tabla(doc, ("Riesgo", "Efecto", "Mitigación", "Estado"), [
    ("Datos incompletos o inconsistentes", "Cobertura con falsa precisión", "Validar al importar, mostrar fuente y frescura, decir «sin dato»", "Diseñado"),
    ("Rutas poco realistas", "Ventanas o restricciones mal modeladas", "Validar reglas con el planificador; revisión humana antes de publicar", "Diseñado"),
    ("Costos mal estimados", "Decisión sesgada", "Mostrar el desglose y calibrar con viajes conciliados", "Por validar en piloto"),
    ("La IA falla o es manipulada", "Explicación errónea o ausente", "Política determinista en el backend; respaldo por plantilla; datos delimitados", "Construido"),
    ("Sin señal en el punto de entrega", "Recepción perdida o duplicada", "Cola local, sincronización idempotente y conciliación de cantidades", "Construido; falta prueba en teléfono"),
    ("QR o evidencias exponen información", "Acceso indebido", "Vista pública limitada a hitos de custodia y límite de consultas", "Construido"),
    ("Falla de red o nube durante la demostración", "Demostración interrumpida", "Punto de acceso móvil, entorno espejo local y video de respaldo", "Preparado"),
    ("Baja adopción o resistencia al cambio", "El sistema no se usa", "Capacitación por rol y recogida de observaciones de bodega y conductores", "Por validar en piloto"),
    ("Costos de nube sin control", "Viabilidad débil", "Medir horas y recursos persistentes; presupuesto con límite", "Por medir"),
], (Cm(3.6), Cm(3.4), Cm(6.8), Cm(2.8)), tam=8.5)

h1(doc, "5", "Límites y próximos pasos")
bullet(doc, "La versión desplegada es el perfil A; la operación nacional (perfil B) está diseñada, no desplegada.")
bullet(doc, "No hay medidas de carga ni de recuperación ante fallos; no declaramos alta disponibilidad.")
bullet(doc, "Tráfico en vivo y normativa de circulación de Quito: las franjas de hora pico son supuestos configurables.")
bullet(doc, "Próximos pasos: levantar el costo total con la contraparte, acordar la línea base y ejecutar el piloto de cuatro semanas.")

guardar(doc, "IstpetDev-Entregable-5-Viabilidad.docx")
