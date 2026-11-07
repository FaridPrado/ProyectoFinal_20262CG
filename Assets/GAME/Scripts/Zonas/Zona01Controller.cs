using UnityEngine;

public class Zona01Controller : MonoBehaviour
{
    private void Start()
    {
        // Pone enPartida en true (sin eso no funciona la pausa), bloquea el cursor
        // y guarda el punto de control de entrada en partida.json
        GameManager.Instance.EscenaIniciada();
    }
}
