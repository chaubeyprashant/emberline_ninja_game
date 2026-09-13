using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Emberline.Enemies;

#if FIREBASE_AUTH
using Firebase.Firestore;
using Firebase.Extensions;
#endif

namespace Emberline.Core
{
    /// <summary>
    /// Synchronizes PlayerPrefs progress data with Firebase Firestore.
    /// Used to preserve progress across devices or app uninstalls.
    /// </summary>
    public class CloudSaveManager : MonoBehaviour
    {
        private static CloudSaveManager _instance;
        public static CloudSaveManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CloudSaveManager");
                    _instance = go.AddComponent<CloudSaveManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private bool _isSyncing = false;
        private const string CollectionName = "user_progress";

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveToCloud();
            }
        }

        private void OnApplicationQuit()
        {
            SaveToCloud();
        }

        /// <summary>
        /// Gathers local progress into a dictionary.
        /// </summary>
        private Dictionary<string, object> GatherLocalProgress()
        {
            var data = new Dictionary<string, object>();

            // Story & Duels
            data["story_unlocked"] = PlayerPrefs.GetInt("story_unlocked", 1);
            data["duels_unlocked"] = PlayerPrefs.GetInt("duels_unlocked", 1);
            
            for (int i = 1; i <= 20; i++)
            {
                data[$"story_stars_{i}"] = PlayerPrefs.GetInt($"story_stars_{i}", 0);
                data[$"duel_won_{i}"] = PlayerPrefs.GetInt($"duel_won_{i}", 0);
            }

            // Economy
            data["ryo"] = PlayerPrefs.GetInt("ryo", 0);
            data["ryo_total"] = PlayerPrefs.GetInt("ryo_total", 0);
            data["ember_shards"] = PlayerPrefs.GetInt("ember_shards", 0);
            data["ngplus"] = PlayerPrefs.GetInt("ngplus", 0);

            // Skill Tree
            foreach (var node in SkillTree.Nodes)
            {
                data[$"skill_{node.id}"] = PlayerPrefs.GetInt($"skill_{node.id}", 0);
            }

            // Feats
            foreach (var feat in Feats.All)
            {
                data[$"feat_{feat.id}"] = PlayerPrefs.GetInt($"feat_{feat.id}", 0);
            }

            // Cosmetics & Blades
            foreach (var s in Cosmetics.All)
            {
                data[$"cos_{s.Id}"] = PlayerPrefs.GetInt($"cos_{s.Id}", 0);
            }
            data["cos_sel"] = PlayerPrefs.GetString("cos_sel", "ash");
            data["blade_finish"] = PlayerPrefs.GetInt("blade_finish", 0);

            // Endless Mode
            data["run_best_score"] = PlayerPrefs.GetInt("run_best_score", 0);
            data["run_best_time"] = PlayerPrefs.GetFloat("run_best_time", 0f);
            data["run_best_kills"] = PlayerPrefs.GetInt("run_best_kills", 0);
            data["run_best_combo"] = PlayerPrefs.GetInt("run_best_combo", 0);
            data["run_best_depth"] = PlayerPrefs.GetInt("run_best_depth", 0);
            data["run_count"] = PlayerPrefs.GetInt("run_count", 0);
            data["run_total_kills"] = PlayerPrefs.GetInt("run_total_kills", 0);

            // Weapons
            string[] weapons = { "blade", "daggers", "bow", "bomb" };
            foreach (var w in weapons)
            {
                data[$"wup_{w}_0"] = PlayerPrefs.GetInt($"wup_{w}_0", 0);
                data[$"wup_{w}_1"] = PlayerPrefs.GetInt($"wup_{w}_1", 0);
                data[$"wup_{w}_2"] = PlayerPrefs.GetInt($"wup_{w}_2", 0);
            }

            // Story Flags
            string[] beats = { "opening", "village", "aiko", "snow" };
            foreach (var b in beats)
            {
                data[$"beat_{b}"] = PlayerPrefs.GetInt($"beat_{b}", 0);
            }
            data["intro_video"] = PlayerPrefs.GetInt("intro_video", 0);

#if FIREBASE_AUTH
            data["last_updated"] = FieldValue.ServerTimestamp;
#endif

            return data;
        }

        public void SaveToCloud()
        {
#if FIREBASE_AUTH
            if (_isSyncing || !AuthManager.Instance.IsAuthenticated || AuthManager.Instance.IsGuest)
                return;

            string uid = AuthManager.Instance.FirebaseUid;
            if (string.IsNullOrEmpty(uid)) return;

            _isSyncing = true;
            var db = FirebaseFirestore.DefaultInstance;
            var docRef = db.Collection(CollectionName).Document(uid);
            
            var data = GatherLocalProgress();

            docRef.SetAsync(data, SetOptions.MergeAll).ContinueWithOnMainThread(task =>
            {
                _isSyncing = false;
                if (task.IsFaulted)
                {
                    Debug.LogError($"[CloudSave] Failed to save progress: {task.Exception}");
                }
                else
                {
                    Debug.Log("[CloudSave] Progress saved successfully.");
                }
            });
#endif
        }

        public void LoadFromCloud(System.Action onComplete = null)
        {
#if FIREBASE_AUTH
            if (!AuthManager.Instance.IsAuthenticated || AuthManager.Instance.IsGuest)
            {
                onComplete?.Invoke();
                return;
            }

            string uid = AuthManager.Instance.FirebaseUid;
            if (string.IsNullOrEmpty(uid))
            {
                onComplete?.Invoke();
                return;
            }

            var db = FirebaseFirestore.DefaultInstance;
            var docRef = db.Collection(CollectionName).Document(uid);

            docRef.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || !task.Result.Exists)
                {
                    Debug.Log("[CloudSave] No cloud progress found or fetch failed.");
                    onComplete?.Invoke();
                    return;
                }

                var dict = task.Result.ToDictionary();
                ApplyCloudProgress(dict);
                Debug.Log("[CloudSave] Progress loaded and merged.");
                onComplete?.Invoke();
            });
