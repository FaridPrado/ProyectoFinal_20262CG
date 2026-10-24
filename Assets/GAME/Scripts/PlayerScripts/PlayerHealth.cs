using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public void RecibirDanio(int cantidad)
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.RecibirDanio(cantidad);

        Debug.Log("Vida actual: " + GameManager.Instance.vidaActual);

        if (GameManager.Instance.vidaActual <= 0)
        {
            Debug.Log("El jugador ha muerto");
        }
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