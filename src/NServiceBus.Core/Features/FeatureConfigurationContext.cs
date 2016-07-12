﻿namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ObjectBuilder;
    using Pipeline;
    using Settings;
    using Transport;

    /// <summary>
    /// The context available to features when they are activated.
    /// </summary>
    public class FeatureConfigurationContext
    {
        internal FeatureConfigurationContext(ReadOnlySettings settings, IConfigureComponents container, PipelineSettings pipelineSettings)
        {
            Settings = settings;
            Container = container;
            Pipeline = pipelineSettings;

            TaskControllers = new List<FeatureStartupTaskController>();
        }

        /// <summary>
        /// A read only copy of the settings.
        /// </summary>
        public ReadOnlySettings Settings { get; }

        /// <summary>
        /// Access to the container to allow for registrations.
        /// </summary>
        public IConfigureComponents Container { get; }

        /// <summary>
        /// Access to the pipeline in order to customize it.
        /// </summary>
        public PipelineSettings Pipeline { get; }

        internal List<FeatureStartupTaskController> TaskControllers { get; }

        /// <summary>
        /// Creates a new satellite processing pipeline.
        /// </summary>
        /// <param name="name">Name of the satellite.</param>
        /// <param name="requiredTransportTransactionMode">Minimum required transaction mode.</param>
        /// <param name="runtimeSettings">Transport runtime settings.</param>
        /// <param name="onMessage">The message func.</param>
        /// <param name="transportAddress">The autogenerated transport address to listen on.</param>
        /// <param name="recoverabilityPolicy">Recoverability policy to be used by recoverability.</param>
        public void AddSatelliteReceiver(string name, string transportAddress, TransportTransactionMode requiredTransportTransactionMode, PushRuntimeSettings runtimeSettings, IRecoverabilityPolicy recoverabilityPolicy, Func<IBuilder, MessageContext, Task> onMessage)
        {
            var satelliteDefinition = new SatelliteDefinition(name, transportAddress, requiredTransportTransactionMode, runtimeSettings, recoverabilityPolicy, onMessage);

            Settings.Get<SatelliteDefinitions>().Add(satelliteDefinition);

            Settings.Get<QueueBindings>().BindReceiving(transportAddress);
        }

        /// <summary>
        /// Registers an instance of a feature startup task.
        /// </summary>
        /// <param name="startupTask">A startup task.</param>
        public void RegisterStartupTask<TTask>(TTask startupTask) where TTask : FeatureStartupTask
        {
            RegisterStartupTask(() => startupTask);
        }

        /// <summary>
        /// Registers a startup task factory.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        public void RegisterStartupTask<TTask>(Func<TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, _ => startupTaskFactory()));
        }

        /// <summary>
        /// Registers a startup task factory which gets access to the builder.
        /// </summary>
        /// <param name="startupTaskFactory">A startup task factory.</param>
        /// <remarks>Should only be used when really necessary. Usually a design smell.</remarks>
        public void RegisterStartupTask<TTask>(Func<IBuilder, TTask> startupTaskFactory) where TTask : FeatureStartupTask
        {
            TaskControllers.Add(new FeatureStartupTaskController(typeof(TTask).Name, startupTaskFactory));
        }
    }
}