using System.Diagnostics.CodeAnalysis;
using IstpetDev.Application.Common.Interfaces;
using IstpetDev.Domain.Common;
using IstpetDev.Domain.Entities;
using NetTopologySuite.Geometries;
using Location = IstpetDev.Domain.Entities.Location;

namespace IstpetDev.Application.Imports;

// Inserta o actualiza por clave natural. Devuelve false cuando una fila no se aplica (ya registrada en el reporte).
internal sealed class CanonicalWriter(IApplicationDbContext db, ImportContext context, ImportReport report, Guid batchId, Guid? actorAccountId)
{

    public void Write(ImportEntity entity, IReadOnlyList<ParsedRow> rows)
    {
        var entityReport = report.Entity(entity);
        var valid = new List<ParsedRow>();
        foreach (var row in rows)
        {
            var ok = true;
            if (entity is ImportEntity.Lots or ImportEntity.Stock or ImportEntity.Consumptions)
                ok = TryProduct(entity, row, row.Text("sku")!, out _);
            if (entity == ImportEntity.Vehicles && !context.Locations.ContainsKey((LocationKinds.Warehouse, row.Text("depot_code")!)))
            { report.Error(entity, row.Row, "depot_code", "unknown_reference", "La bodega no existe; inclúyala en warehouses."); ok = false; }
            if (entity == ImportEntity.Consumptions && !context.ServicePoints.ContainsKey(row.Text("point_code")!))
            { report.Error(entity, row.Row, "point_code", "unknown_reference", "El punto no existe; inclúyalo en service_points."); ok = false; }
            if (entity == ImportEntity.RoadSegments)
            {
                var fromLoc = ResolveLocationByKind(ImportEntity.RoadSegments, row, row.Text("from_code")!, row.Text("from_kind"), "from_code", "from_kind");
                var toLoc = ResolveLocationByKind(ImportEntity.RoadSegments, row, row.Text("to_code")!, row.Text("to_kind"), "to_code", "to_kind");
                if (fromLoc is null || toLoc is null) ok = false;
                else if (fromLoc.Position is null)
                {
                    report.Error(entity, row.Row, "from_code", "missing_coordinates", $"La ubicación de origen '{fromLoc.Code}' no tiene coordenadas.");
                    ok = false;
                }
                else if (toLoc.Position is null)
                {
                    report.Error(entity, row.Row, "to_code", "missing_coordinates", $"La ubicación de destino '{toLoc.Code}' no tiene coordenadas.");
                    ok = false;
                }
            }
            if (entity == ImportEntity.Stock && ok)
            {
                var location = ResolveLocation(row, row.Text("location_code")!);
                var product = context.Products[row.Text("sku")!];
                if (row.Text("lot_code") is { } explicitLot && !context.Lots.ContainsKey((product.Id, explicitLot)))
                { report.Error(entity, row.Row, "lot_code", "unknown_reference", "El lote explícito no existe; inclúyalo en lots o deje lot_code vacío."); ok = false; }
                if (location is null) ok = false;
                else
                {
                    var values = new Dictionary<string, object?>(row.Values) { ["location_kind"] = location.Kind };
                    if (values["lot_code"] is null) values["lot_code"] = $"GENERICO-{product.Sku}";
                    valid.Add(row with { Values = values });
                }
                if (!ok && location is not null) valid.RemoveAt(valid.Count - 1);
            }
            else if (ok) valid.Add(row);
            if (!ok) entityReport.Skipped++;
        }
        object Key(ParsedRow row) => entity switch
        {
            ImportEntity.Products => row.Text("sku")!,
            ImportEntity.Lots => (row.Text("sku"), row.Text("lot_code")),
            ImportEntity.Stock => (row.Text("location_kind"), row.Text("location_code"), row.Text("sku"), row.Text("lot_code")),
            ImportEntity.Consumptions => (row.Text("point_code"), row.Text("sku"), row.Date("date")),
            ImportEntity.RoadSegments => (row.Text("from_code"), row.Text("to_code")),
            _ => row.Text("code")!,
        };
        foreach (var group in valid.GroupBy(Key))
        {
            var row = group.Last();
            var count = group.Count();
            if (count > 1)
            {
                entityReport.Duplicates += count - 1;
                report.Warn(entity, row.Row, null, "duplicate", entity is ImportEntity.Stock or ImportEntity.Consumptions
                    ? "La clave está repetida; se suman las cantidades válidas antes de aplicar."
                    : "La clave está repetida; se conserva la última fila válida.");
            }
            if (entity is ImportEntity.Stock or ImportEntity.Consumptions)
            {
                if (entity == ImportEntity.Consumptions && group.Select(r => r.Int("period_days")).Distinct().Count() > 1)
                { report.Error(entity, row.Row, "period_days", "incompatible_periods", "No se pueden sumar períodos distintos; unifique period_days."); entityReport.Skipped += count; continue; }
                try
                {
                    var total = group.Sum(r => r.Decimal("quantity")!.Value);
                    if (total > ImportLimits.MaximumQuantity) throw new OverflowException();
                    row = row with { Values = new Dictionary<string, object?>(row.Values) { ["quantity"] = total } };
                }
                catch (OverflowException)
                { report.Error(entity, row.Row, "quantity", "out_of_range", "La suma supera numeric(18,3); reduzca las cantidades."); entityReport.Skipped += count; continue; }
            }
            else entityReport.Skipped += count - 1;
            var rounded = new Dictionary<string, object?>(row.Values);
            foreach (var field in rounded.Keys.ToArray())
                if (field is not ("lat" or "lon") && rounded[field] is decimal number)
                {
                    var value = decimal.Round(number, 3, MidpointRounding.AwayFromZero);
                    if (value != number) report.Warn(entity, row.Row, field, "rounded", "Se redondeó a tres decimales después de convertir y sumar.");
                    rounded[field] = value;
                }
            row = row with { Values = rounded };
            var applied = entity switch
            {
                ImportEntity.Warehouses => Warehouse(row, entityReport),
                ImportEntity.ServicePoints => ServicePoint(row, entityReport),
                ImportEntity.Products => Product(row, entityReport),
                ImportEntity.Vehicles => Vehicle(row, entityReport),
                ImportEntity.Lots => Lot(row, entityReport),
                ImportEntity.Stock => Stock(row, entityReport),
                ImportEntity.Consumptions => Consumption(row, entityReport),
                ImportEntity.RoadSegments => RoadSegment(row, entityReport),
                _ => throw new ArgumentOutOfRangeException(nameof(entity)),
            };
            if (!applied) entityReport.Skipped += count;
        }
    }

