using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class BotonCambioEscena : MonoBehaviour
{
    [Header("Destino")]
    public string escenaDestino = "02_Laboratorio";

    [Header("Requisito (opcional)")]
    // Si no esta vacio, el boton solo funciona cuando ese objetivo ya se cumplio.
    // En Recepcion es "recepcion_completa": haber recogido todos los objetos
    public string objetivoRequerido = "";
    public AudioSource fuenteAudio;
    public AudioClip sonidoBloqueado;
    public AudioClip sonidoActivado;

    [Header("Aviso en pantalla (opcional)")]
    public GameObject avisoInteraccion;   // texto "Presiona E para continuar"

    [Header("Efecto al presionar (opcional)")]
    public Renderer luzBoton;             // se pone verde al usarnlo
    public Color colorActivado = Color.green;

    private bool jugadorCerca;
    private bool usado;

    private void Start()
    {
        // El collider tiene que ser trigger para que el jugador lo atraviese
        GetComponent<Collider>().isTrigger = true;

        if (avisoInteraccion != null)
        {
            avisoInteraccion.SetActive(false);
        }
    }

    private void Update()
    {
        if (!jugadorCerca || usado || Keyboard.current == null)
        {
            return;
        }

        // No se puede usar mientras el juego esta en pausa o el jugador murio
        if (GameManager.Instance != null && (GameManager.Instance.enPausa || GameManager.Instance.jugadorMuerto))
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Presionar();
        }
    }

    private void Presionar()
    {
        // Salida bloqueada: avisa, suena y no cambia de escena
        if (!RequisitoCumplido())
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EncolarMensaje("Salida bloqueada: aun te faltan objetos.");
            }

            if (fuenteAudio != null && sonidoBloqueado != null)
            {
                fuenteAudio.PlayOneShot(sonidoBloqueado);
            }

            return;
        }

        // Evita cargar la escena dos veces si se presiona E muy rapido
        usado = true;

        if (fuenteAudio != null && sonidoActivado != null)
        {
            fuenteAudio.PlayOneShot(sonidoActivado);
        }

        MostrarAviso(false);

        if (luzBoton != null)
        {
            luzBoton.material.color = colorActivado;
        }

        if (!Application.CanStreamedLevelBeLoaded(escenaDestino))
        {
            Debug.LogWarning("La escena " + escenaDestino + " no esta en Build Profiles > Scene List.");
            usado = false;
            return;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.CompletarObjetivo("salir_" + gameObject.scene.name);
            GameManager.Instance.CambiarEscena(escenaDestino);
        }
        else
        {
            // Por si se prueba la escena sin GameManager
            UnityEngine.SceneManagement.SceneManager.LoadScene(escenaDestino);
        }
    }

    private bool RequisitoCumplido()
    {
        if (string.IsNullOrEmpty(objetivoRequerido) || GameManager.Instance == null)
        {
            return true;
        }

        return GameManager.Instance.ObjetivoCompletado(objetivoRequerido);
    }

    // Usa el texto propio si se le asigno uno; si no, el aviso del HUD (UIManager)
    private void MostrarAviso(bool mostrar)
    {
        if (avisoInteraccion != null)
        {
            avisoInteraccion.SetActive(mostrar);
        }
        else if (UIManager.Instance != null)
        {
            if (mostrar)
            {
                UIManager.Instance.MostrarAviso("E: usar el boton de salida");
            }
            else
            {
                UIManager.Instance.OcultarAviso();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;

            if (!usado)
            {
                MostrarAviso(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;
            MostrarAviso(false);
        }
    }
}
