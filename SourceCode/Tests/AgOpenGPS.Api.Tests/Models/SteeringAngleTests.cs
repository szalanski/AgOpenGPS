using NUnit.Framework;
using AgOpenGPS.Api.Client.Models;
using System;

namespace AgOpenGPS.Api.Tests.Models
{
    [TestFixture]
    public class SteeringAngleTests
    {
        [Test]
        public void Constructor_ShouldAcceptAnyValue()
        {
            // Arrange & Act
            var zero = new SteeringAngle(0);
            var positive = new SteeringAngle(45);
            var negative = new SteeringAngle(-30);
            var large = new SteeringAngle(180);

            // Assert
            Assert.AreEqual(0, zero.Degrees);
            Assert.AreEqual(45, positive.Degrees);
            Assert.AreEqual(-30, negative.Degrees);
            Assert.AreEqual(180, large.Degrees);
        }

        [Test]
        public void Zero_ShouldReturnZeroSteeringAngle()
        {
            // Act
            var zero = SteeringAngle.Zero;

            // Assert
            Assert.AreEqual(0, zero.Degrees);
        }

        [Test]
        public void ToRadians_ShouldConvertCorrectly()
        {
            // Arrange
            var angle = new SteeringAngle(180);

            // Act
            var radians = angle.ToRadians();

            // Assert
            Assert.AreEqual(Math.PI, radians, 0.0001);
        }

        [Test]
        public void AbsoluteDifferenceTo_ShouldCalculateCorrectly()
        {
            // Arrange
            var angle1 = new SteeringAngle(20);
            var angle2 = new SteeringAngle(-10);

            // Act
            var diff = angle1.AbsoluteDifferenceTo(angle2);

            // Assert
            Assert.AreEqual(30, diff, 0.0001);
        }

        [Test]
        public void Equality_ShouldWorkWithRecordSemantics()
        {
            // Arrange
            var angle1 = new SteeringAngle(25);
            var angle2 = new SteeringAngle(25);
            var angle3 = new SteeringAngle(30);

            // Assert
            Assert.AreEqual(angle1, angle2);
            Assert.AreNotEqual(angle1, angle3);
            Assert.IsTrue(angle1 == angle2);
            Assert.IsTrue(angle1 != angle3);
        }

        [Test]
        public void ToString_ShouldFormatCorrectly()
        {
            // Arrange
            var right = new SteeringAngle(20);
            var left = new SteeringAngle(-15);
            var straight = new SteeringAngle(0);

            // Act & Assert
            Assert.AreEqual("20.0° R", right.ToString());
            Assert.AreEqual("15.0° L", left.ToString());
            Assert.AreEqual("0.0° straight", straight.ToString());
        }

        [Test]
        public void InitProperty_ShouldAllowImmutableUpdate()
        {
            // Arrange
            var original = new SteeringAngle(10);

            // Act
            var updated = original with { Degrees = 20 };

            // Assert
            Assert.AreEqual(10, original.Degrees);
            Assert.AreEqual(20, updated.Degrees);
        }
    }
}
