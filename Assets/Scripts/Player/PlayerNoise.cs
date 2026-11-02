using UnityEngine;

namespace Circus
{
    [RequireComponent(typeof(FirstPersonController))]
    public class PlayerNoise : MonoBehaviour
    {
        FirstPersonController fpc;
        float timer;

        void Awake() => fpc = GetComponent<FirstPersonController>();

        void Update()
        {
            if (!fpc.IsMoving) { timer = 0f; return; }
            var t = Tunables.I;
            timer += Time.deltaTime;
            if (timer < t.footstepNoiseInterval) return;
            timer = 0f;
            if (fpc.IsRunning) NoiseSystem.Emit(transform.position, t.runNoiseRadius, NoiseSource.Run);
            else NoiseSystem.Emit(transform.position, t.walkNoiseRadius, NoiseSource.Walk);
        }
    }
}
