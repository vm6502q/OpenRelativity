using UnityEngine;

namespace OpenRelativity {
    public class CosmicRayEvent {
        public double joules { get; set; }
        public double originTime { get; set; }
        public Vector3 originLocalPosition { get; set; }
        public CosmicRayEvent(double e, double t, Vector3 p) {
            joules = e;
            originTime = t;
            originLocalPosition = p;
        }
    }
}