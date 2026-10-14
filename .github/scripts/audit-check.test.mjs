import test from 'node:test';
import assert from 'node:assert/strict';
import { evaluateAudit } from './audit-check.mjs';

test('evaluateAudit rechaza cuando el status de npm audit es distinto de 0 o 1', () => {
  const result = evaluateAudit({
    status: 2,
    stdout: '',
    stderr: 'EAI_AGAIN',
    exceptions: { schemaVersion: 1, exceptions: [] },
    lockfile: { packages: {} },
    now: new Date('2026-10-14T12:00:00Z')
  });
  assert.equal(result.ok, false);
  assert.match(result.error, /código de salida inesperado|error de red/i);
});

test('evaluateAudit aprueba reporte limpio con status 0 y 0 vulnerabilidades', () => {
  const cleanAudit = JSON.stringify({
    auditReportVersion: 2,
    vulnerabilities: {},
    metadata: { vulnerabilities: { total: 0 } }
  });
  const result = evaluateAudit({
    status: 0,
    stdout: cleanAudit,
    exceptions: { schemaVersion: 1, exceptions: [] },
    lockfile: { packages: {} },
    now: new Date('2026-10-14T12:00:00Z')
  });
  assert.equal(result.ok, true);
});

test('evaluateAudit rechaza reporte con error de registro (ej: EAI_AGAIN)', () => {
  const errorReport = JSON.stringify({
    error: { code: 'EAI_AGAIN', summary: 'request to registry failed' }
  });
  const result = evaluateAudit({
    status: 1,
    stdout: errorReport,
    exceptions: { schemaVersion: 1, exceptions: [] },
    lockfile: { packages: {} },
    now: new Date('2026-10-14T12:00:00Z')
  });
  assert.equal(result.ok, false);
  assert.match(result.error, /error en reporte de auditoria/i);
});

test('evaluateAudit rechaza reporte cuando hay vulnerabilidad no cubierta por excepciones', () => {
  const auditWithVulnerability = JSON.stringify({
    auditReportVersion: 2,
    vulnerabilities: {
      'bad-pkg': {
        name: 'bad-pkg',
        severity: 'high',
        via: [
          {
            url: 'https://github.com/advisories/GHSA-xxxx-yyyy-zzzz',
            name: 'bad-pkg',
            range: '<1.0.0'
          }
        ],
        nodes: ['node_modules/bad-pkg']
      }
    },
    metadata: { vulnerabilities: { total: 1 } }
  });
  const result = evaluateAudit({
    status: 1,
    stdout: auditWithVulnerability,
    exceptions: { schemaVersion: 1, exceptions: [] },
    lockfile: {
      packages: {
        'node_modules/bad-pkg': { version: '0.9.0' }
      }
    },
    now: new Date('2026-10-14T12:00:00Z')
  });
  assert.equal(result.ok, false);
  assert.match(result.error, /sin excepcion aprobada/i);
});

test('evaluateAudit rechaza excepcion vencida o no aprobada', () => {
  const auditData = JSON.stringify({
    auditReportVersion: 2,
    vulnerabilities: {
      braces: {
        name: 'braces',
        severity: 'high',
        via: [
          {
            url: 'https://github.com/advisories/GHSA-vfj7-8cjw-p6xm',
            name: 'braces',
            range: '<=3.0.3'
          }
        ],
        nodes: ['node_modules/braces']
      }
    },
    metadata: { vulnerabilities: { total: 1 } }
  });
  const expiredException = {
    schemaVersion: 1,
    exceptions: [
      {
        status: 'approved',
        advisoryId: 'GHSA-vfj7-8cjw-p6xm',
        package: 'braces',
        vulnerableRange: '<=3.0.3',
        nodePath: 'node_modules/braces',
        installedVersion: '3.0.3',
        scope: 'development-build',
        rationale: 'Prueba',
        mitigations: ['Control en build'],
        approvedBy: 'Admin',
        approvalReference: 'Ref',
        approvedAt: '2026-01-01T00:00:00Z',
        expiresAt: '2026-05-01T00:00:00Z'
      }
    ]
  };
  const result = evaluateAudit({
    status: 1,
    stdout: auditData,
    exceptions: expiredException,
    lockfile: {
      packages: {
        'node_modules/braces': { version: '3.0.3' }
      }
    },
    now: new Date('2026-10-14T12:00:00Z')
  });
  assert.equal(result.ok, false);
  assert.match(result.error, /excepci[oó]n vencida/i);
});

test('evaluateAudit aprueba hallazgo que coincide exactamente con excepcion aprobada vigente', () => {
  const auditData = JSON.stringify({
    auditReportVersion: 2,
    vulnerabilities: {
      braces: {
        name: 'braces',
        severity: 'high',
        via: [
          {
            url: 'https://github.com/advisories/GHSA-vfj7-8cjw-p6xm',
            name: 'braces',
            range: '<=3.0.3'
          }
        ],
        nodes: ['node_modules/braces']
      }
    },
    metadata: { vulnerabilities: { total: 1 } }
  });
  const validException = {
    schemaVersion: 1,
    exceptions: [
      {
        status: 'approved',
        advisoryId: 'GHSA-vfj7-8cjw-p6xm',
        package: 'braces',
        vulnerableRange: '<=3.0.3',
        nodePath: 'node_modules/braces',
        installedVersion: '3.0.3',
        scope: 'development-build',
        rationale: 'Valido',
        mitigations: ['Mitigacion A'],
        approvedBy: 'Lead',
        approvalReference: 'RFC-1',
        approvedAt: '2026-10-14T00:00:00Z',
        expiresAt: '2026-11-30T00:00:00Z'
      }
    ]
  };
  const result = evaluateAudit({
    status: 1,
    stdout: auditData,
    exceptions: validException,
    lockfile: {
      packages: {
        'node_modules/braces': { version: '3.0.3' }
      }
    },
    now: new Date('2026-10-14T12:00:00Z')
  });
  assert.equal(result.ok, true);
});
