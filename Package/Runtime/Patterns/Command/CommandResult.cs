using System;

namespace MDI.Patterns.Command
{
    /// <summary>
    /// Command execution sonucunu temsil eder
    /// </summary>
    public class CommandResult
    {
        /// <summary>
        /// Execution başarılı mı
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Hata mesajı (varsa)
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Exception (varsa)
        /// </summary>
        public Exception Exception { get; set; }

        /// <summary>
        /// Execution süresi (millisecond)
        /// </summary>
        public long ExecutionTimeMs { get; set; }

        /// <summary>
        /// Command'dan dönen data (varsa)
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// Başarılı result oluşturur
        /// </summary>
        /// <param name="data">Dönen data</param>
        /// <param name="executionTimeMs">Execution süresi</param>
        /// <returns>Başarılı CommandResult</returns>
        public static CommandResult Success(object data = null, long executionTimeMs = 0)
        {
            return new CommandResult
            {
                IsSuccess = true,
                Data = data,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Başarısız result oluşturur
        /// </summary>
        /// <param name="errorMessage">Hata mesajı</param>
        /// <param name="exception">Exception</param>
        /// <param name="executionTimeMs">Execution süresi</param>
        /// <returns>Başarısız CommandResult</returns>
        public static CommandResult Failure(string errorMessage, Exception exception = null, long executionTimeMs = 0)
        {
            return new CommandResult
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Exception'dan başarısız result oluşturur
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="executionTimeMs">Execution süresi</param>
        /// <returns>Başarısız CommandResult</returns>
        public static CommandResult Failure(Exception exception, long executionTimeMs = 0)
        {
            return new CommandResult
            {
                IsSuccess = false,
                ErrorMessage = exception?.Message,
                Exception = exception,
                ExecutionTimeMs = executionTimeMs
            };
        }
    }

    /// <summary>
    /// Generic command result
    /// </summary>
    /// <typeparam name="T">Result data tipi</typeparam>
    public class CommandResult<T> : CommandResult
    {
        /// <summary>
        /// Typed result data
        /// </summary>
        public new T Data { get; set; }

        /// <summary>
        /// Başarılı typed result oluşturur
        /// </summary>
        /// <param name="data">Typed data</param>
        /// <param name="executionTimeMs">Execution süresi</param>
        /// <returns>Başarılı CommandResult</returns>
        public static CommandResult<T> Success(T data, long executionTimeMs = 0)
        {
            return new CommandResult<T>
            {
                IsSuccess = true,
                Data = data,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Başarısız typed result oluşturur
        /// </summary>
        /// <param name="errorMessage">Hata mesajı</param>
        /// <param name="exception">Exception</param>
        /// <param name="executionTimeMs">Execution süresi</param>
        /// <returns>Başarısız CommandResult</returns>
        public static new CommandResult<T> Failure(string errorMessage, Exception exception = null, long executionTimeMs = 0)
        {
            return new CommandResult<T>
            {
                IsSuccess = false,
                ErrorMessage = errorMessage,
                Exception = exception,
                ExecutionTimeMs = executionTimeMs
            };
        }

        /// <summary>
        /// Exception'dan başarısız typed result oluşturur
        /// </summary>
        /// <param name="exception">Exception</param>
        /// <param name="executionTimeMs">Execution süresi</param>
        /// <returns>Başarısız CommandResult</returns>
        public static new CommandResult<T> Failure(Exception exception, long executionTimeMs = 0)
        {
            return new CommandResult<T>
            {
                IsSuccess = false,
                ErrorMessage = exception?.Message,
                Exception = exception,
                ExecutionTimeMs = executionTimeMs
            };
        }
    }
}