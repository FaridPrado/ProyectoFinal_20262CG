using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace LaboratorioAbandonado.Menu {
    // Menu module intentionally has no reference to Zona03 C# types.
    public sealed class LAMenuController : MonoBehaviour {
        [SerializeField] private GameObject canvasPrefab;
        [SerializeField] private AudioClip ambiente;
        [SerializeField] private AudioClip click;
        private static string ReadableName(string value){
            string text=value.Replace("_"," ").Trim();
            return text.Length==0?"Objeto":char.ToUpperInvariant(text[0])+text.Substring(1).ToLowerInvariant();
        }
        private AudioSource audioUI;
        [SerializeField] private string firstScene="01_Recepcion";
        private Font font;
        private RectTransform content;
        private Text status;
        private readonly Stack<string> history=new Stack<string>();
        private readonly List<GameObject> controls=new List<GameObject>();
        private string page="Inicio";
        private bool loading;
        private bool confirmNew;
        private SaveData saved;
        private float savedVolume;
        private const string SaveName="Zona03_Checkpoint.json";
        private static readonly Color Ink=new Color(.035f,.065f,.09f);
        private static readonly Color Cyan=new Color(.25f,.85f,.91f);
        private static readonly Color White=new Color(.85f,.93f,.97f);
        [Serializable] private sealed class SaveData {
            public string escena;
            public int vida;
            public bool vigilancia,energia,terminal;
            public List<string> inventario;
            public List<string> objetivos;
        }
        private void Start() {
            Time.timeScale=1;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;
            audioUI=gameObject.AddComponent<AudioSource>();audioUI.clip=ambiente;audioUI.loop=true;audioUI.playOnAwake=false;audioUI.spatialBlend=0;if(ambiente)audioUI.Play();
            savedVolume=PlayerPrefs.GetFloat("LA.Volume",.7f);AudioListener.volume=savedVolume;
            if(!GameManager.Instance)new GameObject("GameManager").AddComponent<GameManager>();
            font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas=FindFirstObjectByType<Canvas>();
            if(!canvas && canvasPrefab)canvas=Instantiate(canvasPrefab).GetComponent<Canvas>();
            if(!canvas){Debug.LogError("Menú: falta la referencia al prefab Canvas compartido.");return;}
            foreach(Transform child in canvas.transform)child.gameObject.SetActive(false);
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvas.GetComponent<CanvasScaler>();if(!scaler)scaler=canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,900);scaler.matchWidthOrHeight=.5f;
            if(!canvas.GetComponent<GraphicRaycaster>())canvas.gameObject.AddComponent<GraphicRaycaster>();
            if(!FindFirstObjectByType<EventSystem>()){
                var es=new GameObject("Eventos del menú");es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }
            RectTransform root=Box("Panel del menú",canvas.transform,0,0,1,1,Ink);
            Box("Línea superior",root,.045f,.922f,.955f,.925f,Cyan);
            Label(root,"LA / SISTEMA DE CONTENCIÓN · MENÚ DE RESPALDO",.045f,.94f,.70f,.98f,19,Cyan);
            Label(root,"PROTOCOLO 03   •   PC",.75f,.94f,.955f,.98f,16,White,TextAnchor.MiddleRight);
            Label(root,"LABORATORIO\nABANDONADO",.05f,.70f,.63f,.88f,56,White);
            Label(root,"Recupera el control. Encuentra la salida.",.053f,.64f,.64f,.70f,21,new Color(.5f,.68f,.76f));
            content=Box("Contenido",root,.05f,.13f,.65f,.62f,new Color(0,0,0,0));
            Box("Separador",root,.69f,.15f,.691f,.86f,new Color(.15f,.29f,.36f));
            Label(root,"INSTALACIÓN / RECORRIDO",.74f,.79f,.95f,.86f,20,Cyan);
            string[] zones={"01  RECEPCIÓN","02  LABORATORIO","03  SALA DE CONTROL","04  SERVIDORES"};
            for(int i=0;i<4;i++){
                float y=.68f-i*.12f;Box("Indicador de zona",root,.745f,y,.752f,y+.05f,i==2?Cyan:new Color(.2f,.35f,.42f));
                Label(root,zones[i],.77f,y-.01f,.96f,y+.06f,19,White);
            }
            Label(root,"EXPLORACIÓN · SISTEMAS · SUPERVIVENCIA",.74f,.15f,.95f,.25f,16,new Color(.5f,.68f,.76f));
            status=Label(root,"",.05f,.045f,.95f,.105f,18,new Color(.95f,.72f,.36f));
            ReadSave();Show("Inicio");
        }
        private RectTransform Box(string name,Transform parent,float x,float y,float x2,float y2,Color color){
            var go=new GameObject(ReadableName(name),typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            Place(go,x,y,x2,y2);go.GetComponent<Image>().color=color;return go.GetComponent<RectTransform>();
        }
        private static void Place(GameObject go,float x,float y,float x2,float y2){var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x2,y2);r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}
        private Text Label(Transform parent,string text,float x,float y,float x2,float y2,int size,Color color,TextAnchor align=TextAnchor.UpperLeft){
            var go=new GameObject("Texto",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);Place(go,x,y,x2,y2);
            var t=go.GetComponent<Text>();t.font=font;t.text=text;t.color=color;t.fontSize=size;t.alignment=align;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        private void Clear(){foreach(Transform t in content){t.gameObject.SetActive(false);Destroy(t.gameObject);}controls.Clear();status.text="";}
        private Button Button(string text,float x,float y,float width,UnityEngine.Events.UnityAction action,bool active=true){
            var r=Box(text,content,x,y,x+width,y+.135f,new Color(.08f,.16f,.21f));
            var button=r.gameObject.AddComponent<Button>();button.targetGraphic=r.GetComponent<Image>();button.interactable=active;
            var c=button.colors;c.normalColor=Color.white;c.highlightedColor=new Color(.38f,.86f,.92f);c.selectedColor=c.highlightedColor;c.pressedColor=new Color(.2f,.6f,.7f);c.disabledColor=new Color(.32f,.36f,.4f);button.colors=c;
            Label(r,text,.05f,0,.95f,1,21,White,TextAnchor.MiddleLeft);
            button.onClick.AddListener(()=>{if(!loading){if(audioUI && click)audioUI.PlayOneShot(click);action();}});controls.Add(r.gameObject);return button;
        }
        private void Navigate(string next){history.Push(page);Show(next);}
        private void Back(){if(history.Count>0)Show(history.Pop());else Show("Inicio");}
        private void TextPage(string heading,string text){Label(content,heading,0,.84f,1,1,27,Cyan);Label(content,text,0,.24f,1,.80f,23,White);Button("← VOLVER",0,.02f,.44f,Back);}
        private void Show(string next){
            page=next;Clear();
            switch(page){
                case "Inicio":
                    history.Clear();ReadSave();
                    Button("NUEVA PARTIDA",0,.80f,.48f,()=>{confirmNew=saved!=null;Navigate(confirmNew?"Confirmar":"Introducción");});
                    Button("CONTINUAR",.52f,.80f,.48f,Continue,saved!=null);
                    Button("INTRODUCCIÓN",0,.60f,.48f,()=>{confirmNew=false;Navigate("Introducción");});
                    Button("MECÁNICAS Y CONTROLES",.52f,.60f,.48f,()=>Navigate("Controles"));
                    Button("AJUSTES",0,.40f,.48f,()=>Navigate("Ajustes"));
                    Button("CRÉDITOS",.52f,.40f,.48f,()=>Navigate("Créditos"));
                    Button("SALIR",0,.20f,.48f,()=>Navigate("Salir"));
                    Label(content,saved!=null?"Checkpoint disponible · "+saved.escena:"Sin checkpoint guardado",0,0,1,.12f,18,new Color(.5f,.68f,.76f));break;
                case "Introducción":
                    TextPage("EL PROTOCOLO SIGUE ACTIVO","Despiertas en un laboratorio aislado. El sistema central ha bloqueado los accesos y los agentes de seguridad siguen patrullando.\n\nRecupera credenciales, restablece los sistemas y utiliza lo aprendido en cada zona para avanzar. Evita a los agentes: perder toda la vida significa volver al checkpoint.");
                    Button("COMENZAR EN RECEPCIÓN →",.48f,.02f,.52f,StartNew);break;
                case "Controles":
                    TextPage("EXPLORA · INTERACTÚA · SOBREVIVE","WASD  ·  Desplazamiento     SHIFT  ·  Correr\nRATÓN  ·  Mirar sin mantener clic derecho\nE / CLIC IZQUIERDO  ·  Activar el objeto apuntado\nESC  ·  Pausa / cursor     R  ·  Reiniciar tras derrota\n\nAcércate a paneles y puertas. Resuelve sus requisitos, rompe la línea de visión del guardia y conserva tu vida. En el menú: flechas / Tab y Enter.");break;
                case "Créditos":
                    TextPage("PROYECTO ACADÉMICO / COMPUTACIÓN GRÁFICA","Laboratorio Abandonado · ProyectoFinal_20262CG\n\nMenú y Zona 03: Farid Stiven Prado Hoyos.\nEscenas restantes: equipo del proyecto.\n\nEntorno y FirstPersonPlayer: SciFi Warehouse Kit adjunto. Los recursos conservan sus archivos y créditos originales.\nImplementación integrada para Unity 6 / PC.");break;
                case "Confirmar":
                    TextPage("NUEVA PARTIDA","Se reiniciarán la vida, el inventario y los objetivos de la sesión. El checkpoint local de Zona 03 se sustituirá al guardar de nuevo.\n\n¿Comenzar desde Recepción?");
                    Button("SÍ, COMENZAR",.48f,.02f,.52f,()=>{confirmNew=false;Show("Introducción");});break;
                case "Salir":
                    TextPage("SALIR DEL LABORATORIO","¿Quieres cerrar el juego? Solo se conservará el progreso guardado en el último checkpoint.");
                    Button("SALIR",.48f,.02f,.52f,Quit);break;
                case "Ajustes":BuildSettings();break;
            }
            if(EventSystem.current && controls.Count>0)EventSystem.current.SetSelectedGameObject(controls[0]);
        }
        private void BuildSettings(){
            Label(content,"AJUSTES",0,.87f,1,1,27,Cyan);
            Slider("Volumen general",.61f,0,1,PlayerPrefs.GetFloat("LA.Volume",.7f),value=>{AudioListener.volume=value;PlayerPrefs.SetFloat("LA.Volume",value);});
            Slider("Sensibilidad del ratón",.35f,.3f,3,PlayerPrefs.GetFloat("LA.Sensitivity",1),value=>PlayerPrefs.SetFloat("LA.Sensitivity",value));
            Button("INVERTIR Y: "+(PlayerPrefs.GetInt("LA.InvertY",0)==1?"SÍ":"NO"),0,.17f,.48f,()=>{PlayerPrefs.SetInt("LA.InvertY",1-PlayerPrefs.GetInt("LA.InvertY",0));Show("Ajustes");});
            Button("PANTALLA: "+(Screen.fullScreen?"COMPLETA":"VENTANA"),.52f,.17f,.48f,()=>{Screen.fullScreen=!Screen.fullScreen;status.text="Cambio de pantalla solicitado (en ejecutable PC).";});
            Button("← GUARDAR Y VOLVER",0,0,.64f,()=>{PlayerPrefs.Save();Back();});
        }
        private void Slider(string title,float y,float min,float max,float value,UnityEngine.Events.UnityAction<float> change){
            var label=Label(content,title+"  "+value.ToString("0.0"),0,y+.11f,1,y+.24f,21,White);
            var r=Box(title,content,.01f,y,.99f,y+.08f,new Color(.13f,.24f,.29f));
            var fillArea=Box("Área de relleno",r,.02f,.25f,.98f,.75f,new Color(0,0,0,0));var fill=Box("Relleno",fillArea,0,0,1,1,Cyan);
            var handleArea=new GameObject("Área del control",typeof(RectTransform));handleArea.transform.SetParent(r,false);Place(handleArea,.02f,0,.98f,1);
            var handle=Box("Control deslizante",handleArea.transform,0,0,0,1,White);handle.sizeDelta=new Vector2(16,0);
            var slider=r.gameObject.AddComponent<Slider>();slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handle.GetComponent<Image>();slider.minValue=min;slider.maxValue=max;slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v=>{label.text=title+"  "+v.ToString("0.0");change(v);});controls.Add(r.gameObject);
        }
        private string SavePath {get{return Path.Combine(Application.streamingAssetsPath,SaveName);}}
        private void ReadSave(){
            saved=null;
            try{if(!File.Exists(SavePath))return;var cp=JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
                if(cp!=null && cp.escena=="03_Control_Demo")cp.escena="03_Control";
                if(cp!=null && cp.vida>0 && cp.vida<=10000 && cp.escena=="03_Control" && cp.inventario!=null && cp.objetivos!=null)saved=cp;
            }catch(Exception ex){Debug.LogWarning("No se pudo leer el checkpoint: "+ex.Message);}
        }
        private bool Ready(string scene){if(Application.CanStreamedLevelBeLoaded(scene))return true;status.text="Falta "+scene+" en Scene List. Añádela en Build Profiles > Scene List.";return false;}
        private void StartNew(){
            if(!Ready(firstScene))return;
            if(confirmNew){Navigate("Confirmar");return;}
            var gm=GameManager.Instance;gm.vidaActual=gm.vidaMaxima;gm.inventario.Clear();gm.objetivosCompletados.Clear();Load(firstScene);
        }
        private void Continue(){
            ReadSave();if(saved==null){Show("Inicio");status.text="No hay un checkpoint válido.";return;}if(!Ready(saved.escena))return;
            var gm=GameManager.Instance;gm.vidaActual=Mathf.Clamp(saved.vida,1,gm.vidaMaxima);gm.inventario=new List<string>(saved.inventario);gm.objetivosCompletados=new List<string>(saved.objetivos);Load(saved.escena);
        }
        private void Load(string scene){loading=true;status.text="Cargando "+scene+"…";PlayerPrefs.Save();Time.timeScale=1;GameManager.Instance.CambiarEscena(scene);}
        private void Quit(){PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit();
#endif
        }
        private void Update(){
            if(loading || !content)return;
#if ENABLE_INPUT_SYSTEM
            bool back=Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool tab=Keyboard.current!=null && Keyboard.current.tabKey.wasPressedThisFrame;
#else
            bool back=Input.GetKeyDown(KeyCode.Escape);
            bool tab=Input.GetKeyDown(KeyCode.Tab);
#endif
            if(tab && EventSystem.current && controls.Count>0){
                int index=controls.IndexOf(EventSystem.current.currentSelectedGameObject);
                for(int count=0;count<controls.Count;count++){index=(index+1)%controls.Count;var selectable=controls[index].GetComponent<Selectable>();if(selectable && selectable.IsInteractable()){EventSystem.current.SetSelectedGameObject(controls[index]);break;}}
            }
            if(back){if(page=="Inicio")Navigate("Salir");else Back();}
        }
    }
}
