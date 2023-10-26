using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalChat.Domain.DTOs
{
    public class FileUploadDto
    {
        
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public Guid? GroupId { get; set; }
        public byte[] FileData { get; set; }
        public string FileName { get; set; }
        public string UploadDirectory { get; set; }

    }
}
