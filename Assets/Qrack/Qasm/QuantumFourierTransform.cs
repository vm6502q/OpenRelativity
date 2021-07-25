using System;
using System.Collections.Generic;

namespace Qrack
{
    public class QuantumFourierTransform : RealTimeQasmProgram
    {
        public OpenRelativity.GameState state;
        public OpenRelativity.ConformalMaps.Schwarzschild schwarzschild;

        public float layerTimeInterval = 0.5f;
        public int maxQubits = 28;

        protected class QftHistoryPoint
        {
            public float Time { get; set; }
            public float Radius { get; set; }
        }

        protected List<QftHistoryPoint> expectationFrames = new List<QftHistoryPoint>();
        protected List<uint> bits = new List<uint>();
        protected override void StartProgram()
        {
            float foldTime = layerTimeInterval;
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 0.0f,
                quantumProgramUpdate = (x, y) =>
                {
                    QuantumSystem qs = x.QuantumSystem;

                    Random rng = new Random();
                    double a1 = 2 * Math.PI * rng.NextDouble();
                    double a2 = 2 * Math.PI * rng.NextDouble();
                    double a3 = 2 * Math.PI * rng.NextDouble();
                    qs.U(0, a1, a2, a3);

                    qs.H(0);

                    bits.Add(0);

                    expectationFrames.Add(new QftHistoryPoint
                    {
                        Time = foldTime - qs.ClockOffset,
                        Radius = qs.PermutationExpectation(bits.ToArray())
                    });

                    if (qs.QubitCount < maxQubits) {
                        qs.QubitCount++;
                    }
                }
            });

            for (uint i = 1; i < maxQubits; i++)
            {
                foldTime = AddLayer(i, foldTime);
            }
        }

        private float AddLayer(uint i, float totTime)
        {
            float foldTime = layerTimeInterval * (float)Math.Pow(2, i);
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = foldTime - totTime,
                quantumProgramUpdate = (x, y) =>
                {
                    schwarzschild.doEvaporate = false;

                    QuantumSystem qs = x.QuantumSystem;

                    Random rng = new Random();
                    double a1 = 2 * Math.PI * rng.NextDouble();
                    double a2 = 2 * Math.PI * rng.NextDouble();
                    double a3 = 2 * Math.PI * rng.NextDouble();
                    qs.U(i, a1, a2, a3);

                    for (uint j = 0; j < i; j++)
                    {
                        uint[] c = new uint[1] { i };
                        uint t = (i - 1U) - j;
                        double lambda = 2 * Math.PI / Math.Pow(2.0, j);
                        qs.MCU(c, t, 0, 0, lambda);
                    }
                    qs.H(i);

                    bits.Add(bits[bits.Count - 1] + 1);
                    List<uint> expBits = bits;
                    expBits.Reverse();

                    expectationFrames.Add(new QftHistoryPoint
                    {
                        Time = foldTime - qs.ClockOffset,
                        Radius = qs.PermutationExpectation(expBits.ToArray())
                    });

                    if (qs.QubitCount < maxQubits)
                    {
                        qs.QubitCount++;
                    }
                }
            });

            return foldTime;
        }

        protected override void Update()
        {
            base.Update();

            while ((expectationFrames.Count > 1) && (expectationFrames[1].Time < state.TotalTimeWorld))
            {
                expectationFrames.RemoveAt(0);
            }

            if (expectationFrames.Count == 0)
            {
                return;
            }

            if (expectationFrames.Count == 1)
            {
                schwarzschild.radius = expectationFrames[0].Radius;
                return;
            }

            float r0 = expectationFrames[0].Radius;
            float t0 = expectationFrames[0].Time;
            float r1 = expectationFrames[1].Radius;
            float t1 = expectationFrames[1].Time;
            float t = state.TotalTimeWorld;

            schwarzschild.radius = r0 + ((t - t0) * (r1 - r0) / (t1 - t0));
        }
    }
}
