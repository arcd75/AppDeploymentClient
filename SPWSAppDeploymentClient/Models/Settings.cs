using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SPWSAppDeploymentClient.Models
{
    public class Settings
    {
        public string Server { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int AppId { get; set; }
        public string AppName { get; set; }
        public string AppVersion { get; set; }
    }
}
