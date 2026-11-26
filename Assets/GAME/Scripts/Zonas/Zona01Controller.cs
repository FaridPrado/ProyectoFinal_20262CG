using System.Collections.Generic;
using UnityEngine;

// Controlador de la Zona 1 (Recepcion). Logica local de la escena:
// el jugador debe recoger ciertos objetos para habilitar la salida.
// Mientras falten la sala esta en emergencia (luces rojas y sirena).
// Cuando se  tenga todo se restablece la energia y se cumple el objetivo
// y se guarda un punto de control. El estado global lo guarda el GameManager.
public class Zona01Controller : MonoBehaviour
{
    [Header("Objetos que hay que recoger para salir")]
    public List<string> objetosRequeridos = new List<string> { "tarjeta_acceso", "registro_incidente", "fusible" };
    public string objetivoDeLaZona = "recepcion_completa";

    [Header("Luces")]
    public GameObject lucesNormales;
    public GameObject lucesEmergencia;

    [Header("Particulas")]
    public ParticleSystem chispasFusible;   // saltan chispas mientras no hay energia

    [Header("Sonido")]
    public AudioSource sirena;
    public AudioSource fuenteEfectos;
    public AudioClip sonidoEnergia;
    public AudioClip sonidoPuerta;

    [Header("Puerta de salida")]
    // Se abre (sube) cuando la zona esta completa
    public Transform puertaSalida;
    public float alturaPuertaAbierta = 4f;
    public float velocidadPuerta = 1.5f;

    [Header("Agente")]
    // Al volver la energia el ruido atrae al zombi hacia la salida
    public AgentController zombi;

    private bool zonaCompleta;
    private Vector3 posicionPuertaCerrada;
    private Vector3 posicionPuertaAbierta;

    private void Start()
    {
        // Pone enPartida en true (sin eso no funciona la pausa), bloquea el cursor
        // y guarda el punto de control de entrada en partida.json
        GameManager.Instance.EscenaIniciada();

        // Si se cargo una partida donde ya se habia completado, se deja la sala como quedo
        zonaCompleta = GameManager.Instance.ObjetivoCompletado(objetivoDeLaZona);
        AplicarLuces();

        if (puertaSalida != null)
        {
            posicionPuertaCerrada = puertaSalida.position;
            posicionPuertaAbierta = posicionPuertaCerrada + Vector3.up * alturaPuertaAbierta;

            // Si la partida cargada ya tenia la zona completa, la puerta empieza abierta
            if (zonaCompleta)
            {
                puertaSalida.position = posicionPuertaAbierta;
            }
        }

        if (!zonaCompleta)
        {
            GameManager.Instance.EncolarMensaje("Alerta: protocolo de contencion activo.");
            GameManager.Instance.EncolarMensaje("Busca la tarjeta de acceso, el registro y el fusible.");
        }
    }

    private void Update()
    {
        int recogidos = ContarRecogidos();

        if (!zonaCompleta && recogidos == objetosRequeridos.Count)
        {
            CompletarZona();
        }

        // Animacion de la puerta: sube poco a poco hasta quedar abierta
        if (zonaCompleta && puertaSalida != null)
        {
            puertaSalida.position = Vector3.MoveTowards(puertaSalida.position, posicionPuertaAbierta, velocidadPuerta * Time.deltaTime);
        }

        if (UIManager.Instance != null)
        {
            if (zonaCompleta)
            {
                UIManager.Instance.MostrarObjetivo("Objetivo: ve a la salida y presiona el boton");
            }
            else
            {
                UIManager.Instance.MostrarObjetivo("Objetivo: encuentra los objetos para abrir la salida (" + recogidos + "/" + objetosRequeridos.Count + ")");
            }
        }
    }

    private int ContarRecogidos()
    {
        int cantidad = 0;

        foreach (string objeto in objetosRequeridos)
        {
            if (GameManager.Instance.TieneObjeto(objeto))
            {
                cantidad++;
            }
        }

        return cantidad;
    }

    private void CompletarZona()
    {
        zonaCompleta = true;
        GameManager.Instance.CompletarObjetivo(objetivoDeLaZona);
        GameManager.Instance.EncolarMensaje("Energia restablecida: la salida esta habilitada.");

        AplicarLuces();

        if (fuenteEfectos != null && sonidoEnergia != null)
        {
            fuenteEfectos.PlayOneShot(sonidoEnergia);
        }

        if (fuenteEfectos != null && sonidoPuerta != null)
        {
            fuenteEfectos.PlayOneShot(sonidoPuerta);
        }

        // Evento -> agente: el ruido de la puerta hace que el zombi vaya a revisar
        if (zombi != null)
        {
            Vector3 punto = transform.position;
            if (puertaSalida != null)
            {
                punto = posicionPuertaCerrada;
            }

            zombi.Investigar(punto);
            GameManager.Instance.EncolarMensaje("Se abrio la puerta... algo viene hacia la salida.");
        }

        // Punto de control: si muere despues, vuelve con los objetos ya recogidos
        GameManager.Instance.GuardarPartida("recepcion_objetos");
    }

    // Cambio de estado de la iluminacion: emergencia mientras falten objetos,
    // luces normales cuando la zona esta completa
    private void AplicarLuces()
    {
        if (lucesNormales != null)
        {
            lucesNormales.SetActive(zonaCompleta);
        }

        if (lucesEmergencia != null)
        {
            lucesEmergencia.SetActive(!zonaCompleta);
        }

        if (chispasFusible != null)
        {
            if (zonaCompleta)
            {
                chispasFusible.Stop();
            }
            else if (!chispasFusible.isPlaying)
            {
                chispasFusible.Play();
            }
        }

        if (sirena != null)
        {
            if (zonaCompleta)
            {
                sirena.Stop();
            }
            else if (!sirena.isPlaying)
            {
                sirena.Play();
            }
        }
    }
}
