# PURGA — VERTICAL SLICE: VHARSIS PRIMA
### Documento de diseño detallado del Planeta 1 (Culto Genestealer) — con números de balance v0.1

> Objetivo: un planeta completo, jugable y **exigente**. El juego no lleva de la mano: no hay tutoriales intrusivos, la información se gana jugando (y muriendo), y la derrota es parte del bucle. La vara de medir: un jugador nuevo debería fracasar en sus primeras expediciones profundas, perder personajes queridos, y aun así ver siempre — en retrospectiva — qué decisión lo mató. Difícil y justo; nunca fácil, nunca tramposo.

---

## 1. Filosofía de balance (leer antes que nada)

Reglas que gobiernan todos los números del documento:

1. **Precisión con hueco para el desastre.** Precisión efectiva del héroe ~80–85%; la del enemigo ~70–80%. Fallar el disparo decisivo pasa, y pasa en el peor momento. El plan del jugador debe sobrevivir a 1–2 fallos; si solo funciona cuando todo acierta, era mal plan.
2. **Regla del 4–6... si juegas bien.** Un combate bien enfocado dura 4–6 rondas. Un combate mal enfocado (ignorar al Susurrante, pegar al tanque) se alarga a 8+ y el desgaste extra es el castigo. El juego no avisa de la prioridad correcta: se deduce o se sufre.
3. **La Corrupción siempre gana la guerra larga.** Existe contrajuego para cada fuente, pero la mitigación total disponible NUNCA cubre el ingreso total (ver §6). Toda expedición deja marca. El jugador no gestiona la Corrupción para eliminarla: la administra para decidir QUIÉN la carga y cuándo rota al banquillo. Como la antorcha de DD: no se vence, se negocia.
4. **Telegrafía mínima.** Solo los golpes de "muerte instantánea de campaña" (la Llamada del Patriarca, la huida del Magus) se anuncian 1 ronda antes. Todo lo demás — mecánicas de mini-bosses, efectos de curiosidades, debilidades enemigas — es información oculta que se aprende jugando y se apunta en el Librarium tras verla una vez (bestiario ganado, no regalado). La primera vez con la Madre de la Camada, sus nidos NO están señalados: descubres que invoca cuando invoca.
5. **Curación escasa y con coste.** La curación en combate es débil; el campamento existe **una vez por substage**, hay que gastar un hueco de inventario en el kit de campamento, y acampar tiene 20% de emboscada nocturna (sin formación elegida, sin ronda de preparación). Descansar es una apuesta, no un botón de reset.
6. **Presupuesto de élite:** máximo 1 mini-boss por substage, nunca en la primera sala... pero SIN marcar en el mapa. Una "lectura anómala" solo aparece si llevas Escriba o servocráneo augur: la información es un slot de equipo, no un regalo.
7. **El inventario es la primera derrota.** 16 huecos de inventario para suministros Y botín. Llevarlo todo es imposible: cada compra desplaza botín futuro y cada tesoro te pregunta qué tiras. La avaricia mata más que el Patriarca.
8. **No hay dificultad "normal".** El juego descrito aquí ES la dificultad base. Existirá un modo superior (relojes más rápidos, muerte permanente de la partida al perder la nave), nunca uno inferior con la excepción de accesibilidad.

### 1.1 Sistema de resolución (núcleo)
- **Impacto:** `Probabilidad = Precisión de la habilidad − Esquiva del objetivo` (mín. 5%, máx. 95%).
- **Daño:** rango fijo por habilidad (ej. 4–7), modificado por % de arma y debuffs.
- **Crítico:** % por habilidad; un crítico del héroe además baja 5 de Corrupción al atacante ("el Emperador guía mi mano"); un crítico enemigo sube +8 de Corrupción al golpeado.
- **Velocidad:** iniciativa = VEL + 1d6 por ronda.
- **Blindaje:** reducción plana de daño (solo élites y jefes en Vharsis: 1–3).
- **Resistencias:** % de resistir Sangrado / Toxina / Aturdimiento / Movimiento / Corrupción / Debuff.

### 1.2 Las dos barras
- **Salud:** a 0 → **Al Borde de la Muerte** (como DD): cualquier golpe posterior requiere prueba de muerte (33% de morir, base). Curar 1 PV saca del estado.
- **Corrupción 0–200:** a 100, **Prueba de Alma** (base: 75% Quebranto / 25% Momento de Fe; la Fe alta mejora el ratio +4% por punto de Fe). A 200: Perdición (ver GDD §4.2).
- **Fe 0–10:** empieza en 3. +1 por victoria de combate sin muertes, +1 por rituales/curiosidades sagradas, −1 por huir, −2 por ver morir a un aliado, −1 por actos impíos. Cada punto de Fe da **+3% resistencia a Corrupción** pasiva.

---

## 2. El planeta: estructura y campaña local

**Vharsis Prima**, ciudad colmena de 12.000 millones de almas. El Culto de la Mano Abierta lleva tres generaciones incubándose. Nadie lo sabe. Tú sí.

### 2.1 Stages
| Stage | Nombre | Nivel enemigo | Substages | Cierre |
|---|---|---|---|---|
| 1 | Puertas de la Colmena | Aprendiz (0–1) | 1-1, 1-2, 1-3 | Jefe: **El Predicador de la Mano** |
| 2 | Niveles Medios | Veterano (2–3) | 2-1, 2-2, 2-3, 2-4 | Jefe: **El Magus Ophenn** |
| 3 | Subcolmena | Campeón (4–5) | 3-1, 3-2, 3-3 | Jefe: **El Primus y la Guardia Purificada** |
| F | El Nido | Campeón+ (6) | F-1 (única, larga) | **EL PATRIARCA** |

