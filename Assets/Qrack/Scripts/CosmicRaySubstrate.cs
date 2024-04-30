using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRelativity {
    public class CosmicRaySubstrate : RelativisticBehavior
    {
        // Lattice parameter of substrate crystal
        public double latticeMeters = 5.43e-10;
        // Speed of sound in substrate crystal
        public double latticeRapidityOfSound = 8433.0;
        // Coupling between flux and probability of noise (inverse of energy level separatation)
        public double fluxCouplingConstant = 6.2415e22;
        // 2 the negative power of unshielded frequency
        public double shieldingFactor = 10.0;
        // Heat capacity of thin film
        public double specificHeatPerSquareMeter = 8000;
        // Qubits potentially affected by this substrat
        public List<Qrack.QuantumSystem> myQubits;

        // Stefan-Boltzmann constant in W m^-2 K^-4
        protected double stefanBoltzmann = 5.67037321e-8;
        // Step of Riemann sum for event frequency
        protected float logEvStep = 0.1f;

        protected List<CosmicRayEvent> myCosmicRayEvents;

        // Approximate the spectrum at the edge of earth's atmosphere,
        // and choose the additive constant in the exponent so
        // 10^11 eV occurs ~1Hz/m^2
        protected float HzPerSquareMeter(float logEv) {
            return Mathf.Pow(10, (44 - 4 * logEv) / 3) * Mathf.Pow(2, (float)(-shieldingFactor));
        }

        protected float JoulesPerEvent(float logEv) {
            return Mathf.Pow(10.0f, logEv) * 1.60218e-19f;
        }

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
                double time = (state.TotalTimeWorld - evnt.originTime);
                double minRadius = (time - state.DeltaTimeWorld) * latticeRapidityOfSound;
                double maxRadius = time * latticeRapidityOfSound;
                double area = Mathf.PI * maxRadius * maxRadius;
                double temp = (evnt.joules * area) / specificHeatPerSquareMeter;
                evnt.joules = evnt.joules - stefanBoltzmann * 2 * area * state.DeltaTimeWorld * temp * temp * temp * temp;
                bool isDone = true;
                for (int j = 0; j < myQubits.Count; ++j) {
                    Qrack.QuantumSystem qubit = myQubits[j];
                    Objects.RelativisticObject qubitRO = qubit.GetComponent<Objects.RelativisticObject>();
                    double dist = (qubitRO.piw - transform.TransformPoint(evnt.originLocalPosition)).magnitude;
                    // Spreads out as if in a topological system, proportional to the perimeter.
                    double intensity = evnt.joules / (2 * Mathf.PI * dist);
                    if (intensity <= 0) {
                        continue;
                    }
                    if ((minRadius < dist) && (maxRadius >= dist)) {
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
            // This should approach continuous sampling, but we're doing it discretely.
            for (float logEv = 10; logEv < 15; logEv = logEv + logEvStep) {
                // Riemann sum step:
                double prob = logEvStep * (HzPerSquareMeter(logEv + logEvStep / 2) + HzPerSquareMeter(logEv - logEvStep / 2)) * surfaceArea * state.DeltaTimeWorld / 2;
                while ((prob > 1) || ((prob > 0) && prob >= Random.Range(0.0f, 1.0f))) {
                    // Cosmic ray event occurs
                    // Pick a (uniformly) random point on the surface.
                    float r = Random.Range(0.0f, lwh.magnitude);
                    float p = Random.Range(0.0f, 2 * Mathf.PI);
                    Vector3 pos = new Vector3(r * Mathf.Cos(p), 0.0f, r * Mathf.Sin(p));
                    myCosmicRayEvents.Add(new CosmicRayEvent(JoulesPerEvent(logEv), state.TotalTimeWorld, pos));
                    prob = prob - 1;
                }
            }
        }
    }
}