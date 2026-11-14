using UnityEngine;
using UnityEngine.AI;
namespace ControlRoomZ03 {
    public enum Z03GuardState {Patrulla,Alerta,Persecucion,Ataque,Retorno}
    public sealed class Z03SecurityAgent : MonoBehaviour {
        [SerializeField] private float velocidad=3.2f;
        [SerializeField] private float distanciaVision=16;
        [SerializeField] private float anguloVision=115;
        [SerializeField] private float rangoAtaque=1.7f;
        [SerializeField] private int danio=12;
        [SerializeField] private float enfriamiento=1.8f;
        [SerializeField] private Transform[] puntosPatrulla;
        [SerializeField] private Z03CharacterAnimation animacion;
        [SerializeField] private AudioClip sonidoAlerta;
        [SerializeField] private AudioClip sonidoAtaque;
        [SerializeField] private AudioSource fuenteAudio;
        private NavMeshAgent nav;
        private Transform player;
        private Vector3[] route;
        private Vector3 lastKnown;
        private float lastSeen=-100;
        private float stateSince;
        private float nextAttack;
        private int index;
        private Light beacon;
        public Z03GuardState Estado {get;private set;}
        void Start(){
            player=FindFirstObjectByType<Z03PlayerMovement>()?.transform;
            var cc=GetComponent<CharacterController>();if(cc)cc.enabled=false;
            nav=GetComponent<NavMeshAgent>();if(!nav)nav=gameObject.AddComponent<NavMeshAgent>();
            nav.radius=.35f;nav.height=1.8f;nav.speed=velocidad;nav.stoppingDistance=.6f;nav.angularSpeed=220;nav.acceleration=10;
            if(NavMesh.SamplePosition(transform.position,out NavMeshHit hit,3,NavMesh.AllAreas))nav.Warp(hit.position);
            if(!nav.isOnNavMesh){Debug.LogError("Z03: el guardia no pudo colocarse sobre la navegación.");enabled=false;return;}
            route=puntosPatrulla!=null && puntosPatrulla.Length>0?System.Array.ConvertAll(puntosPatrulla,t=>t.position):new[]{new Vector3(0,0,21),new Vector3(0,0,3),new Vector3(-12,0,3),new Vector3(-12,0,15),new Vector3(0,0,15),new Vector3(12,0,15),new Vector3(12,0,3)};
            var go=new GameObject("Luz de alerta");go.transform.SetParent(transform,false);go.transform.localPosition=Vector3.up*1.8f;beacon=go.AddComponent<Light>();beacon.range=3;beacon.intensity=2;
            Estado=Z03GuardState.Retorno;Change(Z03GuardState.Patrulla);
        }
        void Update(){
            if(!player || !nav || !nav.isOnNavMesh)return;
            if(GameManager.Instance && GameManager.Instance.vidaActual<=0){nav.isStopped=true;if(animacion)animacion.SetSpeed(0);return;}
            bool sees=Visible();float distance=Vector3.Distance(transform.position,player.position);
            if(sees){lastKnown=player.position;lastSeen=Time.time;if(Estado==Z03GuardState.Patrulla || Estado==Z03GuardState.Retorno)Change(Z03GuardState.Alerta);}
            switch(Estado){
                case Z03GuardState.Patrulla:
                    Move(route[index]);if(!nav.pathPending && nav.remainingDistance<1)index=(index+1)%route.Length;break;
                case Z03GuardState.Alerta:
                    Move(lastKnown);if(Time.time-stateSince>.8f)Change(sees?Z03GuardState.Persecucion:Z03GuardState.Retorno);break;
                case Z03GuardState.Persecucion:
                    Move(lastKnown);
                    if(sees && distance<rangoAtaque)Change(Z03GuardState.Ataque);
                    else if(Time.time-lastSeen>4)Change(Z03GuardState.Retorno);break;
                case Z03GuardState.Ataque:
                    nav.isStopped=true;Vector3 dir=player.position-transform.position;dir.y=0;if(dir.sqrMagnitude>.01f)transform.rotation=Quaternion.RotateTowards(transform.rotation,Quaternion.LookRotation(dir),220*Time.deltaTime);
                    if(!sees || distance>rangoAtaque+.3f)Change(Z03GuardState.Persecucion);
                    else if(Time.time>=nextAttack){nextAttack=Time.time+enfriamiento;if(animacion)animacion.Attack();if(fuenteAudio&&sonidoAtaque)fuenteAudio.PlayOneShot(sonidoAtaque);GameManager.Instance?.RecibirDanio(danio);}break;
                case Z03GuardState.Retorno:
                    Move(route[index]);if(!nav.pathPending&&nav.remainingDistance<1)Change(Z03GuardState.Patrulla);break;
            }
            if(animacion)animacion.SetSpeed(nav.isStopped?0:Mathf.Clamp(nav.velocity.magnitude/velocidad,0,1.5f));
        }
        void Change(Z03GuardState next){
            if(Estado==next)return;Estado=next;stateSince=Time.time;
            if(beacon)beacon.color=next==Z03GuardState.Ataque||next==Z03GuardState.Persecucion?Color.red:next==Z03GuardState.Alerta?Color.yellow:Color.cyan;
            if(fuenteAudio&&sonidoAlerta)fuenteAudio.PlayOneShot(sonidoAlerta,next==Z03GuardState.Alerta?.7f:.2f);
        }
        bool Visible(){
            Vector3 delta=player.position+Vector3.up*.2f-(transform.position+Vector3.up*1.3f);
            if(delta.magnitude>distanciaVision || Vector3.Angle(transform.forward,delta)>anguloVision*.5f)return false;
            if(Physics.Raycast(transform.position+Vector3.up*1.3f,delta.normalized,out RaycastHit hit,delta.magnitude,~0,QueryTriggerInteraction.Ignore))return hit.collider.GetComponentInParent<Z03PlayerMovement>();
            return true;
        }
        void Move(Vector3 target){nav.isStopped=false;if(NavMesh.SamplePosition(target,out NavMeshHit hit,2,NavMesh.AllAreas))nav.SetDestination(hit.position);}
    }
}
