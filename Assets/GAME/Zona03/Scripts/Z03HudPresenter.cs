using System;
using UnityEngine;
using UnityEngine.UI;


namespace ControlRoomZ03
{
    /// <summary>HUD existente: un solo Canvas. La zona actualiza textos, no crea otro Canvas.</summary>
    public sealed class Z03HudPresenter : MonoBehaviour
    {
        [SerializeField] private Text reloj;
        [SerializeField] private Text estado;
        [SerializeField] private Text objetivo;
        [SerializeField] private Text ayuda;
        private string nuevoEstado;
        private string nuevoObjetivo;
        private string nuevaAyuda;
        public void Configure(Text clock, Text status, Text mission, Text helper)
        { reloj=clock; estado=status; objetivo=mission; ayuda=helper; }
        public void SetObjective(string texto) { nuevoObjetivo=texto; }
        public void SetStatus(string texto) { nuevoEstado=texto; }
        public void SetHelp(string texto) { nuevaAyuda=texto; }
        private void Update()
        {
            if(reloj) reloj.text=DateTime.Now.ToString("HH:mm:ss");
            if(estado && nuevoEstado!=null) estado.text=nuevoEstado;
            if(objetivo && nuevoObjetivo!=null) objetivo.text=nuevoObjetivo;
            if(ayuda && nuevaAyuda!=null) ayuda.text=nuevaAyuda;
            if(Z03Input.Down(KeyCode.H))
            {
                // Ocultar textos, sin desactivar el botón de continuación.
                if(objetivo) objetivo.enabled=!objetivo.enabled;
            }
        }
    }
}
