using System;
using System.Runtime.InteropServices;

namespace Henkie.HSI.Board2
{
    /// <summary>
    ///   TO/FROM flags state
    /// </summary>
    [Flags]
    [ComVisible(true)]
    public enum ToFromFlagsState : byte
    {
        /// <summary>
        ///  None
        /// </summary>
        None = 0x00,
        /// <summary>
        ///  TO flag visible
        /// </summary>
        To = 0x01,
        /// <summary>
        ///  FROM flag visible
        /// </summary>
        From = 0x02,
    }
}