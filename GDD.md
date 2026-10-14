# Game Design Document (GDD) — Cuarta Transformacion

---

## 1. Concepcion de la Idea

### 1.1 Ficha Tecnica
* **Titulo del Proyecto:** Cuarta Transformacion
* **Genero:** Accion 2D por niveles (Side-scroller)
* **Motor de Desarrollo:** Unity (C#)
* **Plataforma:** PC / WebGL (navegadores mediante Itch.io)
* **Duracion del Proyecto:** 8 semanas (4 sprints de 2 semanas)

### 1.2 Publico Objetivo
* **Rango de Edad:** 12 a 25 anos.
* **Perfil del Jugador:** Jugadores interesados en juegos de accion y plataformas clasicos en pixel art, con gusto por la cultura regional mexicana, el humor satirico y los retos de combate de ritmo agil.
* **Motivacion:** Superar oleadas de enemigos, dominar el movimiento y vencer a jefes caricaturescos para restablecer el orden en su comunidad antes de las celebraciones patronales.

---

## 2. Diseno del Juego

### 2.1 Historia y Argumento
Dolores Hidalgo, cuna de la Independencia de Mexico, se prepara para sus fiestas patronales cuando una situacion inusual desestabiliza la region: tres figuras exageradas y ridiculas —un vendedor ambulante convertido en meme viral, un politico local caracterizado por promesas incumplidas y un influencer obsesionado con las camaras— se apoderan de distintos sectores del municipio.

Su influencia contagia a los habitantes locales, volviendolos hostiles y bloqueando el transito publico. En medio del desorden, el jugador asume el papel de un vecino arraigado al pueblo, conocedor de cada callejon y tradicion de Dolores Hidalgo, quien decide intervenir personalmente para restablecer la paz antes de que las fiestas patronales queden arruinadas.

### 2.2 Objetivos del Juego
* **Objetivo Narrativo:** Recuperar la tranquilidad en Dolores Hidalgo y Guanajuato derrotando a las tres figuras responsables del alboroto.
* **Objetivo de Jugabilidad:** Recorrer tres niveles tematicos limpiando oleadas de NPCs hostiles y derrotar al jefe de cada zona para desbloquear la siguiente area.
* **Condicion de Victoria:** Derrotar a los tres jefes principales: El Vendedor Viral, El Municipe Carismatico y El Influencer del Callejon.

### 2.3 Estructura de Zonas y Jefes

| Nivel | Zona (Dolores Hidalgo / Guanajuato) | Jefe (Arquetipo Ficticio) | Descripcion del Jefe |
| :---: | :--- | :--- | :--- |
| **Nivel 1** | Jardin Principal / Centro Historico | **El Vendedor Viral** | Comerciante ambulante convertido en fenomeno de internet debido a una frase pegajosa. Ataques basados en lanzamiento de mercancia y ráfagas vocales. |
| **Nivel 2** | Zona de talleres de talavera / Fabrica La Aurora | **El Municipe Carismatico** | Politico local de promesas vacias con microfono en mano. Ataques basados en ondas sonoras amplificadas y convocatoria de simpatizantes. |
| **Nivel 3** | Callejones y cerros de Guanajuato capital | **El Influencer del Callejon** | Combinacion de personaje folklorico urbano y streamer en vivo. Ataques con destellos de flash cegadores y emboscadas en desnivel. |

### 2.4 Mecanicas del Jugador
* **Desplazamiento Horizontal:** Movimiento lateral fluido con respuesta inmediata de control.
* **Sistema de Salto:** Salto con altura variable (dependiente del tiempo de pulsacion), coyote time y almacenamiento previo de pulsacion (jump buffer).
* **Combate:** Golpe cuerpo a cuerpo con hitbox frontal para infligir dano y aplicar empuje (knockback) a los agresores.
* **Salud e Impacto:** Sistema de puntos de vida, periodo de invulnerabilidad temporal con respuesta visual de parpadeo y retroceso fisico.
* **Ciclo de Nivel:** Derrota de oleadas de vecinos contagiados hasta habilitar la arena de combate del jefe de zona.

### 2.5 Estilo Artistico y Sonoro
* **Arte Visual:** Estilo pixel art 2D optimizado para desarrollo en solitario, apoyado en la adaptacion y personalizacion de asset packs con identidad arquitectonica mexicana (kioscos, farolas, talleres de ceramica y callejones coloniales).
* **Audio:** Efectos de sonido claros y legibles para cada accion (impactos, saltos, golpes) y pistas de musica tradicional adaptadas a instrumentacion electronica/chiptune.

---

## 3. Planificacion de Sprints (Scrum - 8 Semanas)

El proyecto se planifica en cuatro sprints quincenales bajo la responsabilidad directa de un unico desarrollador:

| Sprint | Periodo | Enfoque Principal | Entregables Clave | Responsable |
| :--- | :--- | :--- | :--- | :--- |
| **Sprint 1** | Semanas 1 y 2 | Concepcion, Diseno, GDD y Prototipo del Nivel 1 | Documento GDD completo, repositorio Git inicializado, control del jugador funcional y spawn base de NPCs. | @Brayanpower |
| **Sprint 2** | Semanas 3 y 4 | Nivel 1 Completo y Nivel 2 en Greybox | Nivel 1 terminado con arte y Jefe 1 operativo; Nivel 2 estructurado en greybox con Jefe 2 y audio base. | @Brayanpower |
| **Sprint 3** | Semanas 5 y 6 | Nivel 3, Progresion, Arte Final y UI | Nivel 3 jugable con Jefe 3; flujo completo de transicion entre niveles, menus de UI y arte final aplicado. | @Brayanpower |
| **Sprint 4** | Semanas 7 y 8 | Playtesting, Balance, Trailer y Publicacion | Sesiones de prueba documentadas con tres evaluadores, ajustes de dificultad, juego en Itch.io y trailer de 30-60s. | @Brayanpower |

---

## 4. Desarrollo Tecnico

* **Arquitectura de Software en Unity:**
  * `PlayerMovement.cs`: Gestion de fisicas en FixedUpdate, ejecucion de ataques y control de vida del protagonista.
  * `SimpleEnemy.cs`: Comportamiento de patrullaje, recepcion de dano, retroceso y ataque por contacto de los vecinos hostiles.
  * `HazardSpikes.cs`: Obstaculos ambientales estaticos con aplicacion de dano y propulsion.
  * `CameraFollow.cs`: Desplazamiento de camara amortiguado mediante SmoothDamp para mantener encuadrada la accion en todo momento.
* **Control de Entrada:** Nuevo sistema de entrada de Unity (Input System) configurado para teclado, raton y gamepad.
* **Animacion:** Controladores Animator con transiciones sin tiempo de salida para garantizar precision en las respuestas de combate.

---

## 5. Pruebas y Aseguramiento de Calidad (Testing)

* **Protocolo de Evaluacion Externa:** Realizacion de pruebas con al menos tres personas ajenas a la programacion del juego durante el transcurso del desarrollo.
* **Registro de Errores y Balance:**
  * Identificacion de puntos de perdida de ritmo o atasco en el salto entre plataformas.
  * Registro de fallos en colisiones o deteccion de golpes.
  * Ajuste de valores numericos de velocidad, resistencia de oleadas y patrones de ataque de los jefes en funcion de la dificultad observada.

---

## 6. Publicacion y Promocion

* **Plataforma de Lanzamiento:** Publicacion formal en Itch.io mediante compilacion optimizada para WebGL (ejecutable en navegador) y version descargable para PC.
* **Material Promocional:**
  * Trailer de jugabilidad con duracion de 30 a 60 segundos, destacando el combate, el plataformeo y los enfrentamientos contra los tres jefes.
  * Capturas de pantalla descriptivas y redaccion de instrucciones claras en la pagina de publicacion.
* **Canales de Difusion:** Exposicion academica presencial y difusion mediante comunidades de desarrollo independiente.

---

## 7. Gestion de Riesgos y Mitigaciones (Desarrollo Individual)

1. **Riesgo de Alcance:** Si el ritmo de avance al finalizar la quinta semana se ve comprometido, se priorizara entregar dos niveles completamente pulidos y balanceados en lugar de tres niveles incompletos, manteniendo el rigor de evaluacion.
2. **Riesgo de Arte:** Se emplearan asset packs profesionales adaptados tematicamente para evitar cuellos de botella en la ilustracion manual y enfocar las horas disponibles en programacion y jugabilidad.
3. **Riesgo de Testing:** Se recabara retroalimentacion informal de manera continua desde la culminacion de cada nivel en lugar de reservar las pruebas unicamente para la semana final.
4. **Seguimiento Semanal:** Evaluacion interna de avance al termino de cada semana para identificar desajustes en el cronograma con suficiente tiempo de correccion.

---

## 8. Trazabilidad de Issues en GitHub

El seguimiento operativo de este documento se encuentra vinculado a los issues del repositorio:
* **Issue #1:** Sprint 1: Concepcion, Diseno Inicial y Documento GDD
* **Issue #2:** Sprint 1: Control del Jugador y Prototipo de Oleadas en Nivel 1
* **Issue #3:** Sprint 2: Nivel 1 Completo (Jardin Principal) y Jefe 1: El Vendedor Viral
* **Issue #4:** Sprint 2: Nivel 2 en Greybox (Zona de Talavera) y Jefe 2: El Municipe Carismatico
* **Issue #5:** Sprint 3: Nivel 3 (Callejones de Guanajuato) y Jefe 3: El Influencer del Callejon
* **Issue #6:** Sprint 3: Progresion entre Niveles, Interfaz de Usuario y Arte Final
* **Issue #7:** Sprint 4: Playtesting Externo, Analisis de Retroalimentacion y Balance
* **Issue #8:** Sprint 4: Generacion de Build, Trailer Promocional y Lanzamiento en Itch.io
* **Issue #9:** Gestion de Proyecto: Control de Riesgos y Mitigaciones para Desarrollo Individual
