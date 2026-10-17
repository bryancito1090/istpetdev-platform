import { fileURLToPath } from 'node:url';

const pts = {
  ECUA: { name: 'Barrio La Ecuatoriana (Quito Sur)', lat: -0.3079, lon: -78.5682, pkgs: 1 },
  VILL: { name: 'La Villaflora - El Recreo (Quito Sur)', lat: -0.2520, lon: -78.5180, pkgs: 1 },
  GUAM: { name: 'Feria de Autos Guamaní (Quito Sur)', lat: -0.3556, lon: -78.5444, pkgs: 1 },
  CHIM: { name: 'Av. Napo - Chimbacalle (Quito Centro-Sur)', lat: -0.2333, lon: -78.5083, pkgs: 1 },
  CEQ: { name: 'Centro de Exposiciones Quito (Norte)', lat: -0.1925, lon: -78.4839, pkgs: -4 }
};

function toRad(d) { return d * Math.PI / 180; }
function dist(p1, p2) {
  const R = 6371;
  const dLat = toRad(p2.lat - p1.lat);
  const dLon = toRad(p2.lon - p1.lon);
  const a = Math.sin(dLat / 2) ** 2 + Math.cos(toRad(p1.lat)) * Math.cos(toRad(p2.lat)) * Math.sin(dLon / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
}

function permute(arr) {
  if (arr.length <= 1) return [arr];
  const res = [];
  for (let i = 0; i < arr.length; i++) {
    const cur = arr[i];
    const rem = [...arr.slice(0, i), ...arr.slice(i + 1)];
    for (const p of permute(rem)) res.push([cur, ...p]);
  }
  return res;
}

const pickups = ['ECUA', 'VILL', 'GUAM', 'CHIM'];
const allRoutes = permute(pickups).map(p => [...p, 'CEQ']);

const evaluated = allRoutes.map(r => {
  let d = 0;
  for (let i = 0; i < r.length - 1; i++) {
    d += dist(pts[r[i]], pts[r[i + 1]]);
  }
  return { route: r, distance: d };
});

evaluated.sort((a, b) => a.distance - b.distance);

console.log('================================================================');
console.log('🔄 SIMULACIÓN CON TODAS LAS BODEGAS EN LA MISMA PRIORIDAD');
console.log('================================================================\n');

console.log('🏆 TOP 3 MEJORES RUTAS GLOBALES (Objetivo único: Menor distancia en km):');
evaluated.slice(0, 3).forEach((item, idx) => {
  console.log(`\n#${idx + 1} | Distancia: ${item.distance.toFixed(2)} km`);
  const steps = item.route.map((k, i) => `${i + 1}. ${pts[k].name}`);
  console.log('   Secuencia: ' + steps.join('\n   ➜ '));
  console.log(`   Posición de Chimbacalle: Paso ${item.route.indexOf('CHIM') + 1} de 5`);
});

console.log('\n----------------------------------------------------------------');
console.log('📍 COMPARACIÓN: ¿QUÉ LE PASA A CHIMBACALLE SEGÚN EL ORIGEN?');
console.log('----------------------------------------------------------------');

pickups.forEach(orig => {
  const filtered = evaluated.filter(e => e.route[0] === orig);
  const best = filtered[0];
  const chimPos = best.route.indexOf('CHIM') + 1;
  console.log(`\n🚗 Si el vehículo sale de: ${pts[orig].name}`);
  console.log(`   Ruta óptima: ${best.route.map(k => pts[k].name).join(' ➜ ')}`);
  console.log(`   Distancia: ${best.distance.toFixed(2)} km`);
  console.log(`   Llegada a Chimbacalle: Paso ${chimPos} de 5 ${chimPos === 4 ? '(Justo antes de la entrega final)' : ''}`);
});

console.log('\n----------------------------------------------------------------');
console.log('⚖️ IMPACTO EN TIEMPOS Y RETRASOS EN EL ÁREA URBANA DE QUITO:');
console.log('----------------------------------------------------------------');
console.log('1. Cuando Chimbacalle tiene PRIORIDAD ALTA:');
console.log('   - Se asegura su recolección en la franja pico matutina temprana (06:30 - 10:00).');
console.log('   - Evita la congestión vehicular de los túneles y el corredor sur-centro.');
console.log('\n2. Cuando todas tienen la MISMA PRIORIDAD:');
console.log('   - El recorrido sigue un orden geográfico natural Sur ➜ Norte (Guamaní ➜ La Ecuatoriana ➜ Villaflora ➜ Chimbacalle ➜ CEQ).');
console.log('   - Chimbacalle se atiende naturalmente como última parada de recolección (Paso 4), antes de llegar al CEQ.');
