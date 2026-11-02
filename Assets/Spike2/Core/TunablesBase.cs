using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Base of every system's tuning asset (ZatanTunables, PanelTunables...). The asset lives in
    /// &lt;SystemFolder&gt;/Resources/Tunables/&lt;TypeName&gt;.asset and is read through TunablesHub.Get&lt;T&gt;(),
    /// which hands out a runtime copy (the F1 panel never writes the asset).
    /// Every float/int with [Range] becomes a slider in F1 and every bool a toggle, in the tab named by Tab.
    /// </summary>
    public abstract class TunablesBase : ScriptableObject
    {
        /// <summary>F1 tab where the sliders appear. Default: type name without "Tunables".</summary>
        public virtual string Tab => GetType().Name.Replace("Tunables", "");
    }
}
