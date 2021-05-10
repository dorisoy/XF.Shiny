﻿using System;


namespace Shiny.Locations.Infrastructure
{
    public class GeofenceRegionStore
    {
        public string Identifier { get; set; }

        public double CenterLatitude { get; set; }
        public double CenterLongitude { get; set; }
        public double RadiusMeters { get; set; }

        public bool SingleUse { get; set; }
        public bool NotifyOnEntry { get; set; }
        public bool NotifyOnExit { get; set; }
    }
}
