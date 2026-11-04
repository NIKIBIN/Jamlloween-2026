using System;
using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Tranca uma Machine do kit (a porta do trailer) até a progressão chegar numa etapa. Antes dela, usar a
    /// máquina mostra <see cref="lockedText"/> e nada acontece; da etapa em diante ela funciona normalmente.
    /// Também vale no pulo de etapa do debug (F4), porque reage a cada etapa iniciada.
    /// </summary>
    public sealed class StepLockedMachine : MonoBehaviour
    {
        [SerializeField] ProgressionDirector director;
        [SerializeField] Machine machine;
        [Tooltip("A primeira etapa em que a máquina funciona.")]
        [SerializeField] ProgressionStep unlockAt;
        [SerializeField, TextArea(1, 2)] string lockedText = "Antes de sair, preciso consertar o rádio e pegar a lanterna.";
        [Tooltip("Ao chegar na etapa, a máquina abre/liga sozinha.")]
        [SerializeField] bool openOnUnlock;
        [Tooltip("Depois de aberta, não fecha mais (o jogador não consegue usá-la de novo).")]
        [SerializeField] bool keepOpen;

        int unlockIndex;

        void Awake()
        {
            if (director == null) throw new InvalidOperationException($"[{name}] StepLockedMachine sem 'director'.");
            if (machine == null) throw new InvalidOperationException($"[{name}] StepLockedMachine sem 'machine'.");
            if (unlockAt == null) throw new InvalidOperationException($"[{name}] StepLockedMachine sem 'unlockAt'.");
            unlockIndex = IndexOf(unlockAt);
            machine.locked = true;   // trancada até o diretor dizer a etapa
        }

        void OnEnable()
        {
            director.OnStepStarted += OnStepStarted;
            machine.onRefused.AddListener(OnRefused);
            Apply(director.CurrentIndex);
        }

        void OnDisable()
        {
            director.OnStepStarted -= OnStepStarted;
            machine.onRefused.RemoveListener(OnRefused);
        }

        int IndexOf(ProgressionStep step)
        {
            for (int i = 0; i < director.Steps.Count; i++)
                if (director.Steps[i] == step) return i;
            throw new InvalidOperationException($"[{name}] a etapa '{step.name}' não está na lista do diretor.");
        }

        void OnStepStarted(ProgressionStep step, int index) => Apply(index);

        void Apply(int index)
        {
            if (index < unlockIndex)
            {
                machine.locked = true;
                return;
            }
            if (openOnUnlock && !machine.on) machine.SetOn(true);
            machine.locked = keepOpen;
        }

        void OnRefused(string why)
        {
            // Aberta para sempre (keepOpen): a recusa é silenciosa.
            if (why == "locked" && director.CurrentIndex < unlockIndex) ProgressionDirector.ShowText(lockedText);
        }
    }
}
