using Code.Game.Core;
using Code.Game.Core.ExecutorSystem.Conditions;
using Game.Core.Contexts;
using UnityEngine;

namespace Game.Scripts.Conditions
{
    public sealed class VisibilityComponent : ObjectRegistry.MonoRegistered<VisibilityComponent>, IConditionalItem
    {
        [Tooltip("What gets shown and hidden; empty means this object. This component's own object must start " +
                 "active, or it never registers: to hide something from the start, put this on an active parent.")]
        [SerializeField] private GameObject target;

        [Tooltip("Empty means always visible.")]
        [SerializeField] private ConditionJunction visibilityCondition;

        private ConditionService conditionService;

        public ConditionJunction ConditionJunction => visibilityCondition;

        public bool IsActive => target.activeSelf;

        private void Reset()
        {
            target = gameObject;
        }

        protected override void OnInitialize()
        {
            if (target == null) target = gameObject;

            var service = GameServiceLocator.Resolve<ConditionService>();
            service.Subscribe(this);
            conditionService = service;
        }

        protected override void OnDispose()
        {
            conditionService?.Unsubscribe(this);
        }

        public void Activate() => target.SetActive(true);

        public void Deactivate() => target.SetActive(false);
    }
}
