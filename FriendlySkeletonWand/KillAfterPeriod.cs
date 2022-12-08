using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Jotunn.Configs;
using Jotunn.Managers;

namespace FriendlySkeletonWand
{
    public class KillAfterPeriod : MonoBehaviour
    {
        public float period = 10;

        private void Start()
        {
            StartCoroutine(Killer());
        }

        IEnumerator Killer()
        {
            yield return new WaitForSeconds(period);
            if (TryGetComponent(out Humanoid humanoid))
            {
                humanoid.SetHealth(0);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
