# Sistema Frankenstein · prototipo 2D

Abre `Assets/Scenes/FrankensteinPrototype.unity` y pulsa Play. Cada run empieza eligiendo Cazadora, Bailarina o Injertador. La semilla propuesta cambia en cada nueva partida; escribir una semilla permite repetir distribución y azar.

## Controles y menús

- WASD o flechas: movimiento continuo por salas y pasillos.
- E: examinar, recoger recursos, usar mesa, altar o escaleras.
- Tab / Esc: menú de exploración, que pausa movimiento e IA.
- Espacio o Continuar: avanzar un diálogo tras su tiempo mínimo de lectura.

En el editor, haz clic en la pestaña Game para darle el foco al teclado. Los controles combinan la lectura nativa con los eventos de esa vista, sin duplicar pulsaciones; al perder el foco se descartan las teclas retenidas.

El menú de exploración contiene Mapa, Personajes, Objetos, Injertos, Equipo y Diario. Al abrir una acción sobre alguien, se elige primero el personaje afectado. Personajes permite abandonar o matar a un aliado, con confirmación; el líder no admite esas acciones desde el menú. Abandonar hace desaparecer al aliado con sus objetos. Matarlo recupera una parte no torácica si queda alguna y devuelve su equipo.

El combate usa una lista compacta en una esquina: acciones, negociación, objetos, huida, lanzamiento y pasar turno. Las habilidades muestran daño base, precisión base y origen al pasar el cursor. Para atacar se puede pulsar una parte visible del cuerpo o elegir personaje y parte en listas. Los aliados también son objetivos válidos; no se permite golpearse a uno mismo. Los enemigos no muestran PV ni puntos débiles en la UI.

## Anatomía y equipamiento separados

Cada cuerpo tiene cabeza, torso, dos brazos y dos piernas independientes. Los injertos son partes naturales individuales: brazo humano, brazo de orco, pierna humana, pata de lobo, cabeza, etc. Una garra o mordisco natural puede aportar acciones, pero una espada nunca forma parte de la definición del brazo.

Equipo tiene mano izquierda, mano derecha, armadura y amuleto. Hay espada, daga, maza, arco, báculo, cruz, sierra, aguja, cintas, escudo y piezas de protección. Un arco ocupa ambas manos y necesita dos brazos. Quitar o cambiar equipo devuelve las instancias anteriores a la mochila. Al perder un brazo, el arma que ya no puede sostenerse deja de aportar su acción; el equipo de un aliado vuelve al inventario. Los ataques naturales restantes siguen disponibles.

El torso es anatomía interna, no un objeto: no se extrae, se ofrece ni se cose. Destruirlo puede matar, pero nunca produce un torso de botín. Los torsos tienen 105–160 PV base según criatura; el caballero usa multiplicador de cuerpo ×2. Son más fáciles de alcanzar que brazos, piernas y cabeza. Las cabezas suelen tener 25–30 PV y un acceso inicial muy inferior; mutilar abre la guardia. La araña conserva una anatomía vital distinta y la primera decapitación del ghoul produce una corona de tentáculos con otra conducta y acciones.

No hay vida global. Críticos, bloqueo, veneno, infección, regeneración y esquiva siguen activos. Los efectos periódicos pertenecen a la pieza concreta y no se transfieren a una cabeza de sustitución.

## Clases

- Cazadora: sustituye al personaje anterior, cuyos PNG de combate y exploración fueron retirados del proyecto. Vestuario gótico de cazadora, espada y daga. Daño ×1,15 y probabilidad de rendición ×1,5 al mutilar. No tiene Amenazar ni bonus de reclutamiento; no puede reclutar a un rendido. Otro aliado puede actuar como interlocutor.
- Bailarina: diseño adulto con pelo corto cobrizo y ropa de inspiración desértica, sin el abanico ni el estilo japonés anterior. Daño ×0,70, defensa ×0,35 redondeada hacia abajo y evasión base +25 puntos. La danza y las cintas aplican seducción. La afinidad de la Bailarina es ×1,6 contra masculinos, ×0,35 contra femeninos y ×0,65 contra otros; en humanos se multiplica además por ×1,25 para masculinos y ×0,5 para femeninos. Esto afecta al estado aplicado y al reclutamiento por Seducir.
- Injertador: sierra y aguja como objetos separados. Puede operar fuera de una mesa y consume un hilo. Su probabilidad de convencer se multiplica por ×0,60.

