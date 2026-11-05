using System;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Faz uma porta tremer por um instante (um soco do Zatan) e a devolve exatamente à pose em que estava.
    /// Convive com a Machine do kit, que só amostra a animação da porta enquanto ela abre ou fecha: se a animação
    /// mexer na porta durante o tremor, a nova pose dela vira a base.
    /// </summary>
    public sealed class DoorShake : MonoBehaviour
    {
        [SerializeField] Transform door;
        [SerializeField, Min(0.01f)] float duration = 0.35f;
        [Tooltip("Giro máximo em graus (cai até zero ao longo do tremor).")]
        [SerializeField, Min(0f)] float angle = 3f;
        [Tooltip("Deslocamento máximo em metros.")]
        [SerializeField, Min(0f)] float offset = 0.012f;
        [SerializeField, Min(1f)] float frequency = 28f;

        float left;
        Vector3 basePos, appliedPos;
        Quaternion baseRot, appliedRot;
        bool applied;

        void Awake()
        {
            if (door == null) throw new InvalidOperationException($"[{name}] DoorShake sem 'door'.");
        }

        /// <summary>Um tremor curto. Chamado a cada batida na porta.</summary>
        public void Shake()
        {
            if (!applied) { basePos = door.localPosition; baseRot = door.localRotation; }
            left = duration;
        }

        void LateUpdate()
        {
            if (!applied && left <= 0f) return;
            // Se algo mexeu na porta desde o último quadro (a animação de abrir/fechar), essa é a nova base.
            if (applied && (door.localPosition != appliedPos || door.localRotation != appliedRot))
            {
                basePos = door.localPosition;
                baseRot = door.localRotation;
            }

            if (left <= 0f)
            {
                door.SetLocalPositionAndRotation(basePos, baseRot);
                applied = false;
                return;
            }

            left -= Time.deltaTime;
            float k = Mathf.Clamp01(left / duration);
            float t = Time.time * frequency;
            var rot = Quaternion.Euler(
                (Mathf.PerlinNoise(t, 1f) * 2f - 1f) * angle * 0.4f * k,
                (Mathf.PerlinNoise(t, 2f) * 2f - 1f) * angle * k,
                (Mathf.PerlinNoise(t, 3f) * 2f - 1f) * angle * 0.4f * k);
            var pos = new Vector3(Mathf.PerlinNoise(t, 4f) * 2f - 1f, Mathf.PerlinNoise(t, 5f) * 2f - 1f, Mathf.PerlinNoise(t, 6f) * 2f - 1f) * (offset * k);
            appliedPos = basePos + pos;
            appliedRot = baseRot * rot;
            door.SetLocalPositionAndRotation(appliedPos, appliedRot);
            applied = true;
        }
    }
}
