# PURGA: CRUZADA DEL SECTOR MORTIS
### Draft de diseño (GDD v0.2) — Roguelike táctico inspirado en Darkest Dungeon ambientado en Warhammer 40.000

---

## 1. Visión general

**Género:** RPG táctico por turnos con gestión de roster, estrés psicológico y estructura roguelike de campaña.

**Fantasía central:** Eres un Inquisidor recién ascendido del Ordo (a elegir: Hereticus, Xenos o Malleus — ver §3). El Sector Mortis se desmorona: cultos, invasiones xenos y tumbas que despiertan. Diriges a agentes desechables, frágiles y mortales contra horrores que ningún humano debería ver. No ganas salvando a todos; ganas decidiendo a quién sacrificar.

**Pilares de diseño:**
1. **El coste humano es la mecánica.** Cada victoria consume cuerpos y mentes. Los personajes son recursos con nombre.
2. **La fe es un arma y una jaula.** El sistema Fe/Corrupción sustituye y amplía el estrés de DD.
3. **El tiempo es tu enemigo estratégico.** Relojes de amenaza por planeta fuerzan decisiones imposibles.
4. **Cada facción enemiga rompe una regla distinta.** No son "skins": cada campaña cambia cómo se juega.

**Diferencias clave respecto a Darkest Dungeon:**
- Campaña multi-planeta con mapa estratégico de sector (no una sola aldea/finca).
- El "estrés" tiene dos salidas: Corrupción (herejía/mutación) o Fanatismo (poderoso pero incontrolable).
- Combate con recursos de munición y cobertura ligera además de posiciones.
- Relojes de amenaza simultáneos: no puedes atender todos los planetas; algunos caerán.
- El jugador tiene un "personaje": el Inquisidor, que no combate pero interviene (ver §6.5).

---

## 2. Estructura de campaña: el Sector Mortis

### 2.1 El mapa estratégico
En lugar de una aldea con mazmorras, el jugador gestiona la **Sancta Sicaria**, una nave-capilla inquisitorial que viaja entre 5–6 planetas del sector. Cada planeta es una "mini-campaña" temática con sus propios distritos (mazmorras), enemigos, jefes y reloj de amenaza.

**Bucle estratégico:**
1. En el mapa de sector, cada turno estratégico (una "semana imperial") los relojes de amenaza de TODOS los planetas avanzan.
2. Viajar entre planetas cuesta semanas (el Warp es impredecible: eventos de viaje aleatorios).
3. En un planeta, lanzas expediciones (el bucle táctico clásico de DD).
4. Completar misiones clave **retrasa o congela** el reloj de ese planeta. Matar al jefe planetario lo **estabiliza** permanentemente y desbloquea recursos únicos.
5. Si un reloj llega al final: el planeta cae. Consecuencias en cascada (ver 2.3).

### 2.2 Los planetas (cada uno = una facción, una mecánica rota)

**A. Vharsis Prima — Ciudad colmena (Culto Genestealer + Tiránidos)**
- *Tema:* infiltración, paranoia, lo que parece humano no lo es.
- *Mecánica única:* **Infiltración**. Algunos PNJ reclutables y hasta miembros de tu propio roster pueden ser híbridos durmientes. Existe un mini-juego de contrainteligencia en la nave (interrogatorios, augurios) para detectarlos antes de que saboteen una expedición.
- *Mazmorras:* mercados de la colmena, subcolmena, capilla profanada, magistratura.
- *Reloj:* "El Despertar" — al agotarse, el culto invoca a la Flota Enjambre: el planeta es devorado y aparecen incursiones tiránidas en planetas vecinos.
- *Jefe:* el Patriarca, en una arena donde el terreno mismo (biomasa) cura al jefe.

