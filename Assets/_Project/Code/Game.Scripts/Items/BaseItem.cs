using System;
using Code.Game.Scripts.EntitySystem;
using UnityEngine;

namespace Game.Scripts.Items
{
    public enum ItemMode
    {
        Loose,
        Held,
        Stored
    }

    public interface IItemView
    {
        void SetHighlighted(bool highlighted);
        void SetMode(ItemMode mode);
        void FlyTo(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale, bool instant);
        void Release(Vector3 position, Quaternion rotation, Vector3 velocity);
    }

    public abstract class BaseItem : Entity
    {
        public ItemDef Def { get; }
        public IItemView View { get; }
        public IItemContainer Container { get; private set; }

        protected BaseItem(ItemDef def, IItemView view)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            View = view;
            view.SetMode(ItemMode.Loose);
        }

        public void SetContainer(IItemContainer container)
        {
            Container = container;
            View.SetMode(container?.Mode ?? ItemMode.Loose);
        }

        public void Place(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale = 1f,
            bool instant = false)
        {
            View.FlyTo(parent, localPosition, localRotation, scale, instant);
        }

        public void Release(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            SetContainer(null);
            View.Release(position, rotation, velocity);
        }

        public void SetHighlighted(bool highlighted)
        {
            View.SetHighlighted(highlighted);
        }
    }
}
