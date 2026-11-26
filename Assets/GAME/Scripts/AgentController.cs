using UnityEngine;
using UnityEngine.AI;
using ControlRoomZ03; // para reutilizar Z03CharacterAnimation (la animacion del zombi que hizo Farid)

// Agente enemigo (zombi) con una maquina de estados:
// Patrulla -> Alerta -> Persecucion -> Ataque -> Retorno
// Camina con un NavMeshAgent sobre el NavMeshSurface de la escena.
// Ve al jugador por distancia y angulo de vision, y con un Raycast
// comprueba que no haya una pared o una columna en medio.
// Cada cambio de estado cambia el color de su luz y suena un sonido.
[RequireComponent(typeof(NavMeshAgent))]
public class AgentController : MonoBehaviour
{
    public enum EstadoAgente
    {
        Patrulla,
        Alerta,
        Persecucion,
        Ataque,
        Retorno
    }

    [Header("Estado actual (solo lectura)")]
    public EstadoAgente estado = EstadoAgente.Patrulla;

    [Header("Patrulla")]
    public Transform[] puntosPatrulla;
    public float velocidadPatrulla = 1.5f;
    public float velocidadPersecucion = 3.5f;

    [Header("Vision")]
    public float distanciaVision = 12f;
    public float anguloVision = 110f;
    public float alturaOjos = 1.6f;
    public float distanciaOido = 2f;      // tan cerca lo nota aunque este a su espalda

    [Header("Ataque")]
    public float rangoAtaque = 1.6f;
    public int danio = 15;
    public float enfriamientoAtaque = 1.5f;  // segundos entre golpe y golpe

    [Header("Tiempos")]
    public float tiempoAlerta = 1.2f;   // se queda quieto mirando antes de reaccionar
    public float tiempoOlvidar = 4f;    // si deja de ver al jugador este tiempo, se devuelve

    [Header("Reacciones")]
    public Light luzEstado;
    public AudioSource fuenteAudio;
    public AudioClip sonidoAlerta;
    public AudioClip sonidoPersecucion;
    public AudioClip sonidoAtaque;
    public AudioClip sonidoRetorno;
    public Z03CharacterAnimation animacion;

    private NavMeshAgent agente;
    private Transform jugador;
    private PlayerHealth saludJugador;
    private int puntoActual;
    private Vector3 ultimaPosicionVista;
    private float tiempoEnEstado;
    private float tiempoSinVer;
    private float proximoAtaque;

    private void Start()
    {
        agente = GetComponent<NavMeshAgent>();

        saludJugador = FindFirstObjectByType<PlayerHealth>();
        if (saludJugador != null)
        {
            jugador = saludJugador.transform;
        }

        if (!agente.isOnNavMesh)
        {
            Debug.LogWarning("El zombi no esta sobre el NavMesh. Revisa que el NavMeshSurface este horneado (Bake).");
            enabled = false;
            return;
        }

        CambiarEstado(EstadoAgente.Patrulla);
    }

    private void Update()
    {
        if (jugador == null)
        {
            return;
        }

        // En pausa, antes de empezar o con el jugador muerto el zombi se queda quieto
        if (GameManager.Instance != null && (!GameManager.Instance.enPartida || GameManager.Instance.enPausa || GameManager.Instance.jugadorMuerto))
        {
            agente.isStopped = true;
            ActualizarAnimacion();
            return;
        }

        tiempoEnEstado += Time.deltaTime;

        bool loVe = PuedeVerJugador();
        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (loVe)
        {
            ultimaPosicionVista = jugador.position;
            tiempoSinVer = 0f;
        }
        else
        {
            tiempoSinVer += Time.deltaTime;
        }

        switch (estado)
        {
            case EstadoAgente.Patrulla:
                Patrullar();
                if (loVe)
                {
                    CambiarEstado(EstadoAgente.Alerta);
                }
                break;

            case EstadoAgente.Alerta:
                // Primero se detiene y voltea hacia donde vio o escucho algo
                if (tiempoEnEstado < tiempoAlerta)
                {
                    agente.isStopped = true;
                    MirarHacia(ultimaPosicionVista);
                }
                else if (loVe)
                {
                    CambiarEstado(EstadoAgente.Persecucion);
                }
                else
                {
                    // Investigacion: camina hasta el ultimo punto donde vio al jugador
                    IrA(ultimaPosicionVista, velocidadPatrulla);
                    if (Llego())
                    {
                        CambiarEstado(EstadoAgente.Retorno);
                    }
                }
                break;

            case EstadoAgente.Persecucion:
                IrA(ultimaPosicionVista, velocidadPersecucion);
                if (loVe && distancia <= rangoAtaque)
                {
                    CambiarEstado(EstadoAgente.Ataque);
                }
                else if (tiempoSinVer > tiempoOlvidar)
                {
                    CambiarEstado(EstadoAgente.Retorno);
                }
                break;

            case EstadoAgente.Ataque:
                agente.isStopped = true;
                MirarHacia(jugador.position);

                if (!loVe || distancia > rangoAtaque + 0.5f)
                {
                    CambiarEstado(EstadoAgente.Persecucion);
                }
                else if (Time.time >= proximoAtaque)
                {
                    Atacar();
                }
                break;

            case EstadoAgente.Retorno:
                // Vuelve al punto de patrulla donde iba antes de distraerse
                IrA(PuntoPatrullaActual(), velocidadPatrulla);
                if (loVe)
                {
                    CambiarEstado(EstadoAgente.Alerta);
                }
                else if (Llego())
                {
                    CambiarEstado(EstadoAgente.Patrulla);
                }
                break;
        }

        ActualizarAnimacion();
    }

