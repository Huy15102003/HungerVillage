using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTransparencyManager : MonoBehaviour
{
    public static MapTransparencyManager Instance;

    [Header("Layer chứa Map")]
    [SerializeField] private string mapLayerName = "Map";

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private float combatAlpha = 0.3f;

    public enum FadeMode { Alpha, Dither }
    [SerializeField] private FadeMode fadeMode = FadeMode.Dither;

    [Header("Dither Settings")]
    // If set, this property name will be used preferentially for all materials
    [SerializeField] private string DitherPropertyOverride = "";
    [SerializeField] private Material DitherMaterial = null;

    // store original materials when we swap to dither material at runtime
    private readonly Dictionary<Renderer, Material[]> originalMaterials = new();

    private readonly List<Renderer> mapRenderers = new();
    private Coroutine fadeRoutine;

    private void Awake()
    {
        Instance = this;
        CacheMapRenderers();
    }

    private void CacheMapRenderers()
    {
        mapRenderers.Clear();

        int mapLayer = LayerMask.NameToLayer(mapLayerName);

        Renderer[] allRenderers = FindObjectsByType<Renderer>(
            FindObjectsSortMode.None);

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer.gameObject.layer == mapLayer)
            {
                mapRenderers.Add(renderer);
            }
        }

        Debug.Log($"Found {mapRenderers.Count} map renderers.");
        // Log a small sample of materials for diagnostics
        int sample = 0;
        foreach (var r in mapRenderers)
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                Debug.Log($"Map material: {m.name} (shader: {m.shader.name})");
                sample++;
                if (sample >= 8) break;
            }
            if (sample >= 8) break;
        }
    }

    [ContextMenu("Dump Map Materials Properties")]
    public void DumpMapMaterialsProperties()
    {
        int i = 0;
        foreach (var r in mapRenderers)
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                Debug.Log($"Material[{i}] {m.name} - shader {m.shader.name}");
                i++;
            }
        }
    }
    [ContextMenu("Test Enter Combat")]
    public void EnterCombat()
    {
        StartFade(combatAlpha);
    }
    [ContextMenu("Test Exit Combat")]
    public void ExitCombat()
    {
        StartFade(1f);
    }
   

    private void StartFade(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        // If we are using Dither mode and a DitherMaterial is provided, swap opaque materials to instances
        // of the dither material so we can animate a known property on them at runtime.
        bool swapped = false;
        if (fadeMode == FadeMode.Dither && DitherMaterial != null)
        {
            swapped = SwapOpaqueToDither();
        }

        float elapsed = 0f;

        Dictionary<Material, float> startAlphas = new();
        // For dither mode: cache which property to animate per material and its start value
        Dictionary<Material, (string prop, float start)> ditherProps = new();

        // Common candidate property names used by various dither implementations / shader graphs
        string[] ditherCandidates = new string[] {
            "_DitherFactor", "_DitherFade", "_Dither", "_DitherOpacity", "_DitherStrength", "_DitherAmount", "_DitherThreshold", "_DitherValue"
        };

        foreach (Renderer renderer in mapRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (!startAlphas.ContainsKey(mat))
                {
                        startAlphas.Add(mat, mat.color.a);

                        if (fadeMode == FadeMode.Dither)
                        {
                            // try to find a dither property on the material
                            foreach (var cand in ditherCandidates)
                            {
                                if (mat.HasProperty(cand))
                                {
                                    float val = mat.GetFloat(cand);
                                    ditherProps[mat] = (cand, val);
                                    break;
                                }
                            }
                            // if no dither property found, leave it to alpha fallback
                        }
                }
            }
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);

            foreach (Renderer renderer in mapRenderers)
            {
                foreach (Material mat in renderer.materials)
                {
                    if (mat == null)
                        continue;

                        if (fadeMode == FadeMode.Dither && ditherProps.ContainsKey(mat))
                        {
                            var (prop, startVal) = ditherProps[mat];
                            // dither property expected in range 0..1 where 0 = fully visible, 1 = fully hidden in some implementations
                            float value = Mathf.Lerp(startVal, targetAlpha, t);
                            mat.SetFloat(prop, value);
                        }
                        else
                        {
                            Color c = mat.color;

                            c.a = Mathf.Lerp(
                                startAlphas[mat],
                                targetAlpha,
                                t);

                            mat.color = c;
                        }
                }
            }

            yield return null;
        }

        foreach (Renderer renderer in mapRenderers)
        {
            foreach (Material mat in renderer.materials)
            {
                if (mat == null)
                    continue;
                if (fadeMode == FadeMode.Dither && ditherProps.ContainsKey(mat))
                {
                    var (prop, startVal) = ditherProps[mat];
                    mat.SetFloat(prop, targetAlpha);
                }
                else
                {
                    Color c = mat.color;
                    c.a = targetAlpha;
                    mat.color = c;
                }
            }
        }

        // If we swapped materials for dither, and we're restoring to fully visible (alpha 1), restore originals
        if (swapped && Mathf.Approximately(targetAlpha, 1f))
        {
            RestoreOriginalMaterials();
        }
    }

    // Swap renderers that appear opaque to instances of DitherMaterial. Returns true if any swap occurred.
    private bool SwapOpaqueToDither()
    {
        if (DitherMaterial == null) return false;

        bool didSwap = false;

        foreach (var renderer in mapRenderers)
        {
            if (renderer == null) continue;

            var shared = renderer.sharedMaterials;
            Material[] instances = new Material[shared.Length];
            bool any = false;

            for (int i = 0; i < shared.Length; ++i)
            {
                var orig = shared[i];
                if (orig == null) continue;

                // try to detect opaque by common property _Surface (URP Lit: 0 opaque, 1 transparent)
                bool isTransparent = false;
                if (orig.HasProperty("_Surface"))
                {
                    float surf = orig.GetFloat("_Surface");
                    isTransparent = surf > 0.5f;
                }

                if (isTransparent)
                {
                    // keep original
                    instances[i] = orig;
                }
                else
                {
                    // create instance of DitherMaterial and copy main texture/color where possible
                    Material m = new Material(DitherMaterial);
                    // copy base texture if present
                    if (orig.HasProperty("_BaseMap") && m.HasProperty("_BaseMap"))
                    {
                        m.SetTexture("_BaseMap", orig.GetTexture("_BaseMap"));
                    }
                    else if (orig.HasProperty("_MainTex") && m.HasProperty("_MainTex"))
                    {
                        m.SetTexture("_MainTex", orig.GetTexture("_MainTex"));
                    }
                    // copy color
                    if (orig.HasProperty("_BaseColor") && m.HasProperty("_BaseColor"))
                        m.SetColor("_BaseColor", orig.GetColor("_BaseColor"));
                    else if (orig.HasProperty("_Color") && m.HasProperty("_Color"))
                        m.SetColor("_Color", orig.GetColor("_Color"));

                    instances[i] = m;
                    any = true;
                }
            }

            if (any)
            {
                // store originals and assign instances
                originalMaterials[renderer] = renderer.sharedMaterials;
                renderer.materials = instances;
                didSwap = true;
            }
        }

        return didSwap;
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var kv in originalMaterials)
        {
            var renderer = kv.Key;
            var mats = kv.Value;
            if (renderer == null) continue;
            // destroy any created materials to avoid leaks
            foreach (var mat in renderer.materials)
            {
                if (mat == null) continue;
                if (mat.shader == DitherMaterial?.shader)
                {
                    Destroy(mat);
                }
            }

            renderer.materials = mats;
        }

        originalMaterials.Clear();
    }
}