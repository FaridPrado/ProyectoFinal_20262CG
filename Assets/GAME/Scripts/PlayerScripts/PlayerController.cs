using UnityEngine;
using UnityEngine.InputSystem;

// Unity agrega el CharacterController solo si el objeto no lo tiene
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform camara;      // la Main Camera, hija del jugador
    public Animator animator;     // opcional mientras no haya modelo del personaje

    [Header("Movimiento")]
    public float velocidadCaminar = 3f;
    public float velocidadCorrer = 5.5f;
    public float velocidadAgachado = 1.5f;
    public float gravedad = -20f;

    [Header("Mirar con el mouse")]
    public float sensibilidad = 0.1f;
    public float limiteVertical = 80f;

    [Header("Agacharse")]
    public float alturaDePie = 1.8f;
    public float alturaAgachado = 1.0f;
    public float velocidadTransicion = 8f;

    private CharacterController controlador;
    private Vector3 posicionCamaraDePie;
    private float rotacionVertical;
    private float velocidadY;
    private bool controlBloqueado;

    private float tiempoSacudida;
    private float intensidadSacudida;

    // Otros scripts las pueden leer, pero solo este script las cambia.
    // El AgentController las usara para el ruido y para esconderse.
    public bool EstaAgachado { get; private set; }
    public bool EstaCorriendo { get; private set; }

    // 0 = quieto o agachado, 1 = caminando, 2 = corriendo
    public float NivelRuido { get; private set; }

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();

        // Se recuerda donde esta la camara de pie para calcular desde ahi
        // la posicion agachado y la sacudida
        posicionCamaraDePie = camara.localPosition;
    }

    private void Start()
    {
        BloquearCursor(true);
    }

    private void Update()
    {
        if (Keyboard.current == null || Mouse.current == null)
        {
            return;
        }

        // La pausa no se abre mientras una terminal tiene el control
        if (Keyboard.current.escapeKey.wasPressedThisFrame && !controlBloqueado && GameManager.Instance != null)
        {
            GameManager.Instance.AlternarPausa();
        }

        if (EstaInactivo())
        {
            NivelRuido = 0f;
            ActualizarAnimator(0f);
            return;
        }

        Mirar();
        Agacharse();
        Moverse();
        ActualizarCamara();
    }

    // En pausa, muerto, despues de ganar o usando una terminal, no se mueve.
    // Solo consulta al GameManager: no guarda su propia copia del estado (5.2)
    private bool EstaInactivo()
    {
        if (controlBloqueado)
        {
            return true;
        }

        GameManager gm = GameManager.Instance;

        if (gm == null)
        {
            return false;
        }

        return gm.enPausa || gm.jugadorMuerto || gm.gano;
    }

    private void Mirar()
    {
        Vector2 delta = Mouse.current.delta.ReadValue() * sensibilidad;

        // El cuerpo gira a los lados y solo la camara mira arriba y abajo.
        // Asi WASD siempre se mueve hacia donde mira el jugador
        transform.Rotate(Vector3.up * delta.x);

        rotacionVertical = Mathf.Clamp(rotacionVertical - delta.y, -limiteVertical, limiteVertical);
        camara.localRotation = Quaternion.Euler(rotacionVertical, 0f, 0f);
    }

    private void Agacharse()
    {
        Keyboard teclado = Keyboard.current;
        bool quiereAgacharse = teclado.leftCtrlKey.isPressed || teclado.cKey.isPressed;

        if (quiereAgacharse)
        {
            EstaAgachado = true;
        }
        else if (EstaAgachado && HayEspacioParaPararse())
        {
            EstaAgachado = false;
        }

        float alturaObjetivo = alturaDePie;

        if (EstaAgachado)
        {
            alturaObjetivo = alturaAgachado;
        }

        // Lerp cambia la altura poco a poco para que no sea brusco.
        // El centro es la mitad de la altura para que los pies sigan en el piso
        controlador.height = Mathf.Lerp(controlador.height, alturaObjetivo, velocidadTransicion * Time.deltaTime);
        controlador.center = new Vector3(0f, controlador.height / 2f, 0f);
    }

    // Lanza una esfera hacia arriba: si choca con algo, no hay espacio.
    // Evita pararse debajo de una mesa o un ducto y atravesar el techo
    private bool HayEspacioParaPararse()
    {
        float radio = controlador.radius * 0.9f;
        Vector3 origen = transform.position + Vector3.up * (controlador.height - radio);
        float distancia = alturaDePie - controlador.height + 0.05f;
        RaycastHit choque;

        // QueryTriggerInteraction.Ignore: las zonas trigger no cuentan como techo
        bool hayTecho = Physics.SphereCast(origen, radio, Vector3.up, out choque, distancia, ~0, QueryTriggerInteraction.Ignore);

        return !hayTecho;
    }

    private void Moverse()
    {
        Keyboard teclado = Keyboard.current;
        float x = 0f;
        float z = 0f;

        if (teclado.wKey.isPressed) z += 1f;
        if (teclado.sKey.isPressed) z -= 1f;
        if (teclado.dKey.isPressed) x += 1f;
        if (teclado.aKey.isPressed) x -= 1f;

        Vector3 direccion = transform.right * x + transform.forward * z;

        // Normalizar para que en diagonal no se vaya mas rapido
        if (direccion.sqrMagnitude > 1f)
        {
            direccion.Normalize();
        }

        bool moviendose = direccion.sqrMagnitude > 0.01f;
        EstaCorriendo = moviendose && !EstaAgachado && teclado.leftShiftKey.isPressed;

        float velocidad = velocidadCaminar;

        if (EstaAgachado)
        {
            velocidad = velocidadAgachado;
        }
        else if (EstaCorriendo)
        {
            velocidad = velocidadCorrer;
        }

        // En el piso se deja un valor pequenio negativo para que
        // isGrounded no parpadee al bajar rampas o escalones
        if (controlador.isGrounded && velocidadY < 0f)
        {
            velocidadY = -2f;
        }

        velocidadY += gravedad * Time.deltaTime;

        Vector3 movimiento = direccion * velocidad + Vector3.up * velocidadY;
        controlador.Move(movimiento * Time.deltaTime);

        ActualizarRuido(moviendose);

        if (moviendose)
        {
            ActualizarAnimator(velocidad);
        }
        else
        {
            ActualizarAnimator(0f);
        }
    }

    private void ActualizarRuido(bool moviendose)
    {
        if (!moviendose || EstaAgachado)
        {
            NivelRuido = 0f;
        }
        else if (EstaCorriendo)
        {
            NivelRuido = 2f;
        }
        else
        {
            NivelRuido = 1f;
        }
    }

    // Baja la camara al agacharse y aplica la sacudida al recibir danio
    private void ActualizarCamara()
    {
        Vector3 posicion = posicionCamaraDePie;
        posicion.y -= alturaDePie - controlador.height;

        if (tiempoSacudida > 0f)
        {
            tiempoSacudida -= Time.deltaTime;
            posicion += Random.insideUnitSphere * intensidadSacudida;
        }

        camara.localPosition = posicion;
    }

    private void ActualizarAnimator(float velocidad)
    {
        // Sin modelo todavia, el campo esta vacio y simplemente no se anima
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("Velocidad", velocidad);
        animator.SetBool("Agachado", EstaAgachado);
    }

    // ---------- Metodos publicos para otros scripts ----------

    // Retroalimentacion de danio (enunciado 6.3). La llama PlayerHealth
    public void SacudirCamara(float duracion, float intensidad)
    {
        tiempoSacudida = duracion;
        intensidadSacudida = intensidad;
    }

    // Para terminales o paneles de inspeccion: congela al jugador
    // y libera el mouse para poder hacer clic en la interfaz
    public void BloquearControl(bool bloquear)
    {
        controlBloqueado = bloquear;
        BloquearCursor(!bloquear);
    }

    private void BloquearCursor(bool bloquear)
    {
        if (bloquear)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
