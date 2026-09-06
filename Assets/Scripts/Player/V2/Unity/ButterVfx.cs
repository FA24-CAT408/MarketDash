using System.Collections.Generic;
using UnityEngine;

namespace CrazyMarket.Player.V2.Unity
{
    /// <summary>Butter presentation and live patch footprints. Movement rules belong to ButterSurfaceMovement.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(ParticleSystem))]
    public sealed class ButterVfx : MonoBehaviour
    {
        [System.Serializable]
        public struct DripSource
        {
            public string bone;
            public Vector3 offset;
            public float diameter;
        }

        [Header("Liquid assets")]
        [SerializeField] private Material liquidMaterial;
        [SerializeField] private Material puddleMaterial;
        [SerializeField] private Material recallPuffMaterial;
        [SerializeField] private Material recallDropMaterial;
        [SerializeField] private Mesh dropletMesh;
        [SerializeField] private Mesh puddleMesh;
        [SerializeField] private DripSource[] dripSources;
        [Header("Trail")]
        [SerializeField, Range(24,256)] private int puddleCapacity = 256;
        [SerializeField] private float trailSpacing = .38f;
        [SerializeField] private float puddleLifetime = 7f;
        [SerializeField] private LayerMask surfaceLayers = 9;
        [Header("Spray")]
        [SerializeField] private float sprayPerMetre = 1.4f;

        private sealed class Puddle
        {
            public Transform transform;
            public MeshRenderer renderer;
            public Collider surface;
            public float born, lifetime, seed;
            public Vector3 size;
            public bool active;
        }

        private enum DripStage { Attached, Falling, Settling }
        private sealed class DripMotion
        {
            public DripStage stage;
            public Vector3 velocity, releasedSize, previousOrigin, impactPoint, impactNormal;
            public float age;
            public bool hasOrigin;
        }

        private PlayerControllerV2 player;
        private ButterBody body;
        private ParticleSystem droplets;
        private ParticleSystem dashSpray;
        private ParticleSystem recallDrops, recallPuff;
        private struct ReturningDrop { public Vector3 origin; public float size, delay, arc; }
        private ReturningDrop[] returning;
        private ParticleSystem.Particle[] recallParticles;
        private int returningCount;
        private float recallAge, recallDuration;
        private bool recallActive, recallPuffed;
        internal bool GatheringButter => recallActive && recallAge < recallDuration;
        internal bool HasRecallEffect => recallActive;
        private LineRenderer[] dashWake;
        private LineRenderer dashRing;
        private float dashAge = 1f;
        private Vector3 dashOrigin, dashDirection;
        private Transform worldRoot;
        private Puddle[] puddles;
        private Transform[] bones, hangingDrops;
        private MeshRenderer[] dripRenderers;
        private DripMotion[] dripMotions;
        private bool boundAnimatedBones;
        private float[] dripPhases;
        private int nextPuddle;
        private Vector3 previousPosition, previousVelocity, lastTrailPoint;
        private bool wasGrounded, hasTrailPoint;
        private float sprayRemainder, idlePoolTimer, airborneTime, lastImpactTime;
        private long lastRevision = -1;
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private readonly List<ParticleCollisionEvent> collisions = new List<ParticleCollisionEvent>(48);
        private MaterialPropertyBlock properties;
        private static readonly int Seed = Shader.PropertyToID("_Seed");
        private static readonly int Age = Shader.PropertyToID("_Age");
        private static readonly int Opacity = Shader.PropertyToID("_Opacity");

        public int ActivePuddles { get; private set; }
        private float Reserve => body != null && body.isActiveAndEnabled ? body.Reserve : 1f;

