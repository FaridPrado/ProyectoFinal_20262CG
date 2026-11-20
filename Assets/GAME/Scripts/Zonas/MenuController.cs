using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Paneles")]
    public GameObject mainMenuPanel;
    public GameObject selectionPanel;
    public GameObject instructionsPanel;

    private void Start()
    {
        GameManager.Instance.MenuIniciado();
        if (GameManager.Instance.HayPartidaGuardada()) CrearBotonContinuar();
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

    private void CrearBotonContinuar()
    {
        var boton = new GameObject("Continuar partida", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button));
        boton.transform.SetParent(mainMenuPanel.transform, false);
        var rect = (RectTransform)boton.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.78f, 0.48f);
        rect.sizeDelta = new Vector2(250f, 68f);
        var fondo = boton.GetComponent<UnityEngine.UI.Image>();
        fondo.color = new Color(0.045f, 0.16f, 0.21f);
        var control = boton.GetComponent<UnityEngine.UI.Button>();
        control.targetGraphic = fondo;
        control.onClick.AddListener(GameManager.Instance.ContinuarPartida);
        var etiqueta = new GameObject("Texto", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        etiqueta.transform.SetParent(rect, false);
        var area = (RectTransform)etiqueta.transform;
        area.anchorMin = Vector2.zero;
        area.anchorMax = Vector2.one;
        area.offsetMin = area.offsetMax = Vector2.zero;
        var texto = etiqueta.GetComponent<UnityEngine.UI.Text>();
        texto.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        texto.text = "CONTINUAR";
        texto.fontSize = 30;
        texto.alignment = TextAnchor.MiddleCenter;
        texto.raycastTarget = false;
    }

    public void CerrarJuego()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();
    }
}