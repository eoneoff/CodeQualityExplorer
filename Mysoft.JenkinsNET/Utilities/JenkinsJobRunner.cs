using Mysoft.JenkinsNET.Exceptions;
using Mysoft.JenkinsNET.Models;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Mysoft.JenkinsNET.Utilities
{
    /// <summary>
    /// Represents the method that will handle an
    /// event when the job status has changed.
    /// </summary>
    public delegate void StatusChangedEventHandler();

    /// <summary>
    /// Represents a method that will handle an
    /// event when the job console output has changed.
    /// </summary>
    public delegate void ConsoleOutputChangedEventHandler(string newText);

    /// <summary>
    /// Begins building a Jenkins Job, and polls the status
    /// until the job has completed.
    /// </summary>
    public class JenkinsJobRunner
    {
        private readonly IJenkinsClient client;
        private ProgressiveTextReader textReader;
        private bool isJobStarted;

        /// <summary>
        /// Occurs when the status of the running Jenkins Job changes.
        /// </summary>
        public event StatusChangedEventHandler StatusChanged;

        /// <summary>
        /// Occurs when the Console Output of the
        /// running Jenkins Job has changed.
        /// </summary>
        public event ConsoleOutputChangedEventHandler ConsoleOutputChanged;

        /// <summary>
        /// Gets the status of the running Jenkins Job.
        /// </summary>
        public JenkinsJobStatus Status { get; private set; }

        /// <summary>
        /// Gets or sets whether the Jobs Console Output
        /// should be monitored. Default value is false.
        /// </summary>
        public bool MonitorConsoleOutput { get; set; }

        /// <summary>
        /// Time in milliseconds to wait between requests.
        /// Default value is 500.
        /// </summary>
        public int PollInterval { get; set; } = 500;

        /// <summary>
        /// Maximum time in seconds to wait for job to be queued.
        /// Default value is 60 (one minute).
        /// </summary>
        public int QueueTimeout { get; set; } = 60;

        /// <summary>
        /// Maximum time in seconds to wait for job to complete.
        /// Default value is 600 (10 minutes).
        /// </summary>
        public int BuildTimeout { get; set; } = 600;

        /// <summary>
        /// Returns the Console Output of the currently running Jenkins Job.
        /// </summary>
        public string ConsoleOutput => textReader?.Text;


        /// <summary>
        /// Creates a new JobRunner using the provided Jenkins-Client.
        /// </summary>
        public JenkinsJobRunner(IJenkinsClient client)
        {
            this.client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Run the Job.
        /// </summary>
        /// <param name="jobName">The name of the Job to run.</param>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="JenkinsJobBuildException"></exception>
        /// <exception cref="JenkinsJobGetBuildException"></exception>
        public JenkinsBuildBase Run(string jobName)
        {
            if (isJobStarted)
            {
                throw new JenkinsNetException("This JobRunner instance has already been started! Separate JenkinsJobRunner instances are required to run multiple jobs.");
            }

            isJobStarted = true;

            SetStatus(JenkinsJobStatus.Pending);
            var queueStartTime = DateTime.Now;

            var buildResult = client.Jobs.Build(jobName);

            if (buildResult == null)
            {
                throw new JenkinsJobBuildException("An empty build response was returned!");
            }

            return Process(jobName, buildResult, queueStartTime);
        }

        /// <summary>
        /// Run the Job with parameters.
        /// </summary>
        /// <param name="jobName">The name of the Job to run.</param>
        /// <param name="jobParameters">The parameters used to start the Job.</param>
        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="JenkinsJobBuildException"></exception>
        /// <exception cref="JenkinsJobGetBuildException"></exception>
        public JenkinsBuildBase RunWithParameters(string jobName, IDictionary<string, string> jobParameters)
        {
            if (isJobStarted)
            {
                throw new JenkinsNetException("This JobRunner instance has already been started! Separate JenkinsJobRunner instances are required to run multiple jobs.");
            }

            isJobStarted = true;

            SetStatus(JenkinsJobStatus.Pending);
            var queueStartTime = DateTime.Now;

            var buildResult = client.Jobs.BuildWithParameters(jobName, jobParameters);

            if (buildResult == null)
            {
                throw new JenkinsJobBuildException("An empty build response was returned!");
            }

            return Process(jobName, buildResult, queueStartTime);
        }

        /// <exception cref="JenkinsNetException"></exception>
        /// <exception cref="JenkinsJobBuildException"></exception>
        /// <exception cref="JenkinsJobGetBuildException"></exception>
        private JenkinsBuildBase Process(string jobName, JenkinsBuildResult buildResult, DateTime queueStartTime)
        {
            var queueItemNumber = buildResult.GetQueueItemNumber();
            if (!queueItemNumber.HasValue)
            {
                throw new JenkinsNetException("Queue-Item number not found!");
            }

            SetStatus(JenkinsJobStatus.Queued);

            int? buildNumber;
            while (true)
            {
                var queueItem = client.Queue.GetItem(queueItemNumber.Value);
                buildNumber = queueItem?.Executable?.Number;
                if (buildNumber.HasValue)
                {
                    break;
                }

                if (QueueTimeout > 0 && DateTime.Now.Subtract(queueStartTime).TotalSeconds > QueueTimeout)
                {
                    throw new JenkinsNetException("Timeout occurred while waiting for build to start!");
                }

                Thread.Sleep(PollInterval);
            }

            SetStatus(JenkinsJobStatus.Building);
            var buildStartTime = DateTime.Now;

            textReader = new ProgressiveTextReader(client, jobName, buildNumber.ToString());
            textReader.TextChanged += TextReader_TextChanged;

            JenkinsBuildBase buildItem = null;
            while (string.IsNullOrEmpty(buildItem?.Result))
            {
                buildItem = client.Builds.Get<JenkinsBuildBase>(jobName, buildNumber.Value.ToString());
                if (!string.IsNullOrEmpty(buildItem?.Result))
                {
                    break;
                }

                if (BuildTimeout > 0 && DateTime.Now.Subtract(buildStartTime).TotalSeconds > BuildTimeout)
                {
                    throw new JenkinsNetException("Timeout occurred while waiting for build to complete!");
                }

                if (MonitorConsoleOutput && !textReader.IsComplete)
                {
                    textReader.Update();
                }

                Thread.Sleep(PollInterval);
            }

            while (MonitorConsoleOutput && !textReader.IsComplete)
            {
                textReader.Update();
            }

            SetStatus(JenkinsJobStatus.Complete);
            return buildItem;
        }

        private void TextReader_TextChanged(string newText)
        {
            ConsoleOutputChanged?.Invoke(newText);
        }

        private void SetStatus(JenkinsJobStatus newStatus)
        {
            Status = newStatus;

            try
            {
                StatusChanged?.Invoke();
            }
            catch
            {
                //预防订阅时间中出错,吃掉异常
            }
        }
    }
}
