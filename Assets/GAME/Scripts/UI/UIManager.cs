using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ControlRoomZ03;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public sealed class UIManager : MonoBehaviour
{
    private static string ReadableName(string value)
    {
        string text = value.Replace("_", " ").Trim();
        return text.Length == 0 ? "Objeto" : char.ToUpperInvariant(text[0]) + text.Substring(1).ToLowerInvariant();
    }

    private Canvas canvas;
    private GameObject panel;
    private Text title;
    private Text body;
    private Image damage;
    private RawImage feed;
    private Camera security;
    private RenderTexture video;
    private readonly List<GameObject> buttons = new List<GameObject>();
    private readonly Stack<string> pages = new Stack<string>();
    private Z03ZoneGameplay zone;
    private Z03HudPresenter hud;
    private string escena;
    private Font font;
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
        if (!FindFirstObjectByType<EventSystem>())
        {
            var eventos = new GameObject("Eventos de la interfaz", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventos.transform.SetParent(transform, false);
        }

        canvas = hud.GetComponent<Canvas>();
        if (!canvas)
            return;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (zone)
        {
            Text cross = Label("Mira", canvas.transform, "+", 24);
            Rect(cross.gameObject, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-12, -12), new Vector2(12, 12));
            cross.alignment = TextAnchor.MiddleCenter;
            cross.raycastTarget = false;
        }

        damage = Image("Efecto de daño", canvas.transform, new Color(1, 0, 0, 0));
        Rect(damage.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        damage.raycastTarget = false;
        var background = Image("Panel de pausa y terminal", canvas.transform, new Color(.025f, .05f, .08f, .98f));
        panel = background.gameObject;
        Rect(panel, new Vector2(.15f, .12f), new Vector2(.85f, .88f), Vector2.zero, Vector2.zero);
        title = Label("Título", panel.transform, "", 27);
        Rect(title.gameObject, new Vector2(.06f, .8f), new Vector2(.94f, .96f), Vector2.zero, Vector2.zero);
        body = Label("Contenido", panel.transform, "", 19);
        Rect(body.gameObject, new Vector2(.06f, .42f), new Vector2(.94f, .8f), Vector2.zero, Vector2.zero);
        panel.SetActive(false);
        if (zone)
        {
            var raw = new GameObject("Monitor de seguridad", typeof(RectTransform), typeof(RawImage));
            raw.transform.SetParent(panel.transform, false);
            feed = raw.GetComponent<RawImage>();
            feed.raycastTarget = false;
            Rect(raw, new Vector2(.52f, .42f), new Vector2(.94f, .8f), Vector2.zero, Vector2.zero);
            video = new RenderTexture(640, 360, 16);
            video.Create();
            var cg = new GameObject("Cámara de seguridad");
            cg.transform.SetParent(zone.transform);
            cg.transform.position = new Vector3(0, 7, 24);
            cg.transform.LookAt(new Vector3(0, 0, -4));
            security = cg.AddComponent<Camera>();
            security.targetTexture = video;
            security.fieldOfView = 75;
            security.enabled = false;
            feed.texture = video;
            Sign("VIGILANCIA", new Vector3(-15, 2.8f, 12));
            Sign("ENERGÍA", new Vector3(15, 2.8f, 12));
            Sign("AUTORIZACIÓN", new Vector3(0, 2.9f, 18));
            Sign("04 / SERVIDORES", new Vector3(0, 4.5f, 25.5f));
            Sign("03 / SALA DE CONTROL", new Vector3(0, 3.8f, -12));
        }

        if (GameManager.Instance)
            lastHealth = GameManager.Instance.vidaActual;
    }

    void Sign(string text, Vector3 position)
    {
        var go = new GameObject(ReadableName("Señal " + text));
        go.transform.SetParent(zone.transform);
        go.transform.position = position;
        var mesh = go.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.font = font;
        mesh.fontSize = 64;
        mesh.characterSize = .07f;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.color = new Color(.35f, .9f, 1);
        go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
    }

    static void Rect(GameObject go, Vector2 min, Vector2 max, Vector2 lo, Vector2 hi)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = min;
        r.anchorMax = max;
        r.offsetMin = lo;
        r.offsetMax = hi;
    }

    Image Image(string name, Transform parent, Color color)
    {
        var go = new GameObject(ReadableName(name), typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var i = go.GetComponent<Image>();
        i.color = color;
        return i;
    }

    Text Label(string name, Transform parent, string value, int size)
    {
        var go = new GameObject(ReadableName(name), typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.font = font;
        t.text = value;
        t.fontSize = size;
        t.color = new Color(.8f, .93f, 1);
        t.raycastTarget = false;
        return t;
    }

    void Button(string label, int row, UnityEngine.Events.UnityAction action)
    {
        var img = Image("Botón_" + label, panel.transform, new Color(.05f, .2f, .27f));
        Rect(img.gameObject, new Vector2(.06f, .31f - row * .085f), new Vector2(.94f, .38f - row * .085f), Vector2.zero, Vector2.zero);
        var b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(action);
        var text = Label("Texto", img.transform, label, 18);
        Rect(text.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        text.alignment = TextAnchor.MiddleCenter;
        buttons.Add(img.gameObject);
    }

    void Clear()
    {
        foreach (var b in buttons)
        {
            b.SetActive(false);
            Destroy(b);
        }

        buttons.Clear();
        if (feed)
            feed.gameObject.SetActive(false);
        if (security)
            security.enabled = false;
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
            body.text = "Restablece vigilancia y energía.\nLa autorización requiere la credencial del laboratorio.\nConsulta el diagnóstico y las cámaras antes de abrir la salida.";
            Button("Diagnóstico y cámara de seguridad", 0, () =>
            {
                pages.Push("Diagnóstico");
                RenderTerminal();
            });
            Button("Autorizar acceso a Servidores", 1, () =>
            {
                Close();
                zone.Interactuar(Z03Action.Terminal);
            });
            Button("Cerrar terminal", 2, Close);
        }
        else
        {
            body.text = zone.Diagnostico();
            feed.gameObject.SetActive(true);
            security.enabled = true;
            Button("Atrás", 0, () =>
            {
                if (pages.Count > 1)
                    pages.Pop();
                RenderTerminal();
            });
            Button("Cerrar", 1, Close);
        }
    }

    void Pause()
    {
        Clear();
        panel.SetActive(true);
        title.text = "PARTIDA EN PAUSA";
        body.text = "WASD · caminar    Shift · correr    Ctrl · agacharse\nRatón · mirar    E · interactuar    Esc · continuar";
        Button("Continuar", 0, Close);
        Button("Cargar último checkpoint", 1, GameManager.Instance.ReiniciarDesdeGuardado);
        Button("Guardar y volver al menú", 2, ReturnMenu);
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
        if (gm.gano)
        {
            if (!panel.activeSelf)
            {
                Clear();
                panel.SetActive(true);
                title.text = "04 / SERVIDORES";
                body.text = "Has llegado a la sala de servidores. Fin del recorrido disponible.";
                Button("Volver al menú principal", 0, gm.IrAlMenu);
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

    void OnDestroy()
    {
        if (video)
        {
            if (security)
                security.targetTexture = null;
            if (feed)
                feed.texture = null;
            video.Release();
            Destroy(video);
        }
    }
}
