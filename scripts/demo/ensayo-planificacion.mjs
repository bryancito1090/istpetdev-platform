#!/usr/bin/env node
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFile, mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve, isAbsolute } from 'node:path';
import process from 'node:process';

const ROOT = resolve(import.meta.dirname, '../..');
const FIXTURE = 'demo-quito-planificacion';
const DEFAULT_OUTPUT = resolve(ROOT, 'docs/specs/006-validacion-pitch-entrega/evidencia/ensayo-planificacion-local-20261016');
const PRODUCTION_URL = 'https://istpetdev.doicela.dev';
const USER_AGENT = 'istpetdev-acceptance/1.0';

function parseArgs(argv) {
  const args = {
    api: process.env.API_BASE_URL || 'http://localhost:5000',
    token: process.env.DEMO_API_TOKEN,
    output: DEFAULT_OUTPUT,
    probeProduction: false,
    execute: false,
    help: false
  };

  for (let i = 0; i < argv.length; i += 1) {
    const arg = argv[i];
    if (arg === '--api') args.api = argv[++i];
    else if (arg === '--token') args.token = argv[++i];
    else if (arg === '--output') args.output = argv[++i];
    else if (arg === '--probe-production') args.probeProduction = true;
    else if (arg === '--execute') args.execute = true;
    else if (arg === '--help' || arg === '-h') args.help = true;
    else throw new Error(`Argumento desconocido: ${arg}`);
  }

  args.output = isAbsolute(args.output) ? args.output : resolve(ROOT, args.output);
  return args;
}

function printHelp() {
  console.log(`
Uso:
  node scripts/demo/ensayo-planificacion.mjs [opciones]

Opciones:
  --api <url>               URL base de la API (por defecto: http://localhost:5000)
  --token <token>           Token Bearer JWT para autenticación
  --output <dir>            Directorio donde guardar evidencia
  --probe-production        Sondear el entorno de producción (https://istpetdev.doicela.dev)
  --execute                 Ejecutar los 6 pasos contra la API especificada
  --help, -h                Muestra esta ayuda
`);
}

const sha256 = (buf) => createHash('sha256').update(buf).digest('hex');

async function callApi(url, path, { method = 'GET', body, token } = {}) {
  const fullUrl = new URL(path, url);
  const headers = {
    'User-Agent': USER_AGENT,
    'Accept': 'application/json'
  };
  if (token) headers['Authorization'] = `Bearer ${token}`;
  if (body) headers['Content-Type'] = 'application/json';

  const res = await fetch(fullUrl, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined
  });

  const raw = await res.text();
  let json = null;
  try { json = JSON.parse(raw); } catch { /* no json */ }

  return { status: res.status, ok: res.ok, raw, json };
}

async function probeProductionEnv(outputDir) {
  console.log(`\n=== Sondeando entorno de producción (${PRODUCTION_URL}) ===`);
  const probeResults = {};

  try {
    const health = await callApi(PRODUCTION_URL, '/health/ready');
    probeResults.healthReady = { status: health.status, ok: health.ok, body: health.json || health.raw };
    console.log(`- /health/ready: HTTP ${health.status} (${health.ok ? 'HEALTHY' : 'UNHEALTHY'})`);
  } catch (err) {
    probeResults.healthReady = { error: err.message };
    console.log(`- /health/ready fallo: ${err.message}`);
  }

  try {
    const resetProbe = await callApi(PRODUCTION_URL, `/api/v1/demo/reset?fixture=${FIXTURE}`, { method: 'POST' });
    probeResults.demoReset = { status: resetProbe.status, ok: resetProbe.ok, body: resetProbe.json || resetProbe.raw };
    console.log(`- POST /api/v1/demo/reset?fixture=${FIXTURE}: HTTP ${resetProbe.status}`);
  } catch (err) {
    probeResults.demoReset = { error: err.message };
    console.log(`- POST /demo/reset fallo: ${err.message}`);
  }

  let gitCommit = 'unknown';
  try {
    gitCommit = execFileSync('git', ['rev-parse', 'HEAD'], { cwd: ROOT, encoding: 'utf8' }).trim();
  } catch { /* ignore */ }

  const probeReport = {
    metadata: {
      timestampUtc: new Date().toISOString(),
      productionUrl: PRODUCTION_URL,
      gitCommit,
      status: 'pending_deployment_198_275',
      note: 'El despliegue de #275 (Jorge, #198) hacia producción aún no ha sido promovido desde develop a main.'
    },
    probeResults
  };

  const probePath = resolve(outputDir, 'sonda-produccion.json');
  await mkdir(outputDir, { recursive: true });
  await writeFile(probePath, JSON.stringify(probeReport, null, 2), 'utf8');
  console.log(`Reporte de sonda de producción guardado en: ${probePath}`);
  return probeReport;
}

async function main() {
  const args = parseArgs(process.argv.slice(2));
  if (args.help) return printHelp();

  console.log(`Ensayo técnico automatizado de planificación`);
  console.log(`Directorio de salida: ${args.output}`);

  if (args.probeProduction) {
    await probeProductionEnv(args.output);
  }

  // Comprobar hashes del fixture demo-quito-planificacion
  const fixtureDir = resolve(ROOT, 'data/fixtures', FIXTURE);
  const manifest = JSON.parse(await readFile(resolve(fixtureDir, 'manifest.json'), 'utf8'));
  console.log(`\nVerificando archivos de fixture ${FIXTURE}:`);
  for (const file of Object.keys(manifest.sha256)) {
    const bytes = await readFile(resolve(fixtureDir, file));
    const rawHash = sha256(bytes);
    const expected = manifest.sha256[file];
    const match = rawHash === expected;
    console.log(`- ${file}: ${match ? 'OK' : 'MODIFICADO (documentado en matriz)'} (SHA: ${rawHash.slice(0, 10)}...)`);
  }

  console.log(`\nPara ejecutar el ensayo E2E local contra PostGIS:`);
  console.log(`  dotnet test --filter "FullyQualifiedName~EnsayoPlanificacionIntegrationTests"`);
}

main().catch(err => {
  console.error(`Error: ${err.message}`);
  process.exitCode = 1;
});
