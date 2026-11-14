using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace ControlRoomZ03.EditorTools
{
    // Repairs only this zone's material assets, also before entering Play mode.
    // Saved properties are read before changing shaders, including missing shaders.
    [InitializeOnLoad]
    internal static class Z03MaterialCompatibility
    {
        private const string Root = "Assets/GAME/Zona03/Materials";
        private static bool queued;
        private static RenderPipelineAsset lastPipeline;

        static Z03MaterialCompatibility()
        {
            lastPipeline = GraphicsSettings.currentRenderPipeline;
            Schedule();
            EditorApplication.update += CheckPipeline;
        }

        private static void CheckPipeline()
        {
            var pipeline = GraphicsSettings.currentRenderPipeline;
            if (pipeline == lastPipeline) return;
            lastPipeline = pipeline;
            Schedule();
        }

        internal static void Schedule()
        {
            if (queued) return;
            queued = true;
            EditorApplication.delayCall += Repair;
        }

        private static void Repair()
        {
            queued = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Schedule();
                return;
            }
            var pipeline = GraphicsSettings.currentRenderPipeline;
            bool urp = pipeline && pipeline.GetType().Name == "UniversalRenderPipelineAsset";
            if (pipeline && !urp) return;
            if (!AssetDatabase.IsValidFolder(Root)) return;
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material) continue;
                bool screen = material.name.StartsWith("Z03_Pantalla") || material.name == "Z03_Emblema";
                bool particle = material.name == "Z03_Particulas";
                string target = screen ? (urp ? "Universal Render Pipeline/Unlit" : "Unlit/Texture")
                    : particle ? (urp ? "Universal Render Pipeline/Particles/Unlit" : "Particles/Standard Unlit")
                    : (urp ? "Universal Render Pipeline/Lit" : "Standard");
                Shader shader = Shader.Find(target);
                if (!shader || material.shader == shader) continue;
                var saved = new SerializedObject(material);
                var textures = new Dictionary<string, Texture>();
                var scales = new Dictionary<string, Vector2>();
                var offsets = new Dictionary<string, Vector2>();
                var colors = new Dictionary<string, Color>();
                var floats = new Dictionary<string, float>();
                var array = saved.FindProperty("m_SavedProperties.m_TexEnvs");
                for (int i = 0; array != null && i < array.arraySize; i++)
                {
                    var pair = array.GetArrayElementAtIndex(i);
                    string key = pair.FindPropertyRelative("first").stringValue;
                    var value = pair.FindPropertyRelative("second");
                    textures[key] = value.FindPropertyRelative("m_Texture").objectReferenceValue as Texture;
                    scales[key] = value.FindPropertyRelative("m_Scale").vector2Value;
                    offsets[key] = value.FindPropertyRelative("m_Offset").vector2Value;
                }
                array = saved.FindProperty("m_SavedProperties.m_Colors");
                for (int i = 0; array != null && i < array.arraySize; i++)
                {
                    var pair = array.GetArrayElementAtIndex(i);
                    colors[pair.FindPropertyRelative("first").stringValue] = pair.FindPropertyRelative("second").colorValue;
                }
                array = saved.FindProperty("m_SavedProperties.m_Floats");
                for (int i = 0; array != null && i < array.arraySize; i++)
                {
                    var pair = array.GetArrayElementAtIndex(i);
                    floats[pair.FindPropertyRelative("first").stringValue] = pair.FindPropertyRelative("second").floatValue;
                }
                string source = textures.TryGetValue("_BaseMap", out Texture baseMap) && baseMap ? "_BaseMap" : "_MainTex";
                material.shader = shader;
                material.shaderKeywords = new string[0];
                foreach (var pair in textures)
                    if (material.HasProperty(pair.Key))
                    {
                        material.SetTexture(pair.Key, pair.Value);
                        material.SetTextureScale(pair.Key, scales[pair.Key]);
                        material.SetTextureOffset(pair.Key, offsets[pair.Key]);
                    }
                foreach (var pair in colors) if (material.HasProperty(pair.Key)) material.SetColor(pair.Key, pair.Value);
                foreach (var pair in floats) if (material.HasProperty(pair.Key)) material.SetFloat(pair.Key, pair.Value);
                string destination = urp ? "_BaseMap" : "_MainTex";
                if (textures.TryGetValue(source, out Texture texture))
                {
                    material.SetTexture(destination, texture);
                    material.SetTextureScale(destination, scales[source]);
                    material.SetTextureOffset(destination, offsets[source]);
                }
                Color tint = colors.TryGetValue("_BaseColor", out Color baseColor) ? baseColor
                    : colors.TryGetValue("_Color", out Color color) ? color : Color.white;
                string tintKey = urp ? "_BaseColor" : "_Color";
                if (material.HasProperty(tintKey)) material.SetColor(tintKey, screen ? Color.white : tint);
                string smoothKey = urp ? "_Smoothness" : "_Glossiness";
                if (material.HasProperty(smoothKey))
                    material.SetFloat(smoothKey, floats.TryGetValue("_Smoothness", out float smooth) ? smooth
                        : floats.TryGetValue("_Glossiness", out smooth) ? smooth : .35f);
                if (!screen)
                {
                    if (textures.TryGetValue("_BumpMap", out Texture bump) && bump) material.EnableKeyword("_NORMALMAP");
                    if (textures.TryGetValue("_MetallicGlossMap", out Texture metal) && metal) material.EnableKeyword(urp ? "_METALLICSPECGLOSSMAP" : "_METALLICGLOSSMAP");
                    if (colors.TryGetValue("_EmissionColor", out Color emission) && emission.maxColorComponent > 0)
                        material.EnableKeyword("_EMISSION");
                }
                if (particle)
                {
                    material.EnableKeyword(urp ? "_SURFACE_TYPE_TRANSPARENT" : "_ALPHABLEND_ON");
                    material.SetOverrideTag("RenderType", "Transparent");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }
                EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssetIfDirty(material);
            }
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
    }

    internal sealed class Z03MaterialImportWatcher : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
                if (path.StartsWith("Assets/GAME/Zona03/Materials/") && path.EndsWith(".mat"))
                {
                    Z03MaterialCompatibility.Schedule();
                    break;
                }
        }
    }
}
