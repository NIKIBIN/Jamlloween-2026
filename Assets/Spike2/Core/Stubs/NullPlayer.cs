using Circus;
using UnityEngine;

namespace Spike2.Core.Stubs
{
    /// <summary>Wraps the spike 1 FirstPersonController/Flashlight when present; inert otherwise.</summary>
    public class NullPlayer : IPlayer
    {
        static FirstPersonController P => FirstPersonController.I;

        public bool Exists => P != null;
        public Transform Transform => P ? P.transform : null;
        public Vector3 Position => P ? P.transform.position : Vector3.zero;
        public Vector3 HeadPosition => P ? P.HeadPosition : Vector3.up * 1.7f;
        public Vector3 ChestPosition => Position + Vector3.up * 1.3f;
        public Vector3 Forward => P ? P.Forward : Vector3.forward;
        public Camera Camera => P ? P.Cam : Camera.main;
        public bool IsHidden => P && P.CurrentMode == FirstPersonController.Mode.Hidden;
        public bool IsRunning => P && P.IsRunning;
        public bool IsCrouching => false;
        public bool FlashlightOn => Flashlight.IsOn;
        public bool IsBusy => P && P.CurrentMode == FirstPersonController.Mode.Busy;
        public bool MovementLocked { get; set; }
        public void SetBusy(bool busy)
        {
            if (!P) return;
            P.SetMode(busy ? FirstPersonController.Mode.Busy : FirstPersonController.Mode.Free);
            P.LookLocked = busy;
        }
        public void Teleport(Vector3 pos, float yaw) { if (P) P.Teleport(pos, yaw); }
        public void SetFlashlight(bool on) { var f = P ? P.GetComponent<Flashlight>() : null; if (f) f.Set(on); }
    }
}
