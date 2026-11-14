using UnityEngine;
namespace ControlRoomZ03 {
    [RequireComponent(typeof(CharacterController))]
    public sealed class Z03PlayerMovement : MonoBehaviour {
        [SerializeField] private float velocidad=4.2f;
        [SerializeField] private float velocidadRapida=6.5f;
        [SerializeField] private float gravedad=-18f;
        [SerializeField] private Z03CharacterAnimation animacion;
        private CharacterController controller;
        private float caida;
        [SerializeField] private AudioClip pasos;
        private AudioSource audioPasos;
        private float proximoPaso;
        public bool MovimientoBloqueado {get;set;}
        public float NivelMovimiento {get;private set;}
        void Awake() {
            audioPasos=gameObject.AddComponent<AudioSource>();audioPasos.playOnAwake=false;audioPasos.spatialBlend=0;audioPasos.volume=.28f;
            controller=GetComponent<CharacterController>(); controller.height=1.8f;
            controller.radius=.5f; controller.center=Vector3.zero;
            controller.stepOffset=.3f; controller.slopeLimit=45f;
            if(!animacion)animacion=GetComponent<Z03CharacterAnimation>();
        }
        void Update() {
            Vector2 input=Vector2.zero;
            if(!MovimientoBloqueado && !Z03FirstPersonCamera.Paused && Cursor.lockState==CursorLockMode.Locked) {
                input.x=(Z03Input.Held(KeyCode.D)?1:0)-(Z03Input.Held(KeyCode.A)?1:0);
                input.y=(Z03Input.Held(KeyCode.W)?1:0)-(Z03Input.Held(KeyCode.S)?1:0);
            }
            input=Vector2.ClampMagnitude(input,1);
            bool run=Z03Input.Held(KeyCode.LeftShift)||Z03Input.Held(KeyCode.RightShift);
            if(controller.isGrounded && caida<0)caida=-2;
            caida+=gravedad*Time.deltaTime;
            Vector3 move=(transform.forward*input.y+transform.right*input.x)*(run?velocidadRapida:velocidad);
            move.y=caida; controller.Move(move*Time.deltaTime);
            NivelMovimiento=input.magnitude*(run?1.5f:1);
            if(animacion)animacion.SetSpeed(NivelMovimiento);
            if(controller.isGrounded && input.sqrMagnitude>.01f && Time.time>=proximoPaso){if(pasos)audioPasos.PlayOneShot(pasos);proximoPaso=Time.time+(run?.32f:.48f);}
        }
    }
}
