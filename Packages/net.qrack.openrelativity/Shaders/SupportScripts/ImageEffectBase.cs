using UnityEngine;
using UnityEngine.Rendering;

namespace OpenRelativity
{
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("")]
    public class ImageEffectBase : MonoBehaviour
    {
        /// Provides a shader property that is set in the inspector
        /// and a material instantiated from the shader
        public Shader shader;
        private Material m_Material;

        protected void Start()
        {
            // Disable the image effect if the shader can't
            // run on the users graphics card
            if (!shader || !shader.isSupported) {
                enabled = false;
            }
        }

        protected Material material
        {
            get
            {
                if (m_Material == null)
                {
                    m_Material = new Material(shader);
                    m_Material.hideFlags = HideFlags.HideAndDontSave;
                }
                return m_Material;
            }
        }

        // The Universal Render Pipeline doesn't call "OnRenderImage()". Instead, we queue this pass on our camera, each frame.
        private FullScreenMaterialPass m_RenderPass;
        private Camera m_Camera;

        protected void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
        }

        protected void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

            if (m_Material)
            {
                DestroyImmediate(m_Material);
            }
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera renderingCamera)
        {
            if (m_Camera == null)
            {
                m_Camera = GetComponent<Camera>();
            }

            if (renderingCamera != m_Camera || !shader)
            {
                return;
            }

            if (m_RenderPass == null)
            {
                m_RenderPass = new FullScreenMaterialPass(GetType().Name);
            }

            m_RenderPass.Enqueue(renderingCamera, material);
        }
    }
}