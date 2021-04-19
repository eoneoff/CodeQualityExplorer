using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Mysoft.JenkinsNET;
using Mysoft.JenkinsNET.Models;

namespace CodeQualityExplorer.Shared
{
    public partial class MainLayout
    {
        [Inject]
        public  IJenkinsHttpClient JenkinsClient { get; set; }

        protected List<JenkinsProject> Jobs { get; set; }


        protected override async Task OnInitializedAsync()
        {
            var jobs = (await JenkinsClient.JenkinsGet()).Jobs.ToList();
            Jobs = jobs;
        }
    }
}
