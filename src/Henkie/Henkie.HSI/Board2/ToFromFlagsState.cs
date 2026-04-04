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
        To = 0x02, //documentation says this value should be 0x01, but it's wrong (flipped TO/FROM when wired to the HSI as per the documentation pins)
        /// <summary>
        ///  FROM flag visible
        /// </summary>
        From = 0x01, //documentation says this value should be 0x02, but it's wrong (flipped TO/FROM when wired to the HSI as per the documentation pins)
    }
}