﻿#nullable enable

namespace System
{
    public class DatabaseException : FrameworkException
    {
        public DatabaseException(ErrorCode errorCode, string? whoEntityName = null, string? detail = null, Exception? innerException = null)
            : base(errorCode, $"EntityName:{whoEntityName}, Detail:{detail}", innerException)
        {
        }

        public DatabaseException()
        {
        }

        public DatabaseException(string? message) : base(message)
        {
        }

        public DatabaseException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}