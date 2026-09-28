using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CX102PrickHMI
{
    public class NLogHelper
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        #region Debug
        public static void Debug(string msg, params object[] args)
        {
            logger.Debug(msg, args);
        }

        public static void Debug(string msg, Exception err)
        {
            logger.Debug(err, msg);
        }
        #endregion

        #region Info
        public static void Info(string msg, params object[] args)
        {
            logger.Info(msg, args);
        }

        public static void Info(string msg, Exception err)
        {
            logger.Info(err, msg);
        }
        #endregion

        #region  Warn
        public static void Warn(string msg, params object[] args)
        {
            logger.Warn(msg, args);
        }

        public static void Warn(string msg, Exception err)
        {
            logger.Warn(err, msg);
        }

        #endregion

        #region Trace
        public static void Trace(string msg, params object[] args)
        {
            logger.Trace(msg, args);

        }
        public static void Trace(string msg, Exception err)
        {
            logger.Trace(err, msg);
        }
        #endregion

        #region Error
        public static void Error(string msg, params object[] args)
        {
            logger.Error(msg, args);

        }
        public static void Error(string msg, Exception err)
        {
            logger.Error(err, msg);
        }
        #endregion

        #region Fatal
        public static void Fatal(string msg, params object[] args)
        {
            logger.Fatal(msg, args);

        }
        public static void Fatal(string msg, Exception err)
        {
            logger.Fatal(err, msg);
        }
        #endregion
    }
}
