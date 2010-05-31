using System;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Model.NodeGenerators;
using DevExpress.ExpressApp.Security;
using DevExpress.Xpo;
using DevExpress.Xpo.Metadata;
using eXpand.ExpressApp.ModelDifference.DataStore.BaseObjects;
using eXpand.ExpressApp.ModelDifference.DataStore.Builders;
using eXpand.ExpressApp.ModelDifference.DictionaryStores;

namespace eXpand.ExpressApp.ModelDifference
{
    public sealed partial class ModelDifferenceModule : ModuleBase
    {
        private static ModelApplicationCreator _ModelApplicationCreator;
        public static ModelApplicationCreator ModelApplicationCreator
        {
            get
            {
                return _ModelApplicationCreator;
            }
            set
            {
                _ModelApplicationCreator = value;
            }
        }

        private static XafApplication _Application;
        public static XafApplication Application
        {
            get
            {
                return _Application;
            }
        }

        public ModelDifferenceModule()
        {
            this.RequiredModuleTypes.Add(typeof(DevExpress.ExpressApp.CloneObject.CloneObjectModule));
        }

        public override void CustomizeTypesInfo(ITypesInfo typesInfo)
        {
            base.CustomizeTypesInfo(typesInfo);

            if (Application != null && Application.Security != null)
            {
                if (Application.Security is ISecurityComplex)
                    RoleDifferenceObjectBuilder.CreateDynamicMembers((ISecurityComplex)Application.Security);

                UserDifferenceObjectBuilder.CreateDynamicMembers(Application.Security.UserType);
            }
            else
            {
                createDesignTimeCollection(typesInfo, typeof(UserModelDifferenceObject), "Users");
                createDesignTimeCollection(typesInfo, typeof(RoleModelDifferenceObject), "Roles");
            }
        }

        private void createDesignTimeCollection(ITypesInfo typesInfo, Type classType, string propertyName)
        {
            XPClassInfo info = XafTypesInfo.XpoTypeInfoSource.XPDictionary.GetClassInfo(classType);
            if (info.FindMember(propertyName) == null)
            {
                info.CreateMember(propertyName, typeof(XPCollection), true);
                typesInfo.RefreshInfo(classType);
            }
        }

        public override void Setup(XafApplication application)
        {
            base.Setup(application);
            application.CreateCustomUserModelDifferenceStore += ApplicationOnCreateCustomUserModelDifferenceStore;
            if (_Application == null)
                _Application = application;
        }

        public override void AddGeneratorUpdaters(ModelNodesGeneratorUpdaters updaters)
        {
            base.AddGeneratorUpdaters(updaters);
            updaters.Add(new BOModelNodesUpdater());
        }

        private void ApplicationOnCreateCustomUserModelDifferenceStore(object sender, CreateCustomModelDifferenceStoreEventArgs args)
        {
            args.Handled = true;
            args.Store = new XpoUserModelDictionaryDifferenceStore(Application);
        }
    }

    public class BOModelNodesUpdater : ModelNodesGeneratorUpdater<ModelBOModelClassNodesGenerator>
    {
        public override void UpdateNode(ModelNode node)
        {
            var classNode = ((IModelBOModel)node)[typeof(RoleModelDifferenceObject).FullName];
            if (SecuritySystem.UserType != null && !(SecuritySystem.Instance is ISecurityComplex) && classNode != null)
            {
                node.Remove((ModelNode)classNode);
            }
        }
    }
}