using UnityEngine;

namespace ControlRoomZ03
{
    /// <summary>Animación ambiental no invasiva: no controla objetivos ni GameManager.</summary>
    public sealed class Z03AmbientAnimator : MonoBehaviour
    {
        [SerializeField] private Light[] lucesIndicadoras;
        [SerializeField] private Transform[] elementosGiratorios;
        [SerializeField, Range(0f, 1f)] private float amplitud = 0.24f;
        [SerializeField] private float velocidadPulso = 1.2f;
        [SerializeField] private float velocidadRotacion = 12f;

        private float[] intensidadesIniciales;

        public void Configure(Light[] lights, Transform[] rotating)
        {
            lucesIndicadoras = lights;
            elementosGiratorios = rotating;
        }

        private void Awake()
        {
            if (lucesIndicadoras == null) return;
            intensidadesIniciales = new float[lucesIndicadoras.Length];
            for (int i = 0; i < lucesIndicadoras.Length; i++)
                intensidadesIniciales[i] = lucesIndicadoras[i] != null ? lucesIndicadoras[i].intensity : 1f;
        }

        private void Update()
        {
            if (intensidadesIniciales != null)
            {
                for (int i = 0; i < lucesIndicadoras.Length; i++)
                {
                    if (lucesIndicadoras[i] == null) continue;
                    float pulse = Mathf.Sin(Time.time * velocidadPulso * (i % 2 == 0 ? 1f : 1.4f) + i);
                    lucesIndicadoras[i].intensity = intensidadesIniciales[i] * (1f + pulse * amplitud);
                }
            }
            if (elementosGiratorios != null)
                for (int i = 0; i < elementosGiratorios.Length; i++)
                    if (elementosGiratorios[i] != null)
                        elementosGiratorios[i].Rotate(0f, Time.deltaTime * velocidadRotacion * (i % 2 == 0 ? 1 : -1), 0f, Space.Self);
        }
    }
}
