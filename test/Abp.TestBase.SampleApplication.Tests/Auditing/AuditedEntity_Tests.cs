﻿using System;
using System.Linq;
using Abp.Configuration.Startup;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.TestBase.SampleApplication.Crm;
using Abp.TestBase.SampleApplication.Messages;
using Abp.Timing;
using Shouldly;
using Xunit;

namespace Abp.TestBase.SampleApplication.Tests.Auditing
{
    public class AuditedEntity_Tests : SampleApplicationTestBase
    {
        private readonly IRepository<Message> _messageRepository;
        private readonly IRepository<Company> _companyRepository;

        public AuditedEntity_Tests()
        {
            _messageRepository = Resolve<IRepository<Message>>();
            _companyRepository = Resolve<IRepository<Company>>();
        }

        [Fact]
        public void Should_Write_Audit_Properties()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: Create a new entity
            var createdMessage = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));

            //Assert: Check creation properties
            createdMessage.CreatorUserId.ShouldBe(AbpSession.UserId);
            createdMessage.CreationTime.ShouldBeGreaterThan(Clock.Now.Subtract(TimeSpan.FromSeconds(10)));
            createdMessage.CreationTime.ShouldBeLessThan(Clock.Now.Add(TimeSpan.FromSeconds(10)));

            //Act: Select the same entity
            var selectedMessage = _messageRepository.Get(createdMessage.Id);

            //Assert: Select should not change audit properties
            selectedMessage.EntityEquals(createdMessage);

            selectedMessage.CreationTime.ShouldBe(createdMessage.CreationTime);
            selectedMessage.CreatorUserId.ShouldBe(createdMessage.CreatorUserId);

            selectedMessage.LastModifierUserId.ShouldBe(null);
            selectedMessage.LastModificationTime.ShouldBe(null);

            selectedMessage.IsDeleted.ShouldBeFalse();
            selectedMessage.DeleterUserId.ShouldBe(null);
            selectedMessage.DeletionTime.ShouldBe(null);

            //Act: Update the entity
            selectedMessage.Text = "test message 1 - updated";
            _messageRepository.Update(selectedMessage);

            //Assert: Modification properties should be changed
            selectedMessage.LastModifierUserId.ShouldBe(AbpSession.UserId);
            selectedMessage.LastModificationTime.ShouldNotBe(null);
            selectedMessage.LastModificationTime.Value.ShouldBeGreaterThan(
                Clock.Now.Subtract(TimeSpan.FromSeconds(10)));
            selectedMessage.LastModificationTime.Value.ShouldBeLessThan(Clock.Now.Add(TimeSpan.FromSeconds(10)));

            //Act: Delete the entity
            _messageRepository.Delete(selectedMessage);

            //Assert: Deletion audit properties should be set
            selectedMessage.IsDeleted.ShouldBe(true);
            selectedMessage.DeleterUserId.ShouldBe(AbpSession.UserId);
            selectedMessage.DeletionTime.ShouldNotBe(null);
            selectedMessage.DeletionTime.Value.ShouldBeGreaterThan(Clock.Now.Subtract(TimeSpan.FromSeconds(10)));
            selectedMessage.DeletionTime.Value.ShouldBeLessThan(Clock.Now.Add(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public void Should_Not_Set_Audit_User_Properties_Of_Host_Entities_By_Tenant_User()
        {
            Resolve<IMultiTenancyConfig>().IsEnabled = true;

            //Login as host
            AbpSession.TenantId = null;
            AbpSession.UserId = 42;

            //Get a company to modify
            var company = _companyRepository.GetAllList().First();
            company.LastModifierUserId.ShouldBe(null); //initial value

            //Modify the company
            company.Name = company.Name + "1";
            _companyRepository.Update(company);

            //LastModifierUserId should be set
            company.LastModifierUserId.ShouldBe(42);

            //Login as a tenant
            AbpSession.TenantId = 1;
            AbpSession.UserId = 43;

            //Get the same company to modify
            company = _companyRepository.FirstOrDefault(company.Id);
            company.ShouldNotBeNull();
            company.LastModifierUserId.ShouldBe(42); //Previous user's id

            //Modify the company
            company.Name = company.Name + "1";
            _companyRepository.Update(company);

            //LastModifierUserId should set to null since a tenant changing a host entity
            company.LastModifierUserId.ShouldBe(null);
        }

        [Fact]
        public void Should_Not_Set_CreatorUserId_When_DisableCreatorUserId_Configuration_Is_Disabled()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: CreatorUserId
            using (var uowManager = LocalIocManager.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin())
                {
                    using (uowManager.Object.Current.DisableAuditing(AbpAuditFields.CreationUserId))
                    {
                        var message = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));
                        message.CreatorUserId.ShouldBeNull();
                        uow.Complete();
                    }
                }
            }
        }

        [Fact]
        public void Should_Not_Set_LastModifierUserId_When_DisableLastModifierUserId_Configuration_Is_Disabled()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: Create a new entity
            var message = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));

            //Act: CreatorUserId
            using (var uowManager = LocalIocManager.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin())
                {
                    using (uowManager.Object.Current.DisableAuditing(AbpAuditFields.LastModifierUserId))
                    {
                        message.Text = "edited test message 1";
                        _messageRepository.Update(message);
                        uow.Complete();
                    }
                }
            }

            message.LastModifierUserId.ShouldBeNull();
        }

        [Fact]
        public void Should_Not_Set_DeleterUserId_When_DisableDeleterUserId_Configuration_Is_Disabled()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: Create a new entity
            var message = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));

            //Act: CreatorUserId
            using (var uowManager = LocalIocManager.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin())
                {
                    using (uowManager.Object.Current.DisableAuditing(AbpAuditFields.DeleterUserId))
                    {
                        _messageRepository.Delete(message);
                        uow.Complete();
                    }
                }
            }

            message.DeleterUserId.ShouldBeNull();
        }

        [Fact]
        public void Should_Set_CreatorUserId_When_DisableCreatorUserId_Configuration_Is_Enabled()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: CreatorUserId
            using (var uowManager = LocalIocManager.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin())
                {
                    using (uowManager.Object.Current.DisableAuditing(AbpAuditFields.CreationUserId))
                    {
                        using (uowManager.Object.Current.EnableAuditing(AbpAuditFields.CreationUserId))
                        {
                            var message = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));
                            message.CreatorUserId.ShouldNotBeNull();
                            uow.Complete();
                        }
                    }
                }
            }
        }

        [Fact]
        public void Should_Set_LastModifierUserId_When_DisableLastModifierUserId_Configuration_Is_Enabled()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: Create a new entity
            var message = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));

            //Act: CreatorUserId
            using (var uowManager = LocalIocManager.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin())
                {
                    using (uowManager.Object.Current.DisableAuditing(AbpAuditFields.LastModifierUserId))
                    {
                        using (uowManager.Object.Current.EnableAuditing(AbpAuditFields.LastModifierUserId))
                        {
                            message.Text = "edited test message 1";
                            _messageRepository.Update(message);
                            uow.Complete();
                        }
                    }
                }
            }

            message.LastModifierUserId.ShouldNotBeNull();
        }

        [Fact]
        public void Should_Set_DeleterUserId_When_DisableDeleterUserId_Configuration_Is_Enabled()
        {
            //Arrange
            AbpSession.TenantId = 1;
            AbpSession.UserId = 2;

            //Act: Create a new entity
            var message = _messageRepository.Insert(new Message(AbpSession.TenantId, "test message 1"));

            //Act: CreatorUserId
            using (var uowManager = LocalIocManager.ResolveAsDisposable<IUnitOfWorkManager>())
            {
                using (var uow = uowManager.Object.Begin())
                {
                    using (uowManager.Object.Current.DisableAuditing(AbpAuditFields.DeleterUserId))
                    {
                        using (uowManager.Object.Current.EnableAuditing(AbpAuditFields.DeleterUserId))
                        {
                            _messageRepository.Delete(message);
                            uow.Complete();
                        }
                    }
                }
            }

            message.DeleterUserId.ShouldNotBeNull();
        }
    }
}
