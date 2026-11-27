using System.Collections.Generic;
using UnityEngine;

// Controlador de la Zona 4 (Sala de servidores). Logica local de la escena:
//
//  1. Progresion: solo se puede operar con el objeto "acceso_servidores"
//     que da la Zona 3 (Sala de control). Lo consulta en el inventario (List).
//  2. Mecanismo: hay que reiniciar los racks en un orden. El orden vive en una
//     Queue del GameManager: el rack correcto es el del frente (Peek) y al
//     reiniciarlo sale con Dequeue. El estado de cada rack se guarda en el
//     Dictionary de puertas/mecanismos del GameManager ("rack_X" -> true).
//  3. Terminal: muestra el orden y la camara de seguridad. Su navegacion usa la
//     Stack de paginas del UIManager (Atras = Pop) y la Stack de accesos del
//     GameManager (abrir = Push, cerrar sesion = Pop).
//  4. Eventos: un rack equivocado dispara la alarma y los agentes van a
//     investigar ese punto. Al terminar la cola vuelve la energia, cambian las
//     luces, se abre la compuerta y se guarda en el JSON.
//  5. Victoria: cruzar la compuerta de evacuacion con la zona completa.
//
// No guarda copia del estado global: todo lo consulta y notifica al GameManager.
public class Zona04Controller : MonoBehaviour
{
    private const string OBJETIVO_ZONA = "servidores_completa";
    private const string OBJETIVO_TERMINAL = "z04_orden_consultado";
    private const string PUERTA_SALIDA = "compuerta_evacuacion";
    private const string ACCESO_REQUERIDO = "acceso_servidores";
    private const string ID_TERMINAL = "terminal_servidores";

    // Paginas de la terminal (el UIManager usa estos mismos nombres)
    public const string PAGINA_INICIO = "Inicio";
    public const string PAGINA_ORDEN = "Orden de reinicio";
    public const string PAGINA_CAMARA = "Cámara del túnel";

    [Header("Mecanismo: racks y orden de reinicio")]
    [SerializeField] private List<RackServidor> racks = new List<RackServidor>();
    [SerializeField] private string[] ordenReinicio = { "C", "D", "A", "B" };

    [Header("Progresion")]
    // Solo en el Editor: permite dar Play directo en esta escena sin pasar
    // por la Zona 3. En el ejecutable el acceso tiene que venir de la Zona 3
    [SerializeField] private bool darAccesoAlProbarEnEditor = true;

    [Header("Agentes que responden a la alarma")]
    [SerializeField] private List<AgentController> agentes = new List<AgentController>();
    [SerializeField] private JefeMutante jefe;

    [Header("Señuelo acústico (botón de acción de la terminal)")]
    [SerializeField] private AudioSource altavozSenuelo;
    [SerializeField] private AudioClip sonidoSenuelo;
    [SerializeField] private float enfriamientoSenuelo = 45f;

    [Header("Iluminacion (cambio de estado)")]
    [SerializeField] private GameObject lucesEmergencia;
    [SerializeField] private GameObject lucesNormales;

    [Header("Sonido")]
    [SerializeField] private AudioSource sirena;
    [SerializeField] private AudioSource efectos;
    [SerializeField] private AudioClip sonidoAlarma;
    [SerializeField] private AudioClip sonidoEnergia;
    [SerializeField] private AudioClip sonidoAccesoDenegado;

    [Header("Salida")]
    [SerializeField] private CompuertaSalida compuerta;

    [Header("Textos del HUD (los lee el UIManager)")]
    [SerializeField] private string textoObjetivo = "Explora la sala de servidores";
    [SerializeField] private string textoAyuda = "E / Clic interactuar  ·  Ctrl agacharse  ·  Esc pausa";

    // Dictionary local: id del rack -> rack, para encontrar cada uno al
    // restaurar el estado guardado sin recorrer la lista
    private readonly Dictionary<string, RackServidor> racksPorId = new Dictionary<string, RackServidor>();

