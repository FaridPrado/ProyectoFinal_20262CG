using UnityEngine;

// Compuerta de evacuacion del tunel norte. Es el objeto del escenario con
// animacion funcional: usa el Animator PuertaDeslizante (estados Cerrada y
// Abierta, parametro bool "Open") que desliza las hojas LeafL y LeafR.
// La hoja lleva un NavMeshObstacle que corta el NavMesh mientras esta cerrada.
public class CompuertaSalida : MonoBehaviour
{
    private static readonly int ParamAbierta = Animator.StringToHash("Open");

    [SerializeField] private Animator animador;
    [SerializeField] private AudioSource fuenteAudio;
    [SerializeField] private AudioClip sonidoAbrir;
    [SerializeField] private Light luzSenal;     // roja cerrada, verde abierta

    private bool abierta;

    public bool Abierta => abierta;

    private void Awake()
    {
        if (luzSenal != null)
        {
            luzSenal.color = Color.red;
        }
    }

    // instantaneo = true al restaurar desde el JSON: queda abierta sin animar
    public void Abrir(bool instantaneo)
    {
        if (abierta)
        {
            return;
        }

        abierta = true;

        if (animador != null)
        {
            animador.SetBool(ParamAbierta, true);
            if (instantaneo)
            {
                animador.Play("Abierta", 0, 1f);
            }
        }

        if (luzSenal != null)
        {
            luzSenal.color = Color.green;
        }

        if (!instantaneo && fuenteAudio != null && sonidoAbrir != null)
        {
            fuenteAudio.PlayOneShot(sonidoAbrir);
        }
    }
}