### 2.2 Reloj de amenaza local: "El Despertar" (8 fases)
Avanza 1 fase cada 2 semanas imperiales. Efectos acumulativos:
- **Fase 2:** aparecen Neófitos armados en Stage 1 (antes solo cultistas).
- **Fase 3:** los precios en Vharsis suben 20% (el culto controla el comercio).
- **Fase 4:** 1 recluta de la Leva de cada 4 es un infiltrado (detectable en Celdas).
- **Fase 5:** híbridos de 3.ª generación en todos los Stages; emboscadas nocturnas +50%.
- **Fase 6:** el Magus (si vive) lanza un ritual: +10 Corrupción a todo el roster por semana.
- **Fase 7:** purestrains (Genestealers puros) pueden aparecer en cualquier combate (1 máx.).
- **Fase 8:** **La Llamada.** El Patriarca contacta con la Flota Enjambre. Tienes 4 semanas de gracia para matarlo o el planeta cae.
- **Frenos:** matar al jefe de un Stage retrasa el reloj 1 fase. Misiones secundarias "de contención" (destruir células) lo congelan 2 semanas.

### 2.3 Objetivos de misión del planeta
- *Purga:* eliminar todos los combates del mapa.
- *Quemar los nidos:* destruir 3 salas-nido con promethium. Los nidos NO aparecen marcados: el contador muestra "Nidos: 0/3" y se descubren explorando (una sala-nido se reconoce visualmente al entrar o al asomarse desde una sala adyacente). El Escriba en el grupo o un servocráneo augur los revelan en el mapa desde el inicio: la información cuesta un slot, como todo.
- *Contrainteligencia:* recuperar la lista de miembros del culto en un cogitador (escolta del Escriba opcional: +recompensa).
- *Rescate:* escoltar a un VIP hasta la extracción. El VIP viaja como **quinta figura no controlable** en un "hueco fantasma" tras la formación: no actúa, no lucha. Los enemigos PUEDEN atacarlo (el Sicario y los Hormagantes lo priorizan); tiene sus propios PV (12) y si muere, la misión falla en el acto. Al encontrarlo se marca su posición en el mapa, no la ruta: la extracción es el punto de entrada por defecto (el camino ya explorado... que puede repoblarse), o cualquier sala si la Señal de Vox está alta. Proteger al VIP en combate: "Cuerpo a tierra" del Veterano y el "Desafío" de la Hermana cubren su hueco.
- *Decapitación:* misiones de jefe.

---

## 3. Clases jugables del slice (5) — con números

Formato de habilidad: **Nombre** (posición de uso → posición objetivo) | PRE / DAÑO / CRIT / efecto. Nivel 0. Se equipan 4 de 7.

Stats base a nivel 0 (subida por nivel: +10% PV, +5 PRE, +1 daño cada 2 niveles):

| Clase | PV | **Armadura** | Esquiva | VEL | Resist. Corrupción |
|---|---|---|---|---|---|
| Veterano de la Guardia | 27 | 2 | 8 | 4 | 30% |
| Hermana de Batalla | 24 | 3 | 10 | 5 | 50% |
| Comisario | 22 | 1 | 12 | 6 | 40% |
| Psíquico Sancionado | 17 | 0 | 6 | 7 | 20% |
| Sacerdote del Ministorum | 25 | 1 | 5 | 3 | 45% |

**Armadura:** reducción plana de TODO daño físico recibido (mín. 1 de daño siempre). La Hermana con servoarmadura encaja tiros que revientan al Psíquico. La Forja la mejora por rangos (+1 por rango, máx. +2). **La ignoran:** daño psíquico, Corrupción, Toxina y Sangrado (el DoT entra "por dentro"): así la armadura no anula el terror ni los estados, solo el plomo.

**Posiciones — cómo funciona (regla DD):** no hay penalización pasiva por estar "fuera de sitio". Lo que ocurre es que cada habilidad declara desde qué posiciones se usa y a cuáles alcanza: un personaje desplazado simplemente no puede usar sus habilidades equipadas — su turno se reduce a Moverse, usar un consumible o pasar. Un Sacerdote empujado a posición 1 no tiene malus: tiene un turno muerto, salvo que haya equipado el Eviscerador "por si acaso". Por eso equipar 4 de 7 habilidades es una decisión de seguro contra el caos, y por eso las emboscadas que barajan la formación son tan temidas.

### 3.1 Veterano de la Guardia (posiciones 1–3) — daño fiable, el ancla
1. **Descarga de lasgun** (2-3 → cualquiera): PRE 90 / 5–9 / 5% / consume munición.
2. **Fijar bayonetas** (cualquiera): cambia a modo melé el resto del combate: +2 daño en pos. 1-2, ya no gasta munición.
3. **Estocada de bayoneta** (1-2 → 1-2): PRE 95 / 6–10 / 8% / solo en modo melé.
4. **Fuego de supresión** (3-4 → 2-3): PRE 85 / 2–4 / 0% / −10 PRE al objetivo 2 rondas; consume 2 munición.
5. **Granada frag** (2-3 → 2+3 enemigas): PRE 80 / 4–7 en área / 5% / 1 uso por combate.
6. **"¡Por Cadia!"** (cualquiera): +2 Fe propio, +10 PRE al grupo 1 ronda. 1/combate.
7. **Cuerpo a tierra** (cualquiera): +15 Esquiva propia, protege al aliado con menos PV esta ronda.

*Rol de balance:* 6–8 de daño medio por ronda sin condiciones ni preparación. Es el suelo fiable del grupo — pero un grupo de "suelos fiables" pierde la guerra de Corrupción: la fiabilidad también tiene coste de oportunidad.

### 3.2 Hermana de Batalla (posiciones 1–2) — melé sagrado y motor de Fe
1. **Espada de poder** (1-2 → 1-2): PRE 90 / 6–11 / 10%.
2. **Bólter de mano** (2-3 → 2-3): PRE 85 / 4–8 / 6% / munición.
3. **Fe ardiente** (1 → 1+2): PRE 85 / 5–8 / 5% / llamas: 2 de quemadura 3 rondas; los enemigos ABERRANTES reciben +50% (así el tanque enemigo tiene contrajuego).
4. **Acto de Fe: Escudo del Emperador** — gasta 2 Fe: el grupo recibe −30% daño 2 rondas.
5. **Acto de Fe: Juicio** — gasta 3 Fe: siguiente ataque propio PRE 100, daño ×2, crítico garantizado contra objetivos con Corrupción visible (élites del culto).
6. **Himno de guerra** (cualquiera): −6 Corrupción a los dos aliados adyacentes.
7. **Desafío** (1 → provocación): los enemigos deben atacarla 1 ronda; +10 Esquiva mientras.

