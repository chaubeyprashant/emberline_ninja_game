using System.Collections.Generic;
using UnityEngine;

namespace Emberline.Core
{
    /// <summary>
    /// What the open world remembers between visits: where Renzo stood, what
    /// hour it was, the weather, and which places he has found.
    ///
    /// <para>
    /// One JSON blob under one PlayerPrefs key with a version field, so it can
    /// be migrated. This is the first piece of the versioned save the roadmap
    /// asks for; the campaign's own keys are untouched, and when the whole
    /// profile moves to a single SaveData this blob folds into it.
    /// </para>
    /// </summary>
    public static class WorldState
    {
        private const string Key = "world_state";
        public const int Version = 1;

        [System.Serializable]
        public class Data
        {
            public int version = Version;
            public bool hasPosition;
            public float x, y, z;
            public float hour = 7f;
            public int weather;
            public List<string> discovered = new();
        }

        private static Data _data;

        public static Data Current
        {
            get
            {
                if (_data != null) return _data;
                var json = PlayerPrefs.GetString(Key, "");
                _data = string.IsNullOrEmpty(json) ? new Data() : JsonUtility.FromJson<Data>(json) ?? new Data();
                if (_data.version < Version) Migrate(_data);
                return _data;
            }
        }

        private static void Migrate(Data d)
        {
            // Nothing to migrate yet; the field exists so the first real change
            // has somewhere to go.
            d.version = Version;
        }

        /// <summary>Push the live world into the record and write it.</summary>
        public static void Save(Vector3 playerPos)
        {
            var d = Current;
            d.hasPosition = true;
            d.x = playerPos.x; d.y = playerPos.y; d.z = playerPos.z;
            if (TimeOfDay.Instance != null) d.hour = TimeOfDay.Instance.Hour;
            if (WeatherSystem.Instance != null) d.weather = (int)WeatherSystem.Instance.State;
            d.discovered.Clear();
            d.discovered.AddRange(WorldLandmarks.Discovered);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(d));
            PlayerPrefs.Save();
        }

        /// <summary>Apply the record to the live world (explore start).</summary>
        public static void Load()
        {
            var d = Current;
            WorldLandmarks.Discovered.Clear();
            foreach (var id in d.discovered) WorldLandmarks.Discovered.Add(id);
            if (TimeOfDay.Instance != null) TimeOfDay.Instance.SetHour(d.hour);
            if (WeatherSystem.Instance != null) WeatherSystem.Instance.SetState((WeatherState)d.weather, instant: true);
        }

        public static bool TryGetPosition(out Vector3 pos)
        {
            var d = Current;
            pos = new Vector3(d.x, d.y, d.z);
            return d.hasPosition;
        }
    }
}
