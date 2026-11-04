using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Core.Contexts;
using Game.Scripts.Construction;
using Game.Scripts.Items;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Game.Scripts.Marketing
{
    public sealed class ItemFlyInShowcase : MonoBehaviour
    {
        private const int LengthSamples = 24;
        private const float OffShotPenalty = 1e6f;

        [SerializeField] private Camera shotCamera;
        [SerializeField] private bool playOnStart;
        [SerializeField, Min(0f)] private float startDelay = 1f;
        [SerializeField] private int seed = 60;

        [Tooltip("Fills only the racks and sites the camera sees; items still fly in from anywhere.")]
        [SerializeField] private bool onlyRacksInView = true;

        [Tooltip("Items farther from the camera take off first, so the wave rolls toward the viewer.")]
        [SerializeField] private bool farRacksFirst = true;

        [Tooltip("Resources fly into unbuilt construction sites and build them for real. Rewind does not take that back.")]
        [SerializeField] private bool feedConstructionSites = true;

        [Title("Pacing")]
        [Tooltip("Seconds from the first launch to the last one; the flights add a second or two on top.")]
        [SerializeField, Min(0.1f)] private float launchWindow = 7f;

        [SerializeField, Min(1)] private int batchSize = 10;

        [Tooltip("The pause after a batch grows as (closeUpDistance / its distance to the camera) ^ this, so far " +
                 "batches rush by and near ones get the screen time. 0 spaces batches evenly.")]
        [SerializeField, Min(0f)] private float closeUpWeight = 1.5f;

        [Tooltip("Items closer to the camera than this all count as equally close, in meters.")]
        [SerializeField, Min(0.1f)] private float closeUpDistance = 4f;

        [Tooltip("Random delay of each item inside its batch, in seconds.")]
        [SerializeField, Min(0f)] private float itemStagger = 0.3f;

        [Title("Flight")]
        [Tooltip("Flight time for a short hop (x) and for distanceForMaxDuration and farther (y).")]
        [SerializeField] private Vector2 duration = new(1.1f, 2.2f);

        [SerializeField, Min(0.1f)] private float distanceForMaxDuration = 20f;

        [Tooltip("Share of the path covered over normalized time; the path is measured by length, so this is the speed graph.")]
        [SerializeField] private AnimationCurve progressCurve = new(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.35f, 0.2f, 1.1f, 1.1f),
            new Keyframe(0.75f, 0.9f, 1f, 1f),
            new Keyframe(1f, 1f, 0f, 0f));

        [Tooltip("How high the item first goes straight up, in meters.")]
        [SerializeField] private Vector2 liftHeight = new(0.8f, 1.6f);

        [Tooltip("Extra arc height per meter of flight.")]
        [SerializeField, Min(0f)] private float arcPerMeter = 0.25f;

        [Tooltip("Sideways bend per meter of flight, random side.")]
        [SerializeField, Min(0f)] private float swayPerMeter = 0.15f;

        [Tooltip("Where the item comes into its slot from: up and back toward the camera, in meters.")]
        [SerializeField] private Vector2 approach = new(0.6f, 0.8f);

        [Tooltip("Whole turns around a random axis during the flight.")]
        [SerializeField] private Vector2Int tumbleTurns = new(0, 2);

        [Tooltip("Share of the flight by which the item is turned like its slot.")]
        [SerializeField, Range(0.1f, 1f)] private float alignBy = 0.85f;

        [Tooltip("Lights a flying item by the probes at the height between its start and its slot, not up on the arc, " +
                 "where the probes near the ceiling would blow it out.")]
        [SerializeField] private bool groundLighting = true;

        [Tooltip("How far above that height a flying item may still take its lighting from, in meters: " +
                 "0 keeps it as dark as on the floor, more lets it brighten as it rises.")]
        [SerializeField, Min(0f)] private float lightLift = 3f;

        [Title("Landing")]
        [SerializeField, Min(0f)] private float landingPunch = 0.12f;
        [SerializeField, Min(0.01f)] private float landingPunchDuration = 0.18f;

        [Tooltip("Share of the flight after which a resource shrinks into its construction site.")]
        [SerializeField, Range(0f, 0.99f)] private float absorbFrom = 0.8f;

        [Tooltip("How far from the middle of a site resources may end up, as a share of its size.")]
        [SerializeField, Range(0f, 1f)] private float siteSpread = 0.5f;

        private readonly List<Flight> flights = new();
        private readonly List<Snapshot> snapshots = new();
        private readonly ShowcaseSlot slot = new();
        private System.Random random;
        private float clock;

        public bool IsPlaying { get; private set; }

        private IEnumerator Start()
        {
            if (!playOnStart) yield break;

            yield return new WaitForSeconds(startDelay);
            Play();
        }

        [Button, EnableIf("@UnityEngine.Application.isPlaying")]
        public void Play()
        {
            Rewind();

            var cam = shotCamera != null ? shotCamera : Camera.main;
            if (cam == null)
            {
                Debug.LogError($"{nameof(ItemFlyInShowcase)} has no camera.", this);
                return;
            }

            random = new System.Random(seed);
            var progress = GameServiceLocator.Resolve<SortingProgress>();
            var plan = Plan(progress, cam);
            if (plan.Count == 0)
            {
                Debug.LogWarning($"{nameof(ItemFlyInShowcase)}: no loose item fits a free slot.", this);
                return;
            }

            foreach (var item in progress.Items)
            {
                var t = ItemTransform(item);
                snapshots.Add(new Snapshot(item, t.parent, t.position, t.rotation, t.lossyScale));
            }

            Schedule(plan, cam);
            clock = 0f;
            IsPlaying = true;
        }

        [Button, EnableIf("@UnityEngine.Application.isPlaying")]
        public void Rewind()
        {
            IsPlaying = false;
            foreach (var flight in flights)
            {
                ReleaseProbeAnchor(flight);
            }

            flights.Clear();

            foreach (var snapshot in snapshots)
            {
                if (snapshot.Item.Container != slot || (Component)snapshot.Item.View == null) continue;

                SetCollidersEnabled(snapshot.Item, true);
                var t = ItemTransform(snapshot.Item);
                t.SetParent(null, true);
                t.localScale = snapshot.Scale;
                snapshot.Item.Release(snapshot.Position, snapshot.Rotation, Vector3.zero);
                t.SetParent(snapshot.Parent, true);
            }

            snapshots.Clear();
        }

        private List<Flight> Plan(SortingProgress progress, Camera cam)
        {
            var loose = progress.Items.Where(item => item.Container == null).ToList();
            var visible = new HashSet<BaseItem>(loose.Where(item => InView(cam, ItemTransform(item).position)));

            var plan = new List<Flight>();
            if (feedConstructionSites)
            {
                foreach (var adapter in FindObjectsByType<ConstructionSiteAdapter>(FindObjectsSortMode.None))
                {
                    var site = adapter.Entity;
                    if (site == null || site.IsBuilt) continue;

                    var bounds = SiteBounds(adapter.transform);
                    if (onlyRacksInView && !InView(cam, bounds.center)) continue;

                    for (var i = 0; i < site.Requirements.Count; i++)
                    {
                        var requirement = site.Requirements[i];
                        for (var n = site.Delivered[i]; n < requirement.Count; n++)
                        {
                            var itemIndex = PickItem(loose, visible, bounds.center,
                                item => Equals(item.Def, requirement.Resource));
                            if (itemIndex < 0) break;

                            var point = adapter.transform.InverseTransformPoint(RandomPointIn(bounds));
                            plan.Add(new Flight(loose[itemIndex], adapter.transform, point, Quaternion.identity, site));
                            loose.RemoveAt(itemIndex);
                        }
                    }
                }
            }

            var hiddenRacks = new List<Rack>();
            foreach (var rack in progress.Racks)
            {
                if (onlyRacksInView && !InView(cam, rack.SlotRoot.position)) hiddenRacks.Add(rack);
                else PlanRack(rack, loose, visible, plan);
            }

            // Whatever lies in the shot and found no rack in it still takes off, toward the racks off screen.
            var visibleLoose = loose.Where(visible.Contains).ToList();
            foreach (var rack in hiddenRacks)
            {
                PlanRack(rack, visibleLoose, visible, plan);
            }

            return plan;
        }

        private static void PlanRack(Rack rack, List<BaseItem> loose, ISet<BaseItem> visible, List<Flight> plan)
        {
            var target = rack.SlotRoot.position;
            var def = rack.StoredDef;
            if (def == null)
            {
                var first = PickItem(loose, visible, target, rack.Accepts);
                if (first < 0) return;

                def = loose[first].Def;
            }

            var layout = rack.LayoutFor(def);
            var free = Enumerable.Range(0, layout.Capacity)
                .Where(index => rack.StoredDef == null || rack.Slots[index] == null);

            foreach (var index in free)
            {
                var itemIndex = PickItem(loose, visible, target, item => Equals(item.Def, def) && rack.Accepts(item));
                if (itemIndex < 0) break;

                plan.Add(new Flight(loose[itemIndex], rack.SlotRoot, layout.GetPosition(index), layout.SlotRotation));
                loose.RemoveAt(itemIndex);
            }
        }

        // Nearest to the target, so far racks take far items and near ones the items under the camera;
        // items out of the shot only when none in it fit.
        private static int PickItem(List<BaseItem> loose, ISet<BaseItem> visible, Vector3 target,
            System.Func<BaseItem, bool> fits)
        {
            var best = -1;
            var bestScore = float.MaxValue;
            for (var i = 0; i < loose.Count; i++)
            {
                var item = loose[i];
                if (!fits(item)) continue;

                var score = (ItemTransform(item).position - target).sqrMagnitude + (visible.Contains(item) ? 0f : OffShotPenalty);
                if (score >= bestScore) continue;

                best = i;
                bestScore = score;
            }

            return best;
        }

        private static Bounds SiteBounds(Transform site)
        {
            var ghost = site.Find("Ghost");
            var renderers = (ghost != null ? ghost : site).GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0) return new Bounds(site.position, Vector3.one);

            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        private Vector3 RandomPointIn(Bounds bounds)
        {
            var offset = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
            return bounds.center + Vector3.Scale(bounds.extents * siteSpread, offset);
        }

        private void Schedule(List<Flight> plan, Camera cam)
        {
            var camPosition = cam.transform.position;
            var closeness = plan.ToDictionary(flight => flight, flight => Closeness(flight, camPosition));
            plan = (farRacksFirst
                    ? plan.OrderBy(flight => closeness[flight])
                    : plan.OrderByDescending(flight => closeness[flight]))
                .ToList();

            var weights = new float[Mathf.CeilToInt(plan.Count / (float)batchSize)];
            for (var i = 0; i < plan.Count; i++)
            {
                weights[i / batchSize] += closeness[plan[i]];
            }

            // The last batch has no pause after it, so its weight stays out of the split.
            var span = weights.Sum() - weights[^1];
            var batchStart = 0f;
            for (var i = 0; i < plan.Count; i++)
            {
                if (i > 0 && i % batchSize == 0 && span > 0f)
                {
                    batchStart += launchWindow * weights[i / batchSize - 1] / span;
                }

                var flight = plan[i];
                flight.StartTime = batchStart + Range(0f, itemStagger);
                flight.Viewer = camPosition;
                flights.Add(flight);
            }

            RefillSlotsInLaunchOrder();
        }

        private float Closeness(Flight flight, Vector3 camPosition)
        {
            var from = ItemTransform(flight.Item).position;
            var to = flight.Target.TransformPoint(flight.LocalPosition);
            var distance = Mathf.Min(Vector3.Distance(camPosition, from), Vector3.Distance(camPosition, to));
            return Mathf.Pow(closeUpDistance / Mathf.Max(distance, closeUpDistance), closeUpWeight);
        }

        // Launches go by distance, not by rack, so each rack hands its slots out again bottom first in launch order;
        // otherwise an upper layer could land before the one under it.
        private void RefillSlotsInLaunchOrder()
        {
            foreach (var rack in flights.Where(flight => flight.Site == null).GroupBy(flight => flight.Target))
            {
                var slots = rack.Select(flight => (flight.LocalPosition, flight.LocalRotation)).ToList();
                var index = 0;
                foreach (var flight in rack.OrderBy(flight => flight.StartTime))
                {
                    (flight.LocalPosition, flight.LocalRotation) = slots[index++];
                }
            }
        }

        private void Update()
        {
            if (!IsPlaying) return;

            clock += Time.deltaTime;
            var active = false;
            foreach (var flight in flights)
            {
                if (flight.Done) continue;

                active = true;
                if (clock < flight.StartTime) continue;

                if (!flight.Launched) Launch(flight);

                var time = (clock - flight.StartTime) / flight.Duration;
                if (time < 1f)
                {
                    Fly(flight, time);
                    continue;
                }

                if (!flight.Landed) Land(flight);
                if (flight.Done) continue;

                var punch = (clock - flight.StartTime - flight.Duration) / landingPunchDuration;
                var t = ItemTransform(flight.Item);
                if (punch < 1f)
                {
                    t.localScale = flight.Scale * (1f + landingPunch * Mathf.Sin(punch * Mathf.PI));
                    continue;
                }

                t.localScale = flight.Scale;
                flight.Done = true;
            }

            IsPlaying = active;
        }

        private void Launch(Flight flight)
        {
            var t = ItemTransform(flight.Item);
            flight.Item.SetContainer(slot);
            SetCollidersEnabled(flight.Item, false);
            t.SetParent(null, true);

            var from = t.position;
            var to = flight.Target.TransformPoint(flight.LocalPosition);
            var distance = Vector3.Distance(from, to);
            var arc = distance * arcPerMeter;

            var across = Vector3.Cross(Vector3.up, to - from);
            across = across.sqrMagnitude > 0.0001f ? across.normalized : Vector3.right;
            var sway = across * (distance * swayPerMeter * Range(-1f, 1f));

            var back = flight.Viewer - to;
            back.y = 0f;
            back = back.sqrMagnitude > 0.0001f ? back.normalized : Vector3.zero;

            flight.P0 = from;
            flight.P1 = from + Vector3.up * (Range(liftHeight.x, liftHeight.y) + arc) + sway;
            flight.P2 = to + Vector3.up * (approach.x + arc) + back * approach.y + sway;
            flight.P3 = to;
            flight.Lengths = MeasureLengths(flight);

            flight.Duration = Mathf.Lerp(duration.x, duration.y, distance / distanceForMaxDuration) * Range(0.9f, 1.1f);
            flight.FromRotation = t.rotation;
            flight.Scale = t.localScale;
            flight.TumbleAxis = RandomAxis();
            flight.Tumble = random.Next(tumbleTurns.x, tumbleTurns.y + 1) * 360f;
            flight.Launched = true;

            if (!groundLighting) return;

            flight.Renderers = t.GetComponentsInChildren<Renderer>();
            flight.ProbeAnchor = new GameObject("Probe Anchor") { hideFlags = HideFlags.HideAndDontSave }.transform;
            flight.ProbeAnchor.position = from;
            foreach (var renderer in flight.Renderers)
            {
                renderer.probeAnchor = flight.ProbeAnchor;
            }
        }

        private static void ReleaseProbeAnchor(Flight flight)
        {
            if (flight.ProbeAnchor == null) return;

            foreach (var renderer in flight.Renderers)
            {
                if (renderer != null) renderer.probeAnchor = null;
            }

            Destroy(flight.ProbeAnchor.gameObject);
            flight.ProbeAnchor = null;
        }

        private void Fly(Flight flight, float time)
        {
            var progress = Mathf.Clamp01(progressCurve.Evaluate(time));
            var align = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / alignBy));
            var toRotation = flight.Target.rotation * flight.LocalRotation;

            var t = ItemTransform(flight.Item);
            t.position = Bezier(flight, ParameterAt(flight, progress));
            if (flight.ProbeAnchor != null)
            {
                var position = t.position;
                var ground = Mathf.Lerp(flight.P0.y, flight.P3.y, progress);
                flight.ProbeAnchor.position = new Vector3(position.x, Mathf.Min(position.y, ground + lightLift), position.z);
            }
            t.rotation = Quaternion.AngleAxis(flight.Tumble * align, flight.TumbleAxis) *
                         Quaternion.Slerp(flight.FromRotation, toRotation, align);

            if (flight.Site != null)
            {
                t.localScale = flight.Scale * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(absorbFrom, 1f, time)));
            }
        }

        private void Land(Flight flight)
        {
            flight.Landed = true;
            ReleaseProbeAnchor(flight);
            if (flight.Site != null)
            {
                if (flight.Site.CanPut(flight.Item)) flight.Site.Put(flight.Item);
                else flight.Item.SetVisible(false);

                flight.Done = true;
                return;
            }

            var t = ItemTransform(flight.Item);
            t.SetParent(flight.Target, true);
            t.SetLocalPositionAndRotation(flight.LocalPosition, flight.LocalRotation);
            t.localScale = flight.Scale;
        }

        private static Vector3 Bezier(Flight flight, float t)
        {
            var u = 1f - t;
            return u * u * u * flight.P0 + 3f * u * u * t * flight.P1 + 3f * u * t * t * flight.P2 + t * t * t * flight.P3;
        }

        private static float[] MeasureLengths(Flight flight)
        {
            var lengths = new float[LengthSamples + 1];
            var previous = flight.P0;
            for (var i = 1; i <= LengthSamples; i++)
            {
                var point = Bezier(flight, i / (float)LengthSamples);
                lengths[i] = lengths[i - 1] + Vector3.Distance(previous, point);
                previous = point;
            }

            return lengths;
        }

        private static float ParameterAt(Flight flight, float share)
        {
            var lengths = flight.Lengths;
            var target = share * lengths[^1];
            for (var i = 1; i < lengths.Length; i++)
            {
                if (lengths[i] < target) continue;

                var segment = lengths[i] - lengths[i - 1];
                var within = segment > 0f ? (target - lengths[i - 1]) / segment : 0f;
                return (i - 1 + within) / LengthSamples;
            }

            return 1f;
        }

        private static bool InView(Camera cam, Vector3 point)
        {
            var viewport = cam.WorldToViewportPoint(point);
            return viewport.z > 0f && viewport.x is > -0.05f and < 1.05f && viewport.y is > -0.05f and < 1.05f;
        }

        private static Transform ItemTransform(BaseItem item) => ((Component)item.View).transform;

        private static void SetCollidersEnabled(BaseItem item, bool enabled)
        {
            foreach (var collider in ItemTransform(item).GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = enabled;
            }
        }

        private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);

        private Vector3 RandomAxis()
        {
            var axis = new Vector3(Range(-1f, 1f), Range(-1f, 1f), Range(-1f, 1f));
            return axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
        }

        private sealed class ShowcaseSlot : IItemContainer
        {
            public ItemMode Mode => ItemMode.Stored;
        }

        private sealed class Flight
        {
            public readonly BaseItem Item;
            public readonly Transform Target;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public readonly ConstructionSite Site;

            public float StartTime;
            public float Duration;
            public Vector3 Viewer;
            public Vector3 P0, P1, P2, P3;
            public float[] Lengths;
            public Quaternion FromRotation;
            public Vector3 Scale;
            public Vector3 TumbleAxis;
            public float Tumble;
            public bool Launched;
            public bool Landed;
            public bool Done;
            public Renderer[] Renderers;
            public Transform ProbeAnchor;

            public Flight(BaseItem item, Transform target, Vector3 localPosition, Quaternion localRotation,
                ConstructionSite site = null)
            {
                Item = item;
                Target = target;
                LocalPosition = localPosition;
                LocalRotation = localRotation;
                Site = site;
            }
        }

        private readonly struct Snapshot
        {
            public readonly BaseItem Item;
            public readonly Transform Parent;
            public readonly Vector3 Position;
            public readonly Quaternion Rotation;
            public readonly Vector3 Scale;

            public Snapshot(BaseItem item, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale)
            {
                Item = item;
                Parent = parent;
                Position = position;
                Rotation = rotation;
                Scale = scale;
            }
        }
    }
}