    // Lo llama el Zona01Controller cuando hay un ruido fuerte (vuelve la energia):
    // el zombi va a revisar ese punto aunque no haya visto al jugador
    public void Investigar(Vector3 punto)
    {
        // Busca el punto del NavMesh mas cercano (por si el punto queda en una pared)
        NavMeshHit puntoEnPiso;
        if (NavMesh.SamplePosition(punto, out puntoEnPiso, 3f, NavMesh.AllAreas))
        {
            punto = puntoEnPiso.position;
        }

        ultimaPosicionVista = punto;

        if (estado == EstadoAgente.Patrulla || estado == EstadoAgente.Retorno)
        {
            CambiarEstado(EstadoAgente.Alerta);
        }
    }

    private void CambiarEstado(EstadoAgente nuevo)
    {
        estado = nuevo;
        tiempoEnEstado = 0f;
        agente.isStopped = false;

        // Reaccion visible: color de la luz segun el estado
        Color color = Color.cyan;
        AudioClip sonido = null;

        if (nuevo == EstadoAgente.Patrulla)
        {
            color = Color.cyan;
        }
        else if (nuevo == EstadoAgente.Alerta)
        {
            color = Color.yellow;
            sonido = sonidoAlerta;
        }
        else if (nuevo == EstadoAgente.Persecucion)
        {
            color = new Color(1f, 0.4f, 0f);
            sonido = sonidoPersecucion;
        }
        else if (nuevo == EstadoAgente.Ataque)
        {
            color = Color.red;
        }
        else if (nuevo == EstadoAgente.Retorno)
        {
            color = Color.blue;
            sonido = sonidoRetorno;
        }

        if (luzEstado != null)
        {
            luzEstado.color = color;
        }

        // Reaccion audible
        if (fuenteAudio != null && sonido != null)
        {
            fuenteAudio.PlayOneShot(sonido);
        }
    }

    private void Patrullar()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            agente.isStopped = true;
            return;
        }

        IrA(PuntoPatrullaActual(), velocidadPatrulla);

        if (Llego())
        {
            // Siguiente punto; al llegar al ultimo vuelve al primero
            puntoActual = (puntoActual + 1) % puntosPatrulla.Length;
        }
    }

    private Vector3 PuntoPatrullaActual()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
        {
            return transform.position;
        }

        return puntosPatrulla[puntoActual].position;
    }

    private void Atacar()
    {
        // Enfriamiento: el danio se aplica una vez por golpe, no en cada frame
        proximoAtaque = Time.time + enfriamientoAtaque;

        if (animacion != null)
        {
            animacion.Attack();
        }

        if (fuenteAudio != null && sonidoAtaque != null)
        {
            fuenteAudio.PlayOneShot(sonidoAtaque);
        }

        saludJugador.RecibirDanio(danio);
    }

    // Vision: distancia, angulo y Raycast para ver si hay algo en medio
    private bool PuedeVerJugador()
    {
        Vector3 ojos = transform.position + Vector3.up * alturaOjos;
        Vector3 objetivo = jugador.position + Vector3.up * 1f;
        Vector3 direccion = objetivo - ojos;
        float distancia = direccion.magnitude;

        if (distancia > distanciaVision)
        {
            return false;
        }

        Vector3 direccionPlana = direccion;
        direccionPlana.y = 0f;
        float angulo = Vector3.Angle(transform.forward, direccionPlana);

        if (angulo > anguloVision / 2f && distancia > distanciaOido)
        {
            return false;
        }

        // Si el rayo choca primero con una pared o columna, no lo ve
        RaycastHit golpe;
        if (Physics.Raycast(ojos, direccion.normalized, out golpe, distancia, ~0, QueryTriggerInteraction.Ignore))
        {
            return golpe.collider.GetComponentInParent<PlayerHealth>() != null;
        }

        return true;
    }

    private void IrA(Vector3 destino, float velocidad)
    {
        agente.isStopped = false;
        agente.speed = velocidad;
        agente.SetDestination(destino);
    }

    private bool Llego()
    {
        return !agente.pathPending && agente.remainingDistance <= agente.stoppingDistance + 0.3f;
    }

    private void MirarHacia(Vector3 punto)
    {
        Vector3 direccion = punto - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion rotacion = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacion, 240f * Time.deltaTime);
        }
    }

    private void ActualizarAnimacion()
    {
        if (animacion == null)
        {
            return;
        }

        if (agente.isStopped)
        {
            animacion.SetSpeed(0f);
        }
        else
        {
            animacion.SetSpeed(agente.velocity.magnitude);
        }
    }

    // Dibuja en el editor el rango de vision (amarillo) y de ataque (rojo)
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaVision);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
