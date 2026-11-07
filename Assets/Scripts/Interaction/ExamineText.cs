using Jamlloween.Progression;
using UnityEngine;

/// <summary>
/// Examinar um objeto: com E, um texto curto aparece na tela (o mesmo dos textos de bloqueio). Ponha num collider.
/// Com <see cref="whileInteractionActive"/>, só funciona enquanto essa interação está ativa na etapa atual
/// (ex.: "zatan_repair": só antes do conserto do Zatan); fora disso nem mostra o prompt.
/// </summary>
public class ExamineText : MonoBehaviour, IInteractable
{
    [SerializeField, TextArea(1, 3)] string text;
    [Tooltip("Id de StepInteraction. Vazio: sempre.")]
    [SerializeField] string whileInteractionActive;

    bool Active => string.IsNullOrEmpty(whileInteractionActive) || ProgressionDirector.IsInteractionActive(whileInteractionActive);

    public string Prompt => Active ? "Examinar" : string.Empty;

    public void Interact(PlayerInteractor interactor)
    {
        if (Active) ProgressionDirector.ShowText(text);
    }
}
