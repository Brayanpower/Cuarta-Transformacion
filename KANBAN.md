# Tablero Kanban — Cuarta Transformacion (8 Semanas)

Este tablero refleja el avance del proyecto organizado bajo la metodologia Scrum para 4 sprints (8 semanas), desarrollado por **@Brayanpower**.

---

## 1. Columnas del Tablero de Trabajo

```
+--------------------------------+--------------------------------+--------------------------------+
|          HECHO (DONE)          |    EN PROGRESO (IN PROGRESS)   |      POR HACER (TO DO)         |
+--------------------------------+--------------------------------+--------------------------------+
| - [Issue #1] Concepcion,       | - [Issue #2] Controles del     | - [Issue #3] Nivel 1 Completo  |
|   Diseno y Documento GDD       |   Jugador y Spawn de Enemigos  |   y Jefe: El Vendedor Viral    |
| - Movimiento, Salto, Físicas   | - [Issue #9] Gestion y Control | - [Issue #4] Nivel 2 Greybox   |
|   y Coyote Time en C#          |   de Riesgos en Solitario      |   y Jefe: Municipe Carismatico |
| - Animaciones en Animator      |                                | - [Issue #5] Nivel 3 y Jefe:   |
|   (Idle, Run, Jump, Golpe,     |                                |   Influencer del Callejon      |
|   Dano, Revivir)               |                                | - [Issue #6] Progresion, UI    |
| - Combate e Impacto de Espada  |                                |   Canvas y Arte Final          |
| - Sistema de Salud y Knockback |                                | - [Issue #7] Playtesting con   |
| - Camara Suave (CameraFollow)  |                                |   3 Evaluadores Externos       |
| - Nivel Base con Plataformas,  |                                | - [Issue #8] Build Itch.io,    |
|   Picos y Enemigo de Prueba    |                                |   Trailer y Cierre de GDD      |
+--------------------------------+--------------------------------+--------------------------------+
```

---

## 2. Detalle de Estado por Tarea

### Columna: Hecho (Done)
* **[Issue #1: Concepcion, Diseno Inicial y Documento GDD](https://github.com/Brayanpower/Cuarta-Transformacion/issues/1)**
  * Documento `GDD.md` formalizado con la historia de Dolores Hidalgo, arquetipos y rubrica de 40 puntos.
  * Repositorio Git estructurado y sincronizado en GitHub.
* **Componentes del Jugador Implementados (C#):**
  * `PlayerMovement.cs`: Fisicas en `FixedUpdate`, salto variable, coyote time y jump buffer.
  * Combate activo: Ataque con espada y deteccion de hitbox frontal (`Physics2D.OverlapCircleAll`).
  * Sistema de salud: 5 HP, parpadeo de invulnerabilidad temporal y reincorporacion automatica al morir.
* **Animaciones 2D:**
  * Maquina de estados configurada en `AnimatorController.controller` con 6 estados y transiciones precisas.
  * Clips generados a partir de `FinnSprite.png` (Idle, Run, Jump, Golpe, Dano, revivir).
* **Entorno y Pruebas en Escena:**
  * `CameraFollow.cs`: Camara de seguimiento suave con `SmoothDamp`.
  * `SampleScene.unity`: Escenario ampliado a 60 metros con 5 plataformas elevadas para parkour.
  * `HazardSpikes.cs`: Trampas de picos funcionales con impulso vertical y dano.
  * `SimpleEnemy.cs`: Enemigo interactivo con patrullaje, recepcion de dano, retroceso y reaparicion.

---

### Columna: En Progreso (In Progress)
* **[Issue #2: Controles del Jugador y Prototipo de Oleadas en Nivel 1](https://github.com/Brayanpower/Cuarta-Transformacion/issues/2)**
  * Pendiente: Generador automatico de oleadas de vecinos hostiles en puntos especificos del mapa.
* **[Issue #9: Gestion de Proyecto y Control de Riesgos](https://github.com/Brayanpower/Cuarta-Transformacion/issues/9)**
  * Evaluacion continua de tiempos semanales para asegurar la entrega de los niveles comprometidos.

---

### Columna: Por Hacer (To Do)
* **Sprint 2 (Semanas 3 y 4):**
  * **[Issue #3: Nivel 1 Completo (Jardin Principal) y Jefe 1: El Vendedor Viral](https://github.com/Brayanpower/Cuarta-Transformacion/issues/3)**
  * **[Issue #4: Nivel 2 en Greybox (Zona de Talavera) y Jefe 2: El Municipe Carismatico](https://github.com/Brayanpower/Cuarta-Transformacion/issues/4)**
* **Sprint 3 (Semanas 5 y 6):**
  * **[Issue #5: Nivel 3 (Callejones de Guanajuato) y Jefe 3: El Influencer del Callejon](https://github.com/Brayanpower/Cuarta-Transformacion/issues/5)**
  * **[Issue #6: Progresion entre Niveles, Interfaz de Usuario y Arte Final](https://github.com/Brayanpower/Cuarta-Transformacion/issues/6)**
* **Sprint 4 (Semanas 7 y 8):**
  * **[Issue #7: Playtesting Externo, Analisis de Retroalimentacion y Balance](https://github.com/Brayanpower/Cuarta-Transformacion/issues/7)**
  * **[Issue #8: Generacion de Build, Trailer Promocional y Lanzamiento en Itch.io](https://github.com/Brayanpower/Cuarta-Transformacion/issues/8)**

---

## 3. Visualizacion en GitHub

Puedes filtrar los issues en GitHub directamente por su estado utilizando las etiquetas:
* [Ver tareas en Hecho](https://github.com/Brayanpower/Cuarta-Transformacion/issues?q=is%3Aissue+label%3A%22Estado%3A+Hecho%22)
* [Ver tareas En Progreso](https://github.com/Brayanpower/Cuarta-Transformacion/issues?q=is%3Aissue+label%3A%22Estado%3A+En+Progreso%22)
* [Ver tareas Por Hacer](https://github.com/Brayanpower/Cuarta-Transformacion/issues?q=is%3Aissue+label%3A%22Estado%3A+Por+Hacer%22)

Para abrir la vista interactiva de tablero en GitHub:
1. Dirigete a la pestaña **Projects** en tu repositorio: https://github.com/Brayanpower/Cuarta-Transformacion/projects
2. Haz clic en **New project**.
3. Selecciona la plantilla **Board**.
4. Agrega los issues del repositorio arrastrandolos a las columnas correspondientes.
