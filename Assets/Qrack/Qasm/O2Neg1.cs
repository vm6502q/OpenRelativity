using System;

namespace Qrack
{
    public class O2Neg1 : RealTimeQasmProgram
    {
        // Prepare a Bell pair for Alice and Bob to share
        protected override void StartProgram()
        {
            const double aParam = 1e-4;
            double e0 = Math.Sqrt(1.0 - aParam * aParam);

            double[] hamiltonian = {
                // Hermitian 2x2 complex array 
                e0, 0.0, -aParam, 0.0,
                -aParam, 0.0, e0, 0.0,
            };

            TimeEvolveOpHeader teo = new TimeEvolveOpHeader(0, null);

            TimeEvolveOpHeader[] timeEvolveOpHeaders = new TimeEvolveOpHeader[1] { teo };

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                // Iterate every frame
                DeltaTime = 0.0f,
                quantumProgramUpdate = (x, deltaTime) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    qs.TimeEvolve(deltaTime, timeEvolveOpHeaders, hamiltonian);
                }
            });
        }

    }
}
