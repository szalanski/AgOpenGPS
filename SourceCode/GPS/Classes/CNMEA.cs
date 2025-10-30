using AgOpenGPS.Core;
using AgOpenGPS.Core.Models;
using AgOpenGPS.Api.Client.Models;
using System;
using System.Globalization;
using System.Text;

namespace AgOpenGPS
{
    /// <summary>
    /// CNMEA - Legacy GPS data container (partial migration complete)
    ///
    /// STATUS: Adapter pattern removed (Workflow 005), but class retained as working variable container
    ///
    /// MIGRATION HISTORY:
    /// - ✅ Workflow 002: Backend now owns GPS processing via GnssService
    /// - ✅ Workflow 005: UpdateFromBackendState() adapter removed
    /// - ✅ Position.UpdateFixPosition() now reads directly from _cachedState.Gnss
    ///
    /// CURRENT USAGE:
    /// - fix: Working variable for position calculations (initialized from backend state)
    /// - speed, altitude, heading fields: Still read throughout codebase (GUI labels, OpenGL rendering)
    /// - DefineLocalPlane(): Domain logic for coordinate transformation (7+ call sites)
    /// - AverageTheSpeed(): Speed filtering logic
    ///
    /// TODO (Future Workflows):
    /// - Migrate DefineLocalPlane() to backend CoordinateTransformService
    /// - Replace pn.altitude/hdop/age reads with _cachedState.Gnss.* reads in UI code
    /// - Move speed averaging to backend or vehicle state service
    /// - Eventually delete this class entirely when all domain logic migrated
    /// </summary>
    public class CNMEA
    {
        //our current fix (working variable, initialized from backend state in UpdateFixPosition)
        public vec2 fix = new vec2(0, 0);

        //other GIS Info (TODO: Replace reads with _cachedState.Gnss.* throughout codebase)
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

                Properties.Settings.Default.setGPS_SimLatitude = mf.AppModel.LocalPlane.Origin.Latitude;
                Properties.Settings.Default.setGPS_SimLongitude = mf.AppModel.LocalPlane.Origin.Longitude;
                Properties.Settings.Default.Save();
            }
            GeoCoord geoCoord = mf.AppModel.LocalPlane.ConvertWgs84ToGeoCoord(mf.AppModel.CurrentLatLon);
            mf.worldGrid.checkZoomWorldGrid(geoCoord);
        }

    }
}