using UnityEngine;
using UnityEngine.Rendering;

namespace SheepSheepBurger.BurgerAssembly
{
    /// <summary>
    /// Emits persistent, fading grease marks in the cooking UI's local coordinate space.
    /// The system stays alive between drags so existing marks can finish fading out.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PattyGreaseTrail : MonoBehaviour
    {
        private const float ParticleSpacing = 82f;
        private const int MaximumParticles = 160;

        private ParticleSystem particles;
        private Material particleMaterial;
        private Texture2D particleTexture;
        private bool isTracking;
        private Vector2 previousPosition;
        private float distanceSinceLastParticle;

        public ParticleSystem ParticleSystem => particles;

        public bool IsTracking => isTracking;

        public static PattyGreaseTrail Create(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            PattyGreaseTrail existing = FindChildByName<PattyGreaseTrail>(parent, "PattyGreaseTrail");
            if (existing != null)
            {
                existing.Configure();
                return existing;
            }

            var effectObject = new GameObject(
                "PattyGreaseTrail",
                typeof(RectTransform),
                typeof(ParticleSystem),
                typeof(PattyGreaseTrail));
            RectTransform effectRect = effectObject.GetComponent<RectTransform>();
            effectRect.SetParent(parent, false);
            BurgerUiFactory.SetRect(effectRect, Vector2.zero, parent.sizeDelta);
            effectRect.SetAsFirstSibling();

            PattyGreaseTrail trail = effectObject.GetComponent<PattyGreaseTrail>();
            trail.Configure();
            return trail;
        }

        private static T FindChildByName<T>(Transform parent, string childName) where T : Component
        {
            if (parent == null)
            {
                return null;
            }

            foreach (T component in parent.GetComponentsInChildren<T>(true))
            {
                if (component != null && component.gameObject.name == childName)
                {
                    return component;
                }
            }

            return null;
        }

        public void BeginTrail(Vector2 localPosition)
        {
            EnsureConfigured();
            isTracking = true;
            previousPosition = localPosition;
            distanceSinceLastParticle = 0f;
        }

        public void BeginTrailAtWorldPosition(Vector3 worldPosition)
        {
            BeginTrail(transform.InverseTransformPoint(worldPosition));
        }

        public void MoveTo(Vector2 localPosition)
        {
            if (!isTracking)
            {
                BeginTrail(localPosition);
                return;
            }

            Vector2 segmentStart = previousPosition;
            float segmentLength = Vector2.Distance(segmentStart, localPosition);
            previousPosition = localPosition;
            if (segmentLength <= Mathf.Epsilon)
            {
                return;
            }

            float consumedDistance = 0f;
            float distanceToNextParticle = ParticleSpacing - distanceSinceLastParticle;
            while (consumedDistance + distanceToNextParticle <= segmentLength)
            {
                consumedDistance += distanceToNextParticle;
                float t = consumedDistance / segmentLength;
                EmitGreaseParticle(Vector2.Lerp(segmentStart, localPosition, t));
                distanceSinceLastParticle = 0f;
                distanceToNextParticle = ParticleSpacing;
            }

            distanceSinceLastParticle += segmentLength - consumedDistance;
        }

        public void MoveToWorldPosition(Vector3 worldPosition)
        {
            MoveTo(transform.InverseTransformPoint(worldPosition));
        }

        public void EndTrail()
        {
            isTracking = false;
            distanceSinceLastParticle = 0f;
        }

        public void ClearTrail()
        {
            EndTrail();
            if (particles != null)
            {
                particles.Clear(false);
            }
        }

        private void Configure()
        {
            particles = GetComponent<ParticleSystem>();
            if (particles == null)
            {
                return;
            }

            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 13f);
            main.startSize3D = true;
            main.startSizeX = new ParticleSystem.MinMaxCurve(90f, 140f);
            main.startSizeY = new ParticleSystem.MinMaxCurve(45f, 78f);
            main.startSizeZ = 1f;
            main.startColor = new Color(0.62f, 0.39f, 0.08f, 0.88f);
            main.maxParticles = MaximumParticles;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = false;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateGreaseFadeGradient());

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.72f, 1f),
                    new Keyframe(1f, 0.86f)));

            ParticleSystemRenderer particleRenderer = GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            particleRenderer.sortingOrder = 11;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            PattyParticleMaterialResources resources = PattyParticleMaterialUtility.CreateGreaseResources();
            particleMaterial = resources.Material;
            particleTexture = resources.Texture;
            particleRenderer.sharedMaterial = particleMaterial;

            particles.Play(false);
        }

        private void EnsureConfigured()
        {
            if (particles == null)
            {
                Configure();
            }
        }

        private void EmitGreaseParticle(Vector2 localPosition)
        {
            if (particles == null)
            {
                return;
            }

            float width = Random.Range(90f, 140f);
            float height = Random.Range(45f, 78f);
            var emit = new ParticleSystem.EmitParams
            {
                position = new Vector3(localPosition.x, localPosition.y, 0f),
                velocity = Vector3.zero,
                startLifetime = Random.Range(9f, 13f),
                startSize3D = new Vector3(width, height, 1f),
                rotation = Random.Range(0f, 360f),
                startColor = new Color(
                    Random.Range(0.52f, 0.70f),
                    Random.Range(0.30f, 0.45f),
                    Random.Range(0.055f, 0.12f),
                    Random.Range(0.80f, 0.94f)),
                applyShapeToPosition = false
            };
            particles.Emit(emit, 1);
        }

        private static Gradient CreateGreaseFadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.93f, 0.82f, 0.58f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.96f, 0.68f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private void OnDestroy()
        {
            PattyParticleMaterialUtility.DestroyOwned(particleMaterial);
            PattyParticleMaterialUtility.DestroyOwned(particleTexture);
            particleMaterial = null;
            particleTexture = null;
        }
    }

    /// <summary>
    /// Owns a native Particle System that follows one grill item while it cooks.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GrillCookingSmoke : MonoBehaviour
    {
        private RectTransform source;
        private RectTransform effectRect;
        private ParticleSystem particles;
        private Material particleMaterial;
        private Texture2D particleTexture;
        private PattyGrillPhase lastPhase = (PattyGrillPhase)(-1);
        private bool lastHeld;
        private bool hasState;
        private bool isPuffing;
        private bool isBurnt;
        private float nextPuffTime;

        public ParticleSystem ParticleSystem => particles;

        public void Configure(RectTransform target)
        {
            if (target == null || particles != null)
            {
                return;
            }

            source = target;
            var effectObject = new GameObject(
                target.gameObject.name + "SmokeParticles",
                typeof(RectTransform),
                typeof(ParticleSystem));
            effectRect = effectObject.GetComponent<RectTransform>();
            effectRect.SetParent(target, false);
            BurgerUiFactory.SetRect(effectRect, Vector2.zero, Vector2.zero);
            effectRect.SetAsLastSibling();

            particles = effectObject.GetComponent<ParticleSystem>();
            ConfigureParticleSystem();
            UpdatePosition();
        }

        public void SetState(PattyGrillPhase phase, bool isHeld)
        {
            if (particles == null)
            {
                return;
            }

            UpdatePosition();
            if (hasState && phase == lastPhase && isHeld == lastHeld)
            {
                return;
            }

            hasState = true;
            lastPhase = phase;
            lastHeld = isHeld;

            bool shouldEmit = !isHeld && IsSmokingPhase(phase);
            isPuffing = shouldEmit;
            if (!shouldEmit)
            {
                if (particles.isPlaying)
                {
                    particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                }
                return;
            }

            isBurnt = phase == PattyGrillPhase.Overcooked;
            ParticleSystem.MainModule main = particles.main;
            main.startColor = isBurnt
                ? new ParticleSystem.MinMaxGradient(
                    new Color(0.08f, 0.075f, 0.065f, 0.78f),
                    new Color(0.24f, 0.22f, 0.19f, 0.68f))
                : new ParticleSystem.MinMaxGradient(
                    new Color(0.48f, 0.46f, 0.42f, 0.60f),
                    new Color(0.82f, 0.80f, 0.74f, 0.50f));

            if (!particles.isPlaying)
            {
                particles.Play(false);
            }
            ScheduleNextPuff(0.02f, 0.10f);
        }

        private void Update()
        {
            if (!isPuffing || particles == null || Time.time < nextPuffTime)
            {
                return;
            }

            var emit = new ParticleSystem.EmitParams
            {
                applyShapeToPosition = true
            };
            particles.Emit(emit, isBurnt ? Random.Range(3, 6) : Random.Range(2, 5));
            if (isBurnt)
            {
                ScheduleNextPuff(0.18f, 0.38f);
            }
            else
            {
                ScheduleNextPuff(0.35f, 0.70f);
            }
        }

        private void ConfigureParticleSystem()
        {
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = particles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.duration = 2f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.startSpeed = 0f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.7f, 2.65f);
            main.startSize = new ParticleSystem.MinMaxCurve(46f, 72f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.48f, 0.46f, 0.42f, 0.60f),
                new Color(0.82f, 0.80f, 0.74f, 0.50f));
            main.maxParticles = 150;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.enabled = false;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(170f, 82f, 1f);

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.Local;
            velocity.x = new ParticleSystem.MinMaxCurve(-9f, 9f);
            velocity.y = new ParticleSystem.MinMaxCurve(50f, 82f);

            ParticleSystem.NoiseModule noise = particles.noise;
            noise.enabled = true;
            noise.separateAxes = true;
            noise.strengthX = new ParticleSystem.MinMaxCurve(8f, 17f);
            noise.strengthY = new ParticleSystem.MinMaxCurve(5f, 12f);
            noise.strengthZ = 0f;
            noise.frequency = 0.32f;
            noise.scrollSpeed = 0.22f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Medium;

            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(CreateSmokeFadeGradient());

            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.65f),
                    new Keyframe(0.45f, 1.25f),
                    new Keyframe(1f, 1.85f)));

            ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
            particleRenderer.alignment = ParticleSystemRenderSpace.View;
            particleRenderer.sortMode = ParticleSystemSortMode.YoungestInFront;
            particleRenderer.sortingOrder = 25;
            particleRenderer.shadowCastingMode = ShadowCastingMode.Off;
            particleRenderer.receiveShadows = false;

            PattyParticleMaterialResources resources = PattyParticleMaterialUtility.CreateSmokeResources();
            particleMaterial = resources.Material;
            particleTexture = resources.Texture;
            particleRenderer.sharedMaterial = particleMaterial;
        }

        private void UpdatePosition()
        {
            if (source == null || effectRect == null)
            {
                return;
            }

            effectRect.anchoredPosition = Vector2.zero;
            effectRect.localScale = Vector3.one;
            effectRect.localEulerAngles = -source.localEulerAngles;

            if (particles != null)
            {
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.scale = new Vector3(
                    Mathf.Max(1f, source.sizeDelta.x * 0.72f),
                    Mathf.Max(1f, source.sizeDelta.y * 0.52f),
                    1f);
            }
        }

        private void ScheduleNextPuff(float minimumDelay, float maximumDelay)
        {
            nextPuffTime = Time.time + Random.Range(minimumDelay, maximumDelay);
        }

        private static bool IsSmokingPhase(PattyGrillPhase phase)
        {
            return phase == PattyGrillPhase.CookingSide1 ||
                phase == PattyGrillPhase.ReadyToFlip ||
                phase == PattyGrillPhase.Flipping ||
                phase == PattyGrillPhase.CookingSide2 ||
                phase == PattyGrillPhase.Done ||
                phase == PattyGrillPhase.Overcooked;
        }

        private static Gradient CreateSmokeFadeGradient()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.88f, 0.86f, 0.82f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.15f, 0f),
                    new GradientAlphaKey(0.82f, 0.06f),
                    new GradientAlphaKey(0.68f, 0.58f),
                    new GradientAlphaKey(0f, 1f)
                });
            return gradient;
        }

        private void OnDestroy()
        {
            PattyParticleMaterialUtility.DestroyOwned(particleMaterial);
            PattyParticleMaterialUtility.DestroyOwned(particleTexture);
            effectRect = null;
            particles = null;
            particleMaterial = null;
            particleTexture = null;
        }
    }

    internal readonly struct PattyParticleMaterialResources
    {
        public PattyParticleMaterialResources(Material material, Texture2D texture)
        {
            Material = material;
            Texture = texture;
        }

        public Material Material { get; }

        public Texture2D Texture { get; }
    }

    internal static class PattyParticleMaterialUtility
    {
        public static PattyParticleMaterialResources CreateGreaseResources()
        {
            return CreateResources("Runtime Patty Grease", CreateGreaseTexture());
        }

        public static PattyParticleMaterialResources CreateSmokeResources()
        {
            return CreateResources("Runtime Patty Smoke", CreateSmokeTexture());
        }

        public static void DestroyOwned(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        private static PattyParticleMaterialResources CreateResources(string name, Texture2D texture)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            var material = new Material(shader)
            {
                name = name,
                hideFlags = HideFlags.DontSave,
                renderQueue = (int)RenderQueue.Transparent
            };
            material.mainTexture = texture;
            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", texture);
            }
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }
            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            }
            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            }
            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return new PattyParticleMaterialResources(material, texture);
        }

        private static Texture2D CreateGreaseTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Patty Grease Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size) * 2f - 1f;
                    float ny = ((y + 0.5f) / size) * 2f - 1f;
                    float angle = Mathf.Atan2(ny, nx);
                    float wobble = 1f + 0.10f * Mathf.Sin(angle * 5f + 0.8f) +
                        0.055f * Mathf.Sin(angle * 9f - 1.4f);
                    float distance = Mathf.Sqrt(nx * nx + ny * ny) / wobble;
                    float outerFade = 1f - Mathf.SmoothStep(0.68f, 1f, distance);
                    float innerVariation = 0.72f +
                        0.18f * Mathf.Sin(nx * 7.3f + ny * 4.1f) * Mathf.Sin(ny * 6.7f);
                    byte alpha = (byte)Mathf.Clamp(
                        Mathf.RoundToInt(255f * outerFade * innerVariation),
                        0,
                        255);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateSmokeTexture()
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true)
            {
                name = "Runtime Patty Smoke Texture",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float nx = ((x + 0.5f) / size) * 2f - 1f;
                    float ny = ((y + 0.5f) / size) * 2f - 1f;
                    float distance = Mathf.Sqrt(nx * nx + ny * ny);
                    float alphaValue = 1f - Mathf.SmoothStep(0.12f, 1f, distance);
                    alphaValue *= 0.82f + 0.12f * Mathf.Sin(nx * 5.1f + ny * 6.4f);
                    byte alpha = (byte)Mathf.Clamp(
                        Mathf.RoundToInt(255f * alphaValue),
                        0,
                        255);
                    pixels[y * size + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }
    }
}
