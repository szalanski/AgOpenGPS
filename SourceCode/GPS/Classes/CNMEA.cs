using AgOpenGPS.Core;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Api.Client.Models;
using System;
using System.Globalization;
using System.Text;

namespace AgOpenGPS
{
    public class CNMEA
    {
        //our current fix
        public vec2 fix = new vec2(0, 0);

        //other GIS Info
        public double altitude, speed, vtgSpeed = float.MaxValue;

        public double headingTrueDual, headingTrue, hdop, age, headingTrueDualOffset;

        public int fixQuality, ageAlarm;
        public int satellitesTracked;

        private readonly FormGPS mf;

        public CNMEA(FormGPS f)
        {
            //constructor, grab the main form reference
            mf = f;
            mf.AppModel.LocalPlane = new LocalPlane(new Wgs84(0, 0), mf.AppModel.SharedFieldProperties);
            ageAlarm = Properties.Settings.Default.setGPS_ageAlarm;
        }

        /// <summary>
        /// Updates legacy CNMEA fields from backend ApplicationState.
        /// Adapter pattern: translates backend GPS data to legacy field references.
        /// </summary>
        public void UpdateFromBackendState(ApplicationState state)
        {
            if (state?.Gnss == null)
            {
                return;
            }

            // Map backend GNSS state to legacy fields
            fix.easting = state.Gnss.LocalPosition.Easting;
            fix.northing = state.Gnss.LocalPosition.Northing;

            speed = state.Gnss.Speed.KilometersPerHour;
            vtgSpeed = state.Gnss.Speed.KilometersPerHour;

            altitude = state.Gnss.Altitude.Meters;

            headingTrue = state.Gnss.HeadingSingle.Degrees;
            headingTrueDual = state.Gnss.HeadingDual.Degrees;

            fixQuality = state.Gnss.Quality.FixQuality;
            satellitesTracked = state.Gnss.Quality.SatellitesTracked;
            hdop = state.Gnss.Quality.Hdop;
            age = state.Gnss.Quality.Age;
        }

        public void AverageTheSpeed()
        {
            //average the speed
            //if (speed > 70) speed = 70;
            mf.avgSpeed = (mf.avgSpeed * 0.75) + (speed * 0.25);
        }

        public void DefineLocalPlane(Wgs84 origin, bool setSim)
        {
            mf.AppModel.LocalPlane = new LocalPlane(origin, mf.AppModel.SharedFieldProperties);
            if (setSim && mf.timerSim.Enabled)
            {
                mf.AppModel.CurrentLatLon = origin;
                mf.sim.CurrentLatLon = origin;

                Properties.Settings.Default.setGPS_SimLatitude = mf.AppModel.LocalPlane.Origin.Latitude;
                Properties.Settings.Default.setGPS_SimLongitude = mf.AppModel.LocalPlane.Origin.Longitude;
                Properties.Settings.Default.Save();
            }
            GeoCoord geoCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(mf.AppModel.CurrentLatLon);
            mf.worldGrid.checkZoomWorldGrid(geoCoord);
        }

    }
}