    private bool Warehouse(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("code")!;
        UpsertLocation(LocationKinds.Warehouse, code, row.Text("name")!, Position(row), r);
        return true;
    }

    private bool ServicePoint(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("code")!;
        if (!context.ServicePoints.TryGetValue(code, out var point))
        {
            point = new ServicePoint { Code = code };
            db.ServicePoints.Add(point);
            context.ServicePoints[code] = point;
            r.Created++;
        }
        else
        {
            r.Updated++;
            PreserveMissing(row, r, ("lat", point.Position), ("lon", point.Position), ("region", point.Region), ("category", point.Category),
                ("criticality", point.Criticality), ("window_start", point.WindowStartLocal), ("window_end", point.WindowEndLocal),
                ("service_minutes", point.ServiceMinutes), ("lead_time_days", point.LeadTimeDays));
        }
        point.Name = row.Text("name")!;
        point.Position = Position(row) ?? point.Position;
        point.Region = row.Text("region") ?? point.Region;
        point.Category = row.Text("category") ?? point.Category;
        point.Criticality = Supplied(row, "criticality") || string.IsNullOrEmpty(point.Criticality) ? row.Text("criticality")!.ToLowerInvariant() : point.Criticality;
        point.WindowStartLocal = row.Time("window_start") ?? point.WindowStartLocal;
        point.WindowEndLocal = row.Time("window_end") ?? point.WindowEndLocal;
        point.ServiceMinutes = row.Int("service_minutes") ?? point.ServiceMinutes;
        point.LeadTimeDays = row.Decimal("lead_time_days") ?? point.LeadTimeDays;
        point.IsActive = true;
        UpsertLocation(LocationKinds.ServicePoint, code, point.Name, point.Position, null, servicePointId: point.Id);
        return true;
    }

