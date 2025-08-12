using System;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using UiPath.Core;
using UiPath.Core.Activities;
using UiPath.Robot.Activities.Api;
using Newtonsoft.Json;
using LogLevel = UiPath.CodedWorkflows.LogLevel;

namespace PathFindrRoboticEnterpriseFramework
{
    public class SetTransactionStatus : CodedWorkflow
    {
        
        [Workflow]
        public (int io_RetryNumber,int io_TransactionNumber,int io_ConsecutiveSystemExceptions) Execute(
                                BusinessRuleException in_BusinessException,
                                Exception in_SystemException,
                                Dictionary<string,Object> in_Config,
                                Dictionary<string,Object> in_OutputData,
                                QueueItem in_TransactionItem,
                                int io_RetryNumber,
                                int io_TransactionNumber,
                                int io_ConsecutiveSystemExceptions)
        {
            /*
                Changed Set Transaction Status into a coded workflow to allow for scalable QueueItem Output Data and Analytics Data.
                The functionality / steps are still the same between this coded workflow and the out of the box sequence in the RE-Framework
            */
            

            Dictionary<string,object> analyticsData = GetAnalyticsData();
            SerializeOutputData(in_OutputData ??= new());
            
            
            if(in_BusinessException == null && in_SystemException == null)
            {
                //Sucessful Transaction   
                HandleSuccess(in_TransactionItem,in_Config,in_OutputData,analyticsData);
                IncrementTransaction(ref io_TransactionNumber,ref io_ConsecutiveSystemExceptions, ref io_RetryNumber);
            }
            else if(in_BusinessException != null)
            {
                //Business Exception
                HandleBusinessException(in_TransactionItem,in_BusinessException,in_Config,in_OutputData,analyticsData);
                IncrementTransaction(ref io_TransactionNumber,ref io_ConsecutiveSystemExceptions, ref io_RetryNumber);
            }
            else
            {
                //System Exception
                Log("Consecutive system exception counter is " + io_ConsecutiveSystemExceptions.ToString() + ".");
                var screenShotPath = TakeScreenshot(in_Config["ExScreenshotsFolderPath"].ToString());
                var queueRetry = HandleSystemException(in_TransactionItem,in_SystemException,in_Config,screenShotPath,ref io_RetryNumber,in_OutputData,analyticsData);
                io_ConsecutiveSystemExceptions++;
                HandleRetryTransaction(in_Config,ref io_RetryNumber,ref io_TransactionNumber,in_SystemException,queueRetry);
                CloseKillApplications();
            }
            return (io_RetryNumber,io_TransactionNumber,io_ConsecutiveSystemExceptions);
        }
        
        
        
        
        
        
        
        
        
        
        /// <summary>
        /// Executes a given action with retry attempts and wait interval on failure.
        /// </summary>
        private void Retry(Action action,int retryInterval,int retryCount)
        {
            try
            {
               while(true)
                {
                    try
                    {
                        action();
                        break;
                    }
                    catch(Exception exception)
                    {
                        if(retryCount-- <= 0)
                            throw exception;
                        
                        Delay(retryInterval);
                    }
                } 
            }
            catch(Exception e)
            {
                Log("Could not set the transaction status. "+e.Message+" At Source: "+e.Source);
            }
        }
        
        /// <summary>
        /// Runs RetryCurrentTransaction workflow. Only used in System Exceptions.
        /// </summary>
        private void HandleRetryTransaction(Dictionary<string,object> config, ref int retryNumber,ref int transactionNumber,Exception systemException,bool queueRetry)
        {
            var result = RunWorkflow("Framework\\RetryCurrentTransaction.xaml", new Dictionary<string, object>
            {
                {"in_Config",config},
                {"io_RetryNumber",retryNumber},
                {"io_TransactionNumber",transactionNumber},
                {"in_SystemException",systemException},
                {"in_QueueRetry",queueRetry}
            });
            retryNumber = (int) result["io_RetryNumber"];
            transactionNumber = (int) result["io_TransactionNumber"];
        }
        
