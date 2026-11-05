using System.Collections;
using System.Collections.Generic;
using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Progression
{
    /// <summary>Implementado por quem sabe se dar por concluído sem ser jogado (usado pelo pulo de etapas do debug).</summary>
    public interface IStepForceCompletable
    {
        void ForceComplete();
    }

    /// <summary>
    /// Liga uma interação (minigame, pickup, alavanca) ao diretor de progressão. Põe no mesmo objeto da interação.
    /// A interação pergunta <see cref="TryUse"/> antes de abrir e chama <see cref="NotifyCompleted"/> ao concluir.
    /// Sem diretor na cena ela fica sempre liberada.
    /// </summary>
    public class StepInteraction : MonoBehaviour
    {
        static readonly List<StepInteraction> all = new List<StepInteraction>();
        public static IReadOnlyList<StepInteraction> All => all;

        [Tooltip("Id usado nas listas de interações da etapa.")]
        [SerializeField] string interactionId;
        [Tooltip("Evento enviado ao diretor quando esta interação é concluída.")]
        [SerializeField] string completionEvent;
        [Tooltip("Texto mostrado quando o jogador usa isto fora da etapa. Vazio = textos padrão do diretor.")]
        [SerializeField, TextArea(1, 2)] string blockedText;
        [Tooltip("Para itens pegos por outro sistema (Pickup do kit): fora da etapa os colliders ficam desligados.")]
        [SerializeField] bool disableCollidersWhenInactive;
        [Tooltip("Para Pickups: a conclusão (e o que a etapa seguinte dispara, ex.: uma perseguição) só sai depois que o jogador fecha a tela de inspeção do item com E.")]
        [SerializeField] bool waitForInspectClose;
        [Tooltip("Chamado quando o debug pula esta etapa.")]
        public UnityEvent onForceCompleted;

        readonly List<Collider> disabled = new List<Collider>();
        bool forcing;

        public string InteractionId => interactionId;
        public string CompletionEvent => completionEvent;

        void Awake()
        {
            var pickup = GetComponent<Pickup>();
            if (pickup != null) pickup.onPicked.AddListener(_ => OnPicked());
        }

        void OnEnable()
        {
            all.Add(this);
            Refresh();
        }

        void OnDisable()
        {
            all.Remove(this);
            RestoreColliders();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        /// <summary>Configura por código (usado para o pickup criado em tempo de execução).</summary>
        public void Configure(string id, string completion, string blocked, bool disableColliders)
        {
            interactionId = id;
            completionEvent = completion;
            blockedText = blocked;
            disableCollidersWhenInactive = disableColliders;
            Refresh();
        }

        public bool IsActive => ProgressionDirector.IsInteractionActive(interactionId);

        /// <summary>True se a interação pode seguir. Fora da etapa mostra o texto de bloqueio (ou o da etapa, se interceptada).</summary>
        public bool TryUse() => ProgressionDirector.TryUse(interactionId, blockedText);

        public void NotifyCompleted() => ProgressionDirector.RaiseEvent(completionEvent);

        void OnPicked()
        {
            var director = ProgressionDirector.Instance;
            if (!waitForInspectClose || forcing || director == null) { NotifyCompleted(); return; }
            // O Pickup se desliga ao ser pego: a espera roda no diretor, que fica sempre ativo.
            director.StartCoroutine(NotifyAfterInspect());
        }

        /// <summary>O kit abre a inspeção logo depois do onPicked: espera um frame e então até ela fechar.</summary>
        IEnumerator NotifyAfterInspect()
        {
            yield return null;
            var inspect = FindAnyObjectByType<Inspect>();
            while (inspect != null && inspect.Showing) yield return null;
            NotifyCompleted();
        }

        public void ForceComplete()
        {
            foreach (var c in GetComponents<IStepForceCompletable>()) c.ForceComplete();
            var pickup = GetComponent<Pickup>();
            if (pickup != null && gameObject.activeSelf)
            {
                var walker = FindFirstObjectByType<LevelWalker>();
                forcing = true;   // o pulo do debug conclui na hora, sem esperar inspeção
                try { pickup.Use(walker != null ? walker.items : null); }   // o jogador passa a ter o item
                finally { forcing = false; }
            }
            onForceCompleted?.Invoke();
        }

        /// <summary>Reaplica o estado ativo/inativo. O diretor chama a cada troca de etapa.</summary>
        public void Refresh()
        {
            if (!disableCollidersWhenInactive) return;
            if (IsActive) RestoreColliders();
            else if (disabled.Count == 0)
                foreach (var c in GetComponentsInChildren<Collider>())
                    if (c.enabled) { c.enabled = false; disabled.Add(c); }
        }

        void RestoreColliders()
        {
            foreach (var c in disabled) if (c != null) c.enabled = true;
            disabled.Clear();
        }
    }
}