    private bool Product(ParsedRow row, EntityImportReport r)
    {
        var sku = row.Text("sku")!;
        if (context.Products.TryGetValue(sku, out var previous) && previous.BaseUnit != row.Text("base_unit")!.ToUpperInvariant())
        { report.Error(ImportEntity.Products, row.Row, "base_unit", "unit_change", "No cambie la unidad base de un SKU existente; use un SKU nuevo o restablezca el fixture."); return false; }
        if (!context.Products.TryGetValue(sku, out var product))
        {
            product = new Product { Sku = sku };
            db.Products.Add(product);
            context.Products[sku] = product;
            r.Created++;
        }
        else { r.Updated++; PreserveMissing(row, r, ("weight_per_unit", product.WeightPerBaseUnit), ("volume_per_unit", product.VolumePerBaseUnit), ("unit_value", product.UnitValue), ("units_per_package", product.UnitsPerPackage)); }
        product.Name = row.Text("name")!;
        product.BaseUnit = row.Text("base_unit")!.ToUpperInvariant();
        product.WeightPerBaseUnit = Supplied(row, "weight_per_unit") ? row.Decimal("weight_per_unit") ?? 0 : product.WeightPerBaseUnit;
        product.VolumePerBaseUnit = Supplied(row, "volume_per_unit") ? row.Decimal("volume_per_unit") ?? 0 : product.VolumePerBaseUnit;
        product.UnitValue = row.Decimal("unit_value") ?? product.UnitValue;
        product.UnitsPerPackage = row.Decimal("units_per_package") ?? product.UnitsPerPackage;
        product.IsActive = true;
        return true;
    }

    private bool Vehicle(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("code")!;
        var depotCode = row.Text("depot_code")!;
        if (!context.Locations.TryGetValue((LocationKinds.Warehouse, depotCode), out var depot))
        {
            report.Error(ImportEntity.Vehicles, row.Row, "depot_code", "unknown_reference",
                $"La bodega '{depotCode}' no existe. Inclúyala en 'warehouses' en esta importación o en una anterior.");
            return false;
        }
        if (!context.Vehicles.TryGetValue(code, out var vehicle))
        {
            vehicle = new Vehicle { Code = code };
            db.Vehicles.Add(vehicle);
            context.Vehicles[code] = vehicle;
            r.Created++;
        }
        else { r.Updated++; PreserveMissing(row, r, ("max_volume", vehicle.MaxVolume), ("cost_per_km", vehicle.CostPerKm), ("vehicle_type", vehicle.VehicleType), ("usage_scope", vehicle.UsageScope)); }
        vehicle.DepotLocationId = depot.Id;
        vehicle.MaxWeight = row.Decimal("max_weight")!.Value;
        vehicle.MaxVolume = row.Decimal("max_volume") ?? vehicle.MaxVolume;
        vehicle.CostPerKm = row.Decimal("cost_per_km") ?? vehicle.CostPerKm;
        vehicle.VehicleType = row.Text("vehicle_type")?.ToLowerInvariant() ?? vehicle.VehicleType;
        vehicle.UsageScope = row.Text("usage_scope")?.ToLowerInvariant() ?? vehicle.UsageScope;
        vehicle.IsActive = true;
        UpsertLocation(LocationKinds.Vehicle, code, $"Vehículo {code}", null, null, vehicleId: vehicle.Id);
        return true;
    }

    private bool Lot(ParsedRow row, EntityImportReport r)
    {
        var sku = row.Text("sku")!;
        var lotCode = row.Text("lot_code")!;
        if (!context.Products.TryGetValue(sku, out var product))
        {
            var productName = row.Text("product_name");
            if (!string.IsNullOrWhiteSpace(productName))
            {
                var baseUnit = row.Text("base_unit") ?? "UNIDAD";
                product = new Product
                {
                    Sku = sku,
                    Name = productName,
                    BaseUnit = baseUnit.ToUpperInvariant(),
                    IsActive = true
                };
                db.Products.Add(product);
                context.Products[sku] = product;
            }
            else if (!TryProduct(ImportEntity.Lots, row, sku, out product))
            {
                return false;
            }
        }
        else
        {
            var productName = row.Text("product_name");
            if (!string.IsNullOrWhiteSpace(productName) && string.IsNullOrWhiteSpace(product.Name))
            {
                product.Name = productName;
            }
        }

        if (context.Lots.TryGetValue((product.Id, lotCode), out var old)) PreserveMissing(row, r, ("expires_on", old.ExpiresOn), ("origin", old.Origin));
        var lot = FindOrCreateLot(product, lotCode, r);
        lot.ExpiresOn = row.Date("expires_on") ?? lot.ExpiresOn;
        lot.Origin = row.Text("origin") ?? lot.Origin;
        return true;
    }

