using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWSAppDeploymentClient.Models
{
    public class AppFileBlob
    {
        [Key]
        public int AppFileBlobId { get; set; }
        public int AppFileId { get; set; }
        public byte[] FileBlob { get; set; }
    }
}
