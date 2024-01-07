using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using ChebsNecromancy.CustomPrefabs;
using ChebsValheimLibrary.Common;
using ChebsValheimLibrary.Minions;
using UnityEngine;
using Logger = Jotunn.Logger;

namespace ChebsNecromancy.Minions
{
    internal class NeckroGathererMinion : UndeadMinion
    {
        public const string NeckroHomeZdoKey = "NeckroHome";

        // for limits checking
        private static int _createdOrderIncrementer;

        private float _lastUpdate;

        public static ConfigEntry<float> UpdateDelay,
            LookRadius,
            DropoffPointRadius,
            PickupDelay,
            PickupDistance,
            MaxSecondsBeforeDropoff;

        public static MemoryConfigEntry<string, List<string>> ContainerWhitelist;

        private static List<ItemDrop> _itemDrops = new();

        public string NeckroStatus { get; set; }

        private int _autoPickupMask, _pieceMask;

        private Container _container;

        private Container _dropoffTarget;

        private MonsterAI _monsterAI;

        private Humanoid _humanoid;

        private ItemDrop _currentItem;

        private float _lastDropoffAt;

        private GameObject _homeObject;

        public new static void CreateConfigs(BaseUnityPlugin plugin)
        {
            LookRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererLookRadius",
                500f, new ConfigDescription("The radius in which the Neckro Gatherer can see items from.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            DropoffPointRadius = plugin.Config.Bind("NeckroGatherer (Server Synced)",
                "NeckroGathererDropoffPointRadius",
                1000f, new ConfigDescription(
                    "The radius in which the Neckro Gatherer looks for a container to store its load in.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            UpdateDelay = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererUpdateDelay",
                6f, new ConfigDescription(
                    "The delay, in seconds, between item searching & pickup attempts. Attention: small values may impact performance.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            PickupDelay = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererPickupDelay",
                10f, new ConfigDescription(
                    "The Neckro won't pick up items immediately upon seeing them. Rather, it will make note of them and pick them up if they're still on the ground after this delay.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            PickupDistance = plugin.Config.Bind("NeckroGatherer (Server Synced)", "NeckroGathererPickupDistance",
                5f, new ConfigDescription("How close a Neckro needs to be to an item to pick it up.", null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }));
            MaxSecondsBeforeDropoff = plugin.Config.Bind("NeckroGatherer (Client)", "MaxSecondsBeforeDropoff",
                0f,
                new ConfigDescription(
                    "The maximum amount of time, in seconds, before a Neckro is forced to return whatever it is currently carrying. If set to 0, this condition is ignored."));

            var containerWhitelist = plugin.Config.Bind("NeckroGatherer (Server Synced)", "ContainerWhitelist",
                "piece_chest_wood", new ConfigDescription(
                    "The containers that are considered dropoff points. Please use a comma-delimited list of prefab names.",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                ));
            ContainerWhitelist =
                new MemoryConfigEntry<string, List<string>>(containerWhitelist, s => s?.Split(',').Select(str => str.Trim()).ToList());
        }

        public override void Awake()
        {
            base.Awake();
            _createdOrderIncrementer++;
            createdOrder = _createdOrderIncrementer;

            _container = GetComponent<Container>();

            _autoPickupMask = LayerMask.GetMask("item");
            _pieceMask = LayerMask.GetMask("piece");

            canBeCommanded = false;

            _monsterAI = GetComponent<MonsterAI>();
            _humanoid = GetComponent<Humanoid>();

            StartCoroutine(WaitForZNet());
        }

        IEnumerator WaitForZNet()
        {
            yield return new WaitUntil(() => ZNetScene.instance != null);

            // wondering what the code below does? Check comments in the
            // FreshMinion.cs file.
            var freshMinion = GetComponent<FreshMinion>();

            yield return new WaitUntil(() => Player.m_localPlayer != null);

            if (freshMinion != null)
            {
                // record home position as current position
                Home = transform.position;

                // remove the component
                Destroy(freshMinion);
            }
        }

        private void Update()
        {
            if (ZNet.instance == null
                || !(Time.time > _lastUpdate)) return;

            // Some users get null object exceptions inside the neckro's Update method. IDK why exactly that would be.
            // Mod conflicts? Don't know. So to mitigate this, just be extra careful about nulls and abort if anything
            // is null.
            if (_container == null && !TryGetComponent(out _container))
            {
                Logger.LogError("Neckro container is null and cannot be retrieved!");
                return;
            }

            if (_monsterAI == null && !TryGetComponent(out _monsterAI))
            {
                Logger.LogError("Neckro MonsterAI is null and cannot be retrieved!");
                return;
            }

            bool canPick = LookForNearbyItems();
            if (canPick)
            {
                AttemptItemPickup();
            }
            else
            {
                if (_container.GetInventory().NrOfItems() > 0)
                {
                    var home = Home;
                    if (home.Equals(Vector3.negativeInfinity) // unset for some reason
                        || Vector3.Distance(home, transform.position) <= DropoffPointRadius.Value)
                    {
                        _dropoffTarget = GetNearestDropOffPoint();
                        if (_dropoffTarget == null || _dropoffTarget.gameObject == null)
                        {
                            NeckroStatus = "Can't find a container";
                        }
                        else
                        {
                            _monsterAI.SetFollowTarget(_dropoffTarget.gameObject);
                            NeckroStatus = $"Moving toward {_dropoffTarget.name}";
                            if (CloseToDropoffPoint())
                            {
                                DepositItems();
                            }
                        }
                    }
                    else
                    {
                        NeckroStatus = $"Returning home! ({home})";
                        if (_homeObject != null) Destroy(_homeObject);
                        _homeObject = new GameObject();
                        _homeObject.transform.position = home;
                        _monsterAI.SetFollowTarget(_homeObject);
                    }
                }
                else
                {
                    NeckroStatus = "";
                }
            }
            //todo: loot dead gatherers

            _humanoid.m_name = NeckroStatus;

            _lastUpdate = Time.time
                          + UpdateDelay.Value
                          + Random.value; // add a fraction of a second so that multiple
            // workers don't all simultaneously scan
        }

        private bool LookForNearbyItems()
        {
            if (InventoryFull
                || (InventoryHasItems
                    && MaxSecondsBeforeDropoff.Value > 0
                    && Time.time - _lastDropoffAt >= MaxSecondsBeforeDropoff.Value)) return false;

            if (_currentItem == null)
            {
                _currentItem = FindClosest<ItemDrop>(transform, LookRadius.Value, _autoPickupMask,
                    drop => drop.GetTimeSinceSpawned() > PickupDelay.Value
                            && !_itemDrops.Contains(drop), true);
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
                && StoreItem(_currentItem, _container))
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

        private bool InventoryFull => _container.GetInventory().GetEmptySlots() < 1;

        private bool InventoryHasItems => _container.GetInventory().NrOfItems() > 0;

        private Container GetNearestDropOffPoint()
        {
            var allowedContainers = ContainerWhitelist.Value;
            if (allowedContainers == null)
            {
                Logger.LogError("allowedContainers is null");
                Logger.LogInfo(
                    $"allowedContainers = {ContainerWhitelist.Value}, ContainerWhitelist.ConfigEntry.Value = {ContainerWhitelist.ConfigEntry.Value}");
                return null;
            }

            var closestContainer = FindClosest<Container>(transform, DropoffPointRadius.Value, _pieceMask,
                c => c.m_piece != null
                     && c.m_piece.IsPlacedByPlayer()
                     && allowedContainers.Contains(c.m_piece.m_nview.GetPrefabName())
                     && c.GetInventory() != null
                     && c.GetInventory().GetEmptySlots() > 0, true);
            if (closestContainer == null) return null;

            return closestContainer;
        }

        private bool CloseToDropoffPoint()
        {
            return _dropoffTarget != null
                   && Vector3.Distance(transform.position, _dropoffTarget.transform.position) < PickupDistance.Value;
        }

        private void DepositItems()
        {
            _lastDropoffAt = Time.time;
            _dropoffTarget.GetInventory().MoveAll(_container.GetInventory());
        }

        #region HomeZDO

        public Vector3 Home
        {
            get => TryGetComponent(out ZNetView zNetView)
                ? zNetView.GetZDO().GetVec3(NeckroHomeZdoKey, Vector3.negativeInfinity)
                : Vector3.negativeInfinity;
            set
            {
                if (TryGetComponent(out ZNetView zNetView))
                {
                    zNetView.GetZDO().Set(NeckroHomeZdoKey, value);
                }
                else
                {
                    Logger.LogError($"Cannot set neckro home to {value} because it has no ZNetView component.");
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (_homeObject != null) Destroy(_homeObject);
        }
    }
}