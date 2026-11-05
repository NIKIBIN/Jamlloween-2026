using System;
using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// A porta do trailer nas fugas: abre sozinha quando a perseguição começa (o caminho até o abrigo fica livre)
    /// e fecha sozinha assim que o jogador entra no trailer (ligue PlayerEntered no onPlayerEntered do ZatanSafeRoom).
    /// </summary>
    public sealed class SafeRoomDoor : MonoBehaviour
    {
        [Tooltip("A Machine da porta (PF_caravan).")]
        [SerializeField] Machine door;

        bool waitingForPlayer;

        void Awake()
        {
            if (door == null) throw new InvalidOperationException($"[{name}] SafeRoomDoor sem 'door'.");
        }

        /// <summary>Abre a porta e a fecha quando o jogador entrar. Ligado no início das etapas de fuga.</summary>
        public void OpenForEscape()
        {
            if (!door.on) door.SetOn(true);
            waitingForPlayer = true;
        }

        /// <summary>O jogador entrou no trailer: fecha a porta, se ela foi aberta para a fuga.</summary>
        public void PlayerEntered()
        {
            if (!waitingForPlayer) return;
            waitingForPlayer = false;
            if (door.on) door.SetOn(false);
        }
    }
}
