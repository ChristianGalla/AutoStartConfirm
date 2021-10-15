using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStartConfirm.Exceptions {
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
