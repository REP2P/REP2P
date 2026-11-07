using System;

namespace REP2P
{
    /// <summary>
    /// Attach this to your console/debugger
    /// </summary>
    public static class P2PLogger
    {
        /// <summary>
        /// P2P Logger Key that will be appended to your P2P Log
        /// </summary>
        public const string LOG = "[P2P]";
        private static event LogMethod log;
        private static event LogMethod warning;
        private static event LogMethod error;
        private static event ExceptionLogMethod exception;

        /// <summary>
        /// Log Method
        /// </summary>
        /// <param name="log">message</param>
        public delegate void LogMethod(string log);
        /// <summary>
        /// Exception Log Method
        /// </summary>
        /// <param name="exception">exception message</param>
        public delegate void ExceptionLogMethod(Exception exception);
        /// <summary>
        /// Attach your debugger/console
        /// </summary>
        /// <param name="_log">your log/debug method</param>
        /// <param name="_warning">your warn log method</param>
        /// <param name="_error">your error log method</param>
        /// <param name="_exception">your excpetion log method</param>
        public static void Initialize(LogMethod _log, LogMethod _warning, LogMethod _error, ExceptionLogMethod _exception)
        {
            log = _log;
            warning = _warning;
            error = _error;
            exception = _exception;
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="_log">log</param>
        public static void Log(string _log) => log?.Invoke(LOG + _log);
        /// <summary>
        /// Warning
        /// </summary>
        /// <param name="_log">warning</param>
        public static void Warning(string _log) => warning?.Invoke(LOG + _log);
        /// <summary>
        /// Error
        /// </summary>
        /// <param name="_log">error</param>
        public static void Error(string _log) => error?.Invoke(LOG + _log);
        /// <summary>
        /// Exception
        /// </summary>
        /// <param name="_exception">exception</param>
        public static void Exception(Exception _exception) => exception?.Invoke(_exception);
    }
}
