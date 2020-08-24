using System;
using System.IO;
using System.Text;

namespace OnionPeeler.Tomtel
{
    public class Cpu
    {
        public const byte IMMEDIATE_MODE = 0b_000;
        public const byte INDIRECT_MODE = 0b_111;

        public const byte MV_INSTRUCTION_MASK = 0b_11000000;

        public InstructionRegister InstructionRegister { get; private set; }

        public Registers Registers { get; private set; }

        public byte[] Memory { get; private set; }

        public MemoryStream Output { get; private set; }

        public bool IsHalted { get; private set; }

        private BinaryWriter OutputWriter { get; set; }

        public Cpu(byte[] startupMemory)
        {
            InstructionRegister = new InstructionRegister();
            Registers = new Registers();
            Memory = startupMemory;

            Output = new MemoryStream(); // ToDo: Dispose
            OutputWriter = new BinaryWriter(Output, Encoding.ASCII); // ToDo: Dispose
        }

        public bool Run(int maxSteps = 1000000)
        {
            IsHalted = false;

            var stepCount = 0;
            while (!IsHalted && stepCount < maxSteps)
            {
                Step();
                stepCount++;
            }

            return IsHalted;
        }

        public void Step()
        {
            // Load Instruction Register from memory (Program Counter)
            Fetch();

            // Execute instruction from Instruction Register
            Execute();
        }

        private void Fetch()
        {
            const byte DESTINATION_MASK = 0b_00111000;
            const byte SOURCE_MASK = 0b_00000111;

            var opCode = ReadByteFromProgramCounterAndAdvance();
            InstructionRegister.Instruction = InstructionFromOpCode(opCode);

            switch (InstructionRegister.Instruction)
            {
                case Instruction.Halt:
                case Instruction.Out:
                case Instruction.Cmp:
                case Instruction.Add:
                case Instruction.Sub:
                case Instruction.Xor:
                    // No operand
                    break;

                case Instruction.Aptr:
                    InstructionRegister.Operand = ReadByteFromProgramCounterAndAdvance();
                    break;

                case Instruction.Jez:
                case Instruction.Jnz:
                    InstructionRegister.Operand = ReadIntFromProgramCounterAndAdvance();
                    break;

                case Instruction.Mv:
                case Instruction.Mv32:
                    InstructionRegister.SourceRegister = (byte)(opCode & SOURCE_MASK);
                    InstructionRegister.DestinationRegister = (byte)((opCode & DESTINATION_MASK) >> 3);

                    InstructionRegister.Operand = InstructionRegister.Instruction == Instruction.Mv
                        ? /* MV */ ReadByteFromPseudoRegister(InstructionRegister.SourceRegister)
                        : /* MV32 */ ReadIntFromPseudoRegister(InstructionRegister.SourceRegister);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown OpCode: {opCode}");
            }
        }

        private void Execute()
        {
            switch (InstructionRegister.Instruction)
            {
                case Instruction.Halt:
                    IsHalted = true;
                    break;

                case Instruction.Out:
                    OutputWriter.Write(Registers.A);
                    break;

                case Instruction.Jez:
                    if (Registers.F == 0)
                    {
                        Registers.Pc = InstructionRegister.Operand;
                    }
                    break;

                case Instruction.Jnz:
                    if (Registers.F != 0)
                    {
                        Registers.Pc = InstructionRegister.Operand;
                    }
                    break;

                case Instruction.Mv:
                    WriteByteToPseudoRegister(InstructionRegister.DestinationRegister, (byte)InstructionRegister.Operand);
                    break;

                case Instruction.Mv32:
                    WriteIntToPseudoRegister(InstructionRegister.DestinationRegister, InstructionRegister.Operand);
                    break;

                case Instruction.Add:
                    Registers.A += Registers.B;
                    break;

                case Instruction.Cmp:
                    Registers.F = (byte)(Registers.A == Registers.B ? 0 : 1);
                    break;

                case Instruction.Sub:
                    Registers.A -= Registers.B;
                    break;

                case Instruction.Xor:
                    Registers.A ^= Registers.B;
                    break;

                case Instruction.Aptr:
                    Registers.Ptr += (byte)InstructionRegister.Operand;
                    break;
            }
        }

        private Instruction InstructionFromOpCode(byte opCode) => (opCode & MV_INSTRUCTION_MASK) switch
        {
            (byte)Instruction.Mv => Instruction.Mv,
            (byte)Instruction.Mv32 => Instruction.Mv32,
            _ => (Instruction)opCode
        };

        private byte ReadByteFromProgramCounterAndAdvance() => Memory[Registers.Pc++];

        private uint ReadIntFromProgramCounterAndAdvance() => (uint)(
            ReadByteFromProgramCounterAndAdvance() |
            ReadByteFromProgramCounterAndAdvance() << 8 |
            ReadByteFromProgramCounterAndAdvance() << 16 |
            ReadByteFromProgramCounterAndAdvance() << 24);

        private byte ReadByteFromPseudoRegister(byte pseudoRegister) => pseudoRegister switch
        {
            IMMEDIATE_MODE => ReadByteFromProgramCounterAndAdvance(),

            1 => Registers.A,
            2 => Registers.B,
            3 => Registers.C,
            4 => Registers.D,
            5 => Registers.E,
            6 => Registers.F,

            INDIRECT_MODE => Memory[Registers.Ptr + Registers.C],

            _ => throw new InvalidOperationException($"Invalid pseudoregister for read byte: {pseudoRegister}")
        };

        private uint ReadIntFromPseudoRegister(byte pseudoRegister) => pseudoRegister switch
        {
            IMMEDIATE_MODE => ReadIntFromProgramCounterAndAdvance(),

            1 => Registers.La,
            2 => Registers.Lb,
            3 => Registers.Lc,
            4 => Registers.Lc,
            5 => Registers.Ptr,
            6 => Registers.Pc,

            _ => throw new InvalidOperationException($"Invalid pseudoregister for read int: {pseudoRegister}")
        };

        private void WriteByteToPseudoRegister(byte pseudoRegister, byte value)
        {
            switch (pseudoRegister)
            {
                case 1: Registers.A = value; break;
                case 2: Registers.B = value; break;
                case 3: Registers.C = value; break;
                case 4: Registers.D = value; break;
                case 5: Registers.E = value; break;
                case 6: Registers.F = value; break;

                case INDIRECT_MODE:
                    Memory[Registers.Ptr + Registers.C] = value;
                    break;

                default:
                    throw new InvalidOperationException($"Invalid pseudoregister for write byte: {pseudoRegister}");
            }
        }

        private void WriteIntToPseudoRegister(byte pseudoRegister, uint value)
        {
            switch (pseudoRegister)
            {
                case 1: Registers.La = value; break;
                case 2: Registers.Lb = value; break;
                case 3: Registers.Lc = value; break;
                case 4: Registers.Ld = value; break;
                case 5: Registers.Ptr = value; break;
                case 6: Registers.Pc = value; break;

                default:
                    throw new InvalidOperationException($"Invalid pseudoregister for write int: {pseudoRegister}");
            }
        }
    }
}
