namespace Qrack
{
    public class TeleportAlice : RealTimeQasmProgram
    {
        public TeleportBob Bob;

        protected override void StartProgram()
        {

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    qs.Rand(0);
                    qs.MCX(new uint[] { 0 }, 1);
                    qs.H(1);
                }
            });

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    Bob.MeasurmentResults[0] = qs.M(0);
                    Bob.MeasurmentResults[1] = qs.M(1);

                    Bob.ResetProgram();
                    gameObject.SetActive(false);
                }
            });
        }

    }
}
