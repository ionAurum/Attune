using UnityEngine;
using UnityEngine.SceneManagement;

namespace Attune
{
    /// <summary>
    /// Static: the demo under way, if any, and the way between demos and the hub: Play loads a
    /// demo's scene, Next the one after it (on the pitch path while playing the pitch), QuitToHub the
    /// hub. A visit lasts from a demo's loading to leaving it; feedback asks once per visit (ruling
    /// A21).
    /// </summary>
    public static class DemoSession
    {
        private static float _startedAt;

        /// <summary>The demo under way, or null on the hub.</summary>
        public static DemoInfo Current { get; private set; }

        /// <summary>The scene of the demo under way, built or stub, once it has started; else null.</summary>
        public static IDemoScene Scene { get; set; }

        /// <summary>True while playing the pitch path (ruling A13).</summary>
        public static bool IsPitch { get; private set; }

        /// <summary>True once this visit's feedback question has been answered or skipped.</summary>
        public static bool AskedThisVisit { get; set; }

        /// <summary>Seconds since the demo started.</summary>
        public static float SecondsInDemo => Current == null ? 0f : Time.unscaledTime - _startedAt;

        // Statics survive play mode when domain reload is disabled; reset them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Current = null;
            Scene = null;
            IsPitch = false;
            AskedThisVisit = false;
            _startedAt = 0f;
        }

        /// <summary>Starts a demo: closes the menus, hides the hub and loads its scene.</summary>
        public static void Play(DemoInfo demo, bool pitch = false)
        {
            if (demo == null)
            {
                return;
            }

            SessionMenu.Menus?.CloseAll();
            MainMenu.Instance?.Hide();
            Begin(demo, pitch);
            SceneManager.LoadScene(demo.Scene);
        }

        /// <summary>Marks a demo as under way without loading anything: a scene opened straight in the Editor.</summary>
        public static void Begin(DemoInfo demo, bool pitch = false)
        {
            Current = demo;
            IsPitch = pitch;
            AskedThisVisit = false;
            _startedAt = Time.unscaledTime;
            InputRouter.Instance?.ResetCounts();
        }

        /// <summary>The demo after this one, or null if this is the last.</summary>
        public static DemoInfo NextDemo => DemoCatalogue.After(Current, IsPitch);

        /// <summary>Goes on to the next demo, or back to the hub after the last.</summary>
        public static void Next()
        {
            DemoInfo next = NextDemo;
            if (next == null)
            {
                QuitToHub();
            }
            else
            {
                Play(next, IsPitch);
            }
        }

        /// <summary>Leaves the demo for the hub.</summary>
        public static void QuitToHub()
        {
            Current = null;
            IsPitch = false;
            InputRouter.Instance?.WantGestures(null);
            SessionMenu.Menus?.CloseAll();
            SceneManager.LoadScene(DemoCatalogue.HubScene);
            MainMenu.Instance?.Show();
        }
    }
}
