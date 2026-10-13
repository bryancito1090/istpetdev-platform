#!/usr/bin/env node
import { execSync } from 'node:child_process';
import net from 'node:net';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const apiDir = path.resolve(__dirname, '../../src/Api');

const DEFAULTS = {
  server: {
    host: '192.168.7.50',
    port: 15432,
    database: 'istpetdev_dev_01',
    user: 'migrator_dev_01',
    password: '12345'
  },
  local: {
    host: 'localhost',
    port: 15432,
    database: 'istpetdev_dev_01',
    user: 'postgres',
    password: 'postgres_dev_password_local'
  }
};

function checkTcp(host, port, timeoutMs = 2000) {
  return new Promise((resolve) => {
    const socket = new net.Socket();
    let isConnected = false;

    socket.setTimeout(timeoutMs);
    socket.once('connect', () => {
      isConnected = true;
      socket.destroy();
      resolve(true);
    });

    socket.once('timeout', () => {
      socket.destroy();
      resolve(false);
    });

    socket.once('error', () => {
      socket.destroy();
      resolve(false);
    });

    socket.connect(port, host);
  });
}

function getUserSecrets() {
  try {
    const output = execSync('dotnet user-secrets list', { cwd: apiDir, encoding: 'utf-8', stdio: ['ignore', 'pipe', 'ignore'] });
    const match = output.match(/ConnectionStrings:Main\s*=\s*(.+)/);
    return match ? match[1].trim() : null;
  } catch {
    return null;
  }
}

async function showStatus() {
  const currentSecret = getUserSecrets();
  console.log('\n--- Estado Actual de Base de Datos ---');
  if (currentSecret) {
    console.log('Modo: LABORATORIO / SERVIDOR (Configurado en user-secrets)');
    console.log(`Cadena activa: ${currentSecret}`);
  } else {
    console.log('Modo: CASA (DOCKER LOCAL - Leyendo appsettings.Development.json)');
    console.log(`Cadena activa: Host=${DEFAULTS.local.host};Port=${DEFAULTS.local.port};Database=${DEFAULTS.local.database};...`);
  }
  console.log('-------------------------------------\n');
}

async function switchToLab(customHost, customPort) {
  const host = customHost || DEFAULTS.server.host;
  const port = parseInt(customPort || DEFAULTS.server.port, 10);
  const { database, user, password } = DEFAULTS.server;

  console.log(`\nVerificando conectividad con servidor del LABORATORIO (${host}:${port})...`);
  const isOnline = await checkTcp(host, port, 2500);

  if (!isOnline) {
    console.warn(`[AVISO] El puerto ${port} en ${host} no responde en este momento.`);
    console.warn('Asegúrate de que PostgreSQL esté iniciado en el servidor del laboratorio y que el Firewall permita el puerto.');
  } else {
    console.log(`[OK] Conectividad TCP verificada con el Laboratorio (${host}:${port}).`);
  }

  const connStr = `Host=${host};Port=${port};Database=${database};Username=${user};Password=${password}`;
  execSync(`dotnet user-secrets set "ConnectionStrings:Main" "${connStr}"`, { cwd: apiDir, stdio: 'inherit' });

  console.log(`\n[EXITO] Backend configurado para conectar al LABORATORIO (${host}:${port}).`);
  console.log('Para iniciar tu backend: npm run dev:api\n');
}

async function switchToLocal() {
  try {
    execSync('dotnet user-secrets remove "ConnectionStrings:Main"', { cwd: apiDir, stdio: ['ignore', 'ignore', 'ignore'] });
  } catch {
    // Si no existia la clave en user-secrets, no hay problema
  }

  console.log('\n[EXITO] Backend restaurado al modo CASA (Docker local localhost:15432).');
  console.log('Recuerda iniciar tus contenedores locales con: npm run docker:up');
  console.log('Para iniciar tu backend: npm run dev:api\n');
}

async function main() {
  const [,, command, arg1, arg2] = process.argv;

  switch (command) {
    case 'lab':
    case 'server':
      await switchToLab(arg1, arg2);
      break;
    case 'local':
      await switchToLocal();
      break;
    case 'status':
    default:
      await showStatus();
      break;
  }
}

main().catch((err) => {
  console.error('\nError al cambiar modo de base de datos:', err.message);
  process.exit(1);
});
