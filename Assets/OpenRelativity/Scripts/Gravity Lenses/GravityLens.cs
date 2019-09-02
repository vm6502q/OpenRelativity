using UnityEngine;

public class GravityLens : MonoBehaviour
{
    public Camera cam;
    public Material lensMaterial;
    public GravityLens mirrorLens;
    public bool isMirror;

    protected bool doBlit = true;
    protected RenderTexture lensPass;

    private void Start()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (doBlit)
        {
            if (mirrorLens)
            {
                if (!isMirror && lensPass == null)
                {
                    lensPass = new RenderTexture(src);
                }

                if (isMirror)
                {
                    lensMaterial.SetTexture("_lensTex", mirrorLens.lensPass);
                    Graphics.Blit(src, dest, lensMaterial);
                }
                else
                {
                    Graphics.Blit(src, lensPass, lensMaterial);
                    mirrorLens.GetComponent<GravityMirror>().ManualUpdate();
                }
            }
            else
            {
                Graphics.Blit(src, dest, lensMaterial);
            }
        } else
        {
            Graphics.Blit(src, dest);
        }
    }
}
