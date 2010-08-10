﻿using System.Collections.Generic;
using DevExpress.Xpo.DB;
using eXpand.ExpressApp.WorldCreator.SqlDBMapper;
using eXpand.Persistent.Base.PersistentMetaData;
using eXpand.Persistent.Base.PersistentMetaData.PersistentAttributeInfos;
using eXpand.Persistent.BaseImpl.PersistentMetaData;
using eXpand.Persistent.BaseImpl.PersistentMetaData.PersistentAttributeInfos;
using Machine.Specifications;
using Microsoft.SqlServer.Management.Smo;
using TypeMock.ArrangeActAssert;
using System.Linq;
using eXpand.ExpressApp.WorldCreator.Core;

namespace eXpand.Tests.eXpand.WorldCreator.DbMapper
{
    [Subject(typeof(ColumnMapper))]
    public class When_passing_a_non_FK_column_to_a_property_builder : With_Column
    {
        static DataTypeMapper _dataTypeMapper;
        static PersistentCoreTypeMemberInfo _persistentMemberInfo;
        static List<IPersistentAttributeInfo> _persistentAttributeInfos;
        static AttributeMapper _attributeMapper;

        Establish context = () =>
        {
            Isolate.WhenCalled(() => _column.DataType.SqlDataType).WillReturn(SqlDataType.Int);
            _dataTypeMapper = new DataTypeMapper();
            _attributeMapper = new AttributeMapper(ObjectSpace, new DataTypeMapper());
            _persistentAttributeInfos = new List<IPersistentAttributeInfo> {Isolate.Fake.Instance<PersistentAttributeInfo>(Members.CallOriginal,ConstructorWillBe.Called,new object[]{UnitOfWork})};
            Isolate.WhenCalled(() => _attributeMapper.Create(null, Isolate.Fake.Instance<IPersistentMemberInfo>())).WillReturn(_persistentAttributeInfos);
        };

        Because of = () => { _persistentMemberInfo = new ColumnMapper(_dataTypeMapper, _attributeMapper).Create(_column,_owner) as PersistentCoreTypeMemberInfo; };
        It should_return_a_persistentcorememberinfo = () => _persistentMemberInfo.ShouldNotBeNull();
        It should_have_as_name_the_column_name = () => _persistentMemberInfo.Name.ShouldEqual(_column.Name);
        It should_have_as_owner_the_passed_in_owner_classinfo = () => _persistentMemberInfo.Owner.ShouldEqual(_owner);
        It should_have_as_datatype_the_one_taken_from_datatypemapper =
            () => _persistentMemberInfo.DataType.ShouldEqual(DBColumnType.Int32);
        It should_have_a_ReadWriteMember_template =
                    () => _persistentMemberInfo.CodeTemplateInfo.CodeTemplate.TemplateType.ShouldEqual(TemplateType.XPReadWritePropertyMember);
        It should_have_the_attributes_of_the_attributebuilder =
            () => _persistentMemberInfo.TypeAttributes.ShouldContainOnly((PersistentAttributeInfo)_persistentAttributeInfos[0]);
    }


    public class When_passing_an_FK_column_to_a_property_builder:With_ForeignKey_Column
    {
        static ColumnMapper _columnMapper;
        static PersistentCollectionMemberInfo _persistentCollectionMemberInfo;
        static PersistentReferenceMemberInfo _persistentReferenceMemberInfo;

        Establish context = () => {
            _columnMapper = new ColumnMapper(new DataTypeMapper(), new AttributeMapper(ObjectSpace, new DataTypeMapper()));
        };

        Because of = () => {
            _persistentReferenceMemberInfo = _columnMapper.Create(_column,_ownner) as PersistentReferenceMemberInfo;
        };

        It should_create_a_persistenetReferenceMember = () => _persistentReferenceMemberInfo.ShouldNotBeNull();

        It should_have_as_name_the_name_of_the_column =
            () => _persistentReferenceMemberInfo.Name.ShouldEqual(_column.Name);

        It should_have_as_ReferenceClassInfo_the_one_with_the_name_of_the_FK_reference_table = () => _persistentReferenceMemberInfo.ReferenceClassInfo.Name.ShouldEqual(RefTable);

        It should_create_a_collection_association_to_the_referenceclassinfo =
            () => {_persistentCollectionMemberInfo =_persistentReferenceMemberInfo.ReferenceClassInfo.OwnMembers.OfType<PersistentCollectionMemberInfo>().FirstOrDefault();
                _persistentCollectionMemberInfo.ShouldNotBeNull();
            };

