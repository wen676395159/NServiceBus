namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Logging;
    using Transport;

    class TransportReceiver
    {
        public TransportReceiver(
            string id,
            IPushMessages pushMessages,
            PushSettings pushSettings,
            PushRuntimeSettings pushRuntimeSettings,
            IPipelineExecutor pipelineExecutor,
            RecoverabilityExecutor recoverabilityExecutor,
            CriticalError criticalError)
        {
            this.criticalError = criticalError;
            Id = id;
            this.pushRuntimeSettings = pushRuntimeSettings;
            this.pipelineExecutor = pipelineExecutor;
            this.recoverabilityExecutor = recoverabilityExecutor;
            this.pushSettings = pushSettings;

            receiver = pushMessages;
        }

        public string Id { get; }

        public async Task Start()
        {
            if (isStarted)
            {
                throw new InvalidOperationException("The transport is already started");
            }

            Logger.DebugFormat("Receiver {0} is starting, listening to queue {1}.", Id, pushSettings.InputQueue);

            await receiver.Init(c => pipelineExecutor.Invoke(c), c => recoverabilityExecutor.Invoke(c), criticalError, pushSettings).ConfigureAwait(false);

            receiver.Start(pushRuntimeSettings);

            isStarted = true;
        }

        public async Task Stop()
        {
            if (!isStarted)
            {
                return;
            }

            await receiver.Stop().ConfigureAwait(false);

            isStarted = false;
        }

        readonly CriticalError criticalError;

        bool isStarted;
        PushRuntimeSettings pushRuntimeSettings;
        IPipelineExecutor pipelineExecutor;
        RecoverabilityExecutor recoverabilityExecutor;
        PushSettings pushSettings;
        IPushMessages receiver;

        static ILog Logger = LogManager.GetLogger<TransportReceiver>();
    }
}