using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Jamlloween.Lighting;

namespace Jamlloween.Progression
{
    /// <summary>
    /// Grupo de luzes ligado e desligado pelo diretor (ex.: "park"). Fica num objeto que permanece ativo.
    /// Com profiles, ligar e desligar o grupo também troca o visual do mundo pelo WorldLighting.
    /// O estado inicial (startOn) não troca o profile: quem manda no início é o profile inicial do WorldLighting.
    /// </summary>
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

        [Header("Visual do mundo")]
        [Tooltip("Profile aplicado quando o grupo liga (ex.: energia restaurada). Vazio: não muda o visual.")]
        [SerializeField] LightingProfile profileOn;
        [Tooltip("Profile aplicado quando o grupo desliga (ex.: apagão). Vazio: não muda o visual.")]
        [SerializeField] LightingProfile profileOff;
        [Tooltip("Segundos da transição ao ligar.")]
        [SerializeField] float fadeOnSeconds = 2f;
        [Tooltip("Segundos da transição ao desligar. Curto: a energia cai de repente.")]
        [SerializeField] float fadeOffSeconds = 0.3f;

        public bool IsOn { get; private set; }

        void Awake()
        {
            all.Add(this);
            Apply(startOn, false);
        }

        void OnDestroy() => all.Remove(this);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => all.Clear();

        public void Apply(bool on) => Apply(on, true);

        void Apply(bool on, bool changeProfile)
        {
            IsOn = on;
            if (changeProfile) WorldLighting.Set(on ? profileOn : profileOff, on ? fadeOnSeconds : fadeOffSeconds);
            foreach (var l in lights) if (l != null) l.enabled = on;
            foreach (var o in objects) if (o != null) o.SetActive(on);
            onChanged?.Invoke(on);
        }

        /// <summary>True se algum grupo com este id está ligado, ou se nenhum grupo com este id existe.</summary>
        public static bool IsGroupOn(string id)
        {
            bool found = false;
            foreach (var g in all)
            {
                if (g.groupId != id) continue;
                if (g.IsOn) return true;
                found = true;
            }
            return !found;
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
