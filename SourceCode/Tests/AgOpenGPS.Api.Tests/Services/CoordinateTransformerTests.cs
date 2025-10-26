using System;
using AgOpenGPS.Api.Client.Models;
using AgOpenGPS.Api.Services;
using NUnit.Framework;

namespace AgOpenGPS.Api.Tests.Services
{
    [TestFixture]
    public class CoordinateTransformerTests
    {
        // Test origin: Minneapolis, MN area (typical precision agriculture location)
        private static readonly Wgs84Position TestOrigin = new Wgs84Position(45.0, -93.0);

        private CoordinateTransformer _transformer;

        [SetUp]
        public void SetUp()
        {
            _transformer = new CoordinateTransformer(TestOrigin);
        }

        [Test]
        public void Constructor_SetsOriginCorrectly()
        {
            // Arrange & Act
            var transformer = new CoordinateTransformer(TestOrigin);

            // Assert
            Assert.AreEqual(TestOrigin.Latitude, transformer.Origin.Latitude);
            Assert.AreEqual(TestOrigin.Longitude, transformer.Origin.Longitude);
        }

        [Test]
        public void ToLocal_OriginPoint_ReturnsZeroZero()
        {
            // Arrange
            var origin = TestOrigin;

            // Act
            var result = _transformer.ToLocal(origin);

            // Assert
            Assert.AreEqual(0.0, result.Easting, 0.001, "Easting should be 0 at origin");
            Assert.AreEqual(0.0, result.Northing, 0.001, "Northing should be 0 at origin");
        }

        [Test]
        public void ToLocal_PointNorthOfOrigin_HasPositiveNorthing()
        {
            // Arrange - point 1 degree north
            var point = new Wgs84Position(46.0, -93.0);

            // Act
            var result = _transformer.ToLocal(point);

            // Assert
            Assert.Greater(result.Northing, 110000, "Northing should be ~111km for 1 degree latitude");
            Assert.Less(result.Northing, 112000, "Northing should be ~111km for 1 degree latitude");
            Assert.AreEqual(0.0, result.Easting, 100, "Easting should be near 0 (same longitude)");
        }

        [Test]
        public void ToLocal_PointEastOfOrigin_HasPositiveEasting()
        {
            // Arrange - point 1 degree east
            var point = new Wgs84Position(45.0, -92.0);

            // Act
            var result = _transformer.ToLocal(point);

            // Assert
            Assert.Greater(result.Easting, 75000, "Easting should be ~78km for 1 degree longitude at 45° lat");
            Assert.Less(result.Easting, 80000, "Easting should be ~78km for 1 degree longitude at 45° lat");
            Assert.AreEqual(0.0, result.Northing, 100, "Northing should be near 0 (same latitude)");
        }

        [Test]
        public void ToLocal_PointSouthOfOrigin_HasNegativeNorthing()
        {
            // Arrange - point 1 degree south
            var point = new Wgs84Position(44.0, -93.0);

            // Act
            var result = _transformer.ToLocal(point);

            // Assert
            Assert.Less(result.Northing, -110000, "Northing should be negative for south");
            Assert.Greater(result.Northing, -112000, "Northing should be ~-111km");
        }

        [Test]
        public void ToLocal_PointWestOfOrigin_HasNegativeEasting()
        {
            // Arrange - point 1 degree west
            var point = new Wgs84Position(45.0, -94.0);

            // Act
            var result = _transformer.ToLocal(point);

            // Assert
            Assert.Less(result.Easting, -75000, "Easting should be negative for west");
            Assert.Greater(result.Easting, -80000, "Easting should be ~-78km");
        }

        [Test]
        public void ToWgs84_ZeroZero_ReturnsOrigin()
        {
            // Arrange
            var localOrigin = new LocalPosition(0, 0);

            // Act
            var result = _transformer.ToWgs84(localOrigin);

            // Assert
            Assert.AreEqual(TestOrigin.Latitude, result.Latitude, 0.000001, "Latitude should match origin");
            Assert.AreEqual(TestOrigin.Longitude, result.Longitude, 0.000001, "Longitude should match origin");
        }

        [Test]
        public void RoundTrip_WgsToLocalToWgs_PreservesOriginalCoordinates()
        {
            // Arrange - various test points around the origin
            var testPoints = new[]
            {
                new Wgs84Position(45.0, -93.0),      // Origin
                new Wgs84Position(45.1, -93.0),      // 0.1° North
                new Wgs84Position(45.0, -92.9),      // 0.1° East
                new Wgs84Position(44.9, -93.0),      // 0.1° South
                new Wgs84Position(45.0, -93.1),      // 0.1° West
                new Wgs84Position(45.05, -92.95),    // NE
                new Wgs84Position(44.95, -93.05),    // SW
            };

            foreach (var original in testPoints)
            {
                // Act
                var local = _transformer.ToLocal(original);
                var roundTrip = _transformer.ToWgs84(local);

                // Assert - should be accurate to sub-millimeter level
                Assert.AreEqual(original.Latitude, roundTrip.Latitude, 0.000001,
                    $"Latitude round-trip failed for {original}");
                Assert.AreEqual(original.Longitude, roundTrip.Longitude, 0.000001,
                    $"Longitude round-trip failed for {original}");
            }
        }

