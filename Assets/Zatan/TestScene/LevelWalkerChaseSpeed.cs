using System;
using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Zatan.TestScene
{
    /// <summary>
    /// Troca a velocidade de corrida do jogador (LevelWalker.run) conforme o Zatan: corrida calma fora de
    /// perseguição, corrida de fuga enquanto ele avista ou persegue. A velocidade de perseguição do Zatan
    /// é calculada a partir da corrida atual do jogador, então ele continua só um pouco mais rápido.
    /// </summary>
    public sealed class LevelWalkerChaseSpeed : MonoBehaviour
    {
        [SerializeField] LevelWalker walker;
        [SerializeField] ZatanBehaviourBrain brain;
        [Tooltip("Corrida fora de perseguição (m/s).")]
        [SerializeField, Min(0.1f)] float calmRunSpeed = 4f;
        [Tooltip("Corrida enquanto o Zatan avista ou persegue o jogador (m/s).")]
        [SerializeField, Min(0.1f)] float chaseRunSpeed = 6f;

        void Awake()
        {
            if (walker == null) throw new InvalidOperationException($"[{name}] LevelWalkerChaseSpeed sem 'walker'.");
            if (brain == null) throw new InvalidOperationException($"[{name}] LevelWalkerChaseSpeed sem 'brain'.");
        }

        void OnEnable()
        {
            brain.StateChanged += Apply;
            Apply(brain.State);
        }

        void OnDisable() => brain.StateChanged -= Apply;

        void Apply(ZatanBehaviourState state)
        {
            bool chased = state == ZatanBehaviourState.Spotted || state == ZatanBehaviourState.Chase;
            walker.run = chased ? chaseRunSpeed : calmRunSpeed;
        }
    }
}
