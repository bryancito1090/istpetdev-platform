#!/usr/bin/env node
// Verificación de compilación de PWA Campo (/campo/) para CI/CD (Issue #52)
import { existsSync, readFileSync, statSync } from 'node:fs';
import { resolve, join } from 'node:path';

const distDir = resolve(process.argv[2] || 'apps/mobile/dist/mobile/browser');

const errors = [];

function assert(condition, message) {
  if (!condition) {
    errors.push(message);
  }
}

console.log(`Verificando artefactos de PWA Campo en: ${distDir}`);

if (!existsSync(distDir)) {
  console.error(`Error: el directorio de distribución no existe: ${distDir}`);
  process.exit(1);
}

// 1. Validar baseHref en index.html
const indexHtmlPath = join(distDir, 'index.html');
if (!existsSync(indexHtmlPath)) {
  errors.push(`index.html no encontrado en ${indexHtmlPath}`);
} else {
  const indexContent = readFileSync(indexHtmlPath, 'utf8');
  assert(
    indexContent.includes('<base href="/campo/">'),
    'index.html debe contener <base href="/campo/">'
  );
}

// 2. Validar ngsw.json
const ngswPath = join(distDir, 'ngsw.json');
if (!existsSync(ngswPath)) {
  errors.push(`ngsw.json no encontrado en ${ngswPath}`);
} else {
  try {
    const ngsw = JSON.parse(readFileSync(ngswPath, 'utf8'));
    assert(
      ngsw.index === '/campo/index.html',
      `ngsw.json debe tener index: "/campo/index.html" (actual: "${ngsw.index}")`
    );
    assert(
      Array.isArray(ngsw.assetGroups) && ngsw.assetGroups.length > 0,
      'ngsw.json debe contener assetGroups no vacíos'
    );
    assert(
      ngsw.hashTable && typeof ngsw.hashTable === 'object' && Object.keys(ngsw.hashTable).length > 0,
      'ngsw.json debe contener un hashTable con hashes de assets'
    );
    assert(
      ngsw.hashTable && ngsw.hashTable['/campo/index.html'],
      'ngsw.json hashTable debe incluir entrada para "/campo/index.html"'
    );
  } catch (err) {
    errors.push(`Error al parsear ngsw.json: ${err.message}`);
  }
}

// 3. Validar manifest.webmanifest
const manifestPath = join(distDir, 'manifest.webmanifest');
if (!existsSync(manifestPath)) {
  errors.push(`manifest.webmanifest no encontrado en ${manifestPath}`);
} else {
  try {
    const manifest = JSON.parse(readFileSync(manifestPath, 'utf8'));
    assert(
      manifest.scope === './',
      `manifest.webmanifest debe tener scope: "./" (actual: "${manifest.scope}")`
    );
    assert(
      manifest.start_url === './home',
      `manifest.webmanifest debe tener start_url: "./home" (actual: "${manifest.start_url}")`
    );
    assert(
      Boolean(manifest.id),
      'manifest.webmanifest debe declarar un id estable'
    );
    assert(
      Array.isArray(manifest.icons) && manifest.icons.length > 0,
      'manifest.webmanifest debe declarar íconos'
    );

    if (Array.isArray(manifest.icons)) {
      const has192Any = manifest.icons.some(
        (icon) =>
          icon.sizes === '192x192' &&
          (icon.purpose === 'any' || !icon.purpose || icon.purpose.includes('any'))
      );
      const has512Any = manifest.icons.some(
        (icon) =>
          icon.sizes === '512x512' &&
          (icon.purpose === 'any' || !icon.purpose || icon.purpose.includes('any'))
      );
      const has512Maskable = manifest.icons.some(
        (icon) =>
          icon.sizes === '512x512' &&
          Boolean(icon.purpose && icon.purpose.includes('maskable'))
      );

      assert(has192Any, 'manifest.webmanifest debe incluir ícono 192x192 con purpose any');
      assert(has512Any, 'manifest.webmanifest debe incluir ícono 512x512 con purpose any');
      assert(has512Maskable, 'manifest.webmanifest debe incluir ícono 512x512 con purpose maskable');

      // 4. Verificar existencia física de archivos de íconos declarados
      for (const icon of manifest.icons) {
        if (!icon.src) continue;
        const iconPath = join(distDir, icon.src);
        if (!existsSync(iconPath)) {
          errors.push(`Archivo de ícono declarado no existe físicamente: ${icon.src} (${iconPath})`);
        } else {
          const stats = statSync(iconPath);
          if (stats.size === 0) {
            errors.push(`Archivo de ícono está vacío: ${icon.src}`);
          }
        }
      }
    }
  } catch (err) {
    errors.push(`Error al parsear manifest.webmanifest: ${err.message}`);
  }
}

if (errors.length > 0) {
  console.error('\nFalló la verificación de build PWA Campo:');
  errors.forEach((err) => console.error(`  - ${err}`));
  process.exit(1);
}

console.log('✓ Verificación de build PWA Campo exitosa:');
console.log('  - <base href="/campo/"> en index.html');
console.log('  - ngsw.json con index /campo/index.html y hashes en assetGroups');
console.log('  - manifest.webmanifest con scope/start_url relativos, id e íconos 192/512 (any + maskable)');
console.log('  - Archivos físicos de íconos verificados en dist');
process.exit(0);
