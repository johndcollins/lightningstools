using System.Runtime.InteropServices;

namespace Henkie.Common
{
    /// <summary>
    ///  Diagnostic LED operation modes
    /// </summary>
    [ComVisible(true)]
    public enum DiagnosticLEDMode : byte
    {
        /// <summary>
        ///  LED is always OFF
        /// </summary>
        Off = 0x00,
        /// <summary>
        ///  LED is always ON
        /// </summary>
        On = 0x01,
        /// <summary>
        ///  LED flashes at heartbeat rate (power-on default)
        /// </summary>
        Heartbeat = 0x02,
        /// <summary>
        ///  Toggle ON/OFF state per accepted command
        /// </summary>
        ToggleOnAcceptedCommand = 0x03,
        /// <summary>
        /// LED is ON during DOA packet reception
        /// </summary>
        OnDuringDOAPacketReception = 0x04
    }
}