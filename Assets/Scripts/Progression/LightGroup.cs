using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Jamlloween.Progression
{
    /// <summary>Grupo de luzes ligado e desligado pelo diretor (ex.: "park"). Fica num objeto que permanece ativo.</summary>
    public class LightGroup : MonoBehaviour
    {
        static readonly List<LightGroup> all = new List<LightGroup>();

        [SerializeField] string groupId;
        [SerializeField] Light[] lights;
        [Tooltip("Objetos ligados e desligados junto (postes, lâmpadas emissivas, partículas).")]
        [SerializeField] GameObject[] objects;
        [SerializeField] bool startOn = true;
        [Tooltip("Para o que não cabe nas listas acima (materiais emissivos, sons).")]
        public UnityEvent<bool> onChanged;

        public bool IsOn { get; private set; }

        void Awake()
        {
            all.Add(this);
            Apply(startOn);
        }

        void OnDestroy() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public void Apply(bool on)
        {
            IsOn = on;
            foreach (var l in lights) if (l != null) l.enabled = on;
            foreach (var o in objects) if (o != null) o.SetActive(on);
            onChanged?.Invoke(on);
        }

        /// <summary>Liga/desliga todos os grupos com este id. False se nenhum existe.</summary>
        public static bool Set(string id, bool on)
        {
            bool found = false;
            foreach (var g in all)
                if (g.groupId == id) { g.Apply(on); found = true; }
            return found;
        }
    }
}
