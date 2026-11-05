using System;
using Jamlloween.Progression;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Troca objetos quando o diretor emite um evento nomeado: esconde uns e mostra outros (ex.: a cabine do
    /// Zatan inteira vira a cabine amassada em ZatanReleased). Também vale no pulo de etapa do debug (F4),
    /// porque o diretor reemite os eventos das etapas puladas.
    /// </summary>
    public sealed class SwapOnNamedEvent : MonoBehaviour
    {
        [SerializeField] ProgressionDirector director;
        [SerializeField] string eventName = ProgressionDirector.ZatanReleasedEvent;
        [SerializeField] GameObject[] hide = new GameObject[0];
        [SerializeField] GameObject[] show = new GameObject[0];

        void Awake()
        {
            if (director == null) throw new InvalidOperationException($"[{name}] SwapOnNamedEvent sem 'director'.");
            if (string.IsNullOrEmpty(eventName)) throw new InvalidOperationException($"[{name}] SwapOnNamedEvent sem 'eventName'.");
            foreach (var g in hide) if (g == null) throw new InvalidOperationException($"[{name}] item vazio em 'hide'.");
            foreach (var g in show) if (g == null) throw new InvalidOperationException($"[{name}] item vazio em 'show'.");
        }

        void OnEnable() => director.OnNamedEvent += OnEvent;

        void OnDisable() => director.OnNamedEvent -= OnEvent;

        void OnEvent(string id)
        {
            if (id != eventName) return;
            foreach (var g in hide) g.SetActive(false);
            foreach (var g in show) g.SetActive(true);
        }
    }
}
