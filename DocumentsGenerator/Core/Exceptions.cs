using DocumentFormat.OpenXml.Presentation;
using System;

namespace DocumentsGenerator.Core
{
    public class JsonSanitizationException : Exception
    {
        public string Value { get; set; }
        public JsonSanitizationException() {
            Value = string.Empty;
        }
        public JsonSanitizationException(string message) : base(message) {
            Value = string.Empty;
        }
        public JsonSanitizationException(string message, string value) : base(message)
        {
            Value = value;
        }
        public JsonSanitizationException(string message, Exception innerException) : base(message, innerException) {
            Value = string.Empty;
        }
    }

    public class JsonValueIsEmptyException : Exception
    {
        public string Value { get; set; }
        public JsonValueIsEmptyException() {
            Value = string.Empty;
        }
        public JsonValueIsEmptyException(string message) : base(message) {
            Value = string.Empty;
        }
        public JsonValueIsEmptyException(string message, string value) : base(message) {
            Value = value;
        }
        public JsonValueIsEmptyException(string message, Exception innerException) : base(message, innerException) {
            Value = string.Empty;
        }
    }

    public class JsonValueIsDuplicateException : Exception
    {
        public string Value { get; set; }
        public JsonValueIsDuplicateException() {
            Value = string.Empty;
        }
        public JsonValueIsDuplicateException(string message) : base(message) {
            Value = string.Empty;
        }
        public JsonValueIsDuplicateException(string message, string value) : base(message)
        {
            Value = value;
        }
        public JsonValueIsDuplicateException(string message, Exception innerException) : base(message, innerException) {
            Value = string.Empty;
        }
    }
}