*Rol de balance:* la única clase que convierte la barra de Fe en potencia ofensiva/defensiva inmediata. Sin Fe acumulada es simplemente "buena"; con Fe, gana combates.

### 3.3 Comisario (posiciones 2–3) — líder, control, la decisión difícil
1. **Pistola bólter** (2-3 → cualquiera): PRE 88 / 4–8 / 8% / munición.
2. **Sable de energía** (1-2 → 1-2): PRE 90 / 5–9 / 6%.
3. **"¡Ni un paso atrás!"** (cualquiera): el grupo es inmune a empuje/tirón 2 rondas y +10% resistencia a Corrupción.
4. **Arenga férrea** (→ 1 aliado): −10 Corrupción y cura 2 PV ("el miedo es el enemigo").
5. **Señalar al hereje** (→ 1 enemigo): el objetivo recibe +20% daño de todos 2 rondas (el multiplicador de grupo).
6. **Disparo de advertencia** (2-3 → cualquiera): PRE 95 / 1–2 / 0% / Aturdimiento (110% contra no-élites): el control fiable del slice.
7. **EJECUCIÓN SUMARIA** (→ aliado Quebrado o con 150+ Corrupción): mata al aliado. El resto: −30 Corrupción, +2 Fe, +15% daño 2 rondas. Fuera de combate también usable.

*Rol de balance:* poco daño propio; multiplica al grupo. "Señalar al hereje" + foco del grupo debe matar a cualquier no-élite en 1 ronda: esa es la matemática que enseña a priorizar.

### 3.4 Psíquico Sancionado (posiciones 3–4) — el nuker con precio
Cada poder tira en **Peligros del Warp** (d20 al lanzar): 1–2 = pifia (ver tabla). Puede **Quemar cordura**: +20 Corrupción propia para ignorar la tirada.
1. **Toque psíquico** (3-4 → cualquiera): PRE 90 / 3–6 / 5% / sin tirada de Peligro (su ataque "seguro").
2. **Lanza de fuego warp** (3-4 → cualquiera): PRE 85 / 8–14 / 12% / Peligro.
3. **Aplastamiento mental** (3-4 → 3-4): PRE 85 / 4–7 / 5% / +15 Corrupción al enemigo… los enemigos del culto también tienen barra de Cohesión (ver §4.1) / Peligro.
4. **Barrera cinética** (→ 1 aliado): Blindaje +3 durante 3 rondas / Peligro.
5. **Premonición** (grupo): +10 Esquiva grupal 2 rondas y revela salas adyacentes / Peligro.
6. **Grito warp** (3-4 → todos los enemigos): PRE 80 / 3–5 área / 0% / empuja 1 posición / Peligro.
7. **Contención** (propia): −15 Corrupción propia, pierde el turno siguiente ("respirar el Warp despacio").

**Tabla de pifia (1d6):** 1–2: +25 Corrupción propia; 3: 4–8 de daño a sí mismo; 4: el poder golpea a un aliado; 5: −3 Fe del grupo (susurros); 6: **Intrusión** — se añade 1 Horror Menor al combate (8 PV, prioriza al Psíquico). *Nota de balance:* la pifia es un 10% por lanzamiento; con 3 poderes por combate ≈ 27% de que pase algo malo por combate. Doloroso, no letal.

### 3.5 Sacerdote del Ministorum (posiciones 1–2 o 4) — el sostén
1. **Eviscerador** (1-2 → 1-2): PRE 85 / 7–12 / 8% / VEL −2 la ronda que lo usa (arma enorme).
2. **Sermón de la Llama** (4 → grupo): −8 Corrupción a todo el grupo. Su botón principal.
3. **Absolución** (→ 1 aliado): −20 Corrupción, +1 Fe. 2 usos por combate.
4. **Ungüentos y rezos** (→ 1 aliado): cura 4–6 PV, quita Sangrado.
5. **¡Arded, herejes!** (frasco, 4 → 2-3): PRE 85 / 3–5 / quemadura 2×3 rondas / munición (frascos de promethium).
6. **Martirio** (pasiva equipable): cuando un aliado caería Al Borde de la Muerte, el Sacerdote absorbe la mitad del golpe (1/combate).
7. **Letanía del Odio** (grupo): +15% daño contra ABERRANTES e HÍBRIDOS 3 rondas (la tecla específica anti-facción).

*Rol de balance:* el grupo estándar del slice es Sacerdote(4)–Psíquico(3)–Veterano/Comisario(2)–Hermana(1). El Sacerdote convierte la guerra de desgaste de Corrupción en algo ganable: **su "Sermón" contrarresta exactamente el "Cántico" del enemigo tipo Iniciado Susurrante (+8). Esa simetría es intencional y es la lección nº1 de balance del slice: cada agresor tiene su antídoto.**

---

## 4. Facción enemiga: el Culto de la Mano Abierta

### 4.0 El Librarium Táctico (diario y bestiario)
Menú consultable en cualquier momento (nave y expedición), dos pestañas:
- **Glosario:** explica cada mecánica ya encontrada — Corrupción y sus umbrales, Fe, Cohesión, cada estado (Sangrado, Toxina, Quemadura, Aturdido, Marcado...), la Señal de Vox, las Pruebas de Alma. Las entradas aparecen la primera vez que la mecánica te toca (la primera Prueba de Alma añade su entrada, con una nota del Inquisidor de sabor). Nada de muros de texto al empezar: el manual se escribe con tu partida.
- **Bestiario (se gana matando):** cada enemigo tiene 3 niveles de conocimiento: **1 muerte** = retrato, descripción y ataques que ya le has visto usar; **3 muertes** = stats completas (PV, Esquiva, Armadura, resistencias) y debilidades; **jefes** = su entrada revela mecánicas solo tras el primer encuentro (ganes o pierdas — perder contra un jefe al menos compra conocimiento, lo que hace el segundo intento parte del diseño y no un castigo). Con el Librarium de la nave mejorado, el nivel 2 de conocimiento se puede COMPRAR para enemigos aún no dominados: el dinero como atajo de información, nunca de poder.

