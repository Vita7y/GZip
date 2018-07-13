using System;
using System.IO;
using GZip.Properties;

namespace GZip
{
    public class CommandValidator
    {
        private readonly Parameters _parameters;

        public CommandValidator(Parameters parameters)
        {
            _parameters = parameters;
        }

        public Tuple<ResultType, string> ParameterValidationAndSetup(string[] args)
        {
            if (args.Length == 0 || args.Length > 3)
                return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorArgsIsEmpty);

            Parameters.OperationType type;
            if (!Enum.TryParse(args[0].ToUpper(), out type))
                return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorFirstArg);

            if (args[1].Length == 0) return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorSecondArg);

            if (!File.Exists(args[1]))
                return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorInputFileNotFound);

            if (args[2].Length == 0)
                return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorOutputFileNotspecified);

            if (args[1] == args[2])
                return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorInputOutputFIlesSellBeDifferent);

            if (File.Exists(args[2]))
                return new Tuple<ResultType, string>(ResultType.Error, Resources.ErrorOutputFileExist);

            FillParameters(type, args[1], args[2]);
            return new Tuple<ResultType, string>(ResultType.Success, Resources.ParameterInstalled);
        }

        private void FillParameters(Parameters.OperationType type, string input, string output)
        {
            _parameters.InputFileName = input;
            _parameters.OutputFileName = output;
            _parameters.Operation = type;
        }
    }

    public enum ResultType
    {
        Unknown,
        Error,
        Success
    }
}