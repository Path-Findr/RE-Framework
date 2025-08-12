using System;
using System.Activities;
using UiPath.CodedWorkflows;
using UiPath.CodedWorkflows.Utils;
using System.Runtime;

namespace PathFindrRoboticEnterpriseFramework.Framework
{
    [System.ComponentModel.Browsable(false)]
    public class SetTransactionStatusActivity : System.Activities.Activity
    {
        public InArgument<UiPath.Core.BusinessRuleException> in_BusinessException { get; set; }
        public InArgument<System.Exception> in_SystemException { get; set; }
        public InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>> in_Config { get; set; }
        public InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>> in_OutputData { get; set; }
        public InArgument<UiPath.Core.QueueItem> in_TransactionItem { get; set; }
        public InOutArgument<System.Int32> io_RetryNumber { get; set; }
        public InOutArgument<System.Int32> io_TransactionNumber { get; set; }
        public InOutArgument<System.Int32> io_ConsecutiveSystemExceptions { get; set; }

        public SetTransactionStatusActivity()
        {
            this.Implementation = () =>
            {
                return new SetTransactionStatusActivityChild()
                {
                    in_BusinessException = (this.in_BusinessException == null ? (InArgument<UiPath.Core.BusinessRuleException>)Argument.CreateReference((Argument)new InArgument<UiPath.Core.BusinessRuleException>(), "in_BusinessException") : (InArgument<UiPath.Core.BusinessRuleException>)Argument.CreateReference((Argument)this.in_BusinessException, "in_BusinessException")),
                    in_SystemException = (this.in_SystemException == null ? (InArgument<System.Exception>)Argument.CreateReference((Argument)new InArgument<System.Exception>(), "in_SystemException") : (InArgument<System.Exception>)Argument.CreateReference((Argument)this.in_SystemException, "in_SystemException")),
                    in_Config = (this.in_Config == null ? (InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>>)Argument.CreateReference((Argument)new InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>>(), "in_Config") : (InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>>)Argument.CreateReference((Argument)this.in_Config, "in_Config")),
                    in_OutputData = (this.in_OutputData == null ? (InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>>)Argument.CreateReference((Argument)new InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>>(), "in_OutputData") : (InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>>)Argument.CreateReference((Argument)this.in_OutputData, "in_OutputData")),
                    in_TransactionItem = (this.in_TransactionItem == null ? (InArgument<UiPath.Core.QueueItem>)Argument.CreateReference((Argument)new InArgument<UiPath.Core.QueueItem>(), "in_TransactionItem") : (InArgument<UiPath.Core.QueueItem>)Argument.CreateReference((Argument)this.in_TransactionItem, "in_TransactionItem")),
                    io_RetryNumber = (this.io_RetryNumber == null ? (InOutArgument<System.Int32>)Argument.CreateReference((Argument)new InOutArgument<System.Int32>(), "io_RetryNumber") : (InOutArgument<System.Int32>)Argument.CreateReference((Argument)this.io_RetryNumber, "io_RetryNumber")),
                    io_TransactionNumber = (this.io_TransactionNumber == null ? (InOutArgument<System.Int32>)Argument.CreateReference((Argument)new InOutArgument<System.Int32>(), "io_TransactionNumber") : (InOutArgument<System.Int32>)Argument.CreateReference((Argument)this.io_TransactionNumber, "io_TransactionNumber")),
                    io_ConsecutiveSystemExceptions = (this.io_ConsecutiveSystemExceptions == null ? (InOutArgument<System.Int32>)Argument.CreateReference((Argument)new InOutArgument<System.Int32>(), "io_ConsecutiveSystemExceptions") : (InOutArgument<System.Int32>)Argument.CreateReference((Argument)this.io_ConsecutiveSystemExceptions, "io_ConsecutiveSystemExceptions")),
                };
            };
        }
    }

