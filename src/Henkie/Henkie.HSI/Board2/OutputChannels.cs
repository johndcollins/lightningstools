using System;
using System.Runtime.InteropServices;

namespace Henkie.HSI.Board2
{
    /// <summary>
    ///   Output channels
    /// </summary>
    [Flags]
    [ComVisible(true)]
    public enum OutputChannels: byte
    {
        /// <summary>
        ///  Unknown
        /// </summary>
        Unknown = 0,
        /// <summary>
        ///  User defined digital output A
        /// </summary>
        DIG_OUT_A = 1 << 1,
        /// <summary>
        ///  User defined digital output B
        /// </summary>
        DIG_OUT_B = 1 << 2,
        /// <summary>
        ///  User defined digital output X
        /// </summary>
        DIG_OUT_X = 1 << 3,

    }
}
