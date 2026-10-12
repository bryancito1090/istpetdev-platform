---
tipo: arquitectura
estado: aceptado
actualizado: 2026-10-07
tags: [istpetdev, frontend, fsd]
---

# Frontend con Feature-Sliced Design

**Decisión de IstpetDev:** usar **Feature-Sliced Design (FSD)** para organizar el frontend web Angular. Confirmada por el equipo el 6 de octubre de 2026. Mantener componentes standalone y Tailwind.

FSD organiza código y dependencias; no reemplaza Angular ni prescribe nuestro gestor de estado. La documentación oficial permite adoptarlo con distintos frameworks. [Descripción oficial de FSD](https://feature-sliced.design/docs/get-started/overview).

## Capas

Orden de arriba hacia abajo: **app → pages → widgets → features → entities → shared**.

| Capa | Aplicación propuesta a nuestro producto |
|---|---|
| app | Arranque Angular 18+, providers, rutas, shell y configuración global |
| pages | Dashboard operativo, planificación, inventario, entrega, simulador y consulta QR |
| widgets | Bloques compuestos complejos como mapa interactivo de rutas o resumen de riesgos |
| features | Acciones de usuario reutilizadas entre pantallas: registrar consumo, publicar ruta, recibir entrega |
| entities | Modelos de negocio, estado y DTOs específicos: punto, producto, lote, ruta y entrega |
| shared | Utilidades de UI genéricas, clientes HTTP base y contratos importados desde `libs/shared-core` |

Las capas inferiores no importan superiores. No crear la capa obsoleta `processes`. App y shared se dividen directamente por segmentos; el resto por slices. [Capas de FSD](https://feature-sliced.design/docs/reference/layers).

> [!TIP]
> **Adopción Progresiva sin Fricción:** Para acelerar el desarrollo y evitar parálisis ceremonial, los componentes inician en su respectiva `page` o `entity`. Solo se extraen a `features` o `widgets` cuando existe reutilización efectiva en más de una pantalla o cuando la complejidad visual lo exige.

## Slices, segmentos y Monorepo Shared Core

En pages/widgets/features/entities, cada slice agrupa una responsabilidad de producto. Segmentos habituales: ui, model, api, lib y config.

Para evitar duplicar interfaces TypeScript, DTOs de API y utilidades entre el panel web Angular y la aplicación móvil Ionic, el código base de contratos se centraliza en **`libs/shared-core`** (o `@istpetdev/shared-core`). La capa `shared/api` de la web consume y extiende directamente este núcleo.

Slices distintas de una misma capa no se importan directamente por defecto. La composición ocurre arriba. Para una relación excepcional entre entidades, preferir IDs/contratos simples; cualquier excepción se documenta. [Slices y segmentos](https://feature-sliced.design/docs/reference/slices-segments).

## Estructura Angular propuesta

```text
apps/web/src/
  main.ts
  styles.css
  app/
    app.config.ts
    app.routes.ts
    ui/
    providers/
  pages/
    operations-dashboard/
    route-planning/
    inventory/
    delivery-detail/
    simulation/
    public-trace/
  widgets/
    risk-overview/
    route-map/
    delivery-timeline/
  features/
    register-consumption/
    publish-route/
    receive-delivery/
  entities/
    service-point/
    product/
    lot/
    route/
    delivery/
    risk/
  shared/
    api/
    ui/
    lib/
    config/
```

Es el mapa objetivo de responsabilidades; crear carpetas solo con implementación real. Configurar aliases según el workspace: @pages, @widgets, @features, @entities y @shared.

## API pública e imports

Cada slice expone una interfaz explícita, por ejemplo index.ts; los consumidores externos usan esa interfaz. Evitar imports profundos a archivos privados. Dentro de una slice usar referencias locales y no reimportar desde su propia API pública si crea un ciclo.

En shared, exponer APIs por segmento/componente; evitar un barrel global que arrastre toda la aplicación. No exportar todo con comodines por comodidad. [API pública de FSD](https://feature-sliced.design/docs/reference/public-api).

Ejemplos del proyecto:

- pages/route-planning compone widgets/route-map y features/publish-route.
- features/publish-route usa entities/route y shared/api.
- widgets/route-map combina entities/route y entities/service-point.
- shared/ui/map-view recibe geometría genérica y no importa entities/route.
- entities/delivery no importa features/receive-delivery.
- Una feature no importa otra feature para encadenar un proceso; componerlo en página/widget.

## Angular, estado y reglas

Standalone Components se ubican en el segmento ui correspondiente. Providers/rutas globales van en app; servicios/estado propios de una slice permanecen locales. Guards genéricos de sesión pueden estar en shared y se registran arriba; reglas de permisos de producto dependen de entidades/casos concretos.

El servidor sigue validando permisos, stock y rutas. Un store frontend no es la fuente de verdad de inventario. No añadir un gestor de estado solamente por elegir FSD.

## Implementación y verificación

1. Configurar capas/aliases y una página con flujo real.
2. Separar tipos/cliente de entrega en entities/delivery.
3. Extraer las acciones y widgets realmente reutilizados.
4. Revisar imports, ciclos y API pública.
5. Añadir reglas de lint de límites cuando se elija la herramienta existente del repositorio.

La app Ionic mantiene su diseño offline en [[Movil offline y sincronizacion]]. Adoptar allí el mismo esquema se puede decidir después; esta decisión (ADR-13) afecta el frontend web.

Registrar como ADR-13 en [[Decisiones de arquitectura]]. La futura skill frontend debe usar esta nota, [[Frontend y componentes]] y [[Contratos API y eventos]].

## Complemento de versión y repositorio — 7 de octubre de 2026

La estructura/imports FSD anterior se conserva en Angular 22. Rutas acordadas materializadas solo como directorios locales en `~/Projects/ISTPETDEV`; `apps/web/src/app`, `pages`, `widgets`, `features`, `entities` y `shared` son carpetas hermanas. No se ha generado código Angular ni impuesto FSD móvil. Versiones y shared-core en [[Repositorio de software y versiones]]; CI debe considerar cambios en `libs/shared-core/**`.
