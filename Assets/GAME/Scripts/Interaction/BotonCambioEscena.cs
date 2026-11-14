// Boton fisico de la escena: el jugador se acerca, aparece el aviso
// "Presiona E" y al presionar E se carga la escena siguiente.
//
// Uso: ponerlo en un objeto con un Collider marcado como "Is Trigger"
// (la zona donde el jugador puede usar el boton). El jugador debe tener
// el tag "Player".
//
// Igual que el resto del proyecto: sin eventos ni corrutinas. El cambio
// de escena lo hace el GameManager, para que el tiempo vuelva a correr
// y la siguiente escena guarde su punto de control al iniciar.

using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider))]
public class BotonCambioEscena : MonoBehaviour
{
    [Header("Destino")]
    public string escenaDestino = "02_Laboratorio";

    [Header("Aviso en pantalla (opcional)")]
    public GameObject avisoInteraccion;   // texto "Presiona E para continuar"

    [Header("Efecto al presionar (opcional)")]
    public Renderer luzBoton;             // se pone verde al usarlo
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
        // Evita cargar la escena dos veces si se presiona E muy rapido
        usado = true;

        if (avisoInteraccion != null)
        {
            avisoInteraccion.SetActive(false);
        }

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = true;

            if (avisoInteraccion != null && !usado)
            {
                avisoInteraccion.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            jugadorCerca = false;

            if (avisoInteraccion != null)
            {
                avisoInteraccion.SetActive(false);
            }
        }
    }
}