### 4.1 Mecánica de facción: **Cohesión** (la barra de estrés ENEMIGA)
Los cultistas son fanáticos, pero humanos. Los grupos enemigos con miembros HUMANOS/HÍBRIDOS tienen una barra de Cohesión compartida (30 base). Baja con: críticos recibidos (−6), muerte de un aliado (−8), muerte de su líder (−15), habilidades de Fe (−4). A 0: los no-élites huyen o se rinden (XP completa, botín parcial, opcional: prisionero).
- **Por qué existe:** da al jugador una segunda condición de victoria y hace que las builds de Fe/críticos tengan identidad. Los PURESTRAIN y ABERRANTES no tienen Cohesión (los monstruos no dudan) — así el terror escala hacia el final.

### 4.2 Roster enemigo (Stage 1–2; entre paréntesis, versión Veterana)

| Enemigo | Tipo | PV | Esq | VEL | Ataques principales |
|---|---|---|---|---|---|
| **Cultista de la Mano** | Humano | 8 (12) | 5 | 3 | Cuchillo 2–4; Pistola auto 2–5. Carne de cañón: existe para que el grupo sienta poder |
| **Neófito armado** | Humano | 12 (18) | 8 | 4 | Escopeta 4–7 (pos 1-2); recarga 1 ronda tras 2 disparos (ventana de castigo) |
| **Iniciado Susurrante** | Humano | 10 (15) | 10 | 5 | *Cántico de la Mano*: +8 Corrupción a 2 héroes. NO ataca a la vida. Prioridad de foco que el juego enseña |
| **Híbrido de 3.ª gen** | Híbrido | 16 (24) | 12 | 6 | Garra 5–8 con Sangrado 2×2; salta de posición al atacar |
| **Aberrante** | Aberrante | 30 (44) | 2 | 2 | Maza minera 7–12, aturde 25%. Blindaje 2. Débil a fuego (+50%). Lento: siempre actúa último |
| **Sicario del culto** | Humano | 14 (20) | 15 | 7 | Rifle 5–9 solo contra pos. 3-4 (castiga a tu retaguardia); Esquiva alta: contrajuego = aturdir o marcar |
| **Adepto biófago** | Híbrido | 13 (19) | 8 | 4 | Toxinas 3–5 + Toxina 3×3; cura 6 a un aliado no-aberrante. El "sanador enemigo" clásico |
| **Purestrain Genestealer** (Stage 3+) | Purestrain | 26 (34) | 18 | 9 | Garras rasgadoras 8–13, crit 15%, 2 acciones si nadie lo ha golpeado esa ronda. Sin Cohesión. Aterrador a propósito: +10 Corrupción a quien golpea |
| **Horror Menor** (solo por pifia psíquica) | Demonio | 8 | 12 | 8 | Zarpazo 3–5 +5 Corrupción |

### 4.2 bis — Organismos de vanguardia tiránidos (Stage 3 y el Nido)

En la Subcolmena y el Nido empiezan a aparecer bioformas que NO pertenecen al culto: la vanguardia de la Flota Enjambre, llegada en esporas hace meses. Nadie del culto las controla — a veces ni conviven bien con él. Son escasas (1 por combate como máximo en Stage 3; algo más en F-1), nunca tienen Cohesión, y su sola presencia sube +5 de Corrupción al grupo al iniciar el combate ("esto ya no es una herejía; es una invasión").

| Enemigo | PV | Esq | VEL | Ataques y mecánica |
|---|---|---|---|---|
| **Enjambre de rippers** | 14 | 8 | 5 | 2 mordiscos por turno de 2–4. Si hay un cadáver en el combate (aliado o enemigo), lo devora en 1 turno y cura 6. Enemigo "de limpieza": castiga los combates largos y da un uso nuevo a destruir cadáveres |
| **Hormagante** | 15 | 14 | 8 | Garras saltarinas 5–8; al matar o al fallar contra él, salta de posición. Aparece en parejas; barato pero frenético |
| **Termagante** | 13 | 10 | 6 | Devorador 4–7 a cualquier posición + Toxina 2×2. La "infantería a distancia" xenos |
| **Guerrero Tiránido** (élite, solo F-1) | 48 | 10 | 7 | Espada ósea 8–13 / Devorador 5–9 en área 2 posiciones. Blindaje 2. Sinapsis: mientras vive, las demás bioformas del combate ganan +10 PRE y no pueden ser empujadas. Matarlo primero desorganiza al resto (−1 VEL, −10 PRE): el "puzle de foco" invertido del final |

**Composiciones mixtas (Stage 3 / Nido):** Purestrain + Enjambre de rippers (el purestrain mata, los rippers reciclan); 2 Hormagantes + Sicario del culto (velocidad xenos + fuego humano: el culto y la vanguardia cazando juntos, imagen inquietante a propósito); Guerrero Tiránido + 2 Termagantes (solo en F-1, antesala del Patriarca).
**Nota de balance:** las bioformas pegan más fuerte que el culto pero no atacan la Corrupción (salvo el +5 inicial): en el fondo del Nido el peligro vuelve a ser físico, cuando tus recursos de curación ya están agotados por el viaje. El orden de los peligros es la curva de dificultad.

### 4.2 ter — Mini-boss errante del Nido: **El Lictor**
55 PV, Esquiva 25, VEL 10. No aparece en salas: **te sigue por los pasillos de F-1.** Cada 2 salas, tirada de acecho: si te alcanza, emboscada garantizada donde actúa primero y marca a un héroe (*Presa*: +30% daño recibido hasta que el Lictor muera o huya). Tras 3 rondas se desvanece en la pared de biomasa y vuelve a acechar, con la vida que le quede. Muere de verdad solo si lo bajas a 0 antes de que escape.
- Contrajuego (a descubrir): el promethium encendido en una sala impide su entrada esa vez; el "Ojo del Magus" revela su posición en el pasillo; los golpes de área le impiden desvanecerse esa ronda.
- Es el terror pasivo de la misión final: incluso entre combates, F-1 nunca da respiro. Botín si lo matas: **Glándula del Depredador** (talismán: +15% crit / el portador no puede ser sorprendido).

