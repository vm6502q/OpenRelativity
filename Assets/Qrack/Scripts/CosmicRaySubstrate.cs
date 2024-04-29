using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRelativity {
    public class CosmicRaySubstrate : RelativisticBehavior
    {
        // Frequency per area of cosmic ray events
        public float HzPerSquareMeter = 84.3f;
        // Joules per cosmic ray impact
        public float JoulesPerEvent = 3.66f;
        // Lattice parameter of substrate crystal
        public float latticeMeters = 5.43e-10f;
        // Speed of sound in substrate crystal
        public float latticeRapidityOfSound = 8433.0f;
        // Coupling between flux and probability of noise (inverse of defect energy)
        public float fluxCouplingConstant = 6.022e23f / 293000;

        public List<Qrack.QuantumSystem> myQubits;

        protected List<CosmicRayEvent> myCosmicRayEvents;

        // Start is called before the first frame update
        void Start()
        {
            myCosmicRayEvents = new List<CosmicRayEvent>();
        }

        // Update is called once per frame
        void Update()
        {
            Dictionary<Qrack.QuantumSystem, float> myIntensities = new Dictionary<Qrack.QuantumSystem, float>();
            List<CosmicRayEvent> nMyCosmicRayEvents = new List<CosmicRayEvent>();
            for (int i = 0; i < myCosmicRayEvents.Count; ++i) {
                CosmicRayEvent evnt = myCosmicRayEvents[i];
                float minRadius = (state.TotalTimeWorld - (evnt.originTime + state.DeltaTimeWorld)) * latticeRapidityOfSound;
                float maxRadius = (state.TotalTimeWorld - evnt.originTime) * latticeRapidityOfSound;
                bool isDone = true;
                for (int j = 0; j < myQubits.Count; ++j) {
                    Qrack.QuantumSystem qubit = myQubits[j];
                    Objects.RelativisticObject qubitRO = qubit.GetComponent<Objects.RelativisticObject>();
                    float dist = (qubitRO.piw - transform.TransformPoint(evnt.originLocalPosition)).magnitude;
                    if ((minRadius < dist) && (maxRadius >= dist)) {
                        // Spreads out as if in a topological system, proportional to the perimeter.
                        float intensity = JoulesPerEvent / (2 * Mathf.PI * dist);
                        if (myIntensities.ContainsKey(qubit)) {
                            myIntensities[qubit] += intensity;
                        } else {
                            myIntensities[qubit] = intensity;
                        }
                    }
                    if (dist >= minRadius) {
                        isDone = false;
                    }
                }
                if (!isDone) {
                    nMyCosmicRayEvents.Add(evnt);
                }
            }
            myCosmicRayEvents = nMyCosmicRayEvents;
            for (int i = 0; i < myQubits.Count; ++i) {
                Qrack.QuantumSystem qubit = myQubits[i];
                if (!myIntensities.ContainsKey(qubit)) {
                    continue;
                }
                float p = myIntensities[qubit] * fluxCouplingConstant;
                if (p >= Random.Range(0, 1)) {
                    // Bit-flip event occurs
                    qubit.X(0);
                }
                if (p >= Random.Range(0, 1)) {
                    // Bit-flip event occurs
                    qubit.Z(0);
                }
            }

            Vector3 lwh = transform.localScale;
            float surfaceArea = Mathf.PI * (lwh.x * lwh.z);
            float prob = HzPerSquareMeter * surfaceArea * state.DeltaTimeWorld;
            if (prob >= Random.Range(0, 1)) {
                // Cosmic ray event occurs
                // Pick a (uniformly) random point on the surface.
                Vector3 pos = new Vector3(Random.Range(0.0f, 1.0f), 0.0f, Random.Range(0.0f, 1.0f));
                myCosmicRayEvents.Add(new CosmicRayEvent(state.TotalTimeWorld, pos));
            }
        }
    }
}