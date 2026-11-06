using System;
using HeyHeyThere.PSXCarnivalKit;
using UnityEngine;

namespace Jamlloween.Roteiro
{
    /// <summary>
    /// Som 3D de uma Machine do kit que liga e desliga, como a porta do trailer: um clipe ao abrir (on) e outro ao
    /// fechar (off). Segue o onSwitched, então vale tanto para o jogador quanto para a porta que abre sozinha na fuga.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class MachineSwitchSound : MonoBehaviour
    {
        [SerializeField] Machine machine;
        [SerializeField] AudioClip onClip;
        [SerializeField] AudioClip offClip;
        [SerializeField, Range(0f, 1f)] float volume = 1f;

        AudioSource source;

        void Awake()
        {
            if (machine == null) machine = GetComponent<Machine>();
            if (machine == null) throw new InvalidOperationException($"[{name}] MachineSwitchSound sem 'machine'.");
            source = GetComponent<AudioSource>();
        }

        void OnEnable() => machine.onSwitched.AddListener(OnSwitched);

        void OnDisable() => machine.onSwitched.RemoveListener(OnSwitched);

        void OnSwitched(bool on)
        {
            var clip = on ? onClip : offClip;
            if (clip == null) return;
            source.pitch = UnityEngine.Random.Range(0.96f, 1.04f);
            source.PlayOneShot(clip, volume);
        }
    }
}