**Composiciones tipo (Stage 1):** 2 Cultistas + 1 Neófito + 1 Susurrante; 1 Aberrante + 2 Cultistas; 2 Neófitos + 1 Biófago. Las composiciones NO se etiquetan ni se explican: el jugador deduce las prioridades o paga el precio. Desde la fase 2 del Despertar aparecen composiciones "sucias": 2 Susurrantes a la vez (la Corrupción se dispara si no hay respuesta), o Sicario + Biófago (tu retaguardia sangra mientras el sanador enemigo deshace tu trabajo).
**Emboscadas:** 15% en pasillos (35% con Señal de Vox nula, +20% de noche/acampando): el enemigo actúa primero y tu formación se baraja aleatoriamente. La formación rota con un Sacerdote en posición 1 es una sentencia — otro motivo para que cada personaje equipe al menos una habilidad usable fuera de su posición ideal. El juego nunca lo dirá: la segunda emboscada enseña lo que la primera castigó.
**Regla de encuentro:** el daño enemigo esperado por ronda contra el grupo ≈ 12–16 a nivel 0; la mitigación y curación disponibles (~5–7/ronda) dejan un desgaste neto de ~7–9 PV por ronda de combate. Una substage de Stage 1 tiene 6–8 combates: el grupo NO puede limpiarla entera en perfectas condiciones. Llegar al final implica elegir ruta (qué salas evitar), gastar el campamento en el momento justo o retirarse con lo puesto. La retirada a mitad de substage no es fracaso: es la decisión correcta el 30% de las veces, y aprender a olerlo es el verdadero skill del juego.

### 4.3 Mini-bosses de Vharsis
- **El Predicador Hueco** (élite errante, salas sin marcar): 45 PV, Blindaje 1. *Sermón Hueco* +10 Corrupción al grupo cada ronda mientras esté en pos. 3-4; si lo traes a pos. 1-2 (empujes/tirones) queda Silenciado y pega débil. Nada de esto se explica: la solución se descubre empujándolo por accidente o sufriendo. Botín: reliquia garantizada.
- **La Madre de la Camada:** 60 PV. Invoca 2 Cultistas por ronda, sin aviso, hasta que destruyas los 2 nidos de la sala (12 PV cada uno, arden ×2) — que a primera vista parecen decoración del escenario. La primera vez, el jugador pierde el combate contra la marea o huye; la segunda, llega con promethium. Así se aprende aquí.

### 4.4 Jefes de Stage (fases y contrajuego)

**S1 — El Predicador de la Mano** (90 PV, Cohesión propia 60)
- Fase 1: sermones (+10 Corrupción área) y 2 guardias Neófitos.
- Fase 2 (<50%): *Revelación* — se abre el hábito: es un híbrido. +daño, pierde el sermón.
- **Contrajuego:** su Cohesión baja el doble con habilidades de Fe. Un grupo con Hermana+Sacerdote puede "romperlo" moralmente antes de la fase 2. Dos maneras de ganar = jefe bien diseñado.

**S2 — El Magus Ophenn** (130 PV, Blindaje 1)
- Poderes psíquicos: *Dominio* (controla a un héroe 1 ronda; el Comisario con "¡Ni un paso atrás!" lo bloquea), *Espejo warp* (refleja el próximo ataque: se anuncia; la respuesta correcta es un ataque débil o pasar).
- A <30% intenta HUIR (2 rondas de canalización): si escapa, reaparece en el Stage 3 con +30 PV y el reloj avanza 1. **Matar al Magus a tiempo es el clímax del midgame.**

**S3 — El Primus y la Guardia Purificada** (Primus 110 PV + 2 Purestrain)
- El Primus marca a un héroe (*Presa*): los Purestrain le hacen +30%. Contrajuego: "Cuerpo a tierra" del Veterano, "Desafío" de la Hermana, o matar al Primus rápido (los Purestrain sin Primus pierden la coordinación: −1 acción).

### 4.5 EL PATRIARCA (jefe planetario) — 3 fases, 240 PV totales
Arena: el Nido. 2 **Válvulas de biomasa** en los flancos (20 PV cada una) curan al Patriarca 8/ronda mientras existan.
- **F1 "El Trono de Carne" (240→160):** ataques de garra 9–14, invoca 1 Purestrain cada 3 rondas. Prioridad: quemar válvulas (débiles a fuego ×2 → el promethium y "Fe ardiente" brillan).
- **F2 "La Sombra en la Mente" (160→80):** ataque psíquico: +12 Corrupción a 2 héroes/ronda + *Llamada de la Progenie* (aviso 1 ronda; interrumpible con Aturdimiento — el "Disparo de advertencia" del Comisario tiene aquí su momento estelar, PRE reducida a 60% vs jefe: posible, no garantizado).
- **F3 "El Ascenso" (80→0):** desciende del trono: melé brutal 12–18 pero pierde los poderes psíquicos y su Esquiva cae a 0. La fase final es un DPS race honesto: si llegaste con recursos, ganas.
- **Escalado por reloj:** +15 PV por fase de Despertar consumida y, en fase 7+, empieza con 1 Purestrain extra.
- **Recompensa:** estabiliza Vharsis, 3 reliquias únicas, desbloquea al recluta legendario *Hermana Superiora Vex* y el logro de campaña.

---

## 5. Objetos, talismanes y consumibles

### 5.1 Suministros de expedición (compra pre-misión)
| Suministro | Precio | Efecto |
|---|---|---|
| Cargador de lasgun ×6 | 75 | Munición estándar |
| Frasco de promethium | 100 | Quema nidos/biomasa; en combate: +50% vs Aberrantes |
| Ración de campaña | 60 | Cura 3 PV fuera de combate; evita hambre en pasillos |
| Sello de pureza | 200 | −25 Corrupción a un personaje (uso único). Caro adrede: la Corrupción se gestiona, no se compra |
| Agua bendita | 90 | Purifica curiosidades profanadas; +10% res. Corrupción 1 combate |
| Kit médico | 90 | Quita Sangrado/Toxina fuera de combate; cura 4 |
| Ganzúas | 50 | Abre celdas y cofres cerrados |
| Pala | 40 | Escombros y... cierto Ogryn |
| Baliza vox | 120 | +2 niveles de Señal al plantarla (1 uso) |
| Servocráneo médico | 150 | Sigue al grupo: cura 2 PV al más herido cada fin de combate |

