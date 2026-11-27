using UnityEngine;

// Luz roja de emergencia que pulsa. Cuando hay alarma (orden incorrecto)
// pulsa mucho mas rapido durante unos segundos. El controlador la apaga
// cuando se restablece la energia.
[RequireComponent(typeof(Light))]
public class LuzEmergencia : MonoBehaviour
{
    [SerializeField] private float intensidadMaxima = 6f;
    [SerializeField] private float velocidadNormal = 1.6f;
    [SerializeField] private float velocidadAlarma = 9f;
    [SerializeField] private float desfase;   // para que no pulsen todas a la vez

    private Light luz;
    private float finAlarma;

    private void Awake()
    {
        luz = GetComponent<Light>();
    }

    public void Alarma(float segundos)
    {
        finAlarma = Time.time + segundos;
    }

    private void Update()
    {
        float velocidad = Time.time < finAlarma ? velocidadAlarma : velocidadNormal;
        float pulso = Mathf.Abs(Mathf.Sin((Time.time + desfase) * velocidad));
        luz.intensity = intensidadMaxima * (0.25f + 0.75f * pulso);
    }
}
