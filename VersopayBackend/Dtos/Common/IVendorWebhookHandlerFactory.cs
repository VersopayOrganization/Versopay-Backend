// VersopayBackend/Dtos/Common/IVendorWebhookHandlerFactory.cs
using VersopayLibrary.Enums;

namespace VersopayBackend.Dtos.Common
{
    public interface IVendorWebhookHandlerFactory
    {
        IVendorWebhookHandler Resolve(PaymentProvider provider);
        IVendorWebhookHandler Resolve(string providerFromRouteOrHeader);
    }
}
