﻿namespace NServiceBus.AcceptanceTests.Recoverability.Retries
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_immediate_retries_with_native_transactions : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_do_the_configured_number_of_retries_with_native_transactions()
        {
            return Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<RetryEndpoint>(b => b
                    .When((session, context) => session.SendLocal(new MessageToBeRetried
                    {
                        Id = context.Id
                    }))
                    .DoNotFailOnErrorMessages())
                .Done(c => c.ForwardedToErrorQueue)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreEqual(5 + 1, c.NumberOfTimesInvoked, "The Immediate Retries retry 5 times");
                    Assert.AreEqual(5, c.Logs.Count(l => l.Message
                        .StartsWith($"Immediate Retry is going to retry message '{c.PhysicalMessageId}' because of an exception:")));
                })
                .Run();
        }

        class Context : ScenarioContext
        {
            public Guid Id { get; set; }

            public int NumberOfTimesInvoked { get; set; }

            public bool ForwardedToErrorQueue { get; set; }

            public string PhysicalMessageId { get; set; }
        }

        public class RetryEndpoint : EndpointConfigurationBuilder
        {
            public RetryEndpoint()
            {
                EndpointSetup<DefaultServer>((config, context) =>
                {
                    var scenarioContext = (Context) context.ScenarioContext;
                    config.Notifications.Errors.MessageSentToErrorQueue += (sender, message) => scenarioContext.ForwardedToErrorQueue = true;

                    var recoverability = config.Recoverability();
                    recoverability.Immediate(immediate => immediate.NumberOfRetries(5));
                    recoverability.Delayed(settings => settings.TimeIncrease(TimeSpan.FromMilliseconds(1)).NumberOfRetries(3));

                    config.UseTransport(context.GetTransportType())
                        .Transactions(TransportTransactionMode.ReceiveOnly);
                });
            }

            class MessageToBeRetriedHandler : IHandleMessages<MessageToBeRetried>
            {
                public Context TestContext { get; set; }

                public Task Handle(MessageToBeRetried message, IMessageHandlerContext context)
                {
                    if (message.Id != TestContext.Id)
                    {
                        return Task.FromResult(0); // messages from previous test runs must be ignored
                    }

                    TestContext.PhysicalMessageId = context.MessageId;
                    TestContext.NumberOfTimesInvoked++;

                    throw new SimulatedException();
                }
            }
        }

        public class MessageToBeRetried : IMessage
        {
            public Guid Id { get; set; }
        }
    }
}