**Presupuesto:** una misión de Stage 1 recompensa ~900–1.200 tronos; el suministro "correcto" cuesta ~450. Margen real de beneficio, pero equivocarse de compra duele. Como DD, la tienda es el primer combate.

### 5.2 Bebidas y estimulantes (consumibles en combate, máx. 2 por personaje)
| Consumible | Precio | Efecto | Riesgo |
|---|---|---|---|
| Recaf de campaña | 30 | +2 VEL 3 rondas | — |
| Amasec | 45 | −10 Corrupción en combate | −5 PRE 2 rondas |
| Estimulante de combate | 80 | +25% daño 3 rondas | Al acabar: −10% res. Corrupción resto del combate |
| Contraveneno | 40 | Quita Toxina, inmunidad 3 rondas | — |
| Incienso sagrado | 70 | El grupo: +15% res. Corrupción 3 rondas | — |
| "Coraje embotellado" (de contrabando) | 60 | Quita Quebranto 1 combate (¡único remedio in situ!) | +15 Corrupción al terminar. La deuda se paga |

### 5.3 Talismanes/Reliquias (equipables, 2 por personaje) — selección de 14
*Comunes (sin coste):*
1. **Placa de identificación cadiana** — +8% res. Corrupción.
2. **Casquillo bendecido** — +5 PRE a distancia.
3. **Rosario de hierro** — +1 Fe máxima.
4. **Botas de subcolmena** — +3 Esquiva, +1 VEL.
*Raros (con coste, estilo DD):*
5. **Icono de la Legión** (Veterano) — +15% daño en modo bayoneta / −5 Esquiva.
6. **Velo de la mártir** (Hermana) — Actos de Fe cuestan −1 / recibe +10% daño.
7. **Libro de nombres** (Comisario) — "Señalar al hereje" dura 3 rondas / −4 PV máx.
8. **Amuleto de plomo** (Psíquico) — las pifias de Peligro se reducen a 1 (5%) / poderes −10% daño.
9. **Cíngulo del penitente** (Sacerdote) — Sermón −10 Corrupción / él mismo +10% Corrupción recibida.
10. **Filtro rebreather** — inmune a Toxina / −5 PRE.
*Vharsis únicos (botín de jefes/mini-bosses):*
11. **La Mano Cerrada** (del Predicador) — +20% daño contra HÍBRIDOS; los Susurrantes te ignoran.
12. **Ojo del Magus** (del Magus) — ves la intención enemiga de la próxima ronda / +5% Corrupción recibida. (Talismán "de lectura": recompensa saber, no stats.)
13. **Feromona del Nido** (de la Madre) — los Purestrain te atacan a ti (¡talismán de tanque voluntario!) / +10 Esquiva.
14. **Diente del Patriarca** (super botín) — +2 daño, +10% crit, +10% res. Corrupción, sin coste. El único objeto "perfecto" del slice: el premio existe.

### 5.4 Regla de identificación
El botín del culto (objetos con ★) está "sin identificar": puedes equiparlo a ciegas (efecto oculto, 30% de tener un coste de Corrupción pasiva) o pagar 100 tronos en la nave. La avaricia siempre tiene precio, nunca es trampa: el % es público.

---

## 6. Economía de la Corrupción (la cuenta que hace el juego exigente pero justo)

Ingresos de Corrupción esperados en una substage de Stage 1 (6 combates, ~30 rondas):
- Susurrantes, críticos enemigos y emboscadas: ~90–110 puntos repartidos en el grupo.
- Curiosidades falladas, salas sin señal, sorpresas: ~30–40.
**Total: ~120–150 por expedición.**

Mitigación máxima sin gastar tronos (y jugándolo todo perfecto): Sermón del Sacerdote (~40), Himno de la Hermana (~24), Arenga del Comisario (~20), campamento (−15 grupal, si no te emboscan), Fe pasiva (~10%).
**Total mitigable: ~95–105.**

→ **La cuenta NO cierra a propósito.** Incluso el grupo perfecto sale con +20 a +50 de Corrupción acumulada. Sin especialistas de alma, con +80 o más. Consecuencias de diseño:
- **La rotación de roster es obligatoria**, no opcional: necesitas 2 grupos y banquillo, y personajes "quemados" descansando en el Reclusiam (que cuesta tronos y semanas — y las semanas mueven el reloj del Despertar). La tensión tiempo/dinero/cordura es el juego.
- **Las Pruebas de Alma ocurrirán.** El diseño asume 1 Prueba cada 2–3 expediciones por grupo. Los Quebrantos no son fallos del jugador: son el estado normal de una campaña, y gestionarlos (¿lo saco?, ¿lo ejecuto?, ¿aguanto una misión más?) es contenido, no castigo.
- **El Sello de Pureza a 200 tronos** es la válvula de emergencia cara: usarlo significa renunciar a una mejora de Forja. Elegir duele siempre.
- **Tornillo de ajuste:** si el playtest muestra que el jugador medio colapsa antes del Stage 2, el primer dial es el coste del Reclusiam (más barato = rotación más fluida), no el Cántico enemigo. La presión debe sentirse en la nave (estrategia), no convertir cada combate en ruleta (táctica).

### 6.1 Economía de tronos (también apretada)
Misión de Stage 1: ~700–900 tronos de recompensa + botín vendible variable. Suministro razonable: ~450. Reclusiam: 150/personaje/semana. Mejora de Forja I: 2.000. **El jugador nunca puede pagar todo lo que necesita a la vez** — priorizar es el metajuego. El botín "extra" real está en las salas peligrosas y los mini-bosses sin marcar: el riesgo es la única fuente de riqueza, exactamente como la oscuridad en DD.

