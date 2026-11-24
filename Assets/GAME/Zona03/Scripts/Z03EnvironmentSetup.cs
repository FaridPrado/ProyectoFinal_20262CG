using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace ControlRoomZ03 {
    [DefaultExecutionOrder(-500)]
    public sealed class Z03EnvironmentSetup : MonoBehaviour {
        private NavMeshDataInstance navInstance;
        private NavMeshData navData;
        void Start() {
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
        private void BuildNavigation() {
            Physics.SyncTransforms();
            var sources=new List<NavMeshBuildSource>();
            foreach(var c in FindObjectsByType<Collider>(FindObjectsSortMode.None)) {
                if(!c.enabled || c.isTrigger || c.gameObject.scene!=gameObject.scene || c.GetComponentInParent<PlayerController>() || c.GetComponentInParent<Z03SecurityAgent>())continue;
                // Las hojas de la salida se apartan al autorizar el acceso.
                var nodo = c.GetComponentInParent<Z03InteractionNode>();
                if (nodo && nodo.accion == Z03Action.Salida) continue;
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
        void OnDestroy(){if(navInstance.valid)navInstance.Remove();if(navData)Destroy(navData);}
    }
}
