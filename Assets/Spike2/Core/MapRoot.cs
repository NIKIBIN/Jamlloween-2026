using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Lives on the `Map` root built by CircusMapBuilderV2. Holds the source JSON (MapData) and registers every
    /// MapId below it, including inactive ones, before any system's Awake.
    /// </summary>
    [DefaultExecutionOrder(-1100)]
    public class MapRoot : MonoBehaviour
    {
        public TextAsset source;

        void Awake()
        {
            if (source != null) MapData.Load(source.text);
            foreach (var m in GetComponentsInChildren<MapId>(true)) MapRegistry.Register(m);
        }
    }
}
