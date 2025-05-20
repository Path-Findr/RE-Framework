using Newtonsoft.Json;
using PathFindrRoboticEnterpriseFramework.ObjectRepository;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using UiPath.CodedWorkflows;
using UiPath.Core;
using UiPath.Core.Activities;
using UiPath.Core.Activities.Storage;
using UiPath.Excel;
using UiPath.Excel.Activities;
using UiPath.Excel.Activities.API;
using UiPath.Excel.Activities.API.Models;
using UiPath.Orchestrator.Client.Models;
using UiPath.Robot.Activities.Api;
using UiPath.Testing;
using UiPath.Testing.Activities.Api.Models;
using UiPath.Testing.Activities.Models;
using UiPath.Testing.Activities.TestData;
using UiPath.Testing.Activities.TestDataQueues.Enums;
using UiPath.Testing.Enums;
using UiPath.UIAutomationNext.API.Contracts;
using UiPath.UIAutomationNext.API.Models;
using UiPath.UIAutomationNext.Enums;
using LogLevel = UiPath.CodedWorkflows.LogLevel;

namespace PathFindrRoboticEnterpriseFramework
{
    public class SetTransactionStatus : CodedWorkflow
    {
        [Workflow]
        public (int io_RetryNumber,int io_TransactionNumber,int io_ConsecutiveSystemExceptions) Execute(
                                BusinessRuleException in_BusinessException,
                                string in_TransactionField1, 
                                string in_TransactionField2,
                                string in_TransactionID,
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
                LogSuccess(in_Config["LogMessage_Success"].ToString(),io_TransactionNumber,in_TransactionID,in_TransactionField1,in_TransactionField2);
                IncrementTransaction(ref io_TransactionNumber,ref io_ConsecutiveSystemExceptions, ref io_RetryNumber);
            }
            else if(in_BusinessException != null)
            {
                //Business Exception
                HandleBusinessException(in_TransactionItem,in_BusinessException,in_Config,in_OutputData,analyticsData);
                LogException(in_Config["LogMessage_BusinessRuleException"].ToString(),"BusinessException" ,io_TransactionNumber,in_TransactionID,in_TransactionField1,in_TransactionField2);
                IncrementTransaction(ref io_TransactionNumber,ref io_ConsecutiveSystemExceptions, ref io_RetryNumber);
            }
            else
            {
                //System Exception
                Log("Consecutive system exception counter is " + io_ConsecutiveSystemExceptions.ToString() + ".");
                var screenShotPath = TakeScreenshot(in_Config["ExScreenshotsFolderPath"].ToString());
                var queueRetry = HandleSystemException(in_TransactionItem,in_SystemException,in_Config,screenShotPath,ref io_RetryNumber,in_OutputData,analyticsData);
                io_ConsecutiveSystemExceptions++;
                HandleRetryTransaction(in_Config,ref io_RetryNumber,ref io_TransactionNumber,in_SystemException,queueRetry,in_TransactionID,in_TransactionField1,in_TransactionField2);
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
        private void HandleRetryTransaction(Dictionary<string,object> config, ref int retryNumber,ref int transactionNumber,Exception systemException,bool queueRetry,string transactionID,string transactionField1, string transactionField2)
        {
            var result = RunWorkflow("Framework\\RetryCurrentTransaction.xaml", new Dictionary<string, object>
            {
                {"in_Config",config},
                {"io_RetryNumber",retryNumber},
                {"io_TransactionNumber",transactionNumber},
                {"in_SystemException",systemException},
                {"in_QueueRetry",queueRetry},
                {"in_TransactionID",transactionID},
                {"in_TransactionField1",transactionField1},
                {"in_TransactionField2",transactionField2}
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
        private void LogSuccess(string message, int transactionNumber, string transactionID, string field1, string field2)
        {
            Log(message, LogLevel.Info, new Dictionary<string, object>
            {
                { "logF_TransactionStatus", "Success" },
                { "logF_TransactionNumber", transactionNumber.ToString() },
                { "logF_TransactionID", transactionID },
                { "logF_TransactionField1", field1 },
                { "logF_TransactionField2", field2 },
            });
        }
        
        
        /// <summary>
        /// Logs a business or system exception event.
        /// </summary>
        private void LogException(string message, string exceptionType, int transactionNumber, string transactionID, string field1, string field2)
        {
            Log(message, LogLevel.Error, new Dictionary<string, object>
            {
                { "logF_TransactionStatus", exceptionType },
                { "logF_TransactionNumber", transactionNumber.ToString() },
                { "logF_TransactionID", transactionID },
                { "logF_TransactionField1", field1 },
                { "logF_TransactionField2", field2 },
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
                    Log("Cannot Add " + key +" to Output Data as it cannot be serialized",LogLevel.Error);
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