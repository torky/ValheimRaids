using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.IO;
using UnityEngine;
using ValheimRaids.Scripts;
using ValheimRaids.Scripts.AI;

namespace ValheimRaids {
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class RaidAIMod : BaseUnityPlugin {
        public const string PluginGUID = "torky.RaidAI";
        public const string PluginName = "RaidAI";
        public const string PluginVersion = "0.0.2";

        private readonly Harmony m_harmony = new Harmony(PluginGUID);

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake() {
            m_harmony.PatchAll();

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("Raid AI starting up...");
            var directoryPath = Path.GetDirectoryName(typeof(RaidAIMod).Assembly.Location);
            var raidAssets = AssetUtils.LoadAssetBundle($"{directoryPath}/Assets/AssetBundles/raid");
            Jotunn.Logger.LogInfo(raidAssets.name);
            Jotunn.Logger.LogInfo(string.Join(", ", raidAssets.GetAllAssetNames()));

            var raidFloor = raidAssets.LoadAsset<GameObject>("assets/gameobject/raid_floor_block.prefab");
            Jotunn.Logger.LogInfo(raidFloor.name);
            raidFloor.AddComponent<RaidTowerPiece>();
            RaidBuilding.FloorPiecePrefab = raidFloor.GetComponent<Piece>();
            CustomPiece raidFloorPiece = new CustomPiece(raidFloor, "Hammer", true);
            PieceManager.Instance.AddPiece(raidFloorPiece);

            var raidRamp = raidAssets.LoadAsset<GameObject>("assets/gameobject/raid_ramp.prefab");
            Jotunn.Logger.LogInfo(raidRamp.name);
            raidRamp.AddComponent<RaidRamp>();
            RaidBuilding.RaidRampPrefab = raidRamp.GetComponent<Piece>();
            CustomPiece raidRampPiece = new CustomPiece(raidRamp, "Hammer", true);
            PieceManager.Instance.AddPiece(raidRampPiece);

            var raidPlank = raidAssets.LoadAsset<GameObject>("assets/gameobject/raid_plank.prefab");
            Jotunn.Logger.LogInfo(raidPlank.name);
            raidPlank.AddComponent<RaidPlank>();
            RaidBuilding.RaidPlankPrefab = raidPlank.GetComponent<Piece>();
            CustomPiece raidPlankPiece = new CustomPiece(raidPlank, "Hammer", true);
            PieceManager.Instance.AddPiece(raidPlankPiece);
            RampBuilder.Ramps.Add(raidPlank.name);

            var raidStair = raidAssets.LoadAsset<GameObject>("assets/gameobject/raid_wood_stair.prefab");
            Jotunn.Logger.LogInfo(raidStair.name);
            raidStair.AddComponent<RaidStair>();
            RaidBuilding.RaidStairPrefab = raidStair.GetComponent<Piece>();
            CustomPiece raidStairPiece = new CustomPiece(raidStair, "Hammer", true);
            PieceManager.Instance.AddPiece(raidStairPiece);
            RampBuilder.Ramps.Add(raidStair.name);

            var raidTrebuchet = raidAssets.LoadAsset<GameObject>("assets/gameobject/raidtrebuchet.prefab");
            Jotunn.Logger.LogInfo(raidTrebuchet.name);
            CustomPiece raidTrebuchetPiece = new CustomPiece(raidTrebuchet, "Hammer", true);
            PieceManager.Instance.AddPiece(raidTrebuchetPiece);

            var greydwarf = raidAssets.LoadAsset<GameObject>("assets/gameobject/raidgreydwarf.prefab");
            var raidAI = greydwarf.AddComponent<RaidAI>();
            TransferAndCreate(greydwarf, raidAI);

            var towerbuilder = raidAssets.LoadAsset<GameObject>("assets/gameobject/raidtowerbuilder.prefab");
            var towerBuilderAI = towerbuilder.AddComponent<TowerBuilder>();
            TransferAndCreate(towerbuilder, towerBuilderAI);

            var rampbuilder = raidAssets.LoadAsset<GameObject>("assets/gameobject/raidrampbuilder.prefab");
            var rampbuilderAI = rampbuilder.AddComponent<RampBuilder>();
            TransferAndCreate(rampbuilder, rampbuilderAI);

            var defensePoint = raidAssets.LoadAsset<GameObject>("assets/gameobject/defensepoint.prefab");
            Jotunn.Logger.LogInfo(defensePoint.name);
            defensePoint.AddComponent<RaidPoint>();
            CustomPiece defensePointPiece = new CustomPiece(defensePoint, "Hammer", false);
            PieceManager.Instance.AddPiece(defensePointPiece);

            Jotunn.Logger.LogInfo("Raid AI setup finished...");
        }

        public static void TransferAndCreate(GameObject creature, RaidAI raidAI) {
            Jotunn.Logger.LogInfo(creature.name);
            var monsterAI = creature.GetComponent<MonsterAI>();
            raidAI.m_viewRange = monsterAI.m_viewRange;
            raidAI.m_viewAngle = monsterAI.m_viewAngle;
            raidAI.m_hearRange = monsterAI.m_hearRange;
            raidAI.m_alertedEffects = monsterAI.m_alertedEffects;
            raidAI.m_idleSound = monsterAI.m_idleSound;
            raidAI.m_idleSoundInterval = monsterAI.m_idleSoundInterval;
            raidAI.m_idleSoundChance = monsterAI.m_idleSoundChance;
            raidAI.m_pathAgentType = monsterAI.m_pathAgentType;
            raidAI.m_moveMinAngle = monsterAI.m_moveMinAngle;
            raidAI.m_smoothMovement = monsterAI.m_smoothMovement;

            Destroy(monsterAI);
            CreatureConfig creatureConfig = new CreatureConfig { Name = creature.name };
            CustomCreature cc = new CustomCreature(creature, true, creatureConfig);
            CreatureManager.Instance.AddCreature(cc);
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
        class WearNTearBuildOnlyDefensePoint {
            static void Postfix(ref WearNTear __instance) {
                if (__instance.gameObject.TryGetComponent(out RaidPoint point)) {
                    RaidPoint.SetDefensePoint(point.gameObject);
                }
            }
        }
    }
}