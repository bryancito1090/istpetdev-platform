# NNN — Nombre del spec: plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. Las tareas están en [`task.md`](task.md).

**Goal:** Una frase de lo que construye.

**Architecture:** Dos o tres frases del enfoque.

**Tech Stack:** Tecnologías y versiones clave.

**Spec:** [`spec.md`](spec.md)

## Global Constraints

> Requisitos transversales del spec, una línea cada uno, con valores exactos copiados del spec o de Hechos canonicos (versiones, nombres, idioma de mensajes, restricciones de plataforma). Toda tarea los cumple implícitamente.

- …

## Review Focus

> Las cinco entradas o fallos que el spec implica pero ninguna prueba ejercita y que más probablemente afectarían a un usuario, del más probable al menos. Cada línea indica la tarea de `task.md` que añade la prueba que lo fija.

1. … → prueba en Task N.

---

## Arquitectura

> Diagrama o descripción de componentes y flujo de datos. Qué capa (Domain, Application, Infrastructure, Api, web, mobile, n8n) contiene cada pieza.

## Estructura de archivos

| Acción | Ruta | Responsabilidad |
|---|---|---|
| Crear | `ruta/archivo` | … |
| Modificar | `ruta/archivo` | … |
| Eliminar | `ruta/archivo` | … |

## Decisiones técnicas

| Decisión | Alternativas descartadas | Motivo |
|---|---|---|

## Dependencias nuevas

| Paquete | Versión | Proyecto | Motivo |
|---|---|---|---|

## Prerrequisitos de entorno

> Comandos para comprobar que la máquina puede ejecutar el plan (SDK, Docker, herramientas).

## Estrategia de pruebas

> Unitarias, integración y E2E: qué cubre cada nivel y cómo se filtran.

## Orden y paralelización

> Qué tareas bloquean a otras y cuáles se pueden repartir entre personas o agentes.

## Reversión

> Cómo deshacer el cambio si falla en integración o en el despliegue de demo.
