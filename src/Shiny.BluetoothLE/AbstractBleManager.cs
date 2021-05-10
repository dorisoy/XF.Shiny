﻿using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE
{
    public abstract class AbstractBleManager : IBleManager
    {
        public abstract IObservable<AccessState> RequestAccess();
        public abstract IObservable<ScanResult> Scan(ScanConfig? config = null);
        public abstract IObservable<AccessState> WhenStatusChanged();
        public abstract void StopScan();

        public virtual AccessState Status { get; protected set; }
        public virtual bool IsScanning { get; protected set; }
        public virtual IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid) => throw new NotImplementedException("GetKnownPeripheral is not supported on this platform");
        public virtual IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null) => throw new NotImplementedException("GetConnectedPeripherals is not supported on this platform");
    }
}