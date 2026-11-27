using UnityEngine;

// Base comun de todo objeto con el que el jugador puede interactuar
// (terminal, rack de servidor...). El InteractionController lo detecta con
// un raycast desde el centro de la pantalla y llama a Activar() con E o clic.
// Cada objeto suena al activarse (enunciado, seccion 7: sonido) y define
// su propia accion en AlInteractuar().
[RequireComponent(typeof(Collider))]
public abstract class Interactable : MonoBehaviour
{
    [Header("Interaccion")]
    [SerializeField] protected string textoAccion = "Interactuar";
    [SerializeField] protected AudioClip sonidoActivar;
    [SerializeField] protected AudioSource fuenteAudio;   // opcional: si no hay, suena en su posicion

    // Texto que el HUD muestra cuando la mira apunta a este objeto
    public virtual string TextoAccion => textoAccion;

    // Un objeto puede dejar de estar disponible (por ejemplo, un rack ya reiniciado)
    public virtual bool PuedeInteractuar => true;

    public void Activar()
    {
        if (!PuedeInteractuar)
        {
            return;
        }

        Sonar(sonidoActivar);
        AlInteractuar();
    }

    // Lo que hace cada objeto concreto
    protected abstract void AlInteractuar();

    protected void Sonar(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (fuenteAudio != null)
        {
            fuenteAudio.PlayOneShot(clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(clip, transform.position);
        }
    }
}
