using UnityEngine;

// Zona que dana al jugador mientras este dentro del trigger, con cooldown.
// El dano pasa por PlayerHealth para que haya derrota, sonido y sacudida.
public class DanoPorContacto : MonoBehaviour
{
    [SerializeField] private int dano = 5;
    [SerializeField] private float tiempoEntreDanos = 1f;

    private float proximoDano;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < proximoDano) return;

        PlayerHealth vida = other.GetComponentInParent<PlayerHealth>();
        if (vida == null) return;

        proximoDano = Time.time + tiempoEntreDanos;
        vida.RecibirDanio(dano);
    }
}
