using UnityEngine;

public class PlayerShieldAura : MonoBehaviour
{
    private MeshRenderer bubbleRenderer;
    private Material bubbleMat;
    private GameObject ringHorizontal;
    private GameObject ringAngled;
    private Material ringMat1;
    private Material ringMat2;

    private Vector2 textureOffset = Vector2.zero;
    private float pulseTimer;

    private Color hexGridColor = new Color(0.15f, 0.85f, 1f, 0.40f);
    private Color ringColor1 = new Color(0.2f, 0.9f, 1f, 0.60f);
    private Color ringColor2 = new Color(0.1f, 0.75f, 1f, 0.45f);

    public void Setup(bool isSpaceship)
    {
        // 1. Create Hexagonal Forcefield Lattice Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "HexForcefieldBubble";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = isSpaceship ? Vector3.zero : new Vector3(0f, 0.1f, 0f);

        if (isSpaceship)
        {
            sphere.transform.localScale = new Vector3(2.6f, 1.35f, 3.2f);
        }
        else
        {
            sphere.transform.localScale = new Vector3(1.6f, 2.0f, 1.6f);
        }

        Collider col = sphere.GetComponent<Collider>();
        if (col != null) Destroy(col);

        bubbleRenderer = sphere.GetComponent<MeshRenderer>();
        if (bubbleRenderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                         ?? Shader.Find("Universal Render Pipeline/Lit");

            if (shader != null)
            {
                bubbleMat = new Material(shader);
                bubbleMat.SetFloat("_Surface", 1.0f); // Transparent
                bubbleMat.SetFloat("_Blend", 0.0f);   // Alpha blend
                bubbleMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                bubbleMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                bubbleMat.SetInt("_ZWrite", 0);
                bubbleMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Back); // Cull inner faces for crystal clear view
                bubbleMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                bubbleMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                bubbleMat.renderQueue = 3000;

                Texture2D hexTex = Resources.Load<Texture2D>("PowerUps/TX_HexForcefield");
                if (hexTex != null)
                {
                    bubbleMat.SetTexture("_BaseMap", hexTex);
                    bubbleMat.SetTexture("_MainTex", hexTex);
                    bubbleMat.SetTextureScale("_BaseMap", new Vector2(6f, 3f));
                    bubbleMat.SetTextureScale("_MainTex", new Vector2(6f, 3f));
                }

                bubbleMat.SetColor("_BaseColor", hexGridColor);
                bubbleMat.SetColor("_Color", hexGridColor);

                if (bubbleMat.HasProperty("_EmissionColor"))
                {
                    bubbleMat.EnableKeyword("_EMISSION");
                    bubbleMat.SetColor("_EmissionColor", new Color(0.04f, 0.2f, 0.35f, 0.25f));
                }

                bubbleRenderer.material = bubbleMat;
            }
        }

        // 2. Create Dual Holographic Sci-Fi Gyro Tech Rings
        Vector3 ringScale1 = isSpaceship ? new Vector3(3.2f, 3.2f, 1f) : new Vector3(2.1f, 2.1f, 1f);
        Vector3 ringScale2 = isSpaceship ? new Vector3(3.3f, 3.3f, 1f) : new Vector3(2.2f, 2.2f, 1f);

        ringHorizontal = CreateTechRing("TechRing_Equatorial", "PowerUps/TX_Ring_Hexagon_01", ringScale1, Quaternion.Euler(90f, 0f, 0f), ringColor1, out ringMat1);
        ringAngled = CreateTechRing("TechRing_Angled", "PowerUps/TX_Ring_Hexagon_02", ringScale2, Quaternion.Euler(60f, 35f, 0f), ringColor2, out ringMat2);

        // 3. Subtle, non-blinding ambient light
        GameObject lightObj = new GameObject("ShieldFaintGlow");
        lightObj.transform.SetParent(transform, false);
        lightObj.transform.localPosition = Vector3.zero;
        Light lt = lightObj.AddComponent<Light>();
        lt.type = LightType.Point;
        lt.range = isSpaceship ? 4.5f : 3.0f;
        lt.intensity = 0.22f; // Soft, gentle fill
        lt.color = new Color(0.2f, 0.85f, 1f);

        // 4. Looping Hexagon Floating Sparkles
        GameObject hexPrefab = Resources.Load<GameObject>("PowerUps/FX_Hexagon_01");
        if (hexPrefab != null)
        {
            GameObject hex = Instantiate(hexPrefab, transform);
            hex.transform.localPosition = Vector3.zero;
            hex.transform.localScale = isSpaceship ? new Vector3(1.7f, 1.1f, 2.1f) : Vector3.one * 1.1f;
            foreach (var ps in hex.GetComponentsInChildren<ParticleSystem>())
            {
                var main = ps.main;
                main.loop = true;
                if (!ps.isPlaying) ps.Play();
            }
        }
    }

    private GameObject CreateTechRing(string name, string texturePath, Vector3 scale, Quaternion initialRot, Color col, out Material outMat)
    {
        outMat = null;
        GameObject ringObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        ringObj.name = name;
        ringObj.transform.SetParent(transform, false);
        ringObj.transform.localPosition = Vector3.zero;
        ringObj.transform.localRotation = initialRot;
        ringObj.transform.localScale = scale;

        Collider c = ringObj.GetComponent<Collider>();
        if (c != null) Destroy(c);

        MeshRenderer mr = ringObj.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit") 
                         ?? Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader != null)
            {
                outMat = new Material(shader);
                outMat.SetFloat("_Surface", 1.0f);
                outMat.SetFloat("_Blend", 0.0f);
                outMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                outMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                outMat.SetInt("_ZWrite", 0);
                outMat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off); // Visible from all angles
                outMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                outMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                outMat.renderQueue = 3010;

                Texture2D tex = Resources.Load<Texture2D>(texturePath);
                if (tex != null)
                {
                    outMat.SetTexture("_BaseMap", tex);
                    outMat.SetTexture("_MainTex", tex);
                }

                outMat.SetColor("_BaseColor", col);
                outMat.SetColor("_Color", col);
                mr.material = outMat;
            }
        }
        return ringObj;
    }

    private void Update()
    {
        // 1. Slowly rotate and crawl the hexagonal grid matrix
        transform.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);

        if (bubbleMat != null)
        {
            textureOffset.x += 0.015f * Time.deltaTime;
            textureOffset.y += 0.035f * Time.deltaTime;
            if (bubbleMat.HasProperty("_BaseMap"))
                bubbleMat.SetTextureOffset("_BaseMap", textureOffset);
            if (bubbleMat.HasProperty("_MainTex"))
                bubbleMat.SetTextureOffset("_MainTex", textureOffset);
        }

        // 2. Rotate the holographic tech rings around their respective axes
        if (ringHorizontal != null)
        {
            ringHorizontal.transform.Rotate(Vector3.forward, 45f * Time.deltaTime, Space.Self);
        }
        if (ringAngled != null)
        {
            ringAngled.transform.Rotate(Vector3.forward, -32f * Time.deltaTime, Space.Self);
        }

        // 3. Gentle energy breathing pulse
        pulseTimer += Time.deltaTime * 2.2f;
        float pulse = 0.88f + Mathf.Sin(pulseTimer) * 0.12f;

        if (ringMat1 != null)
        {
            Color c1 = ringColor1 * pulse;
            c1.a = ringColor1.a * pulse;
            ringMat1.SetColor("_BaseColor", c1);
        }
        if (ringMat2 != null)
        {
            Color c2 = ringColor2 * pulse;
            c2.a = ringColor2.a * pulse;
            ringMat2.SetColor("_BaseColor", c2);
        }
    }
}
