using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Activities.Statements;
using Microsoft.Win32.SafeHandles;

namespace ConsoleApp4
{
    class Program
    {
        private static ProcessMemoryReader GetLithtechProcess()
        {
            ProcessMemoryReader procReader
                   = new ProcessMemoryReader();

            System.Diagnostics.Process[] myProcesses
                           = System.Diagnostics.Process.GetProcessesByName("lithtech");


            if (myProcesses.Length == 0)
            {
                Console.WriteLine("Lithtech.exe is not running, please start the process and run this again");
            }
            else
            {

                procReader.ReadProcess = myProcesses[0];
                return (procReader);
            }
            return null;

        }
        static void Main(string[] args)
        {

            ProcessMemoryReader pReader = GetLithtechProcess();
            if (pReader != null)
            {
                pReader.OpenProcess();

                Console.WriteLine("Executable Name: " + pReader.GetProcessName() + " Base Addr: " + pReader.GetBaseAddress().ToString("X"));
                ProcessModule temp = pReader.GetModule("object.lto");

                if (temp != null)
                {
                    int bytesRead = 0;
                    byte[] buffer = new byte[4];
                    buffer = pReader.ReadProcessMemory(temp.BaseAddress, (uint)buffer.Length, out bytesRead);


                    //Get our modules correct offset
                    IntPtr finalOffset = temp.BaseAddress - pReader.GetBaseAddress();

                    //Add our static offset
                    IntPtr newOffset = (IntPtr)pReader.GetBaseAddress() + (int)finalOffset + 0x92128;

                    //Setup our multi level pointer
                    int[] offsetList = { 0x58c };

                    //Use our multi level pointer to get the correct pointer address
                    int finalPtrAddr = pReader.GetPointerAddress(newOffset, offsetList);
                    Console.WriteLine(String.Format("Final Offset Pointer Address: {0}", finalPtrAddr.ToString("x")));

                    while (true)
                    {
                        //check if we lost our object.lto and regain control
                        if (temp != null)
                        {
                            //Get our level name
                            string levelName = pReader.ReadProcessMemoryString((IntPtr)finalPtrAddr, 64, out bytesRead);
                            Console.WriteLine(String.Format("Level loaded: {0}", levelName));
                            System.Threading.Thread.Sleep(200);
                            Console.CursorTop--;
                            Console.CursorVisible = false;
                        }
                        else
                        {
                            //We lost it, so try and get it again
                            temp = pReader.GetModule("object.lto");
                        }
                    }
                }
            }

            Console.ReadLine();
        }
    }
}