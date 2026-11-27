using UnityEngine;

// Unico punto de entrada del dano al jugador: lo usan el AgentController,
// el Zombie (via IDamageable) y las zonas de DanoPorContacto. Asi todo dano
// pasa por GameManager.RecibirDanio (derrota al llegar a 0) y tiene
// retroalimentacion (sonido + sacudida de camara).
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Retroalimentacion de danio")]
    public AudioSource fuenteAudio;
    public AudioClip sonidoDanio;
    public float duracionSacudida = 0.25f;
    public float intensidadSacudida = 0.08f;


    private PlayerController jugador;

    private void Awake()
    {
        jugador = GetComponent<PlayerController>();

    }

    // El AgentController la llama una vez por ataque, respetando su cooldown
    public void RecibirDanio(int cantidad)
    {
        if (GameManager.Instance == null || GameManager.Instance.jugadorMuerto)
        {
            return;
        }

        GameManager.Instance.RecibirDanio(cantidad);

        if (fuenteAudio != null && sonidoDanio != null)
        {
            // PlayOneShot no corta otro sonido que este sonando
            fuenteAudio.PlayOneShot(sonidoDanio);
        }

        if (jugador != null)
        {
            jugador.SacudirCamara(duracionSacudida, intensidadSacudida);
        }

        Debug.Log("Vida actual: " + GameManager.Instance.vidaActual);
    }

    // IDamageable: el Zombie ataca con TakeDamage(float); se redirige al mismo flujo
    public void TakeDamage(float amount)
    {
        RecibirDanio(Mathf.RoundToInt(amount));
    }

    public void Curar(int cantidad)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.Curar(cantidad);

        Debug.Log("Vida actual: " + GameManager.Instance.vidaActual);
    }

    public int ObtenerVidaActual()
    {
        if (GameManager.Instance == null)
        {
            return 0;
        }

        return GameManager.Instance.vidaActual;
    }
}
