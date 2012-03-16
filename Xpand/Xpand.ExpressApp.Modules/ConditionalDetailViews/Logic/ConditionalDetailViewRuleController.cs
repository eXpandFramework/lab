﻿using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.SystemModule;
using DevExpress.ExpressApp.Templates;
using Xpand.ExpressApp.ConditionalDetailViews.Model;
using Xpand.ExpressApp.Logic;
using Xpand.ExpressApp.Logic.Conditional.Logic;
using Xpand.ExpressApp.Logic.Model;

namespace Xpand.ExpressApp.ConditionalDetailViews.Logic {
    public class ConditionalDetailViewRuleController : ConditionalLogicRuleViewController<IConditionalDetailViewRule> {
        IConditionalDetailViewRule _ruleForCustomProcessSelectedItem;
        IModelView _previousModel;

        protected override void OnActivated() {
            base.OnActivated();
            if (IsReady) {
                if (View is XpandListView)
                    Frame.GetController<ListViewProcessCurrentObjectController>().CustomProcessSelectedItem += OnCustomProcessSelectedItem;
                else {
                    ResetRules();
                }
            }
        }

        void ResetRules() {
            var previousModelController = Frame.GetController<StartUpInfoController>();
            if (previousModelController != null) {
                _previousModel = previousModelController.PreviousModel;
                if (previousModelController.ConditionalDetailViewRule != null)
                    defaultValuesRulesStorage.Add(previousModelController.ConditionalDetailViewRule, previousModelController.PreviousModel);
            }
        }


        void OnCustomProcessSelectedItem(object sender, CustomProcessListViewSelectedItemEventArgs e) {
            if (_ruleForCustomProcessSelectedItem != null) {
                e.Handled = true;
                var objectSpace = Application.CreateObjectSpace();
                var o = objectSpace.GetObject(e.InnerArgs.CurrentObject);
                var startUpInfoController = MakeRulesApplicable(o, _ruleForCustomProcessSelectedItem);
                var showViewParameters = e.InnerArgs.ShowViewParameters;
                showViewParameters.Controllers.Add(startUpInfoController);
                showViewParameters.CreatedView = Application.CreateDetailView(objectSpace, _ruleForCustomProcessSelectedItem.DetailView, true, o);
                _ruleForCustomProcessSelectedItem = null;
            }
        }

        StartUpInfoController MakeRulesApplicable(object o, IConditionalDetailViewRule ruleForCustomProcessSelectedItem) {
            var startUpInfoController = new StartUpInfoController(true);
            var viewId = Application.FindDetailViewId(o, View);
            var conditionalDetailViewRules = LogicRuleManager<IConditionalDetailViewRule>.Instance[View.ObjectTypeInfo];
            var viewEditMode = (View is DetailView ? ((DetailView)View).ViewEditMode : (ViewEditMode?)null);
            var validModelLogicRules = conditionalDetailViewRules.Where(rule => IsValidRule(rule, new ViewInfo(viewId, true, true, View.ObjectTypeInfo, viewEditMode)));
            foreach (var validModelLogicRule in validModelLogicRules) {
                startUpInfoController.PreviousModel = validModelLogicRule.View;
                startUpInfoController.ConditionalDetailViewRule = validModelLogicRule;
                validModelLogicRule.View = ruleForCustomProcessSelectedItem.DetailView;
            }
            return startUpInfoController;
        }

        public class StartUpInfoController : ViewController {
            public StartUpInfoController()
                : this(false) {
            }

            public StartUpInfoController(bool appropriateContext) {
                Active["AppropriateContext"] = appropriateContext;
            }

            public IModelView PreviousModel { get; set; }
            public IConditionalDetailViewRule ConditionalDetailViewRule { get; set; }
        }

        protected override void OnDeactivated() {
            base.OnDeactivated();
            if (View is XpandListView)
                Frame.GetController<ListViewProcessCurrentObjectController>().CustomProcessSelectedItem += OnCustomProcessSelectedItem;
            foreach (var defaultValuePair in defaultValuesRulesStorage) {
                defaultValuePair.Key.View = defaultValuePair.Value;
            }
        }
        protected override void Dispose(bool disposing) {
            if (disposing)
                Frame.TemplateChanged -= FrameOnTemplateChanged;
            base.Dispose(disposing);
        }
        protected override IModelLogic GetModelLogic() {
            return ((IModelApplicationConditionalDetailView)Application.Model).ConditionalDetailView;
        }
        protected override void OnFrameAssigned() {
            base.OnFrameAssigned();
            Frame.TemplateChanged += FrameOnTemplateChanged;
        }
        void FrameOnTemplateChanged(object sender, EventArgs eventArgs) {
            var supportViewChanged = (Frame.Template) as ISupportViewChanged;
            if (supportViewChanged != null)
                supportViewChanged.ViewChanged += (o, args) => {
                    Active[ActiveObjectTypeHasRules] = LogicRuleManager<IConditionalDetailViewRule>.HasRules(args.View);
                    if (Active) {
                        args.View.SetModel(_detailView);
                    }
                };
        }

        readonly Dictionary<IConditionalDetailViewRule, IModelView> defaultValuesRulesStorage = new Dictionary<IConditionalDetailViewRule, IModelView>();
        IModelDetailView _detailView;

        public override void ExecuteRule(LogicRuleInfo<IConditionalDetailViewRule> info, ExecutionContext executionContext) {
            if (info.Active && !info.InvertingCustomization) {
                if (executionContext == ExecutionContext.CustomProcessSelectedItem)
                    _ruleForCustomProcessSelectedItem = info.Rule;
                else if (executionContext == ExecutionContext.CurrentObjectChanged) {
                    _previousModel = View.Model;
                    View.SetModel(info.Rule.DetailView);
                    if (!defaultValuesRulesStorage.ContainsKey(info.Rule))
                        defaultValuesRulesStorage.Add(info.Rule, info.Rule.View);
                    info.Rule.View = info.Rule.DetailView;
                } else if (executionContext == ExecutionContext.ViewShowing) {
                    info.View.SetModel(info.Rule.DetailView);
                } else if (executionContext == ExecutionContext.ViewChanged) {
                    _detailView = info.Rule.DetailView;
                }
            } else if (info.InvertingCustomization) {
                if (executionContext == ExecutionContext.CurrentObjectChanged) {
                    if (_previousModel != null && _previousModel != View.Model) {
                        View.SetModel(_previousModel);
                        info.Rule.View = _previousModel;
                    }
                }
            }
        }

    }
}