namespace CAN_Tool.Libs.CanAdapters
{
    // The org's own cellular modem (modem_timberline), acting as a transparent SLCAN
    // bridge over its USB-CDC port (see SlcanBridge.cpp on that side) - same wire
    // protocol as any other SLCAN adapter, but sharing its MCU with a full AT-command/
    // MQTT/HTTP stack instead of being a dedicated CAN dongle, so it can't absorb a
    // back-to-back burst the way real hardware (VSComDriver's own adapter, a proper
    // CANable-class dongle) can. Confirmed on real hardware 2026-09-02: flashing over
    // the modem's bridge without any inter-frame pacing overwhelmed the *target*
    // device's own CAN receive handling (same reason the org's built-in CAN-relay
    // firmware paces its own fragment transfer at ~3ms/frame) - this restores the
    // delay table that existed before (SlcanDriver's own history), just scoped to only
    // this adapter type instead of applied to every SLCAN-compatible device.
    public class ModemSlcanDriver : SlcanDriver
    {
        protected override int GetDelay() => _speed switch
        {
            0 => 20,
            1 => 8,
            2 => 4,
            3 => 2,
            _ => 1
        };
    }
}
