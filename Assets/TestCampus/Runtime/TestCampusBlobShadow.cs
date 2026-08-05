using UnityEngine;
using UnityEngine.Rendering;

namespace CrazyMarket.TestCampus
{
    [DisallowMultipleComponent]
    public sealed class TestCampusBlobShadow : MonoBehaviour
    {
        [SerializeField] private float maxDistance = 18f;
        [SerializeField] private float groundOffset = 0.025f;
        [SerializeField] private float groundedRadius = 0.95f;
        [SerializeField] private float airborneRadius = 0.5f;
        [SerializeField] private float groundedAlpha = 0.48f;
        [SerializeField] private float airborneAlpha = 0.14f;

        private Transform _shadow;
        private Material _material;
        private readonly RaycastHit[] _hits = new RaycastHit[12];

        private void Awake()
        {
            GameObject shadowObject = new("Player Blob Shadow");
            _shadow = shadowObject.transform;
            MeshFilter filter = shadowObject.AddComponent<MeshFilter>();
            MeshRenderer renderer = shadowObject.AddComponent<MeshRenderer>();
            filter.sharedMesh = CreateDisc();
            Shader shader = Shader.Find("Sprites/Default");
            _material = new Material(shader) { name = "Test Campus Blob Shadow (Runtime)" };
            _material.color = new Color(0.02f, 0.025f, 0.035f, groundedAlpha);
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private void LateUpdate()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            int count = Physics.RaycastNonAlloc(origin, Vector3.down, _hits, maxDistance, ~0, QueryTriggerInteraction.Ignore);
            bool found = false;
            RaycastHit closest = default;
            float closestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (_hits[i].collider.transform.IsChildOf(transform) || _hits[i].distance >= closestDistance)
                    continue;
                closest = _hits[i];
                closestDistance = _hits[i].distance;
                found = true;
            }

            _shadow.gameObject.SetActive(found);
            if (!found) return;

            float height = Mathf.Max(0f, closestDistance - 0.5f);
            float normalizedHeight = Mathf.Clamp01(height / maxDistance);
            float radius = Mathf.Lerp(groundedRadius, airborneRadius, normalizedHeight);
            float alpha = Mathf.Lerp(groundedAlpha, airborneAlpha, normalizedHeight);
            _shadow.position = closest.point + closest.normal * groundOffset;
            _shadow.rotation = Quaternion.FromToRotation(Vector3.up, closest.normal);
            _shadow.localScale = new Vector3(radius, radius * 0.78f, radius);
            _material.color = new Color(0.02f, 0.025f, 0.035f, alpha);
        }

        private void OnDestroy()
        {
            if (_shadow != null) Destroy(_shadow.gameObject);
            if (_material != null) Destroy(_material);
        }

        private static Mesh CreateDisc()
        {
            const int segments = 40;
            Vector3[] vertices = new Vector3[segments + 1];
            Color[] colors = new Color[segments + 1];
            int[] triangles = new int[segments * 3];
            vertices[0] = Vector3.zero;
            colors[0] = Color.white;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                colors[i + 1] = new Color(1f, 1f, 1f, 0f);
                int triangle = i * 3;
                triangles[triangle] = 0;
                triangles[triangle + 1] = (i + 1) % segments + 1;
                triangles[triangle + 2] = i + 1;
            }
            Mesh mesh = new() { name = "Test Campus Soft Blob Shadow" };
            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
