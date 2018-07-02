﻿using System;
using System.Collections.Generic;
using System.Text;
using HB.Component.Identity;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using HB.Component.Identity.Abstractions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using HB.Component.Authorization.Abstractions;
using HB.Component.Authorization.Entity;
using HB.Component.Identity.Entity;

namespace HB.Component.Authorization
{
    public class JwtBuilder : IJwtBuilder
    {
        private SignInOptions _signInOptions;
        private AuthorizationServerOptions _options;
        private IClaimsPrincipalFactory _claimsPrincipalFactory;
        private ICredentialManager _credentialManager;
        private SigningCredentials _signingCredentials;

        public JwtBuilder(IOptions<AuthorizationServerOptions> options, IClaimsPrincipalFactory claimsPrincipalFactory, ICredentialManager credentialManager)
        {
            _options = options.Value;
            _signInOptions = _options.SignInOptions;
            _claimsPrincipalFactory = claimsPrincipalFactory;
            _credentialManager = credentialManager;
            _signingCredentials = _credentialManager.GetSigningCredentialsFromCertificate();
        }

        public async Task<string> BuildJwtAsync(User user, SignInToken signInToken, string audience)
        {
            DateTime utcNow = DateTime.UtcNow;

            IList<Claim> claims = await _claimsPrincipalFactory.CreateClaimsAsync(user);

            claims.Add(new Claim(ClaimExtensionTypes.SignInTokenIdentifier, signInToken.SignInTokenIdentifier));

            JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

            JwtSecurityToken token = handler.CreateJwtSecurityToken(
                _options.OpenIdConnectConfiguration.Issuer,
                _options.NeedAudienceToBeChecked ? audience : null,
                new ClaimsIdentity(claims),
                utcNow,
                utcNow + _signInOptions.AccessTokenExpireTimeSpan,
                utcNow,
                _signingCredentials
                );

            return handler.WriteToken(token);
        }
    }
}
