using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Import settings for downloaded third-party art (KayKit characters, Kenney
    /// sprites/audio) so batch imports land configured for mobile without manual
    /// inspector work. Also a clip-name dump used to build RigPose → clip maps.
    /// </summary>
    public class EmberArtImport : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (assetPath.StartsWith("Assets/Art/Characters"))
            {
                var mi = (ModelImporter)assetImporter;
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.importAnimation = true;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.meshCompression = ModelImporterMeshCompression.Medium;
                mi.animationCompression = ModelImporterAnimationCompression.Optimal;
                mi.importCameras = false;
                mi.importLights = false;
                mi.isReadable = false;
            }
            else if (assetPath.StartsWith("Assets/Art/Environments"))
            {
                var mi = (ModelImporter)assetImporter;
                mi.animationType = ModelImporterAnimationType.None;
                mi.importAnimation = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.meshCompression = ModelImporterMeshCompression.Medium;
                mi.importCameras = false;
                mi.importLights = false;
                mi.isReadable = false;
            }
        }

        private void OnPreprocessTexture()
        {
            if (assetPath.StartsWith("Assets/Art/Characters")
                || assetPath.StartsWith("Assets/Art/Environments"))
            {
                // KayKit palette textures: tiny, no mips needed, never over 256.
                var ti = (TextureImporter)assetImporter;
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 256;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
            }
            else if (assetPath.StartsWith("Assets/Resources/Art/UI"))
            {
                var ti = (TextureImporter)assetImporter;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single; // Multiple → Load<Sprite> = null
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 512;
            }
            else if (assetPath.StartsWith("Assets/Resources/Art/VFX"))
            {
                var ti = (TextureImporter)assetImporter;
                ti.textureType = TextureImporterType.Default;
                ti.mipmapEnabled = true;
                ti.maxTextureSize = 256;
                ti.alphaIsTransparency = true;
            }
        }

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Resources/Art/Audio")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            var music = assetPath.Contains("/Music/");
            s.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = music ? 0.55f : 0.7f;
            ai.defaultSampleSettings = s;
        }

        /// <summary>
        /// Sets loop flags on the embedded KayKit clips (locomotion/idle loop,
        /// actions don't) — must run before character prefabs are built.
        /// </summary>
        public static void ConfigureCharacterClips()
        {
            string[] looping = { "idle", "walking", "running", "blocking", "aiming",
                "shooting", "spellcasting", "strafe" };
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Characters" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx")) continue;
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;
                var clips = importer.defaultClipAnimations;
                if (clips == null || clips.Length == 0) continue;
                foreach (var clip in clips)
                {
                    var lower = clip.name.ToLowerInvariant();
                    var loop = false;
                    foreach (var key in looping)
                        if (lower.Contains(key)) { loop = true; break; }
                    clip.loopTime = loop;
                    clip.loopPose = false;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
            Debug.Log("[Emberline] Character clip loop flags configured");
        }

        /// <summary>One-time repair: reimport UI sprites stuck in Multiple mode.</summary>
        [MenuItem("Emberline/Reimport UI Sprites")]
        public static void ReimportUiSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Art/UI" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.SaveAndReimport();
            }
            Debug.Log("[Emberline] UI sprites reimported as Single");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        /// <summary>Logs every animation clip inside each character FBX.</summary>
        [MenuItem("Emberline/Dump Character Clips")]
        public static void DumpClips()
        {
            var sb = new StringBuilder("[EmberClips]\n");
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { "Assets/Art/Characters" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx")) continue;
                sb.AppendLine("MODEL " + path);
                foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                    if (o is AnimationClip clip && !clip.name.StartsWith("__preview"))
                        sb.AppendLine($"  CLIP {clip.name} len={clip.length:0.00}");
            }
            var text = sb.ToString();
            Debug.Log(text);
            File.WriteAllText("Logs/ember_clips.txt", text);
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
    }
}
