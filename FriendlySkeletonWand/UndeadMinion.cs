
using UnityEngine;
using static ClutterSystem;

namespace FriendlySkeletonWand
{
    internal class UndeadMinion : MonoBehaviour
    {
        // we add this component to the creatures we create in the mod
        // so that we can use .GetComponent<FriendlySkeletonMinion>()
        // to determine whether a creature was created by the mod, or
        // whether it was created by something else.
        //
        // This allows us to only call wait/follow/whatever on minions
        // that the mod has created and it also persists across sessions,
        // unlike gameObject.name, so that these commands will work
        // even after logging out and coming back in.

        public bool canBeCommanded = true;

        private void OnCollisionEnter(Collision collision)
        {
            // ignore collision with player
            Character character = collision.gameObject.GetComponent<Character>();
            if (character != null && character.m_faction == Character.Faction.Players)
            {
                Physics.IgnoreCollision(collision.gameObject.GetComponent<Collider>(), GetComponent<Collider>());
                return;
            }
        }
    }
}
