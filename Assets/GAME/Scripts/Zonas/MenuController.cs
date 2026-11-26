using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainMenuPanel;
    public GameObject selectionPanel;
    public GameObject instructionsPanel;

    [SerializeField] private UnityEngine.UI.Button continuar;

    private void Start()
    {
        GameManager.Instance.MenuIniciado();
        continuar.gameObject.SetActive(GameManager.Instance.HayPartidaGuardada());
        continuar.onClick.AddListener(GameManager.Instance.ContinuarPartida);
        MostrarMenu();
    }

    public void MostrarMenu()
    {
        mainMenuPanel.SetActive(true);
        selectionPanel.SetActive(false);
        instructionsPanel.SetActive(false);
    }

    public void MostrarSeleccion()
    {
        mainMenuPanel.SetActive(false);
        selectionPanel.SetActive(true);
        instructionsPanel.SetActive(false);
    }

    public void MostrarInstrucciones()
    {
        mainMenuPanel.SetActive(false);
        selectionPanel.SetActive(false);
        instructionsPanel.SetActive(true);
    }

    public void SeleccionarAsuna()
    {
        SeleccionarPersonaje("Asuna");
    }

    public void SeleccionarSurvivalist()
    {
        SeleccionarPersonaje("Survivalist");
    }

    private void SeleccionarPersonaje(string personaje)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("No existe GameManager en la escena.");
            return;
        }

        GameManager.Instance.personajeSeleccionado = personaje;

        Debug.Log("Personaje seleccionado: " + personaje);

        GameManager.Instance.NuevaPartida();
    }

    public void CerrarJuego()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();
    }
}