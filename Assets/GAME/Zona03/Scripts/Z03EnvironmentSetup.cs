using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
namespace ControlRoomZ03 {
    [DefaultExecutionOrder(-500)]
    public sealed class Z03EnvironmentSetup : MonoBehaviour {
        private NavMeshDataInstance navInstance;
        private NavMeshData navData;
        private readonly List<Material> materials=new List<Material>();
        void Start() {
            AdaptMaterials();
            BuildNavigation();
            // Local lighting for the enlarged floor plan, independent of baked data.
            for(int x=-12;x<=12;x+=12)for(int z=-18;z<=18;z+=12) {
                var go=new GameObject("Luz del sector");go.transform.SetParent(transform);
                go.transform.position=new Vector3(x,6,z);
                var l=go.AddComponent<Light>();l.type=LightType.Point;l.range=17;l.intensity=2.3f;
                l.color=x<0?new Color(.35f,.75f,1f):x>0?new Color(1f,.65f,.28f):new Color(.65f,.9f,1f);
                l.shadows=LightShadows.None;
            }
        }
        private void AdaptMaterials() {
            var pipeline=GraphicsSettings.currentRenderPipeline;
            bool urp=pipeline && pipeline.GetType().Name=="UniversalRenderPipelineAsset";
            if(pipeline && !urp)return;
            Shader shader=Shader.Find(urp?"Universal Render Pipeline/Lit":"Standard");if(!shader)return;
            var map=new Dictionary<Material,Material>();
            foreach(var r in FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None)) {
                if(r.gameObject.scene!=gameObject.scene || r is ParticleSystemRenderer)continue;
                var list=r.sharedMaterials;bool changed=false;
                for(int i=0;i<list.Length;i++) {
                    var src=list[i];if(!src || (src.shader && src.shader.name==shader.name))continue;
                    string n=src.shader?src.shader.name:"";
                    if(n.Contains("UI/") || n.Contains("Unlit") || n.Contains("Text"))continue;
                    if(!map.TryGetValue(src,out Material dst)) {
                        dst=new Material(shader){name=src.name+"_Z03_Compatible"};
                        var tex=src.HasProperty("_BaseMap")?src.GetTexture("_BaseMap"):null;
                        if(!tex && src.HasProperty("_MainTex"))tex=src.GetTexture("_MainTex");
                        var col=src.HasProperty("_BaseColor")?src.GetColor("_BaseColor"):src.HasProperty("_Color")?src.GetColor("_Color"):Color.white;
                        string textureKey=urp?"_BaseMap":"_MainTex";
                        string sourceKey=src.HasProperty("_BaseMap") && src.GetTexture("_BaseMap")?"_BaseMap":"_MainTex";
                        dst.SetTexture(textureKey,tex);dst.SetColor(urp?"_BaseColor":"_Color",col);
                        if(src.HasProperty(sourceKey)){dst.SetTextureScale(textureKey,src.GetTextureScale(sourceKey));dst.SetTextureOffset(textureKey,src.GetTextureOffset(sourceKey));}
                        foreach(string key in new[]{"_BumpMap","_MetallicGlossMap","_OcclusionMap","_EmissionMap"})if(src.HasProperty(key)&&dst.HasProperty(key))dst.SetTexture(key,src.GetTexture(key));
                        foreach(string key in new[]{"_Metallic","_BumpScale","_OcclusionStrength"})if(src.HasProperty(key)&&dst.HasProperty(key))dst.SetFloat(key,src.GetFloat(key));
                        float smooth=src.HasProperty("_Smoothness")?src.GetFloat("_Smoothness"):src.HasProperty("_Glossiness")?src.GetFloat("_Glossiness"):.35f;
                        dst.SetFloat(urp?"_Smoothness":"_Glossiness",smooth);
                        if(src.HasProperty("_EmissionColor")){dst.SetColor("_EmissionColor",src.GetColor("_EmissionColor"));if(src.GetColor("_EmissionColor").maxColorComponent>0)dst.EnableKeyword("_EMISSION");}
                        if(dst.GetTexture("_BumpMap"))dst.EnableKeyword("_NORMALMAP");
                        if(dst.GetTexture("_MetallicGlossMap"))dst.EnableKeyword("_METALLICGLOSSMAP");
                        map.Add(src,dst);materials.Add(dst);
                    }
                    list[i]=dst;changed=true;
                }
                if(changed)r.sharedMaterials=list;
            }
        }
        private void BuildNavigation() {
            Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>();
            foreach(var c in FindObjectsByType<Collider>(FindObjectsSortMode.None)) {
                if(!c.enabled || c.isTrigger || c.gameObject.scene!=gameObject.scene || c.GetComponentInParent<Z03PlayerMovement>() || c.GetComponentInParent<Z03SecurityAgent>() || c.GetComponentInParent<Z03InteractionNode>())continue;
                if (!TryCreateNavigationSource(c, out NavMeshBuildSource source)) continue;
                sources.Add(source);
            }
            var settings=NavMesh.GetSettingsByID(0);
            if(settings.agentTypeID<0){Debug.LogError("Z03: no existe configuración de agente Humanoid.");return;}
            settings.agentRadius=.35f;settings.agentHeight=1.8f;settings.agentClimb=.25f;
            navData=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,4,3),new Vector3(40,14,58)),Vector3.zero,Quaternion.identity);
            if(navData)navInstance=NavMesh.AddNavMeshData(navData);
            else Debug.LogError("Z03: no se pudo construir la navegación.");
        }
        // Never hand a CPU-unreadable mesh to the runtime NavMesh builder.
        // Imported meshes keep Read/Write disabled; a local bounding box retains
        // their obstacle volume, including scale/rotation, in standalone builds.
        public static bool TryCreateNavigationSource(Collider collider, out NavMeshBuildSource source) {
            source = new NavMeshBuildSource { area=0, transform=collider.transform.localToWorldMatrix };
            if (collider is BoxCollider box) {
                source.shape=NavMeshBuildSourceShape.Box; source.size=box.size;
                source.transform=box.transform.localToWorldMatrix*Matrix4x4.Translate(box.center);
                return true;
            }
            if (collider is MeshCollider mesh && mesh.sharedMesh) {
                if (mesh.sharedMesh.isReadable) {
                    source.shape=NavMeshBuildSourceShape.Mesh; source.sourceObject=mesh.sharedMesh;
                } else {
                    Bounds bounds=mesh.sharedMesh.bounds;
                    source.shape=NavMeshBuildSourceShape.Box; source.size=bounds.size;
                    source.transform=mesh.transform.localToWorldMatrix*Matrix4x4.Translate(bounds.center);
                }
                return true;
            }
            if (collider is SphereCollider || collider is CapsuleCollider) {
                Bounds bounds=collider.bounds;
                source.shape=NavMeshBuildSourceShape.Box;source.size=bounds.size;
                source.transform=Matrix4x4.TRS(bounds.center,Quaternion.identity,Vector3.one);
                return true;
            }
            return false;
        }
        void OnDestroy(){if(navInstance.valid)navInstance.Remove();if(navData)Destroy(navData);foreach(var m in materials)if(m)Destroy(m);}
    }
}
