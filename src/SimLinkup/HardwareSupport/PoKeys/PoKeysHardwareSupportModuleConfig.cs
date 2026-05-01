using System;
using System.Xml.Serialization;

namespace SimLinkup.HardwareSupport.PoKeys
{
    // Multi-device PoKeys output driver config. One <Device> per physical
    // PoKeys board the user has plugged in; addressed by serial number.
    //
    // Schema:
    //   <PoKeys>
    //     <Devices>
    //       <Device>
    //         <Serial>12345</Serial>            (uint, matches PoKeysDeviceInfo.SerialNumber)
    //         <Name>cockpit-left</Name>          (optional; for friendly names in logs)
    //         <PWMPeriodMicroseconds>20000</PWMPeriodMicroseconds>
    //         <DigitalOutputs>
    //           <Output><Pin>5</Pin><Invert>true</Invert></Output>
    //           ...
    //         </DigitalOutputs>
    //         <PWMOutputs>
    //           <Output><Channel>1</Channel></Output>  (PWM1=pin17 .. PWM6=pin22)
    //           ...
    //         </PWMOutputs>
    //       </Device>
    //     </Devices>
    //   </PoKeys>
    //
    // The PWM period is per-device because the hardware shares one period
    // across all 6 PWM channels on a given board. Stored in microseconds
    // so the file is human-portable across PoKeys55 (12 MHz clock) and
    // PoKeys56/57 (25 MHz clock); the HSM converts to clock cycles at
    // apply time using PoKeysDevice.GetPWMFrequency().
    [Serializable]
    [XmlRoot("PoKeys")]
    public class PoKeysHardwareSupportModuleConfig
    {
        [XmlArray("Devices")]
        [XmlArrayItem("Device")]
        public PoKeysDeviceConfig[] Devices { get; set; }

        public static PoKeysHardwareSupportModuleConfig Load(string filePath)
        {
            return
                Common.Serialization.Util.DeserializeFromXmlFile<PoKeysHardwareSupportModuleConfig>(filePath);
        }

        public void Save(string filePath)
        {
            Common.Serialization.Util.SerializeToXmlFile(this, filePath);
        }
    }

    [Serializable]
    public class PoKeysDeviceConfig
    {
        public uint Serial { get; set; }
        public string Name { get; set; }

        // PWM period in microseconds applied to all 6 PWM channels on
        // this device. 20000 us = 20 ms = a typical RC-servo period and
        // a reasonable default for LED dimming. The hardware accepts
        // anything that fits in 32 bits when converted to clock cycles
        // at the device's PWM base frequency.
        public uint PWMPeriodMicroseconds { get; set; } = 20000;

        [XmlArray("DigitalOutputs")]
        [XmlArrayItem("Output")]
        public PoKeysDigitalOutputConfig[] DigitalOutputs { get; set; }

        [XmlArray("PWMOutputs")]
        [XmlArrayItem("Output")]
        public PoKeysPWMOutputConfig[] PWMOutputs { get; set; }
    }

    [Serializable]
    public class PoKeysDigitalOutputConfig
    {
        // 1-based pin number, 1..55. Stored 1-based to match the editor
        // UI and the silkscreen labels on the board; the HSM converts
        // to the 0-based pin ID the API expects.
        public byte Pin { get; set; }

        // When true, SetPinData is called with bit 7 of the function
        // byte set so the device inverts the pin's logical level.
        // Default true matches the convention every other SimLinkup
        // output uses (state=true => pin sourcing 3.3V), and
        // counteracts the hardware's documented "uninverted output:
        // 0 -> 3.3V, 1 -> 0V" behavior.
        public bool Invert { get; set; } = true;
    }

    [Serializable]
    public class PoKeysPWMOutputConfig
    {
        // 1-based channel number where:
        //   PWM1 = physical pin 17  (DLL channel index 5)
        //   PWM2 = physical pin 18  (DLL channel index 4)
        //   PWM3 = physical pin 19  (DLL channel index 3)
        //   PWM4 = physical pin 20  (DLL channel index 2)
        //   PWM5 = physical pin 21  (DLL channel index 1)
        //   PWM6 = physical pin 22  (DLL channel index 0)
        // The DLL uses reversed indexing (per its xmldoc); the HSM
        // applies the reversal at write time so the config and the
        // editor surface stay in human-readable order.
        public byte Channel { get; set; }
    }
}
