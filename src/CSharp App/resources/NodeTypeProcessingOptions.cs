using System;
using System.Collections.Generic;

namespace VolumetricSelection2077.Resources;

public class NodeTypeProcessingOptions
{
    public static String[] NodeTypeOptions = new[]
    {
        "gameCyberspaceBoundaryNode",
        "gameDynamicEventNode",
        "gameEffectTriggerNode",
        "gameKillTriggerNode",
        "gameWorldBoundaryNode",
        "MinimapDataNode",
        "worldAcousticPortalNode",
        "worldAcousticSectorNode",
        "worldAcousticsOutdoornessAreaNode",
        "worldAcousticZoneNode",
        "worldAdvertisementNode",
        "worldAIDirectorSpawnAreaNode",
        "worldAIDirectorSpawnNode",
        "worldAISpotNode",
        "worldAmbientAreaNode",
        "worldAmbientPaletteExclusionAreaNode",
        "worldAreaProxyMeshNode",
        "worldAreaShapeNode",
        "worldAudioAttractAreaNode",
        "worldAudioSignpostTriggerNode",
        "worldAudioTagNode",
        "worldBakedDestructionNode",
        "worldBendedMeshNode",
        "worldBuildingProxyMeshNode",
        "worldCableMeshNode",
        "worldClothMeshNode",
        "worldCollisionAreaNode",
        "worldCollisionNode",
        "worldCommunityRegistryNode",
        "worldCompiledCommunityAreaNode_Streamable",
        "worldCompiledCommunityAreaNode",
        "worldCompiledCrowdParkingSpaceNode",
        "worldCompiledSmartObjectsNode",
        "worldCrowdNullAreaNode",
        "worldCrowdParkingSpaceNode",
        "worldCrowdPortalNode",
        "worldCurvePathNode",
        "worldDecorationMeshNode",
        "worldDecorationProxyMeshNode",
        "worldDestructibleEntityProxyMeshNode",
        "worldDestructibleProxyMeshNode",
        "worldDeviceNode",
        "worldDistantGINode",
        "worldDistantLightsNode",
        "worldDynamicMeshNode",
        "worldEffectNode",
        "worldEntityNode",
        "worldEntityProxyMeshNode",
        "worldFoliageDestructionNode",
        "worldFoliageNode",
        "worldGenericProxyMeshNode",
        "worldGeometryShapeNode",
        "worldGINode",
        "worldGIShapeNode",
        "worldGISpaceNode",
        "worldGuardAreaNode",
        "worldInstancedDestructibleMeshNode",
        "worldInstancedMeshNode",
        "worldInstancedOccluderNode",
        "worldInterestingConversationsAreaNode",
        "worldInteriorAreaNode",
        "worldInteriorMapNode",
        "worldInvalidProxyMeshNode",
        "worldLightChannelShapeNode",
        "worldLightChannelVolumeNode",
        "worldLocationAreaNode",
        "worldMeshNode",
        "worldMinimapConfigAreaNode",
        "worldMinimapModeOverrideAreaNode",
        "worldMirrorNode",
        "worldNavigationConfigAreaNode",
        "worldNavigationDeniedAreaNode",
        "worldNavigationNode",
        "worldOffMeshConnectionNode",
        "worldOffMeshSmartObjectNode",
        "worldPatrolSplineNode",
        "worldPerformanceAreaNode",
        "worldPhysicalDestructionNode",
        "worldPhysicalFractureFieldNode",
        "worldPhysicalImpulseAreaNode",
        "worldPhysicalTriggerAreaNode",
        "worldPopulationSpawnerNode",
        "worldPrefabNode",
        "worldPrefabProxyMeshNode",
        "worldPreventionFreeAreaNode",
        "worldQuestProxyMeshNode",
        "worldRaceSplineNode",
        "worldReflectionProbeNode",
        "worldRoadProxyMeshNode",
        "worldRotatingMeshNode",
        "worldSaveSanitizationForbiddenAreaNode",
        "worldSceneRecordingContentObserverNode",
        "worldSmartObjectNode",
        "worldSocketNode",
        "worldSpeedSplineNode",
        "worldSplineNode",
        "worldStaticDecalNode",
        "worldStaticFogVolumeNode",
        "worldStaticGpsLocationEntranceMarkerNode",
        "worldStaticLightNode",
        "worldStaticMarkerNode",
        "worldStaticMeshNode",
        "worldStaticOccluderMeshNode",
        "worldStaticParticleNode",
        "worldStaticQuestMarkerNode",
        "worldStaticSoundEmitterNode",
        "worldStaticStickerNode",
        "worldStaticVectorFieldNode",
        "worldTerrainCollisionNode",
        "worldTerrainMeshNode",
        "worldTerrainProxyMeshNode",
        "worldTrafficCollisionGroupNode",
        "worldTrafficCompiledNode",
        "worldTrafficPersistentNode",
        "worldTrafficSourceNode",
        "worldTrafficSplineNode",
        "worldTrafficSpotNode",
        "worldTriggerAreaNode",
        "worldVehicleForbiddenAreaNode",
        "worldWaterNullAreaNode",
        "worldWaterPatchNode",
        "worldWaterPatchProxyMeshNode"
    };

