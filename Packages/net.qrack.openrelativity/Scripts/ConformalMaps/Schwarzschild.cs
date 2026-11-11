using System;
using UnityEngine;

namespace OpenRelativity.ConformalMaps
{
    public class Schwarzschild : ConformalMap
    {
        public bool isExterior { get; set; }

        // The 2019 monopole emission law (dR/dt ~ 1/R) is kept as an opt-in toggle: as a radiation law it exceeds the
        // black-body bound for a body of the horizon's temperature (effective emissivity >> 1). Hawking evaporation is on.
        public bool doMonopoleEvaporate = false;
        public bool doHawkingEvaporate = true;
        public float schwarzschildRadius = 0.5f;

        protected System.Random rng = new System.Random();

        virtual public void SetEffectiveRadius(Vector3 piw)
        {
            //Intentionally left blank as a virtual interface.
        }

        virtual public void ResetSchwarschildRadius()
        {
            //Intentionally left blank as a virtual interface.
        }

        protected float fold
        {
            get
            {
                // Can we actually back-track to perfect 0 folds on the basis of exterior time?
                // We don't know exactly how long the evaporation will take, in the quantum limit.
                // If the black hole is "hairless," shouldn't this only depend on radius, rather than time?
                return (float)(Math.Log(schwarzschildRadius / state.planckLength) / Math.Log(2));
            }
        }

        virtual protected void Start()
        {
            float dist = state.playerTransform.position.magnitude;
            isExterior = (dist > schwarzschildRadius);
            if (!isExterior)
            {
                // From the exterior Schwarzschild perspective, the player's coordinate radius from origin (less than the
                // coordinate distance from origin to event horizon) corresponds with the interior time.
                state.playerTransform.position = state.SpeedOfLight * state.TotalTimeWorld * state.playerTransform.position.normalized;

                // Theoretically, "first-person" local time extends infinitely back into the past and forward into the future,
                // but the limit points for 0 Hubble sphere radius at either extreme might have a finite total time
                // between the limit points.
                state.TotalTimeWorld = dist / state.SpeedOfLight;
                state.TotalTimePlayer = state.TotalTimeWorld;
            }
        }

        override public Comovement ComoveOptical(float properTDiff, Vector3 piw, Quaternion riw)
        {
            // Assume that the spatial component is in world coordinates, and the time is a local time differential
            double tau = properTDiff;
            double rsCubeRoot = Math.Pow(schwarzschildRadius, 1 / 3.0);
            double r;
            double rho;
            if (isExterior)
            {
                r = piw.magnitude;
                rho = (2 * r * Math.Sqrt(r / rsCubeRoot)) / (3 * rsCubeRoot);
            } else {
                tau *= -1;
                // Interior convention (Start(), EnforceHorizon()): the timelike Schwarzschild coordinate r is the clock,
                // r = c * TotalTimeWorld, running 0 -> r_s. The Lemaitre coordinate must then be the SAME function of r
                // as in the exterior branch, rho = (2/3) r^(3/2) / r_s^(1/2), so that nR below returns r at tau = 0.
                // (Previously rho = c*T and r = (rho / (2 r_s^(1/3)))^(2/3), which is neither the inverse of the exterior
                // formula nor dimensionally consistent unless r_s = 1.)
                r = state.SpeedOfLight * state.TotalTimeWorld;
                rho = (2 * r * Math.Sqrt(r / rsCubeRoot)) / (3 * rsCubeRoot);
            }

            // Partial differential, finite difference approach:
            //double diffR = Mathf.Pow(2 * radius / (rho - tau), 1 / 3.0f);
            //r -= diffR;
            // Unless we have a really small and/or adaptive finite difference time step, the above approximation fails close to the event horizon.

            // We can try the integral form, instead, with a major caveat...
            double nR = Math.Pow(schwarzschildRadius * Math.Pow(3.0 / 2 * (rho - tau), 2), 1 / 3.0);
            double diffR = nR - r;
            // The equation we derive this closed-form integral from has many roots.
            // Some of these roots are not admissible without the existence of complex numbers.
            // Some are valid (real) when rho > tau, and some are valid when rho < tau.
            // (The above is real only when rho > tau, so rho better be greater than tau, in our inputs, at least.)
            // This neglects the change in "radius," the Schwarzschild radius, but we approximate that with a finite difference loop, in the FixedUpdate() method.

            // Remember that differential geometry gives us coordinate systems that are valid only over LOCAL regions, i.e. specifically NOT GLOBAL coordinate systems.
            // The coordinates are not "intrinsically" significant. "Intrinsic" properties, i.e. intrinsic curvature, an intrinsic property of the METRIC, is independent of coordinate systems.

            // All that said, the above should serve our purposes in the local region of interest.

            // The integral isn't as "nice" for time, and we approximate to lowest order:
            double diffT = Math.Log((schwarzschildRadius - r) / diffR);

            if (!isExterior)
            {
                double temp = diffT;
                diffT = diffR / state.SpeedOfLight;
                diffR = temp * state.SpeedOfLight;
            }

            Vector4 piw4 = piw + piw.normalized * (float)diffR;
            piw4.w = (float)diffT;

            return new Comovement
            {
                piw = piw4,
                riw = riw
            };
        }

