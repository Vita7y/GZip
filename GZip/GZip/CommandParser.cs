using System;
using System.IO;

namespace GZip
{
    public class CommandValidator
    {
        private readonly Parameters _parameters;

        public CommandValidator(Parameters parameters)
        {
            _parameters = parameters;
        }

        public enum ResultType
        {
            Unknown,
            Error,
            Success
        }

        public Tuple<ResultType, string> ParameterValidationAndSetup(string[] args)
        {

            if (args.Length == 0 || args.Length > 3)
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorArgsIsEmpty);
            }

            Parameters.OperationType type;
            if (!Enum.TryParse(args[0].ToUpper(), out type))
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorFirstArg);
            }

            if (args[1].Length == 0)
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorSecondArg);
            }

            if (!File.Exists(args[1]))
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorInputFileNotFound);
            }

            if (args[2].Length == 0)
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorOutputFileNotspecified);
            }

            if (args[1] == args[2])
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorInputOutputFIlesSellBeDifferent);
            }

            if (File.Exists(args[2]))
            {
                return new Tuple<ResultType, string>(ResultType.Error, Properties.Resources.ErrorOutputFileExist);
            }

            FillParameters(type, args[1], args[2]);
            return new Tuple<ResultType, string>(ResultType.Success, Properties.Resources.ParameterInstalled);
        }

        private void FillParameters(Parameters.OperationType type, string input, string output)
        {
            _parameters.InputFileName = input;
            _parameters.OutputFileName = output;
            _parameters.Operation = type;
        }
    }
}