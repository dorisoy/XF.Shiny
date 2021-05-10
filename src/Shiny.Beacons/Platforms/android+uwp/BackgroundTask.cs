﻿using System;
using System.Linq;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;
using Microsoft.Extensions.Logging;


namespace Shiny.Beacons
{
    public class BackgroundTask
    {
        readonly IBleManager bleManager;
        readonly IBeaconMonitoringManager beaconManager;
        readonly ILogger logger;
        readonly ShinyCoreServices core;
        readonly IDictionary<string, BeaconRegionStatus> states;
        IDisposable? scanSub;


        public BackgroundTask(ShinyCoreServices core,
                              IBleManager centralManager,
                              IBeaconMonitoringManager beaconManager,
                              ILogger<IBeaconMonitorDelegate> logger)
        {
            this.bleManager = centralManager;
            this.beaconManager = beaconManager;
            this.logger = logger;
            this.core = core;
            this.states = new Dictionary<string, BeaconRegionStatus>();
        }


        public void Run()
        {
            this.logger.LogInformation("Starting Beacon Monitoring");

            // I record state of the beacon region so I can fire stuff without going into initial state from unknown
            this.core
                .Bus
                .Listener<BeaconRegisterEvent>()
                .Subscribe(ev =>
                {
                    switch (ev.Type)
                    {
                        case BeaconRegisterEventType.Add:
                            if (this.states.Count == 0)
                            {
                                this.StartScan();
                            }
                            else
                            {
                                lock (this.states)
                                {
                                    this.states.Add(ev.Region.Identifier, new BeaconRegionStatus(ev.Region));
                                }
                            }
                            break;

                        case BeaconRegisterEventType.Update:
                            // this actually shouldn't be allowed
                            break;

                        case BeaconRegisterEventType.Remove:
                            lock (this.states)
                            {
                                this.states.Remove(ev.Region.Identifier);
                                if (this.states.Count == 0)
                                    this.StopScan();
                            }
                            break;

                        case BeaconRegisterEventType.Clear:
                            this.StopScan();
                            break;
                    }
                });

            this.StartScan();
            this.logger.LogInformation("Beacon Monitoring Started Successfully");
        }


        public async void StartScan()
        {
            if (this.scanSub != null)
                return;

            this.logger.LogInformation("Beacon Monitoring Scan Starting");
            var regions = await this.beaconManager.GetMonitoredRegions();
            if (!regions.Any())
                return;

            foreach (var region in regions)
                this.states.Add(region.Identifier, new BeaconRegionStatus(region));

            try
            {
                this.scanSub = this.bleManager
                    .ScanForBeacons(true)
                    .Buffer(TimeSpan.FromSeconds(5))
                    .SubscribeAsyncConcurrent(this.CheckStates);

                this.logger.LogInformation("Beacon Monitoring Scan Started Successfully");
            }
            catch (Exception ex)
            {
                this.logger.LogInformation("Beacon Monitoring Scan Starting");
            }
        }


        IList<BeaconRegionStatus> GetCopy()
        {
            lock (this.states)
                return this.states
                    .Select(x => x.Value)
                    .ToList();
        }


        public void StopScan()
        {
            if (this.scanSub == null)
                return;

            this.scanSub?.Dispose();
            this.states.Clear();
            this.scanSub = null;
            lock (this.states)
                this.states.Clear();

            this.logger.LogInformation("Beacon Monitoring Scan Stopped");
        }


        async Task CheckStates(IList<Beacon> beacons)
        {
            var copy = this.GetCopy();
            var maxAge = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(20));

            foreach (var state in copy)
            {
                foreach (var beacon in beacons)
                {
                    if (state.Region.IsBeaconInRegion(beacon))
                    {
                        state.LastPing = DateTime.UtcNow;
                        if (state.IsInRange == null)
                        {
                            state.IsInRange = true;
                        }
                        else if (!state.IsInRange.Value)
                        {
                            state.IsInRange = true;
                            if (state.Region.NotifyOnEntry)
                            {
                                await this.core.Services.RunDelegates<IBeaconMonitorDelegate>(
                                    x => x.OnStatusChanged(BeaconRegionState.Entered, state.Region)
                                );
                            }
                        }
                    }
                }
            }
            foreach (var state in copy)
            {
                if (state.IsInRange != null && state.IsInRange.Value && state.LastPing > maxAge)
                {
                    state.IsInRange = false;
                    if (state.Region.NotifyOnExit)
                    {
                        await this.core.Services.RunDelegates<IBeaconMonitorDelegate>(
                            x => x.OnStatusChanged(BeaconRegionState.Exited, state.Region)
                        );
                    }
                }
            }
        }
    }
}
