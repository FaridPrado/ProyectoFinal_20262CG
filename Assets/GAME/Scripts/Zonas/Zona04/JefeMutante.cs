using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Jefe de la Zona 4: el especimen biomecanico del tunel. Trabaja encima de su
// AgentController (que sigue haciendo Patrulla/Alerta/Persecucion/Ataque/Retorno)
// y le agrega comportamiento de jefe:
//
//  - Fase 1: casi ciego, oido muy fino (lo configura el AgentController).
//  - Embestida: en Persecucion, si el jugador esta a media distancia, carga a
//    gran velocidad unos segundos; despues queda agotado (ventana para huir o disparar).
//  - Fase 2 (Furia): al bajar del 50% de vida o al restablecerse la energia ruge,
//    se vuelve mas rapido y pega mas fuerte, e invoca infectados de un pool
//    (objetos ya creados e inactivos: no se instancia nada en mitad de la partida).
//  - Muerte: marca el objetivo "especimen_abatido" y guarda en el JSON.
//
// La vida la maneja Zombie (IDamageable); este script escucha sus eventos.
[RequireComponent(typeof(AgentController))]
public class JefeMutante : MonoBehaviour
{
    private const string OBJETIVO_ABATIDO = "especimen_abatido";
    private static readonly int ParamRugir = Animator.StringToHash("rugir");

    [Header("Fase 2: Furia")]
    [SerializeField] private float umbralFuria = 0.5f;          // fraccion de vida
    [SerializeField] private float multiplicadorVelocidadFuria = 1.35f;
    [SerializeField] private int danioFuria = 30;

    [Header("Embestida")]
    [SerializeField] private float distanciaMinEmbestida = 4f;
    [SerializeField] private float distanciaMaxEmbestida = 11f;
    [SerializeField] private float duracionEmbestida = 1.6f;
    [SerializeField] private float multiplicadorEmbestida = 2.2f;
    [SerializeField] private float enfriamientoEmbestida = 8f;
    [SerializeField] private float tiempoAgotado = 1.5f;         // queda lento despues de cargar
    [SerializeField] private float velocidadAgotado = 0.4f;

    [Header("Invocacion (pool de infectados inactivos)")]
    [SerializeField] private List<AgentController> invocables = new List<AgentController>();
    [SerializeField] private Transform[] puntosInvocacion;

    [Header("Reacciones")]
    [SerializeField] private AudioClip sonidoFuria;
    [SerializeField] private AudioClip sonidoEmbestida;
    [SerializeField] private float intensidadLuzFuria = 10f;

    private AgentController agente;
    private Zombie vida;
    private Animator animador;
    private AudioSource fuenteAudio;
    private Transform jugador;

    private float velocidadBase;
    private bool enFuria;
    private float finEmbestida;
    private float finAgotado;
    private float proximaEmbestida;
    private bool invocadosPendientes;     // se ordenan un frame despues de activarlos
    private bool detectado;               // el jugador ya lo vio en accion (para el HUD)

    public bool EnFuria => enFuria;
    public bool Vivo => vida != null && !vida.IsDead;
    public float VidaNormalizada => vida != null ? vida.CurrentHealth / vida.MaxHealth : 0f;

    // El HUD muestra la vida del jefe solo cuando ya entro en combate
    public bool MostrarEnHUD => Vivo && (detectado || enFuria);

    private void Awake()
    {
        agente = GetComponent<AgentController>();
        vida = GetComponent<Zombie>();
        animador = GetComponent<Animator>();
        fuenteAudio = GetComponent<AudioSource>();

        if (vida != null)
        {
            vida.OnHealthChanged += AlCambiarVida;
            vida.OnDied += AlMorir;
        }
    }

    private void OnDestroy()
    {
        if (vida != null)
        {
            vida.OnHealthChanged -= AlCambiarVida;
            vida.OnDied -= AlMorir;
        }
    }

    private void Start()
    {
        // Si ya se abatio en esta partida (viene del JSON), no vuelve a aparecer
        if (GameManager.Instance != null && GameManager.Instance.ObjetivoCompletado(OBJETIVO_ABATIDO))
        {
            gameObject.SetActive(false);
            return;
        }

        PlayerHealth salud = FindFirstObjectByType<PlayerHealth>();
        if (salud != null)
        {
            jugador = salud.transform;
        }

        velocidadBase = agente.velocidadPersecucion;
    }

