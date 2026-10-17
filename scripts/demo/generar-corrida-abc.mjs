#!/usr/bin/env node
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFile, mkdir, rename, writeFile } from 'node:fs/promises';
import { dirname, resolve, isAbsolute } from 'node:path';
import process from 'node:process';

const ROOT = resolve(import.meta.dirname, '../..');
const FIXTURE = 'demo-quito-planificacion';
const SELECTION_PATH = resolve(ROOT, 'docs/specs/006-validacion-pitch-entrega/seleccion-issue-276.propuesta.json');
const DEFAULT_OUTPUT = resolve(ROOT, 'apps/web/src/pages/rutas/corrida-abc.demo-quito.json');
const PUBLIC_OUTPUT = resolve(ROOT, 'apps/web/public/corrida-abc.demo-quito.json');
const FIXTURE_DIR = resolve(ROOT, 'data/fixtures', FIXTURE);

function parseArgs(argv) {
  const args = { execute: false, allowProvisional: false, api: 'http://localhost:5000', config: process.env.DEMO_CONFIG_PATH, output: DEFAULT_OUTPUT };
  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === '--execute') args.execute = true;
    else if (arg === '--allow-provisional-selection') args.allowProvisional = true;
    else if (arg === '--api') args.api = argv[++i];
    else if (arg === '--config') args.config = argv[++i];
    else if (arg === '--output') args.output = argv[++i];
    else if (arg === '--help' || arg === '-h') args.help = true;
    else throw new Error(`Argumento desconocido: ${arg}`);
  }
  if (args.help) return args;
  if (!args.api) throw new Error('--api requiere una dirección.');
  args.api = new URL(args.api);
  args.output = isAbsolute(args.output) ? args.output : resolve(ROOT, args.output);
  args.config = args.config ? (isAbsolute(args.config) ? args.config : resolve(ROOT, args.config)) : undefined;
  return args;
}

function printHelp() {
  console.log(`Uso:\n  node scripts/demo/generar-corrida-abc.mjs [--config ruta]\n  DEMO_API_TOKEN=... DEMO_LOCAL_RESET_ACK=I_CONFIRM_LOCAL_DEMO_DATABASE node scripts/demo/generar-corrida-abc.mjs --execute --config ruta\n  Añada --allow-provisional-selection para ejecutar antes de aprobar la selección.\n\nSin --execute solo valida fixture y selección; no contacta la API. La ejecución requiere API HTTP local, token de planificador, configuración local explícita y confirmación de base de demo local aislada.`);
}

const sha256 = value => createHash('sha256').update(value).digest('hex');
const canonical = value => JSON.stringify(value, (_key, item) => item && !Array.isArray(item) && typeof item === 'object'
  ? Object.fromEntries(Object.keys(item).sort().map(key => [key, item[key]])) : item);

function parseCsv(text) {
  const rows = [];
  let row = [], field = '', quoted = false;
  for (let i = 0; i < text.length; i += 1) {
    const c = text[i];
    if (quoted) {
      if (c === '"' && text[i + 1] === '"') { field += '"'; i += 1; }
      else if (c === '"') quoted = false;
      else field += c;
    } else if (c === '"') quoted = true;
    else if (c === ',') { row.push(field); field = ''; }
    else if (c === '\n') { row.push(field.replace(/\r$/, '')); rows.push(row); row = []; field = ''; }
    else field += c;
  }
  if (field || row.length) { row.push(field.replace(/\r$/, '')); rows.push(row); }
  const [headers, ...data] = rows;
  if (!headers?.length) throw new Error('CSV vacío o sin encabezado.');
  return data.filter(values => values.some(Boolean)).map(values => Object.fromEntries(headers.map((key, index) => [key, values[index] ?? ''])));
}

function quantityFor(rows, order, sku, rule) {
  const matching = rows.filter(row => row.point_code === order.pointCode && row.sku === sku && row.date >= rule.startDate && row.date <= rule.endDate);
  const days = new Set(matching.map(row => row.date));
  if (days.size !== rule.requiredDailySamples || matching.length !== rule.requiredDailySamples || matching.some(row => row.period_days !== '1')) {
    throw new Error(`${order.pointCode}/${sku}: se esperaban ${rule.requiredDailySamples} muestras diarias únicas; recibidas ${days.size}. No se imputan datos faltantes.`);
  }
  const sum = matching.reduce((total, row) => {
    const value = Number(row.quantity);
    if (!Number.isFinite(value) || value < 0) throw new Error(`${order.pointCode}/${sku}: cantidad inválida.`);
    return total + value;
  }, 0);
  return Math.ceil(sum * rule.coverageDays / rule.requiredDailySamples);
}

