using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace ControlRoomZ03
{
    /// <summary>Presentación del HUD de Zona 03. Sin reglas de juego ni dependencia de UIManager.</summary>
    public sealed class Z03HudPresenter : MonoBehaviour
    {
        [SerializeField] private Text reloj;
        [SerializeField] private Text estado;
        [SerializeField] private Text objetivo;
        [SerializeField] private Text ayuda;

        public void Configure(Text clock, Text status, Text mission, Text helper)
        {
            reloj = clock; estado = status; objetivo = mission; ayuda = helper;
        }

        private void Start()
        {
            if (objetivo != null)
                objetivo.text = "RECONOCIMIENTO DE LA ZONA\nExplora las estaciones de control y los sistemas de seguridad.";
            if (ayuda != null)
                ayuda.text = "WASD MOVERSE   •   RATON DERECHO MIRAR   •   Q/E ALTURA   •   SHIFT RAPIDO   •   H OCULTAR HUD";
        }

        private void Update()
        {
            if (reloj != null) reloj.text = DateTime.Now.ToString("HH:mm:ss");
            if (estado != null) estado.text = Mathf.Sin(Time.time * 1.3f) > -.3f ? "●  SISTEMAS EN LINEA" : "○  MONITOREO ACTIVO";
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
            {
                // Mantener visible el encabezado para que H siempre permita recuperar la interfaz.
                bool visible = objetivo != null && objetivo.gameObject.activeSelf;
                if (objetivo != null) objetivo.gameObject.SetActive(!visible);
                if (ayuda != null) ayuda.gameObject.SetActive(!visible);
            }
        }
    }
}
