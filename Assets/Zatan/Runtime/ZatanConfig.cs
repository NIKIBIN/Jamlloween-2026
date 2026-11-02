using UnityEngine;

namespace Jamlloween.Zatan
{
    /// <summary>Parameters the tension brain drives. Values are interpolated from calm (0) to tense (100).</summary>
    public readonly struct ZatanTuning
    {
        public readonly float VisionRange, VisionAngle, HearingMult, PatrolMinRadius, PatrolMaxRadius, FootstepVolume, LoseTime;

        public ZatanTuning(float visionRange, float visionAngle, float hearingMult, float patrolMinRadius, float patrolMaxRadius, float footstepVolume, float loseTime)
        {
            VisionRange = visionRange;
            VisionAngle = visionAngle;
            HearingMult = hearingMult;
            PatrolMinRadius = patrolMinRadius;
            PatrolMaxRadius = patrolMaxRadius;
            FootstepVolume = footstepVolume;
            LoseTime = loseTime;
        }
    }

    [CreateAssetMenu(menuName = "Zatan/Config", fileName = "ZatanConfig")]
    public sealed class ZatanConfig : ScriptableObject
    {
        [Header("Velocidade (relativa ao jogador)")]
        [Tooltip("Somado à velocidade de andar do jogador durante a patrulha.")]
        public float patrolOverWalk = 0.25f;
        [Range(0f, 1f), Tooltip("Investigando: 0 = andar do jogador, 1 = corrida do jogador.")]
        public float investigateBlend = 0.5f;
        [Range(0f, 0.3f), Tooltip("Perseguição à distância = corrida do jogador x (1 + margem).")]
        public float chaseMarginFraction = 0.05f;
        [Tooltip("Distância em que a atenuação começa.")]
        public float attenuationStartDistance = 5f;
        [Tooltip("Distância em que a velocidade já é igual à corrida do jogador.")]
        public float attenuationEndDistance = 2f;
        public float catchDistance = 1.2f;
        [Tooltip("Alcance extra quando ele chegou ao fim do caminho (jogador encostado num obstáculo, fora do NavMesh).")]
        public float corneredExtraReach = 0.6f;
        public float acceleration = 20f;
        public float angularSpeed = 360f;

        [Header("Tensão: subida (por segundo)")]
        public float chaseGain = 3f;
        public float nearGain = 2f;
        public float nearDistance = 12f;
        public float observedGain = 3f;
        [Tooltip("Subida instantânea de um evento roteirizado.")]
        public float scriptedEventGain = 20f;

        [Header("Tensão: descida")]
        public float indoorDecay = 3f;
        public float quietDecay = 2f;
        [Tooltip("Segundos sem perseguição até a tensão começar a cair.")]
        public float quietDelay = 45f;
        [Tooltip("Queda instantânea ao concluir um objetivo.")]
        public float objectiveRelief = 15f;
        [Range(0f, 100f)] public float startTension = 20f;

        [Header("Calmo (tensão 0)")]
        public float calmVisionRange = 22f;
        public float calmVisionAngle = 110f;
        public float calmHearingMult = 1.4f;
        public float calmPatrolMinRadius = 8f;
        public float calmPatrolMaxRadius = 14f;
        [Range(0f, 1f)] public float calmFootstepVolume = 0.2f;
        public float calmLoseTime = 6f;

        [Header("Tenso (tensão 100)")]
        public float tenseVisionRange = 10f;
        public float tenseVisionAngle = 80f;
        public float tenseHearingMult = 0.6f;
        public float tensePatrolMinRadius = 20f;
        public float tensePatrolMaxRadius = 45f;
        [Range(0f, 1f)] public float tenseFootstepVolume = 1f;
        public float tenseLoseTime = 2f;

        [Header("Percepção")]
        public float sightConfirmTime = 0.3f;
        public float walkNoiseRadius = 6f;
        public float runNoiseRadius = 14f;
        public float noiseInterval = 0.35f;
        public float playerViewDistance = 60f;

        [Header("Comportamento")]
        public float patrolDwellMin = 1f;
        public float patrolDwellMax = 3f;
        public float investigateLookTime = 3f;
        public float searchTime = 8f;
        public float searchRadius = 6f;
        public float repathInterval = 0.15f;