    private bool Stock(ParsedRow row, EntityImportReport r)
    {
        var code = row.Text("location_code")!;
        var sku = row.Text("sku")!;
        if (!TryProduct(ImportEntity.Stock, row, sku, out var product)) return false;
        var location = ResolveLocation(row, code);
        if (location is null) return false;

        var lotCode = row.Text("lot_code") ?? $"GENERICO-{sku}";
        
        var lot = FindOrCreateLot(product, lotCode, null);

        if (!context.Balances.TryGetValue((location.Id, lot.Id), out var balance))
        {
            balance = new StockBalance { LocationId = location.Id, LotId = lot.Id };
            db.StockBalances.Add(balance);
            context.Balances[(location.Id, lot.Id)] = balance;
            r.Created++;
        }
        else r.Updated++;

        var quantity = row.Decimal("quantity")!.Value;
        var target = quantity;
        var delta = target - balance.QuantityOnHand;
        if (delta != 0)
        {
            db.InventoryMovements.Add(new InventoryMovement
            {
                Kind = MovementKinds.ImportAdjustment,
                LotId = lot.Id,
                FromLocationId = delta < 0 ? location.Id : null,
                ToLocationId = delta > 0 ? location.Id : null,
                Quantity = Math.Abs(delta),
                ActorAccountId = actorAccountId,
                OperationId = BaseEntity.NewId(),
                OccurredAtUtc = DateTime.UtcNow,
                ImportBatchId = batchId,
                Reason = "Ajuste por importación",
            });
        }
        balance.QuantityOnHand = target;
        return true;
    }

    private bool Consumption(ParsedRow row, EntityImportReport r)
    {
        var pointCode = row.Text("point_code")!;
        var sku = row.Text("sku")!;
        if (!context.ServicePoints.TryGetValue(pointCode, out var point))
        {
            report.Error(ImportEntity.Consumptions, row.Row, "point_code", "unknown_reference",
                $"El punto '{pointCode}' no existe. Inclúyalo en 'service_points'.");
            return false;
        }
        if (!TryProduct(ImportEntity.Consumptions, row, sku, out var product)) return false;

        var date = row.Date("date")!.Value;
        if (!context.Consumptions.TryGetValue((point.Id, product.Id, date), out var consumption))
        {
            consumption = new Consumption { ServicePointId = point.Id, ProductId = product.Id, OccurredOn = date, SourceKind = "import" };
            db.Consumptions.Add(consumption);
            context.Consumptions[(point.Id, product.Id, date)] = consumption;
            r.Created++;
        }
        else r.Updated++;

        var quantity = row.Decimal("quantity")!.Value;
        consumption.Quantity = quantity;
        consumption.PeriodDays = row.Int("period_days") ?? 1;
        consumption.ImportBatchId = batchId;
        return true;
    }

    private bool TryProduct(ImportEntity entity, ParsedRow row, string sku, [NotNullWhen(true)] out Product? product)
    {
        if (context.Products.TryGetValue(sku, out product)) return true;
        report.Error(entity, row.Row, "sku", "unknown_reference", $"El SKU '{sku}' no existe. Inclúyalo en 'products'.");
        return false;
    }

    private Lot FindOrCreateLot(Product product, string code, EntityImportReport? r)
    {
        if (context.Lots.TryGetValue((product.Id, code), out var lot))
        {
            if (r is not null) r.Updated++;
            return lot;
        }
        lot = new Lot { ProductId = product.Id, Code = code };
        db.Lots.Add(lot);
        context.Lots[(product.Id, code)] = lot;
        if (r is not null) r.Created++;
        return lot;
    }

    private bool RoadSegment(ParsedRow row, EntityImportReport r)
    {
        var fromLoc = ResolveLocationByKind(ImportEntity.RoadSegments, row, row.Text("from_code")!, row.Text("from_kind"), "from_code", "from_kind");
        var toLoc = ResolveLocationByKind(ImportEntity.RoadSegments, row, row.Text("to_code")!, row.Text("to_kind"), "to_code", "to_kind");
        if (fromLoc?.Position is null || toLoc?.Position is null) return false;

        var fromLat = Math.Round((decimal)fromLoc.Position.Y, 5);
        var fromLon = Math.Round((decimal)fromLoc.Position.X, 5);
        var toLat = Math.Round((decimal)toLoc.Position.Y, 5);
        var toLon = Math.Round((decimal)toLoc.Position.X, 5);
        var key = (fromLat, fromLon, toLat, toLon);

        if (!context.RoadEdges.TryGetValue(key, out var edge))
        {
            edge = new RoadMatrixEdge
            {
                FromLat = fromLat,
                FromLon = fromLon,
                ToLat = toLat,
                ToLon = toLon,
                Source = "imported",
            };
            db.RoadMatrixEdges.Add(edge);
            context.RoadEdges[key] = edge;
            r.Created++;
        }
        else
        {
            r.Updated++;
            edge.Source = "imported";
        }

        edge.DistanceKm = row.Decimal("distance_km")!.Value;
        edge.DurationMinutes = row.Decimal("duration_min")!.Value;
        edge.EncodedPolyline = row.Text("geometry") ?? edge.EncodedPolyline;
        edge.RetrievedAtUtc = DateTime.UtcNow;
        return true;
    }

