using AgOpenGPS.Core.Models;
using NUnit.Framework;

namespace AgOpenGPS.Core.Tests.Models
{
    public class Wgs84Tests
    {
        [Test]
        public void Test_RegionalDistance()
        {
            // Arrange
            Wgs84 amsterdam = new Wgs84(52.377956, 4.897070);
            Wgs84 denDungen = new Wgs84(51.665, 5.37222);

            // Act
            double distance = amsterdam.Distance(denDungen);
            double distanceBack = denDungen.Distance(amsterdam);

            // Assert
            Assert.That(distance, Is.GreaterThan(80000));
            Assert.That(distance, Is.LessThan(90000));

            Assert.That(distance, Is.EqualTo(distanceBack));
        }

        [Test]
        public void Test_LongDistance()
        {
            // Arrange
            Wgs84 amsterdam = new Wgs84(52.377956, 4.897070);
            Wgs84 opposite = new Wgs84(-52.377956, 4.897070 + 180);

            // Act
            double distance = amsterdam.Distance(opposite);
            double distanceBack = opposite.Distance(amsterdam);

            // Assert
            Assert.That(distance, Is.GreaterThan(19 * 1000 * 1000));
            Assert.That(distance, Is.LessThan(21 * 1000 * 1000));

            Assert.That(distance, Is.EqualTo(distanceBack));
        }
    }
}
