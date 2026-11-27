using UnityEngine;
using UnityEngine.InputSystem;

public class ArmaRecogible : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform puntoArma;      
    [SerializeField] private Camera camara;            
    [SerializeField] private AudioSource sonidoDisparo;
    [SerializeField] private ParticleSystem fogonazo;

    [Header("Giro en el suelo")]
    [SerializeField] private float velocidadGiro = 90f;

    [Header("Disparo")]
    [SerializeField] private float alcance = 100f;
    [SerializeField] private int dano = 10;
    [SerializeField] private float cadencia = 0.2f;

    private bool recogida;
    private bool jugadorCerca;
    private float proximoDisparo;
    private InteractionController interaccion;

    private void Start()
    {
        interaccion = FindFirstObjectByType<InteractionController>();
    }

    private void Update()
    {
        // En pausa, en la terminal o muerto no se recoge ni se dispara
        GameManager gm = GameManager.Instance;
        if (gm != null && (gm.enPausa || gm.jugadorMuerto))
            return;

        if (!recogida)
        {
            transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);

            if (jugadorCerca && Keyboard.current.eKey.wasPressedThisFrame)
                Recoger();
        }
        else if (QuiereDisparar() && Time.time >= proximoDisparo)
        {
            proximoDisparo = Time.time + cadencia;
            Disparar();
        }
    }

    // P o clic izquierdo. El clic no dispara si la mira apunta a un objeto
    // interactivo (en ese caso el clic es para el InteractionController)
    private bool QuiereDisparar()
    {
        if (Keyboard.current != null && Keyboard.current.pKey.isPressed)
            return true;

        bool clic = Mouse.current != null && Mouse.current.leftButton.isPressed;
        return clic && (interaccion == null || !interaccion.TieneObjetivo);
    }

    [Header("Posición al recogerla")]
    [SerializeField] private Vector3 posicionEnMano = Vector3.zero;
    [SerializeField] private Vector3 rotacionEnMano = Vector3.zero;
    [SerializeField] private Vector3 escalaEnMano = Vector3.one;

    private void Recoger()
    {
        recogida = true;
        transform.SetParent(puntoArma);
        transform.localPosition = posicionEnMano;
        transform.localRotation = Quaternion.Euler(rotacionEnMano);
        transform.localScale = escalaEnMano;

        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
    }
    private void Disparar()
    {
        if (sonidoDisparo) sonidoDisparo.Play();
        if (fogonazo) fogonazo.Play();

        Ray rayo = new Ray(camara.transform.position, camara.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(rayo, alcance, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var hit in hits)
        {
            if (hit.collider.transform.root == camara.transform.root) continue; // ignora al Player

            Debug.Log("Disparo golpeó: " + hit.collider.name);
            Debug.DrawLine(rayo.origin, hit.point, Color.red, 1f);

            if (hit.collider.TryGetComponent(out IDamageable objetivo))
            {
                objetivo.TakeDamage(dano);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) jugadorCerca = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) jugadorCerca = false;
    }
}
