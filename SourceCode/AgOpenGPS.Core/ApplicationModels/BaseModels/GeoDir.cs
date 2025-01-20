using System;

namespace AgOpenGPS.Core.ApplicationModels
{
    public struct GeoDir
    {
        private double _angle;
        public GeoDir(double angle)
        {
            _angle = angle;
        }

        public GeoDir(GeoDelta delta)
        {
            _angle = Math.Atan2(delta.EastingDelta, delta.NorthingDelta);
        }

        public double Angle => _angle;
        public double Cos => Math.Cos(_angle);
        public double Sin => Math.Sin(_angle);

        public GeoDir PerpendicularLeft => new GeoDir(_angle - 0.5 * Math.PI);

        public GeoDir PerpendicularRight => new GeoDir(_angle + 0.5 * Math.PI);
        public GeoDir Inverted => new GeoDir(_angle + Math.PI);


        public void Invert()
        {
            _angle += Math.PI;
        }

        public static GeoDelta operator *(double distance, GeoDir dir)
        {
            return new GeoDelta(distance * dir.Cos, distance * dir.Sin);
        }

        public static GeoDir operator +(GeoDir dir, double angle)
        {
            return new GeoDir(dir._angle + angle);
        }

        public static GeoDir operator -(GeoDir dir, double angle)
        {
            return new GeoDir(dir._angle - angle);
        }

    }
}