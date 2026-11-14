using UnityEngine;
using System.Collections;

namespace OpenRelativity
{
    [ExecuteInEditMode]
    [AddComponentMenu("Image Effects/Color Correction (Ramp)")]
    public class ColorCorrectionEffect : ImageEffectBase
    {
        // (The base class queues a full screen pass on the camera, with a material made from the
        // shader that's set in the inspector, where this used to "Graphics.Blit()" in "OnRenderImage()".)
    }
}
