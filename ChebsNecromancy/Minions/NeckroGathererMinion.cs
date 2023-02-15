using BepInEx.Configuration;
using ChebsNecromancy.CustomPrefabs;
using System.Collections.Generic;
using System.Linq;
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

        public new static void CreateConfigs(BasePlugin plugin)
        {
            Allowed = plugin.ModConfig("NeckroGatherer", "NeckroGathererAllowed",
                true, "Whether the Neckro Gatherer is allowed or not.", plugin.BoolValue, true);
            LookRadius = plugin.ModConfig("NeckroGatherer", "NeckroGathererLookRadius", 500f,
                "The radius in which the Neckro Gatherer can see items from.", plugin.FloatQuantityValue, true);
            PickupRadius = plugin.ModConfig("NeckroGatherer", "NeckroGathererPickupRadius", 10f,
                "The radius in which the Neckro Gatherer can pickup items from.", plugin.FloatQuantityValue, true);
            DropoffPointRadius = plugin.ModConfig("NeckroGatherer", "NeckroGathererDropoffPointRadius", 1000f,
                "The radius in which the Neckro Gatherer looks for a container to store its load in.", plugin.FloatQuantityValue,
                true);
            UpdateDelay = plugin.ModConfig("NeckroGatherer", "NeckroGathererUpdateDelay", 3f,
                "The delay, in seconds, between item searching & pickup attempts. Attention: small values may impact performance.",
                plugin.FloatQuantityValue, true);
            ShowMessages = plugin.ModConfig("NeckroGatherer", "NeckroGathererShowMessages", true,
                "Whether the Neckro Gatherer talks or not.", plugin.BoolValue);
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
            if (ZNet.instance == null || !(Time.time > lastUpdate)) return;
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
                && Vector3.Distance(Player.m_localPlayer.transform.position, transform.position) < 5)
            {
                Chat.instance.SetNpcText(gameObject, Vector3.up, 5f, 2f, "", NeckroStatus, false);
            }

            lastUpdate = Time.time + UpdateDelay.Value;
        }

        private void LookForNearbyItems()
        {
            // get all nearby items
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, LookRadius.Value, autoPickupMask);
            if (hitColliders.Length < 1) return;
            // order items from closest to furthest, then take closest one
            Collider closest = hitColliders
                .OrderBy(col => Vector3.Distance(transform.position, col.transform.position))
                .FirstOrDefault();
            if (closest != null)
            {
                ItemDrop itemDrop = closest.GetComponentInParent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (TryGetComponent(out MonsterAI monsterAI))
                    {
                        // move toward that item
                        NeckroStatus = $"Moving toward {itemDrop.m_itemData.m_shared.m_name}";
                        monsterAI.SetFollowTarget(itemDrop.gameObject);
                        return;
                    }
                }
            }
        }

        private void PickupNearbyItems()
        {
            List<string> itemNames = new();
            Collider[] hitColliders = Physics.OverlapSphere(transform.position + Vector3.up, PickupRadius.Value, autoPickupMask);
            foreach (var hitCollider in hitColliders)
            {
                ItemDrop itemDrop = hitCollider.GetComponentInParent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (itemDrop.CanPickup())
                    {
                        itemNames.Add(itemDrop.m_itemData.m_shared.m_name);
                        StoreItem(itemDrop, container);
                    }
                }
            }

            NeckroStatus = itemNames.Count > 0
                ? $"Picking up {string.Join(", ", itemNames)}"
                : "Looking for items...";
        }

        private void StoreItem(ItemDrop itemDrop, Container depositContainer)
        {
            NeckroStatus = $"Storing {itemDrop.m_itemData.m_shared.m_name} in {depositContainer.m_name}";

            ItemDrop.ItemData itemData = itemDrop.m_itemData;
            if (itemData == null) return;

            if (itemData.m_stack < 1) return;

            int originalStackSize = itemData.m_stack;
            int itemsDeposited = 0;

            while (itemData.m_stack-- > 0 && depositContainer.GetInventory().CanAddItem(itemData, 1))
            {
                ItemDrop.ItemData newItemData = itemData.Clone();
                newItemData.m_stack = 1;
                depositContainer.GetInventory().AddItem(newItemData);
                itemsDeposited++;
            }

            itemData.m_stack -= itemsDeposited;

            depositContainer.Save();

            // if the stack was completely deposited, destroy the item
            if (itemData.m_stack <= 0)
            {
                if (itemDrop.GetComponent<ZNetView>() == null)
                    DestroyImmediate(itemDrop.gameObject);
                else
                    ZNetScene.instance.Destroy(itemDrop.gameObject);
            }
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
                .FirstOrDefault(piece => piece.GetComponentInParent<Container>() != null
                    && piece.GetComponentInParent<Container>().GetInventory().GetEmptySlots() > 0);
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
