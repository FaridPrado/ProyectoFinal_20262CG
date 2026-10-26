// Responsabilidad (enunciado 5.1): estado global persistente, cambio de escenas,
// victoria, derrota y coordinacion del guardado/carga del JSON.
//
// Version sin eventos ni corrutinas (no explicados en clase):
//  - Los controladores de escena le AVISAN al GameManager llamando sus metodos
//    (EscenaIniciada, AgregarObjeto, DesbloquearPuerta...).
//  - El UIManager PREGUNTA el estado en su Update (vidaActual, jugadorMuerto,
//    enPausa, HayMensajes...) y se actualiza solo.
//  - La espera antes de reiniciar tras la derrota es un contador en Update.
//
// Mantiene los mismos nombres que ya usa PlayerHealth (Instance, RecibirDanio,
// Curar, vidaActual), asi que ese script sigue funcionando sin cambios.

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Escenas")]
    public string escenaMenu = "00_Menu";

    [Header("Estado del jugador")]
    public int vidaMaxima = 100;
    public int vidaActual = 100;

    [Header("Progreso")]
    public string escenaActual = "00_Menu";
    public string puntoControl = "inicio";
    public float tiempoJugado;
    public int muertes;

    [Header("Derrota")]
    public float segundosAntesDeReiniciar = 3f;

    [Header("Estado de la partida (solo lectura)")]
    public bool enPartida;
    public bool enPausa;
    public bool jugadorMuerto;
    public bool gano;

    // List: crecen durante la partida y se consultan con Contains
    public List<string> inventario = new List<string>();
    public List<string> objetivosCompletados = new List<string>();

    // Dictionary: codigo de puerta -> desbloqueada.
    // Una terminal o puerta pregunta por SU codigo directamente,
    // sin recorrer una lista buscando cual es
    private Dictionary<string, bool> puertas = new Dictionary<string, bool>();

    // Queue: mensajes del sistema que el HUD muestra uno a uno,
    // en el mismo orden en que ocurrieron (FIFO)
    private Queue<string> mensajesSistema = new Queue<string>();

    // Stack: historial de accesos (credenciales usadas en terminales).
    // El tope es siempre el ultimo acceso; deshacer saca justo ese (LIFO)
    private Stack<string> historialAccesos = new Stack<string>();

    // true cuando la escena se carga desde el JSON (continuar o morir),
    // para no sobrescribir el punto de control que se acaba de leer
    private bool cargandoDesdeGuardado;

    // Cuenta hacia atras despues de morir; al llegar a 0 se reinicia
    private float tiempoParaReiniciar;

    // El UIManager compara este numero con el que vio antes:
    // si cambio, sabe que hubo danio y hace el destello rojo
    public int golpesRecibidos;

    private void Awake()
    {
        // Singleton: si al cargar otra escena ya existe un GameManager,
        // el nuevo se destruye y se conserva el original con su estado
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Valores iniciales desde nueva_partida.json. Asi se puede dar Play
        // directo en cualquier escena (sin pasar por el menu) para probarla
        AplicarDatos(SaveManager.CargarNuevaPartida());
    }

    private void Update()
    {
        if (enPartida && !enPausa && !jugadorMuerto)
        {
            tiempoJugado += Time.deltaTime;
        }

        // Espera antes de reiniciar tras la derrota.
        // unscaledDeltaTime: sigue contando aunque timeScale este en 0
        if (jugadorMuerto)
        {
            tiempoParaReiniciar -= Time.unscaledDeltaTime;

            if (tiempoParaReiniciar <= 0)
            {
                ReiniciarDesdeGuardado();
            }
        }
    }

    // ---------- Escenas ----------

    public void CambiarEscena(string nombreEscena)
    {
        // Si se cambia de escena desde la pausa, el tiempo debe volver a correr
        Time.timeScale = 1f;
        SceneManager.LoadScene(nombreEscena);
    }

    public void IrAlMenu()
    {
        enPartida = false;
        CambiarEscena(escenaMenu);
    }

    // Lo llama el controlador de cada escena jugable en su Start
    // (Zona01Controller, Zona02Controller...). Enunciado 5.2: el controlador
    // de escena "consulta y notifica al GameManager"
    public void EscenaIniciada()
    {
        escenaActual = SceneManager.GetActiveScene().name;
        enPartida = true;
        enPausa = false;
        jugadorMuerto = false;
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Punto de control: al entrar a cada escena jugable se guarda el progreso.
        // Si venimos de cargar el JSON no se guarda, para no pisar ese punto
        if (cargandoDesdeGuardado)
        {
            cargandoDesdeGuardado = false;
        }
        else
        {
            GuardarPartida("entrada_" + escenaActual);
        }
    }

    // Lo llama el script del menu en su Start
    public void MenuIniciado()
    {
        escenaActual = escenaMenu;
        enPartida = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ---------- Nueva partida / continuar / guardar ----------

    // Lo llama el boton "Nueva partida" del menu
    public void NuevaPartida()
    {
        AplicarDatos(SaveManager.CargarNuevaPartida());
        cargandoDesdeGuardado = false;
        CambiarEscena(escenaActual);
    }

    // El menu lo usa para activar o desactivar el boton "Continuar"
    public bool HayPartidaGuardada()
    {
        return SaveManager.ExisteGuardado();
    }

    // Lo llama el boton "Continuar" del menu
    public void ContinuarPartida()
    {
        if (!SaveManager.ExisteGuardado())
        {
            NuevaPartida();
            return;
        }

        AplicarDatos(SaveManager.CargarGuardado());
        cargandoDesdeGuardado = true;
        CambiarEscena(escenaActual);
    }

    // Punto de control. Se llama al entrar a una escena y lo pueden llamar
    // los controladores (por ejemplo, una terminal de guardado)
    public void GuardarPartida(string nombrePunto)
    {
        puntoControl = nombrePunto;

        DatosPartida datos = new DatosPartida();
        datos.escenaActual = escenaActual;
        datos.puntoControl = puntoControl;
        datos.vidaMaxima = vidaMaxima;
        datos.vidaActual = vidaActual;
        datos.inventario = new List<string>(inventario);
        datos.objetivosCompletados = new List<string>(objetivosCompletados);
        datos.puertas = SaveManager.DiccionarioALista(puertas);
        datos.mensajesPendientes = SaveManager.ColaALista(mensajesSistema);
        datos.historialAccesos = SaveManager.PilaALista(historialAccesos);
        datos.tiempoJugado = tiempoJugado;
        datos.muertes = muertes;

        SaveManager.Guardar(datos);
        EncolarMensaje("Progreso guardado.");
    }

    // Pasa el contenido leido del JSON al estado del juego
    // y reconstruye la cola, la pila y el diccionario
    private void AplicarDatos(DatosPartida datos)
    {
        escenaActual = datos.escenaActual;
        puntoControl = datos.puntoControl;
        vidaMaxima = datos.vidaMaxima;
        vidaActual = datos.vidaActual;
        inventario = new List<string>(datos.inventario);
        objetivosCompletados = new List<string>(datos.objetivosCompletados);
        puertas = SaveManager.ReconstruirDiccionario(datos.puertas);
        mensajesSistema = SaveManager.ReconstruirCola(datos.mensajesPendientes);
        historialAccesos = SaveManager.ReconstruirPila(datos.historialAccesos);
        tiempoJugado = datos.tiempoJugado;
        muertes = datos.muertes;
        gano = false;
    }

    // ---------- Vida, derrota y victoria ----------

    public void RecibirDanio(int cantidad)
    {
        if (jugadorMuerto)
        {
            return;
        }

        vidaActual = vidaActual - cantidad;

        if (vidaActual < 0)
        {
            vidaActual = 0;
        }

        golpesRecibidos++;

        if (vidaActual == 0)
        {
            Derrota();
        }
    }

    public void Curar(int cantidad)
    {
        if (jugadorMuerto)
        {
            return;
        }

        vidaActual = vidaActual + cantidad;

        if (vidaActual > vidaMaxima)
        {
            vidaActual = vidaMaxima;
        }
    }

    public void Derrota()
    {
        if (jugadorMuerto)
        {
            return;
        }

        // El UIManager ve jugadorMuerto en true y muestra el panel de derrota.
        // Update cuenta los segundos y luego llama a ReiniciarDesdeGuardado
        jugadorMuerto = true;
        muertes++;
        tiempoParaReiniciar = segundosAntesDeReiniciar;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Reinicio desde el ultimo punto de guardado leyendo el JSON (enunciado 10.4)
    public void ReiniciarDesdeGuardado()
    {
        jugadorMuerto = false;

        if (!SaveManager.ExisteGuardado())
        {
            NuevaPartida();
            return;
        }

        // Las estadisticas no deben "rebobinarse" al morir: se conservan
        int muertesTotales = muertes;
        float tiempoTotal = tiempoJugado;

        AplicarDatos(SaveManager.CargarGuardado());

        muertes = muertesTotales;
        tiempoJugado = tiempoTotal;

        cargandoDesdeGuardado = true;
        CambiarEscena(escenaActual);
    }

    public void Victoria()
    {
        // El UIManager ve gano en true y muestra el panel de victoria
        gano = true;
        enPartida = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ---------- Pausa ----------

    public void AlternarPausa()
    {
        if (!enPartida || jugadorMuerto)
        {
            return;
        }

        enPausa = !enPausa;

        if (enPausa)
        {
            // Se congela el juego y se libera el mouse para usar los botones
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // ---------- Inventario y objetivos (List) ----------

    public void AgregarObjeto(string objeto)
    {
        if (!inventario.Contains(objeto))
        {
            inventario.Add(objeto);
        }
    }

    public bool TieneObjeto(string objeto)
    {
        return inventario.Contains(objeto);
    }

    public void CompletarObjetivo(string objetivo)
    {
        if (!objetivosCompletados.Contains(objetivo))
        {
            objetivosCompletados.Add(objetivo);
        }
    }

    public bool ObjetivoCompletado(string objetivo)
    {
        return objetivosCompletados.Contains(objetivo);
    }

    // ---------- Puertas (Dictionary) ----------

    // Cada puerta se registra al iniciar su escena. Si ya venia del JSON
    // no se toca, para no volver a bloquear una puerta ya abierta
    public void RegistrarPuerta(string codigo)
    {
        if (!puertas.ContainsKey(codigo))
        {
            puertas.Add(codigo, false);
        }
    }

    public void DesbloquearPuerta(string codigo)
    {
        puertas[codigo] = true;
    }

    public bool PuertaDesbloqueada(string codigo)
    {
        // Acceso directo por clave: no hay que recorrer nada
        if (puertas.ContainsKey(codigo))
        {
            return puertas[codigo];
        }

        return false;
    }

    // ---------- Mensajes del sistema (Queue) ----------

    public void EncolarMensaje(string mensaje)
    {
        mensajesSistema.Enqueue(mensaje);
    }

    public bool HayMensajes()
    {
        return mensajesSistema.Count > 0;
    }

    // El HUD la llama para mostrar el siguiente mensaje: este es el Dequeue
    // con efecto visible que pide el enunciado (seccion 8)
    public string SiguienteMensaje()
    {
        if (mensajesSistema.Count == 0)
        {
            return "";
        }

        return mensajesSistema.Dequeue();
    }

    // ---------- Historial de accesos (Stack) ----------

    public void RegistrarAcceso(string acceso)
    {
        historialAccesos.Push(acceso);
    }

    // Consultar el ultimo acceso sin sacarlo
    public string UltimoAcceso()
    {
        if (historialAccesos.Count == 0)
        {
            return "";
        }

        return historialAccesos.Peek();
    }

    // Deshace el ultimo acceso ("Cerrar sesion" o "Atras" en una terminal):
    // este es el Pop con efecto visible que pide el enunciado
    public string DeshacerUltimoAcceso()
    {
        if (historialAccesos.Count == 0)
        {
            return "";
        }

        return historialAccesos.Pop();
    }

    public int CantidadAccesos()
    {
        return historialAccesos.Count;
    }
}
