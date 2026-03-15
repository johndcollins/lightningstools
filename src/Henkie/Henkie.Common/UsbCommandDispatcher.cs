using System;
using System.Collections.Generic;
using System.Threading;

namespace Henkie.Common
{
    public class UsbCommandDispatcher: ICommandDispatcher
    {

        private bool _isDisposed = false;
        private ISerialPortConnection SerialPortConnection { get; set; }
        private readonly Queue<UsbCommand> _commandQueue = new Queue<UsbCommand>();
        private System.Timers.Timer _commandSendingTimer;
        private bool _useCommandSendingTimer = false;
        private struct UsbCommand
        {
            public byte Subaddress { get; set; }
            public byte? Data { get; set; }
            public bool UsePseudoCobs { get; set; }
        }
        public UsbCommandDispatcher(ISerialPortConnection serialPortConnection)
        {
            SerialPortConnection = serialPortConnection;
            StartCommandSendingTimer();
        }

        public UsbCommandDispatcher(string COMPort)
        {
            SerialPortConnection = new SerialPortConnection(COMPort);
            StartCommandSendingTimer();
        }
        private void StartCommandSendingTimer()
        {
            if (_useCommandSendingTimer)
            {
                _commandSendingTimer = new System.Timers.Timer(20)
                {
                    AutoReset = true,
                };
                _commandSendingTimer.Elapsed += _commandSendingTimer_Elapsed;
                _commandSendingTimer.Enabled = true;
                _commandSendingTimer.Start();
            }
        }
        public void SendCommand(byte subaddress, byte? data = null, bool usePseudoCOBS = false)
        {
            if (_useCommandSendingTimer)
            {
                var usbCommand = new UsbCommand() { Subaddress = subaddress, Data = data, UsePseudoCobs = usePseudoCOBS };
                _commandQueue.Enqueue(usbCommand);
            }
            else
            {
                SendCommandInternal(subaddress, data, usePseudoCOBS);
            }
        }
        private void _commandSendingTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var nextCommand = _commandQueue.Count > 0 ? _commandQueue.Dequeue() : (UsbCommand?)null;
            if (nextCommand != null)
            {
                SendCommandInternal(nextCommand.Value.Subaddress, nextCommand.Value.Data, nextCommand.Value.UsePseudoCobs);
            }
        }

        private void SendCommandInternal(byte subaddress, byte? data=null, bool usePseudoCOBS = false)
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
                if (_commandSendingTimer != null)
                {
                    _commandSendingTimer.Dispose();
                }
            }
            _isDisposed = true;
        }
    }
}
