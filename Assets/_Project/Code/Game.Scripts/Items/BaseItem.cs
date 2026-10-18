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
        Bounds LocalBounds { get; }

        void SetHighlighted(bool highlighted);
        void SetMarked(bool marked);
        void SetVisible(bool visible);
        void SetMode(ItemMode mode);
        void FlyTo(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale, bool instant);
        void SwapIn(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale, Vector3 fromLocalPosition);
        void SwapOut(Vector3 toLocalPosition);
        void Release(Vector3 position, Quaternion rotation, Vector3 velocity);
        void Settle();
        void Consume();
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

        public void SwapIn(Transform parent, Vector3 localPosition, Quaternion localRotation, float scale,
            Vector3 fromLocalPosition)
        {
            View.SwapIn(parent, localPosition, localRotation, scale, fromLocalPosition);
        }

        public void SwapOut(Vector3 toLocalPosition)
        {
            View.SwapOut(toLocalPosition);
        }

        public void Release(Vector3 position, Quaternion rotation, Vector3 velocity)
        {
            SetContainer(null);
            View.Release(position, rotation, velocity);
        }

        public void Settle()
        {
            View.Settle();
        }

        public void SetHighlighted(bool highlighted)
        {
            View.SetHighlighted(highlighted);
        }

        public void SetMarked(bool marked)
        {
            View.SetMarked(marked);
        }

        public void SetVisible(bool visible)
        {
            View.SetVisible(visible);
        }

        public void Consume()
        {
            View.Consume();
        }
    }
}
