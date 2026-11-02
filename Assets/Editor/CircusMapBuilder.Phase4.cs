using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Circus.EditorTools
{
    public static partial class CircusMapBuilder
    {
        static partial void AddCameraComponents(GameObject pivot, Dictionary<string, object> c, Renderer led)
        {
            var feedGo = new GameObject("FeedCamera");
            feedGo.transform.SetParent(pivot.transform, false);
            feedGo.transform.localPosition = new Vector3(0, 0, 0.3f);
            var cam = feedGo.AddComponent<Camera>();
            cam.fieldOfView = 75f; cam.nearClipPlane = 0.1f; cam.farClipPlane = 60f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            cam.enabled = false;
            var data = feedGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.renderShadows = false;

            var sc = pivot.AddComponent<SecurityCamera>();
            sc.id = (string)c["id"];
            Set(sc, "feedCamera", cam);
            Set(sc, "led", led);
            Set(sc, "ledOff", EmissiveMat("led_off"));
            Set(sc, "ledOn", EmissiveMat("led_red"));
        }

        static partial void AddCarouselComponents(GameObject root, Transform rotor)
        {
            var cc = root.AddComponent<CarouselController>();
            Set(cc, "rotor", rotor);
        }

        static partial void AddMachineExtras(GameObject go, string id)
        {
            if (id == "M4_monitors")
            {
                var ms = go.AddComponent<MonitorStation>();
                Set(ms, "feedMaterial", FeedMaterial());
            }
            if (id == "M5_carousel_panel") go.AddComponent<CarouselLever>();
            if (id == "M1_radio") AddRadio(go);
        }

        static Material FeedMaterial()
        {
            const string path = MatDir + "/monitor_feed.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/MonitorFeed.shader"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        static void AddMechanicSystems(GameObject sys)
        {
            sys.AddComponent<InterferenceDriver>();
        }
    }
}
