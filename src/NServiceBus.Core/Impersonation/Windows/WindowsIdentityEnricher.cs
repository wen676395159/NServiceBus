namespace NServiceBus.Impersonation.Windows
{
    using System.Security.Principal;
    using System.Threading;
    using NServiceBus.MessageMutator;

    class WindowsIdentityEnricher : IMutateOutgoingPhysicalMessages
    {

        public void MutateOutgoing(MutateOutgoingPhysicalMessageContext context)
        {
            if (Thread.CurrentPrincipal != null && Thread.CurrentPrincipal.Identity != null && !string.IsNullOrEmpty(Thread.CurrentPrincipal.Identity.Name))
            {
                context.SetHeader(Headers.WindowsIdentityName,Thread.CurrentPrincipal.Identity.Name);
                return;
            }
            var windowsIdentity = WindowsIdentity.GetCurrent();
            if (windowsIdentity != null)
            {
                context.SetHeader(Headers.WindowsIdentityName,windowsIdentity.Name);
            }

        }
    }
}