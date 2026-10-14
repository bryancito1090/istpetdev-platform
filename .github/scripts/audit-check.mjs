import { spawnSync } from 'node:child_process';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { resolve, dirname } from 'node:path';

export function evaluateAudit({ status, stdout, stderr, exceptions, lockfile, now = new Date() }) {
  if (status !== 0 && status !== 1) {
    return { ok: false, error: `Código de salida inesperado o error de red: ${status}. Stderr: ${stderr || ''}` };
  }

  let audit;
  try {
    audit = typeof stdout === 'string' ? JSON.parse(stdout) : stdout;
  } catch (err) {
    return { ok: false, error: `Error al parsear salida JSON de npm audit: ${err.message}` };
  }

  if (audit.error) {
    return { ok: false, error: `Error en reporte de auditoria npm: ${audit.error.code || audit.error.summary || JSON.stringify(audit.error)}` };
  }

  if (audit.auditReportVersion !== 2) {
    return { ok: false, error: `auditReportVersion debe ser 2, recibido: ${audit.auditReportVersion}` };
  }

  const total = audit.metadata?.vulnerabilities?.total;
  if (!Number.isInteger(total)) {
    return { ok: false, error: 'metadata.vulnerabilities.total no es un entero válido' };
  }

  if (status === 0) {
    if (total === 0) {
      return { ok: true, total: 0 };
    }
    return { ok: false, error: 'npm audit devolvió status 0 pero total de vulnerabilidades > 0' };
  }

  if (total === 0) {
    return { ok: false, error: 'npm audit devolvió status 1 pero total de vulnerabilidades es 0' };
  }

  // Validar excepciones
  if (!exceptions || exceptions.schemaVersion !== 1 || !Array.isArray(exceptions.exceptions)) {
    return { ok: false, error: 'exceptions.json inválido o schemaVersion != 1' };
  }

  const approvedExceptions = [];
  for (const exc of exceptions.exceptions) {
    if (exc.status !== 'approved') {
      return { ok: false, error: `Excepción para ${exc.package} tiene status '${exc.status}', debe ser 'approved'` };
    }
    if (!exc.advisoryId || !exc.package || !exc.vulnerableRange || !exc.nodePath || !exc.installedVersion) {
      return { ok: false, error: `Excepción para ${exc.package} incompleta en campos obligatorios` };
    }
    if (!exc.approvedBy || !exc.approvalReference || !exc.approvedAt || !exc.expiresAt) {
      return { ok: false, error: `Excepción para ${exc.package} carece de aprobador o fechas formales` };
    }

    const expiresAtDate = new Date(exc.expiresAt);
    if (isNaN(expiresAtDate.getTime()) || expiresAtDate <= now) {
      return { ok: false, error: `Excepción vencida o fecha inválida para ${exc.package}: ${exc.expiresAt}` };
    }

    approvedExceptions.push(exc);
  }

  // Recorrer vulnerabilidades del reporte
  const vulnerabilities = audit.vulnerabilities || {};
  for (const [pkgName, vuln] of Object.entries(vulnerabilities)) {
    const directVias = (vuln.via || []).filter(v => typeof v === 'object' && v !== null && v.url);
    if (directVias.length === 0) {
      // Es puramente transitiva dependiente de otra vulnerabilidad directa
      continue;
    }

    for (const via of directVias) {
      const advisoryId = via.url ? via.url.split('/').pop() : null;
      const nodes = vuln.nodes || [];

      for (const nodePath of nodes) {
        const lockPkg = lockfile?.packages?.[nodePath];
        const installedVersion = lockPkg?.version;

        const matchingException = approvedExceptions.find(e =>
          e.package === pkgName &&
          (!advisoryId || e.advisoryId === advisoryId) &&
          e.nodePath === nodePath &&
          (!installedVersion || e.installedVersion === installedVersion)
        );

        if (!matchingException) {
          return {
            ok: false,
            error: `Vulnerabilidad sin excepcion aprobada: ${pkgName} (${via.title || advisoryId}) en ${nodePath} v${installedVersion || 'unknown'}`
          };
        }
      }
    }
  }

  return { ok: true, total, coveredByExceptions: approvedExceptions.length };
}

// Ejecución como script CLI
if (process.argv[1] && process.argv[1].endsWith('audit-check.mjs')) {
  const rootDir = process.cwd();
  const exceptionsPath = resolve(rootDir, 'security/spec000/exceptions.json');
  const lockfilePath = resolve(rootDir, 'package-lock.json');
  const artifactDir = resolve(rootDir, 'artifacts/spec000');
  const artifactFile = resolve(artifactDir, 'audit-ci.json');

  let exceptions;
  try {
    exceptions = JSON.parse(readFileSync(exceptionsPath, 'utf8'));
  } catch (err) {
    console.error(`❌ Error al leer exceptions.json en ${exceptionsPath}: ${err.message}`);
    process.exit(1);
  }

  let lockfile;
  try {
    lockfile = JSON.parse(readFileSync(lockfilePath, 'utf8'));
  } catch (err) {
    console.error(`❌ Error al leer package-lock.json en ${lockfilePath}: ${err.message}`);
    process.exit(1);
  }

  console.log('🔍 Ejecutando npm audit --json --workspaces --include-workspace-root...');
  const auditProc = spawnSync('npm', ['audit', '--json', '--workspaces', '--include-workspace-root'], {
    encoding: 'utf8',
    maxBuffer: 10 * 1024 * 1024,
    shell: true
  });

  try {
    mkdirSync(artifactDir, { recursive: true });
    writeFileSync(artifactFile, auditProc.stdout || '{}', 'utf8');
  } catch (e) {
    console.warn(`⚠️ No se pudo guardar artefacto efímero en ${artifactFile}: ${e.message}`);
  }

  const result = evaluateAudit({
    status: auditProc.status,
    stdout: auditProc.stdout,
    stderr: auditProc.stderr,
    exceptions,
    lockfile,
    now: new Date()
  });

  if (!result.ok) {
    console.error(`\n❌ COMPUERTA DE AUDITORÍA FALLIDA:\n${result.error}\n`);
    process.exit(1);
  }

  console.log(`\n✅ Auditoría superada con éxito. Hallazgos cubiertos por excepciones aprobadas vigentes (${result.total || 0} avisos autorizados).`);
  process.exit(0);
}
