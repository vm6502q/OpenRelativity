namespace Qrack
{

    public class RealTimeQasmInstruction
    {
        public float Time { get; set; }
        public bool IsRelativeTime { get; set; }

        public delegate void QuantumProgramUpdate(RealTimeQasmProgram realTimeQasmProgram);

        public QuantumProgramUpdate quantumProgramUpdate;
    }

}
