using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatData
{
    [Serializable]
    public class ChatMessage
    {
        public String From { get; set; }
        public String To { get; set; }
        public String Command { get; set; }
        public String TextMessage { get; set; }
        public String FileName { get; set; }
        public byte[] FileData { get; set; }
        public DateTime CreatedAT { get; set; }
    }
}
