// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.


using FluxoCaixaDiario.IdentityServer.Controllers.MainModule.Consent;

namespace FluxoCaixaDiario.IdentityServer.Controllers.MainModule.Device
{
    public class DeviceAuthorizationViewModel : ConsentViewModel
    {
        public string UserCode { get; set; }
        public bool ConfirmUserCode { get; set; }
    }
}