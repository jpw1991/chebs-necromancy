using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Entities;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
{
    internal class SpiritPylon : MonoBehaviour
    {
        public static ConfigEntry<bool> Allowed;
        
        public static ConfigEntry<string> CraftingCost;
        public static ConfigEntry<float> SightRadius;
        public static ConfigEntry<float> GhostDuration;
        public static ConfigEntry<float> DelayBetweenGhosts;
        public static ConfigEntry<int> MaxGhosts;

        public const string PrefabName = "ChebGonaz_SpiritPylon.prefab";
        public const string PieceTable = "Hammer";
        public const string IconName = "chebgonaz_spiritpylon_icon.png";
        protected List<GameObject> SpawnedGhosts = new List<GameObject>();

        protected const string DefaultRecipe = "Stone:15,Wood:15,BoneFragments:15,SurtlingCore:1";

        private float ghostLastSpawnedAt;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind("SpiritPylon (Server Synced)", "SpiritPylonAllowed",
                true, new ConfigDescription("Whether making a Spirit Pylon is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            CraftingCost = plugin.Config.Bind("SpiritPylon (Server Synced)", "Spirit Pylon Build Costs",
                DefaultRecipe, new ConfigDescription("Materials needed to build Spirit Pylon. None or Blank will use Default settings.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            SightRadius = plugin.Config.Bind("SpiritPylon (Server Synced)", "SpiritPylonSightRadius",
                30f, new ConfigDescription("How far a Spirit Pylon can see enemies.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            GhostDuration = plugin.Config.Bind("SpiritPylon (Server Synced)", "SpiritPylonGhostDuration",
                30f, new ConfigDescription("How long a Spirit Pylon's ghost persists.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            DelayBetweenGhosts = plugin.Config.Bind("SpiritPylon (Server Synced)", "SpiritPylonDelayBetweenGhosts",
                5f, new ConfigDescription("How long a Spirit Pylon must wait before being able to spawn another ghost.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));

            MaxGhosts = plugin.Config.Bind("SpiritPylon (Server Synced)", "SpiritPylonMaxGhosts",
                3, new ConfigDescription("The maximum number of ghosts that a Spirit Pylon can spawn.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void Awake()
        {
            StartCoroutine(LookForEnemies());
        }

        public CustomPiece GetCustomPieceFromPrefab(GameObject prefab, Sprite icon)
        {
            PieceConfig config = new PieceConfig();
            config.Name = "$chebgonaz_spiritpylon_name";
            config.Description = "$chebgonaz_spiritpylon_desc";

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

        IEnumerator LookForEnemies()
        {
            while (ZInput.instance == null)
            {
                yield return new WaitForSeconds(2);
            }

            // prevent coroutine from doing its thing while the pylon isn't
            // yet constructed
            Piece piece = GetComponent<Piece>();
            while (!piece.IsPlacedByPlayer())
            {
                //Jotunn.Logger.LogInfo("Waiting for player to place pylon...");
                yield return new WaitForSeconds(2);
            }

            while (true)
            {
                yield return new WaitForSeconds(2);

                // clear out any dead/destroyed ghosts
                for (int i=SpawnedGhosts.Count-1; i>=0; i--)
                {
                    if (SpawnedGhosts[i] == null)
                    {
                        SpawnedGhosts.RemoveAt(i);
                    }
                }

                if (Player.m_localPlayer == null) continue;
                if (!EnemiesNearby(out Character characterInRange)) continue;
                
                // spawn ghosts up until the limit
                if (SpawnedGhosts.Count < MaxGhosts.Value)
                {
                    if (Time.time > ghostLastSpawnedAt + DelayBetweenGhosts.Value)
                    {
                        ghostLastSpawnedAt = Time.time;

                        GameObject friendlyGhost = SpawnFriendlyGhost();
                        friendlyGhost.GetComponent<MonsterAI>().SetTarget(characterInRange);
                        SpawnedGhosts.Add(friendlyGhost);
                    }
                }
            }
        }

        protected bool EnemiesNearby(out Character characterInRange)
        {
            List<Character> charactersInRange = new List<Character>();
            Character.GetCharactersInRange(
                transform.position,
                SightRadius.Value,
                charactersInRange
                );
            foreach (var character in charactersInRange.Where(character => character != null && character.m_faction != Character.Faction.Players))
            {
                characterInRange = character;
                return true;
            }
            characterInRange = null;
            return false;
        }

        protected GameObject SpawnFriendlyGhost()
        {
            int quality = 1;

            string prefabName = "ChebGonaz_SpiritPylonGhost";
            GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
            if (!prefab)
            {
                Logger.LogError($"SpawnFriendlyGhost: spawning {prefabName} failed!");
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
