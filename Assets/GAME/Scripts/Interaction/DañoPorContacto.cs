using UnityEngine;

public class DanoPorContacto : MonoBehaviour
{
    [SerializeField] private int dano = 5;
    [SerializeField] private float tiempoEntreDanos = 1f;

    private float proximoDano;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time < proximoDano) return;
        if (GameManager.Instance == null) return;

        proximoDano = Time.time + tiempoEntreDanos;
        GameManager.Instance.vidaActual = Mathf.Max(0, GameManager.Instance.vidaActual - dano);
    }
}