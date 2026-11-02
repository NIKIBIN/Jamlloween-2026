using UnityEngine;

namespace Circus.EditorTools
{
    public static partial class CircusMapBuilder
    {
        static partial void AddSystemExtras(GameObject sys)
        {
            AddVisualSystems(sys);
            AddMechanicSystems(sys);
            AddNarrativeSystems(sys);
            AddMapSystems(sys);
        }

        static partial void AddRadioPartial(GameObject go);
        static void AddRadio(GameObject go) => AddRadioPartial(go);
        static partial void AddNarrativeSystemsPartial(GameObject sys);
        static void AddNarrativeSystems(GameObject sys) => AddNarrativeSystemsPartial(sys);
    }
}
