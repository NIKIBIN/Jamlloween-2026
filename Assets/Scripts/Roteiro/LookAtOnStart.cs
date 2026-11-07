using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// No começo do jogo, vira o jogador para olhar um ponto (o corpo e a inclinação da câmera). Usado no trailer para o
    /// jogador começar olhando para a prancheta, a primeira tarefa. Segura o olhar nos primeiros instantes: quando o
    /// cursor trava, o primeiro movimento do mouse chega de uma vez e giraria a câmera para longe.
    /// </summary>
    [DefaultExecutionOrder(100)]   // depois do LevelWalker, que gira a câmera com o mouse
    public sealed class LookAtOnStart : MonoBehaviour
    {
        [SerializeField] LevelWalker walker;
        [SerializeField] Transform target;
        [Tooltip("Segundos (tempo real) segurando o olhar no alvo depois do começo.")]
        [SerializeField, Min(0f)] float holdSeconds = 0.4f;

        float started;

        void Start()
        {
            if (walker == null) throw new MissingReferenceException(nameof(walker));
            if (target == null) throw new MissingReferenceException(nameof(target));
            started = Time.unscaledTime;
            walker.LookAt(target.position);
        }

        void LateUpdate()
        {
            if (Time.unscaledTime - started > holdSeconds) { enabled = false; return; }
            walker.LookAt(target.position);
        }
    }
}
