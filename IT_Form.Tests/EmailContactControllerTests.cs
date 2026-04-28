// Project comment: EmailContactControllerTests.cs is part of the IT Form application source code.

using System.Collections.Generic;
using System.Threading.Tasks;
using IT_Form.Application.Features.EmailContact;
using IT_Form.Controllers;
using IT_Form.Models;
using IT_Form.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace IT_Form.Tests;

/// <summary>
/// Defines the EmailContactControllerTests class used by the IT Form application.
/// </summary>
public class EmailContactControllerTests : ControllerTestBase
{
    [Fact]
    /// <summary>
    /// Runs the Index_ReturnsView operation.
    /// </summary>
    public async Task Index_ReturnsView()
    {
        using var context = CreateContext();
        context.EmailContacts.Add(new EmailContact { Name = "John Doe", EmailAddress = "john@test.com" });
        await context.SaveChangesAsync();

        var controller = GetController<EmailContactController>(context, role: "IT");
        var result = await controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<EmailContactIndexListItem>>(viewResult.ViewData.Model);
        Assert.Single(model);
    }

    [Fact]
    /// <summary>
    /// Creates data for the Create_Post_ReturnsView_WhenModelIsNull_Adversarial operation.
    /// </summary>
    public async Task Create_Post_ReturnsView_WhenModelIsNull_Adversarial()
    {
        using var context = CreateContext();
        var controller = GetController<EmailContactController>(context);

        var result = await controller.Create((EmailContactEditorPageViewModel)null!);

        Assert.IsType<ViewResult>(result);
    }
}
