using System.Runtime.InteropServices;

namespace Henkie.HSI.Board2
{
    /// <summary>
    ///   Output channels
    /// </summary>
    [ComVisible(true)]
    public enum OutputChannels: byte
    {
        /// <summary>
        /// Unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        ///  User defined digital output A
        /// </summary>
        DIG_OUT_A = 0x01,
        /// <summary>
        ///  User defined digital output B
        /// </summary>
        DIG_OUT_B = 0x02,
        /// <summary>
        ///  User defined digital output X
        /// </summary>
        DIG_OUT_X = 0x03,

    }
}
