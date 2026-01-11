using System;
using System.Xml.Serialization;
using Common.MacroProgramming;
using Henkie.Common;
using Henkie.HSI.Board2;

namespace SimLinkup.HardwareSupport.Henk.HSI.Board2
{
    [Serializable]
    [XmlRoot("Henk_F16_HS1_Board2")]
    public class HenkieF16HSIBoard2HardwareSupportModuleConfig
    {
        [XmlArray("Devices")]
        [XmlArrayItem(nameof(Device))]
        public DeviceConfig[] Devices { get; set; }

        public static HenkieF16HSIBoard2HardwareSupportModuleConfig Load(string filePath)
        {
            return
                Common.Serialization.Util.DeserializeFromXmlFile<HenkieF16HSIBoard2HardwareSupportModuleConfig>(filePath);
        }

        public void Save(string filePath)
        {
            Common.Serialization.Util.SerializeToXmlFile(this, filePath);
        }
    }

    [Serializable]
    public class DeviceConfig
    {
        public string Address { get; set; }
        public string COMPort { get; set; }
        public ConnectionType? ConnectionType { get; set; }
        public DiagnosticLEDMode? DiagnosticLEDMode { get; set; }
        public OutputChannelsConfig OutputChannelsConfig { get; set; }
        public StatorOffsetsConfig StatorOffsetsConfig { get; set; }

        [XmlArray("CourseExciterCalibrationData")]
        [XmlArrayItem(nameof(CalibrationPoint))]
        public CalibrationPoint[] CourseExciterCalibrationData { get; set; }

        [XmlArray("CourseDeviationIndicatorCalibrationData")]
        [XmlArrayItem(nameof(CalibrationPoint))]
        public CalibrationPoint[] CourseDeviationIndicatorCalibrationData { get; set; }
    }

    [Serializable]
    public class StatorOffsetsConfig
    {
        public ushort? CourseExciterS1Offset { get; set; }
        public ushort? CourseExciterS2Offset { get; set; }
        public ushort? CourseExciterS3Offset { get; set; }
    }

    [Serializable]
    public class OutputChannelsConfig
    {
        public OutputChannelConfig DIG_OUT_A { get; set; }
        public OutputChannelConfig DIG_OUT_B { get; set; }
        public OutputChannelConfig DIG_OUT_X { get; set; }
    }

    [Serializable]
    public class OutputChannelConfig
    {
        public bool InitialValue { get; set; }
    }

}