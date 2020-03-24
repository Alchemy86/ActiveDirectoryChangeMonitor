using System;
using System.DirectoryServices.Protocols;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cascade.DirectoryAgent
{
    public class HostedService : IHostedService
    {
        private readonly ILogger _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        public HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifetime)
        {
            if (logger is null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            if (appLifetime is null)
            {
                throw new ArgumentNullException(nameof(appLifetime));
            }

            _logger = logger;
            _appLifetime = appLifetime;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("It begins");
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        private LdapConnection getConnection(AuthType type)
        {
            var identifier = new LdapDirectoryIdentifier(null, 0);
            var connection = new LdapConnection(identifier, null, type)
            {
                AutoBind = true
            };
            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.AutoReconnect = true;
            connection.SessionOptions.TcpKeepAlive = true;

            return connection;
        }

        private void OnStarted()
        {
            var connection = getConnection(AuthType.Negotiate);
            var notifier = new ChangeNotifier(connection);

            notifier.Register("OU=LDS,OU=People,DC=iascorp,DC=co,DC=uk", SearchScope.Subtree);
            notifier.ObjectChanged += new EventHandler<ObjectChangedEventArgs>(notifier_ObjectChanged);
            Console.WriteLine("Waiting for changes...");
        }

        private void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");

            // Perform on-stopping activities here
        }

        private void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");

            // Perform post-stopped activities here
        }

        static void notifier_ObjectChanged(object sender, ObjectChangedEventArgs e)
        {
            Console.WriteLine(e.Result.DistinguishedName);
            foreach (string attrib in e.Result.Attributes.AttributeNames)
            {
                foreach (var item in e.Result.Attributes[attrib].GetValues(typeof(string)))
                {
                    Console.WriteLine("\t{0}: {1}", attrib, item);
                }
            }
            Console.WriteLine();
            Console.WriteLine("====================");
            Console.WriteLine();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("DYIUNG");
            return Task.CompletedTask;
        }
    }
}