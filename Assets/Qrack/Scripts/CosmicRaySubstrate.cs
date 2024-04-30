using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OpenRelativity {
    public class CosmicRaySubstrate : RelativisticBehavior
    {
        // Substrate molar mass
        public double latticeKgPerMol = 2.80855e-2;
        // Substrate density (kg / m^3)
        public double latticeKgPerCubedMeter = 2329.085;
        // Heat capacity of thin film (J / K)
        public double latticeSquareMeterJoulesPerKelvin = 8200.0;
        // Melting point of substrate (K)
        public double latticeMeltingPointK = 1687.0;
        // Boiling point of substrate (K)
        public double latticeBoilingPointK = 3538.0;
        // Heat of Fusion of substrate (J / mol)
        public double latticeHeatOfFusionJPerMol = 50210.0;
        // Heat of Vaporization of substrate (J / mol)
        public double latticeHeatOfVaporizationJPerMol = 383000.0;
        // Speed of sound in substrate crystal
        public double latticeSoundMetersPerSecond = 8433.0;
        // Lattice parameter of substrate crystal
        public double latticeParameterMeters = 5.43e-10;
        // Coupling between flux and probability of noise (inverse of energy level separatation)
        public double fluxCouplingConstant = 5e22;
        // 2 the negative power of unshielded frequency
        public double shieldingFactor = 4.0;
        // Qubits potentially affected by this substrat
        public List<Qrack.QuantumSystem> myQubits;

        // Planck's constant in J / Hz
        protected double planck = 6.62607015e-34;
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
            double maxPhononE = planck * latticeSoundMetersPerSecond / latticeParameterMeters;
            Vector3 lwh = transform.lossyScale;
            double filmSurfaceArea = Mathf.PI * (lwh.x * lwh.z);
            Dictionary<Qrack.QuantumSystem, double> myIntensities = new Dictionary<Qrack.QuantumSystem, double>();
            List<CosmicRayEvent> nMyCosmicRayEvents = new List<CosmicRayEvent>();
            for (int i = 0; i < myCosmicRayEvents.Count; ++i) {
                CosmicRayEvent evnt = myCosmicRayEvents[i];
                double time = (state.TotalTimeWorld - evnt.originTime);
                Vector3 pos = transform.TransformPoint(evnt.originLocalPosition);
                double radius = time * latticeSoundMetersPerSecond;
                double area = Mathf.PI * radius * radius;
                if (area > filmSurfaceArea) {
                    // We don't simulate bouncing off the film boundaries,
                    // but this is the limit of uniform distribution,
                    // before radiative wicking.
                    area = filmSurfaceArea;
                }
                double oRadius = (time - state.DeltaTimeWorld) * latticeSoundMetersPerSecond;
                double oArea = Mathf.PI * oRadius * oRadius;
                if (oArea > filmSurfaceArea) {
                    oArea = filmSurfaceArea;
                }
                double temp = (evnt.joules * area) / latticeSquareMeterJoulesPerKelvin;
                evnt.joules -= stefanBoltzmann * (2 * area) * state.DeltaTimeWorld * temp * temp * temp * temp;
                if (temp > latticeBoilingPointK) {
                    // Since this area is vaporized, the heat is immediately permanently lost to the cryogenic vacuum.
                    evnt.joules -= (latticeHeatOfFusionJPerMol + latticeHeatOfVaporizationJPerMol) * latticeKgPerCubedMeter * (area - oArea) * lwh.y / latticeKgPerMol;
                } else if (temp > latticeMeltingPointK) {
                    // Since this area is melted, energy is deposited from the wave front into the heat of fusion (and then eventually refreezes).
                    evnt.joules -= latticeHeatOfFusionJPerMol * latticeKgPerCubedMeter * (area - oArea) * lwh.y / latticeKgPerMol;
                }
                bool isDone = true;
                for (int j = 0; j < myQubits.Count; ++j) {
                    Qrack.QuantumSystem qubit = myQubits[j];
                    Objects.RelativisticObject qubitRO = qubit.GetComponent<Objects.RelativisticObject>();
                    double dist = (qubitRO.piw - pos).magnitude;
                    // Spreads out as if in a topological system, proportional to the perimeter.
                    double intensity = evnt.joules / (2 * Mathf.PI * dist);
                    if (intensity <= 0) {
                        continue;
                    }
                    isDone = false;
                    if ((radius >= dist) && (oRadius < dist)) {
                        if (myIntensities.ContainsKey(qubit)) {
                            myIntensities[qubit] += intensity;
                        } else {
                            myIntensities[qubit] = intensity;
                        }
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

            // This should approach continuous sampling, but we're doing it discretely.
            for (float logEv = 10; logEv < 15; logEv = logEv + logEvStep) {
                // Riemann sum step:
                double prob = filmSurfaceArea * state.DeltaTimeWorld * logEvStep * (HzPerSquareMeter(logEv + logEvStep / 2) + HzPerSquareMeter(logEv - logEvStep / 2)) / 2;
                while ((prob > 1) || ((prob > 0) && prob >= Random.Range(0.0f, 1.0f))) {
                    // Cosmic ray event occurs
                    // Pick a (uniformly) random point on the surface.
                    float r = Random.Range(0.0f, lwh.x * lwh.x + lwh.z * lwh.z);
                    float p = Random.Range(0.0f, 2 * Mathf.PI);
                    Vector3 pos = new Vector3(r * Mathf.Cos(p), 0.0f, r * Mathf.Sin(p));
                    myCosmicRayEvents.Add(new CosmicRayEvent(JoulesPerEvent(logEv), state.TotalTimeWorld, pos));
                    prob = prob - 1;
                }
            }
        }
    }
}