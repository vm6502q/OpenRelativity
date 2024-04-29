using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRelativity {
    public class CosmicRaySubstrate : RelativisticBehavior
    {
        // Frequency per area of cosmic ray events
        public double HzPerSquareMeter = 84.3;
        // Joules per cosmic ray impact
        public double JoulesPerEvent = 3.66;
        // Lattice parameter of substrate crystal
        public double latticeMeters = 5.43e-10;
        // Speed of sound in substrate crystal
        public double latticeRapidityOfSound = 8433.0;
        // Coupling between flux and probability of noise (inverse of defect energy)
        public double fluxCouplingConstant = 6.022e23 / 293000;

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
            Dictionary<Qrack.QuantumSystem, double> myIntensities = new Dictionary<Qrack.QuantumSystem, double>();
            List<CosmicRayEvent> nMyCosmicRayEvents = new List<CosmicRayEvent>();
            for (int i = 0; i < myCosmicRayEvents.Count; ++i) {
                CosmicRayEvent evnt = myCosmicRayEvents[i];
                double minRadius = (state.TotalTimeWorld - (evnt.originTime + state.DeltaTimeWorld)) * latticeRapidityOfSound;
                double maxRadius = (state.TotalTimeWorld - evnt.originTime) * latticeRapidityOfSound;
                bool isDone = true;
                for (int j = 0; j < myQubits.Count; ++j) {
                    Qrack.QuantumSystem qubit = myQubits[j];
                    Objects.RelativisticObject qubitRO = qubit.GetComponent<Objects.RelativisticObject>();
                    double dist = (qubitRO.piw - transform.TransformPoint(evnt.originLocalPosition)).magnitude;
                    if ((minRadius < dist) && (maxRadius >= dist)) {
                        // Spreads out as if in a topological system, proportional to the perimeter.
                        double intensity = JoulesPerEvent / (2 * Mathf.PI * dist);
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
                double p = myIntensities[qubit] * fluxCouplingConstant;
                if (p >= Random.Range(0.0f, 1.0f)) {
                    // Bit-flip event occurs
                    qubit.X(0);
                }
                if (p >= Random.Range(0.0f, 1.0f)) {
                    // Bit-flip event occurs
                    qubit.Z(0);
                }
            }

            Vector3 lwh = transform.localScale;
            double surfaceArea = Mathf.PI * (lwh.x * lwh.z);
            double prob = HzPerSquareMeter * surfaceArea * state.DeltaTimeWorld;
            if (prob >= Random.Range(0, 1)) {
                // Cosmic ray event occurs
                // Pick a (uniformly) random point on the surface.
                Vector3 pos = new Vector3(Random.Range(0.0f, 1.0f), 0.0f, Random.Range(0.0f, 1.0f));
                myCosmicRayEvents.Add(new CosmicRayEvent(state.TotalTimeWorld, pos));
            }
        }
    }
}