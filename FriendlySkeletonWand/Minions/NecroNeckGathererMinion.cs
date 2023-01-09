using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using Jotunn.Managers;
using UnityEngine;
namespace FriendlySkeletonWand.Minions
{
    internal class NecroNeckGathererMinion : UndeadMinion
    {
        // for limits checking
        private static int createdOrderIncrementer;
        public int createdOrder;

        private float lastUpdate;

        public static ConfigEntry<bool> allowed;
        public static ConfigEntry<float> updateDelay, lookRadius;

        private int autoPickupMask;

        private Container container;

        public static void CreateConfigs(BaseUnityPlugin plugin)
        {
            allowed = plugin.Config.Bind("Server config", "NecroNeckGathererAllowed",
                true, new ConfigDescription("Whether the NecroNeck Gatherer is allowed or not.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            lookRadius = plugin.Config.Bind("Server config", "NecroNeckGathererLookRadius",
                10f, new ConfigDescription("The radius in which the NecroNeck Gatherer can pickup items from.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
            updateDelay = plugin.Config.Bind("Server config", "NecroNeckGathererUpdateDelay",
                3f, new ConfigDescription("The delay, in seconds, between item pickup attempts. Attention: small values may impact performance.", null,
                new ConfigurationManagerAttributes { IsAdminOnly = true }));
        }

        private void Awake()
        {
            createdOrderIncrementer++;
            createdOrder = createdOrderIncrementer;

            Tameable tameable = GetComponent<Tameable>();
            if (tameable != null)
            {
                // let the minions generate a little necromancy XP for their master
                tameable.m_levelUpOwnerSkill = SkillManager.Instance.GetSkill(BasePlugin.necromancySkillIdentifier).m_skill;
            }

            container = GetComponent<Container>();

            container.m_height = LargeCargoCrate.containerHeight.Value;
            container.m_width = LargeCargoCrate.containerWidth.Value;

            autoPickupMask = LayerMask.GetMask(new string[1] { "item" });
        }

        private void Update()
        {
            if (ZNet.instance != null && Time.time > lastUpdate)
            {
                LookForNearbyItems();

                lastUpdate = Time.time + updateDelay.Value;
            }
        }

        private void LookForNearbyItems()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position+Vector3.up, lookRadius.Value, autoPickupMask);
            foreach (var hitCollider in hitColliders)
            {
                ItemDrop itemDrop = hitCollider.GetComponentInParent<ItemDrop>();
                if (itemDrop != null)
                {
                    if (itemDrop.CanPickup())
                    {
                        StoreItem(itemDrop);
                    }
                }
            }
        }

        private void StoreItem(ItemDrop itemDrop)
        {
            ItemDrop.ItemData itemData = itemDrop.m_itemData;
            if (itemData == null) return;

            if (itemData.m_stack < 1) return;

            while (itemData.m_stack-- > 0 && container.GetInventory().CanAddItem(itemData, 1))
            {
                ItemDrop.ItemData newItemData = itemData.Clone();
                newItemData.m_stack = 1;
                container.GetInventory().AddItem(newItemData);
            }

            container.Save();

            if (itemDrop.GetComponent<ZNetView>() == null)
                DestroyImmediate(itemDrop.gameObject);
            else
                ZNetScene.instance.Destroy(itemDrop.gameObject);
        }
    }
}
