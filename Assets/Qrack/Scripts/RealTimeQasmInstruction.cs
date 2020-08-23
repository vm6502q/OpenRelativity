namespace Qrack
{
    public enum Pauli
    {
        PauliI = 0,
        PauliX = 1,
        PauliY = 3,
        PauliZ = 2
    }

    public class RealTimeQasmInstruction
    {
        public float Time { get; set; }
        public bool IsRelativeTime { get; set; }

        public delegate void QuantumProgramUpdate(RealTimeQasmProgram realTimeQasmProgram);

        public QuantumProgramUpdate quantumProgramUpdate;
    }
}
