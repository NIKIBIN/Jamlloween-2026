using System.Collections.Generic;
using UnityEngine;

namespace Circus
{
    /// <summary>The boss talks through the trailer radio (M1). Messages queue on repairs; they play when the
    /// player is near the radio or presses E on it.</summary>
    [RequireComponent(typeof(RepairableMachine))]
    public class RadioMessages : MonoBehaviour, IInteractable
    {
        public static RadioMessages I { get; private set; }

        [SerializeField] float autoPlayDistance = 7f;
        [SerializeField] float secondsPerMessage = 7f;

        [System.Serializable]
        public struct Entry { public string afterMachine; [TextArea] public string text; }

        [SerializeField]
        Entry[] script =
        {
            new Entry { afterMachine = "M1_radio", text = "...chhh... Técnico? Aqui é o patrão. Funcionou, é? Ótimo. O Zatan, na cabine aqui do lado, parou de novo. As crianças adoram ele. Conserta." },
            new Entry { afterMachine = "M2_zatan", text = "...chhh... Ele piscou pra você? Ha. É só o mecanismo. Agora o gerador, lá no galpão norte, no fim do corredor. Sem luz, sem circo." },
            new Entry { afterMachine = "M3_generator", text = "...chhh... A luz voltou. Faltam os monitores da sala de controle, a oeste, e o painel do carrossel, a leste. Tanto faz a ordem. Não demora." },
            new Entry { afterMachine = "M4_monitors", text = "...chhh... As câmeras estão vendo tudo de novo. Tudo. ...Ele também vê, sabia?" },
            new Entry { afterMachine = "M5_carousel_panel", text = "...chhh... A música. Que saudade da música. Ele sempre gostou dela mais do que eu." },
        };

        [SerializeField, TextArea] string laughText = "...chhh... ha... haha... HAHAHA... Você achou que era você que consertava as coisas aqui? ...chhh...";

        readonly Queue<string> pending = new Queue<string>();
        RepairableMachine machine;
        float busyUntil;

        void Awake() { I = this; machine = GetComponent<RepairableMachine>(); }
        void OnDestroy() { if (I == this) I = null; }
        void OnEnable() => EventBus.MachineRepaired += OnRepaired;
        void OnDisable() => EventBus.MachineRepaired -= OnRepaired;

        public int Pending => pending.Count;

        void OnRepaired(string id)
        {
            foreach (var e in script) if (e.afterMachine == id) pending.Enqueue(e.text);
        }

        public void PlayLaugh() => Say(laughText);

        void Say(string text)
        {
            EventBus.RaiseMessage($"<color=#ffd27a>RÁDIO:</color> {text}", secondsPerMessage);
            busyUntil = Time.time + secondsPerMessage * 0.8f;
        }

        void Update()
        {
            if (!machine.IsRepaired || pending.Count == 0 || Time.time < busyUntil) return;
            var p = FirstPersonController.I;
            if (p && Vector3.Distance(p.transform.position, transform.position) < autoPlayDistance) Say(pending.Dequeue());
        }

        public bool CanInteract => machine.IsRepaired;
        public string Prompt => pending.Count > 0 ? $"OUVIR RÁDIO ({pending.Count})" : "RÁDIO (MUDO)";
        public void Interact(Interactor who) { if (pending.Count > 0) Say(pending.Dequeue()); }
    }
}