    public enum Enum : byte
    {
        gameCyberspaceBoundaryNode,
        gameDynamicEventNode,
        gameEffectTriggerNode,
        gameKillTriggerNode,
        gameWorldBoundaryNode,
        MinimapDataNode,
        worldAcousticPortalNode,
        worldAcousticSectorNode,
        worldAcousticsOutdoornessAreaNode,
        worldAcousticZoneNode,
        worldAdvertisementNode,
        worldAIDirectorSpawnAreaNode,
        worldAIDirectorSpawnNode,
        worldAISpotNode,
        worldAmbientAreaNode,
        worldAmbientPaletteExclusionAreaNode,
        worldAreaProxyMeshNode,
        worldAreaShapeNode,
        worldAudioAttractAreaNode,
        worldAudioSignpostTriggerNode,
        worldAudioTagNode,
        worldBakedDestructionNode,
        worldBendedMeshNode,
        worldBuildingProxyMeshNode,
        worldCableMeshNode,
        worldClothMeshNode,
        worldCollisionAreaNode,
        worldCollisionNode,
        worldCommunityRegistryNode,
        worldCompiledCommunityAreaNode_Streamable,
        worldCompiledCommunityAreaNode,
        worldCompiledCrowdParkingSpaceNode,
        worldCompiledSmartObjectsNode,
        worldCrowdNullAreaNode,
        worldCrowdParkingSpaceNode,
        worldCrowdPortalNode,
        worldCurvePathNode,
        worldDecorationMeshNode,
        worldDecorationProxyMeshNode,
        worldDestructibleEntityProxyMeshNode,
        worldDestructibleProxyMeshNode,
        worldDeviceNode,
        worldDistantGINode,
        worldDistantLightsNode,
        worldDynamicMeshNode,
        worldEffectNode,
        worldEntityNode,
        worldEntityProxyMeshNode,
        worldFoliageDestructionNode,
        worldFoliageNode,
        worldGenericProxyMeshNode,
        worldGeometryShapeNode,
        worldGINode,
        worldGIShapeNode,
        worldGISpaceNode,
        worldGuardAreaNode,
        worldInstancedDestructibleMeshNode,
        worldInstancedMeshNode,
        worldInstancedOccluderNode,
        worldInterestingConversationsAreaNode,
        worldInteriorAreaNode,
        worldInteriorMapNode,
        worldInvalidProxyMeshNode,
        worldLightChannelShapeNode,
        worldLightChannelVolumeNode,
        worldLocationAreaNode,
        worldMeshNode,
        worldMinimapConfigAreaNode,
        worldMinimapModeOverrideAreaNode,
        worldMirrorNode,
        worldNavigationConfigAreaNode,
        worldNavigationDeniedAreaNode,
        worldNavigationNode,
        worldOffMeshConnectionNode,
        worldOffMeshSmartObjectNode,
        worldPatrolSplineNode,
        worldPerformanceAreaNode,
        worldPhysicalDestructionNode,
        worldPhysicalFractureFieldNode,
        worldPhysicalImpulseAreaNode,
        worldPhysicalTriggerAreaNode,
        worldPopulationSpawnerNode,
        worldPrefabNode,
        worldPrefabProxyMeshNode,
        worldPreventionFreeAreaNode,
        worldQuestProxyMeshNode,
        worldRaceSplineNode,
        worldReflectionProbeNode,
        worldRoadProxyMeshNode,
        worldRotatingMeshNode,
        worldSaveSanitizationForbiddenAreaNode,
        worldSceneRecordingContentObserverNode,
        worldSmartObjectNode,
        worldSocketNode,
        worldSpeedSplineNode,
        worldSplineNode,
        worldStaticDecalNode,
        worldStaticFogVolumeNode,
        worldStaticGpsLocationEntranceMarkerNode,
        worldStaticLightNode,
        worldStaticMarkerNode,
        worldStaticMeshNode,
        worldStaticOccluderMeshNode,
        worldStaticParticleNode,
        worldStaticQuestMarkerNode,
        worldStaticSoundEmitterNode,
        worldStaticStickerNode,
        worldStaticVectorFieldNode,
        worldTerrainCollisionNode,
        worldTerrainMeshNode,
        worldTerrainProxyMeshNode,
        worldTrafficCollisionGroupNode,
        worldTrafficCompiledNode,
        worldTrafficPersistentNode,
        worldTrafficSourceNode,
        worldTrafficSplineNode,
        worldTrafficSpotNode,
        worldTriggerAreaNode,
        worldVehicleForbiddenAreaNode,
        worldWaterNullAreaNode,
        worldWaterPatchNode,
        worldWaterPatchProxyMeshNode
    }
}