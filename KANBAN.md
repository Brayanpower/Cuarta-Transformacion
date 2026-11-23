# Tablero Kanban — Cuarta Transformacion

Este tablero refleja el avance del proyecto organizado mediante issues individuales en GitHub, desarrollado por **@Brayanpower**.

---

## 1. Columnas del Tablero de Trabajo

```
+-----------------------------------+-----------------------------------+-----------------------------------+
|            HECHO (DONE)           |     EN PROGRESO (IN PROGRESS)     |         POR HACER (TO DO)         |
+-----------------------------------+-----------------------------------+-----------------------------------+
| - [Issue #1] Concepcion, Diseno   | (Sin tareas activas en curso;     | - [Issue #8] Generacion de Build, |
|   Inicial y Documento GDD         |  listo para empaquetado final)    |   Trailer y Lanzamiento           |
| - [Issue #2] Control del Jugador, |                                   |                                   |
|   Combate, Dash y Oleadas         |                                   |                                   |
| - [Issue #3] Nivel 1 Completo:    |                                   |                                   |
|   Jardin Principal de Dolores     |                                   |                                   |
|   Hidalgo, Garbanzos y Jefe 1     |                                   |                                   |
| - [Issue #4] Nivel 2 Completo:    |                                   |                                   |
|   Talleres de Talavera, Audio     |                                   |                                   |
|   Chiptune y Jefe 2               |                                   |                                   |
| - [Issue #5] Nivel 3 Completo:    |                                   |                                   |
|   Callejones de Guanajuato,       |                                   |                                   |
|   Jefe 3 y Celebracion Final      |                                   |                                   |
| - [Issue #6] Progresion, Menus    |                                   |                                   |
|   Formales, Cinematica y Arte     |                                   |                                   |
| - [Issue #7] Balance, Fisicas,    |                                   |                                   |
|   Lava Animada y FOV/Zoom         |                                   |                                   |
| - [Issue #9] Gestion de Proyecto: |                                   |                                   |
|   Control de Riesgos y Kanban     |                                   |                                   |
+-----------------------------------+-----------------------------------+-----------------------------------+
```

---

## 2. Detalle de Estado por Issue

### Columna: Hecho (Done)
* **[Issue #1: Concepcion, Diseno Inicial y Documento GDD](https://github.com/Brayanpower/Cuarta-Transformacion/issues/1)**
  * Documento `GDD.md` formalizado con la historia de Dolores Hidalgo, arquetipos y rubrica de evaluacion.
  * Repositorio Git estructurado con ramas por issue y Pull Requests en GitHub.
* **[Issue #2: Control del Jugador, Mecanicas de Combate y Prototipo](https://github.com/Brayanpower/Cuarta-Transformacion/issues/2)**
  * `PlayerMovement.cs`: fisicas continuas en `FixedUpdate`, salto variable, coyote time, jump buffer, dash con invulnerabilidad (`Left Shift`), ataque de espada y animaciones conectadas en `AnimatorController`.
* **[Issue #3: Nivel 1 Completo — Jardin Principal y Jefe 1: El Vendedor Viral](https://github.com/Brayanpower/Cuarta-Transformacion/issues/3)**
  * `Nivel1.unity`: recorrido completo con esbirros garbanzo (`SimpleEnemy.cs`), resortes Sonic (`SpeedSpring.cs`), coleccionables (`CollectibleItem.cs`), arena bloqueada estilo Sonic y jefe `ViralVendorBoss.cs` con portal colonial (`LevelPortalTrigger.cs`).
* **[Issue #4: Nivel 2 Completo — Talleres de Talavera y Jefe 2: El Municipe Carismatico](https://github.com/Brayanpower/Cuarta-Transformacion/issues/4)**
  * `Nivel2.unity`: plataformas quebradizas de talavera (`BreakableTalaveraPlatform.cs`), jarrones explosivos (`ExplosivePotHazard.cs`), esbirros locales (`Level2TalaveraEnemy.cs`), jefe `CharismaticMayorBoss.cs` y sintetizador chiptune/SFX procedural (`ProceduralSoundManager.cs`).
* **[Issue #5: Nivel 3 Completo — Callejones de Guanajuato y Jefe 3: El Influencer del Callejon](https://github.com/Brayanpower/Cuarta-Transformacion/issues/5)**
  * `Nivel3.unity`: recorrido vertical con drones paparazzi (`Level3DroneEnemy.cs`), fans de selfies (`Level3SelfieFanEnemy.cs`), reflectores (`SpotlightHazard.cs`), jefe final `AlleyInfluencerBoss.cs`, animacion automatica de salida estilo Sonic (`LevelCompletionCelebration.cs`) y titulos de zona (`LevelTitleCard.cs`).
* **[Issue #6: Progresion entre Niveles, Interfaz de Usuario y Arte Final](https://github.com/Brayanpower/Cuarta-Transformacion/issues/6)**
  * Fondos pixel-art nocturnos de Dolores Hidalgo, Talavera y Guanajuato, props coloniales-medievales (`MedievalPropAnimator.cs`), suelos tematicos por nivel, cinematica automatica de 4 actos en Nivel 1 (`Level1IntroCutsceneTrigger.cs`, `StoryCutsceneController.cs`), Menu Principal formal de tablones (`MainMenu.unity`, `MainMenuUI.cs`), ajustes (`SettingsMenuUI.cs`), pausa unificada (`PauseMenu.cs`) y progresion (`LevelProgressionManager.cs`).
* **[Issue #7: Playtesting Externo, Analisis de Retroalimentacion y Balance](https://github.com/Brayanpower/Cuarta-Transformacion/issues/7)**
  * Correccion de colisiones en suelo plano con `CompositeCollider2D` + collider redondeado sin friccion, tope izquierdo del mapa, cooldown tras golpe, bloqueo total de controles en fin de nivel/titulos, opcion de **FOV / Zoom de Camara** (`CameraFollow.cs`) y reemplazo de trampas rojas por lava pixel-art animada con rebote de rescate (`HazardSpikes.cs`).
* **[Issue #9: Gestion de Proyecto: Control de Riesgos y Mitigaciones para Desarrollo Individual](https://github.com/Brayanpower/Cuarta-Transformacion/issues/9)**
  * Cumplimiento de los 3 niveles jugables completos, seguimiento por ramas/PRs de noviembre y actualizacion integral del tablero Kanban.

---

### Columna: En Progreso (In Progress)
* Ninguna tarea activa en curso antes de la publicacion final.

---

### Columna: Por Hacer (To Do)
* **[Issue #8: Generacion de Build, Trailer Promocional y Lanzamiento en Itch.io](https://github.com/Brayanpower/Cuarta-Transformacion/issues/8)**
  * Compilacion final PC/WebGL, capturas, trailer promocional (30–60 s) y pagina oficial en Itch.io.

---

## 3. Visualizacion en GitHub

Puedes filtrar los issues en GitHub directamente por su estado utilizando las etiquetas:
* [Ver issues en Hecho](https://github.com/Brayanpower/Cuarta-Transformacion/issues?q=is%3Aissue+label%3A%22Estado%3A+Hecho%22)
* [Ver issues En Progreso](https://github.com/Brayanpower/Cuarta-Transformacion/issues?q=is%3Aissue+label%3A%22Estado%3A+En+Progreso%22)
* [Ver issues Por Hacer](https://github.com/Brayanpower/Cuarta-Transformacion/issues?q=is%3Aissue+label%3A%22Estado%3A+Por+Hacer%22)
