using UnityEngine;

public class Zona04Controller : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.EscenaIniciada();
        GameManager.Instance.Victoria();
    }
}