        It should_have_as_name_the_name_of_the_column_plus_an_s =() => _persistentCollectionMemberInfo.Name.ShouldEqual(_column.Name + "s");

        It should_have_as_owner_the_referenceClassInfo =() => _persistentCollectionMemberInfo.Owner.ShouldEqual(_persistentReferenceMemberInfo.ReferenceClassInfo);
        It should_have_as_classinfo_the_owner_of_the_reference_memberInfo = () => _persistentCollectionMemberInfo.CollectionClassInfo.ShouldEqual(_persistentReferenceMemberInfo.Owner);

        It should_have_an_association_with_the_same_name_as_the_reference_member_association =
            () =>
            _persistentCollectionMemberInfo.TypeAttributes.OfType<PersistentAssociationAttribute>().Single().
                AssociationName =
            _persistentReferenceMemberInfo.TypeAttributes.OfType<PersistentAssociationAttribute>().Single().
                AssociationName;

        It should_have_a_collectionMember_template =
            () =>
            _persistentCollectionMemberInfo.CodeTemplateInfo.CodeTemplate.TemplateType.ShouldEqual(TemplateType.XPCollectionMember);
    }

    public class When_passing_a_column_with_unknow_datatype:With_Column {
        static IPersistentCoreTypeMemberInfo _persistentCoreTypeMemberInfo;
        static DataTypeMapper _dataTypeMapper;

        Establish context = () => {
            _dataTypeMapper = new DataTypeMapper();
            Isolate.WhenCalled(() => _dataTypeMapper.GetDataType(_column)).WillReturn(DBColumnType.Unknown);
        };

        Because of = () => {
            var attributeBuilder = new AttributeMapper(ObjectSpace, new DataTypeMapper());
            var persistentClassInfo = new PersistentClassInfo(UnitOfWork){PersistentAssemblyInfo = new PersistentAssemblyInfo(UnitOfWork)};
            persistentClassInfo.SetDefaultTemplate(TemplateType.Class);
            _persistentCoreTypeMemberInfo = new ColumnMapper(_dataTypeMapper, attributeBuilder).Create(_column,persistentClassInfo) as IPersistentCoreTypeMemberInfo;
        };

        It should_create_a_persistent_core_typeinfo = () => _persistentCoreTypeMemberInfo.ShouldNotBeNull();

        It should_have_an_unknown_datatype =
            () => _persistentCoreTypeMemberInfo.DataType.ShouldEqual(DBColumnType.Unknown);
    }

    public class When_passing_a_Primary_key_and_the_table_has_more_primary_keys:With_Column {
        static IPersistentCoreTypeMemberInfo _persistentCoreTypeMemberInfo;
        static IPersistentClassInfo _persistentStructClassInfo;
        static IPersistentReferenceMemberInfo _persistentMemberInfo;
        

        Establish context = () => {
            Isolate.WhenCalled(() => _column.InPrimaryKey).WillReturn(true);
            _table.Columns.Add(_column);
            _persistentStructClassInfo = new PersistentClassInfo(UnitOfWork) {Name = _table.Name + TableMapper.KeyStruct,PersistentAssemblyInfo = new PersistentAssemblyInfo(UnitOfWork)};
            _persistentStructClassInfo.SetDefaultTemplate(TemplateType.Class);
        };

        Because of = () => {
            var dataTypeMapper = new DataTypeMapper();
            var attributeMapper = new AttributeMapper(ObjectSpace, dataTypeMapper);
            _persistentMemberInfo = new ColumnMapper(dataTypeMapper, attributeMapper).Create(_column, _owner) as IPersistentReferenceMemberInfo;
        };

        

        It should_create_areference_member_info = () => _persistentMemberInfo.ShouldNotBeNull();

        It should_have_as_reference_classinfo_the_one_with_the_same_name_as_the_column_table_name_plus_KeyStruct =
            () => _persistentMemberInfo.ReferenceClassInfo.ShouldEqual(_persistentStructClassInfo);

        It should_have_as_teamplate_The_readWriteMember =
            () =>_persistentMemberInfo.CodeTemplateInfo.CodeTemplate.TemplateType.ShouldEqual(TemplateType.FieldMember);

