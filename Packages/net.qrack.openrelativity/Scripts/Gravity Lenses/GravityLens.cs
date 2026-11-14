using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OpenRelativity.GravityLenses
{
    public class GravityLens : MonoBehaviour
    {
        public Camera cam;
        public Material lensMaterial;
        public GravityLens mirrorLens;
        public bool isMirror;
        public bool isSkybox = false;

        protected bool doBlit;
        protected bool wasBlit;
        protected List<RenderTexture> lensPass;

        // The Universal Render Pipeline doesn't call "OnRenderImage()". Instead, we queue this pass on our camera, each frame.
        private FullScreenMaterialPass lensRenderPass;
        // A lens with a (gravity) mirror keeps a copy of its output, here, for the mirror lens to composite with its own.
        private RenderTexture lensPassTexture;
        private RTHandle lensPassHandle;

        virtual protected void Start()
        {
            doBlit = true;
            wasBlit = false;
            if (cam == null)
            {
                cam = GetComponent<Camera>();
            }
        }

        virtual protected void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        virtual protected void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            ReleaseLensPassTexture();
        }

        // This takes the place of "OnRenderImage()", with the same logic. Where that method would
        // "Graphics.Blit(src, dest, lensMaterial)", we queue a full screen pass with the material, and where it would
        // "Graphics.Blit(src, dest)", (unmodified,) we just don't queue the pass.
        protected void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (cam == null || renderingCamera != cam)
            {
                return;
            }

            if (isSkybox)
            {
                return;
            }

            if (doBlit)
            {
                wasBlit = true;
                if (mirrorLens)
                {
                    if (isMirror)
                    {
                        if (mirrorLens.lensPass != null && mirrorLens.lensPass.Count > 0)
                        {
                            lensMaterial.SetTexture("_lensTex", mirrorLens.lensPass[0]);
                            mirrorLens.lensPass.RemoveAt(0);
                            EnqueueLensPass(null);
                        }
                    }
                    else
                    {
                        if (lensPass == null)
                        {
                            lensPass = new List<RenderTexture>();
                        }
                        if (EnqueueLensPass(GetLensPassTexture()) && !lensPass.Contains(lensPassTexture))
                        {
                            lensPass.Add(lensPassTexture);
                        }
                    }
                }
                else
                {
                    EnqueueLensPass(null);
                }
            }
            else
            {
                if (wasBlit && isMirror && mirrorLens)
                {
                    wasBlit = false;
                    gameObject.SetActive(false);
                } else
                {
                    wasBlit = false;
                }
            }
        }

        private bool EnqueueLensPass(RTHandle capture)
        {
            if (lensRenderPass == null)
            {
                lensRenderPass = new FullScreenMaterialPass("Gravity Lens");
            }

            return lensRenderPass.Enqueue(cam, lensMaterial, capture);
        }

        // With "OnRenderImage()", a lens with a mirror handed its own destination texture to the mirror lens.
        // Render graph textures only live for the camera that they're made for, so we own a persistent copy.
        private RTHandle GetLensPassTexture()
        {
            int width = Mathf.Max(1, cam.pixelWidth);
            int height = Mathf.Max(1, cam.pixelHeight);

            if (lensPassTexture != null && (lensPassTexture.width != width || lensPassTexture.height != height))
            {
                // The list would hold a reference to a destroyed texture.
                if (lensPass != null)
                {
                    lensPass.Remove(lensPassTexture);
                }
                ReleaseLensPassTexture();
            }

            if (lensPassTexture == null)
            {
                lensPassTexture = new RenderTexture(width, height, 0, cam.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default)
                {
                    name = "Gravity Lens Pass (" + gameObject.name + ")",
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
                lensPassTexture.Create();
                lensPassHandle = RTHandles.Alloc(lensPassTexture);
            }

            return lensPassHandle;
        }

        private void ReleaseLensPassTexture()
        {
            if (lensPassHandle != null)
            {
                lensPassHandle.Release();
                lensPassHandle = null;
            }

            if (lensPassTexture != null)
            {
                lensPassTexture.Release();
                Destroy(lensPassTexture);
                lensPassTexture = null;
            }
        }
    }
}