#else
            onComplete?.Invoke();
#endif
        }

        private void ApplyCloudProgress(Dictionary<string, object> cloudData)
        {
            // Helper to safely get an int and merge using Math.Max to avoid data loss.
            void MergeMaxInt(string key)
            {
                if (cloudData.TryGetValue(key, out object objVal))
                {
                    int cloudVal = System.Convert.ToInt32(objVal);
                    int localVal = PlayerPrefs.GetInt(key, 0);
                    PlayerPrefs.SetInt(key, Mathf.Max(localVal, cloudVal));
                }
            }
            
            // Helper to merge float.
            void MergeMaxFloat(string key)
            {
                if (cloudData.TryGetValue(key, out object objVal))
                {
                    float cloudVal = System.Convert.ToSingle(objVal);
                    float localVal = PlayerPrefs.GetFloat(key, 0f);
                    PlayerPrefs.SetFloat(key, Mathf.Max(localVal, cloudVal));
                }
            }

            MergeMaxInt("story_unlocked");
            MergeMaxInt("duels_unlocked");

            for (int i = 1; i <= 20; i++)
            {
                MergeMaxInt($"story_stars_{i}");
                MergeMaxInt($"duel_won_{i}");
            }

            MergeMaxInt("ryo");
            MergeMaxInt("ryo_total");
            MergeMaxInt("ember_shards");
            MergeMaxInt("ngplus");

            foreach (var node in SkillTree.Nodes) MergeMaxInt($"skill_{node.id}");
            foreach (var feat in Feats.All) MergeMaxInt($"feat_{feat.id}");
            foreach (var s in Cosmetics.All) MergeMaxInt($"cos_{s.Id}");
            
            if (cloudData.TryGetValue("cos_sel", out object cosVal))
            {
                PlayerPrefs.SetString("cos_sel", cosVal.ToString());
            }

            MergeMaxInt("blade_finish");

            MergeMaxInt("run_best_score");
            MergeMaxFloat("run_best_time");
            MergeMaxInt("run_best_kills");
            MergeMaxInt("run_best_combo");
            MergeMaxInt("run_best_depth");
            MergeMaxInt("run_count");
            MergeMaxInt("run_total_kills");

            string[] weapons = { "blade", "daggers", "bow", "bomb" };
            foreach (var w in weapons)
            {
                MergeMaxInt($"wup_{w}_0");
                MergeMaxInt($"wup_{w}_1");
                MergeMaxInt($"wup_{w}_2");
            }

            string[] beats = { "opening", "village", "aiko", "snow" };
            foreach (var b in beats) MergeMaxInt($"beat_{b}");
            
            MergeMaxInt("intro_video");

            PlayerPrefs.Save();
        }
    }
}
