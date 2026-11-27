using UnityEngine;

// Un rack de la sala de servidores. Se activa con el raycast del
// InteractionController. No decide si el reinicio es correcto: se lo pregunta
// al Zona04Controller, que compara su id con el frente de la cola de tareas.
// Muestra su estado con luz, material de pantalla, particulas y sonido.
public class RackServidor : Interactable
{
    [Header("Identidad")]
    [SerializeField] private string idRack = "A";
    [SerializeField] private string nombreRack = "Rack A";
    [SerializeField] private Zona04Controller controlador;

    [Header("Indicadores")]
    [SerializeField] private Light luzEstado;
    [SerializeField] private Renderer pantalla;
    [SerializeField] private Material materialEspera;    // ambar: falta reiniciar
    [SerializeField] private Material materialError;     // rojo: orden incorrecto
    [SerializeField] private Material materialEnLinea;   // verde: reiniciado
    [SerializeField] private ParticleSystem chispas;

    [Header("Sonidos")]
    [SerializeField] private AudioClip sonidoReinicio;
    [SerializeField] private AudioClip sonidoError;

    private static readonly Color ColorEspera = new Color(1f, 0.55f, 0.05f);
    private static readonly Color ColorError = new Color(1f, 0.05f, 0.05f);
    private static readonly Color ColorEnLinea = new Color(0.1f, 1f, 0.3f);

    private bool reiniciado;
    private float finError;
    private string textoCacheado;

    public string Id => idRack;
    public string Nombre => nombreRack;
    public bool Reiniciado => reiniciado;

    public override bool PuedeInteractuar => !reiniciado;

    public override string TextoAccion
    {
        get
        {
            if (textoCacheado == null)
            {
                textoCacheado = "Reiniciar " + nombreRack;
            }

            return textoCacheado;
        }
    }

    private void Awake()
    {
        MostrarEstado(ColorEspera, materialEspera);
    }

    private void Update()
    {
        // Pasado el destello de error vuelve a ambar (en espera)
        if (finError > 0f && Time.time >= finError)
        {
            finError = 0f;
            if (!reiniciado)
            {
                MostrarEstado(ColorEspera, materialEspera);
            }
        }
    }

    protected override void AlInteractuar()
    {
        if (controlador != null)
        {
            controlador.IntentarReiniciar(this);
        }
    }

    // conEfectos = false cuando se restaura desde el JSON (sin chispas ni sonido)
    public void MarcarReiniciado(bool conEfectos)
    {
        reiniciado = true;
        finError = 0f;
        MostrarEstado(ColorEnLinea, materialEnLinea);

        if (conEfectos)
        {
            if (chispas != null)
            {
                chispas.Play();
            }

            Sonar(sonidoReinicio);
        }
    }

    public void MarcarError()
    {
        finError = Time.time + 1.5f;
        MostrarEstado(ColorError, materialError);
        Sonar(sonidoError);
    }

    private void MostrarEstado(Color color, Material material)
    {
        if (luzEstado != null)
        {
            luzEstado.color = color;
        }

        if (pantalla != null && material != null)
        {
            pantalla.sharedMaterial = material;
        }
    }
}