        override public Vector3 GetRindlerAcceleration(Vector3 piw)
        {
            if (isExterior)
            {
                return schwarzschildRadius * SRelativityUtil.cSqrd / (2 * piw.sqrMagnitude) * piw.normalized;
            }

            return Vector3.zero;
        }

        public override Vector3 GetFreeFallVelocity(Vector3 piw)
        {
            if (!isExterior)
            {
                return Vector3.zero;
            }

            float r = piw.magnitude;

            return -state.SpeedOfLight * Mathf.Sqrt(schwarzschildRadius / r) * (piw / r);
        }

        public void EnforceHorizon()
        {
            // Interior time runs over the same span as the radius (divided by c), per Start(); compare like with like.
            // (Previously compared a time against a length, which froze at the wrong moment whenever c != 1.)
            float horizonTime = schwarzschildRadius / state.SpeedOfLight;
            if (!isExterior && (state.TotalTimeWorld >= horizonTime))
            {
                state.TotalTimeWorld = horizonTime;
                state.isMovementFrozen = true;
            }
        }

        virtual protected float deltaRadius
        {
            get
            {
                // This is speculative, but it can simply be turned off, in the editor.
                // This attempts to simulate black hole evaporation at a rate inversely proportional to Schwarzschild radius.
                // It's not properly Hawking radition, but this could be easily modified to approximate that instead.
                double r = SRelativityUtil.EffectiveRaditiativeRadius(schwarzschildRadius, state.gravityBackgroundPlanckTemperature);

                double diffR = 0;
                if (doMonopoleEvaporate) {
                    if (r > state.planckLength)
                    {
                        diffR = SRelativityUtil.SchwarzschildRadiusDecay(state.DeltaTimeWorld, r);
                    }
                    else
                    {
                        diffR = -state.DeltaTimeWorld * state.planckLength / (2 * state.planckTime);
                    }
                }

                if (doHawkingEvaporate) {
                    diffR += SRelativityUtil.HawkingSchwarzschildRadiusDecay(state.DeltaTimeWorld, r);
                }

                if (!isExterior)
                {
                    diffR = -diffR;
                }

                if ((schwarzschildRadius + diffR) < 0)
                {
                    diffR = -schwarzschildRadius;
                }

                return (float)diffR;
            }
        }

        virtual protected void Update()
        {
            EnforceHorizon();

            if (schwarzschildRadius <= 0)
            {
                schwarzschildRadius = 0;
                return;
            }

            if (state.isMovementFrozen || (!doMonopoleEvaporate && !doHawkingEvaporate))
            {
                return;
            }

            float deltaT = state.FixedDeltaTimeWorld;
            float powf = Mathf.Pow(2, fold);
            float deltaF = (float)((isExterior ? -deltaT : deltaT) / (state.planckTime * powf));
            float deltaR = powf * deltaF * (float)rng.NextDouble() / 2;
            float thermoDeltaR = deltaRadius;

            schwarzschildRadius += (isExterior != (deltaR > thermoDeltaR)) ? thermoDeltaR : deltaR;

            if (schwarzschildRadius < 0)
            {
                schwarzschildRadius = 0;
            }
        }
    }
}
