using UnityEngine;

namespace Circus
{
    /// <summary>Raises the VHS interference when the Zatan is near (and visible in the world) or a camera watches the player.</summary>
    public class InterferenceDriver : MonoBehaviour
    {
        [SerializeField] float smoothing = 4f;

        void Update()
        {
            var t = Tunables.I;
            float target = 0f;
            if (t.vhsInterference)
            {
                var p = FirstPersonController.I;
                var z = ZatanBrain.I;
                if (p && z && z.State != ZatanState.Dormant && ZatanIsPresent(z))
                {
                    float d = Vector3.Distance(p.transform.position, z.transform.position);
                    float k = Mathf.Clamp01(1f - d / t.interferenceNearDistance);
                    target = Mathf.Max(target, Mathf.Pow(k, 1.5f) * t.interferenceProximityMax);
                }
                target = Mathf.Max(target, SecurityCamera.MaxWatch01() * t.interferenceCameraWatch);
            }
            VHSGlobals.CameraWatching = SecurityCamera.MaxWatch01();
            VHSGlobals.Interference = Mathf.Lerp(VHSGlobals.Interference, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
        }

        static bool ZatanIsPresent(ZatanBrain z)
        {
            var r = z.GetComponentInChildren<Renderer>();
            return r != null && r.enabled;
        }
    }
}
