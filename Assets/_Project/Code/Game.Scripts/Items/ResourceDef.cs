using System;

namespace Game.Scripts.Items
{
    [Serializable]
    public sealed class ResourceDef : ItemDef
    {
        public override BaseItem CreateItem(IItemView view) => new ResourceItem(this, view);
    }
}
