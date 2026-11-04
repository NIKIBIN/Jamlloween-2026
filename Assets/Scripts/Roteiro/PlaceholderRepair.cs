using System.Collections;
using HeyHeyThere.PSXCarnivalKit;
using Jamlloween.Progression;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// PLACEHOLDER de um conserto que ainda não tem minigame (o painel da roda-gigante). E começa o conserto: o jogador fica
    /// parado alguns segundos ouvindo o som de ferramenta, depois a máquina liga. Fica no mesmo objeto do
    /// StepInteraction: só funciona na etapa dele e conclui a etapa ao terminar.
    /// Troque por um minigame de verdade (classe Minigame) quando ele existir.
    /// </summary>
    [RequireComponent(typeof(StepInteraction))]
    public sealed class PlaceholderRepair : MonoBehaviour, IInteractable, IStepForceCompletable
    {
        [SerializeField] string prompt = "Consertar painel";
        [SerializeField, Min(0.1f)] float repairSeconds = 4f;
        [Tooltip("O jogador, travado durante o conserto.")]
        [SerializeField] LevelWalker walker;
        [SerializeField] AudioSource source;
        [Tooltip("Ferramenta trabalhando, em loop durante o conserto.")]
        [SerializeField] AudioClip workLoop;
        [Tooltip("A máquina ligando, no fim.")]
        [SerializeField] AudioClip doneClip;
        [SerializeField] UnityEvent onRepaired = new UnityEvent();

        StepInteraction gate;
        bool working, done;

        public bool Done => done;
        public string Prompt => working || done ? string.Empty : prompt;
        public UnityEvent OnRepaired => onRepaired;

        void Awake()
        {
            gate = GetComponent<StepInteraction>();
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (source == null) throw new MissingReferenceException(nameof(source));
            if (workLoop == null) throw new MissingReferenceException(nameof(workLoop));
            if (doneClip == null) throw new MissingReferenceException(nameof(doneClip));
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (working || done) return;
            if (!gate.TryUse()) return;
            StartCoroutine(Repair());
        }

        IEnumerator Repair()
        {
            working = true;
            walker.enabled = false;
            source.clip = workLoop;
            source.loop = true;
            source.Play();
            yield return new WaitForSeconds(repairSeconds);
            source.Stop();
            walker.enabled = true;
            Finish();
            gate.NotifyCompleted();
        }

        void Finish()
        {
            working = false;
            done = true;
            source.PlayOneShot(doneClip);
            onRepaired.Invoke();
        }

        /// <summary>Pulo de etapas do debug: a máquina aparece consertada.</summary>
        public void ForceComplete()
        {
            if (done) return;
            StopAllCoroutines();
            if (working) { source.Stop(); walker.enabled = true; }
            Finish();
        }
    }
}
