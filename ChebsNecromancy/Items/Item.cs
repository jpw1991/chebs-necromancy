using ChebsNecromancy.Common;
using System;

namespace ChebsNecromancy.Items
{
    public class InternalName : Attribute
    {
        public readonly string Name;
        public InternalName(string internalName) => Name = internalName;
    }

    internal class Item
    {
        public virtual void CreateConfigs(BasePlugin plugin) { }
        public ChebsRecipe ChebsRecipeConfig = new();

        // coroutines cause problems and this is not a monobehavior, but we
        // may still want some stuff to happen during update.
        protected float DoOnUpdateDelay;
        public virtual void DoOnUpdate()
        {

        }
    }
}
