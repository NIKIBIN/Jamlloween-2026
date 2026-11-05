using Jamlloween.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Diretor da progressão linear. Tem a lista ordenada de etapas, ativa a atual, escuta o evento de conclusão,
    /// executa as ações de início/fim e avança. Só controla etapas, interações, prancheta, rádio e eventos simples
    /// (luzes, sons, triggers). Quem quiser reagir (ex.: o sistema do Zatan) escuta OnStepStarted / OnStepCompleted
    /// e OnNamedEvent / ZatanReleased / GeneratorRestored.
    /// </summary>
    public class ProgressionDirector : MonoBehaviour
    {
        /// <summary>Evento embutido: o rádio terminou de falar. Etapas de "reportar" concluem com ele.</summary>
        public const string RadioIdleEvent = "RadioQueueEmpty";
        /// <summary>Nome do evento nomeado que a etapa do gerador emite.</summary>
        public const string GeneratorRestoredEvent = "GeneratorRestored";
        /// <summary>Nome do evento nomeado emitido ao pegar a gasolina. O comportamento do Zatan escuta este evento.</summary>
        public const string ZatanReleasedEvent = "ZatanReleased";

        public static ProgressionDirector Instance { get; private set; }

        [Header("Etapas (em ordem)")]
        [SerializeField] List<ProgressionStep> steps = new List<ProgressionStep>();

        [Header("Prancheta")]
        [Tooltip("Linhas da prancheta, na ordem em que aparecem. As etapas mudam o estado delas com ações do tipo Task.")]
        [SerializeField] List<ClipboardTask> tasks = new List<ClipboardTask>();

        [Header("Sistemas")]
        [SerializeField] RadioDialogue radio;
        [Tooltip("Toca as ações de som. Vazio = cria um 2D.")]
        [SerializeField] AudioSource sfx;

        [Header("Interação bloqueada")]
        [SerializeField, TextArea(1, 2)] string[] defaultBlockedTexts =
        {
            "Não preciso mexer nisso agora.",
            "Tenho que voltar aqui depois."
        };
        [SerializeField] float blockedTextSeconds = 2.2f;

        [Header("Debug")]
        [Tooltip("Abre a janela para pular direto para qualquer etapa (editor e development build).")]
        [SerializeField] Key debugKey = Key.F4;

        int current = -1;
        readonly HashSet<string> satisfied = new HashSet<string>();
        TaskState[] taskStates = new TaskState[0];
        bool transitioning, finished;
        string blockedText;
        float blockedLeft;
        int blockedCycle;
        bool debugOpen;
        Vector2 debugScroll;

        public IReadOnlyList<ProgressionStep> Steps => steps;
        public IReadOnlyList<ClipboardTask> Tasks => tasks;
        /// <summary>Sobe a cada mudança na prancheta. A prancheta usa para saber quando redesenhar.</summary>
        public int TasksVersion { get; private set; }
        public TaskState GetTaskState(int index) => index >= 0 && index < taskStates.Length ? taskStates[index] : TaskState.Hidden;
        public int CurrentIndex => current;
        /// <summary>Position of a step in the roteiro, or -1.</summary>
        public int IndexOf(ProgressionStep step) => steps.IndexOf(step);
        /// <summary>The radio that plays the lines (for story objects that speak on their own, like the gondola door).</summary>
        public RadioDialogue Radio => radio;
        public ProgressionStep Current => current >= 0 && current < steps.Count ? steps[current] : null;
        /// <summary>True depois que a última etapa foi concluída.</summary>
        public bool Finished => finished;

        public event Action<ProgressionStep, int> OnStepStarted;
        public event Action<ProgressionStep, int> OnStepCompleted;
        /// <summary>Qualquer evento nomeado emitido por uma ação Event (ex.: GeneratorRestored).</summary>
        public event Action<string> OnNamedEvent;
        public event Action GeneratorRestored;
        public event Action ZatanReleased;

        void Awake()
        {
            Instance = this;
            if (radio == null) radio = GetComponent<RadioDialogue>();
            if (sfx == null)
            {
                sfx = gameObject.AddComponent<AudioSource>();
                sfx.playOnAwake = false;
                sfx.spatialBlend = 0f;
            }
        }

        void OnEnable()
        {
            if (radio != null) radio.QueueEmptied += OnRadioIdle;
        }

        void OnDisable()
        {
            if (radio != null) radio.QueueEmptied -= OnRadioIdle;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Instance = null;

        void Start()
        {
            ResetTasks();
            if (steps.Count > 0) StartStep(0);
        }

        // ------------------------------------------------------------------ prancheta

        void ResetTasks()
        {
            taskStates = new TaskState[tasks.Count];
            for (int i = 0; i < tasks.Count; i++) taskStates[i] = tasks[i].initialState;
            TasksVersion++;
        }

        void SetTask(string id, TaskState state)
        {
            bool found = false;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (tasks[i].id != id) continue;
                found = true;
                // Uma tarefa riscada não volta atrás por uma ação posterior.
                if (taskStates[i] != TaskState.Done) taskStates[i] = state;
            }
            if (!found) Debug.LogWarning($"[Progression] tarefa '{id}' nao existe na prancheta.", this);
            TasksVersion++;
        }

        void MarkDone(string eventId)
        {
            bool any = false;
            for (int i = 0; i < tasks.Count; i++)
                if (tasks[i].doneEvent == eventId && taskStates[i] != TaskState.Done) { taskStates[i] = TaskState.Done; any = true; }
            if (any) TasksVersion++;
        }

        void OnRadioIdle() => Raise(RadioIdleEvent);

        // ------------------------------------------------------------------ fluxo

        /// <summary>Evento de conclusão. Conclui a etapa atual se for o que ela espera; senão é ignorado.</summary>
        public void Raise(string eventId)
        {
            var step = Current;
            if (step == null || finished || transitioning || string.IsNullOrEmpty(eventId)) return;
            if (!step.completionEvents.Contains(eventId)) return;
            satisfied.Add(eventId);
            MarkDone(eventId);
            foreach (var e in step.completionEvents)
                if (!satisfied.Contains(e)) return;   // faltam eventos desta etapa
            CompleteCurrent();
        }

        void CompleteCurrent()
        {
            transitioning = true;
            var step = Current;
            int index = current;
            Run(step.onComplete, true);
            Debug.Log($"[Progression] etapa {index} concluida: {step.label}", this);
            OnStepCompleted?.Invoke(step, index);
            transitioning = false;

            if (index + 1 < steps.Count) StartStep(index + 1);
            else finished = true;
        }

        void StartStep(int index)
        {
            transitioning = true;
            current = index;
            finished = false;
            satisfied.Clear();
            var step = steps[index];
            RefreshInteractions();
            Run(step.onStart, true);
            Debug.Log($"[Progression] etapa {index} iniciada: {step.label}", this);
            OnStepStarted?.Invoke(step, index);
            transitioning = false;
        }

        void RefreshInteractions()
        {
            foreach (var i in StepInteraction.All.ToArray()) i.Refresh();
        }

        void Run(List<StepAction> actions, bool full)
        {
            foreach (var a in actions)
            {
                if (!full && !a.IsWorldState) continue;
                switch (a.kind)
                {
                    case StepAction.Kind.RadioLine:
                        if (radio != null) radio.Enqueue(a.line);
                        else Debug.LogWarning("[Progression] sem RadioDialogue para tocar a fala.", this);
                        break;
                    case StepAction.Kind.LightGroup:
                        if (!LightGroup.Set(a.targetId, a.state))
                            Debug.LogWarning($"[Progression] LightGroup '{a.targetId}' nao existe na cena.", this);
                        break;
                    case StepAction.Kind.Sound:
                        if (a.clip != null) sfx.PlayOneShot(a.clip, a.volume);
                        break;
                    case StepAction.Kind.Trigger:
                        if (!StepTrigger.Set(a.targetId, a.state))
                            Debug.LogWarning($"[Progression] StepTrigger '{a.targetId}' nao existe na cena.", this);
                        break;
                    case StepAction.Kind.Task:
                        SetTask(a.targetId, a.taskState);
                        break;
                    case StepAction.Kind.Event:
                        OnNamedEvent?.Invoke(a.targetId);
                        if (a.targetId == GeneratorRestoredEvent) GeneratorRestored?.Invoke();
                        if (a.targetId == ZatanReleasedEvent) ZatanReleased?.Invoke();
                        break;
                }
            }
        }

        // ------------------------------------------------------------------ interações

        /// <summary>True se a interação funciona na etapa atual. Sem diretor, tudo funciona.</summary>
        public static bool IsInteractionActive(string id)
        {
            var d = Instance;
            if (d == null || string.IsNullOrEmpty(id)) return true;
            var step = d.Current;
            return step != null && step.activeInteractions.Contains(id);
        }

        /// <summary>Chamado pela interação antes de abrir. False = não abrir (o texto já foi mostrado).</summary>
        public static bool TryUse(string id, string interactionBlockedText)
        {
            var d = Instance;
            if (d == null || string.IsNullOrEmpty(id)) return true;
            var step = d.Current;
            if (step != null)
            {
                foreach (var hit in step.interceptedInteractions)
                {
                    if (hit.interactionId != id) continue;
                    d.ShowBlocked(hit.text);
                    foreach (var e in step.completionEvents.ToArray()) d.Raise(e);
                    return false;
                }
                if (step.activeInteractions.Contains(id)) return true;
                foreach (var b in step.blockedTexts)
                    if (b.interactionId == id && !string.IsNullOrEmpty(b.text)) { d.ShowBlocked(b.text); return false; }
            }
            d.ShowBlocked(interactionBlockedText);
            return false;
        }

        /// <summary>Atalho para quem envia eventos de conclusão sem ter o diretor em mãos.</summary>
        public static void RaiseEvent(string eventId) => Instance?.Raise(eventId);

        /// <summary>Mostra um texto curto na tela (o mesmo dos textos de bloqueio). Sem diretor na cena nao faz nada.</summary>
        public static void ShowText(string text) => Instance?.ShowBlocked(text);

        void ShowBlocked(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                if (defaultBlockedTexts == null || defaultBlockedTexts.Length == 0) return;
                text = defaultBlockedTexts[blockedCycle++ % defaultBlockedTexts.Length];
            }
            blockedText = text;
            blockedLeft = blockedTextSeconds;
        }

        // ------------------------------------------------------------------ debug

        /// <summary>
        /// Pula direto para uma etapa. Aplica o estado das anteriores: luzes, triggers, eventos nomeados e as
        /// interações marcadas como concluídas (minigames, itens). Falas e sons das anteriores não tocam.
        /// Voltar a uma etapa anterior reaplica luzes/triggers, mas não desfaz minigames já concluídos.
        /// </summary>
        public void JumpTo(int target)
        {
            if (target < 0 || target >= steps.Count) return;
            transitioning = true;
            if (radio != null) radio.Clear();
            ResetTasks();
            for (int i = 0; i < target; i++)
            {
                Run(steps[i].onStart, false);
                foreach (var e in steps[i].completionEvents)
                {
                    MarkDone(e);
                    foreach (var inter in StepInteraction.All.ToArray())
                        if (!string.IsNullOrEmpty(e) && inter.CompletionEvent == e)
                            inter.ForceComplete();
                }
                Run(steps[i].onComplete, false);
            }
            transitioning = false;
            StartStep(target);
        }

        void Update()
        {
#if DEBUG
            var k = Keyboard.current;
            if (k != null && k[debugKey].wasPressedThisFrame) ToggleDebug();
#endif
            if (blockedLeft > 0f) blockedLeft -= Time.deltaTime;
        }

