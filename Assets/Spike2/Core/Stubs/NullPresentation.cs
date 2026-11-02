using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spike2.Core.Stubs
{
    /// <summary>Lines go to the spike 1 message line (subtitle) and the log; onDone fires at once.</summary>
    public class NullAnnouncer : IAnnouncer
    {
        public bool IsSpeaking => false;
        public void Zatan(string text, Action onDone = null) => Say("zatan", "♪ " + text.ToUpperInvariant(), onDone, text);
        public void Boss(string text, Action onDone = null) => Say("boss", "RÁDIO: " + text, onDone, text);
        public void Laugh() => Say("zatan", "♪ (RISADA)", null, "(risada)");
        public void PlayMusic(string cueId) => NullServices.Log($"music {cueId}");
        public void StopMusic() { }
        public void SetMusicMix(float volume01, float clarity01) { }

        static void Say(string speaker, string subtitle, Action onDone, string raw)
        {
            NullServices.Log($"{speaker}: {raw}");
            Circus.EventBus.RaiseMessage(subtitle, 4f);
            Bus.Publish(new Announced { speaker = speaker, text = raw });
            onDone?.Invoke();
        }
    }

    public class NullAmbushWarning : IAmbushWarning
    {
        public bool Warn(Vector3 ambushPos)
        {
            Bus.Publish(new AmbushWarned { pos = ambushPos, kind = "interference" });
            return false;
        }
    }

    /// <summary>Before the capture system exists: publish PlayerCaught and restart the scene (spike 1 behaviour).</summary>
    public class NullCaptureDirector : ICaptureDirector
    {
        public bool InCapture { get; private set; }
        public bool NumberMode { get; set; } = true;
        public bool DeathMode { get; set; }

        public void Capture(string cause)
        {
            if (InCapture || Circus.Tunables.I.godMode) return;
            InCapture = true;
            var p = Services.Get<IPlayer>();
            Bus.Publish(new PlayerCaught { cause = cause, pos = p.Position, act = GameContext.Act });
            NullServices.Log($"caught ({cause}) -> restart scene");
            if (Application.isPlaying) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void Force(CaptureNumber number) => NullServices.Log($"force capture {number}");
    }
}
