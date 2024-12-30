using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Collections;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace TRLEManager
{
    internal class Error : Exception
    {
    //    private static bool _floodPrevention = false;

    //    private static string _outputFileName = null;
        
        public Error() : base() { }
        public Error(string message) : base(message) { }
        public Error(string message, Exception innerException) : base(message, innerException) { }

        //    private static FileStream GetLogFile()
        //    {
        //        if (_outputFileName == null)
        //        {
        //            try
        //            {
        //                _outputFileName = Path.Combine(App.GetErrorLogDirectory().FullName, $"Session {DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss")}.txt");
        //            }
        //            catch (Exception e) when (
        //                e is FormatException || 
        //                e is ArgumentOutOfRangeException || 
        //                e is ArgumentException || 
        //                e is ArgumentNullException)
        //            {
        //                throw new Error($"There was an error generating the error log file.\n\n{e.Message}", e);
        //            }
        //        }

        //        var fileMode = File.Exists(_outputFileName) ? FileMode.Append : FileMode.CreateNew;

        //        try
        //        {
        //            return File.Open(_outputFileName, fileMode);
        //        }
        //        catch (Exception e) when (
        //            e is ArgumentException
        //            || e is ArgumentNullException
        //            || e is PathTooLongException
        //            || e is DirectoryNotFoundException
        //            || e is IOException
        //            || e is UnauthorizedAccessException
        //            || e is ArgumentOutOfRangeException
        //            || e is FileNotFoundException
        //            || e is NotSupportedException)
        //        {
        //            var err = new Error($"There was an error generating the error log file.\n\n{e.Message}", e);
        //            err.Data.Add("_outputFileName", _outputFileName);
        //            throw err;
        //        }
        //    }

        public void LogError()
        {
            //LogException(this);
        }

            public static void LogException(Exception eToLog)
            {
        //        try
        //        {
        //            string template = $"{DateTime.Now.ToString("yyyy-MM-dd hh-mm-ss-fff")} | {eToLog.ToString()}\n";
        //            byte[] data = Encoding.UTF8.GetBytes(template);

        //            using (var fs = GetLogFile())
        //                fs.Write(data, 0, data.Length);
        //        }
        //        catch (Exception e) when (
        //            e is Error
        //            || e is FormatException
        //            || e is ArgumentOutOfRangeException
        //            || e is ArgumentNullException
        //            || e is EncoderFallbackException
        //            || e is ArgumentException
        //            || e is IOException
        //            || e is ObjectDisposedException
        //            || e is NotSupportedException)
        //        {
        //            if (_floodPrevention) return;

        //            _floodPrevention = true;

        //            Debug.WriteLine("There was an error trying to generate an error log entry...\n"
        //                + $"{e.ToString()}\n"
        //                + "Error to be logged...\n"
        //                + $"{eToLog.ToString()}");

        //            return;
        //        }
        //    }
        //    public override string ToString()
        //    {
        //        try
        //        {
        //            var builder = new StringBuilder()
        //                .Append($"\nBase error - {base.ToString()}")
        //                .Append("\nAdditional Info - ");

        //            foreach (DictionaryEntry kvp in Data)
        //            {
        //                ///KeyValuePair<object, object> kvp = (KeyValuePair<object, object>)okvp;
        //                builder.Append($"\n\"{kvp.Key.ToString()}\"=\"{kvp.Value.ToString()}\"");
        //            }
        //            return builder.Append('\n').ToString();
        //        } catch(Exception e) when (e is ArgumentOutOfRangeException)
        //        {
        //            return "TRLEManager.Error";
        //        }
            }
    }

}
