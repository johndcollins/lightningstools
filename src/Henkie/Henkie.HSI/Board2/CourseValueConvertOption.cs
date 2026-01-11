using System.Runtime.InteropServices;

namespace Henkie.HSI.Board2
{
    /// <summary>
    ///   Course value convert options
    /// </summary>
    [ComVisible(true)]
    public enum CourseValueConvertOption : byte
    {
        /// <summary>
        ///  Raw
        /// </summary>
        Raw = 0x00,
        /// <summary>
        ///  Convert to degrees
        /// </summary>
        ToDegrees = 0x01,
        /// <summary>
        ///  ADC / period
        /// </summary>
        ADCPeriod = 0x02,
    }
}