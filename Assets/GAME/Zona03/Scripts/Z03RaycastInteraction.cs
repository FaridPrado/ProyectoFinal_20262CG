using UnityEngine;
namespace ControlRoomZ03 {
    public sealed class Z03RaycastInteraction : MonoBehaviour {
        [SerializeField] private float rango=3.2f;
        private Z03ZoneGameplay zona;
        void Awake(){zona=FindFirstObjectByType<Z03ZoneGameplay>();}
        void Update(){
            if(Z03FirstPersonCamera.Paused || Cursor.lockState!=CursorLockMode.Locked || Time.timeScale==0)return;
            var player=GetComponent<Z03PlayerMovement>(); if(player && player.MovimientoBloqueado)return;
            var cam=Camera.main;if(!cam)return;
            Z03InteractionNode node=null;
            if(Physics.Raycast(cam.transform.position,cam.transform.forward,out RaycastHit hit,rango,~(1<<2),QueryTriggerInteraction.Collide))
                node=hit.collider.GetComponentInParent<Z03InteractionNode>();
            if(zona)zona.SetInteractionHint(node?"E / CLIC: "+node.Etiqueta:"Busca los paneles señalizados");
            if(node && (Z03Input.Down(KeyCode.E)||Z03Input.Click))node.Activar();
        }
    }
}
