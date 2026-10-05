using System;
using System.Collections.Generic;
using System.IO;
using SailorsRest.Rules;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SailorsRest
{
    /// <summary>
    /// The one save slot, shared by every scene. Loads lazily from disk, saves on every scene change
    /// and whenever a scene calls <see cref="Save"/>.
    /// </summary>
    public static class GameSession
    {
        public const string MapScene = "Map";
        public const string PondScene = "ShorePond";
        public const string MarketScene = "Market";

        static PlayerProgress progress;

        public static PlayerProgress Progress
        {
            get
            {
                if (progress == null) Load();
                return progress;
            }
        }

        const string SaveFileName = "sailors-rest-save.json";
        const string TempSuffix = ".tmp";
        const string UnreadableSuffix = ".unreadable";

        public static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        /// <summary>Raised after coins, the creel or gear change, so open screens can refresh.</summary>
        public static event Action Changed;

        public static void NotifyChanged() => Changed?.Invoke();

        public static void Load()
        {
            progress = null;
            try
            {
                if (File.Exists(SavePath)) progress = JsonUtility.FromJson<PlayerProgress>(File.ReadAllText(SavePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Sailor's Rest: save could not be read, starting fresh. {e.Message}");
                KeepUnreadableSave();
            }
            if (progress == null) progress = new PlayerProgress();
            if (progress.creel == null) progress.creel = new List<FishData>();
        }

        /// <summary>Copies a save that failed to load aside, so the next save can't overwrite what might still be recovered.</summary>
        static void KeepUnreadableSave()
        {
            string copy = SavePath + UnreadableSuffix;
            try
            {
                File.Copy(SavePath, copy, true);
                Debug.LogWarning("Sailor's Rest: the unreadable save was kept at " + copy);
            }
            catch (Exception e) { Debug.LogWarning("Sailor's Rest: could not keep a copy of the unreadable save. " + e.Message); }
        }

        public static void Save()
        {
            if (progress == null) return;
            // Write a temp file and swap it in, so a crash mid-write never leaves a half-written save.
            string temp = SavePath + TempSuffix;
            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(progress, true));
                if (File.Exists(SavePath)) File.Replace(temp, SavePath, null);
                else File.Move(temp, SavePath);
            }
            catch (Exception e) { Debug.LogWarning("Sailor's Rest: save failed. " + e.Message); }
        }

        /// <summary>A brand-new game: fresh progress, saved, and every open screen refreshed.</summary>
        public static void ResetSave()
        {
            progress = new PlayerProgress();
            Save();
            NotifyChanged();
        }

        public static void Go(string scene)
        {
            Save();
            SceneManager.LoadScene(scene);
        }

#if UNITY_EDITOR
        // Survive "Enter Play Mode Options" with domain reload off.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            progress = null;
            Changed = null;
        }
#endif
    }
}
