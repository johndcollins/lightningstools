using System;

namespace Henkie.Common
{
    public interface ICommandDispatcher:IDisposable
    {
        void SendCommand(byte subaddress, byte? data = null, bool usePsuedoCOBS = false);
        byte[] SendQuery(byte subaddress, byte? data = null, int bytesToRead = 0, bool usePsuedoCOBS = false);
    }
}