    internal class SetTransactionStatusActivityChild : UiPath.CodedWorkflows.AsyncTaskCodedWorkflowActivity
    {
        public InArgument<UiPath.Core.BusinessRuleException> in_BusinessException { get; set; }
        public InArgument<System.Exception> in_SystemException { get; set; }
        public InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>> in_Config { get; set; }
        public InArgument<System.Collections.Generic.Dictionary<System.String, System.Object>> in_OutputData { get; set; }
        public InArgument<UiPath.Core.QueueItem> in_TransactionItem { get; set; }
        public InOutArgument<System.Int32> io_RetryNumber { get; set; }
        public InOutArgument<System.Int32> io_TransactionNumber { get; set; }
        public InOutArgument<System.Int32> io_ConsecutiveSystemExceptions { get; set; }
        public System.Collections.Generic.IDictionary<string, object> newResult { get; set; }

        public SetTransactionStatusActivityChild()
        {
            DisplayName = "SetTransactionStatus";
        }

        protected override async System.Threading.Tasks.Task<Action<AsyncCodeActivityContext>> ExecuteAsync(AsyncCodeActivityContext context, System.Threading.CancellationToken cancellationToken)
        {
            var var_in_BusinessException = in_BusinessException.Get(context);
            var var_in_SystemException = in_SystemException.Get(context);
            var var_in_Config = in_Config.Get(context);
            var var_in_OutputData = in_OutputData.Get(context);
            var var_in_TransactionItem = in_TransactionItem.Get(context);
            var var_io_RetryNumber = io_RetryNumber.Get(context);
            var var_io_TransactionNumber = io_TransactionNumber.Get(context);
            var var_io_ConsecutiveSystemExceptions = io_ConsecutiveSystemExceptions.Get(context);
            var codedWorkflow = new global::PathFindrRoboticEnterpriseFramework.SetTransactionStatus();
            CodedWorkflowHelper.Initialize(codedWorkflow, new UiPath.CodedWorkflows.Utils.CodedWorkflowsFeatureChecker(new System.Collections.Generic.List<string>() { UiPath.CodedWorkflows.Utils.CodedWorkflowsFeatures.AsyncEntrypoints }), context);
            var result = await System.Threading.Tasks.Task.Run(() => CodedWorkflowHelper.RunWithExceptionHandlingAsync(() =>
            {
                if (codedWorkflow is IBeforeAfterRun codedWorkflowWithBeforeAfter)
                {
                    codedWorkflowWithBeforeAfter.Before(new BeforeRunContext() { RelativeFilePath = "Framework\\SetTransactionStatus.cs" });
                }

                return System.Threading.Tasks.Task.CompletedTask;
            }, () =>
            {
                CodedExecutionHelper.Run(() =>
                {
                    {
                        var result = codedWorkflow.Execute(var_in_BusinessException, var_in_SystemException, var_in_Config, var_in_OutputData, var_in_TransactionItem, var_io_RetryNumber, var_io_TransactionNumber, var_io_ConsecutiveSystemExceptions);
                        newResult = new System.Collections.Generic.Dictionary<string, object>
                        {
                            {
                                "io_RetryNumber",
                                result.io_RetryNumber
                            },
                            {
                                "io_TransactionNumber",
                                result.io_TransactionNumber
                            },
                            {
                                "io_ConsecutiveSystemExceptions",
                                result.io_ConsecutiveSystemExceptions
                            },
                        };
                    }
                }, cancellationToken);
                return System.Threading.Tasks.Task.FromResult(newResult);
            }, (exception, outArgs) =>
            {
                if (codedWorkflow is IBeforeAfterRun codedWorkflowWithBeforeAfter)
                {
                    codedWorkflowWithBeforeAfter.After(new AfterRunContext() { RelativeFilePath = "Framework\\SetTransactionStatus.cs", Exception = exception });
                }

                return System.Threading.Tasks.Task.CompletedTask;
            }), cancellationToken);
            return endContext =>
            {
                io_RetryNumber.Set(endContext, (System.Int32)result["io_RetryNumber"]);
                io_TransactionNumber.Set(endContext, (System.Int32)result["io_TransactionNumber"]);
                io_ConsecutiveSystemExceptions.Set(endContext, (System.Int32)result["io_ConsecutiveSystemExceptions"]);
            };
        }
    }
}