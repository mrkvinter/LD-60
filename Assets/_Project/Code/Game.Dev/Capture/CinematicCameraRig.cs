using Unity.Cinemachine;
using UnityEngine;

namespace Code.Dev.Capture
{
    /// <summary>
    /// Ставит камеру на постановочный ракурс вокруг персонажа — для записи роликов.
    /// </summary>
    [ExecuteAlways]
    public class CinematicCameraRig : MonoBehaviour
    {
        [SerializeField] private bool staged;

        [Header("Кого снимаем")]
        [SerializeField] private Transform subject;

        [Tooltip("Если задан, кадр строится по середине между двумя объектами")]
        [SerializeField] private Transform secondSubject;

        [Header("Ракурс")]
        [Tooltip("Поворот вокруг персонажа от его взгляда: 0 — в лицо, 90 — сбоку, 180 — со спины")]
        [SerializeField, Range(-180f, 180f)] private float yaw = 90f;

        [SerializeField, Range(-40f, 80f)] private float pitch = 8f;
        [SerializeField, Min(0.1f)] private float distance = 4.6f;

        [Tooltip("Высота точки, в которую смотрит камера, от земли под персонажем")]
        [SerializeField] private float focusHeight = 0.85f;

        [SerializeField, Range(10f, 120f)] private float fieldOfView = 50f;

        [Header("Камера")]
        [Tooltip("Пусто — берётся Camera.main")]
        [SerializeField] private Camera targetCamera;

        private Camera stagedCamera;
        private CinemachineBrain stagedBrain;
        private float restoreFieldOfView;

        public bool Staged
        {
            get => staged;
            set => staged = value;
        }

        public Transform Subject
        {
            get => subject;
            set => subject = value;
        }

        public Transform SecondSubject
        {
            get => secondSubject;
            set => secondSubject = value;
        }

        private void OnDisable() => Release();

        private void LateUpdate()
        {
            if (!staged)
            {
                Release();
                return;
            }

            if (!Stage()) return;

            Apply();
        }

        private bool Stage()
        {
            if (stagedCamera != null) return true;

            var camera = targetCamera != null ? targetCamera : Camera.main;
            if (camera == null || subject == null) return false;

            stagedCamera = camera;
            restoreFieldOfView = camera.fieldOfView;

            // Cinemachine пишет трансформ камеры каждый LateUpdate, так что без выключенного
            // мозга ракурс не удержать.
            stagedBrain = camera.GetComponent<CinemachineBrain>();
            if (stagedBrain != null) stagedBrain.enabled = false;

            return true;
        }

        private void Release()
        {
            if (stagedCamera == null) return;

            stagedCamera.fieldOfView = restoreFieldOfView;
            if (stagedBrain != null) stagedBrain.enabled = true;

            stagedCamera = null;
            stagedBrain = null;
        }

        private void Apply()
        {
            var pivot = secondSubject != null
                ? (subject.position + secondSubject.position) * 0.5f
                : subject.position;

            var focus = pivot + Vector3.up * focusHeight;
            var direction = Quaternion.AngleAxis(yaw, Vector3.up) * subject.forward;
            var orbit = Quaternion.AngleAxis(-pitch, Vector3.Cross(Vector3.up, direction)) * direction;

            stagedCamera.transform.position = focus + orbit * distance;
            stagedCamera.transform.rotation = Quaternion.LookRotation(focus - stagedCamera.transform.position);
            stagedCamera.fieldOfView = fieldOfView;
        }
    }
}
