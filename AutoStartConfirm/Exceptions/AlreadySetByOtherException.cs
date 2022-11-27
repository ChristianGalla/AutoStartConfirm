using System;

namespace AutoStartConfirm.Exceptions
{
    public class AlreadySetByOtherException : InvalidOperationException {
        public AlreadySetByOtherException() {
        }

        public AlreadySetByOtherException(string message)
            : base(message) {
        }

        public AlreadySetByOtherException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}
