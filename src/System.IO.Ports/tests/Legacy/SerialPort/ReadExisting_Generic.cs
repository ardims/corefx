// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Ports;
using System.Diagnostics;

public class ReadExisting
{
    public static readonly String s_strDtTmVer = "MsftEmpl, 2003/02/05 15:37 MsftEmpl";
    public static readonly String s_strClassMethod = "SerialPort.ReadExisting()";
    public static readonly String s_strTFName = "ReadExisting.cs";
    public static readonly String s_strTFAbbrev = s_strTFName.Substring(0, 6);
    public static readonly String s_strTFPath = Environment.CurrentDirectory;

    //Set bounds fore random timeout values.
    //If the min is to low read will not timeout accurately and the testcase will fail
    public static int minRandomTimeout = 250;

    //If the max is to large then the testcase will take forever to run
    public static int maxRandomTimeout = 2000;

    //Since ReadExisting should return immediately this is the maximum time in ms that ReadExisting should take
    //before we consider ReadExisting to be blocking and cause the test case to fail
    public static double maxTimeout = 50;

    //The number of random characters to receive
    public static int numRndChar = 8;
    public static readonly int NUM_TRYS = 5;

    private int _numErrors = 0;
    private int _numTestcases = 0;
    private int _exitValue = TCSupport.PassExitCode;

    public static void Main(string[] args)
    {
        ReadExisting objTest = new ReadExisting();
        AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(objTest.AppDomainUnhandledException_EventHandler);

        Console.WriteLine(s_strTFPath + " " + s_strTFName + " , for " + s_strClassMethod + " , Source ver : " + s_strDtTmVer);

        try
        {
            objTest.RunTest();
        }
        catch (Exception e)
        {
            Console.WriteLine(s_strTFAbbrev + " : FAIL The following exception was thorwn in RunTest(): \n" + e.ToString());
            objTest._numErrors++;
            objTest._exitValue = TCSupport.FailExitCode;
        }

        ////	Finish Diagnostics
        if (objTest._numErrors == 0)
        {
            Console.WriteLine("PASS.	 " + s_strTFPath + " " + s_strTFName + " ,numTestcases==" + objTest._numTestcases);
        }
        else
        {
            Console.WriteLine("FAIL!	 " + s_strTFPath + " " + s_strTFName + " ,numErrors==" + objTest._numErrors);

            if (TCSupport.PassExitCode == objTest._exitValue)
                objTest._exitValue = TCSupport.FailExitCode;
        }

        Environment.ExitCode = objTest._exitValue;
    }

    private void AppDomainUnhandledException_EventHandler(Object sender, UnhandledExceptionEventArgs e)
    {
        _numErrors++;
        Console.WriteLine("\nAn unhandled exception was thrown and not caught in the app domain: \n{0}", e.ExceptionObject);
        Console.WriteLine("Test FAILED!!!\n");

        Environment.ExitCode = 101;
    }

