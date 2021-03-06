using CodeQualityExplorer.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeQualityExplorer.Pages
{
    public partial class JobData
    {
        [Parameter]
        public string JobName { get; set; }

        [CascadingParameter]
        public JobLayout Layout { get; set; }

        protected override void OnAfterRender(bool firstRender)
        {
            Layout.JobName = JobName;
        }
    }
}