async function loadPlan() {
  const selection = JSON.parse(await readFile(SELECTION_PATH, 'utf8'));
  if (selection.status !== 'propuesta-pendiente-joseph') throw new Error('Estado de selección inesperado; revise aprobación explícita antes de cambiarlo.');
  if (selection.fixture !== FIXTURE || !selection.orders?.length) throw new Error('La selección no corresponde al fixture esperado.');
  const [manifest, pointCsv, warehouseCsv, productCsv, consumptionCsv] = await Promise.all([
    readFile(resolve(FIXTURE_DIR, 'manifest.json'), 'utf8').then(JSON.parse),
    readFile(resolve(FIXTURE_DIR, 'puntos.csv'), 'utf8').then(parseCsv),
    readFile(resolve(FIXTURE_DIR, 'bodegas.csv'), 'utf8').then(parseCsv),
    readFile(resolve(FIXTURE_DIR, 'productos.csv'), 'utf8').then(parseCsv),
    readFile(resolve(FIXTURE_DIR, 'consumos.csv'), 'utf8').then(parseCsv)
  ]);
  if (manifest.dataset !== FIXTURE) throw new Error('El manifiesto local no corresponde al fixture.');
  const fileHashes = {};
  for (const file of Object.keys(manifest.sha256).sort()) {
    const bytes = await readFile(resolve(FIXTURE_DIR, file));
    const rawHash = sha256(bytes);
    const normalizedHash = sha256(bytes.toString('utf8').replace(/\r\n/g, '\n'));
    if (rawHash !== manifest.sha256[file] && normalizedHash !== manifest.sha256[file]) throw new Error(`Hash de fixture distinto al manifiesto: ${file}.`);
    fileHashes[file] = manifest.sha256[file];
  }
  const points = new Map(pointCsv.map(point => [point.code, point]));
  const products = new Map(productCsv.map(product => [product.sku, product]));
  const orders = selection.orders.map(order => {
    if (!points.has(order.pointCode)) throw new Error(`No existe el punto ${order.pointCode}.`);
    const lines = order.productSkus.map(sku => {
      if (!products.has(sku)) throw new Error(`No existe el producto ${sku}.`);
      const quantity = quantityFor(consumptionCsv, order, sku, selection.demandRule);
      if (quantity <= 0) throw new Error(`${order.pointCode}/${sku}: cantidad derivada no positiva.`);
      return { sku, quantity };
    });
    return { ...order, point: points.get(order.pointCode), lines };
  });
  const referenceWarehouse = warehouseCsv.find(warehouse => warehouse.code === selection.referenceWarehouseCode);
  if (!referenceWarehouse) throw new Error('La bodega de referencia no existe en el fixture.');
  return { selection, orders, referenceWarehouse, fixtureHashes: fileHashes };
}

async function loadConfig(path) {
  if (!path) throw new Error('Proporcione --config con la configuración logística local efectiva.');
  const config = JSON.parse(await readFile(path, 'utf8'));
  const logistics = config.Logistics ?? config;
  const serviceCost = logistics.ServiceCost;
  const carrier = logistics.ContractedCarrier;
  const recommendation = logistics.Recommendation;
  const trafficBands = logistics.TrafficBands;
  if (!serviceCost || serviceCost.FuelPricePerGallonUsd == null || !serviceCost.FuelPriceEffectiveDate || !serviceCost.FuelPriceSource || !serviceCost.VehicleProfiles || !Object.keys(serviceCost.VehicleProfiles).length) {
    throw new Error('La configuración no contiene ServiceCost con precio, fecha, fuente y perfiles de vehículo.');
  }
  if (!carrier || carrier.IsEnabled === false || !carrier.Name || !(Number(carrier.PerKmUsd) > 0)) throw new Error('ContractedCarrier falta o está deshabilitado/incompleto.');
  if (!recommendation || typeof recommendation !== 'object') throw new Error('Falta Logistics.Recommendation.');
  if (!Array.isArray(trafficBands) || trafficBands.length === 0) throw new Error('Falta configurar Logistics.TrafficBands.');
  const relevant = { ServiceCost: serviceCost, ContractedCarrier: carrier, Recommendation: recommendation, TrafficBands: trafficBands, Clock: logistics.Clock ?? null };
  return { sha256: sha256(canonical(relevant)), sections: relevant };
}

function localHttpUrl(url) {
  if (url.protocol !== 'http:') return false;
  const host = url.hostname.toLowerCase();
  return host === 'localhost' || host === '::1' || /^127(?:\.\d{1,3}){3}$/.test(host);
}

