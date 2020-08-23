namespace Qrack
{
    public class TeleportEve : RealTimeQasmProgram
    {
        public TeleportAlice Alice;

        // Prepare a Bell pair for Alice and Bob to share
        protected override void StartProgram()
        {
            ProgramInstructions.Add(new RealTimeQasmInstruction()
            {
                IsRelativeTime = true,
                Time = 1.0f,
                quantumProgramUpdate = (x) =>
                {
                    QuantumSystem qs = x.QuantumSystem;
                    qs.H(1);
                    qs.MCX(new uint[] { 0 }, 2);

                    Alice.InstructionIndex = 0;
                    Alice.gameObject.SetActive(true);
                }
            });
        }

    }
}