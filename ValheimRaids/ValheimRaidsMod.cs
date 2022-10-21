﻿using BepInEx;
using HarmonyLib;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.IO;
using UnityEngine;
using ValheimRaids.Scripts;

namespace JotunnModStub
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal class RaidAIMod : BaseUnityPlugin
    {
        public const string PluginGUID = "torky.RaidAI";
        public const string PluginName = "RaidAI";
        public const string PluginVersion = "0.0.2";

        private readonly Harmony m_harmony = new Harmony(PluginGUID);

        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            m_harmony.PatchAll();

            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("Raid AI starting up...");
            var directoryPath = Path.GetDirectoryName(typeof(RaidAIMod).Assembly.Location);
            var raidAssets = AssetUtils.LoadAssetBundle($"{directoryPath}/Assets/AssetBundles/raid");
            Jotunn.Logger.LogInfo(raidAssets.name);
            Jotunn.Logger.LogInfo(string.Join(", ", raidAssets.GetAllAssetNames()));

            var greydwarf = raidAssets.LoadAsset<GameObject>("assets/gameobject/raidgreydwarf.prefab");
            var monsterAI = greydwarf.GetComponent<MonsterAI>();
            var raidAI = greydwarf.AddComponent<RaidAI>();
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
            Jotunn.Logger.LogInfo(greydwarf.name);
            CreatureConfig greydwarfConfig = new CreatureConfig
            {
                Name = greydwarf.name
            };
            CustomCreature cc = new CustomCreature(greydwarf, true, greydwarfConfig);
            CreatureManager.Instance.AddCreature(cc);

            var defensePoint = raidAssets.LoadAsset<GameObject>("assets/gameobject/defensepoint.prefab");
            defensePoint.AddComponent<DefensePoint>();
            Jotunn.Logger.LogInfo(defensePoint.name);
            CustomPiece defensePointPiece = new CustomPiece(defensePoint, "Hammer", false);
            PieceManager.Instance.AddPiece(defensePointPiece);

            Jotunn.Logger.LogInfo("Raid AI setup finished...");
        }

        [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.OnPlaced))]
        class WearNTearBuildOnlyDefensePoint
        {
            static void Postfix(ref WearNTear __instance)
            {
                if (__instance.gameObject.TryGetComponent(out DefensePoint point))
                {
                    DefensePoint.SetDefensePoint(point.gameObject);
                }
            }
        }
    }
}