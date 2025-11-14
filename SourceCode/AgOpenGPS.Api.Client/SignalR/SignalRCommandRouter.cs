using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using AgOpenGPS.Api.Client.Commands;

namespace AgOpenGPS.Api.Client.SignalR
{
    /// <summary>
    /// Provides hub method resolution for SignalR command dispatch.
    /// </summary>
    public sealed class SignalRCommandRouter : ISignalRCommandRouter
    {
        private readonly IReadOnlyDictionary<Type, string> _commandHubMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalRCommandRouter"/> class.
        /// </summary>
        /// <param name="assemblies">Optional set of assemblies to scan for <see cref="ICommand"/> implementations.</param>
        public SignalRCommandRouter(IEnumerable<Assembly>? assemblies = null)
        {
            var assembliesToScan = (assemblies ?? new[] { typeof(ICommand).Assembly })
                .Where(a => a != null)
                .Distinct()
                .ToArray();

            _commandHubMap = new ReadOnlyDictionary<Type, string>(BuildCommandHubMap(assembliesToScan));
        }

        /// <inheritdoc />
        public bool TryGetHubMethodForCommand(Type commandType, out string hubMethodName)
        {
            if (commandType == null)
                throw new ArgumentNullException(nameof(commandType));

            if (!_commandHubMap.TryGetValue(commandType, out hubMethodName!))
            {
                hubMethodName = string.Empty;
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public string GetHubMethodForCommand(Type commandType)
        {
            if (TryGetHubMethodForCommand(commandType, out var hubMethodName))
            {
                return hubMethodName;
            }

            throw new NotSupportedException(
                $"Command type '{commandType?.Name ?? "<null>"}' does not have a SignalR hub mapping. " +
                "Ensure the command type is included in the assemblies scanned by SignalRCommandRouter.");
        }

        private static Dictionary<Type, string> BuildCommandHubMap(IEnumerable<Assembly> assemblies)
        {
            var map = new Dictionary<Type, string>();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsInterface)
                        continue;

                    if (!typeof(ICommand).IsAssignableFrom(type))
                        continue;

                    var hubMethodName = ResolveHubMethodName(type);
                    map[type] = hubMethodName;
                }
            }

            return map;
        }

        private static string ResolveHubMethodName(Type commandType)
        {
            var typeName = commandType.Name;
            if (typeName.EndsWith("Command", StringComparison.Ordinal))
            {
                return typeName.Substring(0, typeName.Length - "Command".Length);
            }

            return typeName;
        }
    }
}
