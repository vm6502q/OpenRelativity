using UnityEngine;

public class GravityLens : MonoBehaviour
{
    public Material lensMaterial;

    protected bool doBlit = true;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (doBlit)
        {
            Graphics.Blit(src, dest, lensMaterial);
        } else
        {
            Graphics.Blit(src, dest);
        }
    }
}
