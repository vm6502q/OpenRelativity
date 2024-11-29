using UnityEngine;
using OpenRelativity;

namespace Tachyoid
{
    public class ResetBounds : MonoBehaviour
    {

        public Vector3 resetPos;
        private float resetRadius;
        private GameState state;
        private Transform playerTransform;

        // Use this for initialization
        protected void Start()
        {
            state = FindFirstObjectByType<GameState>();
            playerTransform = state.playerTransform;
            resetRadius = GetComponent<SphereCollider>().radius * transform.lossyScale.x;
        }

        protected void Update()
        {
            if ((playerTransform.position - transform.position).sqrMagnitude > (resetRadius * resetRadius))
            {
                state.playerTransform.position = resetPos;
                state.PlayerVelocityVector = Vector3.zero;
            }
        }
    }
}