        It should_add_the_primary_key_as_core_member_info_in_the_reference_class_info = () => {
            _persistentCoreTypeMemberInfo = _persistentStructClassInfo.OwnMembers.OfType<IPersistentCoreTypeMemberInfo>().FirstOrDefault();
        };

        It should_set_the_template_of_the_core_member_to_readWrite =
            () =>
            _persistentCoreTypeMemberInfo.CodeTemplateInfo.CodeTemplate.TemplateType.ShouldEqual(TemplateType.ReadWriteMember);
        It should_have_the_name_of_the_column = () => _persistentCoreTypeMemberInfo.Name.ShouldEndWith(ColumnName);

        

    }

    public class When_passing_a_primary_key_that_is_foreign_key_and_table_has_another_key:With_ForeignKey_Column {
        protected static string _unUsedReferencedKey;
        static PersistentClassInfo _persistentStructClassInfo;
        static PersistentClassInfo _persistentClassInfo;

        Establish context = () =>
        {
            _persistentStructClassInfo = new PersistentClassInfo(UnitOfWork) { Name = _table.Name + TableMapper.KeyStruct, PersistentAssemblyInfo = new PersistentAssemblyInfo(UnitOfWork) };
            _persistentStructClassInfo.SetDefaultTemplate(TemplateType.Struct);
            Isolate.WhenCalled(() => _column.InPrimaryKey).WillReturn(true);
            _unUsedReferencedKey = _foreignKey.ReferencedKey;
            Isolate.WhenCalled(() => _foreignKey.ReferencedKey).WillReturn("PK_");
            _table.Columns.Add(_column);
        };

        Because of = () => new ColumnMapper(new DataTypeMapper(), new AttributeMapper(ObjectSpace, new DataTypeMapper())).Create(_column, _ownner);

        It should_create_a_collection_to_the_referenced_classInfo =
            () =>_refPersistentClassInfo.OwnMembers.OfType<PersistentCollectionMemberInfo>().FirstOrDefault().ShouldNotBeNull();

        It should_create_a_reference_member_to_the_struct_classinfo =
            () =>
            _persistentStructClassInfo.OwnMembers.OfType<IPersistentReferenceMemberInfo>().FirstOrDefault().
                ShouldNotBeNull();
    }

    public class When_passing_a_column_that_is_a_foreign_key_to_a_column_that_is_a_foreign_key_to_this_column : With_ForeignKey_Column
    {
        static ITemplateInfo _templateInfo;
        
        static IPersistentReferenceMemberInfo _persistentReferenceMemberInfo2;

        static IPersistentReferenceMemberInfo _persistentReferenceMemberInfo;

        Establish context = () => {
            Isolate.WhenCalled(() => _column.InPrimaryKey).WillReturn(true);
            AddForeignKey("FK_RefName", _table.Name, ColumnName,_refTable,_refColumn);
        };

        Because of = () => {
            _persistentReferenceMemberInfo = new ColumnMapper(new DataTypeMapper(), new AttributeMapper(ObjectSpace, new DataTypeMapper())).Create(_column, _ownner) as IPersistentReferenceMemberInfo;
        };

        It should_return_a_reference_memberinfo = () => _persistentReferenceMemberInfo.ShouldNotBeNull();
        It should_have_a_one_to_one_template = () => _persistentReferenceMemberInfo.CodeTemplateInfo.CodeTemplate.TemplateType.ShouldEqual(TemplateType.XPOneToOnePropertyMember);

        It should_have_a_templateinfo_in_its_templateinfos_collection =() => {
            _templateInfo = _persistentReferenceMemberInfo.TemplateInfos.FirstOrDefault();
            _templateInfo.ShouldNotBeNull();
        };

        It should_have_as_code_in_the_templateinfo_the_name_of_the_referenced_column =
            () => _templateInfo.TemplateCode.ShouldEqual(RefTablePKFKColumn);

//        It should_create_a_reference_memberinfo_to_the_reference_classinfo = () => {
//            _persistentReferenceMemberInfo2 = _refPersistentClassInfo.OwnMembers.OfType<IPersistentReferenceMemberInfo>().Single();
//            _persistentReferenceMemberInfo2.Name.ShouldEqual("PK_");
//        };

//        It should_have_a_one_to_one_template_at_the_reference_classInfo_as_well = () => _persistentReferenceMemberInfo2.CodeTemplateInfo.CodeTemplate.TemplateType.ShouldEqual(TemplateType.XPOneToOnePropertyMember);
    }
}