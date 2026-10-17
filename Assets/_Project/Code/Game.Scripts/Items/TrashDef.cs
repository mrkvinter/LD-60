using System;

namespace Game.Scripts.Items
{
    [Serializable]
    public sealed class TrashDef : ItemDef
    {
        public override BaseItem CreateItem(IItemView view) => new TrashItem(this, view);
    }
}
