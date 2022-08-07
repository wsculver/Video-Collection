using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoCollection.Helpers
{
    public class Result
    {
        public bool IsFailure => !IsSuccess;
        public bool IsSuccess { get; }
        public bool IsPartial { get; }
        public string Error { get; }

        protected Result(bool isSuccess, bool isPartial, string error)
        {
            IsSuccess = isSuccess;
            IsPartial = isPartial;
            Error = error;
        }

        private Result(bool isSuccess, bool isPartial) : this(isSuccess, isPartial, null) { }

        public static Result Fail(string error) => new Result(false, false, error);

        public static Result<T> Fail<T>(string error) => new Result<T>(default(T), false, false, error);

        public static Result Partial(string error) => new Result(false, true, error);

        public static Result<T> Partial<T>(T value, string error) => new Result<T>(value, false, true, error);

        public static Result Ok() => new Result(true, false);

        public static Result<T> Ok<T>(T value) => new Result<T>(value, true, false);
    }

    public sealed class Result<T> : Result
    {
        public T Value { get; }

        public Result(T value, bool isSuccess, bool isPartial) : this(value, isSuccess, isPartial, null) { }

        public Result(T value, bool isSuccess, bool isPartial, string error) : base(isSuccess, isPartial, error)
        {
            Value = value;
        }
    }
}
