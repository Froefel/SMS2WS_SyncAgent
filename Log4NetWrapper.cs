using System;
using System.Diagnostics;
using System.Linq;

namespace SMS2WS_SyncAgent
{
    public class Log4NetWrapper
    {
        private readonly log4net.ILog log;
        private const string indent = "  ";
        private static int indentLevel;

        //constructor
        public Log4NetWrapper(Type type)
        {
            log = log4net.LogManager.GetLogger(type);
        }

        public void IncreaseIndent()
        {
            //Increase indent
            indentLevel++;
        }

        public void DecreaseIndent()
        {
            //Decrease indent to minimum zero.
            if (indentLevel > 0)
                indentLevel--;
        }

        public void Error(object msg)
        {
            if (log.IsErrorEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Error(msg);
            }
        }

        public void Error(object msg, Exception exception)
        {
            if (log.IsErrorEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Error(msg, exception);
            }
        }

        public void ErrorFormat(string msg, Object obj)
        {
            if (log.IsErrorEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.ErrorFormat(msg, obj);
            }
        }

        public void ErrorFormat(string msg, Object obj0, Object obj1)
        {
            if (log.IsErrorEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.ErrorFormat(msg, obj0, obj1);
            }
        }

        public void ErrorFormat(string msg, Object obj0, Object obj1, Object obj2)
        {
            if (log.IsErrorEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.ErrorFormat(msg, obj0, obj1, obj2);
            }
        }

        public void Info(object msg)
        {
            if (log.IsInfoEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Info(msg);
            }
        }

        public void Info(object msg, Exception exception)
        {
            if (log.IsInfoEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Info(msg, exception);
            }
        }

        public void InfoFormat(string msg, Object obj)
        {
            if (log.IsInfoEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.InfoFormat(msg, obj);
            }
        }

        public void InfoFormat(string msg, Object obj0, Object obj1)
        {
            if (log.IsInfoEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.InfoFormat(msg, obj0, obj1);
            }
        }

        public void Fatal(object msg)
        {
            if (log.IsFatalEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Fatal(msg);
            }
        }

        public void Fatal(object msg, Exception exception)
        {
            if (log.IsFatalEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Fatal(msg, exception);
            }
        }

        public void FatalFormat(string format, Object obj)
        {
            if (log.IsFatalEnabled)
            {
                format = string.Concat(Enumerable.Repeat(indent, indentLevel)) + format;
                log.FatalFormat(format, obj);
            }
        }

        public void FatalFormat(string format, Object obj0, Object obj1)
        {
            if (log.IsFatalEnabled)
            {
                format = string.Concat(Enumerable.Repeat(indent, indentLevel)) + format;
                log.FatalFormat(format, obj0, obj1);
            }
        }

        public void Debug(object msg)
        {
            if (log.IsDebugEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Debug(msg);
            }
        }

        public void Debug(object msg, Exception exception)
        {
            if (log.IsDebugEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.Debug(msg, exception);
            }
        }

        public void DebugFormat(string msg, Object obj)
        {
            if (log.IsDebugEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.ErrorFormat(msg, obj);
            }
        }

        public void DebugFormat(string msg, Object obj0, Object obj1)
        {
            if (log.IsDebugEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.ErrorFormat(msg, obj0, obj1);
            }
        }

        public void DebugFormat(string msg, Object obj0, Object obj1, Object obj2)
        {
            if (log.IsDebugEnabled)
            {
                msg = string.Concat(Enumerable.Repeat(indent, indentLevel)) + msg;
                log.ErrorFormat(msg, obj0, obj1, obj2);
            }
        }
    }


    public static class LogManager
    {
        public static Log4NetWrapper GetLogger(Type type)
        {
            // if configuration file says log4net...
            return new Log4NetWrapper(type);
            // if it says Joe's Log4NetWrapper...
            // return new JoesLoggerWrapper(type);
        }

        public static Log4NetWrapper GetLogger()
        {
            var stack = new StackTrace();
            var frame = stack.GetFrame(1);
            return new Log4NetWrapper(frame.GetMethod().DeclaringType);
        }
    }
}
