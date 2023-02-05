using System.Collections;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
{
    internal class NeckroGathererPylon : MonoBehaviour
    {
        public static ConfigEntry<bool> Allowed;
        public static ConfigEntry<string> CraftingCost;
        public static ConfigEntry<float> SpawnInterval;
        public static ConfigEntry<int> NeckTailsConsumedPerSpawn;

        public const string PrefabName = "ChebGonaz_NeckroGathererPylon.prefab";
        public const string PieceTable = "Hammer";
        public const string IconName = "chebgonaz_neckrogathererpylon_icon.png";

        protected const string DefaultRecipe = "Stone:15,NeckTail:25,SurtlingCore:1";

        protected Container Container;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind("NeckroGathererPylon (Server Synced)", "NeckroGathererPylonAllowed",
                true, new ConfigDescription("Whether making a the pylon is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("NeckroGathererPylon (Server Synced)", "NeckroGathererPylonBuildCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to build the pylon. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SpawnInterval = plugin.Config.Bind("NeckroGathererPylon (Server Synced)", "NeckroGathererSpawnInterval",
                60f, new ConfigDescription("How often the pylon will attempt to create a Neckro Gatherer.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            NeckTailsConsumedPerSpawn = plugin.Config.Bind("NeckroGathererPylon (Server Synced)", "NeckroGathererCreationCost",
                1, new ConfigDescription("How many Neck Tails get consumed when creating a Neckro Gatherer.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void Awake()
        {
            Container = GetComponent<Container>();
            StartCoroutine(SpawnNeckros());
        }

        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new PieceConfig();
            config.Name = "$chebgonaz_neckrogathererpylon_name";
            config.Description = "$chebgonaz_neckrogathererpylon_desc";

            if (Allowed.Value)
            {
                if (string.IsNullOrEmpty(CraftingCost.Value))
                {
                    CraftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                SetRecipeReqs(config, CraftingCost);
            }
            else
            {
                config.Enabled = false;
            }

            config.Icon = icon;
            config.PieceTable = "_HammerPieceTable";
            config.Category = "Misc";

            CustomPiece customPiece = new CustomPiece(prefab, false, config);
            if (customPiece == null)
            {
                Logger.LogError($"AddCustomPieces: {PrefabName}'s CustomPiece is null!");
                return null;
            }
            if (customPiece.PiecePrefab == null)
            {
                Logger.LogError($"AddCustomPieces: {PrefabName}'s PiecePrefab is null!");
                return null;
            }

            return customPiece;
        }


        public void SetRecipeReqs(PieceConfig config, ConfigEntry<string> craftingCost)
        {
            // function to add a single material to the recipe
            void AddMaterial(string material)
            {
                string[] materialSplit = material.Split(':');
                string materialName = materialSplit[0];
                int materialAmount = int.Parse(materialSplit[1]);
                config.AddRequirement(new RequirementConfig(materialName, materialAmount, 0, true));
            }

            // build the recipe. material config format ex: Wood:5,Stone:1,Resin:1
            if (craftingCost.Value.Contains(','))
            {
                string[] materialList = craftingCost.Value.Split(',');

                foreach (string material in materialList)
                {
                    AddMaterial(material);
                }
            }
            else
            {
                AddMaterial(craftingCost.Value);
            }
        }

        private IEnumerator SpawnNeckros()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(SpawnInterval.Value);

                SpawnNeckro();
            }
        }

        protected GameObject SpawnNeckro()
        {
            int neckTailsInInventory = Container.GetInventory().CountItems("$item_necktail");
            if (neckTailsInInventory < NeckTailsConsumedPerSpawn.Value) return null;

            Container.GetInventory().RemoveItem("$item_necktail", NeckTailsConsumedPerSpawn.Value);

            int quality = 1;

            string prefabName = "ChebGonaz_NeckroGatherer";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"spawning {prefabName} failed!");
                return null;
            }

            GameObject spawnedChar = Instantiate(
                prefab,
                transform.position + transform.forward * 2f + Vector3.up,
                Quaternion.identity);

            Character character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            return spawnedChar;
        }
    }
}
