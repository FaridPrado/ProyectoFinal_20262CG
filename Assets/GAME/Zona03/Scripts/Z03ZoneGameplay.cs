using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.UI;

namespace ControlRoomZ03
{
    /// <summary>
    /// Control exclusivo de Zona 03: reactiva subsistemas, requiere credencial,
    /// libera la puerta, activa amenaza y guarda el punto de control JSON en PC.
    /// </summary>
    public sealed class Z03ZoneGameplay : MonoBehaviour
    {
        [Header("Referencias guardadas en 03_Control")]
        [SerializeField] private Z03PlayerMovement jugador;
        [SerializeField] private Z03SecurityAgent agente;
        [SerializeField] private Transform puertaIzquierda;
        [SerializeField] private Transform puertaDerecha;
        [SerializeField] private ParticleSystem particulasEvento;
        [SerializeField] private Light luzAcceso;
        [SerializeField] private AudioSource efectos;
        [SerializeField] private AudioClip audioConfirmacion;
        [SerializeField] private AudioClip audioAlarma;
        [Header("Integración con escenas previas")]
        [SerializeField] private string identificadorCredencial = "credencial_laboratorio";
        [SerializeField] private bool permitirDemostracionDirecta = true;
        private bool vigilancia;
        private bool energia;
        private bool terminal;
        private bool haMuerto;
        private float tiempoMensaje;
        private string mensaje;
        private string pista;
        private readonly Queue<string> avisos=new Queue<string>();
        private readonly Dictionary<string,bool> sistemas=new Dictionary<string,bool>();
        public bool Demostracion {get{return permitirDemostracionDirecta;}}
        public string Diagnostico(){
            sistemas["Vigilancia"]=vigilancia;sistemas["Energía"]=energia;sistemas["Autorización"]=terminal;
            string text="";foreach(var par in sistemas)text+=par.Key+": "+(par.Value?"OK":"PENDIENTE")+"\n";
            return text;
        }
        public void NuevaPartidaZona(){
            var gm=GameManager.Instance;
            if(gm){gm.vidaActual=gm.vidaMaxima;gm.objetivosCompletados.RemoveAll(x=>x.StartsWith("z03_"));gm.inventario.Remove("acceso_servidores");}
            vigilancia=false;energia=false;terminal=false;GuardarCheckpoint();
            Z03FirstPersonCamera.SetPaused(false);SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        private Z03HudPresenter hud;
        private Button continuarBoton;
        private Text botonTexto;
        private Vector3 posicionPuertaIzq;
        private Vector3 posicionPuertaDer;
        private static readonly string SaveName = "Zona03_Checkpoint.json";

        [Serializable]
        private class Checkpoint
        {
            public string escena = "03_Control";
            public int vida;
            public bool vigilancia;
            public bool energia;
            public bool terminal;
            public List<string> inventario = new List<string>();
            public List<string> objetivos = new List<string>();
        }
        private void Start()
        {
            if (!jugador) jugador=FindFirstObjectByType<Z03PlayerMovement>();
            if (!agente) agente=FindFirstObjectByType<Z03SecurityAgent>(FindObjectsInactive.Include);
            hud=FindFirstObjectByType<Z03HudPresenter>();
            if(puertaIzquierda) posicionPuertaIzq=puertaIzquierda.localPosition;
            if(puertaDerecha) posicionPuertaDer=puertaDerecha.localPosition;
            bool restablecido = GameManager.Instance != null &&
                GameManager.Instance.objetivosCompletados.Contains("z03_control_restaurado");
            if(restablecido) { vigilancia=true; energia=true; terminal=true; }
            else if(GameManager.Instance!=null){vigilancia=GameManager.Instance.objetivosCompletados.Contains("z03_vigilancia");energia=GameManager.Instance.objetivosCompletados.Contains("z03_energia");}
            if(agente) agente.gameObject.SetActive(restablecido);
            if (luzAcceso && restablecido) luzAcceso.color=Color.green;
            if (hud)
            {
                // Reutiliza el Canvas existente y crea sólo el despachador de eventos si falta.
                if (FindFirstObjectByType<EventSystem>() == null)
                {
                    var dispatcher = new GameObject("Eventos de la interfaz");
                    dispatcher.AddComponent<EventSystem>();
                    #if ENABLE_INPUT_SYSTEM
                    dispatcher.AddComponent<InputSystemUIInputModule>();
#else
                    dispatcher.AddComponent<StandaloneInputModule>();
#endif
                }
                Canvas canvas=hud.GetComponent<Canvas>();
                if (canvas)
                {
                    foreach(var button in canvas.GetComponentsInChildren<Button>(true))
                        if(button.name=="Panel de ayuda") { continuarBoton=button;break; }
                    if(continuarBoton)
                    {
                        continuarBoton.onClick.RemoveAllListeners();
                        continuarBoton.onClick.AddListener(IrAServidores);
                        continuarBoton.interactable=false;
                        botonTexto=continuarBoton.GetComponentInChildren<Text>(true);
                    }
                }
            }
            if (GameManager.Instance!=null)
            {
                GameManager.Instance.escenaActual="03_Control";
                if(GameManager.Instance.vidaActual<=0) GameManager.Instance.vidaActual=GameManager.Instance.vidaMaxima;
            }
            if(particulasEvento && !restablecido) particulasEvento.Stop(true,ParticleSystemStopBehavior.StopEmitting);
            GuardarCheckpoint();
            Mostrar("Reactiva VIGILANCIA y ENERGÍA; usa la TERMINAL de autorización.",6f);
        }
        public void SetInteractionHint(string valor) { pista=valor; }
        public void Interactuar(Z03Action accion)
        {
            if(haMuerto || Z03FirstPersonCamera.Paused)return;
            switch(accion)
            {
                case Z03Action.Vigilancia:
                    if(vigilancia) { Mostrar("Vigilancia operativa.");break; }
                    vigilancia=true; GameManager.Instance?.CompletarObjetivo("z03_vigilancia");
                    Reproducir(audioConfirmacion);Mostrar("Cámaras y sensores de vigilancia reactivados.");break;
                case Z03Action.Energia:
                    if(energia) { Mostrar("Energía operativa.");break; }
                    energia=true;GameManager.Instance?.CompletarObjetivo("z03_energia");
                    Reproducir(audioConfirmacion);Mostrar("Alimentación de emergencia restaurada.");break;
                case Z03Action.Terminal:
                    if(terminal) { Mostrar("La terminal ya concedió acceso.");break; }
                    if(!vigilancia || !energia) { Mostrar("ERROR: restablece vigilancia y energía antes de autorizar.",5f);break; }
                    bool credencial=GameManager.Instance!=null && GameManager.Instance.TieneObjeto(identificadorCredencial);
                    if(!credencial && !permitirDemostracionDirecta)
                    { Mostrar("ACCESO DENEGADO: falta credencial del laboratorio.",5f);break; }
                    terminal=true;GameManager.Instance?.CompletarObjetivo("z03_control_restaurado");
                    GameManager.Instance?.AgregarObjeto("acceso_servidores");
                    if(particulasEvento) particulasEvento.Play();
                    if(luzAcceso) luzAcceso.color=Color.green;
                    if(agente) agente.gameObject.SetActive(true);
                    Reproducir(audioAlarma);
                    GuardarCheckpoint();
                    Mostrar("ACCESO CONCEDIDO. Seguridad hostil activada. Huye hacia la salida.",7f);
                    break;
                case Z03Action.Salida: IrAServidores();break;
            }
        }
        private void Reproducir(AudioClip clip) { if(efectos && clip) efectos.PlayOneShot(clip); }
        private void Mostrar(string texto,float duracion=4f) {
            if(haMuerto){avisos.Clear();mensaje=texto;tiempoMensaje=Time.time+duracion;return;}
            if(Time.time<tiempoMensaje){avisos.Enqueue(texto);return;}
            mensaje=texto;tiempoMensaje=Time.time+duracion;
        }
        private void Update()
        {
            if(!haMuerto && Time.time>=tiempoMensaje && avisos.Count>0){mensaje=avisos.Dequeue();tiempoMensaje=Time.time+4f;}
            if(terminal)
            {
                if(puertaIzquierda) puertaIzquierda.localPosition=Vector3.Lerp(puertaIzquierda.localPosition,posicionPuertaIzq+Vector3.left*1.9f,Time.deltaTime*2.5f);
                if(puertaDerecha) puertaDerecha.localPosition=Vector3.Lerp(puertaDerecha.localPosition,posicionPuertaDer+Vector3.right*1.9f,Time.deltaTime*2.5f);
            }
            var gm=GameManager.Instance;
            if(gm!=null && gm.vidaActual<=0 && !haMuerto)
            {
                haMuerto=true;
                if(jugador) jugador.MovimientoBloqueado=true;
                Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
                Mostrar("DERROTA. Pulsa R o el botón para reiniciar desde el checkpoint.",10000f);
            }
            if(hud)
            {
                string objetivo=haMuerto?"SIN VIDA — PULSA R PARA REINICIAR":terminal?"ACCESO LIBERADO — ESCAPA A SERVIDORES":
                    "SISTEMAS: Vigilancia "+(vigilancia?"OK":"PENDIENTE")+" | Energía "+(energia?"OK":"PENDIENTE");
                hud.SetObjective(objetivo);
                hud.SetStatus("VIDA: "+(gm!=null?gm.vidaActual:100)+" / "+(gm!=null?gm.vidaMaxima:100) + (terminal?"   |  ALERTA":"   |  CONTROL"));
                hud.SetHelp(haMuerto?"R: REINICIAR DESDE CHECKPOINT":
                    Time.time<tiempoMensaje?mensaje:
                    terminal?"SALIDA AL NORTE — E / CLIC EN PUERTA | ESC: PAUSA":
                    pista+"    ·    WASD · SHIFT correr · ESC pausa");
            }
            if(continuarBoton)
            {
                continuarBoton.interactable=terminal || haMuerto;

            }
            if(haMuerto && Z03Input.Down(KeyCode.R)) Reiniciar();
        }
        private string RutaGuardado {get{return Path.Combine(Application.streamingAssetsPath,SaveName);}}
        public void GuardarCheckpoint()
        {
            var gm=GameManager.Instance;
            if(!gm)return;
            var cp=new Checkpoint{escena=SceneManager.GetActiveScene().name,vida=gm.vidaActual,vigilancia=vigilancia,energia=energia,terminal=terminal,
                inventario=new List<string>(gm.inventario),objetivos=new List<string>(gm.objetivosCompletados)};
            try{Directory.CreateDirectory(Application.streamingAssetsPath);File.WriteAllText(RutaGuardado,JsonUtility.ToJson(cp,true));}
            catch(Exception ex){ Debug.LogWarning("Zona 03: no se pudo guardar JSON: "+ex.Message); }
        }
        public void Reiniciar()
        {
            Z03FirstPersonCamera.SetPaused(false);
            var gm=GameManager.Instance;
            if(gm!=null)
            {
                try
                {
                    if(File.Exists(RutaGuardado))
                    {
                        var cp=JsonUtility.FromJson<Checkpoint>(File.ReadAllText(RutaGuardado));
                        gm.vidaActual=Mathf.Max(1,cp.vida);
                        gm.inventario=new List<string>(cp.inventario??new List<string>());
                        gm.objetivosCompletados=new List<string>(cp.objetivos??new List<string>());
                    }
                    else gm.vidaActual=gm.vidaMaxima;
                }
                catch(Exception ex) { Debug.LogWarning("Zona 03: fallo al cargar checkpoint: "+ex.Message);gm.vidaActual=gm.vidaMaxima; }
                gm.CambiarEscena(SceneManager.GetActiveScene().name);
            }
            else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public void IrAServidores()
        {
            if(haMuerto){Reiniciar();return;}
            if(!terminal) {Mostrar("ACCESO BLOQUEADO: activa la terminal central.");return;}
            if(jugador && Vector3.Distance(jugador.transform.position,new Vector3(0,0,26))>4f){Mostrar("Acércate a la puerta de Servidores al norte.");return;}
            if(!Application.CanStreamedLevelBeLoaded("04_Servidores")){Mostrar("Añade 04_Servidores a Build Profiles > Scene List.",6);return;}
            GuardarCheckpoint();
            Z03FirstPersonCamera.SetPaused(false);
            if(GameManager.Instance!=null) GameManager.Instance.CambiarEscena("04_Servidores");
            else SceneManager.LoadScene("04_Servidores");
        }
    }
}
