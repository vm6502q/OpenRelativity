﻿using UnityEngine;

namespace OpenRelativity.ConformalMaps
{
    public class Kerr : Schwarzschild
    {
        public float spinMomentum;
        public Vector3 spinAxis = Vector3.up;

        public float GetOmega(Vector3 piw)
        {
            Quaternion rot = Quaternion.FromToRotation(spinAxis, Vector3.up);
            piw = rot * piw;
            float rSqr = piw.sqrMagnitude;

            // Radius:
            float r = Mathf.Sqrt(rSqr);
            // Inclination:
            float inc = Mathf.Acos(piw.z / r);
            // Azimuth:
            float azi = Mathf.Atan2(piw.y, piw.x);
            // Time: piw.w

            float a = spinMomentum / (radius * state.planckMass / state.planckLength);
            float aSqr = a * a;
            float cosTheta = Mathf.Cos(azi);
            float sigma = rSqr + aSqr * cosTheta * cosTheta;

            float sinPhi = Mathf.Cos(inc);
            float omega = (radius * r * a * state.SpeedOfLight) / (sigma * (rSqr + aSqr) + radius * r * aSqr * sinPhi * sinPhi);

            return omega;
        }

        override public Vector4 ComoveOptical(float properTDiff, Vector3 piw)
        {
            Quaternion rot = Quaternion.FromToRotation(spinAxis, Vector3.up);
            piw = rot * piw;

            float omega = GetOmega(piw);

            float frameDragAngle = omega * properTDiff;
            Quaternion frameDragRot = Quaternion.Euler(0, frameDragAngle, 0);

            piw = frameDragRot * piw;
            piw = Quaternion.Inverse(rot) * piw;

            return base.ComoveOptical(properTDiff, piw);
        }

        override public Vector3 GetRindlerAcceleration(Vector3 piw)
        {
            Quaternion rot = Quaternion.FromToRotation(spinAxis, Vector3.up);
            Vector3 lpiw = rot * piw;

            float omega = GetOmega(lpiw);
            Vector3 frameDragAccel = (omega * omega / lpiw.magnitude) * spinAxis;

            return frameDragAccel + base.GetRindlerAcceleration(piw);
        }

        override public void Update()
        {
            EnforceHorizon();

            if (radius <= 0 || !doEvaporate || state.isMovementFrozen)
            {
                return;
            }

            float deltaR = deltaRadius;

            radius += deltaR;

            if (radius < 0)
            {
                radius = 0;
            }

            if (spinMomentum <= 0)
            {
                spinMomentum = 0;
                return;
            }

            // These happen to be equal:
            // float constRatio = state.planckAngularMomentum / state.planckLength;
            float constRatio = state.planckMomentum;

            float extremalFrac = spinMomentum / (radius * constRatio);

            spinMomentum += extremalFrac * deltaRadius * constRatio;
        }
    }
}