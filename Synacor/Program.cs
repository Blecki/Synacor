using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Synacor
{
    class Program
    {
        static void Main(string[] args)
        {
            //var testBed = new TestBed(new byte[] { 
            //    0x09, 0x00, 
            //    0x00, 0x80, 
            //    0x01, 0x80, 
            //    0x04, 0x00, 
            //    0x13, 0x00,
            //    0x00, 0x80 });

            var testBed = new TestBed(System.IO.File.ReadAllBytes("challenge.bin"));

            testBed.RunTest();
            testBed.DisplayData();

            Console.ReadKey();
        }
    }
}
