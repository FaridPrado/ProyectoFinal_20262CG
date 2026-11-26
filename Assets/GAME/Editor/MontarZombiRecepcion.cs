// Herramienta del editor (no va en el juego): menu "Proyecto Final".
// Monta en 01_Recepcion lo que falta para la rubrica:
// - NavMeshSurface horneado (para que el zombi pueda caminar)
// - El zombi (reutiliza el prefab Z03_Agente_Seguridad de Farid, pero con
//   nuestro AgentController en vez de su script), su luz y sus sonidos
// - Puntos de patrulla por el vestibulo, el archivo y la bodega
// - La puerta de salida que se abre al completar la zona
// Se puede volver a usar: borra lo que creo antes.
// Despues de usarla, los puntos, el zombi y la puerta se pueden mover a mano.

using ControlRoomZ03;
using Unity.AI.Navigation;
using Unity.AI.Navigation.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class MontarZombiRecepcion
{
    private const string RUTA_ZOMBI = "Assets/GAME/Zona03/Prefabs/Z03_Agente_Seguridad.prefab";
    private const string RUTA_SONIDOS_KIT = "Assets/SciFi Warehouse Kit/Demo/Audio/SFX/";
    private const string RUTA_SONIDOS_Z03 = "Assets/GAME/Zona03/Audio/";
    private const string RUTA_MATERIAL_PUERTA = "Assets/GAME/Materials/PuertaSalida.mat";
    private const string RUTA_HUD = "Assets/GAME/Zona03/Prefabs/Z03_HUD_Completo.prefab";

    // Recorrido del zombi (la sala va de 0 a 24 en X y de 0 a 30 en Z):
    // vestibulo, archivo (lado oeste), cerca de la salida y bodega (lado este)
    private static readonly Vector3[] PUNTOS =
    {
        new Vector3(19f, 0f, 4f),
        new Vector3(5f, 0f, 5f),
        new Vector3(8f, 0f, 20f),
        new Vector3(4f, 0f, 26f),
        new Vector3(17f, 0f, 21f),
        new Vector3(21f, 0f, 27f)
    };

    [MenuItem("Proyecto Final/Montar zombi, NavMesh y puerta (Recepcion)")]
    private static void Montar()
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "01_Recepcion")
        {
            EditorUtility.DisplayDialog("Montar zombi", "Abre primero la escena 01_Recepcion.", "Ok");
            return;
        }

        Transform puerta = CrearPuerta();
        Transform[] puntos = CrearPuntosPatrulla();
        AgentController zombi = CrearZombi(puntos);
        ConectarControlador(puerta, zombi);
        NavMeshSurface superficie = CrearNavMesh();

        EditorSceneManager.MarkSceneDirty(escena);

        // Hornea el NavMesh y lo guarda como asset junto a la escena
        NavMeshAssetManager.instance.StartBakingSurfaces(new Object[] { superficie });

        Debug.Log("Zombi, puntos de patrulla, puerta y NavMesh montados. Cuando termine el Bake, guarda la escena (Ctrl/Cmd + S).");
    }

    // ---------- NavMesh ----------

    private static NavMeshSurface CrearNavMesh()
    {
        GameObject objeto = GameObject.Find("Navegacion");
        if (objeto == null)
        {
            objeto = new GameObject("Navegacion");
        }

        NavMeshSurface superficie = objeto.GetComponent<NavMeshSurface>();
        if (superficie == null)
        {
            superficie = objeto.AddComponent<NavMeshSurface>();
        }

        // Usa todos los objetos de la escena (piso, paredes, columnas, cajas...)
        superficie.collectObjects = CollectObjects.All;
        superficie.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        return superficie;
    }

    // ---------- Puntos de patrulla ----------

    private static Transform[] CrearPuntosPatrulla()
    {
        Borrar("PuntosPatrulla");
        var padre = new GameObject("PuntosPatrulla").transform;

        Transform[] puntos = new Transform[PUNTOS.Length];
        for (int i = 0; i < PUNTOS.Length; i++)
        {
            var punto = new GameObject("Punto " + (i + 1)).transform;
            punto.SetParent(padre);
            punto.position = PUNTOS[i];
            puntos[i] = punto;
        }

        return puntos;
    }

    // ---------- Zombi ----------

    private static AgentController CrearZombi(Transform[] puntos)
    {
        Borrar("Zombi");

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_ZOMBI);
        if (prefab == null)
        {
            Debug.LogError("No se encontro el prefab del zombi en " + RUTA_ZOMBI);
            return null;
        }

        var zombi = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        // Se desempaca para poder quitarle el script de Farid y poner el nuestro
        PrefabUtility.UnpackPrefabInstance(zombi, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        zombi.name = "Zombi";
        zombi.transform.position = puntos[puntos.Length - 1].position;

        Z03SecurityAgent viejo = zombi.GetComponent<Z03SecurityAgent>();
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo);
        }

        var navegacion = zombi.GetComponent<NavMeshAgent>();
        if (navegacion == null)
        {
            navegacion = zombi.AddComponent<NavMeshAgent>();
        }
        navegacion.radius = 0.35f;
        navegacion.height = 1.8f;
        navegacion.speed = 1.5f;
        navegacion.angularSpeed = 240f;
        navegacion.acceleration = 10f;
        navegacion.stoppingDistance = 0.6f;

        // Luz encima de la cabeza que cambia de color con el estado
        var luz = new GameObject("LuzEstado").AddComponent<Light>();
        luz.transform.SetParent(zombi.transform, false);
        luz.transform.localPosition = new Vector3(0f, 2.3f, 0f);
        luz.type = LightType.Point;
        luz.color = Color.cyan;
        luz.range = 4f;
        luz.intensity = 3f;
        luz.shadows = LightShadows.None;

        var agente = zombi.AddComponent<AgentController>();
        agente.puntosPatrulla = puntos;
        agente.luzEstado = luz;
        agente.fuenteAudio = zombi.GetComponent<AudioSource>();
        agente.animacion = zombi.GetComponent<Z03CharacterAnimation>();
        agente.sonidoAlerta = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_SONIDOS_Z03 + "Z03_Alerta.wav");
        agente.sonidoPersecucion = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_SONIDOS_KIT + "wompwomp.wav");
        agente.sonidoAtaque = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_SONIDOS_Z03 + "Z03_Terminal_Bip.wav");
        agente.sonidoRetorno = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_SONIDOS_KIT + "footstep.wav");

        Undo.RegisterCreatedObjectUndo(zombi, "Montar zombi");
        return agente;
    }

    // ---------- Puerta de salida ----------

    private static Transform CrearPuerta()
    {
        Borrar("PuertaSalida");

        // Se pone justo detras del boton de salida, tapando el hueco de la puerta
        Vector3 posicion = new Vector3(3f, 2f, 30.3f);
        GameObject boton = GameObject.Find("BotonSalida");
        if (boton != null)
        {
            posicion = new Vector3(boton.transform.position.x, 2f, boton.transform.position.z + 0.45f);
        }

        GameObject puerta = GameObject.CreatePrimitive(PrimitiveType.Cube);
        puerta.name = "PuertaSalida";
        puerta.transform.position = posicion;
        puerta.transform.localScale = new Vector3(3.5f, 4f, 0.2f);

        var material = AssetDatabase.LoadAssetAtPath<Material>(RUTA_MATERIAL_PUERTA);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", new Color(0.25f, 0.27f, 0.3f));
            material.SetFloat("_Metallic", 0.8f);
            material.SetFloat("_Smoothness", 0.5f);
            AssetDatabase.CreateAsset(material, RUTA_MATERIAL_PUERTA);
        }
        puerta.GetComponent<Renderer>().sharedMaterial = material;

        Undo.RegisterCreatedObjectUndo(puerta, "Montar puerta");
        return puerta.transform;
    }

    // ---------- Zona01Controller ----------

    private static void ConectarControlador(Transform puerta, AgentController zombi)
    {
        var controlador = Object.FindFirstObjectByType<Zona01Controller>();
        if (controlador == null)
        {
            Debug.LogWarning("No hay Zona01Controller en la escena.");
            return;
        }

        controlador.puertaSalida = puerta;
        controlador.zombi = zombi;
        controlador.sonidoPuerta = AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_SONIDOS_KIT + "pressure_release_02.wav");
        EditorUtility.SetDirty(controlador);
    }

    // ---------- HUD compartido ----------
    // La rubrica pide un unico Canvas prefab en todas las escenas.
    // Quita el CanvasJuego viejo de Recepcion y pone el HUD que usan las demas
    // escenas (Z03_HUD_Completo): vida, mensajes, pausa, inventario y derrota.
    [MenuItem("Proyecto Final/Usar HUD compartido en Recepcion")]
    private static void UsarHudCompartido()
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "01_Recepcion")
        {
            EditorUtility.DisplayDialog("HUD compartido", "Abre primero la escena 01_Recepcion.", "Ok");
            return;
        }

        bool yaTieneHud = false;
        foreach (UIManager ui in Object.FindObjectsByType<UIManager>(FindObjectsSortMode.None))
        {
            if (ui.GetComponent<Z03HudPresenter>() != null)
            {
                yaTieneHud = true;
            }
            else
            {
                // Es el CanvasJuego viejo: se borra
                Undo.DestroyObjectImmediate(ui.gameObject);
            }
        }

        if (!yaTieneHud)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_HUD);
            if (prefab == null)
            {
                Debug.LogError("No se encontro el HUD en " + RUTA_HUD);
                return;
            }

            var hud = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(hud, "HUD compartido");
        }

        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("Recepcion usa ahora el HUD compartido. Guarda la escena (Ctrl/Cmd + S).");
    }

    private static void Borrar(string nombre)
    {
        GameObject viejo = GameObject.Find(nombre);
        if (viejo != null)
        {
            Undo.DestroyObjectImmediate(viejo);
        }
    }
}