**B. Kharnath — Mundo en rebelión (Caos, dividido en zonas por dios)**
- *Tema:* la guerra civil como escaparate de los 4 poderes. Cuatro distritos, cuatro reglas.
  - *Zona Khorne:* prohibido el turno defensivo; enemigos ganan fuerza si tu grupo "duda" (pasa turno, se retira).
  - *Zona Nurgle:* enfermedades persistentes que viajan contigo a otros planetas si no las tratas.
  - *Zona Tzeentch:* el mapa de la mazmorra se reordena; las salas mienten.
  - *Zona Slaanesh:* enemigos que atacan la barra de Fe directamente y ofrecen "regalos" (buffs con coste de Corrupción oculto).
- *Reloj:* "La Ascensión" — un campeón mortal acumula favor; al final asciende a Príncipe Demonio (jefe final del planeta MUCHO más difícil si dejaste correr el reloj: el jefe escala con el reloj, idea central del diseño).
- *Mecánica única:* **Marcas del Caos** — botín poderosísimo y maldito; usarlo corrompe, destruirlo enfurece a los dioses (emboscadas demoníacas en el viaje).

**C. Gorkag's Krunch — Mundo minero invadido (Orkos)**
- *Tema:* respiro tonal. Brutal pero casi cómico; el "planeta de descompresión" del jugador (como las misiones cortas de DD).
- *Mecánica única:* **¡WAAAGH! creciente** — el volumen importa. Cada combate ruidoso (armas pesadas, explosivos) atrae más orkos: las salas siguientes se llenan. Jugar sigiloso (armas silenciosas, rutas cortas) los evita. Invierte el incentivo de DD de "limpiar todas las salas".
- *Detalle:* los orkos generan poco estrés/Corrupción (son un horror comprensible), pero muchísimo daño físico. Aquí se curan mentes y se rompen cuerpos. Sirve para "rotar" el roster de forma natural.
- *Reloj:* "El Gran WAAAGH!" — si estalla, hordas orkas asaltan tu nave periódicamente (defensa de la nave, ver §5.4).
- *Jefe:* el Kaudillo en su Gargant a medio construir (jefe por fases con partes destruibles).

**D. Sepulkhra — Mundo tumba (Necrones)**
- *Tema:* horror cósmico frío. Aquí la fe no consuela: los Necrones demuestran que hay cosas más viejas que el Emperador.
- *Mecánica única:* **Reensamblaje** — los enemigos caídos se levantan al cabo de N turnos salvo que gastes un turno/recurso en destruir el cuerpo. Cambia toda la matemática del focus fire de DD.
- *Segunda mecánica:* **Terror existencial** — los Necrones no generan Corrupción del Warp (no son del Warp) sino **Desesperanza**, un daño a la Fe que solo se cura con victorias, no con rezos. Los personajes fanáticos sufren el doble aquí (su fe choca contra la evidencia); los escépticos (Tecnosacerdotes, Asesinos) resisten mejor. Invierte la jerarquía de resistencias del resto del juego.
- *Reloj:* "El Despertar de la Dinastía" — cada fase despierta unidades de mayor nivel en TODAS las mazmorras del planeta.
- *Jefe:* el Señor Supremo, que resucita una vez por combate salvo que hayas encontrado y destruido su pozo de reensamblaje en una misión previa (recompensa la exploración estratégica).

**E. Void Station Erebus — Estación espacial a la deriva (Drukhari / opcional Genestealers puros)**
- *Tema:* misiones cortas de rescate y horror claustrofóbico. Los Eldar Oscuros atacan la barra de Fe con dolor y capturan personajes (¡en vez de matarlos!).
- *Mecánica única:* **Capturados** — un personaje reducido a 0 no muere: es arrastrado a Commorragh (offscreen). Puedes montar una misión de rescate suicida antes de X semanas o perderlo para siempre. Genera las mejores historias emergentes.
- Planeta opcional/secundario, sin reloj propio: es la "mazmorra infinita" para farmear con riesgo.

**F. (Final) La Herida — Anomalía Warp en el corazón del sector**
- El equivalente a la Mazmorra Más Oscura. Solo accesible tras estabilizar (o perder) un número de planetas. Ver §8.

