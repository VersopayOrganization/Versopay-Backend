// VersopayBackend/Webhooks/VendorWebhookHandlerFactory.cs
using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using VersopayBackend.Dtos.Common;
using VersopayLibrary.Enums;

namespace VersopayBackend.Webhooks
{
    public sealed class VendorWebhookHandlerFactory : IVendorWebhookHandlerFactory
    {
        private readonly IServiceProvider _sp;
        private readonly IReadOnlyDictionary<string, PaymentProvider> _aliases;

        public VendorWebhookHandlerFactory(IServiceProvider sp)
        {
            _sp = sp;

            // mapeia nomes aceitos na rota/header para o enum
            _aliases = new Dictionary<string, PaymentProvider>(StringComparer.OrdinalIgnoreCase)
            {
                // Vexy
                ["vexy"] = PaymentProvider.Vexy,
                ["vexybank"] = PaymentProvider.Vexy,
                ["vexy-payments"] = PaymentProvider.Vexy,

                // Versell
                ["versell"] = PaymentProvider.Versell,
                ["versellpay"] = PaymentProvider.Versell,
                ["versell-payments"] = PaymentProvider.Versell,
            };
        }

        public IVendorWebhookHandler Resolve(PaymentProvider provider)
        {
            return provider switch
            {
                PaymentProvider.Vexy => _sp.GetRequiredService<VexyVendorWebhookHandler>(),
                PaymentProvider.Versell => _sp.GetRequiredService<VersellVendorWebhookHandler>(),
                _ => throw new NotSupportedException($"Provider não suportado: {provider}")
            };
        }

        public IVendorWebhookHandler Resolve(string providerFromRouteOrHeader)
        {
            if (string.IsNullOrWhiteSpace(providerFromRouteOrHeader))
                throw new ArgumentException("Provider não informado.", nameof(providerFromRouteOrHeader));

            if (!_aliases.TryGetValue(providerFromRouteOrHeader.Trim(), out var prov))
                throw new NotSupportedException($"Provider desconhecido: '{providerFromRouteOrHeader}'.");

            return Resolve(prov);
        }
    }
}
