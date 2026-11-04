using System;
using System.Collections.Generic;
using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>Every hiding spot of the scene, assigned in the Inspector. Zatan picks the cans he tips from here.</summary>
    public sealed class ZatanHidingRegistry : MonoBehaviour
    {
        [SerializeField] ZatanHidingSpot[] spots;
        [Tooltip("When the catch sequence ends the scene restarts: every can stands up again.")]
        [SerializeField] ZatanCatchSequence catchSequence;

        /// <summary>The player got into a spot.</summary>
        public event Action<ZatanHidingSpot> PlayerHid = delegate { };

        public IReadOnlyList<ZatanHidingSpot> Spots => spots;

        readonly List<ZatanHidingSpot> candidates = new List<ZatanHidingSpot>();

        void Awake()
        {
            if (spots == null || spots.Length == 0) throw new MissingReferenceException(nameof(spots));
            foreach (ZatanHidingSpot spot in spots)
                if (spot == null) throw new MissingReferenceException(nameof(spots));
            if (catchSequence == null) throw new MissingReferenceException(nameof(catchSequence));
        }

        void OnEnable()
        {
            foreach (ZatanHidingSpot spot in spots) spot.Entered += OnEntered;
            catchSequence.Finished += ResetAll;
        }

        void OnDisable()
        {
            foreach (ZatanHidingSpot spot in spots) spot.Entered -= OnEntered;
            catchSequence.Finished -= ResetAll;
        }

        void OnEntered(ZatanHidingSpot spot) => PlayerHid(spot);

        /// <summary>A random standing can within <paramref name="radius"/> of <paramref name="center"/>, occupied or not.</summary>
        public bool TryPickStanding(Vector3 center, float radius, out ZatanHidingSpot picked)
        {
            candidates.Clear();
            foreach (ZatanHidingSpot spot in spots)
                if (!spot.IsTipped && Vector3.Distance(spot.Position, center) <= radius) candidates.Add(spot);
            picked = candidates.Count > 0 ? candidates[UnityEngine.Random.Range(0, candidates.Count)] : null;
            return picked != null;
        }

        public void ResetAll()
        {
            foreach (ZatanHidingSpot spot in spots) spot.ResetUpright();
        }
    }
}