### 2.3 Consecuencias en cascada (lo que DD no tiene)
- Perder un planeta no es game over: es un sector más oscuro. Vharsis cae → incursiones tiránidas aleatorias en otros planetas. Kharnath cae → tormentas Warp que alargan todos los viajes. Sepulkhra cae → flotas necronas bloquean rutas (algunos trayectos cuestan el doble).
- **Regla de diseño:** el jugador NO PUEDE salvar todos los planetas en una partida normal. Salvarlos todos es el desafío tipo "Bloodmoon"/NG+.
- Esto da rejugabilidad: cada partida decides qué sacrificar, y el final refleja el estado del sector.

### 2.4 Estructura de expedición: Stages y Substages (base Darkest Dungeon)

Jerarquía: **Planeta → Stage → Substage → Salas.**

- Cada planeta se divide en **Stages** (distritos, cada vez más profundos). Vharsis, p. ej.: Stage 1 "Puertas de la Colmena", Stage 2 "Nivel Medio", Stage 3 "Subcolmena", Stage Final "El Nido".
- Cada Stage contiene **Substages contiguas** (1-1, 1-2, 1-3…). Completar la 1-1 desbloquea la 1-2. Completar todas las substages de un Stage desbloquea el siguiente Stage y su mini-jefe/jefe de cierre.
- Cada **Substage** es el mapa clásico de DD: salas conectadas por pasillos, ruta a elección del jugador, con un objetivo de misión y niebla de guerra. La Señal de Vox (§4.3) revela u oculta el mapa.
- **Dificultad:** las substages de un mismo Stage comparten nivel de enemigos (Aprendiz / Veterano / Campeón, como DD). Los veteranos de nivel alto se niegan a bajar a substages triviales.

**Retirada y fracaso:**
- Puedes retirarte en cualquier momento desde una sala: pierdes el progreso de esa substage (habrá que reintentarla), conservas botín, XP parcial y todo el estrés/Corrupción/heridas acumulados.
- Retirarse en mitad de combate: tirada de retirada por personaje; fallar deja al personaje un turno expuesto (como DD).
- Con Señal de Vox alta la evacuación es limpia e instantánea; sin señal, la retirada es "a pie": desandas el camino con combates de vuelta.
- **Muerte total del grupo (wipe):** los personajes se pierden (o son Capturados, si es Erebus). El botín queda en la sala: una expedición posterior puede recuperar los cuerpos y el equipo ("misión de recuperación", con los restos de tus muertos como marcador en el mapa).

**Tipos de sala y casilla:**
| Tipo | Contenido |
|---|---|
| Combate | Encuentro estándar; algunos con cobertura (§4.1) o condiciones (oscuridad, biomasa, gravedad) |
| **Mini-boss** | Enemigo de élite con mecánica propia (ver 2.5). Marcado en el mapa como "lectura anómala": sabes que hay *algo*, no qué |
| Tesoro | Cofres, cadáveres, alijos; requieren llaves, ganzúas o Fe (relicarios sellados) |
| Curiosidad | Interactuables estilo DD: altares profanados, cogitadores, servocráneos, cadáveres susurrantes. Usar el suministro correcto da bonus; tocar a ciegas, ruleta |
| Evento/Superviviente | Encuentros narrativos y reclutas encontrables (2.6) |
| Santuario | Sala segura: campamento (curación, sermones, vigilancia) — versión de las hogueras de DD |
| Trampa/Peligro | Detectables según la Perspicacia del grupo |

**Objetivos de misión por substage** (ejemplos por facción): purgar el 100% de combates, destruir 3 nidos con promethium (Tiránidos), profanar/consagrar altares (Caos), sabotear la torreta de chatarra antes de X rondas (Orkos), sellar pozos de reensamblaje (Necrones), rescatar y escoltar VIP hasta la salida (Erebus).

### 2.5 Mini-bosses de sala

Élites únicos que aparecen en salas marcadas como anomalía. No son obligatorios: puedes rodearlos (la ruta es tuya), pero custodian el mejor botín/reclutas de la substage y algunos objetivos los requieren. Ejemplos:

