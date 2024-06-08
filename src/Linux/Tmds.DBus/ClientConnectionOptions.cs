// Copyright 2017 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tmds.DBus
{
    /// <summary>
    /// Options that configure the behavior of a Connection to a remote peer.
    /// </summary>
    public class ClientConnectionOptions : ConnectionOptions
    {
        private string _address;

        /// <summary>
        /// Creates a new Connection with a specific address.
        /// </summary>
        /// <param name="address">Address of the D-Bus peer.</param>
        public ClientConnectionOptions(string address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            _address = address;
        }

        /// <summary>
        /// Base constructor for derived types.
        /// </summary>
        protected ClientConnectionOptions()
        {}

        /// <summary>
        /// Automatically connect and re-connect the Connection.
        /// </summary>
        public bool AutoConnect { get; set; }

        /// <summary>
        /// Sets up tunnel/connects to the remote peer.
        /// </summary>
        protected internal virtual Task<ClientSetupResult> SetupAsync()
        {
            return Task.FromResult(
                new ClientSetupResult
                {
                    ConnectionAddress = _address,
                    SupportsFdPassing = true,
                    UserId = Environment.UserId
                });
        }

        /// <summary>
        /// Action to clean up resources created during succesfull execution of SetupAsync.
        /// </summary>
        protected internal virtual void Teardown(object token)
        {}

        /// <summary>
        /// Run Task continuations asynchronously.
        /// </summary>
        public bool RunContinuationsAsynchronously { get; set; }
    }
}