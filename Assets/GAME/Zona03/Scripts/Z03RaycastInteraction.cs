using UnityEngine;
using UnityEngine.InputSystem;

namespace ControlRoomZ03
{
    public sealed class Z03RaycastInteraction : MonoBehaviour
    {
        [SerializeField] private float rango = 3.2f;
        private Z03ZoneGameplay zona;
        private Camera camara;
        private PlayerController jugador;
        private float radio;

        private void Start()
        {
            zona = GetComponent<Z03ZoneGameplay>();
            jugador = FindFirstObjectByType<PlayerController>();
            radio = jugador.GetComponent<CharacterController>().radius;
            camara = jugador.GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            var partida = GameManager.Instance;
            if (partida.enPausa || partida.jugadorMuerto || !camara) return;
            // Desde el plano del personaje, para que su cuerpo no tape el rayo de tercera persona.
            var rayo = camara.ViewportPointToRay(new Vector3(.5f, .5f));
            float distancia = Vector3.Dot(jugador.transform.position - rayo.origin, rayo.direction);
            Vector3 origen = rayo.GetPoint(Mathf.Max(0, distancia) + radio);
            Z03InteractionNode nodo = null;
            if (Physics.Raycast(origen, rayo.direction, out var hit, rango, ~(1 << 2), QueryTriggerInteraction.Collide))
                nodo = hit.collider.GetComponentInParent<Z03InteractionNode>();
            zona.SetInteractionHint(nodo ? "E / CLIC · " + nodo.Etiqueta : "Busca los paneles señalizados · ESC pausa");
            if (nodo && ((Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)))
                nodo.Activar();
        }
    }
}
