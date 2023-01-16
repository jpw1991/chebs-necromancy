using BepInEx;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Configs;
using BepInEx.Configuration;
using System.Linq;
using Jotunn.Entities;

namespace ChebsNecromancy
{
    internal class BatBeacon : MonoBehaviour
    {
        public static ConfigEntry<bool> allowed;

        public static ConfigEntry<string> craftingCost;
        public static ConfigEntry<float> sightRadius;
        public static ConfigEntry<float> batDuration;
        public static ConfigEntry<float> delayBetweenBats;
        public static ConfigEntry<int> maxBats;

        public static string PrefabName = "ChebGonaz_BatBeacon.prefab";
        public static string PieceTable = "Hammer";
        public static string IconName = "chebgonaz_batbeacon_icon.png";
        protected List<GameObject> spawnedBats = new List<GameObject>();

        protected const string DefaultRecipe = "FineWood:10,Silver:5,Guck:15";

        private float batLastSpawnedAt;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            allowed = plugin.Config.Bind("BatBeacon (Server Synced)", "BatBeaconAllowed",
                true, new ConfigDescription("Whether making a Spirit Pylon is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            craftingCost = plugin.Config.Bind("BatBeacon (Server Synced)", "BatBeaconBuildCosts",
                DefaultRecipe, new ConfigDescription("Materials needed to build the bat beacon. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            sightRadius = plugin.Config.Bind("BatBeacon (Server Synced)", "BatBeaconSightRadius",
                30f, new ConfigDescription("How far a bat beacon can see enemies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            batDuration = plugin.Config.Bind("BatBeacon (Server Synced)", "BatBeaconGhostDuration",
                30f, new ConfigDescription("How long a bat persists.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            delayBetweenBats = plugin.Config.Bind("BatBeacon (Server Synced)", "BatBeaconDelayBetweenBats",
                .5f, new ConfigDescription("How long a bat beacon wait before being able to spawn another bat.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            maxBats = plugin.Config.Bind("BatBeacon (Server Synced)", "BatBeaconMaxBats",
                15, new ConfigDescription("The maximum number of bats that a bat beacon can spawn.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void Awake()
        {
            StartCoroutine(LookForEnemies());
        }

        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new PieceConfig();
            config.Name = "$chebgonaz_batbeacon_name";
            config.Description = "$chebgonaz_batbeacon_desc";

            if (allowed.Value)
            {
                if (string.IsNullOrEmpty(craftingCost.Value))
                {
                    craftingCost.Value = DefaultRecipe;
                }
                // set recipe requirements
                SetRecipeReqs(config, craftingCost);
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
                Jotunn.Logger.LogError($"AddCustomPieces: {PrefabName}'s CustomPiece is null!");
                return null;
            }
            if (customPiece.PiecePrefab == null)
            {
                Jotunn.Logger.LogError($"AddCustomPieces: {PrefabName}'s PiecePrefab is null!");
                return null;
            }

            return customPiece;
        }


        public void SetRecipeReqs(PieceConfig config, ConfigEntry<string> craftingCost)
        {
            // function to add a single material to the recipe
            void addMaterial(string material)
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
                    addMaterial(material);
                }
            }
            else
            {
                addMaterial(craftingCost.Value);
            }
        }

        IEnumerator LookForEnemies()
        {
            yield return new WaitWhile(() => ZInput.instance == null);

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            yield return new WaitWhile(() => !piece.IsPlacedByPlayer());

            while (true)
            {
                yield return new WaitForSeconds(2);

                // clear out any dead/destroyed bats
                for (int i = spawnedBats.Count - 1; i >= 0; i--)
                {
                    if (spawnedBats[i] == null)
                    {
                        spawnedBats.RemoveAt(i);
                    }
                }

                if (EnemiesNearby(out Character characterInRange))
                {
                    // spawn up until the limit
                    if (spawnedBats.Count < maxBats.Value)
                    {
                        if (Time.time > batLastSpawnedAt + delayBetweenBats.Value)
                        {
                            batLastSpawnedAt = Time.time;

                            GameObject friendlyBat = SpawnFriendlyBat();
                            friendlyBat.GetComponent<MonsterAI>().SetTarget(characterInRange);
                            spawnedBats.Add(friendlyBat);
                        }
                    }
                }
            }
        }

        protected bool EnemiesNearby(out Character characterInRange)
        {
            List<Character> charactersInRange = new List<Character>();
            Character.GetCharactersInRange(
                transform.position,
                sightRadius.Value,
                charactersInRange
                );
            foreach (Character character in charactersInRange)
            {
                if (character != null && character.m_faction != Character.Faction.Players)
                {
                    characterInRange = character;
                    return true;
                }
            }
            characterInRange = null;
            return false;
        }

        protected GameObject SpawnFriendlyBat()
        {
            int quality = 1;

            string prefabName = "ChebGonaz_Bat";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Jotunn.Logger.LogError($"spawning {prefabName} failed!");
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
