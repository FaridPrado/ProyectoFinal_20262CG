// Herramienta del editor (no va en el juego): menu "Proyecto Final".
// Monta la escena 01_Recepcion de una vez: decoracion con el kit Sci-Fi,
// los 3 coleccionables, las chispas del fusible, el Canvas del juego
// (prefab CanvasJuego), los sonidos y deja conectados el Zona01Controller
// y el boton de salida. Se puede volver a usar: borra lo que creo antes.
// Despues de usarla, cada objeto se puede mover a mano en la escena.
// Tiene un segundo menu que arma la sala (piso, paredes, techo y luces)
// del tamano de ANCHO x LARGO y reacomoda lo que ya estaba puesto.

using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MontarRecepcion
{
    private const string RUTA_KIT = "Assets/SciFi Warehouse Kit";
    private const string RUTA_SONIDOS = "Assets/SciFi Warehouse Kit/Demo/Audio/SFX/";
    private const string RUTA_CANVAS = "Assets/GAME/Prefabs/CanvasJuego.prefab";
    private const string RUTA_MATERIALES = "Assets/GAME/Materials";

    // Medidas de la sala: de 0 a ANCHO en X y de 0 a LARGO en Z.
    // Las piezas del kit miden 6 x 6, asi que deben ser multiplos de 6.
    private const float BALDOSA = 6f;
    private const float ANCHO = 24f;
    private const float LARGO = 30f;
    private const float ALTO = 9f;

    // Medidas de la primera sala: las posiciones de la decoracion estan
    // escritas pensando en esta sala y se reparten en la sala nueva
    private const float ANCHO_BASE = 12f;
    private const float LARGO_BASE = 18f;
    private const float MARGEN = 0.5f;

    private enum Lado { Centro, Oeste, Este, Norte, Sur }

    [MenuItem("Proyecto Final/Montar Recepcion (decoracion y coleccionables)")]
    private static void Montar()
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "01_Recepcion")
        {
            EditorUtility.DisplayDialog("Montar Recepcion", "Abre primero la escena 01_Recepcion.", "Ok");
            return;
        }

        Directory.CreateDirectory(RUTA_MATERIALES);

        float alturaMostrador = Decorar();
        CrearColeccionables(alturaMostrador);
        ParticleSystem chispas = CrearChispas();
        MontarCanvas();
        ConfigurarControlador(chispas);
        ConfigurarBoton();

        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("Recepcion montada: decoracion, 3 coleccionables, chispas, CanvasJuego y sonidos. Guarda la escena (Ctrl/Cmd + S).");
    }

    // ---------- Decoracion ----------

    // Devuelve la altura de la parte de arriba del mostrador (ahi va la tarjeta)
    private static float Decorar()
    {
        Transform padre = NuevoPadre("Decoracion");

        // Mostrador de la recepcion, en el centro frente a la entrada
        GameObject mostrador = Colocar(padre, "Crate Long", 6.5f, 6.5f, 90f, Lado.Centro);
        Colocar(padre, "Crate Short", 3.9f, 6.5f, 0f, Lado.Centro);

        // Pared oeste: estanterias, extintor y caneca
        Colocar(padre, "Fire Extinguisher", 0f, 2.5f, 90f, Lado.Oeste);
        Colocar(padre, "Garbage Bin", 0f, 4.8f, 0f, Lado.Oeste);
        Colocar(padre, "Shelf Variation 01", 0f, 9.5f, 90f, Lado.Oeste);
        Colocar(padre, "Shelf Variation 04", 0f, 13.2f, 90f, Lado.Oeste);
        Colocar(padre, "Cart", 2.6f, 16f, 20f, Lado.Centro);

        // Pared este: pallets, barriles (uno caido), cajas y la caja de fusibles
        Colocar(padre, "Pallet Variation 03", 0f, 3.5f, 0f, Lado.Este);
        Colocar(padre, "Fusebox 01", 0f, 5.6f, -90f, Lado.Este, 1.3f);
        Colocar(padre, "Barrel", 0f, 7.6f, 0f, Lado.Este);
        Colocar(padre, "Barrel", 0f, 8.7f, 0f, Lado.Este);
        GameObject caido = Colocar(padre, "Barrel", 9.6f, 9.4f, 0f, Lado.Centro);
        if (caido != null)
        {
            caido.transform.rotation = Quaternion.Euler(0f, 30f, 90f);
            caido.transform.position += Vector3.up * 0.5f;
        }
        Colocar(padre, "Crate Short", 0f, 13.6f, 15f, Lado.Este);
        Colocar(padre, "Crate Long", 0f, 15.6f, 0f, Lado.Este);

        // Columnas en medio de la sala: sirven para esconderse del dron
        Colocar(padre, "Pillar", 3f, 11f, 0f, Lado.Centro);
        Colocar(padre, "Pillar", 9f, 11f, 0f, Lado.Centro);

        // Techo: tuberias, rociadores y camara de seguridad en una esquina
        Colocar(padre, "Pipes 02", 6f, 9f, 90f, Lado.Centro, 7.8f);
        Colocar(padre, "Sprinkler 01", 6f, 4f, 0f, Lado.Centro, 8.6f);
        Colocar(padre, "Sprinkler 01", 6f, 14f, 0f, Lado.Centro, 8.6f);
        Colocar(padre, "Security Cam", 0f, 1f, -90f, Lado.Este, 6.5f);

        float alto = 1f;
        if (mostrador != null && Limites(mostrador, out Bounds b))
        {
            alto = b.max.y;
        }

        return alto;
    }

    private static GameObject Colocar(Transform padre, string nombrePrefab, float x, float z, float rotY, Lado lado, float y = 0f)
    {
        GameObject prefab = BuscarPrefab(nombrePrefab);
        if (prefab == null)
        {
            return null;
        }

        var objeto = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        objeto.transform.position = Reubicar(new Vector3(x, y, z), ANCHO_BASE, LARGO_BASE);
        objeto.transform.rotation = Quaternion.Euler(0f, rotY, 0f);

        if (!Limites(objeto, out Bounds b))
        {
            return objeto;
        }

        // Pegar a la pared que corresponde
        Vector3 mover = Vector3.zero;
        if (lado == Lado.Oeste) mover.x = MARGEN - b.min.x;
        if (lado == Lado.Este) mover.x = (ANCHO - MARGEN) - b.max.x;
        if (lado == Lado.Sur) mover.z = MARGEN - b.min.z;
        if (lado == Lado.Norte) mover.z = (LARGO - MARGEN) - b.max.z;
        objeto.transform.position += mover;

        // Que nada quede atravesando una pared
        Limites(objeto, out b);
        mover = Vector3.zero;
        if (b.min.x < MARGEN) mover.x = MARGEN - b.min.x;
        else if (b.max.x > ANCHO - MARGEN) mover.x = (ANCHO - MARGEN) - b.max.x;
        if (b.min.z < MARGEN) mover.z = MARGEN - b.min.z;
        else if (b.max.z > LARGO - MARGEN) mover.z = (LARGO - MARGEN) - b.max.z;
        objeto.transform.position += mover;

        return objeto;
    }

    // ---------- Coleccionables ----------

    private static void CrearColeccionables(float alturaMostrador)
    {
        Transform padre = NuevoPadre("Coleccionables");
        AudioClip sonido = Sonido("pressure_release_01.wav");

        CrearColeccionable(padre, PrimitiveType.Cube, "tarjeta_acceso", "Tarjeta de acceso",
            Reubicar(new Vector3(6.5f, alturaMostrador + 0.2f, 6.5f), ANCHO_BASE, LARGO_BASE), new Vector3(0.35f, 0.03f, 0.22f), new Color(0.2f, 0.6f, 1f), sonido);

        CrearColeccionable(padre, PrimitiveType.Cube, "registro_incidente", "Registro del incidente",
            Reubicar(new Vector3(1.8f, 0.5f, 11.4f), ANCHO_BASE, LARGO_BASE), new Vector3(0.3f, 0.04f, 0.4f), new Color(1f, 0.85f, 0.2f), sonido);

        CrearColeccionable(padre, PrimitiveType.Cylinder, "fusible", "Fusible de repuesto",
            Reubicar(new Vector3(9.6f, 0.5f, 14.6f), ANCHO_BASE, LARGO_BASE), new Vector3(0.12f, 0.2f, 0.12f), new Color(1f, 0.45f, 0.1f), sonido);
    }

    private static void CrearColeccionable(Transform padre, PrimitiveType forma, string id, string nombre, Vector3 posicion, Vector3 escala, Color color, AudioClip sonido)
    {
        GameObject objeto = GameObject.CreatePrimitive(forma);
        objeto.name = nombre;
        objeto.transform.SetParent(padre);
        objeto.transform.position = posicion;
        objeto.transform.localScale = escala;
        objeto.GetComponent<Renderer>().sharedMaterial = MaterialBrillante("Coleccionable_" + id, color);

        // Luz pequena para que se vea en la sala oscura
        var luz = new GameObject("Brillo").AddComponent<Light>();
        luz.transform.SetParent(objeto.transform, false);
        luz.type = LightType.Point;
        luz.color = color;
        luz.range = 2.5f;
        luz.intensity = 2f;

        var coleccionable = objeto.AddComponent<Coleccionable>();
        coleccionable.idObjeto = id;
        coleccionable.nombreVisible = nombre;
        coleccionable.sonidoRecoger = sonido;
    }

    private static Material MaterialBrillante(string nombre, Color color)
    {
        string ruta = RUTA_MATERIALES + "/" + nombre + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }

        material.SetColor("_BaseColor", color);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", color * 2f);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- Particulas ----------

    private static ParticleSystem CrearChispas()
    {
        Transform padre = NuevoPadre("Efectos");

        // Frente a la caja de fusibles de la pared este
        var objeto = new GameObject("ChispasFusible");
        objeto.transform.SetParent(padre);
        objeto.transform.position = Reubicar(new Vector3(ANCHO_BASE - 0.8f, 1.6f, 5.6f), ANCHO_BASE, LARGO_BASE);
        objeto.transform.rotation = Quaternion.Euler(-30f, -90f, 0f);

        var chispas = objeto.AddComponent<ParticleSystem>();
        var principal = chispas.main;
        principal.startLifetime = 0.5f;
        principal.startSpeed = 3f;
        principal.startSize = 0.05f;
        principal.startColor = new Color(1f, 0.7f, 0.2f);
        principal.gravityModifier = 1f;
        principal.maxParticles = 200;

        var emision = chispas.emission;
        emision.rateOverTime = 0f;
        emision.SetBursts(new[] { new ParticleSystem.Burst(0f, 15, 25, 0, 0.8f) });

        var forma = chispas.shape;
        forma.shapeType = ParticleSystemShapeType.Cone;
        forma.angle = 35f;
        forma.radius = 0.05f;

        string ruta = RUTA_MATERIALES + "/Chispas.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.SetColor("_BaseColor", new Color(1f, 0.75f, 0.3f));
        EditorUtility.SetDirty(material);
        objeto.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;

        return chispas;
    }

    // ---------- Canvas (un solo Canvas, como prefab) ----------

    private static void MontarCanvas()
    {
        if (Object.FindFirstObjectByType<UIManager>() != null)
        {
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RUTA_CANVAS);
        if (prefab != null)
        {
            PrefabUtility.InstantiatePrefab(prefab);
            return;
        }

        var raiz = new GameObject("CanvasJuego", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        raiz.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = raiz.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;

        var hud = new GameObject("PanelHUD", typeof(RectTransform));
        hud.transform.SetParent(raiz.transform, false);
        var rectHud = hud.GetComponent<RectTransform>();
        rectHud.anchorMin = Vector2.zero;
        rectHud.anchorMax = Vector2.one;
        rectHud.offsetMin = Vector2.zero;
        rectHud.offsetMax = Vector2.zero;

        var ui = raiz.AddComponent<UIManager>();
        ui.textoObjetivo = Texto(hud.transform, "TextoObjetivo", new Vector2(0, 1), new Vector2(30, -30), new Vector2(1100, 60), TextAlignmentOptions.TopLeft, 30, Color.white);
        ui.textoVida = Texto(hud.transform, "TextoVida", new Vector2(1, 1), new Vector2(-30, -30), new Vector2(400, 60), TextAlignmentOptions.TopRight, 30, Color.white);
        ui.textoMensaje = Texto(hud.transform, "TextoMensaje", new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(1300, 60), TextAlignmentOptions.Center, 32, new Color(1f, 0.85f, 0.3f));
        ui.textoAviso = Texto(hud.transform, "TextoAviso", new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(1000, 60), TextAlignmentOptions.Center, 30, Color.white);
        TMP_Text mira = Texto(hud.transform, "Mira", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(60, 60), TextAlignmentOptions.Center, 36, Color.white);
        mira.text = "+";

        PrefabUtility.SaveAsPrefabAssetAndConnect(raiz, RUTA_CANVAS, InteractionMode.UserAction);
    }

    private static TMP_Text Texto(Transform padre, string nombre, Vector2 ancla, Vector2 posicion, Vector2 tamano, TextAlignmentOptions alineacion, float tamanoLetra, Color color)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform));
        objeto.transform.SetParent(padre, false);
        var rect = objeto.GetComponent<RectTransform>();
        rect.anchorMin = ancla;
        rect.anchorMax = ancla;
        rect.pivot = ancla;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamano;

        var texto = objeto.AddComponent<TextMeshProUGUI>();
        texto.alignment = alineacion;
        texto.fontSize = tamanoLetra;
        texto.color = color;
        texto.text = "";
        return texto;
    }

    // ---------- Controlador y boton ----------

    private static void ConfigurarControlador(ParticleSystem chispas)
    {
        var controlador = Object.FindFirstObjectByType<Zona01Controller>();
        if (controlador == null)
        {
            Debug.LogWarning("No hay Zona01Controller en la escena.");
            return;
        }

        controlador.lucesNormales = GameObject.Find("LucesNormales");
        controlador.lucesEmergencia = GameObject.Find("LucesEmergencia");
        controlador.chispasFusible = chispas;

        var fuentes = new List<AudioSource>(controlador.GetComponents<AudioSource>());
        while (fuentes.Count < 3)
        {
            fuentes.Add(controlador.gameObject.AddComponent<AudioSource>());
        }

        // 0: ambiente (suena siempre), 1: sirena (la maneja el controlador), 2: efectos
        fuentes[0].clip = Sonido("background_rumble.wav");
        fuentes[0].loop = true;
        fuentes[0].playOnAwake = true;
        fuentes[0].volume = 0.4f;

        fuentes[1].clip = Sonido("siren.wav");
        fuentes[1].loop = true;
        fuentes[1].playOnAwake = false;
        fuentes[1].volume = 0.2f;

        fuentes[2].clip = null;
        fuentes[2].playOnAwake = false;

        controlador.sirena = fuentes[1];
        controlador.fuenteEfectos = fuentes[2];
        controlador.sonidoEnergia = Sonido("enginestart.wav");
        EditorUtility.SetDirty(controlador);
    }

    private static void ConfigurarBoton()
    {
        var boton = Object.FindFirstObjectByType<BotonCambioEscena>();
        if (boton == null)
        {
            Debug.LogWarning("No hay BotonCambioEscena en la escena.");
            return;
        }

        boton.objetivoRequerido = "recepcion_completa";

        var fuente = boton.GetComponent<AudioSource>();
        if (fuente == null)
        {
            fuente = boton.gameObject.AddComponent<AudioSource>();
        }
        fuente.playOnAwake = false;

        boton.fuenteAudio = fuente;
        boton.sonidoBloqueado = Sonido("wompwomp.wav");
        boton.sonidoActivado = Sonido("pressure_release_02.wav");
        EditorUtility.SetDirty(boton);
    }

    // ---------- Sala (piso, paredes, techo y luces) ----------

    [MenuItem("Proyecto Final/Construir sala grande (piso, paredes, techo y luces)")]
    private static void ConstruirSala()
    {
        var escena = EditorSceneManager.GetActiveScene();
        if (escena.name != "01_Recepcion")
        {
            EditorUtility.DisplayDialog("Construir sala", "Abre primero la escena 01_Recepcion.", "Ok");
            return;
        }

        GameObject escenarioObj = GameObject.Find("Escenario");
        if (escenarioObj == null)
        {
            escenarioObj = new GameObject("Escenario");
            Undo.RegisterCreatedObjectUndo(escenarioObj, "Construir sala");
        }
        Transform escenario = escenarioObj.transform;

        // Medir la sala que hay ahora mirando las baldosas del piso
        Transform piso = Hijo(escenario, "Piso");
        float anchoViejo = 0f;
        float largoViejo = 0f;
        foreach (Transform baldosa in piso)
        {
            anchoViejo = Mathf.Max(anchoViejo, baldosa.position.x + BALDOSA);
            largoViejo = Mathf.Max(largoViejo, baldosa.position.z + BALDOSA);
        }

        ConstruirPisoYTecho(piso, Hijo(escenario, "Techo"));
        ConstruirParedes(Hijo(escenario, "Paredes"));
        ConstruirLuces(Hijo(escenario, "Luces"));

        // Si la sala cambio de tamano, reacomodar lo que ya estaba puesto
        if (anchoViejo > 0f && (anchoViejo != ANCHO || largoViejo != LARGO))
        {
            MoverHijos("Decoracion", anchoViejo, largoViejo);
            MoverHijos("Coleccionables", anchoViejo, largoViejo);
            MoverHijos("Efectos", anchoViejo, largoViejo);
            MoverObjeto(GameObject.Find("Player"), anchoViejo, largoViejo);

            // El boton va junto a la puerta de salida: solo se corre hacia la pared norte
            GameObject boton = GameObject.Find("BotonSalida");
            if (boton != null)
            {
                Undo.RecordObject(boton.transform, "Construir sala");
                boton.transform.position += new Vector3(0f, 0f, LARGO - largoViejo);
            }
        }

        // Paredes de adentro que dividen la sala en cuartos
        ConstruirCuartos(Hijo(escenario, "ParedesCuartos"));

        EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log("Sala construida de " + ANCHO + " x " + LARGO + ". Revisa la escena y guarda (Ctrl/Cmd + S).");
    }

    private static void ConstruirPisoYTecho(Transform piso, Transform techo)
    {
        Vaciar(piso);
        Vaciar(techo);

        for (float x = 0f; x < ANCHO; x += BALDOSA)
        {
            for (float z = 0f; z < LARGO; z += BALDOSA)
            {
                Pieza(piso, "Floor Tile 01", new Vector3(x, 0f, z), 0f);
                Pieza(techo, "Ceiling Closed", new Vector3(x, ALTO, z), 0f);
            }
        }
    }

    // Cada pared del kit mide 6 de largo. El giro decide hacia donde mira.
    private static void ConstruirParedes(Transform paredes)
    {
        Vaciar(paredes);

        int i = 0;
        for (float z = 0f; z < LARGO; z += BALDOSA)
        {
            // Oeste con ventanas y este con ventiladores, intercalados
            Pieza(paredes, i % 2 == 1 ? "Wall Window" : "Wall Plain", new Vector3(0f, 0f, z), 0f);
            Pieza(paredes, i % 2 == 1 ? "Wall Fan" : "Wall Plain", new Vector3(ANCHO, 0f, z + BALDOSA), 180f);
            i++;
        }

        // Sur: puerta cerrada (por donde se entro). Norte: puerta de salida al laboratorio
        for (float x = 0f; x < ANCHO; x += BALDOSA)
        {
            Pieza(paredes, x == 0f ? "Wall DoorClosed" : "Wall Plain", new Vector3(x, 0f, 0f), 90f);
            Pieza(paredes, x == 0f ? "Wall Door" : "Wall Plain", new Vector3(x + BALDOSA, 0f, LARGO), 270f);
        }
    }

    // La sala queda en 3 cuartos:
    //   - Vestibulo (sur, todo el ancho): donde empieza el jugador, con el mostrador
    //   - Archivo (noroeste): tiene la puerta de salida al laboratorio
    //   - Bodega (noreste): se entra desde el archivo
    // Se camina vestibulo -> archivo -> bodega por puertas abiertas.
    private const float LINEA_VESTIBULO = 12f;   // pared en Z que separa el vestibulo
    private const float LINEA_CUARTOS = 12f;     // pared en X que separa archivo y bodega

    private static void ConstruirCuartos(Transform padre)
    {
        Vaciar(padre);

        // Pared de lado a lado en Z = 12, con puerta entre X = 6 y 12
        for (float x = 0f; x < ANCHO; x += BALDOSA)
        {
            ParedDoble(padre, x == 6f, new Vector3(x, 0f, LINEA_VESTIBULO), true);
        }

        // Pared en X = 12 desde el vestibulo hasta el fondo, con puerta entre Z = 18 y 24
        for (float z = LINEA_VESTIBULO; z < LARGO; z += BALDOSA)
        {
            ParedDoble(padre, z == 18f, new Vector3(LINEA_CUARTOS, 0f, z), false);
        }

        SacarDeLasParedes();
    }

    // Pone dos paredes espalda con espalda para que se vean de los dos lados.
    // inicio es la esquina donde empieza el tramo de 6.
    private static void ParedDoble(Transform padre, bool conPuerta, Vector3 inicio, bool cruzaEnX)
    {
        string pieza = conPuerta ? "Wall Door" : "Wall Plain";
        if (cruzaEnX)
        {
            // Igual que las paredes sur (mira a +Z) y norte (mira a -Z)
            Pieza(padre, pieza, inicio, 90f);
            Pieza(padre, pieza, inicio + new Vector3(BALDOSA, 0f, 0f), 270f);
        }
        else
        {
            // Igual que las paredes oeste (mira a +X) y este (mira a -X)
            Pieza(padre, pieza, inicio, 0f);
            Pieza(padre, pieza, inicio + new Vector3(0f, 0f, BALDOSA), 180f);
        }
    }

    // Si algun objeto quedo atravesando una pared de los cuartos o tapando
    // una puerta, se corre hacia el lado donde esta su centro.
    private static void SacarDeLasParedes()
    {
        GameObject decoracion = GameObject.Find("Decoracion");
        GameObject coleccionables = GameObject.Find("Coleccionables");

        if (decoracion != null)
        {
            foreach (Transform objeto in decoracion.transform)
            {
                if (!Limites(objeto.gameObject, out Bounds antes))
                {
                    continue;
                }

                Vector3 mover = CuantoCorrer(antes);
                if (mover == Vector3.zero)
                {
                    continue;
                }

                Undo.RecordObject(objeto, "Construir sala");
                objeto.position += mover;

                // Lo que estaba encima (por ejemplo la tarjeta en el mostrador) se mueve igual
                if (coleccionables != null)
                {
                    foreach (Transform cosa in coleccionables.transform)
                    {
                        Vector3 p = cosa.position;
                        if (p.x > antes.min.x && p.x < antes.max.x && p.z > antes.min.z && p.z < antes.max.z)
                        {
                            Undo.RecordObject(cosa, "Construir sala");
                            cosa.position += mover;
                        }
                    }
                }
            }
        }

        if (coleccionables != null)
        {
            foreach (Transform cosa in coleccionables.transform)
            {
                if (Limites(cosa.gameObject, out Bounds b))
                {
                    Undo.RecordObject(cosa, "Construir sala");
                    cosa.position += CuantoCorrer(b);
                }
            }
        }
    }

    private static Vector3 CuantoCorrer(Bounds b)
    {
        Vector3 mover = Vector3.zero;

        // Pared en Z = LINEA_VESTIBULO (todo el ancho). Puerta de X = 6 a 12.
        bool enPuertaZ = b.max.x > 6f && b.min.x < 12f;
        mover.z = Separar(b.min.z, b.max.z, LINEA_VESTIBULO, enPuertaZ ? 1.5f : MARGEN);

        // Pared en X = LINEA_CUARTOS (solo del vestibulo hacia el fondo). Puerta de Z = 18 a 24.
        if (b.max.z + mover.z > LINEA_VESTIBULO)
        {
            bool enPuertaX = b.max.z > 18f && b.min.z < 24f;
            mover.x = Separar(b.min.x, b.max.x, LINEA_CUARTOS, enPuertaX ? 1.5f : MARGEN);
        }

        return mover;
    }

    // Cuanto hay que correr un objeto (de min a max) para que quede a
    // "espacio" de la linea, hacia el lado donde esta su centro
    private static float Separar(float min, float max, float linea, float espacio)
    {
        if (max < linea - espacio || min > linea + espacio)
        {
            return 0f;
        }

        float centro = (min + max) / 2f;
        if (centro < linea)
        {
            return (linea - espacio) - max;
        }
        return (linea + espacio) - min;
    }

    private static void ConstruirLuces(Transform luces)
    {
        Transform normales = Hijo(luces, "LucesNormales");
        Transform emergencia = Hijo(luces, "LucesEmergencia");

        // Quitar lo que no sea uno de los dos grupos (el letrero de salida viejo)
        for (int i = luces.childCount - 1; i >= 0; i--)
        {
            Transform hijo = luces.GetChild(i);
            if (hijo != normales && hijo != emergencia)
            {
                Undo.DestroyObjectImmediate(hijo.gameObject);
            }
        }

        // Lamparas del techo: 2 columnas y 3 filas
        Vaciar(normales);
        Color blanco = new Color(0.75f, 0.85f, 1f);
        for (int columna = 1; columna <= 3; columna += 2)
        {
            for (int fila = 1; fila <= 5; fila += 2)
            {
                Vector3 posicion = new Vector3(ANCHO * columna / 4f, ALTO, LARGO * fila / 6f);
                AgregarLuz(Pieza(normales, "Hanging Light", posicion, 0f), new Vector3(0f, -1f, 0f), blanco, 8f, 18f);
            }
        }

        // Luces rojas de emergencia: dos en cada pared larga y una sobre la salida
        Vaciar(emergencia);
        Color rojo = new Color(1f, 0.165f, 0.1f);
        for (int parte = 1; parte <= 2; parte++)
        {
            float z = LARGO * parte / 3f;
            AgregarLuz(Pieza(emergencia, "Wall Light", new Vector3(0.3f, 6f, z), 0f), Vector3.zero, rojo, 5f, 12f);
            AgregarLuz(Pieza(emergencia, "Wall Light", new Vector3(ANCHO - 0.3f, 6f, z), 180f), Vector3.zero, rojo, 5f, 12f);
        }
        AgregarLuz(Pieza(emergencia, "Wall Light", new Vector3(3f, 7f, LARGO - 0.3f), 90f), Vector3.zero, rojo, 5f, 10f);

        // Letrero verde de salida encima de la puerta
        AgregarLuz(Pieza(luces, "Exit Sign", new Vector3(3f, 4.5f, LARGO - 0.2f), 90f), Vector3.zero, new Color(0.2f, 1f, 0.4f), 2f, 4f);
    }

    private static GameObject Pieza(Transform padre, string nombrePrefab, Vector3 posicion, float rotY)
    {
        GameObject prefab = BuscarPrefab(nombrePrefab);
        if (prefab == null)
        {
            return null;
        }

        var objeto = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        objeto.transform.position = posicion;
        objeto.transform.rotation = Quaternion.Euler(0f, rotY, 0f);
        Undo.RegisterCreatedObjectUndo(objeto, "Construir sala");
        return objeto;
    }

    private static void AgregarLuz(GameObject objeto, Vector3 posicionLocal, Color color, float intensidad, float rango)
    {
        if (objeto == null)
        {
            return;
        }

        var luz = new GameObject("Point Light").AddComponent<Light>();
        luz.transform.SetParent(objeto.transform, false);
        luz.transform.localPosition = posicionLocal;
        luz.type = LightType.Point;
        luz.color = color;
        luz.intensity = intensidad;
        luz.range = rango;
        luz.shadows = LightShadows.None;
    }

    // Pasa una posicion de una sala vieja a la sala nueva (ANCHO x LARGO).
    // Lo que esta pegado a una pared se queda pegado a esa misma pared
    // y lo que esta en el medio se reparte en proporcion.
    private static Vector3 Reubicar(Vector3 posicion, float anchoViejo, float largoViejo)
    {
        posicion.x = ReubicarEje(posicion.x, anchoViejo, ANCHO);
        posicion.z = ReubicarEje(posicion.z, largoViejo, LARGO);
        return posicion;
    }

    private static float ReubicarEje(float valor, float medidaVieja, float medidaNueva)
    {
        if (valor < 2f)
        {
            return valor;
        }
        if (valor > medidaVieja - 2f)
        {
            return valor + (medidaNueva - medidaVieja);
        }
        return valor * medidaNueva / medidaVieja;
    }

    private static void MoverHijos(string nombre, float anchoViejo, float largoViejo)
    {
        GameObject grupo = GameObject.Find(nombre);
        if (grupo == null)
        {
            return;
        }

        foreach (Transform hijo in grupo.transform)
        {
            MoverObjeto(hijo.gameObject, anchoViejo, largoViejo);
        }
    }

    private static void MoverObjeto(GameObject objeto, float anchoViejo, float largoViejo)
    {
        if (objeto == null)
        {
            return;
        }

        Undo.RecordObject(objeto.transform, "Construir sala");
        objeto.transform.position = Reubicar(objeto.transform.position, anchoViejo, largoViejo);
    }

    // Busca un hijo por nombre (aunque este apagado) y si no existe lo crea
    private static Transform Hijo(Transform padre, string nombre)
    {
        Transform hijo = padre.Find(nombre);
        if (hijo != null)
        {
            return hijo;
        }

        var nuevo = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(nuevo, "Construir sala");
        nuevo.transform.SetParent(padre, false);
        return nuevo.transform;
    }

    private static void Vaciar(Transform padre)
    {
        for (int i = padre.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(padre.GetChild(i).gameObject);
        }
    }

    // ---------- Utilidades ----------

    private static Transform NuevoPadre(string nombre)
    {
        GameObject viejo = GameObject.Find(nombre);
        if (viejo != null)
        {
            Object.DestroyImmediate(viejo);
        }

        var padre = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(padre, "Montar Recepcion");
        return padre.transform;
    }

    private static GameObject BuscarPrefab(string nombre)
    {
        foreach (string guid in AssetDatabase.FindAssets(nombre + " t:Prefab", new[] { RUTA_KIT }))
        {
            string ruta = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(ruta) == nombre)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
            }
        }

        Debug.LogWarning("No encontre el prefab " + nombre + " en el kit.");
        return null;
    }

    private static AudioClip Sonido(string archivo)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(RUTA_SONIDOS + archivo);
    }

    private static bool Limites(GameObject objeto, out Bounds limites)
    {
        Renderer[] renderers = objeto.GetComponentsInChildren<Renderer>();
        limites = new Bounds(objeto.transform.position, Vector3.zero);
        if (renderers.Length == 0)
        {
            return false;
        }

        limites = renderers[0].bounds;
        foreach (Renderer r in renderers)
        {
            limites.Encapsulate(r.bounds);
        }

        return true;
    }
}
