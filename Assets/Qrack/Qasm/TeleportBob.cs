namespace Qrack
{
    public class TeleportBob : RealTimeQasmProgram
    {
        public TeleportAlice Alice;

        protected override void StartProgram()
        {

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                IsRelativeTime = true,
                Time = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    QuantumSystem qs = x.QuantumSystem;

                    if (Alice.MeasurmentResults[0])
                    {
                        qs.Z(0);
                    }

                    if (Alice.MeasurmentResults[1])
                    {
                        qs.X(0);
                    }
                }
            });

            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                IsRelativeTime = true,
                Time = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    Alice.InstructionIndex = 0;
                    Alice.gameObject.SetActive(true);
                }
            });
        }

    }
}
