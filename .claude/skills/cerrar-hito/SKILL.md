---
name: cerrar-hito
description: Ritual de cierre de un hito de LA VIGILIA — auditoría de huecos reales contra el roadmap §5, pipeline de verificación completo, y actualización de CLAUDE.md §4/§5 con los números reales de la corrida.
disable-model-invocation: true
---

# /cerrar-hito — cerrar un hito como manda §6

**Uso:** `/cerrar-hito [N]` (si no se da N, deduce el hito en curso de `CLAUDE.md` §4)

`CLAUDE.md` §6 lo exige: *"Actualizar §4 al cerrar cada hito"*. §4 es el estado del código y
es lo que otra sesión lee para saber qué existe — si se queda desfasado, la siguiente sesión
reconstruye cosas ya hechas. Este es el ritual completo.

## 1. Auditoría de huecos reales (primero, antes de tocar nada)

§4 lo dice literal: *"Antes de avanzar el roadmap, hacer una auditoría de huecos reales vs §5
(no reconstruir lo ya hecho)"*.

Lee §5 para el hito N y, para **cada** entregable, verifica **en el código** si existe.
No te fíes de §4 ni de mi resumen: comprueba el símbolo.

Salida: una tabla `entregable | estado | evidencia (fichero:línea)` con tres estados:
**hecho** · **parcial** (di exactamente qué falta) · **pendiente**.

Si hay entregables parciales o pendientes, **para aquí y enséñasela al usuario**: el hito no
se cierra, se decide qué falta.

## 2. Pipeline completo

```powershell
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.DataAssetGenerator.RegenerateAll -LogName regen
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.HitoVerification.VerifyBatch -LogName verify
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind verify
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-run.ps1" -Method Purga.EditorTools.CombatSimulator.RunBatch -LogName sim -ExtraArgs "-simCount 200 -simSeed 20260727"
& "E:\Proyectos Unity\DD 40k\.claude\scripts\unity-report.ps1" -Kind sim
```

Condiciones de cierre, **innegociables**:

- [ ] `RESULTADO: N/N` — cero asserts en fallo
- [ ] `VIOLACIONES DE INVARIANTES: 0`
- [ ] Cero excepciones en el log de Unity
- [ ] Los asserts **nuevos** del hito existen y son **end-to-end** (comprueban el efecto en
      combate, no solo que un bool está a `true`)
- [ ] Cero términos SOLAR / Games Workshop / Trench Crusade en texto de pantalla

El **% de éxito** del sim **no** es condición de cierre: es un suelo pesimista con IA tonta.
Regístralo, no lo persigas.

## 3. Escribir §4 de `CLAUDE.md`

Añade el bloque del hito siguiendo **exactamente** el estilo de los que ya están. La
convención del documento, respétala:

- **`ID` de código en inglés entre backticks**, displayName en español LA VIGILIA en negrita.
- Cita los **símbolos reales** (`Campaign.currentSector`, `RepairAutomaton`, `HorrorTier`),
  no descripciones vagas: es lo que hace útil a §4.
- Cierra con la línea de veredicto en el formato de las anteriores:
  `Verificado **N/N** (\`NombreDeChecks\`), sim **X%**, 0 violaciones`.
- Marca en negrita los **PENDIENTES** que quedan y a qué hito se mueven.
- Si una decisión de diseño se cerró durante el hito, anótala como **(decidido)** — así no se
  vuelve a discutir en la siguiente sesión.

## 4. Escribir §5 (roadmap)

Marca el hito como ✅ y **mueve** explícitamente lo que no entró al hito destino. Si un
entregable se reasignó, dilo con la razón (como se hizo con *"la Brigada se movió al Hito 6:
solo sirve para interrogar infiltrados y la infiltración entra allí"*).

## 5. Cambios de diseño

Si durante el hito se cambió algo que §1–§3 daban por cerrado, actualiza **también** esas
secciones y `docs/VIGILIA_biblia_del_mundo.md` (la biblia manda en el detalle). Un número que
diga una cosa en §3 y otra en el código es peor que no tenerlo.

## 6. Resumen al usuario

Cierra con: hito cerrado, asserts N/N, delta del % del sim contra el hito anterior, qué se
movió de hito y qué es lo siguiente en §5.
