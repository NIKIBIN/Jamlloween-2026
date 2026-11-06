using System;
using System.Collections;
using System.Collections.Generic;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// O apagão do final: a energia do parque falha aos poucos. Quando o jogador embarca na gôndola (a partir de
    /// <see cref="startStep"/>; FerrisWheelRide.onBoarded chama <see cref="BeginRide"/>), as luzes do
    /// parque (StepBeacon com energia, ex. "park") apagam uma a uma, das mais longe de <see cref="center"/> para as
    /// mais perto, ao longo de <see cref="spreadSeconds"/>. As de <see cref="lastLights"/> (a roda-gigante) apagam por
    /// último, quando <see cref="finalStep"/> começa (ou no fim da sequência, o que vier depois).
    /// Um pulo de etapa (F4) para depois do apagão já deixa tudo apagado.
    /// </summary>
    public sealed class ProgressiveBlackout : MonoBehaviour
    {
        [SerializeField] ProgressionDirector director;
        [Tooltip("A partir desta etapa o embarque na gôndola começa o apagão (a etapa em que a gôndola está aberta).")]
        [SerializeField] ProgressionStep startStep;
        [Tooltip("As luzes finais apagam quando esta etapa começa.")]
        [SerializeField] ProgressionStep finalStep;
        [Tooltip("De onde se mede a distância (a roda-gigante): longe apaga primeiro.")]
        [SerializeField] Transform center;
        [Tooltip("Apagam por último (as luzes da roda-gigante).")]
        [SerializeField] StepBeacon[] lastLights = new StepBeacon[0];
        [Tooltip("Segundos entre a primeira e a última luz do parque (sem as finais).")]
        [SerializeField, Min(0.1f)] float spreadSeconds = 14f;
        [Tooltip("Quanto cada luz engasga antes de apagar.")]
        [SerializeField, Min(0f)] float flickerSeconds = 1.2f;
        [Tooltip("As finais engasgam mais.")]
        [SerializeField, Min(0f)] float lastFlickerSeconds = 2.5f;

        int startIndex, finalIndex;
        bool running, finished;

        void Awake()
        {
            if (director == null) throw new InvalidOperationException($"[{name}] ProgressiveBlackout sem 'director'.");
            if (startStep == null) throw new InvalidOperationException($"[{name}] ProgressiveBlackout sem 'startStep'.");
            if (finalStep == null) throw new InvalidOperationException($"[{name}] ProgressiveBlackout sem 'finalStep'.");
            if (center == null) throw new InvalidOperationException($"[{name}] ProgressiveBlackout sem 'center'.");
            if (lastLights.Length == 0) throw new InvalidOperationException($"[{name}] ProgressiveBlackout sem 'lastLights'.");
            foreach (var l in lastLights) if (l == null) throw new InvalidOperationException($"[{name}] 'lastLights' tem item vazio.");
            // Já no Awake: o diretor pode começar a primeira etapa antes do nosso Start, e com índices zerados a etapa 0
            // parecia o final (tudo apagava no começo do jogo).
            startIndex = director.IndexOf(startStep);
            finalIndex = director.IndexOf(finalStep);
            if (startIndex < 0 || finalIndex < 0) throw new InvalidOperationException($"[{name}] etapas fora da lista do diretor.");
        }

        void OnEnable() => director.OnStepStarted += OnStepStarted;
        void OnDisable() => director.OnStepStarted -= OnStepStarted;

        void OnStepStarted(ProgressionStep step, int index)
        {
            if (index > finalIndex) { FailAllNow(); return; }        // pulo para depois do final
            if (index == finalIndex) { if (!running) StartCoroutine(Sequence(0f)); finalReached = true; return; }
            // A etapa não dispara sozinha: um pulo (F4) chega nela com o jogador ainda longe da gôndola. Quem começa é o
            // embarque de verdade; num pulo o embarque acontece antes da etapa nova começar, então espera por ela aqui.
            if (boarded && index >= startIndex && !running) StartCoroutine(Sequence(spreadSeconds));
        }

        bool boarded;

        /// <summary>O jogador embarcou e a roda começou a subir: a energia começa a cair. Ligado em FerrisWheelRide.onBoarded.</summary>
        public void BeginRide()
        {
            boarded = true;
            if (running || director.CurrentIndex < startIndex) return;
            StartCoroutine(Sequence(spreadSeconds));
        }

        bool finalReached;

        List<StepBeacon> ParkLights()
        {
            var list = new List<StepBeacon>();
            foreach (var b in StepBeacon.All)
                if (!string.IsNullOrEmpty(b.PowerGroup) && Array.IndexOf(lastLights, b) < 0) list.Add(b);
            Vector3 c = center.position;
            list.Sort((a, b) => (b.transform.position - c).sqrMagnitude.CompareTo((a.transform.position - c).sqrMagnitude));
            return list;
        }

        IEnumerator Sequence(float spread)
        {
            running = true;
            var lights = ParkLights();
            float gap = lights.Count > 1 ? spread / (lights.Count - 1) : 0f;
            for (int i = 0; i < lights.Count; i++)
            {
                lights[i].FailPower(flickerSeconds);
                // Chegou a parada da roda antes do fim: o resto cai depressa.
                float wait = finalReached ? 0.08f : gap;
                if (wait > 0f) yield return new WaitForSeconds(wait);
            }
            while (!finalReached) yield return null;
            foreach (var l in lastLights) l.FailPower(lastFlickerSeconds);
            finished = true;
        }

        void FailAllNow()
        {
            if (finished) return;
            StopAllCoroutines();
            foreach (var b in ParkLights()) b.FailPower(0f);
            foreach (var l in lastLights) l.FailPower(0f);
            running = true; finalReached = true; finished = true;
        }
    }
}