        [Test]
        public void RoundTrip_LocalToWgsToLocal_PreservesOriginalCoordinates()
        {
            // Arrange - various local positions
            var testPoints = new[]
            {
                new LocalPosition(0, 0),           // Origin
                new LocalPosition(1000, 0),        // 1km East
                new LocalPosition(0, 1000),        // 1km North
                new LocalPosition(-500, -500),     // SW
                new LocalPosition(10000, 5000),    // Far point
            };

            foreach (var original in testPoints)
            {
                // Act
                var wgs = _transformer.ToWgs84(original);
                var roundTrip = _transformer.ToLocal(wgs);

                // Assert - should be accurate to millimeter level
                Assert.AreEqual(original.Easting, roundTrip.Easting, 0.001,
                    $"Easting round-trip failed for {original}");
                Assert.AreEqual(original.Northing, roundTrip.Northing, 0.001,
                    $"Northing round-trip failed for {original}");
            }
        }

        [Test]
        public void ToLocal_SmallField_ProducesReasonableCoordinates()
        {
            // Arrange - simulate a 1km x 1km field
            // NE corner ~0.009° north, ~0.013° east from origin
            var neCorner = new Wgs84Position(45.009, -92.987);

            // Act
            var result = _transformer.ToLocal(neCorner);

            // Assert
            Assert.AreEqual(1000, result.Northing, 10, "Northing should be ~1000m");
            Assert.AreEqual(1000, result.Easting, 30, "Easting should be ~1000m (at 45° lat, 0.013° ≈ 1025m)");
        }

        [Test]
        public void EdgeCase_EquatorOrigin_WorksCorrectly()
        {
            // Arrange - origin at equator
            var equatorOrigin = new Wgs84Position(0.0, 0.0);
            var transformer = new CoordinateTransformer(equatorOrigin);

            // Act - point 1 degree north
            var point = new Wgs84Position(1.0, 0.0);
            var local = transformer.ToLocal(point);
            var roundTrip = transformer.ToWgs84(local);

            // Assert
            Assert.Greater(local.Northing, 110000, "Should be ~111km north");
            Assert.Less(local.Northing, 112000, "Should be ~111km north");
            Assert.AreEqual(point.Latitude, roundTrip.Latitude, 0.000001, "Round-trip should preserve latitude");
            Assert.AreEqual(point.Longitude, roundTrip.Longitude, 0.000001, "Round-trip should preserve longitude");
        }

        [Test]
        public void EdgeCase_NearNorthPole_WorksCorrectly()
        {
            // Arrange - origin at high northern latitude (northern Canada/Alaska)
            var highLatOrigin = new Wgs84Position(70.0, -100.0);
            var transformer = new CoordinateTransformer(highLatOrigin);

            // Act - point 0.1 degree east (longitude lines converge near pole)
            var point = new Wgs84Position(70.0, -99.9);
            var local = transformer.ToLocal(point);
            var roundTrip = transformer.ToWgs84(local);

            // Assert - easting should be much smaller than at lower latitudes
            // At 70° latitude, longitude degrees are only ~38km apart
            Assert.Greater(local.Easting, 3500, "Should be ~3.8km east");
            Assert.Less(local.Easting, 4000, "Should be ~3.8km east");
            Assert.AreEqual(point.Latitude, roundTrip.Latitude, 0.000001, "Round-trip should preserve latitude");
            Assert.AreEqual(point.Longitude, roundTrip.Longitude, 0.000001, "Round-trip should preserve longitude");
        }

        [Test]
        public void EdgeCase_SouthernHemisphere_WorksCorrectly()
        {
            // Arrange - origin in southern hemisphere (South America)
            var southOrigin = new Wgs84Position(-30.0, -60.0);
            var transformer = new CoordinateTransformer(southOrigin);

            // Act - point 0.1 degree in each direction
            var ne = new Wgs84Position(-29.9, -59.9);
            var local = transformer.ToLocal(ne);
            var roundTrip = transformer.ToWgs84(local);

            // Assert
            Assert.Greater(local.Northing, 0, "North should be positive northing");
            Assert.Greater(local.Easting, 0, "East should be positive easting");
            Assert.AreEqual(ne.Latitude, roundTrip.Latitude, 0.000001, "Round-trip should preserve latitude");
            Assert.AreEqual(ne.Longitude, roundTrip.Longitude, 0.000001, "Round-trip should preserve longitude");
        }

        [Test]
        public void EdgeCase_DateLineProximity_WorksCorrectly()
        {
            // Arrange - origin near International Date Line
            var dateLineOrigin = new Wgs84Position(45.0, 179.5);
            var transformer = new CoordinateTransformer(dateLineOrigin);

            // Act - points on either side of dateline are handled as separate transformers
            // (in real use, fields wouldn't span the dateline)
            var eastPoint = new Wgs84Position(45.0, 179.6);
            var local = transformer.ToLocal(eastPoint);
            var roundTrip = transformer.ToWgs84(local);

            // Assert
            Assert.Greater(local.Easting, 0, "East should be positive");
            Assert.AreEqual(eastPoint.Latitude, roundTrip.Latitude, 0.000001, "Round-trip should preserve latitude");
            Assert.AreEqual(eastPoint.Longitude, roundTrip.Longitude, 0.000001, "Round-trip should preserve longitude");
        }
    }
}