        [Header("Sumiço")]
        public float vanishMinDistance = 30f;
        [Tooltip("Tempo seguido longe e fora da visão até sumir.")]
        public float vanishDelay = 4f;
        public float vanishCooldown = 20f;
        public float vanishDurationCalm = 4f;
        public float vanishDurationTense = 15f;

        [Header("Luz dos olhos")]
        public Color beamIdleColor = Color.white;
        public Color beamAlertColor = new Color(1f, 0.8f, 0.1f);
        public Color beamDangerColor = Color.red;
        [Tooltip("Na perseguição, a esta distância ou além a luz é toda amarela. O vermelho máximo fica em attenuationStartDistance.")]
        public float beamAlertDistance = 15f;
        [Tooltip("Segundos para passar de patrulha a perseguição e voltar.")]
        public float beamTransitionTime = 0.35f;
        [Tooltip("Graus abaixo da horizontal fora da perseguição.")]
        public float beamIdlePitch = 28f;
        [Tooltip("Graus abaixo da horizontal na perseguição.")]
        public float beamChasePitch = 6f;
        public float beamLength = 16f;
        public float beamConeAngle = 22f;
        [Tooltip("Graus do miolo do feixe, sem perda de brilho. Daí até beamConeAngle a luz some aos poucos.")]
        public float beamInnerConeAngle = 12f;
        [Tooltip("Brilho da luz no chão e nas paredes.")]
        public float beamSurfaceIntensity = 5f;
        [Range(0f, 1f), Tooltip("Quanto a luz no chão e nas paredes atravessa a névoa. 0 = some com a névoa, 1 = ignora a névoa.")]
        public float beamSurfaceFogCut = 0.75f;
        [Range(0f, 1f), Tooltip("Cor mínima da superfície sob a luz, para o chão quase preto da noite aparecer.")]
        public float beamSurfaceMinAlbedo = 0.25f;
        [Tooltip("Brilho do volume visível do feixe.")]
        public float beamOpacity = 0.35f;
        [Range(0f, 1f), Tooltip("Quanto o volume do feixe atravessa a névoa. 0 = some com a névoa, 1 = ignora a névoa.")]
        public float beamFogResistance = 0.6f;

        [Header("Luz dos olhos: obstáculos")]
        [Tooltip("O que bloqueia o feixe.")]
        public LayerMask beamBlockers = ~0;
        [Tooltip("Raios ao redor do raio central de cada olho.")]
        [Min(1)] public int beamFanRays = 6;
        [Range(0f, 1f), Tooltip("Onde ficam os raios ao redor: 0 = no eixo, 1 = na borda do cone.")]
        public float beamFanSpread = 0.7f;
        [Range(0.1f, 1f), Tooltip("Fração dos raios que precisa bater para encurtar o feixe. Assim, um poste fino não corta o cone inteiro.")]
        public float beamBlockFraction = 0.4f;
        [Range(0f, 1f), Tooltip("Superfície com normal.y acima disso é chão: ela já esconde o cone e não encurta o feixe.")]
        public float beamFloorNormalY = 0.7f;
        [Tooltip("Segundos entre as medições.")]
        public float beamProbeInterval = 0.05f;
        [Tooltip("Metros por segundo ao encurtar.")]
        public float beamShrinkSpeed = 80f;
        [Tooltip("Metros por segundo ao voltar a crescer.")]
        public float beamGrowSpeed = 12f;
        public float beamMinLength = 0.3f;

        [Header("Luz dos olhos: paredes")]
        [Tooltip("Raios horizontais que procuram a parede à frente, mesmo com o feixe apontado para o chão.")]
        [Min(2)] public int beamWallRays = 5;
        [Tooltip("Graus entre os raios das pontas.")]
        public float beamWallSpread = 26f;
        [Tooltip("Metros abaixo dos olhos de onde saem os raios.")]
        public float beamWallProbeDrop = 0.7f;
        [Min(1), Tooltip("Raios que precisam bater para a parede cortar a luz. Assim, um poste fino não corta.")]
        public int beamWallMinHits = 2;
        [Tooltip("Metros atrás da parede em que a luz termina de sumir.")]
        public float beamWallClipMargin = 0.5f;

