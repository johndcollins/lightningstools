using System;

namespace Henkie.Common
{
    public class UsbCommandDispatcher : ICommandDispatcher
    {
        private bool _isDisposed = false;
        private ISerialPortConnection SerialPortConnection { get; set; }

        public UsbCommandDispatcher(ISerialPortConnection serialPortConnection)
        {
            SerialPortConnection = serialPortConnection;
        }
        public UsbCommandDispatcher(string COMPort)
        {
            SerialPortConnection = new SerialPortConnection(COMPort);
        }
        public void SendCommand(byte subaddress, byte? data=null, bool usePseudoCOBS = false)
        {
            if (SerialPortConnection != null)
            {
                if (data != null)
                {
                    if (!usePseudoCOBS)
                    {
                        SerialPortConnection.Write(new[] { subaddress, data.Value }, 0, 2);
                        //Console.WriteLine($"Writing command with subAddress:{subaddress} with value byte:{data.Value} to {SerialPortConnection.COMPort}");
                    }
                    else
                    {
                        var checksum = (byte)((subaddress + data.Value) & 0x00FF);
                        var delimiter = (byte)0xFF;
                        SerialPortConnection.Write(new[] { subaddress, data.Value, checksum, delimiter }, 0, 4);
                        //Console.WriteLine($"Writing command with subAddress:{subaddress} with value byte:{data.Value}, checksum:{checksum}, delimiter:{delimiter} to {SerialPortConnection.COMPort}");
                    }
                }
            }
        }
        public byte[] SendQuery(byte subaddress, byte? data = null, int bytesToRead = 0, bool usePsuedoCOBS = false)
        {
            if (bytesToRead <0)
            {
                throw new ArgumentOutOfRangeException(nameof(bytesToRead));
            }
            if (SerialPortConnection != null)
            {
                if (bytesToRead > 0)
                {
                    SerialPortConnection.DiscardInputBuffer();
                }
                SendCommand(subaddress, data, usePsuedoCOBS);
                if (bytesToRead > 0)
                {
                    var readBuffer = new byte[bytesToRead];
                    SerialPortConnection.Read(readBuffer, 0, bytesToRead);
                    return readBuffer;
                }
            }
            return null;
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~UsbCommandDispatcher()
        {
            Dispose();
        }

        private void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                if (SerialPortConnection != null)
                {
                    SerialPortConnection.Dispose();
                }
            }
            _isDisposed = true;
        }
    }
}
