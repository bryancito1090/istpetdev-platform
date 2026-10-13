using System;

namespace IstpetDev.Application.Field;

public class FieldException : Exception
{
    public int StatusCode { get; }
    public string Code { get; }

    public FieldException(int statusCode, string code, string message) : base(message)
    {
        StatusCode = statusCode;
        Code = code;
    }
}

public class FieldConflictException : FieldException
{
    public FieldConflictException(string code, string message) : base(409, code, message) { }
}

public class FieldForbiddenException : FieldException
{
    public FieldForbiddenException(string code, string message) : base(403, code, message) { }
}

public class FieldNotFoundException : FieldException
{
    public FieldNotFoundException(string code, string message) : base(404, code, message) { }
}

public class FieldUnprocessableException : FieldException
{
    public FieldUnprocessableException(string code, string message) : base(422, code, message) { }
}
