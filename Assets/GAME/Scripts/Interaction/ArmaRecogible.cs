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

    private void Update()
    {
        if (!recogida)
        {
            transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);

            if (jugadorCerca && Keyboard.current.eKey.wasPressedThisFrame)
                Recoger();
        }
        else if (Keyboard.current.pKey.isPressed && Time.time >= proximoDisparo)
        {
            proximoDisparo = Time.time + cadencia;
            Disparar();
        }
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
