using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ControlRoomZ03;

public sealed class UIManager : MonoBehaviour
{
    private static string ReadableName(string value)
    {
        string text = value.Replace("_", " ").Trim();
        return text.Length == 0 ? "Objeto" : char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
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
        hud = GetComponent<Z03HudPresenter>();
        eventos.SetActive(true);
        mira.SetActive(zone != null);
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
                title.text = "04 / SERVIDORES";
                body.text = "Has llegado a la sala de servidores. Fin del recorrido disponible.";
                Opcion("Volver al menú principal", 0, gm.IrAlMenu);
            }

            hud.SetObjective("Llegada a servidores");
            hud.SetHelp("Recorrido completado");
        }
        else if (!zone)
        {
            bool laboratorio = escena == "02_Laboratorio";
            hud.SetObjective(laboratorio ? "Recoge la credencial de laboratorio" : "Encuentra el botón de salida de Recepción");
            hud.SetHelp(laboratorio ? "Pulsa E junto al lector para recoger la credencial y entrar en Control" : "Pulsa E junto al botón de salida para entrar al Laboratorio · ESC pausa");
        }

        if (!zone)
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
