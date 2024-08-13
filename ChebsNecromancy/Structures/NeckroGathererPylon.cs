using System.Collections;
using System.Reflection;
using BepInEx.Configuration;
using ChebsNecromancy.Minions;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using ChebsValheimLibrary.Structures;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Structures
{
    internal class NeckroGathererPylon : Structure
    {
        public static ConfigEntry<float> SpawnInterval;

        public static MemoryConfigEntry<string, List<string>> NeckroCost;
        // Cannot set custom width/height due to https://github.com/jpw1991/chebs-necromancy/issues/100
        //public static ConfigEntry<int> ContainerWidth, ContainerHeight;

        private Container _container;

        public new static ChebsRecipe ChebsRecipeConfig = new()
        {
            DefaultRecipe = "Stone:15,NeckTail:25,SurtlingCore:1",
            IconName = "chebgonaz_neckrogathererpylon_icon.png",
            PieceTable = "_HammerPieceTable",
            PieceCategory = "Misc",
            PieceName = "$chebgonaz_neckrogathererpylon_name",
            PieceDescription = "$chebgonaz_neckrogathererpylon_desc",
            PrefabName = "ChebGonaz_NeckroGathererPylon.prefab",
            ObjectName = MethodBase.GetCurrentMethod().DeclaringType.Name
        };

        public static void CreateConfigs(BasePlugin plugin)
        {
            ChebsRecipeConfig.Allowed = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererPylonAllowed",
                true,
                "Whether making a the pylon is allowed or not.", plugin.BoolValue, true);

            ChebsRecipeConfig.CraftingCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName,
                "NeckroGathererPylonBuildCosts",
                ChebsRecipeConfig.DefaultRecipe,
                "Materials needed to build the pylon. None or Blank will use Default settings. Format: " +
                ChebsRecipeConfig.RecipeValue,
                null, true);

            SpawnInterval = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererSpawnInterval", 60f,
                "How often the pylon will attempt to create a Neckro Gatherer.", plugin.FloatQuantityValue, true);

            var neckroCost = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "NeckroGathererCost", "NeckTail:1",
                "The items that are consumed when creating a Neckro Gatherer. Please use a comma-delimited list of prefab names with a : and integer for amount.",
                null, true);
            NeckroCost = new MemoryConfigEntry<string, List<string>>(neckroCost, s => s?.Split(',').Select(str => str.Trim()).ToList());

            // Cannot set custom width/height due to https://github.com/jpw1991/chebs-necromancy/issues/100
            // ContainerWidth = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerWidth", 4,
            //     "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(2, 10), true);
            //
            // ContainerHeight = plugin.ModConfig(ChebsRecipeConfig.ObjectName, "ContainerHeight", 4,
            //     "Inventory size = width * height = 4 * 4 = 16.", new AcceptableValueRange<int>(4, 20), true);
        }

        public new static void UpdateRecipe()
        {
            ChebsRecipeConfig.UpdateRecipe(ChebsRecipeConfig.CraftingCost);
        }

        private void Awake()
        {
            StartCoroutine(SpawnNeckros());
        }

        private IEnumerator SpawnNeckros()
        {
            //yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            var piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            // originally the Container was set on the prefab in unity and set up properly, but it will cause the
            // problem here:  https://github.com/jpw1991/chebs-necromancy/issues/100
            // So we add it here like this instead.
            // Pros: No bug
            // Cons: Cannot set custom width/height
            _container = gameObject.AddComponent<Container>();
            _container.m_name = "$chebgonaz_neckrogathererpylon_name";
            // _container.m_width = ContainerWidth.Value;
            // _container.m_height = ContainerHeight.Value;

            var inv = _container.GetInventory();
            inv.m_name = Localization.instance.Localize(_container.m_name);
            // trying to set width causes error here: https://github.com/jpw1991/chebs-necromancy/issues/100
            // inv.m_width = ContainerWidth.Value;
            // inv.m_height = ContainerHeight.Value;

            while (true)
            {
                yield return new WaitForSeconds(SpawnInterval.Value);

                if (!piece.m_nview.IsOwner()) continue;

                var playersInRange = new List<Player>();
                Player.GetPlayersInRange(transform.position, PlayerDetectionDistance, playersInRange);
                if (playersInRange.Count < 1) continue;

                yield return new WaitWhile(() => playersInRange[0].IsSleeping());

                SpawnNeckro();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        private bool CanSpawnNeckro
        {
            get
            {
                var canSpawn = false;
                foreach (var fuel in NeckroCost.Value)
                {
                    var splut = fuel.Split(':');
                    if (splut.Length != 2)
                    {
                        Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                        return false;
                    }

                    var itemRequired = splut[0];
                    if (!int.TryParse(splut[1], out var itemAmountRequired))
                    {
                        Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                        return false;
                    }

                    var requiredItemPrefab = ZNetScene.instance.GetPrefab(itemRequired);
                    if (requiredItemPrefab == null)
                    {
                        Logger.LogError(
                            $"Error processing config for Neckro Gatherer Costs: {itemRequired} doesn't exist.");
                        return false;
                    }

                    var amountInInv = _container.GetInventory()
                        .CountItems(requiredItemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name);
                    if (amountInInv >= itemAmountRequired)
                    {
                        canSpawn = true;
                    }
                }

                return canSpawn;
            }
        }

        private void ConsumeRequirements()
        {
            foreach (var fuel in NeckroCost.Value)
            {
                var splut = fuel.Split(':');
                if (splut.Length != 2)
                {
                    Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                    return;
                }

                var itemRequired = splut[0];
                if (!int.TryParse(splut[1], out var itemAmountRequired))
                {
                    Logger.LogError("Error in config for Neckro Gatherer Costs - please revise.");
                    return;
                }

                var requiredItemPrefab = ZNetScene.instance.GetPrefab(itemRequired);
                if (requiredItemPrefab == null)
                {
                    Logger.LogError(
                        $"Error processing config for Neckro Gatherer Costs: {itemRequired} doesn't exist.");
                    return;
                }

                var requiredItemName = requiredItemPrefab.GetComponent<ItemDrop>()?.m_itemData.m_shared.m_name;
                _container.GetInventory().RemoveItem(requiredItemName, itemAmountRequired);
            }
        }

        protected void SpawnNeckro()
        {
            // find nearest player and make them the owner
            var player = Player.GetClosestPlayer(transform.position, NeckroGathererMinion.LookRadius.Value);
            if (!player)
            {
                // no master? no neckro
                return;
            }

            if (!CanSpawnNeckro)
            {
                return;
            }

            ConsumeRequirements();

            var quality = 1;

            var prefabName = "ChebGonaz_NeckroGatherer";
            var prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"spawning {prefabName} failed!");
                return;
            }

            var spawnedChar = Instantiate(
                prefab,
                transform.position + transform.forward * 2f + Vector3.up,
                Quaternion.identity);

            var character = spawnedChar.GetComponent<Character>();
            character.SetLevel(quality);

            spawnedChar.GetComponent<NeckroGathererMinion>().UndeadMinionMaster = player.GetPlayerName();
            spawnedChar.AddComponent<FreshMinion>();
        }
    }
}