    private bool tieneAcceso;
    private bool zonaCompleta;
    private float proximoAvisoSalida;
    private float proximoSenuelo;

    public JefeMutante Jefe => jefe;
    public bool SenueloDisponible => Time.time >= proximoSenuelo;
    public int SegundosParaSenuelo => Mathf.CeilToInt(Mathf.Max(0f, proximoSenuelo - Time.time));
    public string TextoObjetivo => textoObjetivo;
    public string TextoAyuda => textoAyuda;
    public bool ZonaCompleta => zonaCompleta;
    public int TotalRacks => ordenReinicio.Length;

    private void Start()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogError("Zona04Controller: no hay GameManager en la escena. Entra desde el menu.");
            return;
        }

        // enPartida = true, cursor bloqueado y punto de control de entrada en el JSON
        gm.EscenaIniciada();

        foreach (RackServidor rack in racks)
        {
            racksPorId[rack.Id] = rack;
            gm.RegistrarPuerta(CodigoRack(rack.Id));
        }
        gm.RegistrarPuerta(PUERTA_SALIDA);

        tieneAcceso = gm.TieneObjeto(ACCESO_REQUERIDO);
#if UNITY_EDITOR
        if (!tieneAcceso && darAccesoAlProbarEnEditor)
        {
            gm.AgregarObjeto(ACCESO_REQUERIDO);
            tieneAcceso = true;
            Debug.Log("Zona 4: acceso_servidores agregado solo para probar en el Editor.");
        }
