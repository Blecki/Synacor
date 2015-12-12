using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synacor
{
    public class Emulator
    {
        public enum StatusCode
        {
            Okay = 0,
            Halt = 1,
            Error = 2,
        }

        public StatusCode Status { get; private set; }
        public String ErrorMessage;

        private const ushort MAX_VALUE = 32768;
        public Stack<ushort> Stack = new Stack<ushort>();
        public ushort[] Registers = new ushort[8];
        public ushort[] Memory = new ushort[MAX_VALUE];
        public ushort IP = 0;

        public void LoadMemory(byte[] Data)
        {
            var memoryPointer = 0;
            for (var dataPointer = 0; 
                dataPointer < Data.Length && memoryPointer < Memory.Length; 
                dataPointer += 2, memoryPointer += 1)
                Memory[memoryPointer] = (ushort)(Data[dataPointer] + (Data[dataPointer + 1] << 8));
        }

        public Action<ushort> Output = null;
        public Func<ushort> Input = null;

        private ushort DecodeOperand(ushort Operand)
        {
            if (Operand >= MAX_VALUE) return Registers[Operand - MAX_VALUE];
            return Operand;
        }

        private void WriteOperand(ushort Operand, ushort NewValue)
        {
            if (Operand >= MAX_VALUE) Registers[Operand - MAX_VALUE] = NewValue;
        }

        private ushort AddWords(ushort A, ushort B)
        {
            return (ushort)((A + B) % MAX_VALUE);
        }

        private ushort Op0
        {
            get { return Memory[AddWords(IP, 1)]; }
        }

        private ushort Op1
        {
            get { return Memory[AddWords(IP, 2)]; }
        }

        private ushort Op2
        {
            get { return Memory[AddWords(IP, 3)]; }
        }

        private void Advance(ushort Distance)
        {
            IP = AddWords(IP, Distance);
        }

        public void Step()
        {
            Status = StatusCode.Okay;

            try
            {
                var instruction = Memory[IP];

                switch (instruction)
                {
                    //halt: 0
                    //  stop execution and terminate the program
                    case 0:
                        Status = StatusCode.Halt;
                        return;

                    //set: 1 a b
                    //  set register <a> to the value of <b>
                    case 1:
                        WriteOperand(Op0, DecodeOperand(Op1));
                        Advance(3);
                        return;

                    //push: 2 a
                    //  push <a> onto the stack
                    case 2:
                        Stack.Push(DecodeOperand(Op0));
                        Advance(2);
                        return;

                    //pop: 3 a
                    //  remove the top element from the stack and write it into <a>; empty stack = error
                    case 3:
                        if (Stack.Count == 0)
                        {
                            Status = StatusCode.Error;
                            ErrorMessage = "Attempt to pop from empty stack.";
                        }
                        else
                        {
                            WriteOperand(Op0, Stack.Pop());
                            Advance(2);
                        }
                        return;

                    //eq: 4 a b c
                    //  set <a> to 1 if <b> is equal to <c>; set it to 0 otherwise
                    case 4:
                        if (DecodeOperand(Op1) == DecodeOperand(Op2))
                            WriteOperand(Op0, 1);
                        else
                            WriteOperand(Op0, 0);
                        Advance(4);
                        return;

                    //gt: 5 a b c
                    //  set <a> to 1 if <b> is greater than <c>; set it to 0 otherwise
                    case 5:
                        if (DecodeOperand(Op1) > DecodeOperand(Op2))
                            WriteOperand(Op0, 1);
                        else
                            WriteOperand(Op0, 0);
                        Advance(4);
                        return;

                    //jmp: 6 a
                    //  jump to <a>
                    case 6:
                        IP = DecodeOperand(Op0);
                        return;

                    //jt: 7 a b
                    //  if <a> is nonzero, jump to <b>
                    case 7:
                        if (DecodeOperand(Op0) != 0)
                            IP = DecodeOperand(Op1);
                        else
                            Advance(3);
                        return;

                    //jf: 8 a b
                    //  if <a> is zero, jump to <b>
                    case 8:
                        if (DecodeOperand(Op0) == 0)
                            IP = DecodeOperand(Op1);
                        else
                            Advance(3);
                        return;

                    //add: 9 a b c
                    //  assign into <a> the sum of <b> and <c> (modulo 32768)
                    case 9:
                        WriteOperand(Op0, AddWords(DecodeOperand(Op1), DecodeOperand(Op2)));
                        Advance(4);
                        return;

                    //mult: 10 a b c
                    //  store into <a> the product of <b> and <c> (modulo 32768)
                    case 10:
                        WriteOperand(Op0, (ushort)((DecodeOperand(Op1) * DecodeOperand(Op2)) % MAX_VALUE));
                        Advance(4);
                        return;

                    //mod: 11 a b c
                    //  store into <a> the remainder of <b> divided by <c>
                    case 11:
                        WriteOperand(Op0, (ushort)(DecodeOperand(Op1) % DecodeOperand(Op2)));
                        Advance(4);
                        return;

                    //and: 12 a b c
                    //  stores into <a> the bitwise and of <b> and <c>
                    case 12:
                        WriteOperand(Op0, (ushort)(DecodeOperand(Op1) & DecodeOperand(Op2)));
                        Advance(4);
                        return;

                    //or: 13 a b c
                    //  stores into <a> the bitwise or of <b> and <c>
                    case 13:
                        WriteOperand(Op0, (ushort)(DecodeOperand(Op1) | DecodeOperand(Op2)));
                        Advance(4);
                        return;

                    //not: 14 a b
                    //  stores 15-bit bitwise inverse of <b> in <a>
                    case 14:
                        WriteOperand(Op0, (ushort)(~DecodeOperand(Op1) & 0x7FFF));
                        Advance(3);
                        return;

                    //rmem: 15 a b
                    //  read memory at address <b> and write it to <a>
                    case 15:
                        WriteOperand(Op0, Memory[DecodeOperand(Op1)]);
                        Advance(3);
                        return;

                    //wmem: 16 a b
                    //  write the value from <b> into memory at address <a>
                    case 16:
                        Memory[DecodeOperand(Op0)] = DecodeOperand(Op1);
                        Advance(3);
                        return;

                    //call: 17 a
                    //  write the address of the next instruction to the stack and jump to <a>
                    case 17:
                        Stack.Push(AddWords(IP, 2));
                        IP = DecodeOperand(Op0);
                        return;

                    //ret: 18
                    //  remove the top element from the stack and jump to it; empty stack = halt
                    case 18:
                        if (Stack.Count == 0)
                            Status = StatusCode.Halt;
                        else
                            IP = Stack.Pop();
                        return;

                    //out: 19 a
                    //  write the character represented by ascii code <a> to the terminal
                    case 19:
                        if (Output != null) Output(DecodeOperand(Op0));
                        Advance(2);
                        return;

                    //in: 20 a
                    //  read a character from the terminal and write its ascii code to <a>; it can be assumed that once input starts, it will continue until a newline is encountered; this means that you can safely read whole lines from the keyboard and trust that they will be fully read
                    case 20:
                        if (Input != null) WriteOperand(Op0, Input());
                        Advance(2);
                        return;

                    //noop: 21
                    //  no operation
                    case 21:
                        Advance(1);
                        return;

                    default:
                        Status = StatusCode.Error;
                        ErrorMessage = "Unknown opcode : " + instruction;
                        return;
                }
            }
            catch (Exception e)
            {
                Status = StatusCode.Error;
                ErrorMessage = "Exception thrown: " + e.Message + "\n" + e.StackTrace;
            }
        }
    }
}
