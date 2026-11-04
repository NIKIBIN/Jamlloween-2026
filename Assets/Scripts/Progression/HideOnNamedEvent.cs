using UnityEngine;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Esconde objetos quando o diretor emite um evento nomeado (padrao: ZatanReleased, ao pegar a gasolina).
    /// Uso: na PF_fortune_machine, para o modelo do Zatan sumir da cabine. Coloque num objeto que fica ativo
    /// (a raiz da maquina, por exemplo), nunca no proprio modelo que vai sumir.
    /// Tambem vale no pulo de etapa do debug (F4): o diretor reaplica os eventos nomeados das etapas anteriores.
    /// </summary>
    public class HideOnNamedEvent : MonoBehaviour
    {
        [SerializeField] string eventName = ProgressionDirector.ZatanReleasedEvent;
        [Tooltip("Objetos que somem. Vazio = procura um filho com um dos nomes abaixo (o primeiro que existir).")]
        [SerializeField] GameObject[] targets;
        [Tooltip("Nomes exatos procurados entre os filhos quando 'targets' esta vazio.")]
        [SerializeField] string[] childNames = { "Zatan_00", "Zatan_V00", "Zatan_V00_SemAnimacao" };

        ProgressionDirector director;
        bool warnedNoDirector;

        void Start()
        {
            if (targets == null || targets.Length == 0)
            {
                Transform child = null;
                foreach (var n in childNames)
                    if ((child = FindChild(transform, n)) != null) break;
                if (child != null) targets = new[] { child.gameObject };
                else Debug.LogWarning($"[HideOnNamedEvent] '{name}': 'targets' vazio e nenhum filho chamado {string.Join(", ", childNames)}.", this);
            }
            TryHook();
        }

        // O diretor pode nascer depois (outra cena carregada, ordem de Start): tenta de novo ate achar.
        void Update()
        {
            if (director != null) return;
            TryHook();
            if (director == null && !warnedNoDirector && Time.timeSinceLevelLoad > 1f)
            {
                warnedNoDirector = true;
                Debug.LogWarning($"[HideOnNamedEvent] '{name}': nenhum ProgressionDirector na cena. O evento '{eventName}' nunca vai chegar.", this);
            }
        }

        void TryHook()
        {
            if (director != null || ProgressionDirector.Instance == null) return;
            director = ProgressionDirector.Instance;
            director.OnNamedEvent += OnEvent;
        }

        void OnDestroy()
        {
            if (director != null) director.OnNamedEvent -= OnEvent;
        }

        void OnEvent(string id)
        {
            if (id != eventName || targets == null) return;
            foreach (var t in targets)
                if (t != null)
                {
                    t.SetActive(false);
                    Debug.Log($"[HideOnNamedEvent] '{eventName}': escondeu '{t.name}'.", this);
                }
        }

        static Transform FindChild(Transform root, string childName)
        {
            if (string.IsNullOrEmpty(childName)) return null;
            foreach (Transform c in root)
            {
                if (c.name == childName) return c;
                var deeper = FindChild(c, childName);
                if (deeper != null) return deeper;
            }
            return null;
        }
    }
}
