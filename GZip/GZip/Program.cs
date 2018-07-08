using System;

namespace GZip
{
    class Program
    {
        private static GZip _compressor;

        static void Main(string[] args)
        {
            var parameters = new Parameters();
            var cmd = new CommandValidator(parameters);

            var resValidation = cmd.ParameterValidationAndSetup(args);
            if (resValidation.Item1 != CommandValidator.ResultType.Success)
            {
                Console.WriteLine(resValidation.Item2);
                return;
            }

            _compressor = new GZip(parameters);
            _compressor.ShowMessage += OnShowMessage;

            Console.CancelKeyPress += CancelKeyPress;

            _compressor.Start();
        }

        static void OnShowMessage(object sender, MessageEventArgs args)
        {
            Console.WriteLine(args.Message);
        }

        static void CancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            if (args.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                Console.WriteLine(Properties.Resources.OperationCancelling);
                args.Cancel = true;
                _compressor.Cancel();

            }
        }
    }
}
