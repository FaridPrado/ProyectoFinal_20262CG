using UnityEngine;

namespace ControlRoomZ03
{
    public enum Z03Action { Vigilancia=0, Energia=1, Terminal=2, Salida=3 }
    public sealed class Z03InteractionNode : MonoBehaviour
    {
        [SerializeField] public Z03Action accion;
        [SerializeField] private AudioClip sonido;
        private AudioSource audioNodo;
        private void Awake()
        {
            if (sonido != null)
            {
                audioNodo=GetComponent<AudioSource>();
                if(audioNodo==null) audioNodo=gameObject.AddComponent<AudioSource>();
                audioNodo.playOnAwake=false; audioNodo.spatialBlend=.85f;
                audioNodo.minDistance=1.3f; audioNodo.maxDistance=14f;
            }
        }
        public string Etiqueta
        {
            get
            {
                switch(accion)
                {
                    case Z03Action.Vigilancia: return "Restablecer vigilancia";
                    case Z03Action.Energia: return "Restablecer energía";
                    case Z03Action.Terminal: return "Autorizar terminal central";
                    default: return "Acceder a servidores";
                }
            }
        }
        public void Activar()
        {
            if(accion==Z03Action.Terminal){var ui=FindFirstObjectByType<Z03Interface>();if(ui){ui.OpenTerminal();if(audioNodo && sonido)audioNodo.PlayOneShot(sonido);return;}}
            if(audioNodo && sonido) audioNodo.PlayOneShot(sonido);
            var control=FindFirstObjectByType<Z03ZoneGameplay>();
            if(control) control.Interactuar(accion);
        }
    }
}
