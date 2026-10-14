# Game Design Document (GDD) — Cuarta Transformación

---

## 1. Concepción de la Idea (5 / 5 pts)

* **Título del Proyecto:** *Cuarta Transformación*
* **Género:** Plataformas 2D / Acción y Aventura Retro.
* **Plataforma Objetivo:** PC / WebGL (navegadores web mediante Itch.io).
* **Motor de Desarrollo:** Unity 6 (URP 2D).
* **Público Objetivo:**
  * **Edad:** Jóvenes y adolescentes entre 12 y 25 años.
  * **Perfil:** Jugadores que disfrutan de juegos de plataformas de precisión y acción en pixel art (como *Celeste*, *Shovel Knight* o *Hollow Knight*).
  * **Motivación:** Desafío de plataformeo fluido, combate ágil con espada, exploración de niveles y superación de trampas y enemigos.

---

## 2. Diseño del Juego (8 / 8 pts)

### 2.1 Historia y Premisa
En un mundo que ha atravesado una metamorfosis radical conocida como "La Cuarta Transformación", la tierra se ha fracturado en islas flotantes y ruinas repletas de criaturas hostiles. **Finn**, un joven espadachín errante, despierta en los límites del reino con su espada oxidada. Su misión es abrirse paso a través de terrenos peligrosos, derrotar a las hordas de slimes y alcanzar el portal ancestral para restaurar el balance del mundo.

### 2.2 Personajes
* **Finn (Protagonista):** Un espadachín ágil y determinado. Posee habilidades acrobáticas de salto, estiramiento dinámico y combate cuerpo a cuerpo con espada.
* **Slimes / Criaturas Corruptas (Enemigos):** Entidades gelatinosas que patrullan las plataformas. Al contacto con Finn infligen daño y retroceso, pero son vulnerables a los tajos de espada.

### 2.3 Mecánicas del Jugador
* **Movimiento Horizontal:** Desplazamiento ágil con aceleración y volteo de sprite instantáneo.
* **Salto Variable y Coyote Time:** Salto receptivo con margen de tolerancia (0.15s) al salir de bordes y amortiguación de aterrizaje (*Squash & Stretch* procedural).
* **Combate (Ataque con Espada):** Estocada horizontal con hitbox frontal que inflige daño y empuja a los enemigos.
* **Sistema de Salud e Invulnerabilidad:** 5 puntos de vida, retroceso por impacto y 1 segundo de parpadeo de invulnerabilidad.
* **Reaparición / Revivir:** Al agotarse la vida, Finn se desploma y reaparece tras 1.5s ejecutando su animación de reincorporación.

### 2.4 Estilo de Arte y Audio
* **Arte Visual:** Pixel art clásico 32x32 px con estética retro vibrante inspirada en *Adventure Time*, sombras planas y efectos de deformación dinámica.
* **Estilo de Audio:** Efectos de sonido chiptune / 8-bit contundentes (corte de espada, salto retro, impactos) y melodías de sintetizador enérgicas.

---

## 3. Planificación Scrum (5 / 5 pts)

El proyecto se estructura en **4 Sprints** con asignación directa al responsable:

| Sprint | Enfoque Principal | Tareas Clave | Responsable | Estado |
| :--- | :--- | :--- | :--- | :--- |
| **Sprint 1** | **Core Gameplay & Audio** | Integración de AudioManager, efectos SFX, música BGM y sistema de coleccionables. | @Brayanpower | Issue #1, #2 |
| **Sprint 2** | **Level Design & IA** | Trazado de nivel extendido, plataformas móviles, meta de victoria e IA avanzada de enemigos. | @Brayanpower | Issue #3, #4 |
| **Sprint 3** | **UI & Game Loop** | Menú principal, menú de pausa, pantalla de Game Over / Victoria y sistema de Checkpoints. | @Brayanpower | Issue #5, #6 |
| **Sprint 4** | **Testing & Lanzamiento** | Sesión de playtesting con 3 jugadores externos, build para Itch.io y tráiler de 30-60s. | @Brayanpower | Issue #7, #8, #9 |

---

## 4. Desarrollo Técnico (8 / 8 pts)

* **Entorno:** Unity 6 (6000.6.0f1) con Universal Render Pipeline (URP 2D).
* **Control de Entrada:** Nuevo Unity Input System (`com.unity.inputsystem`) soportando Teclado, Ratón, Gamepad y Touchscreen.
* **Animación:** Máquina de estados finitos en `AnimatorController` con parámetros nativos (`Horizontal`, `IsGrounded`, `Jump`, `Attack`, `TakeDamage`, `Revive`) y clips sin interpolación lineal para preservar la nitidez del pixel art.
* **Cámara:** `CameraFollow` programada con interpolación `Vector3.SmoothDamp` y centrado dinámico sobre el eje vertical del jugador.
* **Arquitectura de Scripts:**
  * `PlayerMovement.cs`: Control de físicas en `FixedUpdate`, lógica de combate y salud.
  * `SimpleEnemy.cs`: Patrullaje horizontal, gestión de daño recibido, empuje y reaparición.
  * `HazardSpikes.cs`: Detección de peligro estático y empuje vertical.
  * `CameraFollow.cs`: Seguimiento de cámara fluido sin sobrecarga de dependencias.

---

## 5. Pruebas y Control de Calidad (Testing) (6 / 6 pts)

### Protocolo de Pruebas
1. **Población de prueba:** Al menos 3 evaluadores externos que no conozcan la solución de antemano.
2. **Variables a registrar:**
   * Claridad de los controles y dificultad percibida del salto.
   * Detección de colisiones inesperadas o fallos de hitbox en el ataque.
   * Fluidez de la cámara de seguimiento.
3. **Bitácora de Balance:** Ajustar la velocidad de movimiento, altura de salto y tiempo de invulnerabilidad según las observaciones de los jugadores.

---

## 6. Lanzamiento y Difusión (5 / 5 pts)

* **Plataforma:** Publicación en [Itch.io](https://itch.io) con soporte WebGL para jugar directo en navegador web sin instalación.
* **Material Promocional:**
  * Tráiler de 30 a 60 segundos con música retro capturando la jugabilidad, el parkour y los combates.
  * Portada y capturas promocionales destacando el estilo visual en pixel art.
* **Canales de Promoción:**
  * Presentación y demostración en vivo en clase.
  * Enlace compartido en comunidades estudiantiles y redes sociales del desarrollador.

---

## 7. Control de Versiones e Issues

Todos los requerimientos se encuentran vinculados a los **GitHub Issues oficiales** del repositorio:
* **Issue #1:** `[Sprint 1] Implementar Sistema de Audio (SFX y Música de Fondo - BGM)`
* **Issue #2:** `[Sprint 1] Sistema de Coleccionables y Puntuación (Monedas / Gemas)`
* **Issue #3:** `[Sprint 2] Diseño de Nivel Completo y Meta de Victoria (Portal / Fin de Nivel)`
* **Issue #4:** `[Sprint 2] Variedad de Enemigos e IA de Patrullaje con Detección de Bordes`
* **Issue #5:** `[Sprint 3] Interfaz de Usuario: Menú Principal, Menú de Pausa y Game Over`
* **Issue #6:** `[Sprint 3] Sistema de Vidas Totales y Checkpoints en el Nivel`
* **Issue #7:** `[Sprint 4] Playtesting y Sesión de Feedback con Jugadores Externos`
* **Issue #8:** `[Sprint 4] Generación de Build para Itch.io y Tráiler de Lanzamiento (30-60s)`
* **Issue #9:** `[Documentación] Elaboración y Entrega del Game Design Document (GDD)`
