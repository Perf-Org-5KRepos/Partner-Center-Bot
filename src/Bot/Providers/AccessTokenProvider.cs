﻿// -----------------------------------------------------------------------
// <copyright file="AccessTokenProvider.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Microsoft.Store.PartnerCenter.Bot.Providers
{
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Cache;
    using IdentityModel.Clients.ActiveDirectory;
    using Extensions;
    using Models;
    using System;

    /// <summary>
    /// Provides the ability to manage access tokens.
    /// </summary>
    internal sealed class AccessTokenProvider : IAccessTokenProvider
    {
        /// <summary>
        /// Provides access to the core bot providers.
        /// </summary>
        private readonly IBotProvider provider;

        /// <summary>
        /// Type of the assertion representing the user when performing app + user authentication.
        /// </summary>
        private const string AssertionType = "urn:ietf:params:oauth:grant-type:jwt-bearer";

        /// <summary>
        /// Initializes a new instance of <see cref="AccessTokenProvider"/> class.
        /// </summary>
        /// <param name="provider">Provides access to core explorer providers.</param>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="provider"/> is null.
        /// </exception>
        public AccessTokenProvider(IBotProvider provider)
        {
            provider.AssertNotNull(nameof(provider));
            this.provider = provider;
        }

        public async Task<AuthenticationResult> AcquireTokenByAuthorizationCodeAsync(string authority, string code, ApplicationCredential credential, Uri redirectUri, string resource)
        {
            AuthenticationContext authContext;
            ISecureClientSecret secret;

            authority.AssertNotEmpty(nameof(authority));
            code.AssertNotEmpty(nameof(code));
            redirectUri.AssertNotNull(nameof(redirectUri));
            resource.AssertNotEmpty(nameof(resource));

            try
            {
                authContext = new AuthenticationContext(authority);
                secret = new SecureClientSecret(credential.ApplicationSecret);

                return await authContext.AcquireTokenByAuthorizationCodeAsync(
                    code,
                    redirectUri,
                    new ClientCredential(
                        credential.ApplicationId, 
                        secret),
                    resource).ConfigureAwait(false);
            }
            finally
            {
                authContext = null;
                secret = null; 
            }
        }

        public async Task<AuthenticationResult> AcquireTokenSilentAsync(string authority, string resource, string clientId, UserIdentifier objectUserId)
        {
            AuthenticationContext authContext;
            AuthenticationResult authResult;

            authority.AssertNotEmpty(nameof(authority));
            resource.AssertNotEmpty(nameof(resource));
            objectUserId.AssertNotNull(nameof(objectUserId));

            try
            {
                authContext = new AuthenticationContext(authority);

                authResult = await authContext.AcquireTokenSilentAsync(
                    resource,
                    clientId,
                    objectUserId).ConfigureAwait(false);

                return authResult;
            }
            finally
            {
                authContext = null;
            }
        }

        /// <summary>
        /// Gets an access token from the authority.
        /// </summary>
        /// <param name="authority">Address of the authority to issue the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="credential">The application credential to use for token acquisition.</param>
        /// <returns>An instance of <see cref="AuthenticationResult"/> that represented the access token.</returns>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="authority"/> is empty or null.
        /// or
        /// <paramref name="resource"/> is empty or null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="credential"/> is null.
        /// </exception>
        public async Task<AuthenticationResult> GetAccessTokenAsync(string authority, string resource, ApplicationCredential credential)
        {
            AuthenticationContext authContext;
            AuthenticationResult authResult;
            DistributedTokenCache tokenCache;
            ISecureClientSecret secret;

            authority.AssertNotEmpty(nameof(authority));
            resource.AssertNotEmpty(nameof(resource));
            credential.AssertNotNull(nameof(credential));

            try
            {
                if (credential.UseCache)
                {
                    tokenCache = new DistributedTokenCache(provider, resource, $"AppOnly::{authority}::{resource}");
                    authContext = new AuthenticationContext(authority, tokenCache);
                }
                else
                {
                    authContext = new AuthenticationContext(authority);
                }

                secret = new SecureClientSecret(credential.ApplicationSecret);

                authResult = await authContext.AcquireTokenAsync(
                    resource,
                    new ClientCredential(
                        credential.ApplicationId,
                        secret)).ConfigureAwait(false);

                return authResult;
            }
            finally
            {
                authContext = null;
                authResult = null;
                secret = null;
                tokenCache = null;
            }
        }

        /// <summary>
        /// Gets an access token from the authority.
        /// </summary>
        /// <param name="authority">Address of the authority to issue the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="credential">The application credential to use for token acquisition.</param>
        /// <param name="token">Assertion token representing the user.</param>
        /// <returns>An instance of <see cref="AuthenticationResult"/> that represented the access token.</returns>
        /// <exception cref="System.ArgumentException">
        /// <paramref name="authority"/> is empty or null.
        /// or 
        /// <paramref name="resource"/> is empty or null.
        /// or 
        /// <paramref name="token"/> is empty or null.
        /// </exception>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="credential"/> is null.
        /// </exception>
        public async Task<AuthenticationResult> GetAccessTokenAsync(string authority, string resource, ApplicationCredential credential, string token)
        {
            AuthenticationContext authContext;
            AuthenticationResult authResult;
            DistributedTokenCache tokenCache;
            ISecureClientSecret secret;

            authority.AssertNotEmpty(nameof(authority));
            resource.AssertNotEmpty(nameof(authority));
            credential.AssertNotNull(nameof(credential));
            token.AssertNotEmpty(nameof(token));

            try
            {
                if (credential.UseCache)
                {
                    tokenCache = new DistributedTokenCache(provider, resource);
                    authContext = new AuthenticationContext(authority, tokenCache);
                }
                else
                {
                    authContext = new AuthenticationContext(authority);
                }

                secret = new SecureClientSecret(credential.ApplicationSecret);

                authResult = await authContext.AcquireTokenAsync(
                    resource,
                    new ClientCredential(
                        credential.ApplicationId,
                        secret),
                    new UserAssertion(token, AssertionType)).ConfigureAwait(false);

                return authResult;
            }
            finally
            {
                authContext = null;
                tokenCache = null;
                secret = null;
            }
        }

        public async Task<string> GetAuthorizationRequestUrlAsync(string authority, string resource, string clientId, Uri redirectUri, string extraQueryParameters)
        {
            AuthenticationContext authContext;
            Uri authUri;

            authority.AssertNotEmpty(nameof(authority));
            resource.AssertNotEmpty(nameof(resource));
            redirectUri.AssertNotNull(nameof(redirectUri));

            try
            {
                authContext = new AuthenticationContext(authority);

                authUri = await authContext.GetAuthorizationRequestUrlAsync(
                    resource,
                    clientId,
                    redirectUri,
                    UserIdentifier.AnyUser,
                    extraQueryParameters).ConfigureAwait(false);

                return authUri.ToString();
            }
            finally
            {
                authContext = null;
            }
        }
    }
}