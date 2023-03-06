using System;
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

        private float lastUpdate;

        public static ConfigEntry<bool> Allowed, ShowMessages;
        public static ConfigEntry<float> UpdateDelay, LookRadius, DropoffPointRadius, PickupDelay, PickupDistance;

        private static List<ItemDrop> _itemDrops = new();

        public string NeckroStatus { get; set; }

        private int autoPickupMask, pieceMask;

        private Container container;

        private Container dropoffTarget;

        private MonsterAI _monsterAI;

        private ItemDrop _currentItem;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            Allowed = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererAllowed",
                true, new ConfigDescription("Whether the Neckro Gatherer is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            LookRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererLookRadius",
                500f, new ConfigDescription("The radius in which the Neckro Gatherer can see items from.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            DropoffPointRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererDropoffPointRadius",
                1000f, new ConfigDescription("The radius in which the Neckro Gatherer looks for a container to store its load in.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UpdateDelay = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererUpdateDelay",
                6f, new ConfigDescription("The delay, in seconds, between item searching & pickup attempts. Attention: small values may impact performance.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            ShowMessages = plugin.Config.Bind("NeckroGatherer (Client)", "NeckroGathererShowMessages",
                true, new ConfigDescription("Whether the Neckro Gatherer talks or not."));
            PickupDelay = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererPickupDelay",
                10f, new ConfigDescription("The Neckro won't pick up items immediately upon seeing them. Rather, it will make note of them and pick them up if they're still on the ground after this delay.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            PickupDistance = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererPickupDistance",
                5f, new ConfigDescription("How close a Neckro needs to be to an item to pick it up.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
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

            _monsterAI = GetComponent<MonsterAI>();
        }

        private void Update()
        {
            if (ZNet.instance == null
                || !(Time.time > lastUpdate)) return;
            
            
            bool canPick = LookForNearbyItems();
            if (canPick) {
                AttemptItemPickup();
            } else {
                if (container.GetInventory().NrOfItems() > 0)
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
                } else {
                    NeckroStatus = "";
                }
            }
            //todo: loot dead gatherers
            
            if (ShowMessages.Value
                && !String.IsNullOrEmpty(NeckroStatus)
                && Player.m_localPlayer != null
                && Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < 5)
            {
                Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", NeckroStatus, false);   
            }

            lastUpdate = Time.time + UpdateDelay.Value;
        }

        private bool LookForNearbyItems()
        {
            if (InventoryFull()) return false;

            if (_currentItem == null)
            {
                _currentItem = FindClosest<ItemDrop>(LookRadius.Value, autoPickupMask, 
                    drop => drop.GetTimeSinceSpawned() > PickupDelay.Value
                            && !_itemDrops.Contains(drop));
            }
            if (_currentItem == null) return false;
            _itemDrops.Add(_currentItem);
            // move toward that item
            NeckroStatus = $"Moving toward {_currentItem.m_itemData.m_shared.m_name}";
            _monsterAI.SetFollowTarget(_currentItem.gameObject);
            return true;
        }

        private void AttemptItemPickup()
        {
            if (_currentItem is null)
            {
                NeckroStatus = "Looking for items...";
                _monsterAI.SetFollowTarget(null);
                return;
            }

            if (_currentItem.CanPickup()
                && Vector3.Distance(_currentItem.transform.position, transform.position) <= PickupDistance.Value
                && StoreItem(_currentItem, container))
            {
                NeckroStatus = $"Picking up {string.Join(", ", _currentItem.m_itemData.m_shared.m_name)}";
                if (_itemDrops.Contains(_currentItem)) _itemDrops.Remove(_currentItem);
                _currentItem = null;
            }
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

        private bool InventoryFull()
        {
            // return home if no slots found
            return container.GetInventory().GetEmptySlots() < 1;
        }

        private Container GetNearestDropOffPoint() {
            // Container closestContainer;
            Container closestContainer = FindClosest<Container>(DropoffPointRadius.Value, pieceMask,
                c => c.GetInventory().GetEmptySlots() > 0);
            if (closestContainer == null) return null;
            
            // move toward that piece
            _monsterAI.SetFollowTarget(closestContainer.gameObject);
            return closestContainer;
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
