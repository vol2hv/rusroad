﻿using NLog;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _Nlog
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            
            
            
            
            logger.Info(System.Reflection.Assembly.GetEntryAssembly().Location);
            
            logger.Trace("logger.Trace");
            logger.Debug("logger.Debug");
            logger.Info("logger.Info");
            logger.Info("Очень длинное информационное сообщение.\nПрододжение длиннго сообщения.");
            logger.Warn("logger.Warn");
            logger.Error("logger.Error");
            logger.Fatal("logger.Fatal");
        }
    }
}