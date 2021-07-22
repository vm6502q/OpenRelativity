using System;
using System.Collections.Generic;

namespace Qrack
{
    public class QuantumFourierTransform : RealTimeQasmProgram
    {
        public OpenRelativity.GameState state;

        public float layerTimeInterval = 0.5f;
        public int maxQubits = 28;

        protected class QftHistoryPoint
        {
            public float Time { get; set; }
            public float Radius { get; set; }
        }

        protected List<QftHistoryPoint> expectationFrames = new List<QftHistoryPoint>();
        protected List<uint> bits = new List<uint>();

        // Prepare a Bell pair for Alice and Bob to share
        protected override void StartProgram()
        {
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 0.0f,
                quantumProgramUpdate = (x, y) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    qs.H(0);

                    bits.Add(0);

                    /*expectationFrames.Add(new QftHistoryPoint
                    {
                        Time = state.TotalTimeWorld,
                        Radius = qs.PermutationExpectation(bits.ToArray())
                    });*/
                }
            });

            for (uint i = 1; i < maxQubits; i++)
            {
                AddLayer(i);
            }
        }

        private void AddLayer(uint i)
        {
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = layerTimeInterval,
                quantumProgramUpdate = (x, y) =>
                {
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

                    /*expectationFrames.Add(new QftHistoryPoint
                    {
                        Time = state.TotalTimeWorld + qs.ClockOffset,
                        Radius = qs.PermutationExpectation(expBits.ToArray())
                    });*/
                }
            });
        }
    }
}