**Tutorial de economía (único momento de mano tendida):** la primera vez que el grupo regresa a la nave, una sola pantalla ilustrada — el libro de cuentas del Inquisidor — resume en 4 líneas el ciclo: *misiones dan tronos → los tronos pagan suministros, curas y mejoras → nunca alcanzará para todo → lo que no cures hoy lo pagarás mañana.* Sin pop-ups posteriores, sin flechas parpadeantes; el detalle vive en el Glosario para quien lo busque.

---

## 7. Progresión del jugador en el slice
- **Nivel 0→6.** XP por misión; jefes dan nivel casi garantizado.
- **Mejoras de nave disponibles en el slice:** Reclusiam I–III (cura de Corrupción semanal 20/35/50), Enfermería I–II, Forja I–II (mejora de armas: +1 daño y +5 PRE por rango), Barracones (roster 8→12→16).
- **Roster recomendado del slice:** 9–10 personajes para rotar (2 grupos y recambios). La Leva ofrece 2 reclutas/semana de las 5 clases.
- **Duración objetivo del slice completo:** 12–18 horas, 25–35 expediciones (incluyendo repeticiones tras retiradas y wipes). Mortalidad esperada en una primera partida: **6–10 personajes muertos o perdidos**, y probablemente un intento fallido contra el Patriarca antes del bueno. El fracaso es contenido: los cuerpos del wipe quedan en el mapa, el Magus que escapó te espera más fuerte, y la segunda partida del jugador será radicalmente mejor que la primera porque el conocimiento — no los números — es la verdadera progresión.

---

## 8. Menú de inicio

Estética: retablo gótico en penumbra; la Sancta Sicaria orbitando Vharsis al fondo, vitrales animados, coro tenue. El cursor es un servocráneo.

- **Continuar** — retoma la campaña (guardado único por partida, autoguardado constante: no hay save-scumming; las decisiones son piedra).
- **Nueva Cruzada** — nombre del Inquisidor, sigilo personal (cosmético) y voto inicial: 3 modificadores opcionales de partida (p. ej. *Voto de Pobreza*: −20% tronos, +recompensa final; *Voto de Sangre*: relojes +25% rápidos). En el slice el Ordo es fijo (Hereticus); los votos son la rejugabilidad temprana.
- **El Memorial** — la sala de los caídos: cada personaje muerto en TODAS tus partidas, con nombre, retrato, causa de muerte y expediciones sobrevividas. El coste humano, contable y permanente. (Robado con orgullo del cementerio de DD.)
- **Librarium** — glosario y bestiario globales (persisten entre partidas: el conocimiento es la verdadera meta-progresión).
- **Opciones** — audio, vídeo, accesibilidad (daltonismo, tamaño de texto, velocidad de animaciones), idioma.
- **Abandonar el Deber** — salir. El botón usa esa frase, y el Inquisidor murmura al pulsarlo.

---

## 9. La base: Sancta Sicaria (versión del slice)

Pantalla lateral de la nave en corte, estilo la Aldea de DD: cada cubierta es un edificio clicable. Entre expediciones pasa 1 semana imperial automáticamente (el reloj del Despertar avanza); cada acción de nave es gratuita en tiempo salvo que se indique.

| Cubierta | Función | Acciones concretas | Mejoras (I→III) |
|---|---|---|---|
| **El Puente** | Centro de mando | Elegir siguiente expedición (mapa de Vharsis, ver objetivos, recompensas y nivel); gestionar el reloj del Despertar; misiones de contención | — |
| **Reclusiam** | Alma | Asignar personajes a oración (−20/−35/−50 Corrupción por semana, 150 tronos); tratar Quebrantos (2 semanas); rituales de Fe (+1 Fe a un personaje, 1/semana) | Capacidad 2→4→6 plazas |
| **Enfermería** | Cuerpo | Curar heridas persistentes y enfermedades (1 semana, 100 tronos); estabilizar a los que salieron Al Borde de la Muerte | Capacidad y velocidad |
| **Forja del Magos** | Equipo | Mejorar armas (+1 daño, +5 PRE por rango) y armaduras (+1 Armadura por rango) POR PERSONAJE; comprar suministros de expedición; identificar botín ★ (100 tronos) | Rangos de mejora desbloqueados por nivel de personaje |
| **Refectorio** | Vínculos | Ver y gestionar vínculos; asignar 2 personajes a "rancho compartido" (−10 Corrupción a ambos, gratis, 1 pareja/semana; probabilidad de formar vínculo) | Más parejas/semana |
| **Barracones** | Roster | La Leva: 2 reclutas nuevos/semana (nivel 0, quirks visibles solo en parte); despedir personajes; ver el roster completo (8 plazas → 12 → 16) | Reclutas de nivel 1+ en rangos altos |
| **Celdas** | Contrainteligencia | Interrogar reclutas sospechosos (1 interrogatorio/semana; falso positivo posible: ¿despides a un inocente?). Ver 9.1 | Fiabilidad 70%→85%→95% |
| **Librarium** | Conocimiento | Glosario/bestiario; comprar conocimiento nivel 2 de enemigos; investigar jefes tras el primer encuentro (revela una mecánica extra) | Coste de compra reducido |

**Eventos de nave:** al inicio de cada semana, 40% de evento (dilema de texto con 2–3 opciones): un polizón pide asilo, el Magos ofrece "mejorar" a un Ogryn, un predicador incendiario sube la Fe del roster pero también la Corrupción de los escépticos... Los eventos son el pegamento narrativo entre expediciones y muchos dan quirks, objetos o consecuencias a semanas vista.

### 9.1 Infiltración: las tres vías del culto

Cualquier personaje que entre en tu roster puede ser un infiltrado durmiente (híbrido de 4.ª generación o cultista converso). Tres vías de entrada, con riesgo creciente:

| Vía | Riesgo de infiltrado | Detalle |
|---|---|---|
| **La Leva semanal** | 0% antes del Despertar fase 4; **25%** desde fase 4 | La vía "segura" que deja de serlo si dejas correr el reloj: la ciudad que te envía reclutas ya es del culto |
| **Eventos de nave** | **~35%** en eventos de acogida | El polizón que suplica asilo, los supervivientes de una lanzadera a la deriva, el guardia "desertor del culto" que ofrece información... Los eventos de acogida lo avisan en el propio texto con ambigüedad ("sus ojos no parpadean lo suficiente"): aceptar es siempre una apuesta declarada |
| **Reclutas de mazmorra** | **Prisioneros: 40%. Supervivientes escoltados: 15%** | Los prisioneros de las celdas del culto son la vía más golosa (llegan con niveles y quirks buenos) y la más envenenada: ¿por qué el culto los mantenía vivos? |

