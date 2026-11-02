using UnityEngine;
using UnityEngine.SceneManagement;

namespace Circus
{
    /// <summary>Scene-level game state: phase, caught/game over, restart.</summary>
    public class GameState : MonoBehaviour
    {
        public static GameState I { get; private set; }

        [SerializeField] float restartDelay = 3.5f;

        public GamePhase Phase { get; private set; } = GamePhase.Intro;
        public bool IsGameOver { get; private set; }
        public float GameOverTime { get; private set; }
        public bool HasBeenChased { get; set; }
        public float StartTime { get; private set; }

        void Awake() { I = this; StartTime = Time.time; }
        void OnDestroy() { if (I == this) I = null; }

        public void SetPhase(GamePhase p)
        {
            if (p == Phase) return;
            Phase = p;
            EventBus.RaisePhaseChanged(p);
        }

        /// <summary>Spike 2: when set, a capture goes here (CaptureDirector) instead of game over + restart.</summary>
        public static System.Action<string> CaughtOverride;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => CaughtOverride = null;

        public void Caught()
        {
            if (CaughtOverride != null) { CaughtOverride("spike1"); return; }
            if (IsGameOver || Tunables.I.godMode) return;
            IsGameOver = true;
            GameOverTime = Time.unscaledTime;
            EventBus.RaisePlayerCaught();
        }

        void Update()
        {
            if (IsGameOver && Time.unscaledTime - GameOverTime > restartDelay) Restart();
        }

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
