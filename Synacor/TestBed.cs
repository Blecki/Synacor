using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synacor
{
    public class TestBed
    {
        public Emulator Emulator;

        public TestBed(byte[] ProgramData)
        {
            Emulator = new Emulator();
            Emulator.LoadMemory(ProgramData);
            Emulator.Output += c => Console.Write((char)c);
            Emulator.Input += () => (ushort)Console.ReadKey().KeyChar;
        }

        public void RunTest()
        {
            while (Emulator.Status == Synacor.Emulator.StatusCode.Okay)
                Emulator.Step();            
        }

        public void DisplayData()
        {
            Console.WriteLine();
            Console.WriteLine("*** CPU STATUS ***");
            Console.WriteLine(Emulator.Status.ToString());
            if (Emulator.Status == Synacor.Emulator.StatusCode.Error)
                Console.WriteLine(Emulator.ErrorMessage);
            for (var i = 0; i < 8; ++i)
                Console.WriteLine("R{0}: {1:X4}", i, Emulator.Registers[i]);
            Console.WriteLine("IP : {0:X4}", Emulator.IP);
            Console.WriteLine("******************");
        }
    }
}
