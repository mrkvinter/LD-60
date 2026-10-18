using System;
using Code.Game.Scripts.EntitySystem;
using ECM2;
using Game.Scripts.Movement;
using Game.Scripts.Player;
using UnityEngine;
using VContainer.Unity;

namespace Game.Integrations.Ecm2
{
    public sealed class Ecm2MovementAdapter : MovementAdapter
    {
        [SerializeField] private Character character;
        [SerializeField] private Ecm2MovementComponent.Settings settings = new();

        private void Reset()
        {
            character = GetComponent<Character>();
        }

        protected override IMovementComponent CreateComponent()
        {
            if (character == null)
            {
                Debug.LogError($"{nameof(Ecm2MovementAdapter)} on '{name}' has no {nameof(Character)}; the player will not move.", this);
            }

            return new Ecm2MovementComponent(Entity, character, settings);
        }
    }

    public sealed class Ecm2MovementComponent : IMovementComponent, IInitializable, ITickable
    {
        [Serializable]
        public sealed class Settings
        {
            [Tooltip("Max speed while sprinting, as a multiplier of the Character's max walk speed.")]
            public float SprintMultiplier = 1.6f;

            [Tooltip("Multiplier of mouse and stick look speed, on top of the Look scale in PlayerInputActions.")]
            [Min(0f)] public float LookSensitivity = 1f;

            [Tooltip("How far in degrees the camera can look down.")]
            public float MaxPitch = 80f;

            [Tooltip("How far in degrees the camera can look up.")]
            public float MinPitch = -80f;

            [Tooltip("How fast the camera drops to crouch height and rises back, in meters per second.")]
            [Min(0f)] public float CrouchCameraSpeed = 4f;
        }

        private readonly Entity entity;
        private readonly Character character;
        private readonly Settings settings;

        private PlayerInputComponent input;
        private PlayerAbilitiesComponent abilities;
        private Transform cameraAnchor;
        private float walkSpeed;
        private float pitch;
        private float standingCameraHeight;
        private bool isJumpHeld;

        public bool IsEnabled { get; set; } = true;

        public Ecm2MovementComponent(Entity entity, Character character, Settings settings)
        {
            this.entity = entity;
            this.character = character;
            this.settings = settings;
        }

        public void Initialize()
        {
            input = entity.GetComponent<PlayerInputComponent>();
            abilities = entity.GetComponent<PlayerAbilitiesComponent>();
            cameraAnchor = entity.GetComponent<CameraAnchorComponent>()?.Transform;
            if (cameraAnchor != null) standingCameraHeight = cameraAnchor.localPosition.y;

            if (character == null) return;

            character.SetRotationMode(Character.RotationMode.None);
            character.characterMovement.collisionBehaviourCallback = GetCollisionBehaviour;
            walkSpeed = character.maxWalkSpeed;

            if (abilities == null) return;

            abilities.Changed += ApplyJumpCount;
            ApplyJumpCount();
        }

        public void Tick()
        {
            if (character == null || input == null) return;

            if (!IsEnabled)
            {
                character.SetMovementDirection(Vector3.zero);
                character.UnCrouch();
                ReleaseJump();
                UpdateCameraHeight();
                return;
            }

            Move();
            Look();
            UpdateJump();
            UpdateCrouch();
            UpdateCameraHeight();
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (character == null) return;

            var forward = Vector3.ProjectOnPlane(rotation * Vector3.forward, character.GetUpVector());
            var heading = forward.sqrMagnitude > 1e-6f
                ? Quaternion.LookRotation(forward, character.GetUpVector())
                : character.GetRotation();

            var movement = character.characterMovement;
            var interpolation = movement.interpolation;
            movement.interpolation = RigidbodyInterpolation.None;
            movement.SetPositionAndRotation(position, heading, true);
            movement.interpolation = interpolation;
            character.SetVelocity(Vector3.zero);

            pitch = 0f;
            if (cameraAnchor != null) cameraAnchor.localRotation = Quaternion.identity;
        }

        private void Move()
        {
            var move = input.Move;
            var direction = new Vector3(move.x, 0f, move.y).relativeTo(character.transform, character.GetUpVector());

            character.SetMovementDirection(direction);
            character.maxWalkSpeed = input.Sprint && Can(PlayerAbility.Sprint) ? walkSpeed * settings.SprintMultiplier : walkSpeed;
        }

        private void Look()
        {
            var look = (input.IsPointerLook ? input.Look : input.Look * Time.deltaTime) * settings.LookSensitivity;

            character.AddYawInput(look.x);

            if (cameraAnchor == null) return;

            pitch = Mathf.Clamp(pitch + look.y, settings.MinPitch, settings.MaxPitch);
            cameraAnchor.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateJump()
        {
            var wantsJump = input.Jump && Can(PlayerAbility.Jump);
            if (wantsJump == isJumpHeld) return;

            isJumpHeld = wantsJump;
            if (isJumpHeld) character.Jump();
            else character.StopJumping();
        }

        // Loose items are rounded junk underfoot: any part of them can be stepped and stood on, else the steep
        // sides of a log or a crystal stop the player. ECM2 also rides any rigidbody under the feet as a moving
        // platform; a loose item rolling away would drag the player with it. Collisions with it stay.
        private const CollisionBehaviour LooseItemBehaviour = CollisionBehaviour.Walkable |
            CollisionBehaviour.CanStepOn | CollisionBehaviour.CanPerchOn | CollisionBehaviour.CanNotRideOn;

        private static CollisionBehaviour GetCollisionBehaviour(Collider collider) =>
            collider.attachedRigidbody is { isKinematic: false } ? LooseItemBehaviour : CollisionBehaviour.Default;

        // Without an abilities component nothing is locked.
        private bool Can(PlayerAbility ability) => abilities == null || abilities.Has(ability);

        private void ApplyJumpCount()
        {
            character.jumpMaxCount = Can(PlayerAbility.DoubleJump) ? 2 : 1;
        }

        private void ReleaseJump()
        {
            if (!isJumpHeld) return;

            isJumpHeld = false;
            character.StopJumping();
        }

        private void UpdateCrouch()
        {
            if (input.Crouch) character.Crouch();
            else character.UnCrouch();
        }

        private void UpdateCameraHeight()
        {
            if (cameraAnchor == null) return;

            var target = character.IsCrouched()
                ? standingCameraHeight - (character.unCrouchedHeight - character.crouchedHeight)
                : standingCameraHeight;

            var position = cameraAnchor.localPosition;
            position.y = Mathf.MoveTowards(position.y, target, settings.CrouchCameraSpeed * Time.deltaTime);
            cameraAnchor.localPosition = position;
        }
    }
}
