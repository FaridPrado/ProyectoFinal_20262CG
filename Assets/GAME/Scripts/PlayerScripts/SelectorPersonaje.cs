// Muestra el personaje elegido en el menu (Asuna o Survivalist) y oculta el otro.
//
// Los dos modelos estan dentro del prefab Player como hijos. El menu de
// Valeria guarda el nombre en GameManager.personajeSeleccionado y aqui solo
// se lee ese nombre: el movimiento sigue siendo el mismo PlayerController,
// asi que funciona igual con cualquiera de los dos.
//
// Le pasa al PlayerController el Animator del modelo visible para que lo
// anime con los parametros "Velocidad" y "Agachado".

using UnityEngine;

public class SelectorPersonaje : MonoBehaviour
{
    [Header("Modelos (hijos del Player)")]
    public GameObject modeloAsuna;
    public GameObject modeloSurvivalist;

    [Header("Si se da Play sin pasar por el menu")]
    public string personajePorDefecto = "Survivalist";

    private void Awake()
    {
        string elegido = personajePorDefecto;

        if (GameManager.Instance != null && GameManager.Instance.personajeSeleccionado != "")
        {
            elegido = GameManager.Instance.personajeSeleccionado;
        }

        bool esAsuna = elegido == "Asuna";

        if (modeloAsuna != null)
        {
            modeloAsuna.SetActive(esAsuna);
        }

        if (modeloSurvivalist != null)
        {
            modeloSurvivalist.SetActive(!esAsuna);
        }

        GameObject modeloActivo = modeloSurvivalist;

        if (esAsuna)
        {
            modeloActivo = modeloAsuna;
        }

        if (modeloActivo == null)
        {
            return;
        }

        Animator animator = modeloActivo.GetComponent<Animator>();

        if (animator != null)
        {
            // El que mueve al jugador es el CharacterController, no la animacion
            animator.applyRootMotion = false;
            GetComponent<PlayerController>().animator = animator;
        }
    }
}
