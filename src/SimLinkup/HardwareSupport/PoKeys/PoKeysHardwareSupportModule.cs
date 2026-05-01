using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.HardwareSupport;
using Common.MacroProgramming;
using log4net;
using PoKeysDevice_DLL;

namespace SimLinkup.HardwareSupport.PoKeys
{
    // Output-driver HSM for PoLabs PoKeys boards. v1 covers digital pin
    // outputs (1..55) and PWM channel outputs (1..6, on physical pins
    // 17..22). USB only — Ethernet PoKeys are out of scope for now.
    //
    // Multi-device by serial: GetInstances() returns one HSM per
    // <Device> in PoKeysHardwareSupportModule.config that successfully
    // matches an enumerated USB device. Boards that are listed in the
    // config but not currently plugged in are logged and skipped, so
    // an unrelated missing PoKeys doesn't break the whole driver.
    //
    // Out of scope for v1: matrix LED, LCD, PoExtBus, PoNET, digital
    // counters, encoders, analog inputs, Ethernet/network discovery.
    // The DLL surfaces all of these — adding them is mostly a matter
    // of declaring extra signal arrays and wiring more SignalChanged
    // handlers in the same shape as digital + PWM.
    public class PoKeysHardwareSupportModule : HardwareSupportModuleBase, IDisposable
    {
        private static readonly ILog _log = LogManager.GetLogger(typeof(PoKeysHardwareSupportModule));

        // Static, shared across HSM instances. The DLL is implemented
        // such that one PoKeysDevice instance per process can reach
        // any device, so we use a single helper for enumeration. Per-
        // instance device handles are obtained via separate
        // PoKeysDevice objects, one per HSM (each calls ConnectToDevice
        // and DisconnectDevice on its own).
        private readonly PoKeysDeviceConfig _deviceConfig;
        private PoKeysDevice _device;
        private bool _isConnected;
        private bool _isDisposed;

        // Period in clock cycles that we computed at Initialize time
        // from the configured PWMPeriodMicroseconds and the device's
        // GetPWMFrequency() (12 MHz on PoKeys55, 25 MHz on PoKeys56/57).
        // Stored so the HSM can re-emit it on demand and so PWM duty
        // calculations can scale a 0..1 input to [0..period] cycles.
        private uint _pwmPeriodCycles;

        // Live PWM duty cycle cache, indexed by DLL channel slot (0..5).
        // We keep the full 6-slot array because SetPWMOutputsFast takes
        // all 6 at once, even if the user only configured a subset of
        // channels. Unused channels stay at 0 — harmless.
        private uint[] _pwmDuty = new uint[6];

        // Mask of which DLL channel slots were enabled in the
        // SetPWMOutputs initialisation call. Used so SignalChanged for
        // an unconfigured channel (defensive — shouldn't happen) can
        // refuse to clobber a slot the device thinks is disabled.
        private bool[] _pwmEnabled = new bool[6];

        private DigitalSignal[] _digitalOutputSignals;
        private AnalogSignal[] _pwmOutputSignals;

        private PoKeysHardwareSupportModule(PoKeysDeviceConfig deviceConfig)
        {
            _deviceConfig = deviceConfig;
        }

        public override AnalogSignal[] AnalogOutputs => _pwmOutputSignals;
        public override DigitalSignal[] DigitalOutputs => _digitalOutputSignals;

        public override string FriendlyName
        {
            get
            {
                var label = string.IsNullOrWhiteSpace(_deviceConfig?.Name)
                    ? $"serial {_deviceConfig?.Serial}"
                    : $"\"{_deviceConfig.Name}\" (serial {_deviceConfig.Serial})";
                return $"PoKeys {label}";
            }
        }