**Cómo actúa un infiltrado no detectado** (tirada oculta cada expedición en la que participa, o cada 2 semanas en la nave):
- *Sabotaje menor:* desaparecen suministros del inventario a mitad de expedición; la Señal de Vox cae un nivel sin motivo.
- *Delación:* +25% de emboscadas en su expedición (está guiando al enemigo).
- *Traición* (solo si la expedición entra en su Stage de origen o en el Nido): se revela en mitad de un combate — se convierte en Híbrido de 3.ª gen hostil con el equipo que le diste puesto. El combate más memorable del juego, y el argumento definitivo para pagar interrogatorios.
- **Pistas para el jugador atento:** los infiltrados acumulan Corrupción anormalmente despacio, nunca ganan Fe, y rechazan el rancho compartido del Refectorio. Nada de esto se señala: está ahí para quien mira las fichas. La contrainteligencia de verdad es la observación; las Celdas son solo la confirmación.

---

## 10. Flujo de combate (el corazón, paso a paso)

### 10.1 Inicio del combate
1. **Chequeo de sorpresa:** normal / emboscada enemiga (ellos actúan primero y tu formación se baraja) / sorpresa tuya (ronda gratis; se logra con VEL de grupo alta, el Ratling futuro, o entrando desde pasillo con promethium encendido).
2. **Composición visible:** ves qué enemigos hay y sus posiciones. Su información depende del bestiario: enemigos no estudiados muestran "???" en PV y stats.
3. **Iniciativa:** cada figura (héroes y enemigos mezclados) tira VEL + 1d6. Se re-tira CADA ronda: el orden fluctúa, los planes deben ser flexibles.

### 10.2 El turno de un personaje
En su turno, un personaje ejecuta **1 acción principal** + **1 acción menor** (máx. 1 menor por turno):

**Acciones principales:**
- **Habilidad** (de las 4 equipadas, si su posición y la del objetivo lo permiten).
- **Moverse:** desplazarse 1 posición (intercambia sitio con el compañero). Las habilidades con "mueve al usuario" (salto del Híbrido, etc.) hacen esto gratis como parte del ataque.
- **Defender:** +15 Esquiva y −20% daño recibido hasta su próximo turno. La opción del turno "muerto" fuera de posición.
- **Recargar** (si su arma está seca).

**Acciones menores:**
- **Usar consumible** (propio o sobre un adyacente): recaf, contraveneno, amasec... Máx. 2 consumibles llevados por personaje (se asignan al equipar, antes de salir).
- **Pasar objeto** a un adyacente (1 consumible).
- **Gritar una orden** (solo Comisario/líderes con la pasiva): intercambia el orden de iniciativa de dos aliados esta ronda.

### 10.3 Resolución de una habilidad
1. Elegir habilidad → se iluminan los objetivos válidos según posiciones.
2. Tirada de impacto (PRE − Esquiva). Fallo = nada, salvo habilidades "al fallar" (raras).
3. Impacto: daño − Armadura (mín. 1), tirada de crítico.
4. **Aplicación de estados** (si la habilidad los lleva): tirada contra la resistencia del objetivo (ej. Sangrado 100% vs resistencia 40% = 60% de aplicar).
5. Reacciones: Cohesión enemiga baja si procede; Corrupción sube si el golpe era aterrador; los umbrales (Al Borde de la Muerte, Prueba de Alma a 100) se resuelven EN EL ACTO, en mitad de la ronda — una Prueba de Alma en plena refriega, con su animación deteniendo el combate, es un momento firma del juego.

### 10.4 Estados (chuleta completa del slice)
| Estado | Efecto | Se quita con |
|---|---|---|
| **Sangrado** X×N | X daño al inicio de su turno, N turnos. Ignora Armadura | Kit médico, "Ungüentos", vendas |
| **Toxina** X×N | Igual que Sangrado (los rippers y biófagos la usan). Stackea con Sangrado | Contraveneno |
| **Quemadura** X×N | DoT de fuego; los ABERRANTES y nidos reciben ×2 | Se consume sola |
| **Aturdido** | Pierde su próximo turno. Al pasarse: +30 resistencia a Aturdimiento resto del combate (anti-stunlock, para ambos bandos) | — |
| **Marcado** | +% daño recibido de quienes explotan la marca | Expira o muere el marcador |
| **Empujado/Tirado** | Cambio forzoso de posición (resistencia de Movimiento aplica) | — |
| **Buffs/Debuffs** | ±PRE, ±daño, ±Esquiva, ±VEL con duración en rondas, visibles como iconos apilados bajo el retrato | Expiran |

### 10.5 Fin de ronda y fin de combate
- Fin de ronda: DoTs de entorno (sala en llamas, esporas), chequeo de Cohesión enemiga (a 0: huida/rendición), refuerzos si la mecánica lo dicta (Madre de la Camada, rondas 3+ en salas con alarma activada).
- **Retirada del grupo:** acción de grupo declarable al inicio de cualquier ronda. Cada personaje tira (base 60% + VEL): los que pasan salen; los que fallan aguantan una ronda expuestos (+20% daño recibido) y re-tiran. Retirarse de un combate: −1 Fe a todos y el pasillo de vuelta puede repoblarse.
- **Victoria:** botín de la sala, XP, respiro de 3 segundos con el grupo en pie... y de vuelta al mapa. Los DoTs activos NO se curan al acabar: el Sangrado te acompaña al pasillo (los kits médicos existen por algo).

### 10.6 Ritmo objetivo
Un turno de personaje debe resolverse en 5–15 segundos reales (animaciones rápidas, colas de acción, opción de acelerar ×2). Un combate de 5 rondas ≈ 3–5 minutos. Una substage completa ≈ 35–50 minutos. Sesión natural: una expedición + gestión de nave ≈ 1 hora. El juego se juega "una expedición más y lo dejo".
