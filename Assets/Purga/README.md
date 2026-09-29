# LA VIGILIA DE HIERRO — Prototipo (Hitos 2–7)

RPG táctico por turnos inspirado en Darkest Dungeon, ambientación original **de Primera Guerra Mundial apocalíptica**: en el año 12 de la Ruptura, el Infierno (el **Foso**) se derramó sobre la Tierra y el mundo es un frente eterno de trincheras. Mandas una compañía de **la Vigilia de Hierro** contra **la Congregación de la Herida** y sus **Descendidos**. Toda la interfaz es IMGUI provisional y aparece sola al darle a Play (el arte y la UI real van en el Hito 9).

> Nomenclatura: identificadores de código en inglés genérico; **todo texto en pantalla usa nombres LA VIGILIA** (ver `docs/VIGILIA_biblia_del_mundo.md` y `CLAUDE.md`). Nada de propiedad intelectual de terceros en pantalla (§0/§7 del `CLAUDE.md`). El eje del alma es **la Mancha** (corrupción, 0–200) frente a **la Llama** (fe, 0–10).

## Estado (Hitos 2–7 completos, 396 asserts en verde, 0 violaciones de invariantes)
- **Combate + el eje de la luz**: profundidad ligada a la **Bengala** (luz) vs el **Foso** (oscuridad); los humanos cazan lo iluminado, la carne del Foso lo oscuro (canales de amenaza, eje espejo). Iniciativa VEL+1d6, impacto PREC−Esq (5–95), crítico ×1.5, armadura plana, estados (Sangrado/Quemadura/**Gas**/Aturdido/Marcado/buffs), Al Borde de la Muerte.
- **la Mancha / la Llama / la Prueba de Fe**: a 100 Mancha, chequeo 25%+4%/Llama → Temple (Iluminado/Inquebrantable) o Trauma (Conmocionado/Desesperado/Saqueador/Apóstata), con efectos reales; **a 200 el alma se somete a la oscuridad (Caído, muerte)**.
- **6 clases**: Fusilero, Sanadora, Oficial (con **fusilar**), Vidente (oír el Foso), Capellán, y el **Autómata** (inmune a la Mancha, sin Llama, se **avería** en vez de morir).
- **Incursión por pasillos**: nodos-cruce unidos por tramos con **longitud** (cada tramo pasa un turno: Señal/DoT/reloj de misión), eventos de marcha (alijo/trampa/Gas/alambrada), **la Señal** continua 0–100, generación procedural por sector con validación.
- **el Fortín**: Puesto de Mando · **el Barracón** (ficha, equipo de 4 ranuras, escuadra) · la Capilla · la Enfermería · el Taller (mejoras, construir/reparar Autómatas, tienda) · el Comedor · la Leva · **la Brigada** (interrogar infiltrados) · el Manual.
- **Frente procedural**: 4 sectores (Primera Línea → Galerías → Tierra de Nadie → el Reducto), red de nodos generada y validada, degradación por la Crecida (8 fases).
- **Bestiario** completo por categorías (Humanos, Carne del Foso, Descendidos, Fauna), jefes de sector (Barón · Matriarca · Guardián · **Confesor Rojo** de 3 fases) y mini-bosses (Falso Capellán · Zapador · Alfa del Foso · Desollador).
- **Campaña**: roster con permadeath, semilla determinista, **infiltración** (agentes del Foso con tells), **votos** (modificadores de campaña), y el clímax: matar al Confesor abre la Herida → **sellarla** es la victoria (o el sector cae si la Crecida llega a fase 8).

## Cómo probarlo
1. Abre el proyecto en Unity **6000.5.4f1** (Unity 6).
2. Menú **Purga → Regenerar TODOS los assets** (hornea los datos desde `Library.cs`).
3. En una escena vacía: GameObject vacío + Add Component `CombatBootstrap`. Pulsa **Play**.
4. En el Fortín: jura tus **votos**, arma la escuadra en el Barracón, aprovisiona, y baja por el frente vigilando la Crecida.

## Herramientas de dev
- **`/verificar`** (skill de `.claude/`): pipeline completo RegenerateAll → VerifyBatch → RunBatch vía `scripts/unity-run.ps1` (encapsula las trampas de PS 5.1). También los menús *Purga → Verificar* y *Purga → Simular campañas* escriben a `PlaytestLogs/`.
- **Hook de nomenclatura** (`.claude/hooks/check-terms.ps1`): bloquea términos del skin viejo / GW en cadenas visibles de `.cs`.
- **Subagente `combat-reviewer`**: revisa cambios de combate contra las invariantes antes de gastar una corrida de Unity.

## Datos
`Assets/Purga/Scripts/Library.cs` es la fuente editable de verdad (números copiados de los documentos de diseño). Los ScriptableObjects de `Resources/Purga/` son artefactos generados y versionados que consume el runtime: se regeneran desde `Library`, no se editan en el Inspector. Ver `docs/DEVELOPMENT.md`.

*Cuento los que salen. Cuento los que vuelven. El Foso se queda con la diferencia.*
