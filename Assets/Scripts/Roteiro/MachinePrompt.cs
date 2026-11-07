using System;
using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Mostra o prompt "[E] ..." do PlayerInteractor numa Machine do kit (a porta do trailer). Só o texto:
    /// quem usa a máquina no E é o LevelWalker, então Interact não faz nada (senão a porta abriria e fecharia
    /// no mesmo aperto). Fica no objeto do collider que mostra o prompt (a porta do trailer, não as paredes), abaixo da
    /// Machine; a Machine aceita o E só nessas peças pelo useParts dela.
    /// </summary>
    public sealed class MachinePrompt : MonoBehaviour, IInteractable
    {
        [SerializeField] Machine machine;
        [SerializeField] string prompt = "Interagir";

        void Awake()
        {
            if (machine == null) machine = GetComponentInParent<Machine>();
            if (machine == null) throw new InvalidOperationException($"[{name}] MachinePrompt sem 'machine'.");
        }

        public string Prompt => machine.isActiveAndEnabled ? prompt : string.Empty;

        public void Interact(PlayerInteractor interactor) { }
    }
}
