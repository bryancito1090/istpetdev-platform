#!/usr/bin/env node
// NFR-04 de 013: falla si reaparece el login simulado en las apps.
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { extname, join } from 'node:path';

const FORBIDDEN = [
  /loginWithCredentials/,
  /useMockSession/,
  /jwt-mock/,
  /mock-bearer-token/,
  /loginAsDemo/,
  /DEMO_PROFILES/,
  /\/api\/v1\/auth\/login/,
  /auth\/login'/
];
const SKIP = new Set(['node_modules', 'dist', '.angular', 'www', 'coverage']);
const EXTENSIONS = new Set(['.ts', '.html', '.js', '.mjs']);
const roots = process.argv.slice(2).length > 0 ? process.argv.slice(2) : ['apps'];

const hits = [];
const walk = (path) => {
  for (const name of readdirSync(path)) {
    if (SKIP.has(name)) continue;
    const full = join(path, name);
    if (statSync(full).isDirectory()) walk(full);
    else if (EXTENSIONS.has(extname(name))) {
      readFileSync(full, 'utf8').split('\n').forEach((line, index) => {
        if (FORBIDDEN.some((pattern) => pattern.test(line))) hits.push(`${full}:${index + 1}`);
      });
    }
  }
};
roots.forEach(walk);

if (hits.length > 0) {
  console.error('Login simulado detectado (NFR-04 de 013):');
  hits.forEach((hit) => console.error(`  ${hit}`));
  process.exit(1);
}
console.log('Sin login simulado en', roots.join(', '));