async function callApi(base, token, path, { method = 'GET', body } = {}) {
  const response = await fetch(new URL(path, base), {
    method,
    headers: { Authorization: `Bearer ${token}`, Accept: 'application/json', ...(body ? { 'Content-Type': 'application/json' } : {}) },
    ...(body ? { body: JSON.stringify(body) } : {})
  });
  const raw = await response.text();
  let value;
  try { value = raw ? JSON.parse(raw) : null; } catch { value = raw; }
  if (!response.ok) throw new Error(`${method} ${path}: HTTP ${response.status}: ${typeof value === 'string' ? value : JSON.stringify(value)}`);
  return { value, raw };
}

function zonedTimeToUtc(date, time, timeZone) {
  const [year, month, day] = date.split('-').map(Number);
  const [hour, minute] = time.split(':').map(Number);
  const target = Date.UTC(year, month - 1, day, hour, minute);
  let guess = target;
  const fmt = new Intl.DateTimeFormat('en-CA', { timeZone, year: 'numeric', month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit', hourCycle: 'h23' });
  for (let i = 0; i < 3; i += 1) {
    const parts = Object.fromEntries(fmt.formatToParts(new Date(guess)).map(part => [part.type, part.value]));
    const shown = Date.UTC(Number(parts.year), Number(parts.month) - 1, Number(parts.day), Number(parts.hour), Number(parts.minute));
    guess += target - shown;
  }
  return new Date(guess).toISOString();
}

async function execute(args, plan, configInfo) {
  if (!localHttpUrl(args.api)) throw new Error('--execute solo admite HTTP en localhost/127.0.0.1/::1.');
  if (process.env.DEMO_LOCAL_RESET_ACK !== 'I_CONFIRM_LOCAL_DEMO_DATABASE') throw new Error('Antes de ejecutar, confirme una base local aislada definiendo DEMO_LOCAL_RESET_ACK=I_CONFIRM_LOCAL_DEMO_DATABASE.');
  if (plan.selection.status === 'propuesta-pendiente-joseph' && !args.allowProvisional) throw new Error('La selección está pendiente de Joseph. Use --allow-provisional-selection solo para una prueba local provisional; la corrida resultante no es evidencia final.');
  const token = process.env.DEMO_API_TOKEN;
  if (!token) throw new Error('Defina DEMO_API_TOKEN con un token local de Planificador.');
  const health = await fetch(new URL('/health/live', args.api));
  if (!health.ok) throw new Error(`La API local no está disponible: HTTP ${health.status}.`);

  console.log('La ejecución reseteará el fixture demo-quito-planificacion de la organización demo local y creará cuatro pedidos sintéticos.');
  console.log(`Referencia narrativa: ${plan.referenceWarehouse.code}. Bodegas reales de origen: las decide el motor.`);
  const reset = await callApi(args.api, token, `/api/v1/demo/reset?fixture=${FIXTURE}`, { method: 'POST' });
  if (reset.value?.applied !== true) throw new Error('El reset no confirmó applied=true; no se crearán pedidos.');

  const [pointsResponse, productsResponse] = await Promise.all([
    callApi(args.api, token, '/api/v1/imports/data/service-points'),
    callApi(args.api, token, '/api/v1/imports/data/products')
  ]);
  const points = new Map((pointsResponse.value ?? []).map(row => [row.code, row]));
  const products = new Map((productsResponse.value ?? []).map(row => [row.sku, row]));
  const created = [];
  for (const order of plan.orders) {
    const point = points.get(order.pointCode);
    if (!point?.id) throw new Error(`La API no resolvió el ID recién importado de ${order.pointCode}.`);
    const requiredDateUtc = zonedTimeToUtc(plan.selection.plannedDate, order.point.window_end, plan.selection.timeZone);
    const request = {
      destinationServicePointId: point.id,
      requiredDateUtc,
      priority: order.priority,
      sourceType: 'Manual',
      notes: `issue-276:${order.key}; fixture=${FIXTURE}; regla=consumo-7d-cobertura-3d`,
      lines: order.lines.map(line => {
        const product = products.get(line.sku);
        if (!product?.id) throw new Error(`La API no resolvió el ID recién importado de ${line.sku}.`);
        return { productId: product.id, quantity: line.quantity };
      })
    };
    const response = await callApi(args.api, token, '/api/v1/delivery-orders', { method: 'POST', body: request });
    if (!response.value?.id) throw new Error(`La creación de ${order.key} no devolvió ID.`);
    created.push({ key: order.key, pointCode: order.pointCode, orderId: response.value.id, orderNumber: response.value.orderNumber, request });
  }

  const suggestionRequest = { orderIds: created.map(order => order.orderId), plannedDate: plan.selection.plannedDate };
  const suggestion = await callApi(args.api, token, '/api/v1/route-plans/suggest', { method: 'POST', body: suggestionRequest });
  const comparison = suggestion.value?.comparison;
  if (!Array.isArray(suggestion.value?.options) || suggestion.value.options.length === 0) throw new Error('El motor no devolvió opciones; no se guardó la corrida.');
  if (!comparison?.options?.length) throw new Error('El motor no devolvió comparison de #241; no se guardó la corrida.');
  if (!comparison.options.some(option => option.carrierType === 'Contratada')) throw new Error('La respuesta no contiene C contratada con la configuración local; no se guardó como corrida A/B/C.');
  if (!comparison.fuelPrice?.date || !comparison.fuelPrice?.source || comparison.fuelPrice?.value == null) throw new Error('Falta precio de combustible fechado y con fuente; no se guardó la corrida.');

  let commit;
  let gitStatus;
  let patchHash;
  try {
    commit = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: ROOT, encoding: 'utf8' }).trim();
    gitStatus = execFileSync('git', ['status', '--porcelain'], { cwd: ROOT, encoding: 'utf8' });
    const trackedPatch = execFileSync('git', ['diff', 'HEAD', '--binary'], { cwd: ROOT });
    const untrackedPaths = execFileSync('git', ['ls-files', '--others', '--exclude-standard', '-z'], { cwd: ROOT, encoding: 'utf8' })
      .split('\0').filter(Boolean).sort();
    const untracked = [];
    for (const relativePath of untrackedPaths) {
      untracked.push(Buffer.from(relativePath), await readFile(resolve(ROOT, relativePath)));
    }
    patchHash = sha256(Buffer.concat([trackedPatch, ...untracked]));
  } catch { throw new Error('No se pudo registrar la versión Git que produjo la respuesta.'); }
  const envelope = {
    metadata: {
      commit,
      capturedAtUtc: new Date().toISOString(),
      fixture: FIXTURE,
      fixtureHashes: plan.fixtureHashes,
      configSha256: configInfo.sha256,
      configSections: configInfo.sections,
      selectionPath: 'docs/specs/006-validacion-pitch-entrega/seleccion-issue-276.propuesta.json',
      selection: plan.selection,
      selectionStatus: plan.selection.status,
      request: suggestionRequest,
      createdOrders: created.map(({ key, pointCode, orderId, orderNumber }) => ({ key, pointCode, orderId, orderNumber })),
      operationalDate: comparison.operationalDate ?? null,
      workingTreeDirty: Boolean(gitStatus.trim()),
      workingTreePatchSha256: patchHash
    },
    response: suggestion.value,
    responseRawJson: suggestion.raw
  };
  const serialized = `${JSON.stringify(envelope, null, 2)}\n`;
  const outputs = args.output === DEFAULT_OUTPUT ? [args.output, PUBLIC_OUTPUT] : [args.output];
  for (const output of outputs) {
    await mkdir(dirname(output), { recursive: true });
    const temporary = `${output}.tmp-${process.pid}`;
    await writeFile(temporary, serialized, { flag: 'wx' });
    await rename(temporary, output);
  }
  console.log(`Corrida guardada: ${args.output}`);
  if (outputs.length > 1) console.log(`Copia pública para selección manual: ${PUBLIC_OUTPUT}`);
  console.log(`Commit ${commit}; parche local ${patchHash}; configuración ${configInfo.sha256}; pedidos ${created.map(order => order.orderNumber).join(', ')}.`);
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  if (args.help) return printHelp();
  const plan = await loadPlan();
  const configInfo = args.config ? await loadConfig(args.config) : null;
  console.log(`Fixture: ${FIXTURE}; bodega de referencia: ${plan.referenceWarehouse.code}; pedidos: ${plan.orders.length}; líneas: ${plan.orders.reduce((count, order) => count + order.lines.length, 0)}.`);
  for (const order of plan.orders) console.log(`  ${order.key} → ${order.pointCode} (${order.point.window_end} local): ${order.lines.map(line => `${line.sku}=${line.quantity}`).join(', ')}`);
  if (configInfo) console.log(`SHA-256 configuración logística: ${configInfo.sha256}`);
  else if (args.execute) throw new Error('La ejecución requiere --config.');
  if (!args.execute) return console.log('Validación local completa. No se llamó a la API ni se modificó la base.');
  await execute(args, plan, configInfo);
}

main().catch(error => {
  console.error(`Error: ${error.message}`);
  process.exitCode = 1;
});
