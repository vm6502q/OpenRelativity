using UnityEngine;

public class GravityLens : MonoBehaviour
{
    public Material lensMaterial;

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, lensMaterial);
    }
}
