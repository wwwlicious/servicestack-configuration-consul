// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this 
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

namespace ServiceStack.Configuration.Consul
{
    /// <summary>
    /// The consul response DTO
    /// </summary>
    /// <typeparam name="T">the expected response type</typeparam>
    public class Result<T>
    {
        /// <summary>
        /// Returns true if the value was found, otherise false
        /// </summary>
        public bool IsSuccess { get; }
        
        /// <summary>
        /// The returned value from consul
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// The consul lookup response 
        /// </summary>
        /// <param name="value">the value returned or a default</param>
        /// <param name="isSuccess">true if the lookup was successful, otherwise false</param>
        protected Result(T value, bool isSuccess)
        {
            IsSuccess = isSuccess;
            Value = value;
        }

        /// <summary>
        /// Represents a lookup failure response from consul
        /// </summary>
        /// <returns>the value type expected</returns>
        public static Result<T> Fail()
        {
            return new Result<T>(default(T), false);
        }
        
        /// <summary>
        /// Represents a lookup failure response from consul with a default
        /// </summary>
        /// <param name="value">the default value</param>
        /// <returns>the value type expected</returns>
        public static Result<T> Fail(T value)
        {
            return new Result<T>(value, false);
        }

        /// <summary>
        /// Represents a successful response from consul with the returned value
        /// </summary>
        /// <param name="value">the returned value</param>
        /// <returns>the consul response value</returns>
        public static Result<T> Success(T value)
        {
            return new Result<T>(value, true);
        }
    }
}