using BepInEx;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using Jotunn.Utils;
using System.IO;
using UnityEngine;

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
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization Localization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            // Jotunn comes with its own Logger class to provide a consistent Log style for all mods using it
            Jotunn.Logger.LogInfo("Raid AI starting up...");
            var directoryPath = Path.GetDirectoryName(typeof(RaidAIMod).Assembly.Location);
            var test = AssetUtils.LoadAssetBundle($"{directoryPath}/Assets/AssetBundles/raid");
            Jotunn.Logger.LogInfo(test.name);
            Jotunn.Logger.LogInfo(string.Join("", test.GetAllAssetNames()));
            var testGameObject = test.LoadAsset<GameObject>("assets/gameobject/raidgreydwarf.prefab");
            Jotunn.Logger.LogInfo(testGameObject.name);
            CreatureConfig creatureConfig = new CreatureConfig
            {
                Name = testGameObject.name
            };
            CustomCreature cc = new CustomCreature(testGameObject, true, creatureConfig);
            CreatureManager.Instance.AddCreature(cc);
            Jotunn.Logger.LogInfo("Raid AI setup finished...");
        }
    }
}