using UnityEngine;

namespace ControlRoomZ03
{
    public enum Z03Action { Vigilancia = 0, Energia = 1, Terminal = 2, Salida = 3, Registro = 4, Reserva = 5 }

    public sealed class Z03InteractionNode : MonoBehaviour
    {
        public Z03Action accion;
        [SerializeField] private AudioClip sonido;
        [SerializeField] private Renderer piloto;
        [SerializeField] private Material pilotoListo;
        private AudioSource audioNodo;
        private Z03ZoneGameplay zona;

        private void Awake()
        {
            if (!sonido) return;
            audioNodo = GetComponent<AudioSource>();
            if (!audioNodo) return;
            audioNodo.playOnAwake = false;
            audioNodo.spatialBlend = .85f;
            audioNodo.minDistance = 1.3f;
            audioNodo.maxDistance = 14f;
        }

        private void Start()
        {
            zona = FindFirstObjectByType<Z03ZoneGameplay>();
            ActualizarPiloto();
        }

        public string Etiqueta
        {
            get
            {
                switch (accion)
                {
                    case Z03Action.Registro: return "Recuperar registro de incidentes";
                    case Z03Action.Reserva: return "Conectar alimentación de reserva";
                    case Z03Action.Vigilancia: return "Restablecer vigilancia";
                    case Z03Action.Energia: return "Restablecer energía";
                    case Z03Action.Terminal: return "Autorizar terminal central";
                    default: return "Acceder a servidores";
                }
            }
        }

        public void Activar()
        {
            if (audioNodo) audioNodo.PlayOneShot(sonido);
            if (accion == Z03Action.Terminal)
            {
                var ui = FindFirstObjectByType<UIManager>();
                if (ui)
                {
                    ui.OpenTerminal();
                    return;
                }
            }
            if (zona) zona.Interactuar(accion);
            ActualizarPiloto();
        }

        private void ActualizarPiloto()
        {
            if (piloto && pilotoListo && Z03ZoneGameplay.EstacionPreparada(accion))
                piloto.sharedMaterial = pilotoListo;
        }
    }
}
