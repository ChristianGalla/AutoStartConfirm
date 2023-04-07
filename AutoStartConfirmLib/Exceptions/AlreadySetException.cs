using System;

namespace AutoStartConfirm.Exceptions
{
    public class AlreadySetException : InvalidOperationException {
        public AlreadySetException() {
        }

        public AlreadySetException(string message)
            : base(message) {
        }

        public AlreadySetException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
