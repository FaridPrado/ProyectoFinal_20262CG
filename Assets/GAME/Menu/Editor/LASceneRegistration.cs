#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
namespace LaboratorioAbandonado.Menu.Editor {
    internal static class LASceneRegistration {
        [InitializeOnLoadMethod] private static void Schedule(){EditorApplication.delayCall+=Register;}
        private static void Register(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            var old=EditorBuildSettings.scenes;
            var scenes=new List<EditorBuildSettingsScene>();var known=new HashSet<string>();
            foreach(var scene in old){
                // Drop missing entries (including the removed demonstration scene).
                if(System.IO.File.Exists(scene.path) && known.Add(scene.path))scenes.Add(new EditorBuildSettingsScene(scene.path,scene.enabled));
            }
            foreach(var name in new[]{"01_Recepcion","02_Laboratorio","03_Control","04_Servidores","00_Menu_Respaldo"}){
                string path="Assets/GAME/Scenes/"+name+".unity";
                if(!System.IO.File.Exists(path))continue;
                if(known.Add(path))scenes.Add(new EditorBuildSettingsScene(path,true));
                else foreach(var scene in scenes)if(scene.path==path)scene.enabled=true;
            }
            bool changed=scenes.Count!=old.Length;
            for(int i=0;!changed && i<old.Length;i++)changed=scenes[i].path!=old[i].path || scenes[i].enabled!=old[i].enabled;
            if(changed)EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
#endif
