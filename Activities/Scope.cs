using Flower.Activities.Behaviors;
using System;
using System.Activities;
using System.Activities.DynamicUpdate;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace Flower.Basic.Activities
{
    [DefaultValue(true)]
    [ContentProperty("Activities")]
    public class Scope : NativeActivity, INotifyPropertyChanged
    {
        public Scope()
        {
            this.lastIndexHint = new Variable<int>();
            this.lastRetryHint = new Variable<int>();

            this.OnChildExecutionCompletedCallback = new CompletionCallback(OnChildExecutionCompleted);
            this.OnChildExecutionFailedCallback = new FaultCallback(OnChildExecutionFailed);
            this.DisplayName = "Scope";


            if (this.Cache_ManagedActionBehavior == null)
            {
                this.Cache_ManagedActionBehavior = new ManagedActionBehavior();
            }

            if (this.Cache_ContinueOnErrorBehavior == null)
            {
                this.Cache_ContinueOnErrorBehavior = new ContinueOnErrorBehavior();
            }


            if (this.Cache_RetryBehavior == null)
            {
                this.Cache_RetryBehavior = new RetryBehavior();
            }

            this.LogSettings = new LogConfiguration()
            {
                ExceptionLogLevel = ExceptionLogLevels.MessageAndSource,
                IncludeTiming = true,
                LogLevel = LogLevels.Information,
                LogBegin = true,
                LogFinish = false
            };


        }

        private DateTime TimingAnchor;
        private void LogBegin(NativeActivityContext context)
        {

            if (this.LogSettings.LogBegin)
            {
                StringBuilder logBeginMsg = new StringBuilder();
                logBeginMsg.AppendLine(string.Format("BEGIN: {0}", this.DisplayName));
                logBeginMsg.Append(string.Format(", Started at [{0}]", this.TimingAnchor));
                Helpers.Logger.Log(logBeginMsg.ToString());
            }

        }

        private void LogFinish(NativeActivityContext context)
        {
            if (this.LogSettings.LogFinish)
            {
                StringBuilder logBeginMsg = new StringBuilder();
                logBeginMsg.AppendLine(string.Format("FINISH: {0}", this.DisplayName));
                logBeginMsg.Append(string.Format(", Completed at [{0}], Total Execution Time [{1}]", DateTime.Now, (DateTime.Now - this.TimingAnchor)));
                Helpers.Logger.Log(logBeginMsg.ToString());
            }
        }
        protected override void OnCreateDynamicUpdateMap(NativeActivityUpdateMapMetadata metadata, Activity originalActivity)
        {
            for (int i = 0; i < this.Activities.Count - 1; i++)
            {
                for (int j = i + 1; j < this.Activities.Count; j++)
                {
                    if (this.Activities[i] == this.Activities[j])
                    {
                        metadata.DisallowUpdateInsideThisActivity(
                            string.Format("Duplicate references of activities not allowed! Activity [Id: {2}, Name: {0}] and [Id: {3}, Name: {1}] are duplicate of each other",
                            this.Activities[i].DisplayName,
                            this.Activities[j].DisplayName,
                            this.Activities[i].Id,
                            this.Activities[j].Id));
                        break;
                    }
                }
            }

        }
        protected override void CacheMetadata(NativeActivityMetadata metadata)
        {
            base.CacheMetadata(metadata);
            metadata.SetChildrenCollection(this.Activities);
            metadata.SetVariablesCollection(this.Variables);
            metadata.AddImplementationVariable(this.lastIndexHint);
            metadata.AddImplementationVariable(this.lastRetryHint);

        }

        protected override void Execute(NativeActivityContext context)
        {
            this.InitializeExecution(context);
        }

        public void InitializeExecution(NativeActivityContext context)
        {
            this.TimingAnchor = DateTime.Now;
            this.LogBegin(context);
            if (checkConditionSatisfied(context) && this.activities != null && this.Activities.Count > 0)
            {
                Activity activity = this.Activities.First();

                context.ScheduleActivity(activity, this.OnChildExecutionCompletedCallback, this.OnChildExecutionFailedCallback);
            }
        }

        private bool checkConditionSatisfied(NativeActivityContext context)
        {
            if (this.ConditionType == ConditionTypes.Activity && this.ConditionActivity != null)
            {
                context.ScheduleFunc(this.ConditionActivity, (cntxt, completedInstance, result) =>
                {
                    this.Condition = result;
                });
            }
            else if (this.ConditionType == ConditionTypes.Unconditional)
            {
                this.Condition = true; //enforce condition to be true
            }
            else //this branch is for expression type condition which directly sets Condition property so no need to check anythıng
            {

            }

            return this.Condition;
        }

        private void OnChildExecutionFailed(NativeActivityFaultContext context, Exception propagatedException, ActivityInstance propagatedFrom)
        {
            var FailoverBehavior = this.FailoverBehaviorCache[this.ActiveFailoverBehaviorCacheIndex];

            if (FailoverBehavior is RetryBehavior)
            {
                context.HandleFault();
                RetryBehavior beh = FailoverBehavior as RetryBehavior;

                beh.CurrentRetryAttempt.Set(context, this.lastRetryHint.Get(context));

                var a = context.ScheduleActivity(beh,
                    new CompletionCallback((c, cins) =>
                    {
                        if (beh.ShouldRetry.Get(context))
                        {
                            Task.Delay(beh.RetryInterval.Get(context)); //Delay

                            this.InitializeExecution(context);
                        }
                    })
                );

            }
            else if (FailoverBehavior is ContinueOnErrorBehavior)
            {
                context.HandleFault();
                context.ScheduleActivity(FailoverBehavior,
                    new CompletionCallback((c, cins) =>
                    {
                        //no need to take anu exception as this is not necessary in that point
                    })
                );
            }
            else if (FailoverBehavior is ManagedActionBehavior)
            {
                context.HandleFault();
                context.ScheduleActivity(FailoverBehavior,
                    new CompletionCallback((c, cins) =>
                    {

                    })
                );
            }
            else //NoneBehavior or anything else
            {
                throw propagatedException;
            }
        }

        private void OnChildExecutionCompleted(NativeActivityContext context, ActivityInstance completedInstance)
        {

            int num = this.lastIndexHint.Get(context);
            if (num >= this.Activities.Count || this.Activities[num] != completedInstance.Activity)
            {
                num = this.Activities.IndexOf(completedInstance.Activity);
            }
            int num2 = num + 1;
            if (num2 == this.Activities.Count)
            {
                this.LogFinish(context);
                return;
            }
            Activity activity = this.Activities[num2];
            context.ScheduleActivity(activity, this.OnChildExecutionCompletedCallback, this.OnChildExecutionFailedCallback);

            this.lastIndexHint.Set(context, num2);
        }


        private Collection<Activity> activities;

        private Collection<Variable> variables;
        private Variable<int> lastIndexHint;
        private Variable<int> lastRetryHint;


        [Browsable(false)]
        internal CompletionCallback OnChildExecutionCompletedCallback { get; set; }

        [Browsable(false)]
        internal FaultCallback OnChildExecutionFailedCallback { get; set; }


        [Browsable(false)]
        public Collection<Variable> Variables
        {
            get
            {
                if (this.variables == null)
                {
                    ValidatingCollection<Variable> coll = new ValidatingCollection<Variable>();
                    coll.OnAddValidationCallback = new Action<Variable>((item) => { if (item == null) { throw new ArgumentNullException("item"); } });
                    this.variables = coll;
                }
                return this.variables;
            }
        }

        [Browsable(false)]
        [DependsOn("Variables")]
        public Collection<Activity> Activities
        {
            get
            {
                if (this.activities == null)
                {
                    ValidatingCollection<Activity> coll = new ValidatingCollection<Activity>();
                    coll.OnAddValidationCallback = new Action<Activity>((item) => { if (item == null) { throw new ArgumentNullException("item"); } });
                    this.activities = coll;
                }

                return this.activities;
            }
        }


        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;



        #region Failover Behvior caches here...

        [Browsable(false)]
        public int ActiveFailoverBehaviorCacheIndex { get; set; } = -1;


        [Browsable(false)]
        public FailoverBehaviorBaseActivity Cache_ContinueOnErrorBehavior { get; set; }

        [Browsable(false)]
        public FailoverBehaviorBaseActivity Cache_RetryBehavior { get; set; }

        [Browsable(false)]
        public FailoverBehaviorBaseActivity Cache_ManagedActionBehavior { get; set; }
        public List<FailoverBehaviorBaseActivity> FailoverBehaviorCache
        {
            get
            {
                return new List<FailoverBehaviorBaseActivity>()
                {
                    this.Cache_RetryBehavior,
                    this.Cache_ContinueOnErrorBehavior, 
                    this.Cache_ManagedActionBehavior
                };
            }
        }
        #endregion


        #region Conditional run settings...

        [Browsable(false), DefaultValue(ConditionTypes.Unconditional)]
        public ConditionTypes ConditionType { get; set; }

        public bool Condition { get; set; }

        [Browsable(false)]
        public ActivityFunc<bool> ConditionActivity { get; set; }


        #endregion

        public LogConfiguration LogSettings { get; set; }


    }
}
