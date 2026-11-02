using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Área que o diretor ativa e desativa por etapa. Quando o jogador entra com ela ativa, toca o som,
    /// dispara onEntered e, se configurado, envia um evento de conclusão ao diretor.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class StepTrigger : MonoBehaviour
    {
        static readonly List<StepTrigger> all = new List<StepTrigger>();

        [SerializeField] string triggerId;
        [SerializeField] bool startActive;
        [Tooltip("Dispara uma vez só por ativação.")]
        [SerializeField] bool oneShot = true;
        [SerializeField] AudioClip sound;
        [Range(0f, 1f)] [SerializeField] float volume = 1f;
        [Tooltip("Evento enviado ao diretor na entrada (ex.: conclui 'Ir até o gerador'). Vazio = nenhum.")]
        [SerializeField] string completionEvent;
        public UnityEvent onEntered;

        public bool IsActive { get; private set; }
        bool inside;

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            all.Add(this);
            IsActive = startActive;
        }

        void OnDestroy() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public void SetActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            inside = false;
        }

        /// <summary>Ativa/desativa todos os triggers com este id. False se nenhum existe.</summary>
        public static bool Set(string id, bool active)
        {
            bool found = false;
            foreach (var t in all)
                if (t.triggerId == id) { t.SetActive(active); found = true; }
            return found;
        }

        // Stay (e não Enter) para disparar também quando o jogador já está dentro na hora da ativação.
        void OnTriggerStay(Collider other)
        {
            if (!IsActive || inside || !IsPlayer(other)) return;
            inside = true;
            if (oneShot) IsActive = false;
            if (sound != null) AudioSource.PlayClipAtPoint(sound, transform.position, volume);
            onEntered?.Invoke();
            ProgressionDirector.RaiseEvent(completionEvent);
        }

        void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other)) inside = false;
        }

        static bool IsPlayer(Collider c) =>
            c.GetComponentInParent<PlayerInteractor>() != null || c.GetComponentInParent<Circus.FirstPersonController>() != null;
    }
}
