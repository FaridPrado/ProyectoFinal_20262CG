using UnityEngine;
using UnityEngine.InputSystem;

// Objeto que el jugador recoge (tarjeta, registro, fusible...).
// Se recoge estando cerca: con E o haciendo clic mientras la mira (el centro
// de la pantalla) apunta al objeto. Esa comprobacion usa un raycast.
// Al recogerlo se agrega al inventario del GameManager (List) y se encola
// un mensaje para el HUD (Queue).
[RequireComponent(typeof(Collider))]
public class Coleccionable : MonoBehaviour
{
    [Header("Datos del objeto")]
    public string idObjeto = "tarjeta_acceso";       // lo que se guarda en el inventario y en el JSON
    public string nombreVisible = "Tarjeta de acceso";

    [Header("Recoger")]
    public float distanciaRecoger = 2.5f;
    public float distanciaRaycast = 10f;
    public AudioClip sonidoRecoger;

    [Header("Animacion")]
    public float velocidadGiro = 60f;
    public float alturaFlote = 0.08f;

    private Transform jugador;
    private Vector3 posicionInicial;
    private bool avisoMostrado;

    private void Start()
    {
        // Si ya estaba en el inventario (se cargo una partida), no debe aparecer
        if (GameManager.Instance != null && GameManager.Instance.TieneObjeto(idObjeto))
        {
            gameObject.SetActive(false);
            return;
        }

        posicionInicial = transform.position;

        GameObject objetoJugador = GameObject.FindGameObjectWithTag("Player");
        if (objetoJugador != null)
        {
            jugador = objetoJugador.transform;
        }
    }

    private void Update()
    {
        // Gira y flota para que se note que se puede recoger
        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
        float flote = Mathf.Sin(Time.time * 2f) * alturaFlote;
        transform.position = posicionInicial + new Vector3(0f, flote, 0f);

        if (jugador == null || Keyboard.current == null || Mouse.current == null)
        {
            return;
        }

        if (GameManager.Instance != null && (GameManager.Instance.enPausa || GameManager.Instance.jugadorMuerto))
        {
            return;
        }

        bool cerca = Vector3.Distance(jugador.position, transform.position) <= distanciaRecoger;

        if (cerca && !avisoMostrado)
        {
            MostrarAviso("E o clic: recoger " + nombreVisible);
            avisoMostrado = true;
        }
        else if (!cerca && avisoMostrado)
        {
            OcultarAviso();
            avisoMostrado = false;
        }

        if (!cerca)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Recoger();
        }
        else if (Mouse.current.leftButton.wasPressedThisFrame && LaMiraApuntaAlObjeto())
        {
            Recoger();
        }
    }

    // Raycast desde el centro de la pantalla. Se usa RaycastAll porque en
    // tercera persona el rayo puede tocar primero al propio jugador
    private bool LaMiraApuntaAlObjeto()
    {
        Camera camara = Camera.main;
        if (camara == null)
        {
            return false;
        }

        Ray rayo = camara.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        RaycastHit[] golpes = Physics.RaycastAll(rayo, distanciaRaycast);

        foreach (RaycastHit golpe in golpes)
        {
            if (golpe.collider.gameObject == gameObject)
            {
                return true;
            }
        }

        return false;
    }

    private void Recoger()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AgregarObjeto(idObjeto);
            GameManager.Instance.EncolarMensaje("Recogiste: " + nombreVisible);
        }

        if (sonidoRecoger != null)
        {
            // El objeto se desactiva enseguida, por eso el sonido se toca en su posicion
            AudioSource.PlayClipAtPoint(sonidoRecoger, transform.position);
        }

        OcultarAviso();
        gameObject.SetActive(false);
    }

    private void MostrarAviso(string texto)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.MostrarAviso(texto);
        }
    }

    private void OcultarAviso()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.OcultarAviso();
        }
    }
}