    private Location? ResolveLocationByKind(ImportEntity entity, ParsedRow row, string code, string? kindText, string codeCol, string kindCol)
    {
        if (kindText is not null)
        {
            var kind = HeaderNormalizer.Normalize(kindText) switch
            {
                "warehouse" or "bodega" or "almacen" or "deposito" => LocationKinds.Warehouse,
                "servicepoint" or "service_point" or "punto" or "cliente" or "punto_de_servicio" => LocationKinds.ServicePoint,
                "vehicle" or "vehiculo" or "camion" => LocationKinds.Vehicle,
                _ => null,
            };
            if (kind is null)
            {
                report.Error(entity, row.Row, kindCol, "invalid_value",
                    $"Tipo de ubicación '{kindText}' desconocido. Use warehouse/bodega, service_point/punto o vehicle/vehiculo.");
                return null;
            }
            if (context.Locations.TryGetValue((kind, code), out var exact)) return exact;
            report.Error(entity, row.Row, codeCol, "unknown_reference", $"No existe {kindText} '{code}'.");
            return null;
        }
        var matches = LocationKinds.All.Where(k => context.Locations.ContainsKey((k, code))).ToList();
        if (matches.Count == 1) return context.Locations[(matches[0], code)];
        report.Error(entity, row.Row, codeCol, matches.Count == 0 ? "unknown_reference" : "ambiguous_reference",
            matches.Count == 0
                ? $"La ubicación '{code}' no existe como bodega, punto ni vehículo."
                : $"'{code}' existe como {string.Join(" y ", matches)}; añada la columna {kindCol}.");
        return null;
    }

    private Location? ResolveLocation(ParsedRow row, string code) =>
        ResolveLocationByKind(ImportEntity.Stock, row, code, row.Text("location_kind"), "location_code", "location_kind");

    private Location UpsertLocation(string kind, string code, string name, Point? position, EntityImportReport? r,
        Guid? servicePointId = null, Guid? vehicleId = null)
    {
        if (context.Locations.TryGetValue((kind, code), out var location))
        {
            location.Name = name;
            location.Position = position ?? location.Position;
            location.IsActive = true;
            if (r is not null) r.Updated++;
            return location;
        }
        location = new Location
        {
            Kind = kind, Code = code, Name = name, Position = position,
            ServicePointId = servicePointId, VehicleId = vehicleId, IsActive = true,
        };
        db.Locations.Add(location);
        context.Locations[(kind, code)] = location;
        if (r is not null) r.Created++;
        return location;
    }

    private static bool Supplied(ParsedRow row, string field) => row.SuppliedFields?.Contains(field) ?? row.Values.GetValueOrDefault(field) is not null;
    private static void PreserveMissing(ParsedRow row, EntityImportReport report, params (string Field, object? Value)[] fields)
    {
        foreach (var (field, previous) in fields)
        {
            if (Supplied(row, field) || previous is null) continue;
            var key = $"{field}_preserved";
            report.Assumptions[key] = new Assumption("Campo ausente: se conserva el valor existente", report.Assumptions.GetValueOrDefault(key)?.Rows + 1 ?? 1);
            if (report.Assumptions.TryGetValue(field, out var assumption))
                report.Assumptions[field] = assumption with { Note = "Campo ausente; se aplica defecto a registros nuevos y se conserva el dato existente cuando está informado (ver *_preserved)." };
        }
    }

    private static Point? Position(ParsedRow row) =>
        row.Decimal("lat") is { } lat && row.Decimal("lon") is { } lon ? new Point((double)lon, (double)lat) { SRID = 4326 } : null;
}
