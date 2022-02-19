using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Exceptions {
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
