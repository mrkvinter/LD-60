using UnityEngine;

namespace Game.Scripts.Items
{
    /// <summary>Something the item in hand can be put into: a rack or a construction site.</summary>
    public interface IItemReceiver
    {
        bool CanPut(BaseItem item);
        void Put(BaseItem item, Vector3? viewer);
    }
}
