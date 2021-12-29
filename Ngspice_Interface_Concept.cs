//    Sample sharedspice C# interface by github.com/nextguyover, created on 29/12/2021
//    Originally created for https://sourceforge.net/p/ngspice/discussion/120972/thread/49c3689c56/#78cb

using System;
using System.Runtime.InteropServices;

public class Ngspice_Interface_Concept
{
    public delegate int SendChar(string callerOut, int idNum, IntPtr pointer);                              //Define the delegates required to interface with Ngspice.dll
    public delegate int SendStat(string simStatus, int idNum, IntPtr pointer);                              //Note: char* data type in C++ seems to translate to string type in C#
    public delegate int ControlledExit(int exitStatus, bool unloadStatus, bool exitType, int idNum, IntPtr pointer);    //IntPtr type stores a pointer as an integer
    public delegate int SendData(IntPtr pvecvaluesall, int structNum, int idNum, IntPtr pointer);
    public delegate int SendInitData(IntPtr pvecinfoall, int idNum, IntPtr pointer);
    public delegate int BGThreadRunning(bool backgroundThreadRunning, int idNum, IntPtr pointer);

    [DllImport("ngspice-35.dll")] private static extern int ngSpice_Init(SendChar aa, SendStat bb, ControlledExit cc, SendData dd, SendInitData ee, BGThreadRunning ff, IntPtr pointer);    //define external dll functions (aa, bb, cc, dd, ee, ff are random variable names)
    [DllImport("ngspice-35.dll")] private static extern int ngSpice_Command(string commandString);
    //Other required ngspice functions can be defined as above

    public IntPtr dummyIntPtr = new IntPtr();

    void main(){    //initialising Ngspice by sending delegates as parameters
        ngSpice_Init(new SendChar(SendCharReceive), new SendStat(SendStatReceive), new ControlledExit(ControlledExitReceive), new SendData(SendDataReceive), new SendInitData(SendInitDataReceive), new BGThreadRunning(BGThreadRunningReceive), dummyIntPtr);

        //commands can be sent to Ngspice like this:
        ngSpice_Command("example command");
    }

    //------------------Structures for parsing data from SendDataReceive()-------------------//

    public struct vecValuesAll
	{
	    public int vecCount; 					// number of vectors in plot
		public int vecIndex; 					// index of actual set of vectors , i.e.	the number of accepted data points
		public IntPtr vecArray;					// values of actual set of vectors, indexed from 0 to veccount - 1
	};

    //WARNING: The boolean variables of the structure below (isScale and isComplex) may not be memory mapped correctly between C and C#
    //and as such, may not yield correct values. (I did no testing on these two variables) 

    [StructLayout(LayoutKind.Sequential)] public struct vecValue
	{
	    public string vecName; 			    // name of a specific vector (as char*, this pointer can be turned into a string later)
		public double cReal;			        // actual data value (real)
		public double cImag;			        // actual data value (imaginary)
		public bool isScale; 			        // if ’name ’ is the scale vector
		public bool isComplex; 		            // if the data are complex numbers
	};

    public struct vecValuePtrStruct             // this structure purely makes parsing easier using C# Marshalling
	{
	    public IntPtr vecValuePtr;
	};

    //------------------Callback functions-------------------//

    static int SendCharReceive(string callerOut, int idNum, IntPtr pointer){
        Debug.Log("SendCharReceive called: " + callerOut);
        return 0;
    }

    static int SendStatReceive(string simStatus, int idNum, IntPtr pointer){
        Debug.Log("SendStatReceive called: " + simStatus);
        return 0;
    }

    static int ControlledExitReceive(int exitStatus, bool unloadStatus, bool exitType, int idNum, IntPtr pointer){
        Debug.Log("ControlledExitReceive called: " + exitStatus);
        return 0;
    }

    static int SendDataReceive(IntPtr pvecvaluesall, int structNum, int idNum, IntPtr pointer){
        Debug.Log("SendDataReceive called");

        vecValuesAll allValues = (vecValuesAll)Marshal.PtrToStructure(pvecvaluesall, typeof(vecValuesAll));			// get allValues struct from unmanaged memory

        vecValuePtrStruct[] vecValuePtrs = new vecValuePtrStruct[allValues.vecCount];	    // define array of IntPtrs
		vecValue[] values = new vecValue[allValues.vecCount];                               // define array of vector values

        MarshalUnmananagedArray2Struct(allValues.vecArray, allValues.vecCount, out vecValuePtrs);   // marshal array of pointers from location of allValues.vecArray, and paste into vecValuePtrs array

        for(int i = 0; i < allValues.vecCount; i++){                                                        // iterate through each vector
            values[i] = (vecValue)Marshal.PtrToStructure(vecValuePtrs[i].vecValuePtr, typeof(vecValue));    // marshal each vector value structure from each pointer value in vecValuePtrs array
        }

        //values[] now contains all the vector structures
        //Do something...
        
        return 0;
    }

    static int SendInitDataReceive(IntPtr pvecinfoall, int idNum, IntPtr pointer){
        Debug.Log("SendInitDataReceive called");
        return 0;
    }   

    static int BGThreadRunningReceive(bool backgroundThreadRunning, int idNum, IntPtr pointer){
        Debug.Log("BGThreadRunningReceive called");
        return 0;
    }

    //------------------Helper Function-------------------//

    public static void MarshalUnmananagedArray2Struct<T>(IntPtr unmanagedArray, int length, out T[] mangagedArray)		// https://stackoverflow.com/a/40376326/9112181
	{                                                                                                                   // converts an array of structures from unmanaged memory to a managed variable
	    var size = Marshal.SizeOf(typeof(T));  
	    mangagedArray = new T[length];

	    for (int i = 0; i < length; i++)
	    {
	        IntPtr nextStructureMemBlock = IntPtr.Add(unmanagedArray, i * size);		//inrements pointer memory location to position of next struct in array
	        mangagedArray[i] = Marshal.PtrToStructure<T>(nextStructureMemBlock);
	    }
	}
}