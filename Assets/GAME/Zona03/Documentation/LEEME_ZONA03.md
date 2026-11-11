# ZONA 03 — SALA DE CONTROL / PAQUETE DE AMBIENTACIÓN

**Responsable:** Farid Stiven Prado Hoyos  
**Proyecto:** ProyectoFinal_20262CG · Unity 6 (6000.3.14f1) · URP

Este módulo desarrolla las tareas 1–7 del plan actualizado: diseño espacial, modelos 3D, ambientación, materiales y luces, partículas, audio y Canvas.

## Instalación

1. Haz un respaldo o crea una rama nueva de tu copia personal de GitHub.
2. Copia las carpetas `Assets/GAME/Zona03` y los archivos que incorpora este paquete. **No borres** `Assets/GAME/Scenes` ni ninguna otra carpeta ya existente.
3. Abre el proyecto con Unity Hub usando Unity 6000.3.14f1 y deja terminar de importar.
4. En el menú superior ejecuta **Tools → Zona 03 → Construir sala de control (escena + prefabs)**.
5. Se abrirá y guardará automáticamente `Assets/GAME/Scenes/03_Control.unity`; el generador no cambia otras escenas ni GameManager.
6. Pulsa Play. Para explorar utiliza clic derecho + ratón, WASD, Q/E y Shift. La interfaz se renderiza desde un Canvas persistente de la escena.
7. Guarda los cambios en una rama tuya, inspecciona `git status` y `git diff --stat`, y publica los nuevos archivos `.meta`, `.prefab`, `.mat`, `.unity`, `.obj`, `.png`, `.wav`.

**Importante:** Unity requiere ejecutar el menú del paso 4 **una vez** para crear y serializar de forma compatible la escena, los materiales y los prefabs. La generación es repetible y recrea únicamente el grupo `Z03_GENERATED_CONTROL_ROOM` y sus recursos de zona; un segundo uso reemplaza los elementos generados. No lo ejecutes sobre cambios manuales sin guardarlos antes.

## Carpeta y contenido

- `Models/`: 6 modelos OBJ originales de geometría low-poly: consola, terminal, rack, silla, cámara y ventilación.
- `Textures/`: 8 texturas originales (telemetría, vigilancia, mapa, alarmas, datos, señalización, emblema y rejillas).
- `Audio/`: 4 clips originales en WAV (ambiente, terminal, alerta y ventilador).
- `Editor/Z03SceneBuilder.cs`: generador editor 3D/URP que crea jerarquía, luces, partículas, 4 prefabs de Canvas y guarda la escena.
- `Scripts/Z03CameraExplorer.cs`: recorrido libre de prueba compatible con Input System.
- `Scripts/Z03AmbientAnimator.cs`: animación visual de luz y holoanillos.
- `Scripts/Z03HudPresenter.cs`: textos dinámicos del HUD. No implementa gameplay.
- `Materials/`, `Prefabs/`: Unity crea aquí los archivos `.mat` y `.prefab` al ejecutar el generador.

## Diseño y recorrido

- **Entrada**: extremo sur, ruta central de acceso con señales de suelo.
- **Puesto principal**: centro de sala, núcleo de telemetría holográfica.
- **Puestos laterales**: 4 estaciones con pantallas y sillas.
- **Racks y ventilación**: laterales, con audio 3D.
- **Panel mural**: área norte con pantallas, mapa técnico y puerta de seguridad.
- **Salida**: portal norte marcado hacia `04_Servidores`. Por ahora es **visual** (no lógica de interacción ni cambio de escena).

## Alcance y límites

Este paquete cubre siete tareas de presentación y ambientación. No modifica `GameManager`, `Zona03Controller` o `UIManager`, ni implementa todavía interacción, misiones o transición real entre escenas (esas son tareas posteriores). El movimiento libre está pensado para comprobar la escena sin depender de un controlador de jugador todavía incompleto.

La importación y ejecución en Unity **deben comprobarse en tu equipo**: el ZIP fue construido y validado a nivel de archivos y código, pero no ejecutado dentro del Editor Unity en este entorno.

## Créditos y licencias

Todos los modelos, texturas y sonidos incluidos en `Zona03` son creaciones procedimentales originales para este paquete. No se incorporan assets de terceros. Puedes utilizarlos y modificarlos dentro del proyecto académico.
