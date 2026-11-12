# TODO / Mejoras — HireWire

Rastreo de mejoras por aplicar y completadas.

**Convenciones:**
- **Fecha:** `YYYY-MM-DD`.
- **Complejidad:** `Alta` / `Media` / `Baja`.
- **Probabilidad de resultar en bugs:** `Alta` / `Media` / `Baja`.
- Pendientes ordenados de mayor a menor prioridad (bugs/fallos de uso arriba).
- Al completar: mover a "Tareas completadas" con su fecha de cierre.
- Nunca registrar datos sensibles (passwords, tokens).

---

## Mejoras por aplicar

| ID | Tarea detallada | Complejidad | Prob. de bugs | Fecha descubierta |
|----|-----------------|-------------|---------------|-------------------|
| IMP-001 | *(pendiente)* | | | |

---

## Tareas completadas

| ID | Tarea detallada | Fecha completada |
|----|-----------------|------------------|
| DONE-001 | Publicación bajo subpath (controlzeta.io/hirewire): `<base href>` dinámico desde `NavigationManager.BaseUri`, links absolutos → relativos, `UsePathBase` opcional por config `PathBase`, `UseHttpsRedirection` solo con puerto HTTPS conocido. Verificado en local (raíz y PathBase) y en producción. Commit 1ea1245. | 2026-11-12 |
| DONE-002 | Fase 1 de cobertura de pruebas: `HireWire.Tests` recreado (xUnit + Moq + FluentAssertions + Coverlet) y agregado a la solución; 89 pruebas (Domain + Application). Cobertura de líneas ~56% (Application 47%, Domain 97.5%). Branch `TestCoverage`. | 2026-11-12 |