using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Attune
{
    /// <summary>
    /// Static: builds a scene's systems in code as it loads (design, Building in code), so the scene
    /// holds its environment and little else: a built demo's screen in its scene, the demo stub in
    /// the stub scene.
    /// </summary>
    public static class SceneBootstrap
    {
        // Each built demo's scene, and the screen that plays it.
        private static readonly Dictionary<string, Type> _screens = new()
        {
            ["CompassSweep"] = typeof(CompassSweep),
            ["KeyboardGarden"] = typeof(KeyboardGarden),
            ["DwellBuilder"] = typeof(DwellBuilder),
            ["KiteBreath"] = typeof(KiteBreath),
            ["HumFlyer"] = typeof(HumFlyer),
        };

        // Before the first scene loads, so its sceneLoaded is heard too.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Listen()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (_screens.TryGetValue(scene.name, out Type screen))
            {
                // Opened straight in the Editor: play this scene's demo.
                if (DemoSession.Current == null || DemoSession.Current.Scene != scene.name)
                {
                    DemoSession.Begin(DemoCatalogue.Find(scene.name));
                }

                new GameObject(scene.name).AddComponent(screen);
                return;
            }

            if (scene.name != DemoCatalogue.StubScene)
            {
                return;
            }

            // Opened straight in the Editor: play the first demo that is still a stub.
            if (DemoSession.Current == null)
            {
                DemoInfo first = null;
                foreach (DemoInfo demo in DemoCatalogue.All)
                {
                    if (demo.IsStub)
                    {
                        first = demo;
                        break;
                    }
                }

                DemoSession.Begin(first ?? DemoCatalogue.All[0]);
            }

            new GameObject("Demo stub").AddComponent<DemoStub>();
        }
    }
}
