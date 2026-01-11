using System.Runtime.InteropServices;

namespace Henkie.HSI.Board1
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
        ///  User defined digital output #1
        /// </summary>
        DIG_OUT_1 = 0x01,
        /// <summary>
        ///  User defined digital output #2
        /// </summary>
        DIG_OUT_2 = 0x02,
    }
}
