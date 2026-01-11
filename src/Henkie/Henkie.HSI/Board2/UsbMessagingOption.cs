using System.Runtime.InteropServices;

namespace Henkie.HSI.Board2
{
    /// <summary>
    ///   USB Messaging Option
    /// </summary>
    [ComVisible(true)]
    public enum UsbMessagingOption : byte
    {
        /// <summary>
        /// Send never (disabled)
        /// </summary>
        SendNever = 0x00,
        /// <summary>
        /// Send only on request
        /// </summary>
        SendOnlyOnRequest = 0x01,
        /// <summary>
        /// Send on interval (periodically)
        /// </summary>
        SendOnInterval = 0x02,
        /// <summary>
        /// Send only if the value has changed
        /// </summary>
        SendOnlyOnChange = 0x03
    }
}