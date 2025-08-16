using System;
using MDI.Containers;
using MDI.Core;

namespace MDI.Examples.Basic
{
    /// <summary>
    /// Basit DI kullanım örneği
    /// </summary>
    public class BasicDIExample
    {
        /// <summary>
        /// Ana test method'u
        /// </summary>
        public static void RunExample()
        {
            Console.WriteLine("=== MDI+ Basic DI Example ===");

            // 1. Container oluştur
            using var container = new MDIContainer();

            // 2. Service'leri register et
            Console.WriteLine("1. Registering services...");
            
            container
                .RegisterSingleton<ILogger, ConsoleLogger>()
                .RegisterTransient<IEmailService, EmailService>()
                .Register<INotificationService, NotificationService>();

            // 3. Service'leri resolve et ve kullan
            Console.WriteLine("2. Resolving and using services...");
            
            var notificationService = container.Resolve<INotificationService>();
            notificationService.SendNotification("Hello from MDI+!");

            // 4. Singleton test - aynı instance olmalı
            Console.WriteLine("3. Testing singleton behavior...");
            
            var logger1 = container.Resolve<ILogger>();
            var logger2 = container.Resolve<ILogger>();
            
            Console.WriteLine($"Logger instances are the same: {ReferenceEquals(logger1, logger2)}");

            // 5. Transient test - farklı instance'lar olmalı
            Console.WriteLine("4. Testing transient behavior...");
            
            var email1 = container.Resolve<IEmailService>();
            var email2 = container.Resolve<IEmailService>();
            
            Console.WriteLine($"Email instances are the same: {ReferenceEquals(email1, email2)}");

            Console.WriteLine("=== Example completed successfully! ===");
        }
    }

    // Service Interface'leri
    public interface ILogger
    {
        void Log(string message);
    }

    public interface IEmailService
    {
        void SendEmail(string to, string subject, string body);
    }

    public interface INotificationService
    {
        void SendNotification(string message);
    }

    // Service Implementation'ları
    public class ConsoleLogger : ILogger
    {
        public void Log(string message)
        {
            Console.WriteLine($"[LOG] {message}");
        }
    }

    public class EmailService : IEmailService
    {
        private readonly ILogger _logger;

        public EmailService(ILogger logger)
        {
            _logger = logger;
        }

        public void SendEmail(string to, string subject, string body)
        {
            _logger.Log($"Sending email to {to}: {subject}");
            // Email gönderme logic'i burada olacak
        }
    }

    public class NotificationService : INotificationService
    {
        private readonly ILogger _logger;
        private readonly IEmailService _emailService;

        public NotificationService(ILogger logger, IEmailService emailService)
        {
            _logger = logger;
            _emailService = emailService;
        }

        public void SendNotification(string message)
        {
            _logger.Log($"Sending notification: {message}");
            _emailService.SendEmail("user@example.com", "Notification", message);
        }
    }
}