    public bool RunTest()
    {
        bool retValue = true;
        TCSupport tcSupport = new TCSupport();

        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadWithoutOpen), TCSupport.SerialPortRequirements.None);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadAfterFailedOpen), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ReadAfterClose), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Timeout), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(SuccessiveReadTimeoutNoData), TCSupport.SerialPortRequirements.OneSerialPort);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(Default_MaxInt), TCSupport.SerialPortRequirements.OneSerialPort);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(DefaultParityReplaceByte), TCSupport.SerialPortRequirements.NullModem);

        retValue &= tcSupport.BeginTestcase(new TestDelegate(NoParityReplaceByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(RNDParityReplaceByte), TCSupport.SerialPortRequirements.NullModem);
        retValue &= tcSupport.BeginTestcase(new TestDelegate(ParityErrorOnLastByte), TCSupport.SerialPortRequirements.NullModem);

        _numErrors += tcSupport.NumErrors;
        _numTestcases = tcSupport.NumTestcases;
        _exitValue = tcSupport.ExitValue;

        return retValue;
    }

    #region Test Cases
    public bool ReadWithoutOpen()
    {
        SerialPort com = new SerialPort();

        Console.WriteLine("Verifying read method throws exception without a call to Open()");
        if (!VerifyReadException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_001!!! Verifying read method throws exception without a call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool ReadAfterFailedOpen()
    {
        SerialPort com = new SerialPort("BAD_PORT_NAME");

        Console.WriteLine("Verifying read method throws exception with a failed call to Open()");

        //Since the PortName is set to a bad port name Open will thrown an exception
        //however we don't care what it is since we are verfifying a read method
        try
        {
            com.Open();
        }
        catch (System.Exception)
        {
        }
        if (!VerifyReadException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_002!!! Verifying read method throws exception with a failed call to Open() FAILED");
            return false;
        }

        return true;
    }


    public bool ReadAfterClose()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying read method throws exception after a call to Cloes()");
        com.Open();
        com.Close();

        if (!VerifyReadException(com, typeof(System.InvalidOperationException)))
        {
            Console.WriteLine("Err_003!!! Verifying read method throws exception after a call to Cloes() FAILED");
            return false;
        }

        return true;
    }


    public bool Timeout()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        Console.WriteLine("Verifying ReadTimeout={0}", com.ReadTimeout);
        com.Open();

        if (!VerifyTimeout(com))
        {
            Console.WriteLine("Err_004!!! Verifying timeout FAILED");
            return false;
        }

        return true;
    }


    public bool Default_MaxInt()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);

        com.ReadTimeout = System.Int32.MaxValue;
        Console.WriteLine("Verifying ReadTimeout={0}", com.ReadTimeout);
        com.Open();

        if (!VerifyTimeout(com))
        {
            Console.WriteLine("Err_005!!! Verifying timeout FAILED");
            return false;
        }

        return true;
    }


    public bool SuccessiveReadTimeoutNoData()
    {
        SerialPort com = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        Random rndGen = new Random(-55);
        bool retValue = true;

        com.ReadTimeout = rndGen.Next(minRandomTimeout, maxRandomTimeout);
        //		com.Encoding = new System.Text.UTF7Encoding();
        com.Encoding = System.Text.Encoding.Unicode;

        Console.WriteLine("Verifying ReadTimeout={0} with successive call to read method and no data", com.ReadTimeout);
        com.Open();

        if ("" != com.ReadExisting())
        {
            Console.WriteLine("ERROR!!!: Read did not reuturn empty string when it timed out");
            retValue = false;
        }

        retValue &= VerifyTimeout(com);
        if (!retValue)
        {
            Console.WriteLine("Err_006!!! Verifying with successive call to read method and no data FAILED");
        }

        return retValue;
    }


    public bool DefaultParityReplaceByte()
    {
        if (!VerifyParityReplaceByte(-1, numRndChar - 2))
        {
            Console.WriteLine("Err_007!!! Verifying default ParityReplace byte FAILED");
            return false;
        }

        return true;
    }


    public bool NoParityReplaceByte()
    {
        Random rndGen = new Random(-55);

        if (!VerifyParityReplaceByte((int)'\0', rndGen.Next(0, numRndChar - 1)))
        {
            Console.WriteLine("Err_008!!! Verifying no ParityReplace byte FAILED");
            return false;
        }

        return true;
    }


    public bool RNDParityReplaceByte()
    {
        Random rndGen = new Random(-55);

        if (!VerifyParityReplaceByte(rndGen.Next(0, 128), 0))
        {
            Console.WriteLine("Err_009!! Verifying random ParityReplace byte FAILED");
            return false;
        }

        return true;
    }


    public bool ParityErrorOnLastByte()
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(15);
        byte[] bytesToWrite = new byte[numRndChar];
        char[] expectedChars = new char[numRndChar];
        char[] actualChars;
        bool retValue = true;
        int waitTime;

        /* 1 Additional character gets added to the input buffer when the parity error occurs on the last byte of a stream
             We are verifying that besides this everything gets read in correctly. See NDP Whidbey: 24216 for more info on this */
        Console.WriteLine("Verifying default ParityReplace byte with a parity errro on the last byte");

        //Genrate random characters without an parity error
        for (int i = 0; i < bytesToWrite.Length; i++)
        {
            byte randByte = (byte)rndGen.Next(0, 128);

            bytesToWrite[i] = randByte;
            expectedChars[i] = (char)randByte;
        }

        bytesToWrite[bytesToWrite.Length - 1] = (byte)(bytesToWrite[bytesToWrite.Length - 1] | 0x80); //Create a parity error on the last byte
        expectedChars[expectedChars.Length - 1] = (char)com1.ParityReplace; // Set the last expected char to be the ParityReplace Byte

        com1.Parity = Parity.Space;
        com1.DataBits = 7;
        com1.ReadTimeout = 250;

        com1.Open();
        com2.Open();

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);

        waitTime = 0;

        while (bytesToWrite.Length + 2 > com1.BytesToRead && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        actualChars = (com1.ReadExisting()).ToCharArray();

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != actualChars[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", (int)expectedChars[i], (int)actualChars[i]);
                retValue = false;
            }
        }

        if (1 < com1.BytesToRead)
        {
            Console.WriteLine("ERROR!!!: Expected BytesToRead=0 actual={0}", com1.BytesToRead);
            Console.WriteLine("ByteRead={0}, {1}", com1.ReadByte(), bytesToWrite[bytesToWrite.Length - 1]);
            retValue = false;
        }

        com1.DiscardInBuffer();

        bytesToWrite[bytesToWrite.Length - 1] = (byte)'\n';
        expectedChars[expectedChars.Length - 1] = (char)bytesToWrite[bytesToWrite.Length - 1];

        retValue &= VerifyRead(com1, com2, bytesToWrite, expectedChars);

        com1.Close();
        com2.Close();

        if (!retValue)
            Console.WriteLine("Err_010!!! Verifying default ParityReplace byte with a parity errro on the last byte failed");

        return retValue;
    }
    #endregion

    #region Verification for Test Cases
    private bool VerifyTimeout(SerialPort com)
    {
        System.Diagnostics.Stopwatch timer = new Stopwatch();
        int actualTime = 0;
        bool retValue = true;
        string strReadReturn;

        strReadReturn = com.ReadExisting(); //Warm up read method

        if ("" != strReadReturn)
        {
            Console.WriteLine("ERROR!!!: Read did not reuturn empty string when it timed out the first time");
            retValue = false;
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

        for (int i = 0; i < NUM_TRYS; i++)
        {
            timer.Start();
            strReadReturn = com.ReadExisting();
            timer.Stop();

            if ("" != strReadReturn)
            {
                Console.WriteLine("ERROR!!!: Read did not reuturn empty string when it timed out");
                retValue = false;
            }

            actualTime += (int)timer.ElapsedMilliseconds;
            timer.Reset();
        }

        System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Normal;
        actualTime /= NUM_TRYS;

        //Verify that the percentage difference between the expected and actual timeout is less then maxPercentageDifference
        if (maxTimeout < actualTime)
        {
            Console.WriteLine("ERROR!!!: The read method timedout in {0} expected {1}", actualTime, 0);
            retValue = false;
        }

        if (com.IsOpen)
            com.Close();

        return retValue;
    }


    private bool VerifyReadException(SerialPort com, Type expectedException)
    {
        bool retValue = true;

        try
        {
            com.ReadExisting();
            Console.WriteLine("ERROR!!!: No Excpetion was thrown");
            retValue = false;
        }
        catch (System.Exception e)
        {
            if (e.GetType() != expectedException)
            {
                Console.WriteLine("ERROR!!!: {0} exception was thrown expected {1}", e.GetType(), expectedException);
                retValue = false;
            }
        }
        return retValue;
    }


    public bool VerifyParityReplaceByte(int parityReplace, int parityErrorIndex)
    {
        SerialPort com1 = new SerialPort(TCSupport.LocalMachineSerialInfo.FirstAvailablePortName);
        SerialPort com2 = new SerialPort(TCSupport.LocalMachineSerialInfo.SecondAvailablePortName);
        Random rndGen = new Random(-55);
        byte[] byteBuffer = new byte[numRndChar];
        char[] charBuffer = new char[numRndChar];
        int expectedChar;

        //Genrate random characters without an parity error
        for (int i = 0; i < byteBuffer.Length; i++)
        {
            int randChar = rndGen.Next(0, 128);

            byteBuffer[i] = (byte)randChar;
            charBuffer[i] = (char)randChar;
        }

        if (-1 == parityReplace)
        {
            //If parityReplace is -1 and we should just use the default value
            expectedChar = com1.ParityReplace;
        }
        else if ('\0' == parityReplace)
        {
            //If parityReplace is the null charachater and parity replacement should not occur
            com1.ParityReplace = (byte)parityReplace;
            expectedChar = charBuffer[parityErrorIndex];
        }
        else
        {
            //Else parityReplace was set to a value and we should expect this value to be returned on a parity error
            com1.ParityReplace = (byte)parityReplace;
            expectedChar = parityReplace;
        }

        //Create an parity error by setting the highest order bit to true
        byteBuffer[parityErrorIndex] = (byte)(byteBuffer[parityErrorIndex] | 0x80);
        charBuffer[parityErrorIndex] = (char)expectedChar;

        Console.WriteLine("Verifying ParityReplace={0} with an ParityError at: {1} ", com1.ParityReplace, parityErrorIndex);

        com1.Parity = Parity.Space;
        com1.DataBits = 7;

        com1.Open();
        com2.Open();

        return VerifyRead(com1, com2, byteBuffer, charBuffer);
    }


    private bool VerifyRead(SerialPort com1, SerialPort com2, byte[] bytesToWrite, char[] expectedChars)
    {
        bool retValue = true;
        string rcvString;
        char[] rcvBuffer;
        char[] buffer = new char[expectedChars.Length];
        int bytesRead, totalBytesRead, charsRead, totalCharsRead;
        int waitTime = 0;

        com2.Write(bytesToWrite, 0, bytesToWrite.Length);
        com1.ReadTimeout = 250;

        while (com1.BytesToRead < bytesToWrite.Length && waitTime < 500)
        {
            System.Threading.Thread.Sleep(50);
            waitTime += 50;
        }

        totalBytesRead = 0;
        totalCharsRead = 0;

        while (0 != com1.BytesToRead)
        {
            //While their are more characters to be read
            rcvString = com1.ReadExisting();
            rcvBuffer = rcvString.ToCharArray();

            charsRead = rcvBuffer.Length;
            bytesRead = com1.Encoding.GetByteCount(rcvBuffer, 0, charsRead);

            if (expectedChars.Length < totalCharsRead + charsRead)
            {
                //If we have read in more characters then we expect
                Console.WriteLine("ERROR!!!: We have received more characters then were sent");
                retValue = false;
                break;
            }

            System.Array.Copy(rcvBuffer, 0, buffer, totalCharsRead, charsRead);

            totalBytesRead += bytesRead;
            totalCharsRead += charsRead;

            if (bytesToWrite.Length - totalBytesRead != com1.BytesToRead)
            {
                System.Console.WriteLine("ERROR!!!: Expected BytesToRead={0} actual={1}", bytesToWrite.Length - totalBytesRead, com1.BytesToRead);
                retValue = false;
            }
        }

        //Compare the chars that were written with the ones we expected to read
        for (int i = 0; i < expectedChars.Length; i++)
        {
            if (expectedChars[i] != buffer[i])
            {
                System.Console.WriteLine("ERROR!!!: Expected to read {0}  actual read  {1}", (int)expectedChars[i], (int)buffer[i]);
                retValue = false;
            }
        }

        if (com1.IsOpen)
            com1.Close();

        if (com2.IsOpen)
            com2.Close();

        return retValue;
    }
    #endregion
}
