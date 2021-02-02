using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


/// <summary>
/// ProcessMemoryReader is a class that enables direct reading a process memory
/// </summary>
public class ProcessMemoryReaderApi
{
    // constants information can be found in <winnt.h>
    [Flags]
    public enum ProcessAccessType
    {
        PROCESS_TERMINATE = (0x0001),
        PROCESS_CREATE_THREAD = (0x0002),
        PROCESS_SET_SESSIONID = (0x0004),
        PROCESS_VM_OPERATION = (0x0008),
        PROCESS_VM_READ = (0x0010),
        PROCESS_VM_WRITE = (0x0020),
        PROCESS_DUP_HANDLE = (0x0040),
        PROCESS_CREATE_PROCESS = (0x0080),
        PROCESS_SET_QUOTA = (0x0100),
        PROCESS_SET_INFORMATION = (0x0200),
        PROCESS_QUERY_INFORMATION = (0x0400)
    }

    // function declarations are found in the MSDN and in <winbase.h> 

    //		HANDLE OpenProcess(
    //			DWORD dwDesiredAccess,  // access flag
    //			BOOL bInheritHandle,    // handle inheritance option
    //			DWORD dwProcessId       // process identifier
    //			);
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, UInt32 dwProcessId);

    //		BOOL CloseHandle(
    //			HANDLE hObject   // handle to object
    //			);
    [DllImport("kernel32.dll")]
    public static extern Int32 CloseHandle(IntPtr hObject);

    //		BOOL ReadProcessMemory(
    //			HANDLE hProcess,              // handle to the process
    //			LPCVOID lpBaseAddress,        // base of memory area
    //			LPVOID lpBuffer,              // data buffer
    //			SIZE_T nSize,                 // number of bytes to read
    //			SIZE_T * lpNumberOfBytesRead  // number of bytes read
    //			);
    [DllImport("kernel32.dll")]
    public static extern Int32 ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesRead);

    //		BOOL WriteProcessMemory(
    //			HANDLE hProcess,                // handle to process
    //			LPVOID lpBaseAddress,           // base of memory area
    //			LPCVOID lpBuffer,               // data buffer
    //			SIZE_T nSize,                   // count of bytes to write
    //			SIZE_T * lpNumberOfBytesWritten // count of bytes written
    //			);
    [DllImport("kernel32.dll")]
    public static extern Int32 WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [In, Out] byte[] buffer, UInt32 size, out IntPtr lpNumberOfBytesWritten);


}

public class ProcessMemoryReader
{

    public ProcessMemoryReader()
    {
    }

    /// <summary>	
    /// Process from which to read		
    /// </summary>
    public Process ReadProcess
    {
        get
        {
            return m_ReadProcess;
        }
        set
        {
            m_ReadProcess = value;
        }
    }

    private Process m_ReadProcess = null;

    private IntPtr m_hProcess = IntPtr.Zero;

    public void OpenProcess()
    { 
        ProcessMemoryReaderApi.ProcessAccessType access;
        access = ProcessMemoryReaderApi.ProcessAccessType.PROCESS_VM_READ
            | ProcessMemoryReaderApi.ProcessAccessType.PROCESS_VM_WRITE
            | ProcessMemoryReaderApi.ProcessAccessType.PROCESS_VM_OPERATION;
        m_hProcess = ProcessMemoryReaderApi.OpenProcess((uint)access, 1, (uint)m_ReadProcess.Id);
    }

    public void CloseHandle()
    {
        int iRetValue;
        iRetValue = ProcessMemoryReaderApi.CloseHandle(m_hProcess);
        if (iRetValue == 0)
            throw new Exception("CloseHandle failed");
    }

    public int GetBaseAddress()
    {
        return ReadProcess.Modules[0].BaseAddress.ToInt32();
    }

    public ProcessModule GetModule(string szModuleName)
    {
        foreach(ProcessModule pModule in ReadProcess.Modules)
        {
            if(pModule.ModuleName == szModuleName)
            {
                Console.WriteLine("Module Name: " + pModule.ModuleName + " Base Addr: " + pModule.BaseAddress.ToString("X"));
                return pModule;
            }
        }
        return null;
    }

    public string GetProcessName()
    {
        return ReadProcess.ProcessName;
    }

    public int ReadProcessInt(IntPtr MemoryAddress)
    {

        byte[] buffer = new byte[4];
        IntPtr ptrBytesRead;
        ProcessMemoryReaderApi.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, 4, out ptrBytesRead);

        return BitConverter.ToInt32(buffer, 0);
    }

    public byte[] ReadProcessMemory(IntPtr MemoryAddress, uint bytesToRead, out int bytesRead)
    {
        byte[] buffer = new byte[bytesToRead];

        IntPtr ptrBytesRead;
        ProcessMemoryReaderApi.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, bytesToRead, out ptrBytesRead);

        bytesRead = ptrBytesRead.ToInt32();

        return buffer;
    }

    public string ReadProcessMemoryString(IntPtr MemoryAddress, uint bytesToRead, out int bytesRead)
    {
        byte[] buffer = new byte[bytesToRead];

        IntPtr ptrBytesRead;
        ProcessMemoryReaderApi.ReadProcessMemory(m_hProcess, MemoryAddress, buffer, bytesToRead, out ptrBytesRead);
            
        bytesRead = ptrBytesRead.ToInt32();
            
        return System.Text.Encoding.Default.GetString(buffer);

    }

    public int GetPointerAddress(IntPtr StartAddress, int[] offsets)
    {
        if ((int)StartAddress == -1)
            return -1;
        int ptr = ReadProcessInt(StartAddress);
        for(int i = 0; i < offsets.Length-1; i++)
        {
            ptr += offsets[i];
            ptr = ReadProcessInt((IntPtr)ptr);
        }
        ptr += offsets[offsets.Length - 1];
        return ptr;
    }

    public void WriteProcessMemory(IntPtr MemoryAddress, byte[] bytesToWrite, out int bytesWritten)
    {
        IntPtr ptrBytesWritten;
        ProcessMemoryReaderApi.WriteProcessMemory(m_hProcess, MemoryAddress, bytesToWrite, (uint)bytesToWrite.Length, out ptrBytesWritten);

        bytesWritten = ptrBytesWritten.ToInt32();
    }
}