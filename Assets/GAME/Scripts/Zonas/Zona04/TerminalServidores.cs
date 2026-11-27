using UnityEngine;

// Terminal de la sala de servidores. Al activarla registra el acceso en la
// pila del GameManager (Push) y abre el panel de terminal del Canvas unico
// a traves del controlador de la zona.
public class TerminalServidores : Interactable
{
    [SerializeField] private Zona04Controller controlador;

    protected override void AlInteractuar()
    {
        if (controlador != null)
        {
            controlador.AbrirTerminal();
        }
    }
}
