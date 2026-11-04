using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Bridge from the progression director to scene systems that live in their own assemblies (Zatan, audio cues,
    /// props). Each named event or step start/end runs a UnityEvent wired in the Inspector, so the director never
    /// needs to know who listens. Scene systems send completion events back with <see cref="Raise"/>.
    /// </summary>
    public class ProgressionEventRelay : MonoBehaviour
    {
        [Serializable]
        public class NamedEventBinding
        {
            [Tooltip("Name emitted by an Event action of a step (e.g. GeneratorRestored, ZatanReleased).")]
            public string eventName;
            public UnityEvent action = new UnityEvent();
        }

        [Serializable]
        public class StepBinding
        {
            public ProgressionStep step;
            public UnityEvent onStarted = new UnityEvent();
            public UnityEvent onCompleted = new UnityEvent();
        }

        [SerializeField] ProgressionDirector director;
        [SerializeField] List<NamedEventBinding> namedEvents = new List<NamedEventBinding>();
        [SerializeField] List<StepBinding> steps = new List<StepBinding>();

        void Awake()
        {
            if (director == null) throw new MissingReferenceException(nameof(director));
            foreach (var b in namedEvents)
                if (string.IsNullOrEmpty(b.eventName)) throw new InvalidOperationException($"[ProgressionEventRelay] '{name}': binding without an event name.");
            foreach (var b in steps)
                if (b.step == null) throw new MissingReferenceException($"[ProgressionEventRelay] '{name}': step binding without a step.");
        }

        void OnEnable()
        {
            director.OnNamedEvent += OnNamed;
            director.OnStepStarted += OnStarted;
            director.OnStepCompleted += OnCompleted;
        }

        void OnDisable()
        {
            director.OnNamedEvent -= OnNamed;
            director.OnStepStarted -= OnStarted;
            director.OnStepCompleted -= OnCompleted;
        }

        /// <summary>The bindings, for editor tools that wire them.</summary>
        public List<NamedEventBinding> NamedEvents => namedEvents;
        public List<StepBinding> StepBindings => steps;

        /// <summary>Sends a completion event to the director (for UnityEvents of other systems).</summary>
        public void Raise(string eventId) => director.Raise(eventId);

        void OnNamed(string id)
        {
            foreach (var b in namedEvents)
                if (b.eventName == id) b.action.Invoke();
        }

        void OnStarted(ProgressionStep step, int index)
        {
            foreach (var b in steps)
                if (b.step == step) b.onStarted.Invoke();
        }

        void OnCompleted(ProgressionStep step, int index)
        {
            foreach (var b in steps)
                if (b.step == step) b.onCompleted.Invoke();
        }
    }
}