        public bool ContainsButter(Collider surface, Vector3 point, float minimumAge)
        {
            if (!isActiveAndEnabled || puddles == null || surface == null) return false;
            foreach (var puddle in puddles)
            {
                if (!puddle.active || puddle.surface != surface) continue;
                float elapsed = Time.time - puddle.born;
                float age = elapsed / puddle.lifetime;
                if (elapsed < minimumAge || age >= .95f) continue;
                Vector3 local = puddle.transform.InverseTransformPoint(point);
                if (Mathf.Abs(local.y) > .08f || Mathf.Abs(local.x) > .5f || Mathf.Abs(local.z) > .5f) continue;
                // Match Organic Puddle's polar silhouette, including its shrinking/fading transform.
                Vector2 p = new Vector2(local.x, local.z) * 2f;
                float angle = Mathf.Atan2(p.y, p.x);
                float radius = .72f + .10f * Mathf.Sin(angle * 3f + puddle.seed)
                    + .065f * Mathf.Sin(angle * 5f - puddle.seed * 1.7f)
                    + .03f * Mathf.Sin(angle * 8f + puddle.seed * .8f)
                    + .012f * Mathf.Sin(angle * 4f + age * 2f + puddle.seed);
                if (p.sqrMagnitude <= radius * radius) return true;
            }
            return false;
        }

        private void Awake()
        {
            player = GetComponentInParent<PlayerControllerV2>();
            body = player != null ? player.GetComponent<ButterBody>() : null;
            droplets = GetComponent<ParticleSystem>();
            if (player == null || liquidMaterial == null || puddleMaterial == null ||
                dropletMesh == null || puddleMesh == null)
            {
                Debug.LogError("Butter VFX needs a V2 player and its liquid assets.", this);
                enabled = false;
                return;
            }
            worldRoot = new GameObject("Butter VFX - Pooled Surfaces").transform;
            CreateDashEffects();
            CreateRecallEffects();
            properties = new MaterialPropertyBlock();
            puddles = new Puddle[puddleCapacity];
            for (int i = 0; i < puddles.Length; i++)
            {
                var go = new GameObject("Butter puddle " + i);
                go.transform.SetParent(worldRoot, false);
                go.AddComponent<MeshFilter>().sharedMesh = puddleMesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = puddleMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.enabled = false;
                puddles[i] = new Puddle { transform = go.transform, renderer = renderer };
            }
            var rig = player.GetComponentsInChildren<Transform>(true);
            bones = new Transform[dripSources.Length];
            hangingDrops = new Transform[dripSources.Length];
            dripPhases = new float[dripSources.Length];
            dripRenderers = new MeshRenderer[dripSources.Length];
            dripMotions = new DripMotion[dripSources.Length];
            for (int i = 0; i < dripSources.Length; i++)
            {
                foreach (var bone in rig)
                    if (bone.name == dripSources[i].bone) { bones[i] = bone; break; }
                if (bones[i] == null) bones[i] = player.transform;
                var drop = new GameObject("Hanging butter - " + dripSources[i].bone);
                drop.transform.SetParent(worldRoot, false);
                drop.AddComponent<MeshFilter>().sharedMesh = dropletMesh;
                var renderer = drop.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = liquidMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                hangingDrops[i] = drop.transform;
                dripRenderers[i] = renderer;
                dripMotions[i] = new DripMotion();
                dripPhases[i] = i * .173f % 1f;
            }
            previousPosition = player.transform.position;
        }

