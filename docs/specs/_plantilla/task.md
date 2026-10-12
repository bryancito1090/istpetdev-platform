# NNN — Nombre del spec: tareas

> Pasos ejecutables del [`plan.md`](plan.md). Cada tarea entrega algo probable de forma independiente y termina en commit. Cada paso es una acción de 2–5 minutos. TDD: prueba que falla → implementación mínima → prueba que pasa → commit.
>
> Sin marcadores vacíos: nada de «TBD», «añadir validación», «similar a la tarea N» ni pruebas sin código.

## Resumen

| ID | Tarea | Depende de | Paralelizable | Estado |
|---|---|---|---|---|
| T1 | … | — | [P] | pendiente |

`[P]` = puede ejecutarse en paralelo con otras `[P]` del mismo nivel porque no toca los mismos archivos.

---

### Task 1: Nombre del componente

**Files:**
- Create: `ruta/exacta/archivo`
- Modify: `ruta/exacta/existente:líneas`
- Test: `ruta/exacta/prueba`

**Interfaces:**
- Consumes: firmas exactas que usa de tareas anteriores.
- Produces: nombres, parámetros y tipos exactos que usan tareas posteriores.

- [ ] **Step 1: Escribir la prueba que falla**

```csharp
// código real de la prueba
```

- [ ] **Step 2: Ejecutar la prueba y verificar que falla**

Run: `comando exacto`
Expected: FAIL con «mensaje esperado»

- [ ] **Step 3: Implementación mínima**

```csharp
// código real
```

- [ ] **Step 4: Ejecutar la prueba y verificar que pasa**

Run: `comando exacto`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add rutas
git commit -m "feat(ámbito): descripción"
```
