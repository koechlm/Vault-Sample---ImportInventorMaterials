using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Connectivity.Extensibility.Framework;
using Autodesk.DataManagement.Client.Framework.Vault.Currency.Entities;
using Autodesk.Connectivity.JobProcessor.Extensibility;
using Autodesk.Connectivity.WebServices;

[assembly: ApiVersion("12.0")]
[assembly: ExtensionId("494c7aba-9cf2-4d8f-a3ca-75947a3a4cc4")]


namespace API_Onboarding_Custom_Job
{
    public class JobExtension : IJobHandler
    {
        private static string JOB_TYPE = "YourCompany.JobName";

        #region IJobHandler Implementation
        public bool CanProcess(string jobType)
        {
            return jobType == JOB_TYPE;
        }

        public JobOutcome Execute(IJobProcessorServices context, IJob job)
        {
            try
            {
                MessageBox.Show("Hello World", "Job-Template-Messenger", MessageBoxButtons.OK);
                return JobOutcome.Success;
            }
            catch (Exception ex)
            {
                context.Log(ex, "Job-Template Job failed: " + ex.ToString() + " ");
                return JobOutcome.Failure;
            }

        }

        public void OnJobProcessorShutdown(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }

        public void OnJobProcessorSleep(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }

        public void OnJobProcessorStartup(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }

        public void OnJobProcessorWake(IJobProcessorServices context)
        {
            //throw new NotImplementedException();
        }
        #endregion IJobHandler Implementation
    }
}
