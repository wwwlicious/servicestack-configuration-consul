// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    public class Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }

        protected Result(T value, bool isSuccess)
        {
            IsSuccess = isSuccess;
            Value = value;
        }

        public static Result<T> Fail()
        {
            return new Result<T>(default(T), false);
        }

        public static Result<T> Fail(T value)
        {
            return new Result<T>(value, false);
        }

        public static Result<T> Success(T value)
        {
            return new Result<T>(value, true);
        }
    }
}