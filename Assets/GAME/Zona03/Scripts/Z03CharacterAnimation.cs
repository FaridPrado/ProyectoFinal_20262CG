using UnityEngine;

namespace ControlRoomZ03
{
    public sealed class Z03CharacterAnimation : MonoBehaviour
    {
        private static readonly int Velocidad = Animator.StringToHash("Velocidad");
        private const float DuracionGolpe = 0.4f;
        private Animator animador;
        private Transform brazo;
        private Transform antebrazo;
        private float inicioGolpe = -DuracionGolpe;

        private void Awake()
        {
            animador = GetComponentInChildren<Animator>();
            brazo = animador.GetBoneTransform(HumanBodyBones.RightUpperArm);
            antebrazo = animador.GetBoneTransform(HumanBodyBones.RightLowerArm);
        }

        public void SetSpeed(float velocidad)
        {
            animador.SetFloat(Velocidad, velocidad);
        }

        public void Attack()
        {
            inicioGolpe = Time.time;
        }

        private void LateUpdate()
        {
            float fase = (Time.time - inicioGolpe) / DuracionGolpe;
            if (fase >= 1f || !brazo || !antebrazo || Time.timeScale == 0f) return;

            // Extiende el brazo sobre la pose de marcha y lo devuelve al terminar el golpe.
            var alcance = Quaternion.FromToRotation(antebrazo.position - brazo.position, transform.forward);
            brazo.rotation = Quaternion.Slerp(Quaternion.identity, alcance, Mathf.Sin(fase * Mathf.PI)) * brazo.rotation;
        }
    }
}