#if DEBUG
        CursorLockMode savedLock;
        bool savedVisible;

        void ToggleDebug()
        {
            debugOpen = !debugOpen;
            if (debugOpen) { savedLock = Cursor.lockState; savedVisible = Cursor.visible; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
            else { Cursor.lockState = savedLock; Cursor.visible = savedVisible; }
        }
#endif

        void OnGUI()
        {
            float w = Screen.width, h = Screen.height;
            if (blockedLeft > 0f && !string.IsNullOrEmpty(blockedText))
            {
                float a = Mathf.Clamp01(blockedLeft / 0.4f);
                UIStyle.OsdText(new Rect(0, h * 0.70f, w, 40 * UIStyle.Scale), blockedText, UIStyle.Prompt, a);
            }

#if DEBUG
            if (!debugOpen) return;
            var box = new Rect(20, 20, 380, Mathf.Min(h - 40, 40 + steps.Count * 28));
            GUILayout.BeginArea(box, GUI.skin.box);
            GUILayout.Label($"PROGRESSAO ({debugKey} fecha)");
            debugScroll = GUILayout.BeginScrollView(debugScroll);
            for (int i = 0; i < steps.Count; i++)
            {
                var s = steps[i];
                string label = $"{(i == current ? ">" : " ")} {i}  {(s == null ? "(vazia)" : string.IsNullOrEmpty(s.label) ? s.name : s.label)}";
                if (GUILayout.Button(label, GUI.skin.button) && s != null) JumpTo(i);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
#endif
        }
    }
}
