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
        Pose Pose { get; }

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
        private const float MovedDistance = 0.005f;
        private const float MovedAngle = 0.5f;

        private readonly Pose origin;

        public ItemDef Def { get; }
        public IItemView View { get; }
        public IItemContainer Container { get; private set; }

        // Saves point at the item by it; -1 for an item the level did not place.
        public int Id { get; set; } = -1;

        protected BaseItem(ItemDef def, IItemView view)
        {
            Def = def ?? throw new ArgumentNullException(nameof(def));
            View = view;
            origin = view.Pose;
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

        // Only a loose item away from where the level put it: the rest is the level itself or its container's save.
        public SaveData Save()
        {
            if (Container != null) return null;

            var pose = View.Pose;
            var moved = (pose.position - origin.position).sqrMagnitude >= MovedDistance * MovedDistance ||
                        Quaternion.Angle(pose.rotation, origin.rotation) >= MovedAngle;
            return moved ? new SaveData { Pose = pose } : null;
        }

        public void Load(SaveData data)
        {
            if (data == null || Container != null) return;

            Release(data.Pose.position, data.Pose.rotation, Vector3.zero);
        }

        [Serializable]
        public class SaveData
        {
            public Pose Pose;
        }
    }
}