        private void OnEnable()
        {
            if (player != null) previousPosition = player.transform.position;
            if (worldRoot != null) worldRoot.gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (player == null || worldRoot == null || Time.deltaTime <= 0) return;
            if (!boundAnimatedBones)
            {
                // Humanoid initialization exposes an animated rig that can differ
                // from the imported rest-pose hierarchy found during Awake.
                var skin = player.GetComponentInChildren<SkinnedMeshRenderer>();
                if (skin != null)
                    for (int i = 0; i < bones.Length; i++)
                        foreach (var bone in skin.bones)
                            if (bone != null && bone.name == dripSources[i].bone) { bones[i] = bone; break; }
                boundAnimatedBones = true;
            }
            var snapshot = player.Snapshot;
            Vector3 position = player.transform.position;
            bool teleport = snapshot.Revision != lastRevision &&
                (snapshot.ActionFlags & PlayerActionFlags.Teleported) != 0;
            lastRevision = snapshot.Revision;
            if (teleport || Vector3.Distance(position, previousPosition) > 3f)
                ClearTransientEffects();
            previousPosition = position;
            Vector3 velocity = Vector3.ProjectOnPlane(snapshot.Velocity, Vector3.up);
            float speed = velocity.magnitude;
            bool grounded = snapshot.StableGrounded;

            UpdateDashEffects();
            UpdateRecallEffects();
            if (GatheringButter)
            {
                previousVelocity = velocity;
                wasGrounded = grounded;
                return;
            }
            UpdateDrips(velocity, snapshot.ControlBlocked);
            if (Reserve > 0f && grounded && !snapshot.ControlBlocked && !teleport && TryGetSurface(position + Vector3.up*.7f, 1.7f, out RaycastHit floor))
            {
                Vector3 direction = speed > .2f ? velocity / speed : player.transform.forward;
                float turning = speed > 2 ? Vector3.Angle(previousVelocity, velocity) / Mathf.Max(Time.deltaTime, .001f) : 0;
                if (!wasGrounded && airborneTime > .18f)
                {
                    PlacePuddle(floor.collider, floor.point, floor.normal, direction, new Vector2(.85f,.95f), 1.2f);
                    EmitSpray(floor.point, direction, Mathf.Clamp(Mathf.CeilToInt(airborneTime*12), 8, 18), 1.3f);
                }
                if (speed > .5f)
                {
                    idlePoolTimer = 0;
                    if (!hasTrailPoint) { lastTrailPoint = floor.point; hasTrailPoint = true; }
                    if (Vector3.Distance(lastTrailPoint, floor.point) >= trailSpacing / Mathf.Lerp(.3f, 1f, Reserve))
                    {
                        // The organic mask fills about 72% of the quad; match the visible ribbon to Blobby's width.
                        float width = Mathf.Lerp(2.1f,2.4f,Mathf.Clamp01(speed/10));
                        PlacePuddle(floor.collider, floor.point, floor.normal, direction, new Vector2(width, .9f), 1);
                        lastTrailPoint = floor.point;
                    }
                    sprayRemainder += speed * sprayPerMetre * Reserve * Time.deltaTime * (1 + Mathf.Clamp01(turning/180));
                    int count = Mathf.FloorToInt(sprayRemainder);
                    sprayRemainder -= count;
                    if (count > 0) EmitSpray(floor.point, direction, Mathf.Min(count,8), Mathf.Lerp(.4f,1,speed/10));
                }
                else
                {
                    hasTrailPoint = false;
                    idlePoolTimer += Time.deltaTime * Reserve;
                    if (idlePoolTimer >= 2.5f)
                    {
                        idlePoolTimer = 0;
                        PlacePuddle(floor.collider, floor.point, floor.normal, direction, new Vector2(.42f,.5f), 1);
                    }
                }
                airborneTime = 0;
            }
            else
            {
                hasTrailPoint = false;
                if (!grounded) airborneTime += Time.deltaTime;
            }
            previousVelocity = velocity;
            wasGrounded = grounded;
            UpdatePuddles();
        }