#endif

        zonaCompleta = gm.ObjetivoCompletado(OBJETIVO_ZONA);

        // La cola se llena solo la primera vez; si viene del JSON se respeta
        if (!zonaCompleta)
        {
            gm.IniciarTareas(ordenReinicio);
        }

        // Restaurar los racks que ya estaban reiniciados en el guardado
        foreach (string id in ordenReinicio)
        {
            RackServidor rack;
            if (racksPorId.TryGetValue(id, out rack) && gm.PuertaDesbloqueada(CodigoRack(id)))
            {
                rack.MarcarReiniciado(false);
            }
        }

        AplicarEstadoZona(true);
        ActualizarObjetivo();

        if (!tieneAcceso)
        {
            gm.EncolarMensaje("ACCESO DENEGADO: falta la autorización de la Sala de control.");
        }
        else if (!zonaCompleta)
        {
            gm.EncolarMensaje("Sala de servidores: energía inestable.");
            gm.EncolarMensaje("Busca la terminal para ver el orden de reinicio.");
        }
    }

    // ---------- Terminal ----------

    public void AbrirTerminal()
    {
        GameManager gm = GameManager.Instance;

        if (!tieneAcceso)
        {
            Sonar(sonidoAccesoDenegado);
            gm.EncolarMensaje("ACCESO DENEGADO: necesitas el acceso de la Sala de control.");
            return;
        }

        // Stack de accesos: Push al iniciar sesion en la terminal
        gm.RegistrarAcceso(ID_TERMINAL);

        if (!gm.ObjetivoCompletado(OBJETIVO_TERMINAL))
        {
            gm.CompletarObjetivo(OBJETIVO_TERMINAL);
            ActualizarObjetivo();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OpenTerminal();
        }
    }

    // Boton de accion de la terminal: hace sonar el altavoz de la sala oeste.
    // Todos los agentes (incluido el jefe) van a investigar ese punto, lo que
    // despeja el camino. Tiene enfriamiento para que no se pueda abusar.
    public void ActivarSenuelo()
    {
        GameManager gm = GameManager.Instance;

        if (!SenueloDisponible)
        {
            gm.EncolarMensaje("Señuelo recargando: " + SegundosParaSenuelo + " s.");
            return;
        }

        if (altavozSenuelo == null)
        {
            return;
        }

        proximoSenuelo = Time.time + enfriamientoSenuelo;
        if (sonidoSenuelo != null)
        {
            altavozSenuelo.PlayOneShot(sonidoSenuelo);
        }

        Vector3 punto = altavozSenuelo.transform.position;
        foreach (AgentController agente in agentes)
        {
            if (agente != null && agente.isActiveAndEnabled)
            {
                agente.Investigar(punto);
            }
        }

        gm.RegistrarAcceso("senuelo_acustico");
        gm.EncolarMensaje("Señuelo activado: los infectados van hacia la sala oeste.");
    }

    // "Cerrar sesion" en la terminal: Pop de la pila de accesos con efecto visible
    public void CerrarSesion()
    {
        GameManager gm = GameManager.Instance;
        string acceso = gm.DeshacerUltimoAcceso();
        gm.EncolarMensaje(acceso.Length > 0 ? "Sesión cerrada: " + acceso : "No había sesiones abiertas.");
    }

    // Texto de cada pagina de la terminal (el UIManager lo dibuja)
    public string TextoTerminal(string pagina)
    {
        GameManager gm = GameManager.Instance;
        int enLinea = ordenReinicio.Length - gm.CantidadTareas();
        if (zonaCompleta)
        {
            enLinea = ordenReinicio.Length;
        }

        if (pagina == PAGINA_ORDEN)
        {
            var texto = new System.Text.StringBuilder();
            texto.Append("Secuencia obligatoria de reinicio:\n");
            for (int i = 0; i < ordenReinicio.Length; i++)
            {
                bool hecho = gm.PuertaDesbloqueada(CodigoRack(ordenReinicio[i]));
                texto.Append(i + 1).Append(". RACK ").Append(ordenReinicio[i]).Append(hecho ? "   [EN LÍNEA]\n" : "   [FUERA DE LÍNEA]\n");
            }

            texto.Append(zonaCompleta ? "\nTodos los servidores en línea." : "\nSiguiente en la cola: RACK " + gm.SiguienteTarea());
            texto.Append("\nUn rack fuera de orden activa la alarma.");
            return texto.ToString();
        }

        if (pagina == PAGINA_CAMARA)
        {
            return "Cámara de seguridad del túnel inferior.\nSe detectó movimiento de un espécimen.\nNo hagas ruido cerca: responde al sonido.";
        }

        return "NEXUS / SALA DE SERVIDORES\n" +
               "Energía: " + (zonaCompleta ? "RESTABLECIDA" : "INESTABLE") + "\n" +
               "Racks en línea: " + enLinea + " / " + ordenReinicio.Length + "\n" +
               "Sesión activa: " + (gm.UltimoAcceso().Length > 0 ? gm.UltimoAcceso() : "ninguna") +
               "   (accesos en la pila: " + gm.CantidadAccesos() + ")\n" +
               "Señuelo acústico (altavoz sala oeste): " + (SenueloDisponible ? "LISTO" : "recargando " + SegundosParaSenuelo + " s");
    }

    // ---------- Mecanismo de racks ----------

    public void IntentarReiniciar(RackServidor rack)
    {
        GameManager gm = GameManager.Instance;

        if (!tieneAcceso)
        {
            rack.MarcarError();
            gm.EncolarMensaje("ACCESO DENEGADO: el rack no acepta comandos sin autorización.");
            return;
        }

        if (rack.Id == gm.SiguienteTarea())
        {
            // Correcto: sale de la cola (Dequeue) y queda guardado en el Dictionary
            gm.CompletarTarea();
            gm.DesbloquearPuerta(CodigoRack(rack.Id));
            rack.MarcarReiniciado(true);

            if (gm.CantidadTareas() == 0)
            {
                CompletarZona();
            }
            else
            {
                gm.EncolarMensaje(rack.Nombre + " en línea (" + (ordenReinicio.Length - gm.CantidadTareas()) + "/" + ordenReinicio.Length + ").");
                gm.GuardarPartida("servidor_" + rack.Id);
            }
        }
        else
        {
            // Incorrecto: alarma, luces en alerta y los agentes van a ese punto
            rack.MarcarError();
            Sonar(sonidoAlarma);
            AlarmaLuces(4f);
            foreach (AgentController agente in agentes)
            {
                if (agente != null && agente.isActiveAndEnabled)
                {
                    agente.Investigar(rack.transform.position);
                }
            }

            gm.EncolarMensaje("Secuencia incorrecta: alarma activada. Algo viene hacia " + rack.Nombre + ".");
        }

        ActualizarObjetivo();
    }

    private void CompletarZona()
    {
        GameManager gm = GameManager.Instance;
        zonaCompleta = true;
        gm.CompletarObjetivo(OBJETIVO_ZONA);
        gm.DesbloquearPuerta(PUERTA_SALIDA);

        AplicarEstadoZona(false);
        gm.EncolarMensaje("Servidores en línea: energía restablecida.");

        // Evento -> jefe: la subida de energia despierta al especimen en furia
        if (jefe != null && jefe.Vivo)
        {
            jefe.ActivarFuria("La energía despierta al espécimen: entra en furia.");
        }
        gm.EncolarMensaje("Compuerta de evacuación abierta en el túnel norte.");
        gm.GuardarPartida("servidores_restablecidos");
    }

    // Cambio de estado de iluminacion, sirena y compuerta
    private void AplicarEstadoZona(bool instantaneo)
    {
        if (lucesEmergencia != null)
        {
            lucesEmergencia.SetActive(!zonaCompleta);
        }

        if (lucesNormales != null)
        {
            lucesNormales.SetActive(zonaCompleta);
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

        if (zonaCompleta && compuerta != null)
        {
            compuerta.Abrir(instantaneo);
        }

        if (zonaCompleta && !instantaneo)
        {
            Sonar(sonidoEnergia);
        }
    }

    private void AlarmaLuces(float segundos)
    {
        if (lucesEmergencia == null)
        {
            return;
        }

        foreach (LuzEmergencia luz in lucesEmergencia.GetComponentsInChildren<LuzEmergencia>())
        {
            luz.Alarma(segundos);
        }
    }

    // ---------- Salida y victoria ----------

    public void JugadorEnSalida()
    {
        GameManager gm = GameManager.Instance;
        if (gm.gano || gm.jugadorMuerto)
        {
            return;
        }

        if (!zonaCompleta)
        {
            if (Time.time >= proximoAvisoSalida)
            {
                proximoAvisoSalida = Time.time + 4f;
                gm.EncolarMensaje("La compuerta está sellada: restablece los servidores.");
            }
            return;
        }

        gm.CompletarObjetivo("zona04_evacuada");
        gm.GuardarPartida("evacuacion");
        gm.Victoria();
    }

    // ---------- HUD ----------

    // Se llama solo cuando algo cambia (no en cada frame)
    private void ActualizarObjetivo()
    {
        GameManager gm = GameManager.Instance;

        if (!tieneAcceso)
        {
            textoObjetivo = "Sin autorización: consigue el acceso en la Sala de control";
        }
        else if (zonaCompleta)
        {
            textoObjetivo = "Energía restablecida: llega a la compuerta de evacuación del túnel norte";
        }
        else if (!gm.ObjetivoCompletado(OBJETIVO_TERMINAL))
        {
            textoObjetivo = "Encuentra la terminal de servidores y consulta el orden de reinicio";
        }
        else
        {
            int hechos = ordenReinicio.Length - gm.CantidadTareas();
            textoObjetivo = "Reinicia los racks en orden (" + hechos + "/" + ordenReinicio.Length + ")  ·  Siguiente: RACK " + gm.SiguienteTarea();
        }
    }

    // Por si otra logica necesita cambiar el texto del HUD
    public void CambiarObjetivo(string objetivo, string ayuda)
    {
        textoObjetivo = objetivo;
        textoAyuda = ayuda;
    }

    private static string CodigoRack(string id)
    {
        return "rack_" + id;
    }

    private void Sonar(AudioClip clip)
    {
        if (efectos != null && clip != null)
        {
            efectos.PlayOneShot(clip);
        }
    }
}
