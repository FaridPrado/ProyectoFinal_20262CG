using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace LaboratorioAbandonado.Menu
{
    public sealed class LAMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject[] paginas;
        [SerializeField] private Text estado;
        [SerializeField] private Text estadoGuardado;
        [SerializeField] private Button continuar;
        [SerializeField] private Slider volumen;
        [SerializeField] private Slider sensibilidad;
        [SerializeField] private Text etiquetaVolumen;
        [SerializeField] private Text etiquetaSensibilidad;
        [SerializeField] private Text etiquetaInvertir;
        [SerializeField] private Text etiquetaPantalla;
        [SerializeField] private AudioSource audioUI;
        [SerializeField] private AudioClip click;

        private readonly Stack<string> historial = new Stack<string>();
        private Selectable[] controles;
        private string pagina;
        private bool cargando;

        private void Start()
        {
            GameManager.Instance.MenuIniciado();
            AudioListener.volume = PlayerPrefs.GetFloat("LA.Volume", .7f);
            Mostrar("Inicio");
        }

        private void Sonido()
        {
            if (click) audioUI.PlayOneShot(click);
        }

        private void Mostrar(string nombre)
        {
            pagina = nombre;
            estado.text = "";
            foreach (GameObject vista in paginas)
            {
                bool visible = vista.name == nombre;
                vista.SetActive(visible);
                if (visible) controles = vista.GetComponentsInChildren<Selectable>();
            }

            if (nombre == "Inicio")
            {
                historial.Clear();
                bool existe = GameManager.Instance.HayPartidaGuardada();
                continuar.interactable = existe;
                estadoGuardado.text = existe ? "Partida guardada disponible" : "Sin checkpoint guardado";
            }
            if (nombre == "Ajustes")
            {
                volumen.SetValueWithoutNotify(PlayerPrefs.GetFloat("LA.Volume", .7f));
                sensibilidad.SetValueWithoutNotify(PlayerPrefs.GetFloat("LA.Sensitivity", 1f));
                etiquetaVolumen.text = "Volumen general  " + volumen.value.ToString("0.0");
                etiquetaSensibilidad.text = "Sensibilidad del ratón  " + sensibilidad.value.ToString("0.0");
                OpcionesDePantalla();
            }
            if (EventSystem.current && controles.Length > 0)
                EventSystem.current.SetSelectedGameObject(controles[0].gameObject);
        }

        public void Abrir(string nombre)
        {
            if (cargando) return;
            Sonido();
            historial.Push(pagina);
            Mostrar(nombre);
        }

        public void Volver()
        {
            Sonido();
            Mostrar(historial.Count > 0 ? historial.Pop() : "Inicio");
        }

        public void Nueva() => Abrir(GameManager.Instance.HayPartidaGuardada() ? "Confirmar" : "Introducción");
        public void Confirmar() { Sonido(); Mostrar("Introducción"); }
        public void Comenzar()
        {
            if (cargando) return;
            cargando = true;
            Sonido();
            GameManager.Instance.NuevaPartida();
        }
        public void Continuar()
        {
            if (cargando) return;
            cargando = true;
            Sonido();
            GameManager.Instance.ContinuarPartida();
        }
        public void CambiarVolumen(float valor)
        {
            AudioListener.volume = valor;
            PlayerPrefs.SetFloat("LA.Volume", valor);
            etiquetaVolumen.text = "Volumen general  " + valor.ToString("0.0");
        }
        public void CambiarSensibilidad(float valor)
        {
            PlayerPrefs.SetFloat("LA.Sensitivity", valor);
            etiquetaSensibilidad.text = "Sensibilidad del ratón  " + valor.ToString("0.0");
        }
        private void OpcionesDePantalla()
        {
            etiquetaInvertir.text = PlayerPrefs.GetInt("LA.InvertY", 0) == 1 ? "INVERTIR Y: SÍ" : "INVERTIR Y: NO";
            etiquetaPantalla.text = Screen.fullScreen ? "PANTALLA: COMPLETA" : "PANTALLA: VENTANA";
        }
        public void Invertir()
        {
            Sonido();
            PlayerPrefs.SetInt("LA.InvertY", 1 - PlayerPrefs.GetInt("LA.InvertY", 0));
            OpcionesDePantalla();
        }
        public void Pantalla()
        {
            Sonido();
            bool completa = !Screen.fullScreen;
            Screen.fullScreen = completa;
            etiquetaPantalla.text = completa ? "PANTALLA: COMPLETA" : "PANTALLA: VENTANA";
            estado.text = "Cambio de pantalla solicitado (en ejecutable PC).";
        }
        public void GuardarOpciones() { PlayerPrefs.Save(); Volver(); }
        public void Salir()
        {
            Sonido();
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Update()
        {
            var teclado = Keyboard.current;
            if (cargando || teclado == null || controles == null) return;
            if (teclado.tabKey.wasPressedThisFrame && EventSystem.current)
            {
                var elegido = EventSystem.current.currentSelectedGameObject;
                int indice = System.Array.FindIndex(controles, c => c.gameObject == elegido);
                for (int i = 0; i < controles.Length; i++)
                {
                    indice = (indice + 1) % controles.Length;
                    if (!controles[indice].IsInteractable()) continue;
                    EventSystem.current.SetSelectedGameObject(controles[indice].gameObject);
                    break;
                }
            }
            if (teclado.escapeKey.wasPressedThisFrame)
            {
                if (pagina == "Inicio") Abrir("Salir");
                else Volver();
            }
        }
    }
}
