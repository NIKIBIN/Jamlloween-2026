using UnityEngine;

namespace Circus
{
    /// <summary>Pickup that appears after given machines are repaired; reading it shows a short line foretelling the next event.</summary>
    public class TarotCard : MonoBehaviour, IInteractable
    {
        public string id;
        public string cardName;
        public string foretells;
        [TextArea] public string text;
        public string[] appearsAfterAny;
        public bool startsHunt;
        [SerializeField] float spinSpeed = 60f;
        [SerializeField] float bob = 0.08f;

        bool shown, taken;
        Renderer[] rends; Collider[] cols;
        Vector3 basePos;

        void Awake()
        {
            rends = GetComponentsInChildren<Renderer>();
            cols = GetComponentsInChildren<Collider>();
            basePos = transform.position;
            SetShown(false);
        }

        void OnEnable() => EventBus.MachineRepaired += OnRepaired;
        void OnDisable() => EventBus.MachineRepaired -= OnRepaired;

        void OnRepaired(string mid)
        {
            if (taken || shown) return;
            foreach (var a in appearsAfterAny) if (a == mid) { SetShown(true); return; }
        }

        void SetShown(bool v)
        {
            shown = v;
            foreach (var r in rends) r.enabled = v;
            foreach (var c in cols) c.enabled = v;
        }

        void Update()
        {
            if (!shown) return;
            transform.Rotate(0, spinSpeed * Time.deltaTime, 0, Space.World);
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f) * bob;
        }

        public bool CanInteract => shown && !taken;
        public string Prompt => "PEGAR CARTA";

        public void Interact(Interactor who)
        {
            taken = true;
            SetShown(false);
            EventBus.RaiseMessage($"<b>{cardName.ToUpperInvariant()}</b>\n<i>{text}</i>", 7f);
            if (startsHunt) PhaseDirector.I?.HuntNow();
        }
    }
}