## Hilos y costura

Cada run comienza con cuatro hilos. Hay carretes recogibles en los pisos y los cofres contienen uno o dos hilos adicionales. Un carrete da dos hilos y desaparece tras recogerlo.

El operador es el Injertador vivo del grupo si lo hay; en otro caso, el líder. El Injertador gasta un hilo y puede trabajar en campo. Los demás necesitan una mesa y gastan dos. Hay mesa en el vestíbulo y junto al descenso. El menú indica operador, acceso y recursos. Sin suficientes hilos no se consume la pieza. La pieza instalada se cura y la anterior vuelve a la mochila; el equipo es independiente. Por defecto los no muertos no admiten costura.

## Rendición, reclutamiento y enfado

Rendición y disposición para negociar son sistemas distintos. La rendición empieza al 0 %, y solo se hace una tirada cuando se pierde una parte durante ese combate; esperar turnos o causar heridas sin mutilación no añade tiradas.

| Poder del perfil | Primera mutilación | Por cada mutilación adicional |
| --- | --- | --- |
| Débil: hasta 1 | 5 % | +2 puntos |
| Medio: mayor que 1 y menor que 2 | 3 % | +1 punto |
| Fuerte: 2 o más | 1 % | +0,5 puntos |

La Cazadora multiplica esa probabilidad por 1,5 cuando provoca el corte. El contador se reinicia entre combates; una muerte por pieza vital no requiere rendición.

Hablar y Ofrecer siguen disponibles según cabeza, brazos y recursos. Seducir es especialidad de la Bailarina. El oro, valor de una pieza, afinidad al soborno, heridas y circunstancias cambian el éxito. El oro es especialmente útil contra el Caballero. Toda oferta válida se consume, acepte o rechace.

Cada enemigo recibe al inicio un límite aleatorio de uno a tres rechazos. Alcanzarlo lo enfurece, aumenta su daño un 15 % y bloquea todo reclutamiento hasta el siguiente combate. Si estaba rendido, vuelve a luchar. Un rechazo previo al límite no permite escapar gratis al enemigo. El enfado y los rechazos se limpian al iniciar otro combate.

Se puede atacar a rendidos durante una lucha. Si ya no quedan enemigos activos, «Atacar al rendido» permite elegir un aliado y una acción para seguir mutilándolo o matarlo. También se puede rematar, dejar marchar o negociar con un interlocutor válido.

## Huida, extremidades y botín

Huir consume la acción. La movilidad de cada personaje se calcula con sus piernas presentes, poder de perfil y velocidad: `(0,15 + piernas) × poder × max(0,2; 1 + velocidad/10)`. La probabilidad usa `45 % × movilidad aliada / movilidad enemiga`, con penalización ×0,65 si hay aliados sin piernas y límites de 2–95 %. Si no queda ninguna pierna aliada, la probabilidad es cero. Los enemigos rendidos no persiguen y no suman movilidad contraria.

El fallo consume turno. El éxito devuelve al vestíbulo con los supervivientes, conserva sus heridas y deja al enemigo en el piso. No cura ni da oro, botín o victoria sobre la sala.

Una pierna perdida multiplica precisión por 0,72 y reduce velocidad. Sin piernas la precisión se multiplica por 0,45, cae mucho la velocidad y se actúa una ronda de cada dos. Un aliado sin brazos ni piernas necesita que otro con brazo gaste su turno en lanzarlo. Conserva su bando, aparece al lado enemigo y recibe una acción ofensiva; después no actúa hasta otro combate, donde vuelve al lado aliado.