        [Header("Captura: tempos (segundos)")]
        public float scareGrabTime = 0.25f;
        public float scareShakeTime = 1f;
        public float scareBiteTime = 0.5f;
        public float scareBlackTime = 0.35f;
        [Tooltip("Tempo para o mundo ficar preto, a partir do início do agarrão.")]
        public float scareBlackoutFadeTime = 0.05f;

        [Header("Captura: enquadramento")]
        [Tooltip("Metros da câmera à boca enquanto ele segura o jogador.")]
        public float scareHoldDistance = 0.75f;
        [Tooltip("Metros da câmera à boca no fim da mordida.")]
        public float scareBiteDistance = 0.08f;
        [Tooltip("Metros acima da boca que ficam na altura da câmera enquanto ele segura, para os olhos entrarem no quadro.")]
        public float scareHoldFaceRaise = 0.2f;
        [Tooltip("Metros que o jogador é erguido.")]
        public float scareLift = 0.08f;
        [Tooltip("Luz que só Zatan recebe enquanto o mundo está preto.")]
        [ColorUsage(false, true)] public Color scareLightColor = new Color(5f, 1.8f, 1.4f);

        [Header("Captura: chacoalhão")]
        public float scareShakeFrequency = 14f;
        [Tooltip("Metros de deslocamento da câmera.")]
        public float scareShakePosition = 0.06f;
        [Tooltip("Graus de giro da câmera em torno do eixo de visão.")]
        public float scareShakeRoll = 11f;
        [Tooltip("Graus de inclinação e guinada da câmera.")]
        public float scareShakeTilt = 4f;
        [Tooltip("Metros que ele se move ao contrário da câmera.")]
        public float scareShakeBodyPosition = 0.05f;
        [Tooltip("Graus que ele sacode.")]
        public float scareShakeBodyTilt = 7f;
        [Range(0f, 1f), Tooltip("Parte do chacoalhão que continua durante a mordida.")]
        public float scareBiteShake = 0.35f;

        [Header("Captura: mordida")]
        [Tooltip("Graus de boca entreaberta enquanto ele segura, mostrando os dentes.")]
        public float scareJawHoldAngle = 10f;
        [Tooltip("Graus que a mandíbula abre.")]
        public float scareJawOpenAngle = 45f;
        [Range(0.05f, 0.95f), Tooltip("Parte da mordida gasta só abrindo a boca, antes do bote.")]
        public float scareBiteOpenFraction = 0.5f;
        [Range(0.05f, 1f), Tooltip("Parte da mordida em que os dentes batem: som e tranco final.")]
        public float scareBiteImpactFraction = 0.9f;
        [Tooltip("Graus do tranco da câmera no impacto.")]
        public float scareImpactKick = 9f;

        public float BeamDanger(float distance) => 1f - Mathf.InverseLerp(attenuationStartDistance, beamAlertDistance, distance);

        public ZatanTuning Evaluate(float tension01)
        {
            float t = Mathf.Clamp01(tension01);
            return new ZatanTuning(
                Mathf.Lerp(calmVisionRange, tenseVisionRange, t),
                Mathf.Lerp(calmVisionAngle, tenseVisionAngle, t),
                Mathf.Lerp(calmHearingMult, tenseHearingMult, t),
                Mathf.Lerp(calmPatrolMinRadius, tensePatrolMinRadius, t),
                Mathf.Lerp(calmPatrolMaxRadius, tensePatrolMaxRadius, t),
                Mathf.Lerp(calmFootstepVolume, tenseFootstepVolume, t),
                Mathf.Lerp(calmLoseTime, tenseLoseTime, t));
        }

        public float ChaseSpeed(float distance, float playerRunSpeed)
        {
            float far = playerRunSpeed * (1f + chaseMarginFraction);
            float blend = Mathf.InverseLerp(attenuationEndDistance, attenuationStartDistance, distance);
            return Mathf.Lerp(playerRunSpeed, far, blend);
        }

        public float PatrolSpeed(float playerWalkSpeed) => playerWalkSpeed + patrolOverWalk;

        public float InvestigateSpeed(float playerWalkSpeed, float playerRunSpeed) => Mathf.Lerp(playerWalkSpeed, playerRunSpeed, investigateBlend);
    }
}
