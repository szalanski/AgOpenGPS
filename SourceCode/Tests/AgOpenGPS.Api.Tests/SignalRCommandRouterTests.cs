using System;
using AgOpenGPS.Api.Client.Commands;
using AgOpenGPS.Api.Client.SignalR;

namespace AgOpenGPS.Api.Tests
{
    public class SignalRCommandRouterTests
    {
        private static SignalRCommandRouter CreateRouter()
            => new SignalRCommandRouter(new[] { typeof(SignalRCommandRouterTests).Assembly });

        [Test]
        public void GetHubMethodForCommand_ShouldTrimCommandSuffix()
        {
            var router = CreateRouter();

            var name = router.GetHubMethodForCommand(typeof(DefaultNameCommand));

            Assert.That(name, Is.EqualTo("DefaultName"));
        }

        [Test]
        public void GetHubMethodForCommand_ShouldUseFullNameWhenNoSuffixPresent()
        {
            var router = CreateRouter();

            var name = router.GetHubMethodForCommand(typeof(Ping));

            Assert.That(name, Is.EqualTo("Ping"));
        }

        [Test]
        public void GetHubMethodForCommand_ShouldHandleInheritance()
        {
            var router = CreateRouter();

            var name = router.GetHubMethodForCommand(typeof(DerivedCommand));

            Assert.That(name, Is.EqualTo("Derived"));
        }

        [Test]
        public void GetHubMethodForCommand_ShouldThrow_WhenCommandNotDiscovered()
        {
            var router = new SignalRCommandRouter(Array.Empty<System.Reflection.Assembly>());

            Assert.That(() => router.GetHubMethodForCommand(typeof(DefaultNameCommand)),
                Throws.TypeOf<NotSupportedException>());
        }

        [Test]
        public void TryGetHubMethodForCommand_ShouldReturnFalse_WhenCommandNotDiscovered()
        {
            var router = new SignalRCommandRouter(Array.Empty<System.Reflection.Assembly>());

            var result = router.TryGetHubMethodForCommand(typeof(DefaultNameCommand), out var hubMethod);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(hubMethod, Is.Empty);
            });
        }

        private sealed record DefaultNameCommand() : ICommand;

        private sealed record Ping() : ICommand;

        private abstract record BaseCommand : ICommand;

        private sealed record DerivedCommand() : BaseCommand;
    }
}
