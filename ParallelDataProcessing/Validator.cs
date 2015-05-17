using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelDataProcessing
{
    public class ValidationResult
    {
        private readonly IList<string> _errors;
        private readonly bool _isValid;
        private readonly int _validatedData;

        public ValidationResult(
            bool isValid,
            int validatedData,
            IEnumerable<string> errors)
        {
            _isValid = isValid;
            _validatedData = validatedData;
            _errors = errors.ToList().AsReadOnly();
        }

        public bool IsValid
        {
            get
            {
                return _isValid;
            }
        }

        public IEnumerable<string> Errors
        {
            get
            {
                return _errors;
            }
        }

        public int ValidatedData
        {
            get
            {
                return _validatedData;
            }
        }

    }

    public class Validator
    {
        public ValidationResult Validate(int toValidate)
        {
            var isValid = toValidate % 10 != 0;
            var errors = isValid ? new string[0] : new[] { "Should not be divisible by 10" };

            // Do some work here...
            Thread.Sleep(50);

            //Console.WriteLine("Thread({0})::Validator[{1}]::Validate[toValidate::{2}])", Thread.CurrentThread.ManagedThreadId, this.GetHashCode(), toValidate);

            return new ValidationResult(isValid, toValidate, errors);
        }

    }
}
