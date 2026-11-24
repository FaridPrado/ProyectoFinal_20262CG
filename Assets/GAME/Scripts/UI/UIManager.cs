using TMPro;
using UnityEngine;

// Controla el Canvas del juego (prefab CanvasJuego, el mismo en todas las escenas).
// Por ahora maneja el HUD: objetivo, vida, aviso de interaccion y los mensajes
// del sistema. Los mensajes salen de la Queue del GameManager uno a uno
// (Dequeue), cada uno se ve unos segundos y luego sale el siguiente.
public class UIManager : MonoBehaviour
{
    // Hay un Canvas por escena (instancia del prefab), asi que no usa DontDestroyOnLoad
    public static UIManager Instance;

    [Header("Textos del HUD")]
    public TMP_Text textoObjetivo;
    public TMP_Text textoVida;
    public TMP_Text textoMensaje;
    public TMP_Text textoAviso;

    [Header("Mensajes")]
    public float segundosPorMensaje = 3f;

    private float tiempoMensaje;

    private void Awake()
    {
        Instance = this;

        if (textoMensaje != null)
        {
            textoMensaje.text = "";
        }

        OcultarAviso();
    }

    private void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (textoVida != null)
        {
            textoVida.text = "Vida: " + GameManager.Instance.vidaActual + " / " + GameManager.Instance.vidaMaxima;
        }

        // Mientras se ve un mensaje se descuenta su tiempo.
        // unscaledDeltaTime: los mensajes siguen avanzando aunque el juego este en pausa
        if (tiempoMensaje > 0f)
        {
            tiempoMensaje -= Time.unscaledDeltaTime;

            if (tiempoMensaje <= 0f && textoMensaje != null)
            {
                textoMensaje.text = "";
            }

            return;
        }

        // Este es el Dequeue con efecto visible: saca el mensaje mas antiguo de la cola
        if (GameManager.Instance.HayMensajes() && textoMensaje != null)
        {
            textoMensaje.text = GameManager.Instance.SiguienteMensaje();
            tiempoMensaje = segundosPorMensaje;
        }
    }

    public void MostrarObjetivo(string texto)
    {
        if (textoObjetivo != null)
        {
            textoObjetivo.text = texto;
        }
    }

    public void MostrarAviso(string texto)
    {
        if (textoAviso != null)
        {
            textoAviso.text = texto;
            textoAviso.gameObject.SetActive(true);
        }
    }

    public void OcultarAviso()
    {
        if (textoAviso != null)
        {
            textoAviso.gameObject.SetActive(false);
        }
    }
}
