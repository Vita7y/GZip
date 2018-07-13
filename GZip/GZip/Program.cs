using System;
using GZip.Properties;

namespace GZip
{
    internal class Program
    {
        private static GZip _compressor;

        private static void Main(string[] args)
        {
            var parameters = new Parameters();
            var cmd = new CommandValidator(parameters);

            var resValidation = cmd.ParameterValidationAndSetup(args);
            if (resValidation.Item1 != ResultType.Success)
            {
                Console.WriteLine(resValidation.Item2);
                return;
            }

            using (_compressor = new GZip(parameters))
            {
                Console.CancelKeyPress += CancelKeyPress;
                _compressor.ShowMessage += OnShowMessage;
                if (_compressor.Start())
                    _compressor.WhaitToEnd();
            }
        }

        private static void OnShowMessage(object sender, MessageEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        private static void CancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            if (args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine(Resources.OperationCancelling);
                args.Cancel = true;
                _compressor.Stop();
            }
        }
    }
}