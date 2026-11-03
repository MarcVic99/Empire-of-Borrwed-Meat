# El Imperio de la Carne Prestada — conjunto visual

Integrado en el proyecto `Empire-of-Borrwed-Meat`. Arte generado con la herramienta integrada `image_gen` siguiendo el prompt maestro y la imagen del usuario. La paleta usa carbón, borgoña, marfil envejecido, oro antiguo y malva grisáceo. Los PNG conservan su transparencia; los ajustes de recorte se guardan como metadatos, sin modificar las imágenes.

| Grupo | Entrega | Uso actual |
|---|---|---|
| Cazadora, Bailarina, Injertador | 3 atlas con frente, espalda y presentación | Combate y fichas |
| Artwork de selección | 3 ilustraciones individuales con poses de personalidad | Zoom suave del 16 %, foco en cara/torso y recorte dentro de la tarjeta |
| Exploración de personajes | 48 fotogramas de base y capas de cabeza/cuerpo | Caminar; idle de tres poses de cabeza con pies fijos |
| Enemigos | 6 frentes y 6 espaldas | Recolector, recolectora, licántropo, ghoul, arácnida y caballero hueco; también aliados reclutados |
| Monstruos en exploración | 16 fotogramas, cuatro tipos con cuatro pasos | Patrullas y persecuciones |
| Mazmorra | 8 objetos y 16 piezas de suelo/muro/decoración | Cofres, mesa, libro, candelabro, columna, altar, escaleras; suelo y muros |
| Equipo y anatomía | 16 iconos de objetos y 8 componentes de injerto | Inventario, equipamiento, ofertas e injertos |
| Interfaz | Panel reutilizable por nueve secciones | Menús, tarjetas, diálogos; hover/click animados por el código existente |
| Combate | Fondo de capilla simplificada: arco, pilares, estandarte y candelero | Contraste bajo y suelo despejado para destacar combatientes |

Hay **18 recursos gráficos activos** en `Assets/Frankenstein/Resources/Frankenstein/Imperio`. Las versiones anteriores, incluido el escenario detallado, se conservan fuera de Resources, en `Assets/Frankenstein/ArtSource/Imperio/Iterations`.

## Animaciones reutilizables

`Assets/Frankenstein/Art/Imperio/Animations` contiene **32 clips `.anim` en bucle**: 24 de personajes (idle y caminar en sur, oeste, este y norte) y 8 de monstruos. Hay **88 sprites recortados**: 64 de base y 24 capas adicionales de cuerpo/cabeza. Importación con pivote inferior central, filtro Point, sin mipmaps y sin compresión.

Para usar personajes en otra escena, arrastra un prefab de `Art/Imperio/Prefabs`. Incluyen un SpriteRenderer para el cuerpo, un hijo `Head` y un Animator. Sus controladores tienen `Moving` (bool) y `Direction` (int: 0 sur, 1 oeste, 2 este, 3 norte). Los clips de caminar muestran el sprite entero y desactivan la cabeza separada; el idle mantiene cuerpo/pies inmóviles y mueve sólo `Head` con pasos discretos. El prototipo presenta las mismas capas directamente en su interfaz de exploración.

Las filas de los personajes son sur, oeste, este y norte. El caminar conserva sus cuatro fotogramas a 9 fps. El idle ya no alterna poses corporales: usa la primera imagen como base fija y tres posiciones de cabeza (centro, izquierda, derecha), con cambios a 1,8 fps y sin balanceo vertical. Las filas de monstruos son recolector, licántropo, ghoul y arácnida: **no son cuatro direcciones**; su animación de exploración es frontal oblicua y admite espejo horizontal.

## Integración y límites

Los miembros ausentes siguen retirando las regiones correspondientes del sprite. Las regiones de la arácnida se adaptan a su silueta ancha. Los injertos usan componentes separados; las armas continúan siendo equipamiento independiente. En combate, las armas usan sprites del nuevo atlas superpuestos en las manos; su alineación aún admite pulido según cada anatomía. Los sprites de exploración todavía no reflejan cada cambio de arma, armadura o injerto.

Es un conjunto inicial integrado y revisado, con animación por fotogramas generados; todavía admite pulido manual de continuidad entre pasos y variantes adicionales. No incluye nuevas animaciones de ataque, daño o muerte. Las piezas de arco y decoración del tileset se entregan para ampliar escenarios; el mapa actual usa principalmente su suelo y sus muros.

`imperio-generation.json` conserva todos los prompts, rutas finales y procedencia. `layouts.json` conserva los límites de los fotogramas. `Docs/Frankenstein/imperio-alpha-checks.txt` recoge la revisión de transparencia y separación.

## Comprobación

En Unity: **Frankenstein → Comprobar arte imperial**. Comprueba recursos, importación, los 48 bordes superiores, los 32 clips, la geometría de la arácnida, el foco del zoom y los doce idles direccionales: mismo cuerpo, misma posición de pies y exactamente tres posiciones de cabeza. La comprobación general conserva los controles y reglas anteriores.

Para reconstruir los recortes y clips, ejecuta por orden los snippets de `Docs/Frankenstein/ArtTools`: `build-imperio-layouts.cs.txt`, `import-imperio-sprites.cs.txt` y, por último, `build-head-idles.cs.txt`. El último paso añade las capas de cabeza/cuerpo y actualiza los clips y prefabs.
