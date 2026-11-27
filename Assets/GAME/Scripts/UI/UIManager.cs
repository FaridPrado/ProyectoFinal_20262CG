using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ControlRoomZ03;
using TMPro;

public sealed class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
    private static string ReadableName(string value)
    {
        string text = value.Replace("_", " ").Trim();
        return text.Length == 0 ? "Objeto" : char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
    }

    [Header("Avisos y objetivos")]
    [SerializeField] private GameObject panelAviso;
    [SerializeField] private TextMeshProUGUI textoAviso;
    [SerializeField] private TextMeshProUGUI textoObjetivo;

    // Objetivo y aviso que manda el controlador de la escena (por ejemplo
    // Zona01Controller o un Coleccionable). Si el HUD no tiene textos propios
    // para ellos, se muestran en el HUD compartido (ver LateUpdate)
    private string objetivoEscena;
    private string avisoEscena;

    public void MostrarAviso(string mensaje)
    {
        avisoEscena = mensaje;
        if (textoAviso != null) textoAviso.text = mensaje;
        if (panelAviso != null) panelAviso.SetActive(true);
    }

    public void OcultarAviso()
    {
        avisoEscena = null;
        if (panelAviso != null) panelAviso.SetActive(false);
    }

    public void MostrarObjetivo(string objetivo)
    {
        objetivoEscena = objetivo;
        if (textoObjetivo != null) textoObjetivo.text = objetivo;
    }

    [SerializeField] private GameObject panel;
    [SerializeField] private Text title;
    [SerializeField] private Text body;
    [SerializeField] private Text avisoSistema;
    [SerializeField] private Image damage;
    [SerializeField] private RawImage feed;
    [SerializeField] private Camera security;
    [SerializeField] private GameObject mira;
    [SerializeField] private GameObject eventos;
    [SerializeField] private Button[] opciones;
    private float finAviso;
    private bool mostrandoDerrota;
    private readonly Stack<string> pages = new Stack<string>();
    private Z03ZoneGameplay zone;
    private Zona04Controller zona04;
    private InteractionController interaccion;
    private Z03HudPresenter hud;
    private string escena;
    private bool terminalOpen;
    private bool wasPaused;
    private int lastHealth = 100;
    private float flash;
    void Start()
    {
        escena = gameObject.scene.name;
        if (escena == "00_Menu_Respaldo" || escena == "00_Menu")
        {
            enabled = false;
            return;
        }

        zone = FindFirstObjectByType<Z03ZoneGameplay>();
        zona04 = FindFirstObjectByType<Zona04Controller>();
        interaccion = FindFirstObjectByType<InteractionController>();
        hud = GetComponent<Z03HudPresenter>();
        if (eventos != null) eventos.SetActive(false);
        mira.SetActive(zone != null || zona04 != null);
        if (mira != null) mira.SetActive(zone != null);
        if (GameManager.Instance) lastHealth = GameManager.Instance.vidaActual;
    }

    private void Opcion(string label, int fila, UnityEngine.Events.UnityAction accion)
    {
        Button boton = opciones[fila];
        boton.name = "Botón " + label.ToLowerInvariant();
        boton.GetComponentInChildren<Text>(true).text = label;
        boton.onClick.AddListener(accion);
        boton.gameObject.SetActive(true);
    }

    private void Clear()
    {
        foreach (Button boton in opciones)
        {
            boton.onClick.RemoveAllListeners();
            boton.gameObject.SetActive(false);
        }
        feed.gameObject.SetActive(false);
        if (security) security.enabled = false;
    }

    public void OpenTerminal()
    {
        if (!panel)
            return;
        pages.Clear();
        pages.Push("Inicio");
        terminalOpen = true;
        GameManager.Instance.AlternarPausa();
        RenderTerminal();
    }

    void RenderTerminal()
    {
        Clear();
        panel.SetActive(true);
        if (zona04 != null)
        {
            RenderTerminalZona04();
            return;
        }
        title.text = "TERMINAL / " + pages.Peek();
        if (pages.Peek() == "Inicio")
        {
            body.text = "Oeste: recupera el registro y valida vigilancia.\nEste: conecta la reserva y restablece energía.\nPara salir, presenta la credencial del laboratorio.";
            Opcion("Diagnóstico y cámara de seguridad", 0, () =>
            {
                pages.Push("Diagnóstico");
                RenderTerminal();
            });
            Opcion("Autorizar acceso a Servidores", 1, () =>
            {
                Close();
                zone.Interactuar(Z03Action.Terminal);
            });
            Opcion("Cerrar terminal", 2, Close);
        }
        else
        {
            body.text = zone.Diagnostico();
            feed.gameObject.SetActive(true);
            security.enabled = true;
            Opcion("Atrás", 0, () =>
            {
                if (pages.Count > 1)
                    pages.Pop();
                RenderTerminal();
            });
            Opcion("Cerrar", 1, Close);
        }
    }

    // Terminal de la Zona 4 (sala de servidores). Es un panel del mismo Canvas
    // prefab; el contenido lo da el Zona04Controller. La navegacion usa la
    // pila de paginas: entrar = Push, Atras = Pop (vuelve a la pagina anterior)
    void RenderTerminalZona04()
    {
        string pagina = pages.Peek();
        title.text = "TERMINAL SERVIDORES / " + pagina.ToUpperInvariant();
        body.text = zona04.TextoTerminal(pagina);

        if (pagina == Zona04Controller.PAGINA_INICIO)
        {
            Opcion("Orden de reinicio", 0, () =>
            {
                pages.Push(Zona04Controller.PAGINA_ORDEN);
                RenderTerminal();
            });
            Opcion("Cámara del túnel", 1, () =>
            {
                pages.Push(Zona04Controller.PAGINA_CAMARA);
                RenderTerminal();
            });
            // Boton de accion: el señuelo atrae a todos los agentes a la sala oeste
            Opcion(zona04.SenueloDisponible ? "Activar señuelo acústico" : "Señuelo recargando (" + zona04.SegundosParaSenuelo + " s)", 2, () =>
            {
                zona04.ActivarSenuelo();
                Close();
            });
            Opcion("Cerrar sesión", 3, () =>
            {
                zona04.CerrarSesion();
                Close();
            });
        }
        else
        {
            if (pagina == Zona04Controller.PAGINA_CAMARA && security != null)
            {
                feed.gameObject.SetActive(true);
                security.enabled = true;
            }

            Opcion("Atrás", 0, () =>
            {
                if (pages.Count > 1)
                    pages.Pop();
                RenderTerminal();
            });
            Opcion("Salir", 1, Close);
        }
    }

    // Zona 4: vida del jugador y, cuando el jefe entra en combate, su vida en %.
    // El texto solo se rearma cuando cambia algun valor (sin basura por frame)
    private int ultimaVidaMostrada = -1;
    private int ultimoJefeMostrado = -2;
    private string textoEstadoZona04;

    void ActualizarEstadoZona04(GameManager gm)
    {
        JefeMutante jefe = zona04.Jefe;
        int jefePct = jefe != null && jefe.MostrarEnHUD ? Mathf.CeilToInt(jefe.VidaNormalizada * 100f) : -1;
        if (jefePct >= 0 && jefe.EnFuria)
            jefePct += 1000;   // distinto valor para que se rearme al entrar en furia

        if (gm.vidaActual != ultimaVidaMostrada || jefePct != ultimoJefeMostrado || textoEstadoZona04 == null)
        {
            ultimaVidaMostrada = gm.vidaActual;
            ultimoJefeMostrado = jefePct;
            textoEstadoZona04 = "VIDA " + gm.vidaActual + " / " + gm.vidaMaxima;
            if (jefePct >= 0)
                textoEstadoZona04 += "   ·   ESPÉCIMEN " + (jefePct % 1000) + "%" + (jefe.EnFuria ? " (FURIA)" : "");
        }

        hud.SetStatus(textoEstadoZona04);
    }

    void Pause()
    {
        Clear();
        panel.SetActive(true);
        title.text = "PARTIDA EN PAUSA";
        body.text = "WASD · caminar    Shift · correr    Ctrl · agacharse\nRatón · mirar    E · interactuar    Esc · continuar";
        Opcion("Continuar", 0, Close);
        Opcion("Cargar último checkpoint", 1, GameManager.Instance.ReiniciarDesdeGuardado);
        Opcion("Guardar y volver al menú", 2, ReturnMenu);
        Opcion("Inventario y evidencias", 3, Inventario);
    }

    private void Inventario()
    {
        Clear();
        title.text = "INVENTARIO Y EVIDENCIAS";
        var objetos = GameManager.Instance.inventario;
        body.text = objetos.Count == 0 ? "Todavía no has recogido objetos." :
            string.Join("\n", objetos.ConvertAll(id => ReadableName(id.Replace("z03_", ""))));
        Opcion("Atrás", 0, Pause);
    }

    void ReturnMenu()
    {
        var partida = GameManager.Instance;
        partida.GuardarPartida(partida.puntoControl);
        partida.IrAlMenu();
    }

    void Close()
    {
        terminalOpen = false;
        pages.Clear();
        panel.SetActive(false);
        if (security)
            security.enabled = false;
        if (GameManager.Instance.enPausa)
            GameManager.Instance.AlternarPausa();
    }

    void LateUpdate()
    {
        if (!panel)
            return;
        var gm = GameManager.Instance;
        if (gm.jugadorMuerto)
        {
            if (!mostrandoDerrota)
            {
                mostrandoDerrota = true;
                Clear();
                panel.SetActive(true);
                title.text = "SIN VIDA";
                body.text = "Recuperando el punto de control: " + ReadableName(gm.puntoControl);
                damage.color = new Color(.65f, .02f, .02f, .3f);
            }
            return;
        }

        if (avisoSistema && !gm.enPausa && Time.time >= finAviso)
        {
            if (gm.HayMensajes())
            {
                avisoSistema.text = gm.SiguienteMensaje();
                finAviso = Time.time + 2.8f;
            }
            else if (avisoSistema.text.Length > 0)
                avisoSistema.text = "";
        }

        if (gm.gano)
        {
            if (!panel.activeSelf)
            {
                Clear();
                panel.SetActive(true);
                if (zona04 != null)
                {
                    int minutos = (int)(gm.tiempoJugado / 60f);
                    int segundos = (int)(gm.tiempoJugado % 60f);
                    title.text = "EVACUACIÓN COMPLETADA";
                    body.text = "Reiniciaste los servidores y escapaste por la compuerta del túnel norte.\n" +
                                "Tiempo de partida: " + minutos + " min " + segundos.ToString("00") + " s   ·   Muertes: " + gm.muertes;
                }
                else
                {
                    title.text = "04 / SERVIDORES";
                    body.text = "Has llegado a la sala de servidores. Fin del recorrido disponible.";
                }
                Opcion("Volver al menú principal", 0, gm.IrAlMenu);
            }

            hud.SetObjective(zona04 != null ? "Laboratorio evacuado" : "Llegada a servidores");
            hud.SetHelp("Recorrido completado");
        }
        else if (zona04 != null)
        {
            // Zona 4: el texto lo define su controlador de escena
            hud.SetObjective(zona04.TextoObjetivo);
            // Si la mira apunta a un objeto interactivo se muestra su accion
            hud.SetHelp(interaccion != null && interaccion.TieneObjetivo ? interaccion.TextoActual : zona04.TextoAyuda);
        }
        else if (!zone)
        {
            bool laboratorio = escena == "02_Laboratorio";

            // Si el controlador de la escena mando su propio objetivo o aviso, se usa ese
            if (objetivoEscena != null)
                hud.SetObjective(objetivoEscena);
            else
                hud.SetObjective(laboratorio ? "Recoge la credencial de laboratorio" : "Encuentra el botón de salida de Recepción");

            if (avisoEscena != null)
                hud.SetHelp(avisoEscena);
            else
                hud.SetHelp(laboratorio ? "Pulsa E junto al lector para recoger la credencial y entrar en Control" : "Pulsa E junto al botón de salida para entrar al Laboratorio · ESC pausa");
        }

        if (zona04 != null)
            ActualizarEstadoZona04(gm);
        else if (!zone)
            hud.SetStatus("VIDA " + gm.vidaActual + " / " + gm.vidaMaxima);
        bool paused = gm.enPausa;
        if (paused && !wasPaused && !terminalOpen)
            Pause();
        if (!paused && wasPaused)
            Close();
        wasPaused = paused;
        int hp = GameManager.Instance ? GameManager.Instance.vidaActual : 100;
        if (hp < lastHealth)
            flash = .5f;
        lastHealth = hp;
        flash = Mathf.Max(0, flash - Time.unscaledDeltaTime);
        damage.color = new Color(.9f, .02f, .02f, flash * .65f);
    }
}
