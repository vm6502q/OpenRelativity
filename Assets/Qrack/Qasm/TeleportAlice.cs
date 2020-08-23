namespace Qrack
{
    public class TeleportAlice : RealTimeQasmProgram
    {
        public TeleportBob Bob;

        public bool[] MeasurmentResults { get; set; }

        protected override void StartProgram()
        {
            MeasurmentResults = new bool[2];

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                IsRelativeTime = false,
                Time = 2.0f,
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
                IsRelativeTime = true,
                Time = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    MeasurmentResults[0] = qs.M(0);
                    MeasurmentResults[1] = qs.M(1);

                    Bob.InstructionIndex = 0;
                    Bob.gameObject.SetActive(true);
                }
            });
        }

    }
}