- **El Predicador Hueco** (Vharsis): un sacerdote híbrido; cada turno "sermonea" subiendo Corrupción a todo el grupo hasta que lo silencies (ataques a posición 1 no le afectan: hay que alcanzar la 3-4).
- **Carnicero de Khorne** (Kharnath): gana +daño por cada aliado tuyo que sangre; te obliga a jugar limpio y rápido.
- **Doc Matasanoz** (Gorkag): mini-boss cómico que "cura" a sus orkos amputándoles cosas; prioriza tu retaguardia con la sierra.
- **Criptotecnólogo** (Sepulkhra): no ataca; acelera el reensamblaje de todos los caídos y teletransporta tu formación. Matarlo cambia el combate entero.
- **Homúnculo** (Erebus): marca a un personaje para Captura; si acaba el combate vivo, se lleva al marcado aunque ganes.

### 2.6 Reclutas encontrables en expedición

Además de la Leva semanal (§5), hay personajes que SOLO se consiguen dentro de las mazmorras:

- **Supervivientes:** atrincherados en salas de evento. Ocupan hueco de grupo si están (máx. 4) o te siguen como quinto "no combatiente" que debes proteger hasta la salida. Llegan con quirks de lo vivido allí.
- **Prisioneros:** celdas que requieren llave/ganzúa encontrada en la misma substage. Riesgo: en Vharsis puede ser un híbrido durmiente (se detecta después, en las Celdas de la nave).
- **Condicionales únicos** (con nombre, uno por partida):
  - *Asesino de la Officio:* aparece en Stage 3 de cualquier planeta si completaste el Stage 2 sin muertes.
  - *Renegado Redimido:* evento en zona Tzeentch; solo si le perdonas la vida (Ordo Hereticus puede reclutarlo; otros Ordos deben pagar Autoridad).
  - *Tecnosacerdote Herético Reformado:* en Sepulkhra, si le llevas un artefacto necrón intacto.
  - *Ogryn "Piedra":* enterrado vivo en Gorkag; requiere palas (¡suministro barato que nadie lleva!) — homenaje al diseño de provisiones de DD.

### 2.7 Escalada de jefes a lo largo de la campaña

Curva de dificultad de jefes en tres niveles:

1. **Mini-bosses de sala** (2.5): desde el primer Stage; enseñan mecánicas en pequeño.
2. **Jefes de Stage:** cierran cada Stage (el Magus del culto, el Herrero de Guerra, el Mekániko…). Versión Veterano y Campeón según profundidad, con mecánicas nuevas en cada versión (como los jefes escalonados de DD).
3. **Jefes planetarios:** el Patriarca, el Príncipe Demonio, el Kaudillo, el Señor Supremo. Escalan con el reloj de amenaza: cuanto más tardes, más fases y esbirros tienen.
4. **SUPER BOSS final — El Corazón de la Herida** (§8): jefe dinámico multifase que hereda una mecánica de cada facción que conquistó su planeta en tu partida (reensamblaje + biomasa curativa + marcas del Caos…). Además, jefe secreto opcional post-campaña para el roster superviviente: **el Espejo del Inquisidor**, una versión caída de ti mismo con tus propias intervenciones usadas en tu contra.

---

## 3. El Inquisidor (el "jugador" con rostro)

Al empezar, eliges Ordo, que funciona como "clase de campaña" (equivalente a elegir dificultad/estilo, o a los caminos de DD2):

| Ordo | Bonus | Coste |
|---|---|---|
| **Hereticus** | Herramientas de purga de Corrupción más baratas; puede "condenar" quirks heréticos gratis | Los xenos (Necrones, Tiránidos) le generan +estrés a su gente |
| **Xenos** | Botín xenos utilizable (¡armas necronas, venenos drukhari!); mejor info de enemigos | Usar tecnología xenos genera sospecha: eventos de "puritanos" que te auditan |
| **Malleus** | Sus psíquicos tiran 2 veces en Peligros del Warp y eligen; acceso a armas sagradas anti-demonio | Los demonios lo conocen: jefes del Caos con mecánicas extra contra ti |

