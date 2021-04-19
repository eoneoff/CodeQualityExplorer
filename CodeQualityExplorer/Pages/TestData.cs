using CodeQualityExplorer.Shared;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeQualityExplorer.Pages
{
    public partial class TestData
    {
        [Parameter]
        public string JobName { get; set; }

        [CascadingParameter]
        public JobLayout Layout { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Layout.JobName = JobName;
        }
    }
}
