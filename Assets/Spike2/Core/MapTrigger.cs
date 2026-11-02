using UnityEngine;

namespace Spike2.Core
{
    /// <summary>
    /// Trigger volume from the map (`triggers` in the JSON). Added by the builder next to the BoxCollider trigger.
    /// Publishes TriggerEntered/TriggerExited when the player (tag "Player") goes in/out. Acts listen on the Bus.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MapTrigger : MonoBehaviour
    {
        public string triggerId;
        public bool PlayerInside { get; private set; }

        string Id => string.IsNullOrEmpty(triggerId) ? (GetComponent<MapId>()?.id ?? name) : triggerId;

        static bool IsPlayer(Collider c) => c.CompareTag("Player") || (c.attachedRigidbody && c.attachedRigidbody.CompareTag("Player"));

        void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other) || PlayerInside) return;
            PlayerInside = true;
            Bus.Publish(new TriggerEntered { triggerId = Id });
        }

        void OnTriggerExit(Collider other)
        {
            if (!IsPlayer(other) || !PlayerInside) return;
            PlayerInside = false;
            Bus.Publish(new TriggerExited { triggerId = Id });
        }

        /// <summary>Is the player inside trigger `id` right now?</summary>
        public static bool IsInside(string id)
        {
            var t = MapRegistry.Get<MapTrigger>(id);
            return t != null && t.PlayerInside;
        }
    }
}
