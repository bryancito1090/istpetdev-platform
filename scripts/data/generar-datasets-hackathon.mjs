import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const ROOT = path.resolve(__dirname, '../..');
const NEW_DIR = path.join(ROOT, 'data/dataset-hackathon-quito');
const FIXTURE_DIR = path.join(ROOT, 'data/fixtures/hackathon-recoleccion-integrantes');
const FIXTURE_QUITO_DIR = path.join(ROOT, 'data/fixtures/dataset-hackathon-quito');
const DATA_DIR = path.join(ROOT, 'data');

[NEW_DIR, FIXTURE_DIR, FIXTURE_QUITO_DIR].forEach(dir => {
  if (!fs.existsSync(dir)) {
    fs.mkdirSync(dir, { recursive: true });
  }
});

// 100% de ubicaciones en el área de Quito (Pichincha, Ecuador)
const locations = [
  { code: 'BOD-ECUA', name: 'Barrio La Ecuatoriana (Quito Sur)', lat: -0.3079, lon: -78.5682, region: 'Pichincha', pkgs: 1, crit: 'normal', winS: '07:00', winE: '18:00', cat: 'recoleccion_integrantes' },
  { code: 'BOD-VILL', name: 'La Villaflora - El Recreo (Quito Sur)', lat: -0.2520, lon: -78.5180, region: 'Pichincha', pkgs: 1, crit: 'normal', winS: '07:00', winE: '18:00', cat: 'recoleccion_integrantes' },
  { code: 'BOD-GUAM', name: 'Feria de Autos Guamaní (Quito Sur)', lat: -0.3556, lon: -78.5444, region: 'Pichincha', pkgs: 1, crit: 'media', winS: '07:00', winE: '18:00', cat: 'recoleccion_integrantes' },
  { code: 'BOD-CHIM', name: 'Av. Napo - Chimbacalle (Quito)', lat: -0.2333, lon: -78.5083, region: 'Pichincha', pkgs: 1, crit: 'alta', winS: '06:30', winE: '10:00', cat: 'recoleccion_integrantes' },
  { code: 'BOD-CEQ', name: 'Centro de Exposiciones Quito (Sede Hackathon)', lat: -0.1925, lon: -78.4839, region: 'Pichincha', pkgs: -4, crit: 'alta', winS: '08:00', winE: '20:00', cat: 'entrega_final' }
];

