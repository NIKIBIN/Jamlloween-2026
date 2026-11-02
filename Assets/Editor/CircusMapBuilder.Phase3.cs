using UnityEngine;

namespace Circus.EditorTools
{
    public static partial class CircusMapBuilder
    {
        static partial void PostBuild() => VHSSetup.Run();

        static void AddVisualSystems(GameObject sys)
        {
            sys.AddComponent<LowResRenderer>();
            sys.AddComponent<OSDOverlay>();
        }
    }
}
