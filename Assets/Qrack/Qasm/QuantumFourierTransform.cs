using System;
using System.Collections.Generic;
using UnityEngine;

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

        protected void InitRandomQubit(QuantumSystem qs, uint i)
        {
            System.Random rng = new System.Random();
            double a1 = 2 * Math.PI * rng.NextDouble();
            double a2 = 2 * Math.PI * rng.NextDouble();
            double a3 = 2 * Math.PI * rng.NextDouble();
            qs.U(i, a1, a2, a3);
        }

        protected List<QftHistoryPoint> expectationFrames = new List<QftHistoryPoint>();
        protected List<uint> bits = new List<uint>();
        protected override void StartProgram()
        {
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 0.0f,
                quantumProgramUpdate = (x, y) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    InitRandomQubit(qs, 0);

                    qs.H(0);

                    bits.Add(0);

                    expectationFrames.Add(new QftHistoryPoint
                    {
                        Time = state.TotalTimeWorld,
                        Radius = qs.PermutationExpectation(bits.ToArray())
                    });

                    if (qs.QubitCount < maxQubits)
                    {
                        qs.QubitCount++;

                        InitRandomQubit(qs, 1);

                        uint[] c = new uint[1] { 1 };
                        uint t = 0;
                        double lambda = 2 * Math.PI;
                        qs.MCU(c, t, 0, 0, lambda);
                        qs.H(1);

                        bits.Add(1);
                        List<uint> expBits = bits;
                        expBits.Reverse();

                        expectationFrames.Add(new QftHistoryPoint
                        {
                            Time = state.TotalTimeWorld + layerTimeInterval,
                            Radius = qs.PermutationExpectation(expBits.ToArray())
                        });

                        if (qs.QubitCount < maxQubits)
                        {
                            qs.QubitCount++;
                        }
                    }
                }
            });

            for (uint i = 2; i < maxQubits; i++)
            {
                AddLayer(i);
            }
        }

        private void AddLayer(uint i)
        {
            float foldTime = layerTimeInterval * (float)Math.Pow(2, i - 1);
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = foldTime,
                quantumProgramUpdate = (x, y) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    InitRandomQubit(qs, i);

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
                        Time = state.TotalTimeWorld + foldTime,
                        Radius = qs.PermutationExpectation(expBits.ToArray())
                    });

                    if (qs.QubitCount < maxQubits)
                    {
                        qs.QubitCount++;
                    }
                }
            });
        }

        protected override void Update()
        {
            base.Update();

            if ((expectationFrames.Count == 0) || (state.DeltaTimeWorld <= 0))
            {
                return;
            }

            schwarzschild.EnforceHorizonEpsilon();

            int nextFrame = 1;

            while ((nextFrame < expectationFrames.Count) && (expectationFrames[nextFrame].Time < state.TotalTimeWorld))
            {
                nextFrame++;
            }

            if (nextFrame >= expectationFrames.Count)
            {
                schwarzschild.radius = expectationFrames[expectationFrames.Count - 1].Radius;
                schwarzschild.doEvaporate = true;
                return;
            }

            int lastFrame = nextFrame - 1;

            float r0 = expectationFrames[lastFrame].Radius;
            float t0 = expectationFrames[lastFrame].Time;
            float r1 = expectationFrames[nextFrame].Radius;
            float t1 = expectationFrames[nextFrame].Time;

            schwarzschild.radius -= state.DeltaTimeWorld * (r1 - r0) / (t1 - t0);
            schwarzschild.doEvaporate = false;
        }
    }
}