        /// <summary>
        /// Handles setting transaction status to successful.
        /// </summary>
        private void HandleSuccess(QueueItem item, Dictionary<string, object> config,Dictionary<string,object> outputData,Dictionary<string,object> analyticData)
        {
            int retryCount =  Int32.Parse(config["RetryNumberSetTransactionStatus"].ToString());
            
            Retry(() => 
            {
                if (item is QueueItem && item?.Status == QueueItemStatus.InProgress)
                    system.SetTransactionStatus(item,
                        ProcessingStatus.Successful,
                        config["OrchestratorQueueFolder"].ToString(),
                        analyticData, outputData, "", ErrorType.Application, "", 30000);
            },1000,retryCount);
            
            LogSuccess(config["LogMessage_Success"].ToString());
        }
        
        
         /// <summary>
        /// Handles setting transaction status to failed due to a business exception.
        /// </summary>
        private void HandleBusinessException(QueueItem item, BusinessRuleException ex, Dictionary<string, object> config,Dictionary<string,object> outputData,Dictionary<string,object> analyticData)
        {
            int retryCount =  Int32.Parse(config["RetryNumberSetTransactionStatus"].ToString());
            AddFaultedDetails(analyticData,ex);
            
            Retry(() => 
            {
                if (item is QueueItem && item?.Status == QueueItemStatus.InProgress)
                    system.SetTransactionStatus(item,
                        ProcessingStatus.Failed,
                        config["OrchestratorQueueFolder"].ToString(),
                        analyticData, outputData, "", ErrorType.Business, ex.Message, 30000);
            },1000,retryCount);
            
            LogBusinessException(config["LogMessage_BusinessRuleException"].ToString());
        }
        
        
        /// <summary>
        /// Handles setting transaction status to failed due to a system exception.
        /// </summary>
        private bool HandleSystemException(QueueItem item, Exception ex, Dictionary<string, object> config,String details,ref int retryNumber,Dictionary<string,object> outputData,Dictionary<string,object> analyticData)
        {
            int retryCount =  Int32.Parse(config["RetryNumberSetTransactionStatus"].ToString());
            bool queueRetry = item is QueueItem && item?.Status == QueueItemStatus.InProgress;
            AddFaultedDetails(analyticData,ex);
            
            Retry(() => 
            {
                if (queueRetry)
                    system.SetTransactionStatus(item,
                        ProcessingStatus.Failed,
                        config["OrchestratorQueueFolder"].ToString(),
                        analyticData, outputData, String.IsNullOrEmpty(details) ? String.Empty: "Screenshot: "+ details, ErrorType.Application, ex.Message, 30000);
            },1000,retryCount);
            
            if(queueRetry)
                retryNumber = item.RetryNo;
            
            return queueRetry;
        }
        
        
        /// <summary>
        /// Runs the screenshot workflow and returns the screenshot path (if successful).
        /// </summary>
        private string TakeScreenshot(string folder)
        {
            string screenshotPath = "";
            try
            {
                
                var result = RunWorkflow("Framework\\TakeScreenshot.xaml", new Dictionary<string, object>
                {
                    { "in_Folder", folder },
                    { "io_FilePath", screenshotPath }
                });
                screenshotPath = result["io_FilePath"].ToString();
            }
            catch (Exception ex)
            {
                Log("Failed to take screenshot: " + ex.Message + " at Source: " + ex.Source);
            }
            return screenshotPath;
        }
        
        
        /// <summary>
        /// Attempts to close all applications. Falls back to killing processes if needed.
        /// </summary>
        private void CloseKillApplications(){
            try
            {
                RunWorkflow("Framework\\CloseAllApplications.xaml");
            }
            catch(Exception CloseException)
            {
                Log("CloseAllApplications failed. "+CloseException.Message+" at Source: "+CloseException.Source);
                try
                {
                    RunWorkflow("Framework\\KillAllProcesses.xaml");
                }
                catch(Exception KillException)
                {
                    Log("KillAllProcesses failed. "+KillException.Message+" at Source: "+KillException.Source);
                }
            }
        }
        
        
        /// <summary>
        /// Increments the transaction number and resets retry and consecutive system exception counters.
        /// </summary>
        private void IncrementTransaction(ref int transactionNumber,ref int consecutiveSystemExceptions, ref int retryNumber)
        {
            transactionNumber +=1;
            consecutiveSystemExceptions = 0;
            retryNumber = 0;
        }
        
        
        /// <summary>
        /// Logs a successful transaction event.
        /// </summary>
        private void LogSuccess(string message)
        {
            Log(message, LogLevel.Info, new Dictionary<string, object>
            {
                { "logF_TransactionStatus", "Success" }
            });
        }
        
        
        /// <summary>
        /// Logs a business exception event.
        /// </summary>
        private void LogBusinessException(string message)
        {
            Log(message, LogLevel.Error, new Dictionary<string, object>
            {
                { "logF_TransactionStatus", "BusinessException" }
            });
        }
        
        
        /// <summary>
        /// Adds Stack Trace and Exception Type to analytics data. To be used only for Business and System Exceptions
        /// </summary>
        private void AddFaultedDetails(Dictionary<string,object> analyticData,Exception ex)
        {
            if (ex.Data.Contains("FaultedDetails"))
            {
                var faultedDetails = ex.Data["FaultedDetails"];
                var trace = faultedDetails.GetType().GetProperty("WorkflowExceptionStackTrace")?.GetValue(faultedDetails);
                if (trace != null)
                    analyticData["Stack Trace"] = trace;
            }
            analyticData["Exception Type"] = ex.GetType().ToString();
        }
        
        
        /// <summary>
        /// Returns a Dictionary of all analytics data that will be used when setting the transaction status
        /// </summary>
        private Dictionary<string,object> GetAnalyticsData(){
            
            IRunningJobInformation currentJob = GetRunningJobInformation();

            return new(){
                {"Hostname",Environment.MachineName},
                {"Process",currentJob.ProcessName + "-" + currentJob.ProcessVersion},
                {"Job ID",currentJob.JobId},
                {"Folder Name",currentJob.FolderName}
            };
        }
        
        /// <summary>
        /// Serializes all of the output data, if an item cannot be serialized it is removed from the output data
        /// </summary>
        private void SerializeOutputData(Dictionary<string,object> outputData)
        {
            var keysToRemove = new List<string>();

            foreach (var key in outputData.Keys)
            {
                try
                {
                    if(!IsBasicType(outputData[key]))
                        outputData[key] = JsonConvert.SerializeObject(outputData[key],Formatting.Indented);
                }catch(Exception e)
                {
                    Log("Cannot Add " + key +" to Output Data as it cannot be serialized: " + e.Message,LogLevel.Error);
                    keysToRemove.Add(key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                outputData.Remove(key);
            }
        }
        
        public bool IsBasicType(object obj)
        {
            if (obj == null) return false;
        
            var type = obj.GetType();
        
            return type.IsPrimitive
                   || type == typeof(string)
                   || type == typeof(decimal)
                   || type == typeof(DateTime);
        }
    }
}