namespace OnionPeeler.Tomtel
{
    public class InstructionRegister
    {
        public Instruction Instruction;

        public uint Operand;

        public byte SourceRegister; // 0: Immediate, 1..6: Direct, 7: Indirect
        public byte DestinationRegister;
    }
}