function toRad(d) { return d * Math.PI / 180; }
function haversine(lat1, lon1, lat2, lon2) {
  const R = 6371;
  const dLat = toRad(lat2 - lat1);
  const dLon = toRad(lon2 - lon1);
  const a = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) * Math.sin(dLon / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

// 1. bodegas.csv
const bodegasCsv = [
  'code,name,lat,lon',
  ...locations.map(l => `${l.code},${l.name},${l.lat},${l.lon}`)
].join('\n') + '\n';

// 2. puntos.csv (para importar en service_points)
const puntosCsv = [
  'code,name,lat,lon,region,category,criticality,window_start,window_end,service_minutes,lead_time_days',
  ...locations.map(l => `${l.code.replace('BOD-', 'PTO-')},${l.name},${l.lat},${l.lon},${l.region},${l.cat},${l.crit},${l.winS},${l.winE},15,1`)
].join('\n') + '\n';

// 3. vehiculos.csv (flota asignada al área urbana de Quito)
const vehiculos = [
  { code: 'VEH-ECUA', depot_code: 'BOD-ECUA', max_weight: 800, max_volume: 5.0, vehicle_type: 'Furgón Urbano Ligero', usage_scope: 'Urbano Quito Sur', cost_per_km: 0.22 },
  { code: 'VEH-VILL', depot_code: 'BOD-VILL', max_weight: 800, max_volume: 5.0, vehicle_type: 'Furgón Urbano Ligero', usage_scope: 'Urbano Quito Sur-Centro', cost_per_km: 0.22 },
  { code: 'VEH-GUAM', depot_code: 'BOD-GUAM', max_weight: 800, max_volume: 5.0, vehicle_type: 'Furgón Urbano Ligero', usage_scope: 'Urbano Quito Sur', cost_per_km: 0.22 },
  { code: 'VEH-CHIM', depot_code: 'BOD-CHIM', max_weight: 800, max_volume: 5.0, vehicle_type: 'Furgón Urbano Rápido', usage_scope: 'Urbano Prioritario', cost_per_km: 0.20 },
  { code: 'VEH-CEQ', depot_code: 'BOD-CEQ', max_weight: 2000, max_volume: 12.0, vehicle_type: 'Vehículo Logístico Sede', usage_scope: 'Consolidación Matriz', cost_per_km: 0.25 }
];

const vehiculosCsv = [
  'code,depot_code,max_weight,max_volume,vehicle_type,usage_scope,cost_per_km',
  ...vehiculos.map(v => `${v.code},${v.depot_code},${v.max_weight},${v.max_volume},${v.vehicle_type},${v.usage_scope},${v.cost_per_km}`)
].join('\n') + '\n';

// 4. productos.csv (Producto: Integrantes del Equipo)
const productosCsv = [
  'sku,name,base_unit,weight_per_unit,unit_value,units_per_package,volume_per_unit',
  'INT-HACK-01,Integrantes del Equipo (Pasajeros Hackathon),persona,70.00,0.00,1,0.25'
].join('\n') + '\n';

// 5. inventario.csv (Stock inicial de 1 integrante por cada bodega de Quito)
const inventarioCsv = [
  'location_kind,location_code,sku,lot_code,quantity',
  'warehouse,BOD-ECUA,INT-HACK-01,LOT-ECUA,1',
  'warehouse,BOD-VILL,INT-HACK-01,LOT-VILL,1',
  'warehouse,BOD-GUAM,INT-HACK-01,LOT-GUAM,1',
  'warehouse,BOD-CHIM,INT-HACK-01,LOT-CHIM,1',
  'warehouse,BOD-CEQ,INT-HACK-01,LOT-DEST,0'
].join('\n') + '\n';

// 6. lotes.csv
const lotesCsv = [
  'sku,lot_code,expires_on,origin,product_name,base_unit',
  'INT-HACK-01,LOT-ECUA,2026-10-31,Barrio La Ecuatoriana (Quito Sur),Integrantes del Equipo,persona',
  'INT-HACK-01,LOT-VILL,2026-10-31,La Villaflora - El Recreo (Quito Sur),Integrantes del Equipo,persona',
  'INT-HACK-01,LOT-GUAM,2026-10-31,Guamani Quito Sur,Integrantes del Equipo,persona',
  'INT-HACK-01,LOT-CHIM,2026-10-31,Chimbacalle Quito Centro-Sur,Integrantes del Equipo,persona'
].join('\n') + '\n';

// 7. tramos.csv (Matriz de distancias viales urbanas en Quito)
const tramosRows = ['from_code,to_code,distance_km,duration_min,geometry,from_kind,to_kind'];
for (const orig of locations) {
  for (const dest of locations) {
    if (orig.code !== dest.code) {
      const dKm = haversine(orig.lat, orig.lon, dest.lat, dest.lon);
      const speed = 25; // Velocidad promedio tráfico urbano en Quito (~25 km/h)
      const tMin = Math.round((dKm / speed) * 60) + 5;
      tramosRows.push(`${orig.code},${dest.code},${dKm.toFixed(2)},${tMin},LINESTRING,warehouse,warehouse`);
    }
  }
}
const tramosCsv = tramosRows.join('\n') + '\n';

// 8. mapping.json
const mappingJson = JSON.stringify({
  name: 'dataset-hackathon-quito',
  files: {
    warehouses: { file: 'bodegas.csv' },
    service_points: { file: 'puntos.csv' },
    vehicles: { file: 'vehiculos.csv' },
    products: { file: 'productos.csv' },
    stock: { file: 'inventario.csv' },
    lots: { file: 'lotes.csv' },
    road_segments: { file: 'tramos.csv' }
  }
}, null, 2) + '\n';

// 9. Dataset Unificado de Ruta (ruta_recoleccion_bodegas.csv)
const rutaUnificadaCsv = [
  'stop_code,stop_name,canton_province,latitude,longitude,stop_type,packages_pickup,packages_dropoff,net_package_change,priority_level,is_highest_priority,time_window_start,time_window_end,service_minutes,final_destination_code',
  'BOD-ECUA,Barrio La Ecuatoriana,Quito (Pichincha),-0.3079,-78.5682,pickup,1,0,+1,normal,false,07:00,18:00,15,DEST-CEQ',
  'BOD-VILL,La Villaflora - El Recreo,Quito (Pichincha),-0.2520,-78.5180,pickup,1,0,+1,normal,false,07:00,18:00,15,DEST-CEQ',
  'BOD-GUAM,Feria de Autos Guamani,Quito (Pichincha),-0.3556,-78.5444,pickup,1,0,+1,media,false,07:00,18:00,15,DEST-CEQ',
  'BOD-CHIM,Av. Napo - Chimbacalle,Quito (Pichincha),-0.2333,-78.5083,pickup,1,0,+1,alta,true,06:30,10:00,15,DEST-CEQ',
  'DEST-CEQ,Centro de Exposiciones Quito,Quito (Pichincha),-0.1925,-78.4839,delivery,0,4,-4,final,false,08:00,19:00,30,DEST-CEQ'
].join('\n') + '\n';

// 10. README.md explicativo para la carpeta
const readmeMd = `# Dataset Hackathon - Recolección Urbana Quito

Dataset con coordenadas geográficas y restricciones operativas dentro del área urbana del Distrito Metropolitano de Quito (Pichincha, Ecuador).

## Puntos de Parada y Recolección
| Código | Nombre | Sector | Latitud | Longitud | Tipo | Prioridad |
|---|---|---|---|---|---|---|
| BOD-ECUA | Barrio La Ecuatoriana | Quito Sur | -0.3079 | -78.5682 | Recolección | Normal |
| BOD-VILL | La Villaflora - El Recreo | Quito Sur-Centro | -0.2520 | -78.5180 | Recolección | Normal |
| BOD-GUAM | Feria de Autos Guamaní | Quito Sur | -0.3556 | -78.5444 | Recolección | Media |
| BOD-CHIM | Av. Napo - Chimbacalle | Quito Centro-Sur | -0.2333 | -78.5083 | Recolección | **Alta (Urgente)** |
| BOD-CEQ | Centro de Exposiciones Quito | Quito Norte (Iñaquito) | -0.1925 | -78.4839 | Entrega Final | Sede Hackathon |

## Archivos Generados
- \`bodegas.csv\`: Catálogo de bodegas de origen y destino.
- \`puntos.csv\`: Puntos de servicio compatibles con la importación en la plataforma.
- \`vehiculos.csv\`: Flota asignada a cada depósito con costos y capacidades urbanas.
- \`productos.csv\`: Definición de integrantes como unidad transportable.
- \`inventario.csv\`: Stock de integrantes disponible en cada punto.
- \`lotes.csv\`: Trazabilidad y procedencia de cada lote.
- \`tramos.csv\`: Matriz de distancias y tiempos de viaje con tráfico urbano.
- \`ruta_recoleccion_bodegas.csv\`: Dataset consolidado de la ruta de recolección y entrega.
- \`mapping.json\`: Descriptor canónico para la carga de datos masiva en la plataforma.
`;

function writeFilesTo(dir) {
  fs.writeFileSync(path.join(dir, 'bodegas.csv'), bodegasCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'puntos.csv'), puntosCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'vehiculos.csv'), vehiculosCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'productos.csv'), productosCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'inventario.csv'), inventarioCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'lotes.csv'), lotesCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'tramos.csv'), tramosCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'ruta_recoleccion_bodegas.csv'), rutaUnificadaCsv, 'utf8');
  fs.writeFileSync(path.join(dir, 'mapping.json'), mappingJson, 'utf8');
  fs.writeFileSync(path.join(dir, 'README.md'), readmeMd, 'utf8');
}

// 1. Guardar en la NUEVA carpeta dedicada: data/dataset-hackathon-quito/
writeFilesTo(NEW_DIR);

// 2. Guardar en fixtures para importación estándar
writeFilesTo(FIXTURE_DIR);
writeFilesTo(FIXTURE_QUITO_DIR);

// 3. Guardar también copias directas en data/
fs.writeFileSync(path.join(DATA_DIR, 'bodegas.csv'), bodegasCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'bodegas_recoleccion.csv'), bodegasCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'puntos_entrega.csv'), puntosCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'puntos_recoleccion.csv'), puntosCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'vehiculos.csv'), vehiculosCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'productos.csv'), productosCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'inventario_integrantes.csv'), inventarioCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'lotes.csv'), lotesCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'tramos.csv'), tramosCsv, 'utf8');
fs.writeFileSync(path.join(DATA_DIR, 'ruta_recoleccion_bodegas.csv'), rutaUnificadaCsv, 'utf8');

console.log('✅ Archivos generados exitosamente en la nueva carpeta:');
console.log(`📁 ${NEW_DIR}`);
console.log(`📁 ${FIXTURE_QUITO_DIR}`);
console.log(`📁 ${FIXTURE_DIR}`);