        public static IHardwareSupportModule[] GetInstances()
        {
            var toReturn = new List<IHardwareSupportModule>();
            try
            {
                var hsmConfigFilePath = Path.Combine(
                    Util.CurrentMappingProfileDirectory,
                    "PoKeysHardwareSupportModule.config");
                if (!File.Exists(hsmConfigFilePath))
                {
                    _log.InfoFormat("No PoKeys config at {0}; PoKeys driver inactive for this profile.",
                        hsmConfigFilePath);
                    return toReturn.ToArray();
                }
                var hsmConfig = PoKeysHardwareSupportModuleConfig.Load(hsmConfigFilePath);
                if (hsmConfig == null || hsmConfig.Devices == null || hsmConfig.Devices.Length == 0)
                {
                    return toReturn.ToArray();
                }

                foreach (var deviceConfig in hsmConfig.Devices)
                {
                    if (deviceConfig == null) continue;
                    try
                    {
                        var hsm = new PoKeysHardwareSupportModule(deviceConfig);
                        if (hsm.Initialize())
                        {
                            toReturn.Add(hsm);
                        }
                        else
                        {
                            // Initialize logs the specific failure (no enum match,
                            // connection refused, etc.). Don't treat one missing
                            // PoKeys as fatal — other declared boards may still
                            // be plugged in and usable.
                            hsm.Dispose();
                        }
                    }
                    catch (Exception perDeviceException)
                    {
                        _log.Error(perDeviceException.Message, perDeviceException);
                    }
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
            return toReturn.ToArray();
        }

        // Connect to the configured PoKeys by serial, configure pin
        // functions for every declared digital output, prime the PWM
        // hardware if any PWM outputs are declared, and create the
        // signal arrays the runtime will publish. Returns false if
        // the device couldn't be reached so GetInstances can skip it
        // cleanly without registering a half-initialized HSM.
        private bool Initialize()
        {
            _device = new PoKeysDevice();
            // Discover. Extended info on so SerialNumber is populated;
            // USB on, Ethernet off (v1 scope); default discovery timeout
            // (parameter is ignored when ethernet=false but the API
            // requires the int).
            var discovered = _device.EnumeratePoKeysDevices(true, true, false, 0);
            if (discovered == null || discovered.Count == 0)
            {
                _log.WarnFormat("No PoKeys devices found on USB; cannot bind serial {0}.",
                    _deviceConfig.Serial);
                return false;
            }
            // Match the configured serial. If two boards happen to
            // enumerate in different order across runs, we still pick
            // the right one because the lookup is by serial, not index.
            PoKeysDeviceInfo target = null;
            foreach (var info in discovered)
            {
                if (info != null && (uint)info.SerialNumber == _deviceConfig.Serial)
                {
                    target = info;
                    break;
                }
            }
            if (target == null)
            {
                _log.WarnFormat(
                    "PoKeys with serial {0} declared in config but not currently plugged in. " +
                    "Skipping this device — other PoKeys boards (if any) will still be initialised.",
                    _deviceConfig.Serial);
                return false;
            }

            if (!_device.ConnectToDevice(target))
            {
                _log.ErrorFormat("Failed to connect to PoKeys serial {0}.", _deviceConfig.Serial);
                return false;
            }
            _isConnected = true;

            ConfigureDigitalOutputs();
            ConfigurePWMOutputs();

            _digitalOutputSignals = CreateDigitalOutputSignals();
            _pwmOutputSignals = CreatePWMOutputSignals();

            _log.InfoFormat(
                "PoKeys {0} initialised: {1} digital output{2}, {3} PWM channel{4}, period={5} us.",
                FriendlyName,
                _digitalOutputSignals.Length, _digitalOutputSignals.Length == 1 ? "" : "s",
                _pwmOutputSignals.Length, _pwmOutputSignals.Length == 1 ? "" : "s",
                _deviceConfig.PWMPeriodMicroseconds);
            return true;
        }

        // For each declared digital output, write a SetPinData call so
        // the pin is in digital-output mode with the configured invert
        // bit. After power-up the device defaults all pins to digital
        // input for safety, so we MUST flip every pin we plan to drive.
        private void ConfigureDigitalOutputs()
        {
            if (_deviceConfig.DigitalOutputs == null) return;
            // Bit 2 = digital output, bit 7 = invert state (per the
            // SetPinData xmldoc). We always force the invert bit per
            // the config field's value rather than reading whatever
            // the device's flash currently holds — that way PoKeys
            // saved with the vendor's tool to a different invert
            // setting still behave deterministically under SimLinkup.
            const byte digitalOutputBit = 0x04;
            const byte invertBit = 0x80;
            foreach (var pinCfg in _deviceConfig.DigitalOutputs)
            {
                if (pinCfg == null) continue;
                if (pinCfg.Pin < 1 || pinCfg.Pin > 55)
                {
                    _log.WarnFormat("PoKeys serial {0}: digital output pin {1} out of range (1..55); skipping.",
                        _deviceConfig.Serial, pinCfg.Pin);
                    continue;
                }
                var pinId0 = (byte)(pinCfg.Pin - 1);
                var function = (byte)(digitalOutputBit | (pinCfg.Invert ? invertBit : 0));
                try
                {
                    _device.SetPinData(pinId0, function);
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }
            }
        }

        // Initialise the device's PWM block once with the configured
        // period and zero duty across all 6 channels. After this,
        // signal-changed handlers use SetPWMOutputsFast (duty-only) on
        // the hot path so the period byte doesn't get re-sent on every
        // tick. PWMOutputs in config can be empty — that's fine, we
        // skip the whole block to avoid spurious USB traffic.
        private void ConfigurePWMOutputs()
        {
            if (_deviceConfig.PWMOutputs == null || _deviceConfig.PWMOutputs.Length == 0) return;

            // Build the channel-enable mask. The DLL uses reversed
            // indexing (PWM1=pin17 = channel 5; PWM6=pin22 = channel 0),
            // so the config's 1-based PWM channel maps to (6 - n).
            for (var i = 0; i < 6; i++) _pwmEnabled[i] = false;
            foreach (var pwmCfg in _deviceConfig.PWMOutputs)
            {
                if (pwmCfg == null) continue;
                if (pwmCfg.Channel < 1 || pwmCfg.Channel > 6)
                {
                    _log.WarnFormat("PoKeys serial {0}: PWM channel {1} out of range (1..6); skipping.",
                        _deviceConfig.Serial, pwmCfg.Channel);
                    continue;
                }
                _pwmEnabled[6 - pwmCfg.Channel] = true;
            }

            // Convert the configured period from microseconds to
            // device clock cycles. GetPWMFrequency returns 12e6 for
            // PoKeys55 and 25e6 for PoKeys56+; either way one
            // microsecond costs (frequency / 1e6) cycles. We compute
            // in double then narrow to uint, with a defensive clamp
            // against pathological huge configured periods.
            var freqHz = _device.GetPWMFrequency();
            var cyclesDouble = (double)_deviceConfig.PWMPeriodMicroseconds * (freqHz / 1e6);
            if (cyclesDouble < 1) cyclesDouble = 1;
            if (cyclesDouble > uint.MaxValue) cyclesDouble = uint.MaxValue;
            _pwmPeriodCycles = (uint)cyclesDouble;

            for (var i = 0; i < 6; i++) _pwmDuty[i] = 0;

            try
            {
                _device.SetPWMOutputs(ref _pwmEnabled, ref _pwmPeriodCycles, ref _pwmDuty);
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private DigitalSignal[] CreateDigitalOutputSignals()
        {
            if (_deviceConfig.DigitalOutputs == null) return new DigitalSignal[0];
            var list = new List<DigitalSignal>();
            foreach (var pinCfg in _deviceConfig.DigitalOutputs)
            {
                if (pinCfg == null) continue;
                if (pinCfg.Pin < 1 || pinCfg.Pin > 55) continue;
                var signal = new DigitalSignal
                {
                    Category = "Outputs",
                    CollectionName = "Digital Pins",
                    FriendlyName = $"Digital pin {pinCfg.Pin}",
                    Id = $"PoKeys[{_deviceConfig.Serial}]__DIGITAL_PIN[{pinCfg.Pin}]",
                    Index = pinCfg.Pin,
                    PublisherObject = this,
                    Source = _device,
                    SourceFriendlyName = FriendlyName,
                    SourceAddress = _deviceConfig.Serial.ToString(),
                    SubSource = pinCfg.Pin,
                    SubSourceFriendlyName = $"Pin {pinCfg.Pin}",
                    SubSourceAddress = null,
                    State = false
                };
                signal.SignalChanged += DigitalOutputSignalChanged;
                list.Add(signal);
            }
            return list.ToArray();
        }

        private AnalogSignal[] CreatePWMOutputSignals()
        {
            if (_deviceConfig.PWMOutputs == null) return new AnalogSignal[0];
            var list = new List<AnalogSignal>();
            foreach (var pwmCfg in _deviceConfig.PWMOutputs)
            {
                if (pwmCfg == null) continue;
                if (pwmCfg.Channel < 1 || pwmCfg.Channel > 6) continue;
                // PWM channels surface as 0..1 fractional duty cycle
                // — this matches how every other "analog-like" output
                // in SimLinkup is normalised (gauges and resolvers
                // work in -10..+10 V; PoKeys PWM is genuinely a duty
                // ratio, so 0..1 is the natural unit). The handler
                // converts to clock cycles at write time using the
                // cached _pwmPeriodCycles.
                var pin = 16 + pwmCfg.Channel; // PWM1 -> 17, PWM6 -> 22
                var signal = new AnalogSignal
                {
                    Category = "Outputs",
                    CollectionName = "PWM Channels",
                    FriendlyName = $"PWM{pwmCfg.Channel} (pin {pin})",
                    Id = $"PoKeys[{_deviceConfig.Serial}]__PWM[{pwmCfg.Channel}]",
                    Index = pwmCfg.Channel,
                    PublisherObject = this,
                    Source = _device,
                    SourceFriendlyName = FriendlyName,
                    SourceAddress = _deviceConfig.Serial.ToString(),
                    SubSource = pwmCfg.Channel,
                    SubSourceFriendlyName = $"PWM{pwmCfg.Channel}",
                    SubSourceAddress = null,
                    State = 0,
                    MinValue = 0,
                    MaxValue = 1,
                    IsPercentage = true
                };
                signal.SignalChanged += PWMOutputSignalChanged;
                list.Add(signal);
            }
            return list.ToArray();
        }

        private void DigitalOutputSignalChanged(object sender, DigitalSignalChangedEventArgs args)
        {
            if (!_isConnected || _device == null) return;
            var signal = sender as DigitalSignal;
            if (signal == null || !signal.Index.HasValue) return;
            var pin = signal.Index.Value;
            if (pin < 1 || pin > 55) return;
            try
            {
                _device.SetOutput((byte)(pin - 1), args.CurrentState);
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        private void PWMOutputSignalChanged(object sender, AnalogSignalChangedEventArgs args)
        {
            if (!_isConnected || _device == null) return;
            var signal = sender as AnalogSignal;
            if (signal == null || !signal.Index.HasValue) return;
            var channel = signal.Index.Value;
            if (channel < 1 || channel > 6) return;
            // Clamp to [0,1] so a stray out-of-range input doesn't
            // produce a wraparound on the uint cast below.
            var fraction = args.CurrentState;
            if (fraction < 0) fraction = 0;
            if (fraction > 1) fraction = 1;
            var dutyCycles = (uint)(fraction * _pwmPeriodCycles);
            // Reverse-index for the DLL: PWM1 (config) -> slot 5 (DLL).
            _pwmDuty[6 - channel] = dutyCycles;
            try
            {
                _device.SetPWMOutputsFast(ref _pwmDuty);
            }
            catch (Exception e)
            {
                _log.Error(e.Message, e);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~PoKeysHardwareSupportModule()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed) return;
            if (disposing)
            {
                try
                {
                    if (_device != null && _isConnected)
                    {
                        _device.DisconnectDevice();
                    }
                }
                catch (Exception e)
                {
                    _log.Error(e.Message, e);
                }
                _isConnected = false;
                _device = null;
            }
            _isDisposed = true;
        }
    }
}