        private void UpdateDrips(Vector3 velocity, bool blocked)
        {
            for (int i = 0; i < bones.Length; i++)
            {
                float diameter = dripSources[i].diameter * Mathf.Lerp(.35f, 1f, Reserve);
                Vector3 origin = bones[i].position + player.transform.rotation * dripSources[i].offset;
                var motion = dripMotions[i];
                var drop = hangingDrops[i];
                Vector3 sourceVelocity = motion.hasOrigin ? (origin-motion.previousOrigin)/Time.deltaTime : velocity;
                motion.previousOrigin = origin;
                motion.hasOrigin = true;
                float opacity = 1;
                if (motion.stage == DripStage.Attached)
                {
                    if (Reserve <= 0f)
                    {
                        dripRenderers[i].enabled = false;
                        dripPhases[i] = 0f;
                        continue;
                    }
                    dripRenderers[i].enabled = true;
                    float phase = Mathf.Min(1,dripPhases[i] + (blocked ? 0 : Time.deltaTime*(.32f+i*.035f)*Reserve));
                    float grow = Mathf.SmoothStep(.12f,1,phase);
                    float height = diameter*Mathf.Lerp(.6f,1.6f,phase)*grow;
                    drop.SetPositionAndRotation(origin+Vector3.down*height*.45f,Quaternion.identity);
                    drop.localScale = new Vector3(diameter*grow,height,diameter*grow);
                    dripPhases[i] = phase;
                    if (phase >= 1 && !blocked)
                    {
                        // The attached mesh itself falls: no smaller replacement or camera-facing particle.
                        motion.stage = DripStage.Falling;
                        motion.age = 0;
                        motion.releasedSize = drop.localScale;
                        motion.velocity = velocity*.85f + Vector3.ClampMagnitude(sourceVelocity-velocity,2)*.25f + Vector3.down*.15f;
                        dripPhases[i] = 0;
                    }
                }
                else opacity = AdvanceReleasedDrip(drop,motion);
                properties.Clear();
                properties.SetFloat(Opacity,opacity);
                dripRenderers[i].SetPropertyBlock(properties);
            }
        }

