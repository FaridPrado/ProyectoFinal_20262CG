using UnityEngine;
using UnityEngine.Rendering;
namespace ControlRoomZ03 {
    [DefaultExecutionOrder(-100)]
    public sealed class Z03FirstPersonCamera : MonoBehaviour {
        [SerializeField] private Transform jugador;
        [SerializeField,Range(.3f,3f)] private float sensibilidad=1f;
        public static bool Paused {get;private set;}
        private float pitch;
        private Z03PlayerMovement player;
        public static void SetPaused(bool value) {
            Paused=value; Time.timeScale=value?0f:1f;
            Cursor.lockState=value?CursorLockMode.None:CursorLockMode.Locked; Cursor.visible=value;
        }
        void Awake() {
            Paused=false; Time.timeScale=1;
            if(!jugador)jugador=FindFirstObjectByType<Z03PlayerMovement>()?.transform;
            if(jugador) {
                player=jugador.GetComponent<Z03PlayerMovement>();
                foreach(var t in jugador.GetComponentsInChildren<Transform>())t.gameObject.layer=2;
                // Keep the animated body visible when looking down; hide head from its own camera.
                foreach(var r in jugador.GetComponentsInChildren<Renderer>())
                    if(r.name.ToLowerInvariant().Contains("casco") || r.name.ToLowerInvariant().Contains("cabeza") || r.name.ToLowerInvariant().Contains("visor"))
                        r.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            }
            var cam=GetComponent<Camera>(); if(cam){cam.nearClipPlane=.05f;cam.fieldOfView=75;}
        }
        void Start(){SetPaused(false);}
        void Update() {
            if(Z03Input.Down(KeyCode.Escape))SetPaused(!Paused);
            if(Paused || !jugador || (player && player.MovimientoBloqueado))return;
            if(Cursor.lockState!=CursorLockMode.Locked) {if(Z03Input.Click)SetPaused(false);return;}
            Vector2 d=Z03Input.Look*sensibilidad*PlayerPrefs.GetFloat("LA.Sensitivity",1f);
            if(PlayerPrefs.GetInt("LA.InvertY",0)==1)d.y=-d.y;
            pitch=Mathf.Clamp(pitch-d.y,-78,78);
            jugador.Rotate(0,d.x,0);
        }
        void LateUpdate() {
            if(!jugador)return;
            transform.position=jugador.position+Vector3.up*.7f;
            transform.rotation=Quaternion.Euler(pitch,jugador.eulerAngles.y,0);
        }
        void OnApplicationFocus(bool focus){if(!focus)SetPaused(true);}
        void OnDisable(){Paused=false;Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
    }
}
