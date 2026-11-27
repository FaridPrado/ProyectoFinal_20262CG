using UnityEngine;

// Trigger detras de la compuerta de evacuacion. Cuando el jugador entra
// avisa al controlador, que decide si hay victoria (zona completa) o no.
[RequireComponent(typeof(Collider))]
public class SalidaZona04 : MonoBehaviour
{
    [SerializeField] private Zona04Controller controlador;

    private void OnTriggerEnter(Collider other)
    {
        if (controlador != null && other.CompareTag("Player"))
        {
            controlador.JugadorEnSalida();
        }
    }
}
