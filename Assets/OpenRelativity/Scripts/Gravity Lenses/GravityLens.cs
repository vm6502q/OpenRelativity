using UnityEngine;

namespace OpenRelativity.GravityLenses
{
    public class GravityLens : MonoBehaviour
    {
        public Camera cam;
        public Material lensMaterial;
        public GravityLens mirrorLens;
        public bool isMirror;

        protected bool doBlit;
        protected bool wasBlit;
        protected RenderTexture lensPass;

        private void Start()
        {
            doBlit = true;
            wasBlit = false;
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }
        }

        void OnRenderImage(RenderTexture src, RenderTexture dest)
        {
            if (doBlit)
            {
                wasBlit = true;
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
            }
            else
            {
                if (wasBlit && isMirror && mirrorLens)
                {
                    wasBlit = false;
                    gameObject.SetActive(false);
                }
                else
                {
                    wasBlit = false;
                    Graphics.Blit(src, dest);
                }
            }
        }
    }
}
