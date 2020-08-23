namespace Qrack
{
    public class TeleportBob : RealTimeQasmProgram
    {
        public TeleportEve Eve;

        public bool[] MeasurmentResults = new bool[2];

        protected override void StartProgram()
        {
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    QuantumSystem qs = x.QuantumSystem;

                    if (MeasurmentResults[0])
                    {
                        qs.Z(0);
                    }

                    if (MeasurmentResults[1])
                    {
                        qs.X(0);
                    }
                }
            });

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                DeltaTime = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    Eve.ResetProgram();
                    gameObject.SetActive(false);
                }
            });
        }

    }
}
