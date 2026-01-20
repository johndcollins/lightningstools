using System;

namespace Henkie.Common
{
    public interface ICommandDispatcher:IDisposable
    {
        void SendCommand(byte subaddress, byte? data = null, bool sendChecksumAndDelimiter = false);
        byte[] SendQuery(byte subaddress, byte? data = null, int bytesToRead = 0, bool sendChecksumAndDelimiter = false);
    }
}
