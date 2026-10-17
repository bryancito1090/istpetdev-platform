#!/usr/bin/env node
import { execSync } from 'node:child_process';
import fs from 'node:fs';
import net from 'node:net';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '../..');
const apiDir = path.resolve(__dirname, '../../src/Api');
const envFile = path.resolve(rootDir, '.env');

function loadEnvConfig() {
  if (!fs.existsSync(envFile)) {
    return {};
  }
  const content = fs.readFileSync(envFile, 'utf-8');
  const env = {};
  for (const line of content.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith('#')) continue;
    const idx = trimmed.indexOf('=');
    if (idx > 0) {
      const key = trimmed.slice(0, idx).trim();
      const val = trimmed.slice(idx + 1).trim();
      env[key] = val;
    }
  }
  return env;
}

function parseConnectionString(connStr) {
  if (!connStr) return {};
  const result = {};
  const parts = connStr.split(';');
  for (const part of parts) {
    const [k, v] = part.split('=');
    if (k && v) {
      result[k.trim().toLowerCase()] = v.trim();
    }
  }
  return result;
}

function maskConnectionString(connStr) {
  if (!connStr) return '(no configurada)';
  return connStr.replace(/(password=)([^;]+)/i, '$1******');
}

function getCloudConfig() {
  const env = loadEnvConfig();
  const connStr = env.ConnectionStrings__Main || '';
  const parsed = parseConnectionString(connStr);

  return {
    host: env.DATABASE_HOST || parsed.host || 'localhost',
    port: parseInt(env.DATABASE_PORT || parsed.port || '5432', 10),
    database: env.DATABASE_NAME || parsed.database || 'istpetdev_dev_01',
    user: env.DATABASE_USER || parsed.username || 'migrator_dev_01',
    password: env.DATABASE_PASSWORD || parsed.password || '',
    rawConnection: connStr
  };
}

function getLocalConfig() {
  const env = loadEnvConfig();
  return {
    host: process.env.LOCAL_DATABASE_HOST || env.LOCAL_DATABASE_HOST || 'localhost',
    port: parseInt(process.env.LOCAL_DATABASE_PORT || env.LOCAL_DATABASE_PORT || '15432', 10),
    database: process.env.LOCAL_DATABASE_NAME || env.LOCAL_DATABASE_NAME || 'istpetdev_dev_01',
    user: process.env.LOCAL_DATABASE_USER || env.LOCAL_DATABASE_USER || 'migrator_dev_01',
    password: process.env.LOCAL_DATABASE_PASSWORD || env.LOCAL_DATABASE_PASSWORD || process.env.POSTGRES_PASSWORD || 'postgres_dev_password_local'
  };
}

function checkTcp(host, port, timeoutMs = 2500) {
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
  const cloud = getCloudConfig();

  console.log('\n--- Estado de Conexion de Base de Datos ---');
  if (currentSecret) {
    if (currentSecret.includes('localhost') || currentSecret.includes('127.0.0.1')) {
      console.log('Modo Activo: DOCKER LOCAL (localhost:15432)');
    } else if (currentSecret.includes(cloud.host)) {
      console.log(`Modo Activo: DESARROLLO CENTRALIZADO (Nube / .env - ${cloud.host}:${cloud.port})`);
    } else {
      console.log('Modo Activo: PERSONALIZADO (Configurado en user-secrets)');
    }
    console.log(`Cadena de conexion activa: ${maskConnectionString(currentSecret)}`);
  } else {
    console.log(`Modo Activo: DESARROLLO CENTRALIZADO (.env - ${cloud.host}:${cloud.port})`);
    console.log(`Cadena de conexion (.env): ${maskConnectionString(cloud.rawConnection)}`);
  }
  console.log('-------------------------------------------\n');
}

async function switchToDev(customHost, customPort) {
  const cloud = getCloudConfig();
  const host = customHost || cloud.host;
  const port = parseInt(customPort || cloud.port, 10);
  const { database, user, password } = cloud;

  console.log(`\nVerificando conectividad con servidor de desarrollo centralizado (${host}:${port})...`);
  const isOnline = await checkTcp(host, port, 3000);

  if (!isOnline) {
    console.warn(`[AVISO] El puerto ${port} en ${host} no responde en este momento.`);
    console.warn('Verifica tu conexion a Internet o el Security Group en la instancia de desarrollo.');
  } else {
    console.log(`[OK] Conectividad TCP verificada con servidor de desarrollo (${host}:${port}).`);
  }

  if (!password && (!cloud.rawConnection || !cloud.rawConnection.toLowerCase().includes('password='))) {
    console.warn('[AVISO] DATABASE_PASSWORD no esta configurada en .env ni en variables de entorno.');
  }

  const connStr = cloud.rawConnection || `Host=${host};Port=${port};Database=${database};Username=${user};Password=${password}`;
  execSync(`dotnet user-secrets set "ConnectionStrings:Main" "${connStr}"`, { cwd: apiDir, stdio: 'inherit' });

  console.log(`\n[EXITO] Backend configurado para desarrollo centralizado (${host}:${port}).`);
  console.log('Para iniciar tu backend: npm run dev:api\n');
}

async function switchToLocal() {
  const { host, port, database, user, password } = getLocalConfig();
  const connStr = `Host=${host};Port=${port};Database=${database};Username=${user};Password=${password};Include Error Detail=true;`;
  execSync(`dotnet user-secrets set "ConnectionStrings:Main" "${connStr}"`, { cwd: apiDir, stdio: 'inherit' });

  console.log(`\n[EXITO] Backend configurado para DOCKER LOCAL (${host}:${port}).`);
  console.log('Recuerda iniciar tus contenedores locales con: npm run docker:up');
  console.log('Para iniciar tu backend: npm run dev:api\n');
}

async function main() {
  const [,, command, arg1, arg2] = process.argv;

  switch (command) {
    case 'dev':
    case 'cloud':
    case 'aws':
      await switchToDev(arg1, arg2);
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
  console.error('\nError al gestionar base de datos:', err.message);
  process.exit(1);
});
