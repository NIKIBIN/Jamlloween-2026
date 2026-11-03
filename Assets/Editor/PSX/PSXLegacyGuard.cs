using UnityEditor;

/// <summary>
/// Os builders antigos dos minigames (ZatanRepairPanelBuilder, GeneratorFuelBuilder, MinigameWorkLampBuilder) e o
/// MinigameMaterialConverter recriam prefabs com primitivas e devolvem os materiais ao Flashlight Fog Surface.
/// Depois da conversao PSX, isso desfaz o trabalho. Eles continuam no projeto, mas pedem confirmacao antes de rodar.
/// </summary>
public static class PSXLegacyGuard
{
    public static bool Confirm(string tool) =>
        EditorUtility.DisplayDialog(
            "Ferramenta antiga",
            $"{tool} recria o prefab com primitivas do Unity e volta os materiais para o Flashlight Fog Surface.\n\n" +
            "Isso desfaz a conversao para o padrao PSX (Tools > PSX). Rodar mesmo assim?",
            "Rodar mesmo assim", "Cancelar");
}
