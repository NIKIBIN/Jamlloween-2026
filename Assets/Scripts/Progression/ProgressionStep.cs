using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>Uma fala do rádio: legenda e, opcionalmente, o áudio. Sem áudio, a legenda fica o tempo de leitura.</summary>
    [Serializable]
    public class RadioLine
    {
        public string speaker = "PATRÃO";
        [TextArea(2, 5)] public string text;
        [Tooltip("Opcional. Quando existe, a legenda dura o tamanho do áudio.")]
        public AudioClip audio;
        [Range(0f, 1f)] public float volume = 1f;
        [Tooltip("Opcional. Tempo da legenda em segundos quando a fala não tem áudio. 0 = automático, pelo número de caracteres.")]
        [Min(0f)] public float seconds;
    }

    /// <summary>Estado de uma tarefa na prancheta.</summary>
    public enum TaskState
    {
        Hidden,   // não aparece
        Future,   // aparece esmaecida
        Current,  // aparece em destaque
        Done      // aparece riscada com [X]
    }

    /// <summary>
    /// Uma linha da prancheta. A prancheta é independente das etapas: as etapas mudam o estado das tarefas
    /// com ações (Task) e o evento de conclusão da tarefa a risca na hora em que acontece.
    /// </summary>
    [Serializable]
    public class ClipboardTask
    {
        public string id;
        public string text;
        [Tooltip("Sublinha, desenhada com recuo.")] public bool indent;
        public TaskState initialState = TaskState.Hidden;
        [Tooltip("Quando este evento é aceito pela etapa atual, a tarefa fica riscada. Vazio = só por ação.")]
        public string doneEvent;
    }

    /// <summary>Algo que o diretor faz ao iniciar ou concluir uma etapa. Só os campos do tipo escolhido são usados.</summary>
    [Serializable]
    public class StepAction
    {
        public enum Kind
        {
            RadioLine,   // line
            LightGroup,  // targetId = id do LightGroup, state = ligado
            Sound,       // clip, volume (2D)
            Trigger,     // targetId = id do StepTrigger, state = ativo
            Event,       // targetId = nome do evento nomeado emitido pelo diretor (ex.: GeneratorRestored)
            Task         // targetId = id da tarefa da prancheta, taskState = novo estado
        }

        public Kind kind;
        [Tooltip("RadioLine: a fala.")] public RadioLine line;
        [Tooltip("LightGroup / Trigger: id do objeto na cena. Event: nome do evento.")] public string targetId;
        [Tooltip("LightGroup: ligado. Trigger: ativo.")] public bool state = true;
        [Tooltip("Task: o novo estado da tarefa na prancheta.")] public TaskState taskState = TaskState.Current;
        [Tooltip("Sound: o som.")] public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;

        /// <summary>Ações que só mudam o estado do mundo. São as únicas aplicadas nas etapas puladas pelo debug.</summary>
        public bool IsWorldState => kind == Kind.LightGroup || kind == Kind.Trigger || kind == Kind.Event || kind == Kind.Task;
    }

    /// <summary>Texto mostrado quando o jogador tenta usar uma interação interceptada. Usar conclui a etapa.</summary>
    [Serializable]
    public class InterceptedInteraction
    {
        public string interactionId;
        [TextArea(1, 3)] public string text;
    }

    /// <summary>Uma etapa da progressão linear. A ordem é a da lista do ProgressionDirector.</summary>
    [CreateAssetMenu(menuName = "Jamlloween/Progression Step", fileName = "Step")]
    public class ProgressionStep : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Nome da etapa para o debug e o log. A prancheta tem a sua própria lista de tarefas no diretor.")]
        public string label;

        [Header("Interações")]
        [Tooltip("Ids (StepInteraction) que funcionam nesta etapa. As demais mostram o texto de bloqueio.")]
        public List<string> activeInteractions = new List<string>();
        [Tooltip("Ids que, ao serem usados, mostram o texto daqui, não abrem e concluem a etapa.")]
        public List<InterceptedInteraction> interceptedInteractions = new List<InterceptedInteraction>();

        [Tooltip("Textos mostrados ao usar uma interação que ainda não funciona nesta etapa. Não concluem nada; sem texto aqui vale o da interação ou o padrão.")]
        public List<InterceptedInteraction> blockedTexts = new List<InterceptedInteraction>();

        [Header("Conclusão")]
        [Tooltip("Eventos que concluem a etapa (enviados por StepInteraction, StepTrigger ou pelo rádio). Todos são necessários, em qualquer ordem. Vazio = nunca conclui.")]
        public List<string> completionEvents = new List<string>();

        [Header("Ações")]
        public List<StepAction> onStart = new List<StepAction>();
        public List<StepAction> onComplete = new List<StepAction>();
    }
}
