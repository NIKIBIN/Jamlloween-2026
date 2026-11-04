using System;
using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>The deep brain: a 0-100 meter of the player's tension that tunes the behaviour brain. Hidden from the player.</summary>
    public sealed class ZatanTension : MonoBehaviour
    {
        public const float Max = 100f;

        [SerializeField] ZatanConfig config;
        [SerializeField] ZatanTarget target;
        [SerializeField] ZatanBehaviourBrain brain;
        [SerializeField] ZatanPerception perception;

        public event Action<float> Changed = delegate { };

        public float Value { get; private set; }
        public float Normalized => Value / Max;
        public ZatanTuning Tuning { get; private set; }
        public float TimeSinceChase { get; private set; }
        public bool PlayerIndoors => indoorZones > 0;

        int indoorZones;

        void Awake()
        {
            if (config == null) throw new MissingReferenceException(nameof(config));
            if (target == null) throw new MissingReferenceException(nameof(target));
            if (brain == null) throw new MissingReferenceException(nameof(brain));
            if (perception == null) throw new MissingReferenceException(nameof(perception));
            Set(config.startTension);
        }

        void Update()
        {
            if (brain.State == ZatanBehaviourState.Caught) return;

            bool chasing = brain.IsHunting;
            TimeSinceChase = chasing ? 0f : TimeSinceChase + Time.deltaTime;

            float perSecond = 0f;
            if (chasing) perSecond += config.chaseGain;
            if (brain.IsPresent)
            {
                float distance = Vector3.Distance(transform.position, target.Position);
                if (distance <= config.nearDistance) perSecond += config.nearGain * (1f - distance / config.nearDistance);
                if (perception.PlayerSeesZatan) perSecond += config.observedGain;
            }
            if (!chasing)
            {
                if (PlayerIndoors) perSecond -= config.indoorDecay;
                if (TimeSinceChase >= config.quietDelay) perSecond -= config.quietDecay;
            }
            if (!Mathf.Approximately(perSecond, 0f)) Set(Value + perSecond * Time.deltaTime);
        }

        public void RaiseScriptedEvent() => Set(Value + config.scriptedEventGain);

        public void CompleteObjective() => Set(Value - config.objectiveRelief);

        public void SetValue(float value) => Set(value);

        public void EnterIndoorZone() => indoorZones++;

        public void ExitIndoorZone()
        {
            if (indoorZones == 0) throw new InvalidOperationException("Exit of an indoor zone that was never entered.");
            indoorZones--;
        }

        void Set(float value)
        {
            Value = Mathf.Clamp(value, 0f, Max);
            Tuning = config.Evaluate(Normalized);
            Changed(Value);
        }
    }
}
