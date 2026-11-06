using System;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Ao pular de etapa pelo menu de debug (F4), leva o jogador ao ponto daquela etapa. Um ponto por etapa, na mesma
    /// ordem da lista do diretor: arraste os objetos TP_xx na cena para onde o jogador deve começar. A seta azul do
    /// gizmo é para onde ele olha. Um ponto vazio mantém o jogador onde está.
    /// </summary>
    public sealed class DebugJumpTeleport : MonoBehaviour
    {
        [SerializeField] ProgressionDirector director;
        [Tooltip("A raiz do jogador (o objeto com o CharacterController).")]
        [SerializeField] CharacterController player;
        [Tooltip("Um ponto por etapa, na ordem do menu F4. Vazio = não teleporta nessa etapa.")]
        [SerializeField] Transform[] points = new Transform[0];

        void Awake()
        {
            if (director == null) throw new InvalidOperationException($"[{name}] DebugJumpTeleport sem 'director'.");
            if (player == null) throw new InvalidOperationException($"[{name}] DebugJumpTeleport sem 'player'.");
            if (points.Length != director.Steps.Count)
                Debug.LogWarning($"[{name}] {points.Length} pontos para {director.Steps.Count} etapas: confira a lista 'points' (um por etapa, na ordem do F4).", this);
        }

        void OnEnable() => director.OnJumped += OnJumped;
        void OnDisable() => director.OnJumped -= OnJumped;

        void OnJumped(int index)
        {
            if (index < 0 || index >= points.Length || points[index] == null) return;
            var p = points[index];
            // O CharacterController desfaz um teleporte enquanto está ligado.
            player.enabled = false;
            player.transform.SetPositionAndRotation(p.position, Quaternion.Euler(0f, p.eulerAngles.y, 0f));
            player.enabled = true;
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            for (int i = 0; i < points.Length; i++)
            {
                var p = points[i];
                if (p == null) continue;
                Gizmos.color = new Color(1f, 0.6f, 0.1f);
                Gizmos.DrawWireSphere(p.position + Vector3.up * 0.9f, 0.35f);
                Gizmos.DrawLine(p.position, p.position + Vector3.up * 1.8f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(p.position + Vector3.up * 1.6f, p.forward * 1.2f);
                UnityEditor.Handles.Label(p.position + Vector3.up * 2.1f, p.name);
            }
        }
#endif
    }
}
