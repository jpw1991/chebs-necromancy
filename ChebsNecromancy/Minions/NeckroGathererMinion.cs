using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.CustomPrefabs;
using UnityEngine;

namespace ChebsNecromancy.Minions
{
    internal class NeckroGathererMinion : UndeadMinion
    {
        // for limits checking
        private static int _createdOrderIncrementer;
        public int createdOrder;

        private float lastUpdate;

        public static ConfigEntry<bool> Allowed, ShowMessages;
        public static ConfigEntry<float> UpdateDelay, LookRadius, PickupRadius, DropoffPointRadius;

        public string NeckroStatus { get; set; }

        private int autoPickupMask, pieceMask;

        private Container container;

        private Container dropoffTarget;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererAllowed",
                true, new ConfigDescription("Whether the Neckro Gatherer is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererLookRadius",
                500f, new ConfigDescription("The radius in which the Neckro Gatherer can see items from.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            PickupRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererPickupRadius",
                10f, new ConfigDescription("The radius in which the Neckro Gatherer can pickup items from.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            DropoffPointRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererDropoffPointRadius",
                1000f, new ConfigDescription("The radius in which the Neckro Gatherer looks for a container to store its load in.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UpdateDelay = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between item searching & pickup attempts. Attention: small values may impact performance.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ShowMessages = plugin.Config.Bind("NeckroGatherer (Client)", "NeckroGathererShowMessages",
                true, new ConfigDescription("Whether the Neckro Gatherer talks or not."));
        }

        public override void Awake()
        {
            base.Awake(); 
            _createdOrderIncrementer++;
            createdOrder = _createdOrderIncrementer;

            container = GetComponent<Container>();

            container.m_height = LargeCargoCrate.ContainerHeight.Value;
            container.m_width = LargeCargoCrate.ContainerWidth.Value;

            autoPickupMask = LayerMask.GetMask("item");
            pieceMask = LayerMask.GetMask("piece");

            canBeCommanded = false;
        }

        private void Update()
        {
            if (ZNet.instance == null
                || !(Time.time > lastUpdate)) return;
            if (ReturnHome())
            {
                dropoffTarget = GetNearestDropOffPoint();
                if (dropoffTarget == null)
                {
                    NeckroStatus = "Can't find a container";
                }
                else
                {
                    NeckroStatus = $"Moving toward {dropoffTarget.name}";
                    if (CloseToDropoffPoint())
                    {
                        DepositItems();
                    }
                }
            }
            else
            {
                LookForNearbyItems();
                PickupNearbyItems();
                //todo: loot dead gatherers
            }
            
            if (ShowMessages.Value
                && NeckroStatus != ""
                && Player.m_localPlayer != null
                && Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < 5)
            {
                Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", NeckroStatus, false);   
            }

            lastUpdate = Time.time + UpdateDelay.Value;
        }

        private void LookForNearbyItems() {
            ItemDrop itemDrop = FindClosest<ItemDrop>(LookRadius.Value, autoPickupMask, drop => drop.GetTimeSinceSpawned() > 4);
            if (TryGetComponent(out MonsterAI monsterAI))
            {
                // move toward that item
                NeckroStatus = $"Moving toward {itemDrop.m_itemData.m_shared.m_name}";
                monsterAI.SetFollowTarget(itemDrop.gameObject);
            }
        }

        private void PickupNearbyItems()
        {
            List<string> itemNames = new();
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, PickupRadius.Value, autoPickupMask);
            foreach (var hitCollider in hitColliders)
            {
                ItemDrop itemDrop = hitCollider.GetComponentInParent<ItemDrop>();
                if (itemDrop != null
                    && itemDrop.CanPickup()
                    && StoreItem(itemDrop, container))
                {
                    itemNames.Add(itemDrop.m_itemData.m_shared.m_name);
                }
            }

            NeckroStatus = itemNames.Count > 0
                ? $"Picking up {string.Join(", ", itemNames)}"
                : "Looking for items...";
        }

        private bool StoreItem(ItemDrop itemDrop, Container depositContainer)
        {
            ItemDrop.ItemData itemData = itemDrop.m_itemData;
            if (itemData == null) return false;

            if (itemData.m_stack < 1) return false;
            
            NeckroStatus = $"Storing {itemData.m_shared.m_name} in {depositContainer.m_name}";

            int originalStackSize = itemData.m_stack;
            int itemsDeposited = 0;

            var depositInventory = depositContainer.GetInventory();

            var itemsOfTypeInInventoryBefore = depositInventory.CountItems(itemData.m_shared.m_name);
            
            while (itemData.m_stack-- > 0 && depositInventory.CanAddItem(itemData, 1))
            {
                ItemDrop.ItemData newItemData = itemData.Clone();
                newItemData.m_stack = 1;
                depositInventory.AddItem(newItemData);
                itemsDeposited++;
            }

            itemData.m_stack -= itemsDeposited;

            depositContainer.Save();
            
            // do a sanity check to make sure that nothing has been lost
            var itemsOfTypeInInventoryAfter = depositInventory.CountItems(itemData.m_shared.m_name);
            var completelyDeposited = originalStackSize == itemsDeposited;

            // if the stack was completely deposited, destroy the item
            if (itemData.m_stack <= 0 && completelyDeposited)
            {
                // Jotunn.Logger.LogInfo($"Neckro: destroying {itemData.m_shared.m_name} because completely " +
                //                       $"deposited." +
                //                       $"Original stack size: {originalStackSize}, " +
                //                       $"Item count before: {itemsOfTypeInInventoryBefore}, " +
                //                       $"now: {itemsOfTypeInInventoryAfter}. " +
                //                       $"Completely deposited: {completelyDeposited}");
                if (itemDrop.GetComponent<ZNetView>() == null)
                    DestroyImmediate(itemDrop.gameObject);
                else
                    ZNetScene.instance.Destroy(itemDrop.gameObject);
            }

            return itemsDeposited > 0;
        }

        private bool ReturnHome()
        {
            // return home if no slots found
            return container.GetInventory().GetEmptySlots() < 1;
        }

        private Container GetNearestDropOffPoint()
        {
            // find and return drop off point (some container with room)

            // doesnt work, dunno why
            //List<Piece> nearbyPieces = new List<Piece>();
            //Piece.GetAllPiecesInRadius(transform.position, dropoffPointRadius.Value, nearbyPieces);
            //
            //if (nearbyPieces.Count < 1) return false;
            Collider[] nearbyPieces = Physics.OverlapSphere(transform.position + Vector3.up, DropoffPointRadius.Value, pieceMask);
            if (nearbyPieces.Length < 1) return null;

            // order piece from closest to furthest, then take closest container
            Collider closest = nearbyPieces
                .OrderBy(piece => Vector3.Distance(transform.position, piece.transform.position))
                .FirstOrDefault(piece =>
                {
                    var pieceContainer = piece.GetComponentInParent<Container>();
                    return pieceContainer != null && pieceContainer.GetInventory().GetEmptySlots() > 0;
                });
            if (closest != null)
            {
                Container closestContainer = closest.GetComponentInParent<Container>();
                if (closestContainer != null
                    && TryGetComponent(out MonsterAI monsterAI))
                {
                    // move toward that piece
                    monsterAI.SetFollowTarget(closest.gameObject);
                    return closestContainer;
                }
            }
            return null;
        }

        private bool CloseToDropoffPoint()
        {
            return dropoffTarget != null && Vector3.Distance(transform.position, dropoffTarget.transform.position) < 5;
        }

        private void DepositItems()
        {
            dropoffTarget.GetInventory().MoveAll(container.GetInventory());
        }
    }
}
