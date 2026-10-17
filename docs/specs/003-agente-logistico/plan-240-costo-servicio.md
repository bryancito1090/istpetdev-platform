# Plan #240 — costo total estimado de servir

Fecha: 16-oct-2026. Base: develop, después de #230. Alcance: implementación,
pruebas sobre PostGIS aislado, PR y merge a develop; sin despliegue a main.

1. Una migración EF aditiva: Product.UnitValue/UnitsPerPackage nullable y
   Vehicle.VehicleType/UsageScope nullable. Importación opcional con PreserveMissing,
   validación de no negativos, paquetes positivos y tipos/usos admitidos.
2. ServiceCostOptions propio y una propiedad nullable en LogisticsOptions.
   ServiceCostCalculator conserva LogisticsCostCalculator. Sin configuración,
   metadatos de combustible completos o perfil válido, conservar costo y tiempos
   anteriores y explicar la ausencia del perfil/configuración.
3. Desglose estimado en RouteEvaluationResult y VehicleMatchResult. Fuel en USD,
   Wear, Driver, Helpers (costo), HelperCount (personas), Shrink, Total, LoadPct,
   FuelPrice (USD/galón, fecha, fuente) y Notes. LegacyTransport representa el
   componente km del fallback sin llamarlo combustible medido.
4. Carga = máximo peso simultáneo / capacidad, como fracción en combustible y
   porcentaje en salida. Merma: valor transportado contado solo en recogidas,
   tasa normal hasta 90% inclusive, superior después. Bultos: techo de unidades /
   unidades por paquete por SKU/parada; bulto más pesado considerando el parcial.
   Ayudantes: máximo requerido en todas las paradas; conductor viaja aparte. Descarga =
   10 + bultos/(1 + ayudantes de la ruta), solo sin tiempo definido en punto.
   Mantener tiempos/costo legado si falta perfil; faltantes de catálogo visibles.
5. Cambios acotados en VehicleMatcher/RouteEvaluator. No editar
   RoutePlansController; documentar para Anthony la integración pendiente de
   /confirm. #244 y #245 rebasan después. La pantalla corresponde a #242.
6. Pruebas de fórmulas/límites, JSON, ambos consumidores, importación y rollback
   de transacción, historial desde cero y actualización de fixture demo conservando
   datos. Evidencia reproducible y captura del desglose obtenido del dataset.
7. Prelaunch local, suite backend/controles pertinentes, CI remoto del HEAD final,
   revisión, aprobación permitida por GitHub, merge y cierre verificados.

La propuesta de PR #239 es referencia de diseño; se integró a develop durante
esta implementación (82c4f65).
La autorización de migración de #240/#253 actualiza su recomendación inicial de
solo configuración; no introducir otras migraciones. Todos los valores de prueba
son sintéticos; #243 es dueño de la calibración del dataset de Quito.

[Ejemplo de configuración](service-cost.example.json): valores placeholder del
issue, no se carga automáticamente. #243 debe sustituirlos con los parámetros
sintéticos documentados antes de activar ServiceCost en la demo. Omitir la
sección conserva el comportamiento anterior; no activar perfiles cero por defecto.
