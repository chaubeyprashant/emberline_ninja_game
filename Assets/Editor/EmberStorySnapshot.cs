using System.IO;
using Emberline.Story;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Emberline.EditorTools
{
    /// <summary>
    /// Renders the story set in each dressing state, so a scene can be reviewed
    /// as an image rather than assumed to look right. Sits alongside EmberSnapshot
    /// and EmberUiSnapshot, which do the same job for the arenas and the menus.
    /// </summary>
    public static class EmberStorySnapshot
    {
        [MenuItem("Emberline/Snapshot Story")]
        public static void Run()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Opening.unity", OpenSceneMode.Single);
            var cam = Object.FindFirstObjectByType<Camera>();
            var set = Object.FindFirstObjectByType<VillageSet>();
            var rig = cam.GetComponent<CameraRig>();
            Debug.Log($"[SNAP] set={(set != null)} cam={(cam != null)} cast={Object.FindObjectsByType<CastMember>(FindObjectsSortMode.None).Length}");

            foreach (var (state, file, shot, subject) in new[]
            {
                (SetState.Peace, "Logs/story_peace.png", ShotCamera.SlowDolly, "FATHER"),
                (SetState.Attack, "Logs/story_attack.png", ShotCamera.Wide, "MOTHER"),
                (SetState.Ruin, "Logs/story_ruin.png", ShotCamera.Wide, "REN"),
                (SetState.Ruin, "Logs/story_child.png", ShotCamera.OverShoulder, "CHILD"),
            })
            {
                set.Apply(state);
                var subj = Cast.Find(subject);
                CinematicCamera.Apply(shot, subj, 2f, rig);
                // Drive one LateUpdate so the scripted shot places the camera.
                rig.SendMessage("LateUpdate", SendMessageOptions.DontRequireReceiver);
                Shoot(cam, file);
                Debug.Log($"[SNAP] {state}/{shot} on {subject} → {file}");
            }
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }

        private static void Shoot(Camera cam, string file)
        {
            var rt = new RenderTexture(1600, 900, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            File.WriteAllBytes(file, tex.EncodeToPNG());
        }
    }
}
