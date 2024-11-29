using UnityEngine;

namespace Qrack
{
#if OPEN_RELATIVITY_INCLUDED
    public class ActionIndicator : OpenRelativity.RelativisticBehavior
    {
#else
    public class ActionIndicator : MonoBehaviour
    {
        public Transform playerTransform;
#endif
        // Use this for initialization
        protected void Awake()
        {
            SetState(false);
        }

        protected void Update()
        {
#if OPEN_RELATIVITY_INCLUDED
            transform.LookAt(state.playerTransform, transform.up);
#else
            if (playerTransform != null) {
                transform.LookAt(playerTransform, transform.up);
            }
#endif
        }

        public void SetState(bool onOrOff)
        {
            Renderer myRenderer = transform.GetComponent<Renderer>();

            if (myRenderer == null)
            {
                return;
            }

            myRenderer.enabled = onOrOff;
        }
    }
}