    private void Update()
    {
        if (!Vivo || jugador == null)
        {
            return;
        }

        GameManager gm = GameManager.Instance;
        if (gm != null && (gm.enPausa || gm.jugadorMuerto))
        {
            return;
        }

        if (invocadosPendientes)
        {
            invocadosPendientes = false;
            foreach (AgentController infectado in invocables)
            {
                if (infectado != null && infectado.isActiveAndEnabled)
                {
                    infectado.Investigar(jugador.position);
                }
            }
        }

        if (agente.estado == AgentController.EstadoAgente.Persecucion || agente.estado == AgentController.EstadoAgente.Ataque)
        {
            detectado = true;
        }

        ActualizarEmbestida();
    }

    // ---------- Embestida ----------

    private void ActualizarEmbestida()
    {
        // Fin de la carga: queda agotado (lento) unos segundos
        if (finEmbestida > 0f && Time.time >= finEmbestida)
        {
            finEmbestida = 0f;
            finAgotado = Time.time + tiempoAgotado;
            agente.velocidadPersecucion = velocidadAgotado;
        }

        // Se recupera
        if (finAgotado > 0f && Time.time >= finAgotado)
        {
            finAgotado = 0f;
            agente.velocidadPersecucion = VelocidadNormal();
        }

        bool libre = finEmbestida == 0f && finAgotado == 0f;
        if (!libre || agente.estado != AgentController.EstadoAgente.Persecucion || Time.time < proximaEmbestida)
        {
            return;
        }

        float distancia = Vector3.Distance(transform.position, jugador.position);
        if (distancia >= distanciaMinEmbestida && distancia <= distanciaMaxEmbestida)
        {
            finEmbestida = Time.time + duracionEmbestida;
            proximaEmbestida = Time.time + enfriamientoEmbestida;
            agente.velocidadPersecucion = VelocidadNormal() * multiplicadorEmbestida;
            Sonar(sonidoEmbestida);
        }
    }

    private float VelocidadNormal()
    {
        return enFuria ? velocidadBase * multiplicadorVelocidadFuria : velocidadBase;
    }

    // ---------- Fase 2: Furia ----------

    private void AlCambiarVida(float actual, float maxima)
    {
        if (!enFuria && actual > 0f && actual / maxima <= umbralFuria)
        {
            ActivarFuria("Has herido al espécimen: entra en furia.");
        }
    }

    // Tambien la llama el Zona04Controller cuando vuelve la energia
    public void ActivarFuria(string mensaje)
    {
        if (enFuria || !Vivo || !isActiveAndEnabled)
        {
            return;
        }

        enFuria = true;
        detectado = true;
        agente.danio = danioFuria;
        if (finEmbestida == 0f && finAgotado == 0f)
        {
            agente.velocidadPersecucion = VelocidadNormal();
        }

        if (agente.luzEstado != null)
        {
            agente.luzEstado.intensity = intensidadLuzFuria;
            agente.luzEstado.range *= 1.5f;
        }

        if (animador != null)
        {
            animador.SetTrigger(ParamRugir);
        }

        Sonar(sonidoFuria);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.EncolarMensaje(mensaje);
        }

        Invocar();

        if (jugador != null)
        {
            agente.Investigar(jugador.position);
        }
    }

    // Activa los infectados del pool junto a los puntos de invocacion
    private void Invocar()
    {
        int activados = 0;
        for (int i = 0; i < invocables.Count; i++)
        {
            AgentController infectado = invocables[i];
            if (infectado == null || infectado.gameObject.activeSelf)
            {
                continue;
            }

            if (puntosInvocacion != null && puntosInvocacion.Length > 0)
            {
                Transform punto = puntosInvocacion[i % puntosInvocacion.Length];
                infectado.transform.SetPositionAndRotation(punto.position, punto.rotation);
            }

            infectado.gameObject.SetActive(true);
            activados++;
        }

        if (activados > 0)
        {
            invocadosPendientes = true;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.EncolarMensaje("El rugido despierta a " + activados + " infectados en el túnel.");
            }
        }
    }

    // ---------- Muerte ----------

    private void AlMorir()
    {
        if (agente.luzEstado != null)
        {
            agente.luzEstado.enabled = false;
        }

        agente.enabled = false;

        GameManager gm = GameManager.Instance;
        if (gm != null)
        {
            gm.CompletarObjetivo(OBJETIVO_ABATIDO);
            gm.EncolarMensaje("Espécimen abatido.");
            gm.GuardarPartida("especimen_abatido");
        }
    }

    private void Sonar(AudioClip clip)
    {
        if (fuenteAudio != null && clip != null)
        {
            fuenteAudio.PlayOneShot(clip);
        }
    }
}
