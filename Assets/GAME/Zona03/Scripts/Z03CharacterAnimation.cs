using UnityEngine;

namespace ControlRoomZ03
{
    /// <summary>Animación procedural visual de extremidades, caminar/correr y ataque del agente.</summary>
    public sealed class Z03CharacterAnimation : MonoBehaviour
    {
        [SerializeField] private Transform brazoIzquierdo;
        [SerializeField] private Transform brazoDerecho;
        [SerializeField] private Transform piernaIzquierda;
        [SerializeField] private Transform piernaDerecha;
        [SerializeField] private Transform torso;
        private Vector3 torsoInicial;
        private float velocidad;
        private float ataqueHasta;
        private void Awake() { if(torso) torsoInicial=torso.localPosition; }
        public void SetSpeed(float value) { velocidad=Mathf.Clamp(value,0f,1.6f); }
        public void Attack() { ataqueHasta=Time.time+.4f; }
        private void Update()
        {
            float fase=Time.time*(velocidad>1f?12f:8.3f);
            float swing=Mathf.Sin(fase)*32f*velocidad;
            if (brazoIzquierdo) brazoIzquierdo.localRotation=Quaternion.Euler(swing*.65f,0f,0f);
            if (brazoDerecho) brazoDerecho.localRotation=Quaternion.Euler(Time.time<ataqueHasta?-78f:-swing*.65f,0f,0f);
            if (piernaIzquierda) piernaIzquierda.localRotation=Quaternion.Euler(-swing,0f,0f);
            if (piernaDerecha) piernaDerecha.localRotation=Quaternion.Euler(swing,0f,0f);
            if(torso) torso.localPosition=torsoInicial + Vector3.up * (Mathf.Abs(Mathf.Sin(fase))*.035f*velocidad);
        }
    }
}
