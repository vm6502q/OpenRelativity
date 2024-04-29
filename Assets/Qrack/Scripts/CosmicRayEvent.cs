using UnityEngine;

namespace OpenRelativity {
    public class CosmicRayEvent {
        public float originTime { get; set; }
        public Vector3 originLocalPosition { get; set; }
        public CosmicRayEvent(float t, Vector3 p) {
            originTime = t;
            originLocalPosition = p;
        }
    }
}