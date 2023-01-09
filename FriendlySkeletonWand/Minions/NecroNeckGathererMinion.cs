using System;
using System.Collections;
using Jotunn.Managers;
using UnityEngine;
namespace FriendlySkeletonWand.Minions
{
    internal class NecroNeckGathererMinion : UndeadMinion
    {
        // for limits checking
        private static int createdOrderIncrementer;
        public int createdOrder;

        private float updateDelay;

        //todo expose to config
        public const float lookRadius = 5f;

        private int autoPickupMask;

        private Container container;

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

            autoPickupMask = LayerMask.GetMask(new string[1] { "item" });
        }

        private void Update()
        {
            if (Time.time > updateDelay)
            {
                LookForNearbyItems();

                updateDelay = Time.time + 5f;
            }
        }

        private void LookForNearbyItems()
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position+Vector3.up, lookRadius, autoPickupMask);
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
