using UnityEngine;
using UnityEngine.InputSystem;

namespace Circus
{
    public class Flashlight : MonoBehaviour
    {
        public static bool IsOn { get; private set; }

        [SerializeField] Light spot;
        [SerializeField] bool startOn = true;

        void Start() => Set(startOn);

        void Update()
        {
            var t = Tunables.I;
            spot.range = t.flashlightRange;
            spot.intensity = t.flashlightIntensity;
            spot.spotAngle = t.flashlightAngle;
            spot.innerSpotAngle = t.flashlightAngle * 0.5f;

            var kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame && !DebugPanelState.IsOpen
                && FirstPersonController.I.CurrentMode != FirstPersonController.Mode.Dead)
                Set(!IsOn);
        }

        public void Set(bool on)
        {
            IsOn = on;
            spot.enabled = on;
        }

        void OnDisable() => IsOn = false;
    }
}