        private float AdvanceReleasedDrip(Transform drop, DripMotion motion)
        {
            float delta = Time.deltaTime;
            motion.age += delta;
            if (motion.stage == DripStage.Falling)
            {
                Vector3 next = drop.position + motion.velocity*delta + Vector3.down*(4*delta*delta);
                motion.velocity += Vector3.down*(8*delta);
                float halfHeight = motion.releasedSize.y*.5f;
                if (TryGetSurface(next+Vector3.up*(halfHeight+.15f),halfHeight*2+.3f,out RaycastHit hit) &&
                    Vector3.Dot(next-hit.point,hit.normal) <= halfHeight+.018f)
                {
                    motion.stage = DripStage.Settling;
                    motion.age = 0;
                    motion.impactPoint = hit.point;
                    motion.impactNormal = hit.normal;
                    drop.SetPositionAndRotation(hit.point+hit.normal*(halfHeight+.018f),Quaternion.FromToRotation(Vector3.up,hit.normal));
                    PlacePuddle(hit.collider,hit.point,hit.normal,player.transform.forward,
                        new Vector2(motion.releasedSize.x*2.1f,motion.releasedSize.z*2.3f),.7f);
                    return 1;
                }
                drop.position = next;
                float opacity = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.6f,2,motion.age));
                if (motion.age >= 2) motion.stage = DripStage.Attached;
                return opacity;
            }
            float settle = Mathf.Clamp01(motion.age/.22f);
            Vector3 flat = new Vector3(motion.releasedSize.x*1.9f,.012f,motion.releasedSize.z*1.9f);
            drop.localScale = Vector3.Lerp(motion.releasedSize,flat,Mathf.SmoothStep(0,1,settle));
            drop.position = motion.impactPoint+motion.impactNormal*(drop.localScale.y*.5f+.018f);
            if (settle >= 1) motion.stage = DripStage.Attached;
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.55f,1,settle));
        }

        private void EmitSpray(Vector3 point, Vector3 direction, int count, float intensity)
        {
            count = Mathf.CeilToInt(count * Reserve);
            intensity *= Mathf.Lerp(.45f, 1f, Reserve);
            Vector3 side = Vector3.Cross(Vector3.up,direction);
            for (int i = 0; i < count; i++)
            {
                float lateral = Random.Range(-1f,1f);
                float diameter = Random.Range(.13f,.24f) * intensity;
                var emit = new ParticleSystem.EmitParams
                {
                    position = point + Vector3.up*.13f + side*lateral*.35f,
                    velocity = -direction*Random.Range(.6f,2.2f)*intensity + side*lateral*1.4f*intensity + Vector3.up*Random.Range(1.6f,3.8f)*intensity,
                    startSize3D = new Vector3(diameter,diameter*1.7f,diameter),
                    startLifetime = Random.Range(.45f,.85f),
                    startColor = Color.white
                };
                droplets.Emit(emit,1);
            }
        }

        private bool TryGetSurface(Vector3 origin, float distance, out RaycastHit closest)
        {
            closest = default;
            float best = float.MaxValue;
            int count = Physics.RaycastNonAlloc(origin,Vector3.down,hits,distance,surfaceLayers,QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                if (hit.collider.transform.IsChildOf(player.transform) || hit.rigidbody != null || hit.normal.y < .5f || hit.distance >= best) continue;
                closest = hit;
                best = hit.distance;
            }
            return best < float.MaxValue;
        }

        private void OnParticleCollision(GameObject other)
        {
            if (puddles == null || Time.time-lastImpactTime < .14f || other.GetComponentInParent<Rigidbody>() != null) return;
            int count = droplets.GetCollisionEvents(other,collisions);
            for (int i = 0; i < count; i++)
            {
                var hit = collisions[i];
                if (hit.normal.y < .5f) continue;
                PlacePuddle(hit.colliderComponent as Collider,hit.intersection,hit.normal,Vector3.forward,new Vector2(.3f,.4f),.7f);
                lastImpactTime = Time.time;
                break;
            }
        }

        private void PlacePuddle(Collider surface, Vector3 point, Vector3 normal, Vector3 forward, Vector2 size, float lifetimeScale)
        {
            if (Reserve <= 0f || GatheringButter) return;
            var puddle = puddles[nextPuddle];
            nextPuddle = (nextPuddle+1)%puddles.Length;
            puddle.active = true;
            puddle.surface = surface;
            puddle.born = Time.time;
            puddle.lifetime = puddleLifetime*lifetimeScale*Mathf.Lerp(.25f,1f,Reserve);
            puddle.seed = Random.value*40;
            float coverage = Mathf.Lerp(.18f,1f,Reserve);
            puddle.size = new Vector3(size.x*coverage,1,size.y*Mathf.Lerp(.45f,1f,Reserve));
            Vector3 tangent = Vector3.ProjectOnPlane(forward,normal).normalized;
            if (tangent.sqrMagnitude < .1f) tangent = Vector3.ProjectOnPlane(Vector3.right,normal).normalized;
            puddle.transform.SetPositionAndRotation(point+normal*.018f,Quaternion.LookRotation(tangent,normal));
            puddle.renderer.enabled = true;
        }

        private void UpdatePuddles()
        {
            ActivePuddles = 0;
            foreach (var puddle in puddles)
            {
                if (!puddle.active) continue;
                float elapsed = Time.time-puddle.born;
                float age = elapsed/puddle.lifetime;
                if (age >= 1) { puddle.active = false; puddle.renderer.enabled = false; continue; }
                ActivePuddles++;
                float spread = Mathf.Lerp(.55f,1,Mathf.SmoothStep(0,1,elapsed/.18f));
                float fade = 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1,age));
                puddle.transform.localScale = puddle.size * spread * Mathf.Lerp(.85f,1,fade);
                properties.SetFloat(Seed,puddle.seed);
                properties.SetFloat(Age,age);
                properties.SetFloat(Opacity,fade*.94f*Mathf.SmoothStep(0,1,elapsed/.08f));
                puddle.renderer.SetPropertyBlock(properties);
            }
        }

        public void ClearTransientEffects()
        {
            recallActive = false;
            returningCount = 0;
            if (recallDrops != null) recallDrops.Clear();
            if (recallPuff != null) recallPuff.Clear();
            if (droplets != null) droplets.Clear();
            if (dashSpray != null) dashSpray.Clear();
            dashAge = 1f;
            if (dashRing != null) dashRing.enabled = false;
            if (dashWake != null) foreach (var line in dashWake)
                if (line != null) line.enabled = false;
            if (puddles != null) foreach (var puddle in puddles)
            {
                puddle.active=false;
                if (puddle.renderer != null) puddle.renderer.enabled=false;
            }
            ActivePuddles=0;
            hasTrailPoint=false;
            sprayRemainder=idlePoolTimer=airborneTime=0;
            previousVelocity=Vector3.zero;
            wasGrounded=false;
            if (dripRenderers != null) foreach (var renderer in dripRenderers)
                if (renderer != null) renderer.enabled = false;
            if (dripMotions != null)
                for (int i=0;i<dripMotions.Length;i++)
                {
                    dripMotions[i].stage=DripStage.Attached;
                    dripMotions[i].hasOrigin=false;
                    dripPhases[i]=i*.173f%1f;
                }
        }

        private void CreateRecallEffects()
        {
            int capacity = Mathf.Max(12, puddleCapacity + droplets.main.maxParticles + dashSpray.main.maxParticles + dripSources.Length);
            returning = new ReturningDrop[capacity];
            recallParticles = new ParticleSystem.Particle[capacity];
            var go = new GameObject("Butter returning to Blobby");
            go.transform.SetParent(worldRoot, false);
            recallDrops = go.AddComponent<ParticleSystem>();
            recallDrops.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = recallDrops.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.simulationSpeed = 0f;
            main.maxParticles = capacity;
            main.startSize3D = main.startRotation3D = true;
            var emission = recallDrops.emission; emission.enabled = false;
            var shape = recallDrops.shape; shape.enabled = false;
            var renderer = recallDrops.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.mesh = dropletMesh;
            renderer.sharedMaterial = recallDropMaterial != null ? recallDropMaterial : liquidMaterial;
            renderer.enableGPUInstancing = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            go = new GameObject("Butter refill puff");
            go.transform.SetParent(worldRoot, false);
            recallPuff = go.AddComponent<ParticleSystem>();
            recallPuff.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            main = recallPuff.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 16;
            main.startSpeed = 0f;
            emission = recallPuff.emission; emission.enabled = false;
            shape = recallPuff.shape; shape.enabled = false;
            var size = recallPuff.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f,.35f),new Keyframe(.18f,1f),new Keyframe(.5f,.95f),new Keyframe(1f,0f)));
            renderer = recallPuff.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = recallPuffMaterial != null ? recallPuffMaterial : liquidMaterial;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void PlayRecall(float duration)
        {
            if (!isActiveAndEnabled || recallDrops == null) { ClearTransientEffects(); return; }
            returningCount = 0;
            foreach (var puddle in puddles)
                if (puddle.active) AddReturningDrop(puddle.transform.position, Mathf.Clamp(Mathf.Sqrt(puddle.size.x*puddle.size.z)*.4f,.12f,.7f));
            for (int i=0;i<hangingDrops.Length;i++)
                if (dripRenderers[i].enabled) AddReturningDrop(hangingDrops[i].position, Mathf.Max(.08f,hangingDrops[i].localScale.x));
            int count = droplets.GetParticles(recallParticles);
            for (int i=0;i<count;i++) AddReturningDrop(recallParticles[i].position, Mathf.Max(.08f,recallParticles[i].GetCurrentSize(droplets)));
            count = dashSpray.GetParticles(recallParticles);
            for (int i=0;i<count;i++) AddReturningDrop(recallParticles[i].position, Mathf.Max(.08f,recallParticles[i].GetCurrentSize(dashSpray)));
            if (returningCount == 0)
                for (int i=0;i<12;i++)
                {
                    float angle=i*Mathf.PI*2f/12f;
                    AddReturningDrop(player.transform.position+new Vector3(Mathf.Cos(angle)*1.3f,.2f,Mathf.Sin(angle)*1.3f),.16f);
                }
            count = returningCount;
            ClearTransientEffects();
            returningCount = count;
            recallDuration = Mathf.Max(.2f,duration);
            recallAge = 0f;
            recallActive = true;
            recallPuffed = false;
            recallDrops.Play();
        }

        private void AddReturningDrop(Vector3 origin, float size)
        {
            if (returningCount >= returning.Length) return;
            returning[returningCount] = new ReturningDrop { origin=origin, size=size,
                delay=(returningCount%5)*.018f, arc=Mathf.Clamp(Vector3.Distance(origin,player.transform.position)*.15f,.6f,2.8f) };
            returningCount++;
        }

        private void UpdateRecallEffects()
        {
            if (!recallActive) return;
            recallAge += Time.deltaTime;
            Vector3 target = player.transform.position + Vector3.up*.95f;
            recallPuff.transform.position = target;
            int live = 0;
            for (int i=0;i<returningCount;i++)
            {
                var drop = returning[i];
                float t = Mathf.Clamp01((recallAge-drop.delay)/(recallDuration*.82f));
                if (t >= 1f) continue;
                float pull = t*t;
                Vector3 side = Vector3.Cross(Vector3.up,target-drop.origin).normalized;
                Vector3 arc = (Vector3.up*drop.arc + side*((i%2==0?1f:-1f)*.45f))*Mathf.Sin(t*Mathf.PI);
                recallParticles[live++] = new ParticleSystem.Particle {
                    position=Vector3.Lerp(drop.origin,target,pull)+arc,
                    startLifetime=10f, remainingLifetime=10f,
                    startSize3D=new Vector3(drop.size,drop.size*(1f+t*2f),drop.size)*(1f-pull*.85f),
                    rotation3D=Quaternion.FromToRotation(Vector3.up,(target-drop.origin).normalized).eulerAngles,
                    startColor=new Color(1f,1f,.75f,1f)
                };
            }
            recallDrops.SetParticles(recallParticles,live);
            if (!recallPuffed && recallAge >= recallDuration*.78f)
            {
                recallPuffed = true;
                recallPuff.Play();
                for(int i=0;i<14;i++)
                {
                    // An even burst keeps the outlined cloudlets readable around the torso.
                    float angle=i*2.399963f;
                    float height=1f-2f*(i+.5f)/14f;
                    float radius=Mathf.Sqrt(1f-height*height);
                    Vector3 direction=new Vector3(Mathf.Cos(angle)*radius,height*.65f,Mathf.Sin(angle)*radius);
                    recallPuff.Emit(new ParticleSystem.EmitParams { position=direction*.6f, velocity=direction*2.1f+Vector3.up*.45f,
                        startSize=Random.Range(1.15f,1.6f), startLifetime=Random.Range(.45f,.6f),
                        startColor=Color.white, rotation=Random.Range(-25f,25f) },1);
                }
            }
            if (recallAge >= recallDuration+.6f) { recallActive=false; returningCount=0; }
        }

        private void CreateDashEffects()
        {
            var sprayObject = new GameObject("Butter dash spray");
            sprayObject.transform.SetParent(worldRoot, false);
            dashSpray = sprayObject.AddComponent<ParticleSystem>();
            dashSpray.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = dashSpray.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 128;
            main.startSize3D = true;
            main.startRotation3D = true;
            main.startColor = Color.white;
            main.startSpeed = 0f;
            main.gravityModifier = .15f;
            var emission = dashSpray.emission; emission.enabled = false;
            var shape = dashSpray.shape; shape.enabled = false;
            var size = dashSpray.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f,1f,1f,0f));
            var renderer = dashSpray.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.alignment = ParticleSystemRenderSpace.World;
            renderer.enableGPUInstancing = false;
            renderer.mesh = dropletMesh;
            renderer.sharedMaterial = liquidMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            dashWake = new LineRenderer[3];
            for (int i = 0; i < dashWake.Length; i++) dashWake[i] = CreateDashLine("Butter dash wake " + i, 4, false);
            dashRing = CreateDashLine("Butter dash impulse ring", 32, true);
        }

        private LineRenderer CreateDashLine(string name, int points, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(worldRoot, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = liquidMaterial;
            line.useWorldSpace = true;
            line.positionCount = points;
            line.loop = loop;
            line.numCapVertices = 4;
            line.generateLightingData = true;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.enabled = false;
            return line;
        }

        public void PlayDash(Vector3 direction)
        {
            if (!isActiveAndEnabled || dashSpray == null) return;
            dashOrigin = player.transform.position + Vector3.up*.95f;
            dashDirection = direction.normalized;
            dashAge = 0f;
            Vector3 side = Vector3.Cross(Vector3.up, dashDirection);
            dashSpray.Play();
            for (int i = 0; i < 30; i++)
            {
                float spread = Random.Range(-1f,1f);
                float diameter = Random.Range(.16f,.28f);
                dashSpray.Emit(new ParticleSystem.EmitParams
                {
                    position = dashOrigin + side*spread*.35f + Vector3.up*Random.Range(-.3f,.35f),
                    velocity = -dashDirection*Random.Range(4f,11f) + side*spread*3f + Vector3.up*Random.Range(-1f,2f),
                    startSize3D = new Vector3(diameter,diameter*3.2f,diameter),
                    rotation3D = Quaternion.FromToRotation(Vector3.up,-dashDirection).eulerAngles,
                    startLifetime = Random.Range(.2f,.4f),
                    startColor = new Color(1f,.97f,.7f,1f)
                }, 1);
            }
        }

        private void UpdateDashEffects()
        {
            if (dashAge >= .4f) return;
            dashAge += Time.deltaTime;
            float fade = 1f - Mathf.SmoothStep(0f,1f,Mathf.InverseLerp(.13f,.4f,dashAge));
            Vector3 side = Vector3.Cross(Vector3.up,dashDirection);
            Vector3 tip = player.transform.position + Vector3.up*.95f;
            for (int i = 0; i < dashWake.Length; i++)
            {
                var line = dashWake[i];
                line.enabled = fade > 0f;
                line.startWidth = .025f*fade;
                line.endWidth = (i == 1 ? .32f : .16f)*fade;
                line.startColor = new Color(1f,.88f,.38f,0f);
                line.endColor = new Color(1f,.98f,.78f,fade*.85f);
                Vector3 offset = side*((i-1)*.3f) + Vector3.up*(i == 1 ? -.15f : .12f);
                line.SetPosition(0,dashOrigin + offset);
                line.SetPosition(1,Vector3.Lerp(dashOrigin,tip,.4f) + offset*1.1f);
                line.SetPosition(2,Vector3.Lerp(dashOrigin,tip,.8f) + offset*.7f);
                line.SetPosition(3,tip + offset*.3f);
            }
            dashRing.enabled = dashAge < .24f;
            float radius = Mathf.Lerp(.3f,1.2f,Mathf.Clamp01(dashAge/.24f));
            dashRing.widthMultiplier = .14f*(1f-Mathf.Clamp01(dashAge/.24f));
            dashRing.startColor = dashRing.endColor = new Color(1f,.98f,.8f,fade*.7f);
            for (int i=0;i<dashRing.positionCount;i++)
            {
                float angle = i*Mathf.PI*2f/dashRing.positionCount;
                dashRing.SetPosition(i,dashOrigin + (side*Mathf.Cos(angle)+Vector3.up*Mathf.Sin(angle))*radius);
            }
        }

        private void OnDisable()
        {
            ClearTransientEffects();
            if (worldRoot != null) worldRoot.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (worldRoot != null) Destroy(worldRoot.gameObject);
        }
    }
}
