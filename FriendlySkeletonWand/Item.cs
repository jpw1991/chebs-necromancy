using Jotunn.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FriendlySkeletonWand
{
    internal class Item
    {
        public string ItemName;

        public virtual CustomItem GetCustomItem()
        {
            return null;
        }
    }
}
