using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Attune
{
    /// <summary>
    /// Where profiles are kept (design, Profiles): each as JSON under Attune.Profile.&lt;n&gt;, the
    /// list of their numbers under Attune.Profiles, and the one last played under
    /// Attune.CurrentProfile. Through the framework's Settings, never PlayerPrefs, so a self-test
    /// keeps them in memory. A test hands it a store of its own.
    /// </summary>
    public sealed class ProfileStore
    {
        /// <summary>The key of the list of profile numbers, comma separated.</summary>
        public const string ListKey = "Attune.Profiles";

        /// <summary>The start of each profile's key.</summary>
        public const string KeyPrefix = "Attune.Profile.";

        /// <summary>The key of the profile last played.</summary>
        public const string CurrentKey = "Attune.CurrentProfile";

        private readonly Func<string, string> _get;
        private readonly Action<string, string> _set;
        private readonly Action _save;

        /// <summary>A store that reads with get (null for a key never set), writes with set, and saves with save.</summary>
        public ProfileStore(Func<string, string> get, Action<string, string> set, Action save = null)
        {
            _get = get;
            _set = set;
            _save = save;
        }

        /// <summary>The store on this machine's settings.</summary>
        public static ProfileStore OnSettings() =>
            new(key => Aurumworks.Settings.GetString(key, null), Aurumworks.Settings.SetString, Aurumworks.Settings.Save);

        /// <summary>Every profile's number, oldest first.</summary>
        public List<int> Ids
        {
            get
            {
                var ids = new List<int>();
                foreach (string part in (_get(ListKey) ?? "").Split(','))
                {
                    if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) && id > 0 && !ids.Contains(id))
                    {
                        ids.Add(id);
                    }
                }

                return ids;
            }
        }

        /// <summary>The number of the profile last played, or 0.</summary>
        public int CurrentId
        {
            get => int.TryParse(_get(CurrentKey), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0;
            set
            {
                _set(CurrentKey, value.ToString(CultureInfo.InvariantCulture));
                _save?.Invoke();
            }
        }

        /// <summary>Every profile that loads, oldest first.</summary>
        public List<Profile> All()
        {
            var all = new List<Profile>();
            foreach (int id in Ids)
            {
                Profile profile = Load(id);
                if (profile != null)
                {
                    all.Add(profile);
                }
            }

            return all;
        }

        /// <summary>A profile by number, or null if there is none or it doesn't read.</summary>
        public Profile Load(int id)
        {
            string json = _get(KeyPrefix + id.ToString(CultureInfo.InvariantCulture));
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }

            try
            {
                var profile = JsonUtility.FromJson<Profile>(json);
                if (profile == null)
                {
                    return null;
                }

                profile.Id = id;
                profile.Timing ??= new Signals.TimingValues();
                profile.Timing.Clamp();
                profile.Calibrations ??= new List<Profile.SourceCalibration>();
                return profile;
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"Profile {id} doesn't read, and is left as it is: {exception.Message}");
                return null;
            }
        }

        /// <summary>Makes a profile, named "Player n" unless given a name, and saves it.</summary>
        public Profile Create(string name = null)
        {
            List<int> ids = Ids;
            int id = 1;
            foreach (int taken in ids)
            {
                id = Math.Max(id, taken + 1);
            }

            var profile = new Profile { Id = id, Name = string.IsNullOrWhiteSpace(name) ? "Player " + id : name.Trim() };
            Save(profile);
            return profile;
        }

        /// <summary>Saves a profile, adding it to the list if it is new.</summary>
        public void Save(Profile profile)
        {
            if (profile == null || profile.Id <= 0)
            {
                return;
            }

            _set(KeyPrefix + profile.Id.ToString(CultureInfo.InvariantCulture), JsonUtility.ToJson(profile));
            List<int> ids = Ids;
            if (!ids.Contains(profile.Id))
            {
                ids.Add(profile.Id);
                _set(ListKey, string.Join(",", ids));
            }

            _save?.Invoke();
        }
    }
}
