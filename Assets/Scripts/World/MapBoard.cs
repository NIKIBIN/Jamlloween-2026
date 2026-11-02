using UnityEngine;

namespace Circus
{
    /// <summary>Map pinned to the trailer wall. E opens it full screen.</summary>
    public class MapBoard : MonoBehaviour, IInteractable
    {
        public bool CanInteract => CircusMapUI.I != null;
        public string Prompt => "VER MAPA";
        public void Interact(Interactor who) => CircusMapUI.I.OpenFullscreen();
    }
}
