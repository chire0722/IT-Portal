using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IT_Form.Application.Features.SharedMailbox;
using IT_Form.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;
using IT_Form.Tests.Helpers;

namespace IT_Form.Tests
{
    public class SharedMailboxControllerTests : ControllerTestBase
    {
        [Fact]
        public async Task Index_ReturnsView_ForITRole()
        {
            using var context = CreateContext();
            context.SharedMailboxes.Add(new IT_Form.Models.SharedMailbox { MailboxName = "info@ytc.com", IsActive = true });
            await context.SaveChangesAsync();

            var controller = GetController<SharedMailboxController>(context, role: "IT");
            var result = await controller.Index();

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<SharedMailboxIndexListItem>>(viewResult.ViewData.Model);
            Assert.Single(model);
        }

        [Fact]
        public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
        {
            using var context = CreateContext();
            var controller = GetController<SharedMailboxController>(context);

            var result = await controller.Create((SharedMailboxEditorPageViewModel)null!);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Edit_Post_GuardAgainstTampering_Adversarial()
        {
            using var context = CreateContext();
            var mailbox = new IT_Form.Models.SharedMailbox { MailboxName = "test@ytc.com", MailboxAddress = "test@ytc.com", IsActive = true };
            context.SharedMailboxes.Add(mailbox);
            await context.SaveChangesAsync();

            var controller = GetController<SharedMailboxController>(context);

            var result = await controller.Edit(mailbox.Id + 1, new SharedMailboxEditorPageViewModel
            {
                Id = mailbox.Id,
                MailboxName = mailbox.MailboxName,
                MailboxAddress = mailbox.MailboxAddress,
                IsActive = mailbox.IsActive
            });

            Assert.IsType<NotFoundResult>(result);
        }
    }
}
