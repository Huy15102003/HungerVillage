using System.Collections.Generic;
using UnityEngine;

public class CameraOcclusion : MonoBehaviour
{
    public Transform target;
    public LayerMask mapLayer;

    private HashSet<Renderer> currentHits = new();

    [Header("Debug")]
    [SerializeField] private bool ShowDebugGizmos = true;
    [SerializeField] private float GizmoSphereSize = 0.1f;

    // cache of the last unfiltered hits along the ray (used for gizmo drawing)
    private RaycastHit[] lastAllHits;

    void LateUpdate()
    {
        if(target == null)
            return;
        HashSet<Renderer> newHits = new();

        Vector3 dir = target.position - transform.position;
        float dist = dir.magnitude;

        RaycastHit[] hits = Physics.RaycastAll(
            transform.position,
            dir.normalized,
            dist,
            mapLayer
        );

        // also store all hits (no layer mask) for debug drawing so we can see if the player blocks the ray
        lastAllHits = Physics.RaycastAll(transform.position, dir.normalized, dist);

        foreach (RaycastHit hit in hits)
        {
            Renderer r = hit.collider.GetComponent<Renderer>();
            //Debug.Log($"Hit: {hit.collider.name}");
            if (r == null)
                continue;

            newHits.Add(r);

            SetAlpha(r, 0.2f);
        }

        foreach (Renderer r in currentHits)
        {
            if (!newHits.Contains(r))
            {
                SetAlpha(r, 1f);
            }
        }

        currentHits = newHits;
    }

    void SetAlpha(Renderer r, float alpha)
    {
        Material mat = r.material;

        Color c = mat.color;
        c.a = alpha;

        mat.color = c;
    }

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