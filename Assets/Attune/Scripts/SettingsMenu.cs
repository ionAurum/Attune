using System;
using Aurumworks;
using UnityEngine;
using UnityEngine.Audio;

namespace Attune
{
    /// <summary>
    /// The settings menu, built in code on the framework's menu shell (SettingsPage): fullscreen and
    /// the volumes, kept in the framework's Settings and applied as they change. The main menu and
    /// the session menu open it over themselves (SettingsMenu.Instance.Open); Back, UI Cancel and UI
    /// Menu (Escape, the gamepad's East and Start) close it, which writes the settings to disk. Also
    /// lists the game's volumes, and keeps the mixer to them from startup on. On the Menus prefab,
    /// with SessionMenu.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        [Serializable]
        private struct VolumeControl
        {
            [Tooltip("The slider's label.")]
            [SerializeField] private string _label;
            [Tooltip("The mixer's exposed volume parameter, in decibels.")]
            [SerializeField] private string _parameter;

            public VolumeControl(string label, string parameter)
            {
                _label = label;
                _parameter = parameter;
            }

            public string Label => _label;
            public string Parameter => _parameter;
        }

        [Header("Volume")]
        [SerializeField] private AudioMixer _mixer;
        [Tooltip("A slider per mixer volume, in this order. Parameters without one stay at the mixer's own level.")]
        [SerializeField] private VolumeControl[] _volumes =
        {
            new("Master", "MasterVolume"),
            new("Music", "MusicVolume"),
            new("Effects", "EffectsVolume"),
        };

        private VolumeSetting[] _settings; // one per entry of _volumes

        /// <summary>The game's settings menu, or null before the game has started.</summary>
        public static SettingsMenu Instance { get; private set; }

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        //----------

        private void Awake()
        {
            Instance = this;
            _settings = new VolumeSetting[_volumes.Length];
            for (int i = 0; i < _volumes.Length; i++)
            {
                _settings[i] = Settings.AddVolume(_volumes[i].Label);
            }

            Settings.VolumeChanged += OnVolumeChanged;
        }

        private void OnDestroy()
        {
            Settings.VolumeChanged -= OnVolumeChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // The mixer ignores SetFloat until it's running, which it isn't yet in Awake.
        private void Start()
        {
            for (int i = 0; i < _volumes.Length; i++)
            {
                if (_mixer != null && !_mixer.SetFloat(_volumes[i].Parameter, _settings[i].Decibels))
                {
                    Debug.LogWarning($"{name}: the mixer {_mixer.name} has no exposed parameter \"{_volumes[i].Parameter}\".", this);
                }
            }
        }

        //----------

        /// <summary>
        /// Opens the menu over whatever is showing, with the values of this moment, and returns it;
        /// null before the menu stack exists.
        /// </summary>
        public RectTransform Open()
        {
            MenuStack menus = SessionMenu.Menus;
            return menus != null ? new SettingsPage { More = ProfileRows }.Open(menus) : null;
        }

        // The profile's rows (design, The hub, Settings): its name, its source, recalibrating, and the timing values.
        private static void ProfileRows(RectTransform column)
        {
            InputRouter router = InputRouter.Instance;
            if (router == null)
            {
                return;
            }

            UiTheme theme = UiKit.Theme;
            Profile profile = router.Profile;
            UiKit.Text(column, "Profile", 26f, theme.Muted);
            if (profile.Id > 0)
            {
                UiKit.InputField(column, profile.Name, name =>
                {
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        profile.Name = name.Trim();
                        router.SaveProfile();
                    }
                }, placeholder: "Name");
            }

            UiKit.Button(column, "Source: " + router.Source.Name, () => SourcePage.Open(isNew: false), 60f);
            RectTransform row = UiKit.Row(column, 60f, 16f);
            UiKit.Button(row, "Recalibrate", () => CalibrationPage.Open(), 60f);
            UiKit.Button(row, "Timing", TimingPage.Open, 60f);
        }

        /// <summary>
        /// The level the mixer plays one of the game's volumes at, in decibels; null if the volume
        /// isn't one of them, or the mixer has no parameter for it.
        /// </summary>
        public float? MixerDecibels(VolumeSetting volume)
        {
            int index = Array.IndexOf(_settings, volume);
            if (index >= 0 && _mixer != null && _mixer.GetFloat(_volumes[index].Parameter, out float decibels))
            {
                return decibels;
            }

            return null;
        }

        //----------

        private void OnVolumeChanged(VolumeSetting volume)
        {
            int index = Array.IndexOf(_settings, volume);
            if (index >= 0 && _mixer != null)
            {
                _mixer.SetFloat(_volumes[index].Parameter, volume.Decibels);
            }
        }
    }
}