**Intervenciones del Inquisidor** (recurso "Autoridad", se regenera por semana): una vez por expedición puedes p. ej. ordenar bombardeo orbital (si hay señal de vox, ver §4.3), decretar ejecución sumaria a distancia, o invocar "En Nombre del Emperador" (limpia el estrés de todo el grupo una vez). Autoridad también se gasta en el mapa estratégico (requisar recursos a gobernadores, etc.). Un solo recurso, tensión constante entre lo táctico y lo estratégico.

---

## 4. Sistemas centrales de combate y expedición

### 4.1 Combate: base DD, tres capas nuevas
- **Posiciones 1–4** por bando, movimiento y ataques dependientes de posición: se mantiene, funciona.
- **Capa 1 — Munición:** las armas a distancia tienen cargadores. Recargar cuesta un turno menor. Las armas cuerpo a cuerpo nunca se agotan → decisión de composición real (la Guardia dispara, luego fija bayonetas).
- **Capa 2 — Cobertura ligera:** algunas salas tienen 1–2 posiciones "con cobertura" (bonus de esquiva a distancia, anulada por lanzallamas/granadas). Añade lectura de sala sin convertirlo en XCOM.
- **Capa 3 — Blindaje vs. Salud:** los enemigos pesados (Necrones, Nobles orkos) tienen Blindaje que reduce daño plano. Armas de energía/fusión lo ignoran. Crea nichos de equipo por planeta.

### 4.2 Fe y Corrupción (el corazón del juego)
Cada personaje tiene DOS barras en tensión:

- **Corrupción (0–200):** sube con horrores del Warp, brujería, botín maldito, actos heréticos. 
  - A 100: **Prueba de Alma.** Fallo → *Quebranto* (aflicciones estilo DD: Paranoico, Blasfemo, Codicioso, Desesperado…). Éxito → **Momento de Fe** (virtudes: Mártir, Iluminado, Inquebrantable…).
  - A 200: **el personaje se pierde.** Tirada oculta: muta en combate (se convierte en enemigo Spawn del Caos AHÍ MISMO), deserta con tu botín, o sabotea la expedición. Nunca sabes cuál.
- **Fe (0–10):** recurso activo. Se gasta en habilidades de Fe (milagros de la Sororitas, "aguantar la línea" del Comisario, rezos que reducen Corrupción del grupo). Se recupera con victorias, reliquias, y rituales en la nave.
  - **Fe alta = escudo:** cada punto de Fe da resistencia pasiva a Corrupción.
  - **Fe 10 mantenida mucho tiempo → riesgo de Fanatismo:** el personaje gana daño y resistencias enormes pero puede desobedecer (atacar al objetivo que él considera más hereje, negarse a retirarse, ejecutar a civiles en misiones de rescate). El fervor también es peligroso. Muy 40K.

**La ejecución del Comisario:** cualquier líder con la habilidad puede ejecutar a un aliado Quebrado o mutando: el grupo pierde a un miembro pero TODOS los demás pierden 30 de Corrupción y ganan 2 de Fe ("el Emperador ve nuestra disciplina"). Si ejecutas a alguien que habría superado su prueba… nunca lo sabrás.

### 4.3 La Señal de Vox (sustituto de la antorcha)
- Barra de señal con tu nave. Baja al profundizar, en zonas blindadas, tormentas Warp.
- **Señal alta:** puedes usar intervenciones del Inquisidor, ves el mapa, evacuación disponible en cualquier momento, menos botín (las zonas conectadas ya están saqueadas).
- **Señal nula:** botín x2, enemigos "de las profundidades", sin evacuación (retirarse = combates de vuelta), la Corrupción sube pasivamente ("estamos solos"). 
- Objetos (balizas vox, servocráneos repetidores) permiten manipularla tácticamente.

