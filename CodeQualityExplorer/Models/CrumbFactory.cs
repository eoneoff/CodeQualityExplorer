using Mysoft.JenkinsNET;
using Mysoft.JenkinsNET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace CodeQualityExplorer.Models
{
    public class CrumbFactory
    {
        private readonly JenkinsAuth _authClient;

        private JenkinsCrumb crumb;

        public JenkinsCrumb Crumb
        {
            get
            {
                crumb ??= _authClient.TryGetCrumb();
                return crumb;
            }
        }

        public CrumbFactory(JenkinsAuth authClient) => _authClient = authClient;
    }
}
