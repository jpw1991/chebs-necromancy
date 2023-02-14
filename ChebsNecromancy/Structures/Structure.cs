using ChebsNecromancy.Common;
using UnityEngine;

namespace ChebsNecromancy.Structures
{
    internal class Structure: MonoBehaviour
    {
        public virtual void CreateConfigs(BasePlugin plugin) { }
        public ChebsRecipe ChebsRecipeConfig;
    }
}
