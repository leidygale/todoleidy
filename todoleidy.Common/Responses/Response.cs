using System;
using System.Collections.Generic;
using System.Text;

namespace todoleidy.Common.Responses
{
    public class Response
    {
        public bool IsSuccess { get; set; } // Devuelve el resultado si es true, si es false envia mensaje

        public string Message { get; set; } //En caso que falle, saca mensaje

        public object Result { get; set; } // 

    }
}
