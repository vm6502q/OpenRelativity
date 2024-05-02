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
        public double latticeSquareMeterJPerK = 8200.0;
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
        // 2 to the negative power of unshielded frequency
        public double shieldingFactor = 1.0;
        // Number of reflection bounces
        public int reflectionBounces = 2;
        // Qubits potentially affected by this substrate
        public List<Qrack.QuantumSystem> myQubits;

        // Planck's constant in J / Hz
        protected double planck = 6.62607015e-34;
        // Stefan-Boltzmann constant in W m^-2 K^-4
        protected double stefanBoltzmann = 5.67037321e-8;
        // Step of Riemann sum for event frequency
        protected float logEvStep = 0.025f;

        protected SphereCollider myCollider;
        protected Objects.RelativisticObject myRO;
        protected List<CosmicRayEvent> myCosmicRayEvents;

        // Approximate the spectrum at the edge of earth's atmosphere,
        // and choose the additive constant in the exponent so
        // 10^11 eV occurs ~1Hz/m^2
        protected float HzPerSquareMeter(float logEv) {
            return Mathf.Pow(10, (44 - 4 * logEv) / 3) * Mathf.Pow(2, (float)(-shieldingFactor));
        }

        protected float JoulesPerEvent(float logEv) {
            // Joules per log(eV)
            float j = Mathf.Pow(10.0f, logEv) * 1.60218e-19f;
            double area = Mathf.PI * (latticeParameterMeters * latticeParameterMeters);
            return (float)Melt(j, area);
        }

        protected double Melt(double j, double area) {
            double temp = j * area / latticeSquareMeterJPerK;
            double height = transform.lossyScale.y;
            if (temp >= latticeBoilingPointK) {
                // Since this area is vaporized, the heat is immediately permanently lost to the cryogenic vacuum.
                j -= (latticeHeatOfFusionJPerMol + latticeHeatOfVaporizationJPerMol) * latticeKgPerCubedMeter * area * height / latticeKgPerMol;
            } else if (temp >= latticeMeltingPointK) {
                // Since this area is melted, energy is deposited from the wave front into the heat of fusion (and then eventually refreezes).
                // Upon refreezing, the energy lost from the wave front has been given up to entropy and is no longer coherent with the original wave front.
                j -= latticeHeatOfFusionJPerMol * latticeKgPerCubedMeter * area * height / latticeKgPerMol;
            }
            return j;
        }

        // Start is called before the first frame update
        void Start()
        {
            myRO = GetComponent<Objects.RelativisticObject>();
            myCollider = GetComponent<SphereCollider>();
            myCosmicRayEvents = new List<CosmicRayEvent>();
        }

        // Update is called once per frame
        void Update()
        {
            float height = transform.localScale.y;
            float localTime = myRO.GetLocalTime();
            float localDeltaTime = myRO.localDeltaTime;
            Dictionary<Qrack.QuantumSystem, double> myIntensities = new Dictionary<Qrack.QuantumSystem, double>();
            List<CosmicRayEvent> nMyCosmicRayEvents = new List<CosmicRayEvent>();
            for (int i = 0; i < myCosmicRayEvents.Count; ++i) {
                CosmicRayEvent evnt = myCosmicRayEvents[i];
                double time = (localTime - evnt.originTime);
                Vector3 pos = transform.TransformPoint(evnt.originLocalPosition);
                double tRadius = time * latticeSoundMetersPerSecond;
                double tArea = Mathf.PI * tRadius * tRadius;
                double oRadius = (time - localDeltaTime) * latticeSoundMetersPerSecond;
                double oArea = Mathf.PI * oRadius * oRadius;
                double wRadius = tRadius - latticeParameterMeters;
                double wArea = Mathf.PI * (tRadius * tRadius - wRadius * wRadius);
                double temp = (evnt.joules * wArea) / latticeSquareMeterJPerK;
                evnt.joules -= stefanBoltzmann * (2 * wArea) * localDeltaTime * temp * temp * temp * temp;
                evnt.joules = Melt(evnt.joules, tArea - oArea);
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
                    if ((tRadius >= dist) && (oRadius < dist)) {
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

            Vector3 localLwh = transform.localScale;
            float filmRadius = Mathf.Sqrt(localLwh.x * localLwh.x + localLwh.z * localLwh.z);
            Vector3 lwh = transform.lossyScale;
            float filmSurfaceArea = Mathf.PI * (lwh.x * lwh.z);
            // This should approach continuous sampling, but we're doing it discretely.
            for (float logEv = 9.0f; logEv < 15.0f; logEv = logEv + logEvStep) {
                // Riemann sum step:
                double prob = filmSurfaceArea * localDeltaTime * logEvStep * (HzPerSquareMeter(logEv + logEvStep / 2) + HzPerSquareMeter(logEv - logEvStep / 2)) / 2;
                while ((prob > 1) || ((prob > 0) && prob >= Random.Range(0.0f, 1.0f))) {
                    // Cosmic ray event occurs.
                    // Pick a (uniformly) random point on the surface.
                    float r = Random.Range(0.0f, filmRadius);
                    float p = Random.Range(0.0f, 2 * Mathf.PI);
                    Vector3 pos = new Vector3(r * Mathf.Cos(p), 0.0f, r * Mathf.Sin(p));
                    double e = JoulesPerEvent(logEv);
                    double t = localTime + latticeParameterMeters / latticeSoundMetersPerSecond;
                    myCosmicRayEvents.Add(new CosmicRayEvent(e, t, pos));
                    prob = prob - 1;

                    if (reflectionBounces <= 0) {
                        continue;
                    }

                    Vector3 closest = myCollider.ClosestPoint(transform.TransformPoint(pos));
                    Vector3 f = closest.normalized;
                    Vector3 opposite = (2 * filmRadius - closest.magnitude) * f;
                    closest = 2 * closest;
                    opposite = 2 * opposite;
                    f = 2 * filmRadius * f;

                    for (int bounceCount = 0; bounceCount < reflectionBounces; ++bounceCount) {
                        myCosmicRayEvents.Add(new CosmicRayEvent(e, t, transform.InverseTransformPoint(2 * closest)));
                        myCosmicRayEvents.Add(new CosmicRayEvent(e, t, transform.InverseTransformPoint(2 * opposite)));

                        closest += f;
                        opposite -= f;
                    }
                }
            }
        }
    }
}
