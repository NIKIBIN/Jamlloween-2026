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
            Event        // targetId = nome do evento nomeado emitido pelo diretor (ex.: GeneratorRestored)
        }

        public Kind kind;
        [Tooltip("RadioLine: a fala.")] public RadioLine line;
        [Tooltip("LightGroup / Trigger: id do objeto na cena. Event: nome do evento.")] public string targetId;
        [Tooltip("LightGroup: ligado. Trigger: ativo.")] public bool state = true;
        [Tooltip("Sound: o som.")] public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;

        /// <summary>Ações que só mudam o estado do mundo. São as únicas aplicadas nas etapas puladas pelo debug.</summary>
        public bool IsWorldState => kind == Kind.LightGroup || kind == Kind.Trigger || kind == Kind.Event;
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
        [Header("Prancheta")]
        [Tooltip("Texto da tarefa. Vazio = a etapa não aparece na prancheta.")]
        public string taskText;

        [Header("Interações")]
        [Tooltip("Ids (StepInteraction) que funcionam nesta etapa. As demais mostram o texto de bloqueio.")]
        public List<string> activeInteractions = new List<string>();
        [Tooltip("Ids que, ao serem usados, mostram o texto daqui, não abrem e concluem a etapa.")]
        public List<InterceptedInteraction> interceptedInteractions = new List<InterceptedInteraction>();

        [Header("Conclusão")]
        [Tooltip("Evento que conclui a etapa (enviado por uma StepInteraction, StepTrigger ou pelo rádio). Vazio = nunca conclui.")]
        public string completionEvent;

        [Header("Ações")]
        public List<StepAction> onStart = new List<StepAction>();
        public List<StepAction> onComplete = new List<StepAction>();
    }
}