### 4.4 Logística de expedición
Provisiones compradas antes de salir, estilo DD pero temáticas: cargadores, células de energía, sellos de pureza (curan Corrupción in situ, carísimos), agua bendita, kits de campaña, promethium (antorcha física para quemar biomasa/nidos), servocráneo médico. El exceso se vende con pérdida: la tensión de compra de DD se conserva.

---

## 5. La Sancta Sicaria (la "aldea")

Instalaciones mejorables con recursos de cada planeta (cada planeta da un recurso único → razón económica para no abandonar planetas):

1. **Reclusiam (capilla):** cura Corrupción, entrena Fe. Mejoras del Ordo Hereticus.
2. **Enfermería:** cura enfermedades, heridas persistentes, quita mutaciones físicas (con cicatrices).
3. **Refectorio/Cantina:** reduce estrés social; los personajes forman **vínculos** aquí (ver 5.2).
4. **Forja del Magos:** mejora armas/armaduras, analiza tecnología xenos (Ordo Xenos), fabrica munición especial.
5. **Celdas de interrogatorio:** contrainteligencia genestealer; también "reeducación" de quirks (más rápida que la capilla, pero con riesgo de trauma).
6. **Librarium prohibido:** investiga jefes (revela mecánicas antes del combate), descifra botín del Caos. Usarlo genera Corrupción a los investigadores.
7. **Barracones/Reclutamiento:** la Diligencia de DD → **la Leva**: cada semana llegan reclutas cuyo tipo depende de qué planetas siguen en pie (¡otro coste de perder planetas: pierdes acceso a esas clases de recluta!).

### 5.2 Vínculos (mejora sobre DD)
Los personajes que sobreviven juntos a expediciones forman vínculos: *Hermanos de armas* (bonus juntos, estrés masivo si uno muere), *Rivalidad* (compiten: +daño, +estrés mutuo), *Devoción* (uno protege al otro automáticamente), *Sospecha* (¿será un híbrido?). Los vínculos hacen que el roster cuente historias solo.

### 5.3 Eventos de nave
Eventos semanales estilo DD (peregrinos polizones, un puritano audita tu bodega, motín en cubierta 3, un demonio susurra en el Warp durante el viaje…). Muchos son dilemas con la firma del juego: no hay opción buena, solo la menos mala.

### 5.4 Defensa de la nave
Si ciertos relojes estallan (WAAAGH!, incursión drukhari), abordan la Sancta Sicaria: combate defensivo con TODO tu roster disponible en oleadas, usando los pasillos de tu propia nave como mazmorra. Perder instalaciones las desactiva hasta repararlas.

---

## 6. Clases jugables (roster inicial: 12)

Cada clase: 7 habilidades (equipas 4), 2 recursos (Salud + Fe), afinidades de Corrupción distintas.

| Clase | Rol | Firma |
|---|---|---|
| **Veterano de la Guardia** | Línea frontal versátil | "Fijar bayonetas": cambia de modo disparo a melé, con stats distintos |
| **Hermana de Batalla** | Melé sagrado / soporte de Fe | Actos de Fe: milagros que gastan Fe del grupo entero para efectos enormes |
| **Comisario** | Líder / control | Ejecución; "¡Ni un paso atrás!": nadie puede ser empujado de posición |
| **Psíquico Sancionado** | Nuker | Cada poder tira en Peligros del Warp; puede pedir "quemar cordura" para potenciar |
| **Tecnosacerdote** | Soporte/utilidad | Repara Blindaje aliado, sobrecarga armas, inmune a Desesperanza necrona |
| **Ogryn** | Tanque puro | "Demasiado tonto para temer": inmune a la mitad de fuentes de estrés; Corrupción altísima si su "protegido" muere |
| **Ratling** | Francotirador pos. 4 | Roba botín extra; huye primero (¡puede abandonar el combate solo!) |
| **Árbites** | Control de masas | Escudo supresor: cobertura portátil para su posición |
| **Sacerdote del Ministorum** | Sanador de Fe | Sermones: convierte Corrupción en Fe; con motosierra eviscerator, claro |
| **Asesino de la Officio** (desbloqueable) | Burst crítico | Entra en mitad de la expedición vía inserción; no forma vínculos jamás |
| **Escriba/Adepta** (soporte raro) | Buffs de información | Marca debilidades; débil pero multiplica al grupo; los jugadores la subestimarán y luego la amarán |
| **Renegado Redimido** (desbloqueable, Hereticus) | Comodín | Empieza con 80 de Corrupción permanente mínima; habilidades del Caos "domesticadas" |

