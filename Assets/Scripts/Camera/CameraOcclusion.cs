using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusion : MonoBehaviour
{
    public Transform target;
    public LayerMask mapLayer;
    // previous continuous-raycast storage removed in favor of single-shot capture
    // When requested, we store occlusion targets (the GameObjects hit by a ray between
    // this camera and the target). These objects will be deactivated while single-shot
    // occlusion is applied and restored afterwards.
    private readonly HashSet<GameObject> occlusionTargets = new();
    private readonly Dictionary<GameObject, bool> originalActiveState = new();

    [Header("Debug")]
    [SerializeField] private bool ShowDebugGizmos = true;
    [SerializeField] private float GizmoSphereSize = 0.1f;

    // cache of the last unfiltered hits along the ray (used for gizmo drawing)
    private RaycastHit[] lastAllHits;

    // NOTE: we no longer perform raycasts every frame. Call CaptureOcclusionTargets()
    // to perform the raycast once (for example when entering turn base), then call
    // ApplyOcclusion() to hide the captured objects. Call RestoreOcclusion() when done.
    void LateUpdate()
    {
        // Intentionally empty to avoid continuous raycasts.
    }

    // Perform a single raycast from this camera to the target and capture hit objects.
    public void CaptureOcclusionTargets()
    {
        occlusionTargets.Clear();
        originalActiveState.Clear();

        if (target == null)
            return;

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        // capture filtered hits (mapLayer)
        RaycastHit[] hits = Physics.RaycastAll(transform.position, dir.normalized, dist, mapLayer);
        // also store all hits (no layer mask) for debug drawing so we can see if the player blocks the ray
        lastAllHits = Physics.RaycastAll(transform.position, dir.normalized, dist);

        foreach (var hit in hits)
        {
            var go = hit.collider.gameObject;
            if (go == null)
                continue;

            if (!occlusionTargets.Contains(go))
            {
                occlusionTargets.Add(go);
                originalActiveState[go] = go.activeSelf;
            }
        }
    }

    // Deactivate captured objects.
    public void ApplyOcclusion()
    {
        foreach (var go in occlusionTargets)
        {
            if (go != null)
                go.SetActive(false);
        }
    }

    // Restore original active state of captured objects.
    public void RestoreOcclusion()
    {
        foreach (var kv in originalActiveState)
        {
            if (kv.Key != null)
                kv.Key.SetActive(kv.Value);
        }
        occlusionTargets.Clear();
        originalActiveState.Clear();
    }

    void SetAlpha(Renderer r, float alpha)
    {
        Material mat = r.material;

        Color c = mat.color;
        c.a = alpha;

        mat.color = c;
    }

    //// Capture occlusion targets by raycasting once from this camera to the target.
    //// This will populate occlusionTargets with the hit GameObjects (using hit.collider.gameObject)
    //// and store their original active state in originalActiveState.
    //public void CaptureOcclusionTargets()
    //{
    //    occlusionTargets.Clear();
    //    originalActiveState.Clear();

    //    if (target == null) return;

    //    Vector3 dir = target.position - transform.position;
    //    float dist = dir.magnitude;

    //    // Raycast using the configured mapLayer mask
    //    RaycastHit[] hits = Physics.RaycastAll(transform.position, dir.normalized, dist, mapLayer);

    //    foreach (var h in hits)
    //    {
    //        var go = h.collider.gameObject;
    //        if (go == null) continue;

    //        if (!occlusionTargets.Contains(go))
    //        {
    //            occlusionTargets.Add(go);
    //            originalActiveState[go] = go.activeSelf;
    //            Debug.Log($"Captured occlusion target: {go.name}");
    //        }
    //    }
    //}

    //// Hide captured targets. Call after CaptureOcclusionTargets() when entering turnbase.
    //public void ApplyOcclusion()
    //{
    //    foreach (var go in occlusionTargets)
    //    {
    //        if (go == null) continue;
    //        go.SetActive(false);
    //    }
    //}

    //// Restore captured targets to their original active state. Call when exiting turnbase.
    //public void RestoreOcclusion()
    //{
    //    foreach (var kv in originalActiveState)
    //    {
    //        var go = kv.Key;
    //        bool wasActive = kv.Value;
    //        if (go == null) continue;
    //        go.SetActive(wasActive);
    //    }

    //    occlusionTargets.Clear();
    //    originalActiveState.Clear();
    //}

    void OnDrawGizmos()
    {
        if (!ShowDebugGizmos || target == null)
            return;

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        // draw main ray
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + dir);

        // draw filtered hits (mapLayer) as yellow spheres
        RaycastHit[] filtered = Physics.RaycastAll(transform.position, dir.normalized, dist, mapLayer);
        Gizmos.color = Color.yellow;
        foreach (var h in filtered)
        {
            Gizmos.DrawSphere(h.point, GizmoSphereSize);
        }

        // draw all hits (no mask) as red spheres to detect player blocking
        if (lastAllHits != null)
        {
            Gizmos.color = Color.red;
            foreach (var h in lastAllHits)
            {
                Gizmos.DrawWireSphere(h.point, GizmoSphereSize * 1.3f);
            }
        }
    }
}