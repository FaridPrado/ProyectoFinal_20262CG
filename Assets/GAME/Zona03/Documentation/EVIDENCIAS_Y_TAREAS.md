# Evidencias de las siete tareas — Farid Stiven Prado Hoyos

Este documento permite verificar en la **rama propia** `farid-zona03` la entrega correspondiente a la Zona 03 de Control. Son las tareas 2–8 del plan original, renumeradas aquí del 1 al 7.

| N.º | Tarea | Resultado implementado | Evidencia en Unity / archivos |
|---|---|---|---|
| 1 | Diseñar la distribución y el recorrido | Sala 26 × 20 unidades con acceso sur, pasillo central, plataforma de mando, cuatro estaciones laterales, racks, terminal y portal norte. | `03_Control.unity` → `Z03_GENERATED_CONTROL_ROOM/01_ARQUITECTURA_Y_CIRCULACION`; `Documentation/Plano_Conceptual_Zona03.png` |
| 2 | Buscar y seleccionar recursos 3D | Se diseñaron seis modelos originales OBJ para evitar dependencias y licencias externas: consola, silla, rack, terminal, cámara y panel de ventilación. | `Models/*.obj`; `02_PUESTOS_DE_OPERACION_Y_MOBILIARIO` |
| 3 | Construir y ambientar el escenario | Jerarquía organizada, suelo modular, techos, muros, señalización, puestos, pantallas, consola de seguridad, iluminación de piso y racks. | `03_Control.unity` → grupos `01`, `02`, `03`, `07` |
| 4 | Materiales, texturas e iluminación | Materiales URP/Lit generados y texturas de telemetría, vigilancia, datos, seguridad, señalización y planos; luces blancas, azules, ámbar y verdes. | `Materials/*.mat`, `Textures/*.png`, grupo `04_ILUMINACION_AMBIENTAL` |
| 5 | Efectos de partículas | Partículas holográficas, trazos de datos, chispas cerca de la terminal y ventilación. Animación de holograma y luces. | `05_EFECTOS_DE_PARTICULAS`, `Scripts/Z03AmbientAnimator.cs` |
| 6 | Audio ambiental y efectos | Clip WAV ambiental en bucle, ventiladores espaciales y clips listos para bip y alerta posterior. | `Audio/*.wav`, `06_AUDIO_AMBIENTAL_Y_EFECTOS` |
| 7 | Prefabs de Canvas | HUD superior con nombre de zona, estado, reloj, panel de objetivo e instrucciones; cuatro prefabs exportados para reutilización. | `Prefabs/Z03_HUD_Completo.prefab`, `Z03_StatusPanel.prefab`, `Z03_ObjectivePanel.prefab`, `Z03_InteractionPrompt.prefab` |

## Qué no está dentro de estas siete tareas

No se han desarrollado **interacciones con terminales**, reglas de misión, guardado de progreso, integración de `EscenaIniciada()` o transición jugable a la zona 4. Son tareas 9+ del plan original; los carteles y el portal al norte tienen por ahora una función **visual**. No se modifica el `GameManager` ni `UIManager` existentes.

## Validaciones antes de completar Google Tasks

1. Abre `03_Control.unity` en Unity 6 con URP.
2. Comprueba que no hay errores de importación ni materiales rosas.
3. Ejecuta **Tools → Zona 03 → Validar entrega y recursos**.
4. Pulsa **Play**, verifica recorrido libre, HUD visible, sonido ambiental y efectos.
5. Guarda la escena y sube a tu GitHub los archivos generados, incluidos `.meta`.
6. Si todo funciona en Unity, marca las tareas correspondientes como completadas y adjunta el enlace al commit y una captura del editor.

## Notas de autoría

Recursos originales creados de manera procedimental para el proyecto académico. No se descargaron paquetes externos y no existe dependencia de los enlaces de Asset Store compartidos en el chat.
