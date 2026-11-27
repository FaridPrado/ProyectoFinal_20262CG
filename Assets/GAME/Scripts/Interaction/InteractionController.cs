using UnityEngine;
using UnityEngine.InputSystem;

// Va en el jugador. Cada frame lanza un raycast desde el centro de la pantalla
// (donde esta la mira) y, si choca con un Interactable que esta cerca del
// jugador, lo guarda como objetivo. Con E o clic izquierdo lo activa.
// El HUD lee TextoActual para mostrar "E / Clic · <accion>".
public class InteractionController : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private float distanciaRayo = 8f;     // desde la camara (tercera persona)
    [SerializeField] private float alcanceJugador = 2.8f;  // distancia maxima jugador -> objeto

    private Camera camara;
    private Interactable actual;
    private string textoActual;

    // Buffer reutilizado: RaycastNonAlloc no crea basura (GC) en cada frame
    private readonly RaycastHit[] golpes = new RaycastHit[8];

    public Interactable Actual => actual;
    public bool TieneObjetivo => actual != null;
    public string TextoActual => textoActual;

    private void Start()
    {
        camara = Camera.main;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (camara == null || (gm != null && (gm.enPausa || gm.jugadorMuerto || !gm.enPartida)))
        {
            CambiarObjetivo(null);
            return;
        }

        CambiarObjetivo(BuscarObjetivo());

        if (actual == null)
        {
            return;
        }

        bool pulsoE = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool hizoClic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        if (pulsoE || hizoClic)
        {
            actual.Activar();
            // Despues de activarlo puede cambiar su texto (o dejar de estar disponible)
            textoActual = null;
            CambiarObjetivo(actual.PuedeInteractuar ? actual : null, true);
        }
    }

    private Interactable BuscarObjetivo()
    {
        Ray rayo = camara.ScreenPointToRay(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f));
        int cantidad = Physics.RaycastNonAlloc(rayo, golpes, distanciaRayo, ~0, QueryTriggerInteraction.Ignore);

        // El golpe mas cercano que no sea el propio jugador
        float masCerca = float.MaxValue;
        Collider elegido = null;
        for (int i = 0; i < cantidad; i++)
        {
            Collider c = golpes[i].collider;
            if (c.transform.IsChildOf(transform))
            {
                continue;
            }

            if (golpes[i].distance < masCerca)
            {
                masCerca = golpes[i].distance;
                elegido = c;
            }
        }

        if (elegido == null)
        {
            return null;
        }

        Interactable objeto = elegido.GetComponentInParent<Interactable>();
        if (objeto == null || !objeto.PuedeInteractuar)
        {
            return null;
        }

        // Tiene que estar al alcance del jugador, no solo de la camara
        Vector3 punto = elegido.ClosestPoint(transform.position + Vector3.up);
        if (Vector3.Distance(transform.position + Vector3.up, punto) > alcanceJugador)
        {
            return null;
        }

        return objeto;
    }

    // El texto se arma solo cuando cambia el objetivo (no en cada frame)
    private void CambiarObjetivo(Interactable nuevo, bool forzar = false)
    {
        if (nuevo == actual && !forzar)
        {
            return;
        }

        actual = nuevo;
        textoActual = actual != null ? "E / Clic  ·  " + actual.TextoAccion : null;
    }
}
