import tsParser from '@typescript-eslint/parser';
import boundaries from 'eslint-plugin-boundaries';

export default [
  {
    ignores: [
      '**/node_modules/**',
      '**/dist/**',
      '**/.angular/**',
      '**/*.d.ts'
    ]
  },
  {
    files: ['apps/web/src/**/*.ts'],
    languageOptions: {
      parser: tsParser,
      parserOptions: {
        ecmaVersion: 'latest',
        sourceType: 'module'
      }
    },
    plugins: {
      boundaries
    },
    settings: {
      'import/resolver': {
        typescript: {
          alwaysTryTypes: true,
          project: ['./apps/web/tsconfig.json']
        }
      },
      'boundaries/elements': [
        {
          type: 'app',
          pattern: 'apps/web/src/app/**/*',
          partialMatch: false
        },
        {
          type: 'pages',
          pattern: 'apps/web/src/pages/*',
          capture: ['slice']
        },
        {
          type: 'widgets',
          pattern: 'apps/web/src/widgets/*',
          capture: ['slice']
        },
        {
          type: 'features',
          pattern: 'apps/web/src/features/*',
          capture: ['slice']
        },
        {
          type: 'entities',
          pattern: 'apps/web/src/entities/*',
          capture: ['slice']
        },
        {
          type: 'shared',
          pattern: 'apps/web/src/shared/**/*',
          partialMatch: false
        }
      ],
      'boundaries/ignore': [
        '**/*.spec.ts',
        '**/*.test.ts'
      ]
    },
    rules: {
      // Regla de Oro FSD: Jerarquía estricta y aislamiento de slices
      'boundaries/dependencies': [
        2,
        {
          default: 'disallow',
          message: 'Violación FSD: "{{file.type}}" no puede importar desde "{{dependency.type}}". Jerarquía permitida: app -> pages -> widgets -> features -> entities -> shared.',
          policies: [
            {
              from: { element: { type: 'app' } },
              allow: [
                { to: { element: { type: ['app', 'pages', 'widgets', 'features', 'entities', 'shared'] } } }
              ]
            },
            {
              from: { element: { type: 'pages' } },
              allow: [
                { to: { element: { type: ['widgets', 'features', 'entities', 'shared'] } } }
              ]
            },
            {
              from: { element: { type: 'widgets' } },
              allow: [
                // Permite importar archivos dentro del mismo widget
                { to: { element: { type: 'widgets', captured: { slice: '{{from.captured.slice}}' } } } },
                { to: { element: { type: ['features', 'entities', 'shared'] } } }
              ]
            },
            {
              from: { element: { type: 'features' } },
              allow: [
                // Permite importar archivos dentro de la misma feature, prohíbe importar entre features distintas
                { to: { element: { type: 'features', captured: { slice: '{{from.captured.slice}}' } } } },
                { to: { element: { type: ['entities', 'shared'] } } }
              ]
            },
            {
              from: { element: { type: 'entities' } },
              allow: [
                // Permite importar archivos dentro de la misma entity
                { to: { element: { type: 'entities', captured: { slice: '{{from.captured.slice}}' } } } },
                { to: { element: { type: ['shared'] } } }
              ]
            },
            {
              from: { element: { type: 'shared' } },
              allow: [
                { to: { element: { type: 'shared' } } }
              ]
            }
          ]
        }
      ],

      // Regla FSD 2: API pública obligatoria (prohíbe deep imports a slices externos)
      'boundaries/entry-point': [
        2,
        {
          default: 'allow',
          message: 'Violación FSD: Acceso prohibido a las partes internas de "{{dependency.type}}". Debe importar a través de su API pública (index.ts).',
          policies: [
            {
              target: { element: { type: ['pages', 'widgets', 'features', 'entities'] } },
              allow: [
                'index.ts',
                'index.js'
              ]
            }
          ]
        }
      ]
    }
  }
];
