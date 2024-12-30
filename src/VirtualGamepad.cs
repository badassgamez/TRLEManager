/*
    Copyright © badassgamez Ltd.

    Author: badassgamez
    Created:    11.22.2024

    License: MIT
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

using Timer = System.Threading.Timer;

namespace TRLEManager
{
    public enum VirtualGamepadButton
    {
        X,
        A,
        B,
        Y,
        L1,
        R1,
        L2,
        R2,
        Start,
        Menu,
        L3,
        R3,
        Aux1,
        Aux2,
        Aux3,
        Aux4,
        Aux5,
        HatUp,
        HatRight,
        HatDown,
        HatLeft,
        StartShiftedX,
        StartShiftedA,
        StartShiftedB,
        StartShiftedY,
        StartShiftedL1,
        StartShiftedR1,
        StartShiftedL2,
        StartShiftedR2,
        MenuShiftedX,
        MenuShiftedA,
        MenuShiftedB,
        MenuShiftedY,
        MenuShiftedL1,
        MenuShiftedR1,
        MenuShiftedL2,
        MenuShiftedR2,
    }

    public struct ButtonReport
    {
        public VirtualGamepadButton button;
        public bool down;
    }

    public class VirtualGamepadChangedEventArgs
    {
        public ButtonReport[] changeReport;
        public ushort changeReportReleventLength;

        public BitArray state;
    }

    public class VirtualGamepad
    {
        Gamepad _gamepad;
        BitArray _state;
        VirtualGamepadChangedEventArgs _eventBuffer;
        SynchronizationContext _syncContext;

        VirtualGamepadButton[] _gamepadMap;
                
        bool _startShiftConsumed;
        bool _menuShiftConsumed;

        public event EventHandler<VirtualGamepadChangedEventArgs> OnVirtualChanged;

        public Gamepad BaseGamepad { get { return _gamepad; } }
        public BitArray ButtonValues { get { return _state; } }

        public VirtualGamepad(Gamepad baseGamepad)
        {
            _gamepad = baseGamepad;
            _eventBuffer = new VirtualGamepadChangedEventArgs();
            
            var numberVirtualButtons = Enum.GetValues(typeof(VirtualGamepadButton)).Length;
            _eventBuffer.changeReport = new ButtonReport[numberVirtualButtons];

            _state = new BitArray(numberVirtualButtons);
            _eventBuffer.state = _state;
            
            _startShiftConsumed = false;
            _menuShiftConsumed = false;

            baseGamepad.OnGamepadChanged += Gamepad_OnGamepadChanged;

            RepopulateMap();
        }

        public void RepopulateMap()
        {
            _gamepadMap = App.GetGamepadMapping();
        }

        public void StartMonitor()
        {
            if (_syncContext != null)
                return;

            _syncContext = SynchronizationContext.Current;
            _gamepad.StartMonitor();
        }

        public void StopMonitor()
        {
            if (_syncContext == null)
                return;

            _syncContext = null;
            _gamepad.StopMonitor();
        }

        private void PushReport(VirtualGamepadButton button, bool down)
        {
            ref ButtonReport thisReport = ref _eventBuffer.changeReport[_eventBuffer.changeReportReleventLength++];
            thisReport.button = button;
            thisReport.down = down;
        }

        void SendKeyTimerExec(object buttonReportWrapped)
        {
            if (_syncContext == null)
                return;

            _syncContext.Post(s => {
                _eventBuffer.changeReportReleventLength = 1;
                _eventBuffer.changeReport[0] = (ButtonReport)buttonReportWrapped;
                ref ButtonReport buttonReportAlias = ref _eventBuffer.changeReport[0];

                _state[(int)buttonReportAlias.button] = buttonReportAlias.down;

                try
                {
                    OnVirtualChanged?.Invoke(this, _eventBuffer);
                }
                catch (Exception e)
                {
                    Error.LogException(e);
                }
            }, buttonReportWrapped);
        }
        
        private readonly Dictionary<VirtualGamepadButton, Tuple<VirtualGamepadButton?, VirtualGamepadButton?>> _shiftMap = new Dictionary<VirtualGamepadButton, Tuple<VirtualGamepadButton?, VirtualGamepadButton?>>() 
        {
            { VirtualGamepadButton.X, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedX, VirtualGamepadButton.MenuShiftedX) },
            { VirtualGamepadButton.A, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedA, VirtualGamepadButton.MenuShiftedA) },
            { VirtualGamepadButton.B, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedB, VirtualGamepadButton.MenuShiftedB) },
            { VirtualGamepadButton.Y, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedY, VirtualGamepadButton.MenuShiftedY) },
            { VirtualGamepadButton.L1, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedL1, VirtualGamepadButton.MenuShiftedL1) },
            { VirtualGamepadButton.R1, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedR1, VirtualGamepadButton.MenuShiftedR1) },
            { VirtualGamepadButton.L2, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedL2, VirtualGamepadButton.MenuShiftedL2) },
            { VirtualGamepadButton.R2, new Tuple<VirtualGamepadButton?, VirtualGamepadButton?>(VirtualGamepadButton.StartShiftedR2, VirtualGamepadButton.MenuShiftedR2) },
        };

        private void ProcessButtonChange(VirtualGamepadButton button, bool buttonState)
        {
            if (!_shiftMap.TryGetValue(button, out var shiftTuple))
            {
                PushReport(button, buttonState);
                return;
            }

            (VirtualGamepadButton? startShiftButton, VirtualGamepadButton? menuShiftButton) = shiftTuple;

            if (startShiftButton.HasValue
                && ((_state[(int)VirtualGamepadButton.Start] && buttonState)
                || (_state[(int)startShiftButton.Value] && !buttonState)))
            {
                if (buttonState)
                    _startShiftConsumed = true;

                _state[(int)startShiftButton.Value] = buttonState;
                PushReport(startShiftButton.Value, buttonState);
            }
            else if (menuShiftButton.HasValue
                && ((_state[(int)VirtualGamepadButton.Menu] && buttonState)
                || (_state[(int)menuShiftButton.Value] && !buttonState)))
            {
                if (buttonState)
                    _menuShiftConsumed = true;

                _state[(int)menuShiftButton.Value] = buttonState;
                PushReport(menuShiftButton.Value, buttonState);
            }
            else
            {
                PushReport(button, buttonState);
            }
        }

        private void Gamepad_OnGamepadChanged(object sender, GamepadChangedEventArgs e)
        {
            BitArray buttons = _gamepad.ButtonValues;
            uint dpad = _gamepad.HatSwitchValue;

            _eventBuffer.changeReportReleventLength = 0;

            for (uint i = 0; i < e.buttonChangeCount; i++)
            {
                var physicalButtonChanged = e.buttonChanges[i];
                var virtualButtonChanged = _gamepadMap[physicalButtonChanged];
                var buttonState = buttons[physicalButtonChanged];

                _state[(int)virtualButtonChanged] = buttonState;

                switch (virtualButtonChanged)
                {
                    case VirtualGamepadButton.Start:
                        if (!buttonState)
                        {
                            if (_startShiftConsumed)
                            {
                                _startShiftConsumed = false;
                                break;
                            }

                            PushReport(VirtualGamepadButton.Start, true);
                            Timer tmr = new Timer(SendKeyTimerExec, 
                                new ButtonReport { button = VirtualGamepadButton.Start, down = false }, 30, Timeout.Infinite);
                        }

                        break;

                    case VirtualGamepadButton.Menu:
                        if (!buttonState)
                        {
                            if (_menuShiftConsumed)
                            {
                                _menuShiftConsumed = false;
                                break;
                            }

                            PushReport(VirtualGamepadButton.Menu, true);
                            Timer tmr = new Timer(SendKeyTimerExec,
                                new ButtonReport { button = VirtualGamepadButton.Menu, down = false }, 30, Timeout.Infinite);
                        }
                        break;

                    case VirtualGamepadButton.A:
                    case VirtualGamepadButton.B:
                    case VirtualGamepadButton.X:
                    case VirtualGamepadButton.Y:
                    case VirtualGamepadButton.L1:
                    case VirtualGamepadButton.R1:
                    case VirtualGamepadButton.L2:
                    case VirtualGamepadButton.R2:
                    case VirtualGamepadButton.L3:
                    case VirtualGamepadButton.R3:
                        ProcessButtonChange(virtualButtonChanged, buttonState);
                        break;
                }
            }

            if (e.prevDpad == dpad)
            {
                if (_eventBuffer.changeReportReleventLength > 0)
                    OnVirtualChanged?.Invoke(this, _eventBuffer);

                return;
            }
            

            if ((dpad & 0x1) > 0 && (e.prevDpad & 0x1) == 0)
                PushReport(VirtualGamepadButton.HatUp, true);
            else if ((dpad & 0x1) == 0 && (e.prevDpad & 0x1) > 0)
                PushReport(VirtualGamepadButton.HatUp, false);

            if ((dpad & 0x2) > 0 && (e.prevDpad & 0x2) == 0)
                PushReport(VirtualGamepadButton.HatRight, true);
            else if ((dpad & 0x2) == 0 && (e.prevDpad & 0x2) > 0)
                PushReport(VirtualGamepadButton.HatRight, false);

            if ((dpad & 0x4) > 0 && (e.prevDpad & 0x4) == 0)
                PushReport(VirtualGamepadButton.HatDown, true);
            else if ((dpad & 0x4) == 0 && (e.prevDpad & 0x4) > 0)
                PushReport(VirtualGamepadButton.HatDown, false);

            if ((dpad & 0x8) > 0 && (e.prevDpad & 0x8) == 0)
                PushReport(VirtualGamepadButton.HatLeft, true);
            else if ((dpad & 0x8) == 0 && (e.prevDpad & 0x8) > 0)
                PushReport(VirtualGamepadButton.HatLeft, false);

            if (_eventBuffer.changeReportReleventLength > 0)
                OnVirtualChanged?.Invoke(this, _eventBuffer);
        }
    }
}