**Progresión:** niveles 0–6 como DD, pero al subir eliges 1 de 2 mejoras por habilidad (árbol mínimo, builds distintas del mismo personaje). Los veteranos se niegan a misiones triviales (se conserva: es buen diseño).

---

## 7. Economía y objetos

- **Tronos** (oro), **recursos de mejora** por planeta, **Sellos de Pureza** (moneda de curación espiritual), **Favores del Ordo** (moneda meta para desbloqueos entre partidas — capa roguelike ligera).
- **Abalorios → Reliquias y Amuletos:** de reliquias imperiales verificadas (seguras) a artefactos xenos/caos (poderosos, con Corrupción pasiva o efectos ocultos que se revelan al usarlos N veces).
- **Botín maldito:** objetos del Caos no identificados. El Librarium los identifica… o los equipas a ciegas. La avaricia es un vector de Corrupción, como en DD la antorcha baja por codicia.

---

## 8. El final: La Herida

Anomalía Warp central. Requisitos: derrotar N jefes planetarios. Cuatro misiones finales (como DD):
1. **El Umbral** — cruzar la barrera Geller de una nave espacio-muerta.
2. **El Coro** — silenciar a tres coros de invocación simultáneos (gestión de tiempo dentro de la mazmorra).
3. **El Espejo** — te enfrentas a versiones corruptas de personajes TUYOS muertos durante la campaña (¡el juego los recuerda!).
4. **El Corazón de la Herida** — jefe final: entidad que usa mecánicas de todas las facciones según qué planetas cayeron en tu partida (jefe final dinámico: tu campaña esculpe al enemigo final).

**Regla de la Herida:** quien sobrevive a una misión final se niega a volver (como DD). Y quien la completa gana el rasgo "Ha visto la Herida": inmune a Corrupción, pero ya no puede recuperar Fe. Vacíos por dentro.

**Finales múltiples** según planetas salvados, Ordo elegido, y una decisión final (sellar la Herida sacrificando al grupo / purgar el sector vía Exterminatus múltiple / usar la Herida — final herético oculto).

---

## 9. Tono, arte y audio (notas breves)

- **Arte:** 2D con trazo grueso y sombras duras estilo DD, pero paleta 40K: dorados sucios, púrpuras del Warp, verde gauss. Cada planeta con paleta propia.
- **Narrador:** en vez del Ancestro, **el propio Inquisidor** dicta sus memorias ("Vocación. Deber. Y la lenta aritmética del sacrificio."). Frases lapidarias en combate, estilo DD.
- **Audio:** coros gregorianos corruptos, vox distorsionada, silencio absoluto en Sepulkhra (los Necrones no hacen ruido: solo pasos metálicos — el silencio como terror).

## 10. Alcance y priorización (si esto fuese un proyecto real)

- **Vertical slice:** 1 planeta (Vharsis), 5 clases (Veterano, Hermana, Psíquico, Comisario, Sacerdote), sistema Fe/Corrupción completo, nave con 4 instalaciones.
- **Fase 2:** mapa de sector con 3 planetas y relojes.
- **Fase 3:** resto de planetas, defensa de nave, La Herida.
- **Riesgos de diseño a vigilar:** (1) demasiados sistemas simultáneos → el jugador nuevo se ahoga: introducir mecánicas por planeta lo mitiga; (2) IP de Games Workshop: como proyecto comercial necesitaría licencia — versión "serial numbers filed off" con lore propio como plan B; (3) el doble reloj (sector + expedición) puede frustrar: incluir modo "Peregrinaje" sin relojes.
