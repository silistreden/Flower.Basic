using System;
using System.Activities.Presentation.Metadata;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flower.Basic.Activities.Design
{
    public class ActivityDesignerMetadata : IRegisterMetadata
    {
        public void Register()
        {
            Trace.WriteLine("IRegisterMetadata initialized...");
            RegisterAll();
        }

        public void RegisterAll()
        {
            AttributeTableBuilder builder = new AttributeTableBuilder();
            //builder.AddCustomAttributes(typeof(Activities.Scope), new DesignerAttribute(typeof(ScopeDesigner)));
            //builder.AddCustomAttributes(typeof(Activities.Behaviors.ContinueOnErrorBehavior), new DesignerAttribute(typeof(Behaviors.ContinueOnError)));
            //builder.AddCustomAttributes(typeof(Activities.Behaviors.RetryBehavior), new DesignerAttribute(typeof(Behaviors.Retry)));
            //builder.AddCustomAttributes(typeof(Activities.Behaviors.ManagedActionBehavior), new DesignerAttribute(typeof(Behaviors.ManagedActions)));


            ////builder.AddCustomAttributes(typeof(Activities.Scope), new DesignerAttribute(typeof(Scope)));

            //builder.AddCustomAttributes(typeof(Activities.Common.FailoverScope), new DesignerAttribute(typeof(Common.FailoverScope)));

            ////builder.AddCustomAttributes(typeof(Activities.Logical.IfElseIf), new DesignerAttribute(typeof(Logical.IfElseIf)));
            ////builder.AddCustomAttributes(typeof(Activities.Logical.IfElseIfBranch), new DesignerAttribute(typeof(ConditionalScope)));
            //builder.AddCustomAttributes(typeof(Activities.Excel.WorkbookScope), new DesignerAttribute(typeof(Excel.WorkbookScope)));
            //builder.AddCustomAttributes(typeof(Activities.Flow), new DesignerAttribute(typeof(FlowDesigner)));


            MetadataStore.AddAttributeTable(builder.CreateTable());
        }
    }

}
