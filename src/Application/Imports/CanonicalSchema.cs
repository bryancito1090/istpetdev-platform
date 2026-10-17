namespace IstpetDev.Application.Imports;

public enum FieldType { Text, Decimal, Integer, Date, Time }

// DefaultValue se aplica cuando falta el dato; MissingNote explica el supuesto en el reporte (guardarraíl G3).
public sealed record CanonicalField(
    string Name, FieldType Type, bool Required, string[] Synonyms,
    string? DefaultValue = null, string? MissingNote = null, bool IsCode = false)
{
    public IEnumerable<string> Candidates() => Synonyms.Prepend(Name).Select(HeaderNormalizer.Normalize);
}

public static class CanonicalSchema
{
    public static readonly ImportEntity[] Order = Enum.GetValues<ImportEntity>();

    private static readonly string[] Lat = ["latitud", "latitude", "y"];
    private static readonly string[] Lon = ["longitud", "longitude", "lng", "long", "x"];
    private static readonly string[] Sku = ["codigo_producto", "producto", "cod_producto"];

    private static CanonicalField Code(string name, params string[] synonyms) => new(name, FieldType.Text, true, synonyms, IsCode: true);

    public static readonly IReadOnlyDictionary<ImportEntity, IReadOnlyList<CanonicalField>> Fields =
        new Dictionary<ImportEntity, IReadOnlyList<CanonicalField>>
        {
            [ImportEntity.Warehouses] =
            [
                Code("code", "codigo", "cod", "id", "bodega", "codigo_bodega"),
                new("name", FieldType.Text, true, ["nombre", "descripcion"]),
                new("lat", FieldType.Decimal, true, Lat),
                new("lon", FieldType.Decimal, true, Lon),
            ],
            [ImportEntity.ServicePoints] =
            [
                Code("code", "codigo", "cod", "id", "punto", "codigo_punto", "cod_cliente", "codigo_cliente", "cliente_id"),
                new("name", FieldType.Text, true, ["nombre", "cliente", "razon_social", "punto_nombre"]),
                new("lat", FieldType.Decimal, false, Lat, MissingNote: "sin coordenadas: el punto no será ruteable"),
                new("lon", FieldType.Decimal, false, Lon),
                new("region", FieldType.Text, false, ["provincia", "ciudad", "zona"]),
                new("category", FieldType.Text, false, ["tipo", "categoria", "segmento"]),
                new("criticality", FieldType.Text, false, ["criticidad", "prioridad"], DefaultValue: "normal"),
                new("window_start", FieldType.Time, false, ["hora_inicio", "apertura", "desde"], MissingNote: "se usará la ventana global por defecto"),
                new("window_end", FieldType.Time, false, ["hora_fin", "cierre", "hasta"]),
                new("service_minutes", FieldType.Integer, false, ["tiempo_servicio", "minutos_servicio"], MissingNote: "se usará el tiempo de servicio global"),
                new("lead_time_days", FieldType.Decimal, false, ["lead_time", "tiempo_reposicion", "dias_entrega"], MissingNote: "se usará el lead time global"),
            ],
            [ImportEntity.Products] =
            [
                Code("sku", "codigo", "cod_producto", "producto_id", "item", "codigo_producto"),
                new("name", FieldType.Text, true, ["nombre", "descripcion", "producto"]),
                new("base_unit", FieldType.Text, true, ["unidad", "um", "unidad_medida", "unidad_base"]),
                new("weight_per_unit", FieldType.Decimal, false, ["peso", "peso_unitario", "kg"], DefaultValue: "0", MissingNote: "capacidad por peso no evaluable"),
                new("unit_value", FieldType.Decimal, false, ["valor_unitario"]),
                new("units_per_package", FieldType.Decimal, false, ["unidades_por_bulto"]),
                new("volume_per_unit", FieldType.Decimal, false, ["volumen", "volumen_unitario", "m3"], DefaultValue: "0", MissingNote: "capacidad por volumen no evaluable"),
            ],
            [ImportEntity.Vehicles] =
            [
                Code("code", "codigo", "placa", "vehiculo"),
                Code("depot_code", "bodega", "deposito", "base", "codigo_bodega"),
                new("max_weight", FieldType.Decimal, true, ["capacidad_kg", "capacidad", "carga_maxima", "peso_maximo"]),
                new("max_volume", FieldType.Decimal, false, ["capacidad_m3", "volumen_maximo"], MissingNote: "sin límite de volumen"),
                new("vehicle_type", FieldType.Text, false, ["tipo_vehiculo"]),
                new("usage_scope", FieldType.Text, false, ["uso_vehiculo"]),
                new("cost_per_km", FieldType.Decimal, false, ["costo_km", "costo_por_km"], MissingNote: "se usará el costo por km global"),
            ],
            [ImportEntity.Lots] =
            [
                Code("sku", Sku),
                Code("lot_code", "lote", "codigo_lote"),
                new("expires_on", FieldType.Date, false, ["vencimiento", "fecha_vencimiento", "caducidad"]),
                new("origin", FieldType.Text, false, ["origen", "proveedor"]),
                new("product_name", FieldType.Text, false, ["nombre_producto", "descripcion_producto", "nombre"]),
                new("base_unit", FieldType.Text, false, ["unidad", "unidad_base", "um"]),
            ],
            [ImportEntity.Stock] =
            [
                new("location_kind", FieldType.Text, false, ["tipo_ubicacion", "tipo"]),
                Code("location_code", "ubicacion", "codigo_ubicacion", "bodega", "punto"),
                Code("sku", Sku),
                new("lot_code", FieldType.Text, false, ["lote", "codigo_lote"], MissingNote: "se usa el lote GENERICO-{sku}", IsCode: true),
                new("quantity", FieldType.Decimal, true, ["cantidad", "stock", "existencia", "saldo"]),
            ],
            [ImportEntity.Consumptions] =
            [
                Code("point_code", "punto", "codigo_punto", "cliente", "cod_cliente"),
                Code("sku", Sku),
                new("date", FieldType.Date, true, ["fecha", "dia", "mes", "periodo"]),
                new("quantity", FieldType.Decimal, true, ["cantidad", "consumo", "unidades"]),
                new("period_days", FieldType.Integer, false, ["dias_periodo", "periodo_dias"], DefaultValue: "1"),
            ],
            [ImportEntity.RoadSegments] =
            [
                Code("from_code", "desde", "origen", "origen_codigo", "from"),
                Code("to_code", "hasta", "destino", "destino_codigo", "to"),
                new("distance_km", FieldType.Decimal, true, ["distancia", "km", "distancia_km"]),
                new("duration_min", FieldType.Decimal, true, ["duracion", "tiempo", "minutos", "duracion_min"]),
                new("geometry", FieldType.Text, false, ["geometria", "polyline", "polilinea", "encoded_polyline", "geom"]),
                new("from_kind", FieldType.Text, false, ["tipo_origen", "tipo_desde"]),
                new("to_kind", FieldType.Text, false, ["tipo_destino", "tipo_hasta"]),
            ],
        };
}
