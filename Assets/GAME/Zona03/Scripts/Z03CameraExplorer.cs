using UnityEngine;
using UnityEngine.InputSystem;

namespace ControlRoomZ03
{
    /// <summary>
    /// Control de exploración de prueba para la escena aún sin jugador.
    /// Ratón derecho = mirar; WASD = desplazarse; Q/E = subir/bajar; Shift = rápido.
    /// Se desactiva automáticamente cuando hay un PlayerController activo en escena.
    /// </summary>
    public sealed class Z03CameraExplorer : MonoBehaviour
    {
        [SerializeField, Range(1f, 20f)] private float velocidad = 5f;
        [SerializeField, Range(0.05f, 1f)] private float sensibilidad = 0.18f;
        [SerializeField] private bool desactivarSiExisteJugador = true;
        private float pitch;
        private float yaw;

        private void Start()
        {
            if (desactivarSiExisteJugador && FindAnyObjectByType<PlayerController>() != null)
            {
                enabled = false;
                return;
            }
            Vector3 angulos = transform.eulerAngles;
            pitch = angulos.x > 180f ? angulos.x - 360f : angulos.x;
            yaw = angulos.y;
        }

        private void Update()
        {
            var keys = Keyboard.current;
            var mouse = Mouse.current;
            if (keys == null) return;
            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue();
                yaw += delta.x * sensibilidad;
                pitch = Mathf.Clamp(pitch - delta.y * sensibilidad, -85f, 85f);
                transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Vector3 movement = Vector3.zero;
            if (keys.wKey.isPressed) movement += transform.forward;
            if (keys.sKey.isPressed) movement -= transform.forward;
            if (keys.dKey.isPressed) movement += transform.right;
            if (keys.aKey.isPressed) movement -= transform.right;
            if (keys.eKey.isPressed) movement += Vector3.up;
            if (keys.qKey.isPressed) movement -= Vector3.up;
            float factor = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed ? 2.5f : 1f;
            transform.position += movement.normalized * velocidad * factor * Time.deltaTime;
        }
    }
}
