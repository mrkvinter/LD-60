using Code.Game.Core.ExecutorSystem;
using Code.Game.Core.ExecutorSystem.Actions;
using Code.Game.Core.ExecutorSystem.Conditions;
using Code.Game.Scripts.EntitySystem;
using Game.Core.Contexts;
using Game.Scripts.Player;
using UnityEngine;

namespace Game.Scripts.Triggers
{
    [RequireComponent(typeof(Collider))]
    public sealed class ActionTriggerZone : MonoBehaviour
    {
        [Tooltip("Only the player sets it off; otherwise any collider does.")]
        [SerializeField] private bool playerOnly = true;

        [Tooltip("Fires once, then ignores everyone.")]
        [SerializeField] private bool once = true;

        [Tooltip("Empty means always.")]
        [SerializeField] private ConditionJunction conditions;
        [SerializeField] private GameActionJunction actions;

        private bool fired;

        private void Reset()
        {
            if (TryGetComponent<Collider>(out var zone)) zone.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (once && fired) return;
            if (playerOnly && !IsPlayer(other)) return;

            var executor = GameServiceLocator.Resolve<ActionExecutorSystem>();
            if (!executor.Check(conditions)) return;

            fired = true;
            executor.Execute(actions);
        }

        private static bool IsPlayer(Collider other)
        {
            var adapter = other.GetComponentInParent<IEntityAdapter>();
            return adapter?.BaseEntity?.GetComponent<IsPlayerComponent>() != null;
        }
    }
}