Cortar ya no entrega automáticamente un injerto. Examinar restos de un enemigo muerto hace una tirada única del 35 % para recuperar como máximo una parte no torácica, tomada de sus partes cortadas o restantes. No se puede repetir la tirada. Su equipo tiene otra tirada del 25 % para un objeto. Los cofres tienen un 20 % de incluir una parte y un 45 % de incluir equipo. Así las piezas corporales son escasas. Reanimar y examinar el mismo cadáver son decisiones excluyentes.

## Exploración y presentación

Hay tres pisos con 6–9 salas conectadas mediante pasillos físicos, sin puertas que teletransporten. La cámara sigue al personaje elegido. Los enemigos patrullan, comprueban visión a través de zonas transitables y persiguen o huyen según el tipo. Un marcador puede representar varios combatientes. El primer pasillo tiene una presencia visible. Cofres, mesas, hilos, notas, braseros y cajas acompañan el mapa.

Para descender se resuelve al guardián de la salida y se reproduce ante el altar la secuencia de la nota del vestíbulo. El orden cambia entre pisos y las notas indican su piso. Tras resolverlo, E sobre las escaleras permite seguir. La salida del tercero completa la run.

Los diálogos conservan quién habla, enfocan y amplían su figura, colocan el texto en un bocadillo separado y bloquean acciones. Duran entre 2,4 y 5 segundos según texto; tras 0,9 segundos se pueden avanzar con Espacio o Continuar. El siguiente turno y sus efectos esperan a que termine la cola de diálogo. Las protestas y respuestas ofrecen pistas sobre vulnerabilidad, resistencia y soborno.

## Arte y comprobaciones

Los nuevos PNG de combate son `Resources/Frankenstein/Characters/hunter.png`, `dancer-v2.png` y `grafter-unarmed.png`, con frente, espalda y presentación. Los nuevos atlas de exploración son `Resources/Frankenstein/Exploration/topdown-hunter.png`, `topdown-dancer-v2.png` y `topdown-grafter-v2.png`. Sus filas se recortan por los espacios transparentes reales, evitando cortar sombrero o cabeza al mirar al norte, tanto en idle como en marcha. Los cuerpos base tienen manos vacías; el equipo de combate se dibuja por separado. Es una primera representación modular, no un sistema de animación ósea completo; los sprites cenitales aún no reflejan todos los injertos y cambios de equipo.

Se generaron con image_gen integrado. `Resources/Frankenstein/revision-cast-generation.json` conserva prompts y referencias. Los atlas anteriores de la Bailarina y el Injertador quedan como versiones históricas; los del personaje sustituido se retiraron. Se mantienen `props.png`, `roaming.png` y `panel.png` para escenario, enemigos y UI.

`Frankenstein > Comprobar prototipo completo` ejecuta `PrototypeChecks.RunAll()`. Comprueba equipo independiente y arcos a dos manos, pérdida de acciones, hilos y mesas, torsos no inventariables, debilidades de clase y afinidades, rendición exacta, furia, huida y retirada sin recompensa, fuego amigo, ataques a rendidos, botín limitado, abandono/muerte de aliados, lanzamiento, mapas, IA, lore, puzles y progresión con las tres clases. También comprueba que las cuatro celdas norte de los nuevos atlas no corten la silueta y que los turnos esperen al diálogo.

Los recorridos de campaña pasan turnos enemigos para aislar progresión; otras simulaciones usan IA real. La dificultad final requiere partidas de equilibrio. Core no depende de Unity y no se añaden paquetes. Guardado y audio siguen pendientes.

## Dirección visual: El Imperio de la Carne Prestada
El conjunto activo y sus 32 clips se documentan en `Assets/Frankenstein/Art/Imperio/README.md`. Los prompts y rutas están en `Resources/Frankenstein/Imperio/imperio-generation.json`. Paquete importable: `Docs/Frankenstein/Imperio-Assets-v2.unitypackage`.
